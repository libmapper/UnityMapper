using Mapper;
using Unity.Collections.LowLevel.Unsafe;
using Unity.Jobs;
using UnityEngine;
using UnityMapper.API;
using UnityMapper.Builtin;
using UnityMapper.Instances;
using Type = System.Type;

namespace UnityMapper;

using Type = Type;

public class LibmapperDevice : MonoBehaviour
{
    [SerializeField] private int pollTime = 1;
    [SerializeField] private bool nonBlockingPolling;

    /// <summary>
    ///     Whether or not to wait for Freeze() to be called before processing signals.
    /// </summary>
    [SerializeField] private bool useApi;

    private readonly Dictionary<Type, ITypeConverter> _converters = new();

    private readonly Dictionary<Type, IPropertyExtractor> _extractors = new();

    private DefaultPropertyExtractor? _defaultExtractor;

    private Device _device;
    private bool _frozen; // when frozen, no new extractors, mappers, or signals can be added
    private JobHandle? _handle;

    private PollJob _job;


    private bool _lastReady = false;

    private readonly System.Collections.Generic.List<SignalCollection> _properties = [];

    public void Start()
    {
        var tmp = _frozen;
        _frozen = false; // in case another script called Freeze() before unity calls Start()

        _device = new Device(gameObject.name);
        _job = new PollJob(_device._obj, pollTime);

        // Builtin extractors
        RegisterExtractor(new TransformExtractor());
        RegisterExtractor(new AudioSourceExtractor());
        RegisterExtractor(new CameraExtractor());
        RegisterExtractor(new LightExtractor());

        // Builtin type converters
        RegisterTypeConverter(new Vector3Converter());
        RegisterTypeConverter(new Vector2Converter());
        RegisterTypeConverter(new QuaternionConverter());
        RegisterTypeConverter(new ColorConverter());
        RegisterTypeConverter(new BoolConverter());

        RegisterExtensions();

        _frozen = tmp; // restore previous frozen state;

        if (!useApi) Freeze();
    }

    // Use physics update for consistent timing
    private void FixedUpdate()
    {
        if (!_frozen) return; // wait until Freeze() is called to start polling

        if (_handle != null || nonBlockingPolling)
        {
            if (nonBlockingPolling)
                _device.Poll();
            else
                _handle.Value.Complete();

            if (_device.GetIsReady())
                // find components in children
                foreach (var list in GetComponentsInChildren<LibmapperComponentList>())
                {
                    if (!list.visited)
                        foreach (var component in list.componentsToExpose)
                        {
                            var maps = ExtractProperties(component);

                            // TODO: this is REALLY ugly, fix later
                            foreach (var mapped in maps)
                            {
                                var wrappedMap = mapped; // wrapped version to primitive-ize type
                                var kind = mapped.GetMappedType();
                                var type = CreateLibmapperTypeFromPrimitive(kind);

                                if (type == Mapper.Type.Null)
                                {
                                    var mapper = _converters[wrappedMap.GetMappedType()];
                                    if (mapper == null)
                                        throw new ArgumentException("No mapper found for type: " +
                                                                    wrappedMap.GetMappedType());

                                    type = CreateLibmapperTypeFromPrimitive(mapper.SimpleType);
                                    if (type == Mapper.Type.Null)
                                        throw new ArgumentException("Mapper type is not a simple type: " +
                                                                    mapper.GetType());

                                    wrappedMap = new WrappedBoundProperty(mapped, mapper);
                                }

                                RegisterProperty(wrappedMap, component, list);
                            }
                        }

                    list.visited = true;
                }

            foreach (var collection in _properties) collection.Update();
        }

        if (!nonBlockingPolling) _handle = _job.Schedule();
    }

    public virtual void RegisterExtensions()
    {
    }

    /// <summary>
    ///     Either adds the property to a collection as an instance of an existing signal or creates a new signal
    /// </summary>
    private void RegisterProperty(IBoundProperty property, Component comp, LibmapperComponentList list)
    {
        var spec = new SignalSpec(property.GetName(), comp.gameObject, property, list);
        foreach (var existing in _properties)
            if (existing.CanAccept(spec))
            {
                Debug.Log($"Added {spec.Property.GetName()} to existing collection");
                existing.Add(spec);
                return;
            }

        var collection = new SignalCollection(_device, spec);
        _properties.Add(collection);
        Debug.Log($"Created new collection for {spec.Property.GetName()}");
    }

    /// <summary>
    ///     Register a property extractor for a specific component type.
    /// </summary>
    /// <param name="extractor">Object that will produce a list of properties when given a component</param>
    /// <typeparam name="T">Component type being targeted</typeparam>
    public void RegisterExtractor<T>(IPropertyExtractor<T> extractor) where T : Component
    {
        if (_frozen)
            throw new InvalidOperationException(
                "Can't register new extractors after Freeze(). Make sure \"Use API\" is checked in the inspector.");
        if (typeof(T) == typeof(Component))
            throw new ArgumentException("Can't override generic extractor for Component type");
        _extractors[typeof(T)] = extractor;
    }

    /// <summary>
    ///     Register a type converter for libmapper to automatically convert complex types into simple types.
    /// </summary>
    /// <param name="converter">A type mapper</param>
    /// <typeparam name="T">The complex type</typeparam>
    /// <typeparam name="U">The primitive type</typeparam>
    public void RegisterTypeConverter<T, U>(ITypeConverter<T, U> converter) where T : notnull where U : notnull
    {
        if (_frozen)
            throw new InvalidOperationException(
                "Can't register new converters after Freeze(). Make sure \"Use API\" is checked in the inspector.");
        _converters[typeof(T)] = converter;
    }

    /// <summary>
    ///     Add a new component to be exposed by the device.
    /// </summary>
    /// <param name="component"></param>
    /// <exception cref="InvalidOperationException">If called while the device is frozen</exception>
    public void AddComponent(Component component)
    {
        if (_frozen)
            throw new InvalidOperationException(
                "Can't add new components after Freeze(). Make sure \"Use API\" is checked in the inspector.");
    }

    /// <summary>
    ///     Freeze the device, preventing new extractors, mappers, or components from being added.
    /// </summary>
    public void Freeze()
    {
        _frozen = true;
    }

    public static Mapper.Type CreateLibmapperTypeFromPrimitive(Type t)
    {
        if (t.IsArray) t = t.GetElementType();
        if (t == typeof(float))
            return Mapper.Type.Float;
        if (t == typeof(int))
            return Mapper.Type.Int32;
        if (t == typeof(double))
            return Mapper.Type.Double;
        return Mapper.Type.Null;
    }


    private System.Collections.Generic.List<IBoundProperty> ExtractProperties(Component target)
    {
        if (_extractors.ContainsKey(target.GetType())) return _extractors[target.GetType()].ExtractProperties(target);

        // use default extractor
        _defaultExtractor ??= new DefaultPropertyExtractor(_converters);
        return _defaultExtractor.ExtractProperties(target);
    }
}

internal class WrappedBoundProperty(IBoundProperty inner, ITypeConverter converter) : IBoundProperty
{
    public int GetVectorLength()
    {
        return converter.VectorLength;
    }

    public Type GetMappedType()
    {
        return converter.SimpleType;
    }

    public void SetObject(object value)
    {
        inner.SetObject(converter.CreateComplexObject(value));
    }

    public object GetValue()
    {
        return converter.CreateSimpleObject(inner.GetValue());
    }

    public string GetName()
    {
        return inner.GetName();
    }

    public string? Units => inner.Units;
    public (float min, float max)? Bounds => inner.Bounds;
}

public readonly struct PollJob(IntPtr devicePtr, int pollTime) : IJob
{
    [NativeDisableUnsafePtrRestriction] // it's probably still unsafe but I don't care
    private readonly IntPtr _devicePtr = devicePtr;

    public void Execute()
    {
        var device = new Device(_devicePtr);
        device.Poll(pollTime);
    }
}