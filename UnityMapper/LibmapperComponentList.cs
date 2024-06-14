using UnityEngine;

namespace UnityMapper;

public class LibmapperComponentList : MonoBehaviour
{
    public event EventHandler<List<Component>>? Destroyed; 
    
    public bool isEphemeral = false;
    public bool canInstance = true;
    public SignalType type = SignalType.ReadWrite;
    public List<Component> componentsToExpose = [];
    internal bool Visited = false;

    private void OnDestroy()
    {
        Destroyed?.Invoke(this, componentsToExpose);
    }
}

public enum SignalType
{
    ReadOnly,
    [InspectorName("Read and Write")]
    ReadWrite,
    // TODO: This option should be removed once the cause of the timestamp bug is found
    WriteOnly
}