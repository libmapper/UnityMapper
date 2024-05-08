using UnityEngine;
using UnityMapper.API;

namespace UnityMapper.Builtin;

public class CameraExtractor : IPropertyExtractor<Camera> 
{
    public List<IMappedProperty> ExtractProperties(Camera component)
    {
        List<IMappedProperty> list = [new FieldOfViewProperty(component)];
        if (component.usePhysicalProperties)
        {
            // TODO: Add physical properties
            // Probably want some kind of source generation because there's a LOT of properties
            // & one class per property would get ridiculous 
        }
        return list;
    }
}

public class FieldOfViewProperty(Camera component) : IMappedProperty
{
    public Type GetMappedType()
    {
        return typeof(float);
    }

    public void SetObject(object value)
    {
        component.fieldOfView = (float) value;
    }

    public object GetValue()
    {
        return component.fieldOfView;
    }

    public string GetName()
    {
        return "Camera/Field of View";
    }

    public string? Units => "°";
    public (float min, float max)? Bounds => (0.0f, 180.0f);
}