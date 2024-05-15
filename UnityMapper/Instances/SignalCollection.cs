using Mapper;
using UnityEngine;
using UnityMapper.API;

namespace UnityMapper.Instances;

/// <summary>
/// This class is an abstraction over a Signal that allows for manipulation of multiple instances of the same signal.
///
/// This class also dictates how individual signals can be grouped into instances
/// </summary>
public class SignalCollection
{
    private Device _device;
    
    public SignalCollection(Device device)
    {
        _device = device;
    }
    
    /// <summary>
    /// A full path name to the signal, from the highest root GameObject.
    /// </summary>
    public string Name { get; private set; }
    
}

/// <summary>
/// Contains uniquely identifying information about a signal. Used by a <see cref="SignalCollection"/> to group signals.
/// </summary>
public record SignalSpec(string LocalName, GameObject Owner, IBoundProperty Property)
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
    /// Accessor for the property on that specific object
    /// </summary>
    public IBoundProperty Property { get; private set; } = Property;
}