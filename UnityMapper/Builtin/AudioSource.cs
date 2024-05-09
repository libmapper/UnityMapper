using UnityEngine;
using UnityMapper.API;

namespace UnityMapper.Builtin;

public class AudioSourceExtractor : IPropertyExtractor<AudioSource>
{
    public List<IAccessibleProperty> ExtractProperties(AudioSource component)
    {
        return [new VolumeProperty(component), new PitchProperty(component)];
    }
}

internal class VolumeProperty(AudioSource component) : IAccessibleProperty
{
    public Type GetMappedType()
    {
        return typeof(float);
    }

    public void SetObject(object value)
    {
        component.volume = (float) value;
    }

    public object GetValue()
    {
        return component.volume;
    }

    public string GetName()
    {
        return "AudioSource/Volume";
    }

    public string? Units => "%";
    public (float min, float max)? Bounds => (0.0f, 1.0f);
}

internal class PitchProperty(AudioSource component) : IAccessibleProperty
{
    public Type GetMappedType()
    {
        return typeof(float);
    }

    public void SetObject(object value)
    {
        component.pitch = (float) value;
    }

    public object GetValue()
    {
        return component.volume;
    }

    public string GetName()
    {
        return "AudioSource/Pitch";
    }

    public string? Units => null;
    public (float min, float max)? Bounds => (-3.0f, 3.0f);
}