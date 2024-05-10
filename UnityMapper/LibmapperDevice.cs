using Mapper;
using UnityEngine;
using UnityMapper.Builtin;

namespace UnityMapper;

/// <summary>
/// A version of <see cref="BaseLibmapperDevice"/> designed to be used from within the Unity editor.
/// </summary>
public class LibmapperDevice : BaseLibmapperDevice
{

    /// <summary>
    /// Whether to wait for Freeze() to be called before processing signals.
    /// </summary>
    [SerializeField] private bool useApi = false;
    
    public void Start()
    {
        var tmp = Frozen;
        Frozen = false; // in case another script called Freeze() before unity calls Start()
        
        Device = new Device(gameObject.name);
        
        // Builtin extractors
        RegisterExtractor(new TransformExtractor());
        
        // Builtin type converters
        RegisterTypeConverter(new Vector3Converter());
        RegisterTypeConverter(new Vector2Converter());
        RegisterTypeConverter(new QuaternionConverter());

        RegisterExtensions();
        
        Frozen = tmp; // restore previous frozen state;
        
        if (!useApi)
        {
            Freeze();
        }
    }

    private HashSet<(IAccessibleProperty prop, Signal bound, Component owner)> _properties = new();
    private bool _lastReady = false;
    
    public void FixedUpdate()
    {
        PollBegin();
        
        if (Device.GetIsReady() && !_lastReady)
        {
            _lastReady = true; // only run once
            var props = new Dictionary<string, (IAccessibleProperty prop, Component owner)>();
            DiscoverProperties(props);
            foreach (var (name, (prop, owner)) in props)
            {
                var signal = Device.AddSignal(Signal.Direction.Both, name, prop.GetVectorLength(), CreateLibmapperTypeFromPrimitive(prop.BackingType), prop.Units );
                if (prop.Bounds != null)
                {
                    var val = prop.Bounds.Value;
                    signal.SetProperty(Property.Min, val.min);
                    signal.SetProperty(Property.Max, val.max);
                }
                _properties.Add((prop, signal, owner));
            }
        }

        if (Device.GetIsReady())
        {
            UpdateSignals(_properties);
        }
        
        PollEnd();
    }
}