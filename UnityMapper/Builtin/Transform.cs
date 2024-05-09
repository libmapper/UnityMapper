using UnityEngine;
using UnityMapper.API;

namespace UnityMapper.Builtin;

public class TransformExtractor : IPropertyExtractor<Transform>
{
    public List<IAccessibleProperty> ExtractProperties(Transform component)
    {
        return
        [
            new AccessiblePosition(),
            new AccessibleScale(),
            new AccessibleRotation()
        ];
    }
}

internal class AccessiblePosition : IAccessibleProperty<Transform, float[]>
{
    public void Set(Transform target, float[] value)
    {
        target.position = new Vector3(value[0], value[1], value[2]);
    }

    public float[] Get(Transform target)
    {
        return [target.position.x, target.position.y, target.position.z];
    }
    public int GetVectorLength()
    {
        return 3;
    }

    public string Name => "Transform/Position";
    public string? Units => "m";
    public (float min, float max)? Bounds => null;
}

internal class AccessibleScale : IAccessibleProperty<Transform, float[]>
{
    public void Set(Transform target, float[] value)
    {
        target.localScale = new Vector3(value[0], value[1], value[2]);
    }

    public float[] Get(Transform target)
    {
        return [target.localScale.x, target.localScale.y, target.localScale.z];
    }
    public int GetVectorLength()
    {
        return 3;
    }

    public string Name => "Transform/Scale";
    public string? Units => null;
    public (float min, float max)? Bounds => (0.0f, float.MaxValue);

}

internal class AccessibleRotation : IAccessibleProperty<Transform, float[]>
{

    public void Set(Transform target, float[] value)
    {
        target.rotation = Quaternion.Euler(value[0], value[1], value[2]);
    }

    public float[] Get(Transform target)
    {
        var euler = target.rotation.eulerAngles;
        return [euler.x, euler.y, euler.z];
    }
    public int GetVectorLength()
    {
        return 3;
    }

    public string Name => "Transform/Rotation";
    public string? Units => "°";
    public (float min, float max)? Bounds => (-360.0f, 360.0f);
}

