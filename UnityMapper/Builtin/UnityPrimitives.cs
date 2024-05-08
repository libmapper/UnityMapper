using UnityEngine;
using UnityMapper.API;

namespace UnityMapper.Builtin;

public class Vector3Converter : ITypeConverter<Vector3, float[]>
{
    public float[] CreateSimple(Vector3 complex)
    {
        return new float[] {complex.x, complex.y, complex.z};
    }

    public Vector3 CreateComplex(float[] simple)
    {
        return new Vector3(simple[0], simple[1], simple[2]);
    }

    public int VectorLength => 3;
}

public class Vector2Converter : ITypeConverter<Vector2, float[]>
{
    public float[] CreateSimple(Vector2 complex)
    {
        return new float[] {complex.x, complex.y};
    }

    public Vector2 CreateComplex(float[] simple)
    {
        return new Vector2(simple[0], simple[1]);
    }

    public int VectorLength => 2;
}

public class QuaternionConverter : ITypeConverter<Quaternion, float[]>
{
    public float[] CreateSimple(Quaternion complex)
    {
        return new float[] {complex.x, complex.y, complex.z, complex.w};
    }

    public Quaternion CreateComplex(float[] simple)
    {
        return new Quaternion(simple[0], simple[1], simple[2], simple[3]);
    }

    public int VectorLength => 4;
}

public class ColorConverter : ITypeConverter<Color, float[]>
{
    public int VectorLength => 4;
    public float[] CreateSimple(Color complex)
    {
        return new float[] {complex.r, complex.g, complex.b, complex.a};
    }

    public Color CreateComplex(float[] simple)
    {
        return new Color(simple[0], simple[1], simple[2], simple[3]);
    }
}

public class BoolConverter : ITypeConverter<bool, int>
{
    public int VectorLength => 1;
    
    public int CreateSimple(bool value)
    {
        return value ? 1 : 0;
    }

    public bool CreateComplex(int simple)
    {
        return simple >= 1;
    }
}