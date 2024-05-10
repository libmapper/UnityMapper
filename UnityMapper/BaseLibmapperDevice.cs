using Unity.Collections.LowLevel.Unsafe;
using Unity.Jobs;
using UnityMapper.API;
using UnityMapper.Builtin;

namespace UnityMapper;
using System.Reflection;
using Mapper;
using UnityEngine;
using UnityEngine.Serialization;
using Type = System.Type;

public abstract class BaseLibmapperDevice : MonoBehaviour
{
    
    protected Device Device;

    public ulong InstanceId { get; set; }
    
    private readonly Dictionary<Type, IPropertyExtractor> _extractors = new();
    private readonly Dictionary<Type, ITypeConverter> _converters = new();
    protected bool Frozen = false; // when frozen, no new extractors, mappers, or signals can be added

    //private System.Collections.Generic.List<(Signal, IMappedProperty, Mapper.Time lastChanged)> _properties = [];

    [SerializeField] private int pollTime = 1;
    
    [FormerlySerializedAs("_componentsToMap")] [SerializeField]
    private System.Collections.Generic.List<Component> componentsToExpose = [];

    private PollJob? _job;
    
    /// <summary>
    /// Called before the device is frozen, allowing for custom extractors, mappers, and signals to be registered.
    /// Overriding this method is an alternative to obtaining a reference to the device and calling
    /// RegisterExtractor, RegisterTypeConverter, and AddComponent manually.
    /// </summary>
    public virtual void RegisterExtensions()
    {
        
    }
    
    protected void PollBegin()
    {
        if (_handle != null)
        {
            _handle.Value.Complete();
        } 
    }
    
    protected void PollEnd()
    {
        _job ??= new PollJob(Device._obj, pollTime);

        _handle = _job.Value.Schedule();
    }

    protected void DiscoverProperties(Dictionary<string, (IAccessibleProperty prop, Component owner)> storage)
    {
        foreach (var component in componentsToExpose)
        {
            var maps = CreateAccessors(component);
                
            // TODO: this is REALLY ugly, fix later
            foreach (var mapped in maps)
            {
                var wrappedMap = mapped; // wrapped version to primitive-ize type
                var kind = mapped.BackingType;
                var type = CreateLibmapperTypeFromPrimitive(kind);

                if (type == Mapper.Type.Null)
                {
                    var mapper = _converters[wrappedMap.BackingType];
                    if (mapper == null)
                    {
                        throw new ArgumentException("No mapper found for type: " + wrappedMap.BackingType);
                    }

                    type = CreateLibmapperTypeFromPrimitive(mapper.SimpleType);
                    if (type == Mapper.Type.Null)
                    {
                        throw new ArgumentException("Mapper type is not a simple type: " + mapper.GetType());
                    }

                    wrappedMap = new WrappedAccessibleProperty(mapped, mapper);
                }
                storage.Add(wrappedMap.Name, (wrappedMap, component));
            }
        }
    }
    
    private Dictionary<string, Mapper.Time> _lastChanged = new();

    protected void UpdateSignals(IEnumerable<(IAccessibleProperty prop, Signal signal, Component owner)> properties)
    {
        foreach (var (prop, signal, owner) in properties)
        {
            if (!_lastChanged.ContainsKey(prop.Name))
            {
                _lastChanged[prop.Name] = new Mapper.Time(0);
            }
            
            var lastChanged = _lastChanged[prop.Name]!;
            var signalValue = signal.GetValue(InstanceId);
            
            if (signalValue.Item2 > lastChanged && signalValue.Item1 != null)
            {
                // incoming update from the network
                prop.SetObject(owner, signalValue.Item1);
                _lastChanged[prop.Name].Set(signalValue.Item2);
            }
            else
            {
                // push current value to  network
                signal.SetValue(prop.GetValue(owner), InstanceId);
                _lastChanged[prop.Name].Set(Device.GetTime());
            }
        }
    }
    
    private bool _lastReady = false;
    private JobHandle? _handle;
    
    /// <summary>
    /// Register a property extractor for a specific component type.
    /// </summary>
    /// <param name="extractor">Object that will produce a list of properties when given a component</param>
    /// <typeparam name="T">Component type being targeted</typeparam>
    public void RegisterExtractor<T>(IPropertyExtractor<T> extractor) where T : Component
    {
        if (Frozen)
        {
            throw new InvalidOperationException("Can't register new extractors after Freeze(). Make sure \"Use API\" is checked in the inspector.");
        }
        if (typeof(T) == typeof(Component))
        {
            throw new ArgumentException("Can't override generic extractor for Component type");
        }
        _extractors[typeof(T)] = extractor;
    }
    
    /// <summary>
    /// Register a type converter for libmapper to automatically convert complex types into simple types.
    /// </summary>
    /// <param name="converter">A type mapper</param>
    /// <typeparam name="T">The complex type</typeparam>
    /// <typeparam name="U">The primitive type</typeparam>
    public void RegisterTypeConverter<T, U>(ITypeConverter<T, U> converter) where T : notnull where U : notnull
    {
        if (Frozen)
        {
            throw new InvalidOperationException("Can't register new converters after Freeze(). Make sure \"Use API\" is checked in the inspector.");
        }
        _converters[typeof(T)] = converter;
    }
    
    /// <summary>
    /// Add a new component to be exposed by the device. 
    /// </summary>
    /// <param name="component"></param>
    /// <exception cref="InvalidOperationException">If called while the device is frozen</exception>
    public void AddComponent(Component component)
    {
        if (Frozen)
        {
            throw new InvalidOperationException("Can't add new components after Freeze(). Make sure \"Use API\" is checked in the inspector.");
        }
        componentsToExpose.Add(component);
    }

    /// <summary>
    /// Freeze the device, preventing new extractors, mappers, or components from being added.
    /// </summary>
    public void Freeze()
    {
        Frozen = true;
    }

    protected static Mapper.Type CreateLibmapperTypeFromPrimitive(Type t)
    {
        if (t.IsArray)
        {
            t = t.GetElementType();
        }
        if (t == typeof(float))
        {
            return Mapper.Type.Float;
        }
        else if (t == typeof(int))
        {
            return Mapper.Type.Int32;
        }
        else if (t == typeof(double))
        {
            return Mapper.Type.Double;
        }
        else
        {
            return Mapper.Type.Null;
        }
    }


    private System.Collections.Generic.List<IAccessibleProperty> CreateAccessors(Component target)
    {
        if (_extractors.ContainsKey(target.GetType()))
        {
            return _extractors[target.GetType()].ExtractProperties(target);
        }
        else
        {
            // generic mapping 
            var candidates = target.GetType().GetFields();
            Debug.Log("Extracting properties from " + target.GetType(), target);
            var l = new System.Collections.Generic.List<IAccessibleProperty>();
            foreach (var prop in candidates)
            {
                var baseType = CreateLibmapperTypeFromPrimitive(prop.FieldType);
                if (baseType == Mapper.Type.Null && !_converters.ContainsKey(prop.FieldType)) continue;
                var mapped = new AccessibleClassField(prop);
                
                if (baseType == Mapper.Type.Null) // this type needs to be wrapped in order to be turned into a signal
                {
                    
                    var converter = _converters[prop.FieldType];
                    Debug.Log("Extracting (wrapped) property: " + prop.Name + " of type: " + CreateLibmapperTypeFromPrimitive(converter.SimpleType) + " for libmapper.", target);
                    l.Add(new WrappedAccessibleProperty(mapped, converter));
                }
                else
                {
                    Debug.Log("Extracting property: " + prop.Name + " of type: " + baseType + " for libmapper.", target);
                    l.Add(mapped);
                }
            }

            return l;
        }
    }
}

internal class WrappedAccessibleProperty(IAccessibleProperty inner, ITypeConverter converter) : IAccessibleProperty
{
    public int GetVectorLength()
    {
        return converter.VectorLength;
    }
    
    public void SetObject(object target, object value)
    {
        inner.SetObject(target, converter.CreateComplexObject(value));
    }

    public object GetValue(object target)
    {
        return converter.CreateSimpleObject(inner.GetValue(target));
    }

    public string Name => inner.Name;
    public Type BackingType => converter.SimpleType;

    public string? Units => inner.Units;
    public (float min, float max)? Bounds => inner.Bounds;
}

public readonly struct PollJob(IntPtr devicePtr, int pollTime) : IJob
{
    [NativeDisableUnsafePtrRestriction] // it's probably still unsafe but I don't care
    private readonly IntPtr _devicePtr = devicePtr;

    public void Execute()
    {
        var device = new Device(_devicePtr);
        device.Poll(pollTime);
    }
}

