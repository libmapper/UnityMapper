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
    
    public override void Start()
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
}