using Unity.AI.Navigation;
using UnityEngine;
using UnityEngine.AI;

public class NavMeshInitializerScript : MonoBehaviour
{
    [Header("Build Settings")]
    [Tooltip("Build the NavMesh automatically on Start.")]
    [SerializeField] private bool _buildOnStart = true;
    [Tooltip("Voxel size used when baking. Lower = more precise but slower.")]
    [SerializeField] private float _voxelSize = 0.2f;
    [Tooltip("Which geometry sources to use for baking. PhysicsColliders are recommended for runtime builds.")]
    [SerializeField] private NavMeshCollectGeometry _useGeometry = NavMeshCollectGeometry.PhysicsColliders;
    [Tooltip("Which objects are included in the bake.")]
    [SerializeField] private CollectObjects _collectObjects = CollectObjects.All;

    private NavMeshSurface _surface;

    private void Start()
    {
        if (_buildOnStart)
        {
            BuildNavMesh();
        }
    }

    public void BuildNavMesh()
    {
        _surface = GetComponent<NavMeshSurface>();
        if (_surface == null)
        {
            _surface = gameObject.AddComponent<NavMeshSurface>();
        }

        _surface.collectObjects = _collectObjects;
        _surface.useGeometry = _useGeometry;
        _surface.voxelSize = _voxelSize;
        _surface.BuildNavMesh();
    }

    public void ClearNavMesh()
    {
        if (_surface == null)
            _surface = GetComponent<NavMeshSurface>();

        _surface?.RemoveData();
    }
}
