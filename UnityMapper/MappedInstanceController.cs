using Mapper;
using UnityEngine;

namespace UnityMapper;

/// <summary>
/// Controls <see cref="InstancedLibmapperDevice"/> components attached to child GameObjects.
///
/// This only uses one Libmapper device, and can be thought of as a flattened combination of all child objects.
/// If multiple child objects expose identical properties, they'll be expressed as separate instances of the same signal.
/// </summary>
public class MappedInstanceController : MonoBehaviour
{
    
    private Device _device;
    private MergedSignalContainer _mergedSignals;

    void Start()
    {
        _device = new Device("MapperInstanceController");
        _mergedSignals = new MergedSignalContainer(_device);
    }

    private ulong _lastInstanceId = 10;
    
    private readonly Dictionary<ulong, (InstancedLibmapperDevice device, GameObject owner)> _trackedInstances = [];

    private Dictionary<IAccessibleProperty, HashSet<ulong>> _properties = [];

    void FixedUpdate()
    {
        var children = gameObject.GetComponentsInChildren<InstancedLibmapperDevice>()
            .Select(j => j.gameObject)
            .ToList();
        // Check for new instances
        foreach (var instance in children) // loop over all children with a LibmapperDevice
        {
            if (_trackedInstances.Values.Any(inst => inst.owner == instance)) continue; // ignore already tracked instance
            
            // new instance to be tracked
            var device = instance.GetComponent<InstancedLibmapperDevice>();
            var id = _lastInstanceId++;
            device.InstanceId = id;
            _trackedInstances[id] = (device, instance);
            ChildAdded(id);
        }
        
        // Check for removed instances
        foreach (var instance in _trackedInstances)
        {
            if (children.Contains(instance.Value.owner)) continue; // ignore still tracked instance
            
            ChildRemoved(instance.Key);
            // instance was removed
            _trackedInstances.Remove(instance.Key);
        }
    }

    private void ChildAdded(ulong id)
    {
        var device = _trackedInstances[id].device;
        var props = new Dictionary<string, (IAccessibleProperty prop, Component owner)>();
        device.DiscoverProperties(props);
        foreach (var (name, (prop, owner)) in props)
        {
            if (!_properties.ContainsKey(prop))
            {
                _properties[prop] = [];
            }
            _properties[prop].Add(id);
            var signal = _device.AddSignal(Signal.Direction.Incoming, name, prop.GetVectorLength(),
                BaseLibmapperDevice.CreateLibmapperTypeFromPrimitive(prop.BackingType), prop.Units);
        }
    }

    private void ChildRemoved(ulong id)
    {
        
    }
}


/// <summary>
/// This class tracks all identical signals from child instances and merges them into a single signal.
/// </summary>
class MergedSignalContainer(Device _device)
{
    /// <summary>
    /// All signals held by this container and which instances are subscribed to them
    /// </summary>
    private readonly Dictionary<IAccessibleProperty, (Signal signal, HashSet<(ulong ownerId, Component trackedComponent)> subscribers)> _signals = [];
    

    /// <summary>
    /// Called to add a new instance's properties to the container.
    /// </summary>
    /// <param name="id"></param>
    /// <param name="props"></param>
    public void AddProperties(ulong id, IEnumerable<(IAccessibleProperty prop, Component owner)> props)
    {
        foreach (var (prop, owner) in props)
        {
            if (!_signals.Keys.Any(c => c.IsSameProperty(prop))) // newly discovered property
            {
                var signal = _device.AddSignal(Signal.Direction.Incoming, prop.Name, prop.GetVectorLength(),
                    BaseLibmapperDevice.CreateLibmapperTypeFromPrimitive(prop.BackingType));
                _signals[prop] = (signal, new HashSet<(ulong, Component)>());
                Debug.Log("Discovered new signal: " + prop.Name);
            }

            _signals[prop].subscribers.Add((id, owner));
            _signals[prop].signal.ReserveInstance(id);
        }
    }
}