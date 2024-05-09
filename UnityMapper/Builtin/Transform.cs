using UnityEngine;
using UnityMapper.API;

namespace UnityMapper.Builtin;

public class TransformExtractor : IPropertyExtractor<Transform>
{
    public List<IAccessibleProperty> ExtractProperties(Transform component)
    {
        return
        [
            new AccessiblePosition(component),
            new AccessibleScale(component),
            new AccessibleRotation(component)
        ];
    }
}

internal class AccessiblePosition(Transform transform) : IAccessibleProperty
{
    public void SetObject(object val)
    {
        var value = (Single[])val;
        transform.position = new Vector3(value[0], value[1], value[2]);
    }
    public object GetValue()
    {
        return new float[] {transform.position.x, transform.position.y, transform.position.z};
    }

    public Type GetMappedType()
    {
        return typeof(float[]);
    }

    public int GetVectorLength()
    {
        return 3;
    }

    public string GetName()
    {
        return "Transform/Position";
    }
    
    public string? Units => "m";
    public (float min, float max)? Bounds => null;
}
internal class AccessibleScale(Transform transform) : IAccessibleProperty
{
    public void SetObject(object val)
    {
        var value = (Single[])val;
        transform.localScale = new Vector3(value[0], value[1], value[2]);
    }
    public object GetValue()
    {
        return new float[] {transform.localScale.x, transform.localScale.y, transform.localScale.z};
    }

    public Type GetMappedType()
    {
        return typeof(float[]);
    }

    public int GetVectorLength()
    {
        return 3;
    }

    public string GetName()
    {
        return "Transform/Scale";
    }
    public string? Units => null;
    public (float min, float max)? Bounds => null;
}

internal class AccessibleRotation(Transform transform) : IAccessibleProperty
{
    public void SetObject(object val)
    {
        var value = (Single[])val;
        transform.rotation = new Quaternion(value[0], value[1], value[2], value[3]);
    }
    public object GetValue()
    {
        return new float[] {transform.rotation.x, transform.rotation.y, transform.rotation.z, transform.rotation.w};
    }

    public Type GetMappedType()
    {
        return typeof(float[]);
    }

    public int GetVectorLength()
    {
        return 4;
    }

    public string GetName()
    {
        return "Transform/Rotation";
    }

    public string? Units => null;
    public (float min, float max)? Bounds => (-1.0f, 1.0f);
}

