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

public class LibmapperDevice : MonoBehaviour
{
    
    private Device _device;
    
    private readonly Dictionary<Type, IPropertyExtractor> _extractors = new();
    private readonly Dictionary<Type, ITypeMapper> _mappers = new();

    private System.Collections.Generic.List<(Signal, IMappedProperty, Mapper.Time lastChanged)> _properties = [];

    [SerializeField] private int pollTime = 1;
    
    [FormerlySerializedAs("_componentsToMap")] [SerializeField]
    private System.Collections.Generic.List<Component> componentsToExpose = [];
    // Start is called before the first frame update

    private PollJob _job;
    void Start()
    {
        _device = new Device(gameObject.name);
        _job = new PollJob(_device._obj, pollTime);
                
        // Builtin extractors
        RegisterExtractor(new TransformExtractor());
        
        // Builtin type converters
        RegisterTypeMapper(new Vector3Mapper());
        RegisterTypeMapper(new Vector2Mapper());
        RegisterTypeMapper(new QuaternionMapper());
    }
    
    
    private bool _lastReady = false;
    private JobHandle? _handle;
    // Use physics update for consistent timing
    void FixedUpdate()
    {
        if (_handle != null)
        {
            _handle.Value.Complete();
            if (_device.GetIsReady() && !_lastReady)
            {
                Debug.Log("registering signals");
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
                            var mapper = _mappers[wrappedMap.GetMappedType()];
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
                        
                        Debug.Log("Registered signal of type: " + type + " with length: " + wrappedMap.GetVectorLength());
                        var signal = _device.AddSignal(Signal.Direction.Incoming, wrappedMap.GetName(), wrappedMap.GetVectorLength(), type);
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
                    lastChanged.Set(_device.GetTime());
                }
            
            }
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
        if (typeof(T) == typeof(Component))
        {
            throw new ArgumentException("Can't override generic extractor for Component type");
        }
        _extractors[typeof(T)] = extractor;
    }
    
    /// <summary>
    /// Register a type mapper for libmapper to automatically convert complex types into simple types.
    /// </summary>
    /// <param name="mapper">A type mapper</param>
    /// <typeparam name="T">The complex type</typeparam>
    /// <typeparam name="U">The primitive type</typeparam>
    public void RegisterTypeMapper<T, U>(ITypeMapper<T, U> mapper) where T : notnull where U : notnull
    {
        _mappers[typeof(T)] = mapper;
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
            Debug.Log("Extracting properties from " + target.GetType());
            var l = new System.Collections.Generic.List<IMappedProperty>();
            foreach (var prop in candidates)
            {
                var baseType = CreateLibmapperTypeFromPrimitive(prop.FieldType);
                if (baseType == Mapper.Type.Null && _mappers[prop.FieldType] == null) continue;
                Debug.Log("Mapping property: " + prop.Name + " of type: " + baseType + " for libmapper.");
                var mapped = new MappedClassField(prop, target);
                
                if (baseType == Mapper.Type.Null) // this type needs to be wrapped in order to be turned into a signal
                {
                    var mapper = _mappers[prop.FieldType];
                    l.Add(new WrappedMappedProperty(mapped, mapper));
                }
                else
                {
                    l.Add(mapped);
                }
            }

            return l;
        }
    }
}

internal class WrappedMappedProperty(IMappedProperty inner, ITypeMapper mapper) : IMappedProperty
{
    public int GetVectorLength()
    {
        return mapper.VectorLength;
    }

    public Type GetMappedType()
    {
        return mapper.SimpleType;
    }

    public void SetObject(object value)
    {
        inner.SetObject(mapper.CreateComplexObject(value));
    }

    public object GetValue()
    {
        return mapper.CreateSimpleObject(inner.GetValue());
    }

    public string GetName()
    {
        return inner.GetName();
    }
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

