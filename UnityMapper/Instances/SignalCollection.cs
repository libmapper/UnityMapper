using Mapper;
using UnityEngine;
using UnityMapper.API;
using Time = Mapper.Time;

namespace UnityMapper.Instances;

/// <summary>
/// This class is an abstraction over a Signal that allows for manipulation of multiple instances of the same signal.
///
/// This class also dictates how individual signals can be grouped into instances
/// </summary>
public class SignalCollection
{
    private Device _device;
    private Signal _signal;
    private readonly Dictionary<ulong, SignalSpec> _signals = [];
    private readonly Dictionary<ulong, Time> _lastUpdates = [];
    private readonly Dictionary<ulong, Signal.Instance> _instances = [];
    private ulong nextId = 10;
    
    /// <summary>
    /// 
    /// </summary>
    /// <param name="device"></param>
    /// <param name="spec"></param>
    /// <param name="owner"></param>
    public SignalCollection(Device device, SignalSpec spec)
    {
        _device = device;
        Name = GetFullPathname(spec.Owner) + "/" + spec.LocalName;
        _signal = device.AddSignal(Signal.Direction.Incoming, this.Name, spec.Property.GetVectorLength(), 
            LibmapperDevice.CreateLibmapperTypeFromPrimitive(spec.Property.GetMappedType()), spec.Property.Units, 0);

        var id = nextId++;
        spec.AssignInstanceID(id);
        _signals.Add(id, spec);
        _signal.ReserveInstance(id);
        _instances.Add(id, new Signal.Instance(_signal._obj, id));
        _lastUpdates.Add(id, _device.GetTime());
    }
    
    /// <summary>
    /// A full path name to the signal, from the highest root GameObject.
    /// </summary>
    public string Name { get; private set; }

    /// <summary>
    /// If this collection can accept the specified discovered signal
    /// </summary>
    /// <param name="other"></param>
    public bool CanAccept(SignalSpec other) => _signals[10].CanGroupWith(other);
    
    public void Update()
    {
        foreach (var id in _instances.Keys)
        {
            var newVal = _signal.GetValue(id);
            if (newVal.Item2 > _lastUpdates[id] || true)
            {
                _lastUpdates[id] = newVal.Item2;
                if (newVal.Item1 == null) continue;
                _signals[id].Property.SetObject(newVal.Item1);
            }
            else
            {
                _instances[id].SetValue(_signals[id].Property.GetValue());
                _lastUpdates[id].Set(_device.GetTime());
            }
        }
    }

    public void Add(SignalSpec toAdd)
    {
        if (!CanAccept(toAdd))
        {
            throw new InvalidOperationException("Cannot accept signal");
        }

        var id = nextId++;
        toAdd.AssignInstanceID(id);
        _signals.Add(id, toAdd);
        _signal.ReserveInstance(id);
        _instances.Add(id, new Signal.Instance(_signal._obj, id));
        _lastUpdates.Add(id, _device.GetTime());
    }

    public void RemoveAllFromList(LibmapperComponentList target)
    {
        // If any signals are owned by this component, remove them
        foreach (var id in _signals.Keys)
        {
            if (_signals[id].OwningList == target)
            {
                _signal.RemoveInstance(id);
                _signals.Remove(id);
                _instances.Remove(id);
                _lastUpdates.Remove(id);
            }
        }
    }


    private static string GetFullPathname(GameObject obj)
    {
        var path = obj.name;
        while (obj.transform.parent != null)
        {
            obj = obj.transform.parent.gameObject;
            path = obj.name + "/" + path;
        }

        return path;
    }
}

/// <summary>
/// Contains uniquely identifying information about a signal. Used by a <see cref="SignalCollection"/> to group signals.
/// </summary>
public record SignalSpec(string LocalName, GameObject Owner, IBoundProperty Property, LibmapperComponentList OwningList)
{
    /// <summary>
    /// The name of this signal, relative to it's owning object. For example, a camera's FOV slider would be "Camera/fov"
    /// </summary>
    public string LocalName { get; private set; } = LocalName;

    /// <summary>
    /// The GameObject that this property belongs to.
    /// </summary>
    public GameObject Owner { get; private set; } = Owner;

    /// <summary>
    /// The LibmapperComponentList used to discover this signal
    /// </summary>
    public LibmapperComponentList OwningList { get; private set; } = OwningList;

    /// <summary>
    /// Accessor for the property on that specific object
    /// </summary>
    public IBoundProperty Property { get; private set; } = Property;

    /// <summary>
    /// Internal instance ID of this signal. Can only be set once.
    /// </summary>
    public ulong InstanceId { get; private set; } = 0;

    private bool _hasBeenAsigned = false;
    public void AssignInstanceID(ulong id)
    {
        if (_hasBeenAsigned)
        {
            throw new InvalidOperationException("Instance ID already assigned");
        }

        InstanceId = id;
        _hasBeenAsigned = true;
    }
    
    
    /// <summary>
    /// Whether this signal should be expressed as an instance of another signal
    /// </summary>
    /// <param name="other">Another signal to test for similarity</param>
    public bool CanGroupWith(SignalSpec other)
    {
        return Owner.transform.parent.gameObject == other.Owner.transform.parent.gameObject // both owned by the same parent
               && LocalName == other.LocalName // both have the same local name
               && SimilarName(Owner.name, other.Owner.name); // both gameobjects have the same or similar names
    }

    /// <summary>
    /// Used to determine if the names of two GameObjects are similar enough to be grouped together.
    /// Tests for name equality, and chops off the last segment of the name if it contains a period.
    /// </summary>
    /// <returns></returns>
    private static bool SimilarName(string a, string b)
    {
        if (!a.Contains("."))
        {
            return a == b;
        }

        // very slow way of doing this, should improve
        var aSplit = string.Join('.', a.Split(".")[..^1]);
        var bSplit = string.Join('.', b.Split(".")[..^1]);
        return aSplit == bSplit;
    }
}