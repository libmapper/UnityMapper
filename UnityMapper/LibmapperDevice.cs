using Unity.Collections.LowLevel.Unsafe;
using Unity.Jobs;

namespace UnityMapper;
using System.Reflection;
using Mapper;
using UnityEngine;
using UnityEngine.Serialization;
using Type = System.Type;

public class LibmapperDevice : MonoBehaviour
{
    
    private Device _device;

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
            Debug.LogWarning("Unsupported type: " + t + " for libmapper mapping. Ignoring.");
            return Mapper.Type.Null;
        }
    }


    private static System.Collections.Generic.List<IMappedProperty> CreateMapping(Component target)
    {
        if (target is Transform transformTarget)
        {
            var l = new System.Collections.Generic.List<IMappedProperty>();
            l.Add(new MappedPosition(transformTarget));
            l.Add(new MappedRotation(transformTarget));
            l.Add(new MappedScale(transformTarget));
            return l;
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

class MappedPosition(Transform transform) : IMappedProperty
{
    public void SetObject(object val)
    {
        var value = (Single[])val;
        transform.position = new Vector3(value[0], value[1], value[2]);
    }
    public object GetValue()
    {
        return new float[] {transform.position.x, transform.position.y, transform.position.z};
    }

    public Type GetMappedType()
    {
        return typeof(float[]);
    }

    public int GetVectorLength()
    {
        return 3;
    }

    public string GetName()
    {
        return "Position";
    }
}
class MappedScale(Transform transform) : IMappedProperty
{
    public void SetObject(object val)
    {
        var value = (Single[])val;
        transform.localScale = new Vector3(value[0], value[1], value[2]);
    }
    public object GetValue()
    {
        return new float[] {transform.localScale.x, transform.localScale.y, transform.localScale.z};
    }

    public Type GetMappedType()
    {
        return typeof(float[]);
    }

    public int GetVectorLength()
    {
        return 3;
    }

    public string GetName()
    {
        return "Scale";
    }
}
class MappedRotation(Transform transform) : IMappedProperty
{
    public void SetObject(object val)
    {
        var value = (Single[])val;
        transform.rotation = new Quaternion(value[0], value[1], value[2], value[3]);
    }
    public object GetValue()
    {
        return new float[] {transform.rotation.x, transform.rotation.y, transform.rotation.z, transform.rotation.w};
    }

    public Type GetMappedType()
    {
        return typeof(float[]);
    }

    public int GetVectorLength()
    {
        return 4;
    }

    public string GetName()
    {
        return "Rotation";
    }
}
