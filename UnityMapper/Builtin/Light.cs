using UnityEngine;
using UnityMapper.API;

namespace UnityMapper.Builtin;

public class LightExtractor : IPropertyExtractor<Light>
{
    public List<IBoundProperty> ExtractProperties(Light component)
    {
        return [new IntensityProperty(component), new ColorProperty(component)];
    }
}

internal class IntensityProperty(Light component) : IBoundProperty
{
    public Type GetMappedType()
    {
        return typeof(float);
    }

    public void SetObject(object value)
    {
        component.intensity = (float) value;
    }

    public object GetValue()
    {
        return component.intensity;
    }

    public string GetName()
    {
        return "Light/Intensity";
    }

    public string? Units => "cd";
    public (float min, float max)? Bounds => (0.0f, 8.0f);
}

internal class ColorProperty(Light component) : IBoundProperty
{
    public Type GetMappedType()
    {
        return typeof(Color);
    }

    public void SetObject(object value)
    {
        component.color = (Color) value;
    }

    public object GetValue()
    {
        return component.color;
    }

    public string GetName()
    {
        return "Light/Color";
    }

    public string? Units => null;
    public (float min, float max)? Bounds => (0.0f, 1.0f);
}