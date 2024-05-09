using UnityEngine;
using UnityMapper.API;

namespace UnityMapper.Builtin;

public class AudioSourceExtractor : IPropertyExtractor<AudioSource>
{
    public List<IAccessibleProperty> ExtractProperties(AudioSource component)
    {
        return [new VolumeProperty(), new PitchProperty()];
    }
}

internal class VolumeProperty : IAccessibleProperty<AudioSource, float>
{
    

    public string Name => "AudioSource/Volume";

    public string? Units => "%";
    public (float min, float max)? Bounds => (0.0f, 1.0f);
    
    public void Set(AudioSource target, float value)
    {
        target.volume = value;
    }

    public float Get(AudioSource target)
    {
        return target.volume;
    }
}

internal class PitchProperty : IAccessibleProperty<AudioSource, float>
{

    public string GetName()
    {
        return "AudioSource/Pitch";
    }

    public string Name { get; }
    public string? Units => null;
    public (float min, float max)? Bounds => (-3.0f, 3.0f);
    
    public void Set(AudioSource target, float value)
    {
        target.pitch = value;
    }

    public float Get(AudioSource target)
    {
        return target.pitch;
    }
}