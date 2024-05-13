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

    void Start()
    {
        _device = new Device("MapperInstanceController");
    }

    private ulong _lastInstanceId = 0;
    
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

    void ChildAdded(ulong id)
    {
        
    }

    void ChildRemoved(ulong id)
    {
        
    }
}