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
    
    private Dictionary<Type, IPropertyExtractor> _extractors = new();

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
                        var kind = mapped.GetMappedType();
                        var type = CreateLibmapperTypeFromPrimitive(kind);
                        Debug.Log("Registered signal of type: " + type + " with length: " + mapped.GetVectorLength());
                        var signal = _device.AddSignal(Signal.Direction.Incoming, mapped.GetName(), mapped.GetVectorLength(), type);
                        _properties.Add((signal, mapped, new Mapper.Time()));
                        signal.SetValue(mapped.GetValue());
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
                if (baseType == Mapper.Type.Null) continue;
                Debug.Log("Mapping property: " + prop.Name + " of type: " + baseType + " for libmapper.");
                var mapped = new MappedClassField(prop, target);
                l.Add(mapped);
            }

            return l;
        }
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

