using UnityEngine;
using UnityMapper.API;

namespace UnityMapper.Builtin;

public class CameraExtractor : IPropertyExtractor<Camera> 
{
    public List<IAccessibleProperty> ExtractProperties(Camera component)
    {
        List<IAccessibleProperty> list = [new FieldOfViewProperty()];
        if (component.usePhysicalProperties)
        {
            // TODO: Add physical properties
            // Probably want some kind of source generation because there's a LOT of properties
            // & one class per property would get ridiculous 
        }
        return list;
    }
}

public class FieldOfViewProperty : IAccessibleProperty<Camera, float>
{
    public string Name => "Camera/Field of View";

    public string? Units => "°";
    
    public (float min, float max)? Bounds => (0.0f, 180.0f);

    public void Set(Camera target, float value)
    {
        target.fieldOfView = value;
    }

    public float Get(Camera target)
    {
        return target.fieldOfView;
    }
}