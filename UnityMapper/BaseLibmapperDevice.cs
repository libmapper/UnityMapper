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

public class BaseLibmapperDevice : MonoBehaviour
{
    
    protected Device Device;
    
    private readonly Dictionary<Type, IPropertyExtractor> _extractors = new();
    private readonly Dictionary<Type, ITypeConverter> _converters = new();
    protected bool Frozen = false; // when frozen, no new extractors, mappers, or signals can be added

    private System.Collections.Generic.List<(Signal, IMappedProperty, Mapper.Time lastChanged)> _properties = [];

    [SerializeField] private int pollTime = 1;
    
    [FormerlySerializedAs("_componentsToMap")] [SerializeField]
    private System.Collections.Generic.List<Component> componentsToExpose = [];

    private PollJob _job;
    
    public virtual void Start()
    {

    }
    
    /// <summary>
    /// Called before the device is frozen, allowing for custom extractors, mappers, and signals to be registered.
    /// Overriding this method is an alternative to obtaining a reference to the device and calling
    /// RegisterExtractor, RegisterTypeConverter, and AddComponent manually.
    /// </summary>
    public virtual void RegisterExtensions()
    {
        
    }
    
    
    private bool _lastReady = false;
    private JobHandle? _handle;
    // Use physics update for consistent timing
    void FixedUpdate()
    {
        if (!Frozen) return; // wait until Freeze() is called to start polling
        
        if (_handle != null)
        {
            _handle.Value.Complete();
            if (Device.GetIsReady() && !_lastReady)
            {
                Debug.Log("Registering signals");
                // device just became ready
                _lastReady = true;
                foreach (var component in componentsToExpose)
                {
                    var maps = CreateMapping(component);
                
                    // TODO: this is REALLY ugly, fix later
                    foreach (var mapped in maps)
                    {
                        var wrappedMap = mapped; // wrapped version to primitive-ize type
                        var kind = mapped.GetMappedType();
                        var type = CreateLibmapperTypeFromPrimitive(kind);

                        if (type == Mapper.Type.Null)
                        {
                            var mapper = _converters[wrappedMap.GetMappedType()];
                            if (mapper == null)
                            {
                                throw new ArgumentException("No mapper found for type: " + wrappedMap.GetMappedType());
                            }

                            type = CreateLibmapperTypeFromPrimitive(mapper.SimpleType);
                            if (type == Mapper.Type.Null)
                            {
                                throw new ArgumentException("Mapper type is not a simple type: " + mapper.GetType());
                            }
                            
                            wrappedMap = new WrappedMappedProperty(mapped, mapper);
                        }
                        
                        Debug.Log("Registered libmapper signal of type: " + type + " with length: " + wrappedMap.GetVectorLength());
                        var signal = Device.AddSignal(Signal.Direction.Incoming, wrappedMap.GetName(), wrappedMap.GetVectorLength(), type);
                        _properties.Add((signal, wrappedMap, new Mapper.Time()));
                        signal.SetValue(wrappedMap.GetValue());
                    }
                
                }
            }
            foreach (var (signal, mapped, lastChanged) in _properties)
            {
                var value = signal.GetValue();
                // check if the value has changed
                if (value.Item2 > lastChanged)
                {
                    // the value was changed on the network, so we should update the local value
                    mapped.SetObject(value.Item1);
                    lastChanged.Set(value.Item2);
                }
                else
                {
                    // no remote updates have happened, so push our local value
                    signal.SetValue(mapped.GetValue());
                    lastChanged.Set(Device.GetTime());
                }
            
            }
        }
        else
        {
            _job = new PollJob(Device._obj, pollTime);
        }

        _handle = _job.Schedule();

    }
    
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

    private static Mapper.Type CreateLibmapperTypeFromPrimitive(Type t)
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


    private System.Collections.Generic.List<IMappedProperty> CreateMapping(Component target)
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
            var l = new System.Collections.Generic.List<IMappedProperty>();
            foreach (var prop in candidates)
            {
                var baseType = CreateLibmapperTypeFromPrimitive(prop.FieldType);
                if (baseType == Mapper.Type.Null && !_converters.ContainsKey(prop.FieldType)) continue;
                var mapped = new MappedClassField(prop, target);
                
                if (baseType == Mapper.Type.Null) // this type needs to be wrapped in order to be turned into a signal
                {
                    
                    var converter = _converters[prop.FieldType];
                    Debug.Log("Extracting (wrapped) property: " + prop.Name + " of type: " + CreateLibmapperTypeFromPrimitive(converter.SimpleType) + " for libmapper.", target);
                    l.Add(new WrappedMappedProperty(mapped, converter));
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

internal class WrappedMappedProperty(IMappedProperty inner, ITypeConverter converter) : IMappedProperty
{
    public int GetVectorLength()
    {
        return converter.VectorLength;
    }

    public Type GetMappedType()
    {
        return converter.SimpleType;
    }

    public void SetObject(object value)
    {
        inner.SetObject(converter.CreateComplexObject(value));
    }

    public object GetValue()
    {
        return converter.CreateSimpleObject(inner.GetValue());
    }

    public string GetName()
    {
        return inner.GetName();
    }

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

