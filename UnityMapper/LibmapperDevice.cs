using System;
using System.Collections;
using System.Collections.Generic;
using System.Reflection;
using Mapper;
using UnityEngine;
using UnityEngine.Serialization;
using Time = UnityEngine.Time;
using Type = System.Type;

public class LibmapperDevice : MonoBehaviour
{
    
    private Device _device;

    private System.Collections.Generic.List<(Signal, MappedProperty)> _properties = [];

    
    [FormerlySerializedAs("_componentsToMap")] [SerializeField]
    private System.Collections.Generic.List<Component> componentsToExpose = [];
    // Start is called before the first frame update
    void Start()
    {
        _device = new Device("UNITY_" + gameObject.name);
    }


    private bool _lastReady = false;
    // Use physics update for consistent timing
    void FixedUpdate()
    {
        _device.Poll(1);
        
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
                    _properties.Add((signal, mapped));
                    signal.SetValue(mapped.GetValue());
                }
                
            }
            
        }
        foreach (var (signal, mapped) in _properties)
        {
            var value = signal.GetValue();
            if (value.Item1 != null)
            {
                mapped.SetObject(value.Item1);
            }
        }
       
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
    
    
    protected static System.Collections.Generic.List<MappedProperty> CreateMapping(Component target)
    {
        if (target is Transform transformTarget)
        {
            var l = new System.Collections.Generic.List<MappedProperty>();
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
            var l = new System.Collections.Generic.List<MappedProperty>();
            foreach (var prop in candidates)
            {
                var baseType = CreateLibmapperTypeFromPrimitive(prop.FieldType);
                if (baseType == Mapper.Type.Null) continue;
                Debug.Log("Mapping property: " + prop.Name + " of type: " + baseType + " for libmapper.");
                var mapped = new MappedClassProperty(prop, target);
                l.Add(mapped);
            }

            return l;
        }
    }
}


class MappedClassProperty : MappedProperty
{
    private FieldInfo _info;
    private object _target;

    public MappedClassProperty(FieldInfo info, object target)
    {
        _info = info;
        _target = target;
    }

    public Type GetMappedType()
    {
        return _info.FieldType;
    }

    public void SetObject(object value)
    {
        _info.SetValue(_target, value);
    }

    public object GetValue()
    {
        return _info.GetValue(_target);
    }

    public string GetName()
    {
        return _info.DeclaringType.Name + "." + _info.Name;
    }
}


class MappedPosition : MappedProperty
{
    private readonly Transform _transform;
    
    
    public MappedPosition(Transform transform)
    {
        _transform = transform;
    }

    public void SetObject(object val)
    {
        var value = (Single[])val;
        _transform.position = new Vector3(value[0], value[1], value[2]);
    }
    public object GetValue()
    {
        return new float[] {_transform.position.x, _transform.position.y, _transform.position.z};
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
class MappedScale : MappedProperty
{
    private readonly Transform _transform;
    
    
    public MappedScale(Transform transform)
    {
        _transform = transform;
    }

    public void SetObject(object val)
    {
        var value = (Single[])val;
        _transform.localScale = new Vector3(value[0], value[1], value[2]);
    }
    public object GetValue()
    {
        return new float[] {_transform.localScale.x, _transform.localScale.y, _transform.localScale.z};
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
class MappedRotation : MappedProperty
{
    private readonly Transform _transform;
    
    
    public MappedRotation(Transform transform)
    {
        _transform = transform;
    }

    public void SetObject(object val)
    {
        var value = (Single[])val;
        _transform.rotation = new Quaternion(value[0], value[1], value[2], value[3]);
    }
    public object GetValue()
    {
        return new float[] {_transform.rotation.x, _transform.rotation.y, _transform.rotation.z, _transform.rotation.w};
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

public interface MappedProperty
{
    /// <summary>
    /// The vector size of this mapped property.
    /// If > 1, T should be an array of a supported primitive;
    /// </summary>
    /// <returns>An integer >= 1</returns>
    int GetVectorLength()
    {
        return 1;
    }

    Type GetMappedType();
    
    void SetObject(object value);
    
    object GetValue();

    string GetName();
}

