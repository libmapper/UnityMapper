using UnityEngine;
using UnityMapper.API;

namespace UnityMapper.Builtin;

public class Vector3Converter : ITypeConverter<Vector3, float[]>
{
    private float[] _buffer = new float[3];
    public float[] CreateSimple(Vector3 complex)
    {
        _buffer[0] = complex.x;
        _buffer[1] = complex.y;
        _buffer[2] = complex.z;
        return _buffer;
    }

    public Vector3 CreateComplex(float[] simple)
    {
        return new Vector3(simple[0], simple[1], simple[2]);
    }

    public int VectorLength => 3;
}

public class Vector2Converter : ITypeConverter<Vector2, float[]>
{
    private float[] _buffer = new float[2];
    public float[] CreateSimple(Vector2 complex)
    {
        _buffer[0] = complex.x;
        _buffer[1] = complex.y;
        return _buffer;
    }

    public Vector2 CreateComplex(float[] simple)
    {
        return new Vector2(simple[0], simple[1]);
    }

    public int VectorLength => 2;
}

public class QuaternionConverter : ITypeConverter<Quaternion, float[]>
{
    private float[] _buffer = new float[4];
    public float[] CreateSimple(Quaternion complex)
    {
        _buffer[0] = complex.x;
        _buffer[1] = complex.y;
        _buffer[2] = complex.z;
        _buffer[3] = complex.w;
        return _buffer;
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
    private float[] _buffer = new float[4];
    public float[] CreateSimple(Color complex)
    {
        _buffer[0] = complex.r;
        _buffer[1] = complex.g;
        _buffer[2] = complex.b;
        _buffer[3] = complex.a;
        return _buffer;
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