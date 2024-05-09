using UnityEngine;
using UnityMapper.API;

namespace UnityMapper.Builtin;

public class LightExtractor : IPropertyExtractor<Light>
{
    public List<IAccessibleProperty> ExtractProperties(Light component)
    {
        return [new IntensityProperty(), new ColorProperty()];
    }
}

internal class IntensityProperty : IAccessibleProperty<Light, float>
{
    

    public string Name => "Light/Intensity";

    public string? Units => "cd";
    public (float min, float max)? Bounds => (0.0f, 8.0f);
    
    public void Set(Light target, float value)
    {
        target.intensity = value;
    }

    public float Get(Light target)
    {
        return target.intensity;
    }
}

internal class ColorProperty : IAccessibleProperty<Light, Color>
{

    public string Name => "Light/Color";

    public string? Units => null;
    public (float min, float max)? Bounds => (0.0f, 1.0f);
    
    public void Set(Light target, Color value)
    {
        target.color = value;
    }

    public Color Get(Light target)
    {
        return target.color;
    }
}