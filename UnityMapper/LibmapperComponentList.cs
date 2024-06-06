using UnityEngine;

namespace UnityMapper;

public class LibmapperComponentList : MonoBehaviour
{
    public event EventHandler<List<Component>>? Destroyed; 
    public List<Component> componentsToExpose = [];
    internal bool Visited = false;

    private void OnDestroy()
    {
        Destroyed?.Invoke(this, componentsToExpose);
    }
}