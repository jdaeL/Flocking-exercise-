using UnityEngine;

/// <summary>Owns the flock settings and creates its agents at runtime.</summary>
public sealed class Flock : MonoBehaviour
{
    [Header("Spawn")]
    [SerializeField] private FlockUnit flockUnitPrefab;
    [SerializeField, Min(1)] private int flockSize = 100;
    [SerializeField] private Vector3 spawnBounds = new Vector3(12f, 3f, 12f);

    [Header("Movement")]
    [SerializeField, Min(0f)] private float minSpeed = 3f;
    [SerializeField, Min(0f)] private float maxSpeed = 5f;
    [SerializeField, Min(0.01f)] private float steeringResponsiveness = 2.5f;
    [SerializeField, Range(1f, 360f)] private float fieldOfView = 270f;

    [Header("Flocking Weights")]
    [SerializeField, Min(0f)] private float separationWeight = 2.2f;
    [SerializeField, Min(0f)] private float alignmentWeight = 1f;
    [SerializeField, Min(0f)] private float cohesionWeight = 0.8f;
    [SerializeField, Min(0f)] private float boundaryWeight = 1.5f;
    [SerializeField, Min(0f)] private float obstacleWeight = 3f;

    [Header("Neighborhood")]
    [SerializeField, Min(0.01f)] private float neighborRadius = 6f;
    [SerializeField, Min(0.01f)] private float minimumSeparationDistance = 1.5f;
    [SerializeField, Min(0.01f)] private float boundaryRadius = 22f;

    [Header("Obstacle Avoidance")]
    [SerializeField, Min(0.1f)] private float obstacleDetectionDistance = 5f;
    [SerializeField, Min(0.01f)] private float obstacleProbeRadius = 0.45f;
    [SerializeField] private LayerMask obstacleMask = ~0;

    private FlockUnit[] allUnits;
    public FlockUnit[] AllUnits => allUnits;
    public float MinSpeed => minSpeed;
    public float MaxSpeed => maxSpeed;
    public float SteeringResponsiveness => steeringResponsiveness;
    public float FieldOfView => fieldOfView;
    public float SeparationWeight => separationWeight;
    public float AlignmentWeight => alignmentWeight;
    public float CohesionWeight => cohesionWeight;
    public float BoundaryWeight => boundaryWeight;
    public float ObstacleWeight => obstacleWeight;
    public float NeighborRadius => neighborRadius;
    public float MinimumSeparationDistance => minimumSeparationDistance;
    public float BoundaryRadius => boundaryRadius;
    public float ObstacleDetectionDistance => obstacleDetectionDistance;
    public float ObstacleProbeRadius => obstacleProbeRadius;
    public LayerMask ObstacleMask => obstacleMask;

    private void Start()
    {
        if (flockUnitPrefab == null)
        {
            Debug.LogError("Flock requires the Fish prefab in Flock Unit Prefab.", this);
            enabled = false;
            return;
        }

        GenerateUnits();
    }

    private void Update()
    {
        if (allUnits == null) return;
        for (int i = 0; i < allUnits.Length; i++)
            if (allUnits[i] != null) allUnits[i].MoveUnit();
    }

    private void GenerateUnits()
    {
        allUnits = new FlockUnit[flockSize];
        for (int i = 0; i < flockSize; i++)
        {
            Vector3 offset = Vector3.Scale(Random.insideUnitSphere, spawnBounds);
            Quaternion rotation = Quaternion.Euler(0f, Random.Range(0f, 360f), 0f);
            FlockUnit unit = Instantiate(flockUnitPrefab, transform.position + offset, rotation, transform);
            unit.AssignFlock(this, Random.Range(minSpeed, Mathf.Max(minSpeed, maxSpeed)));
            allUnits[i] = unit;
        }
    }

    private void OnDrawGizmosSelected()
    {
        Gizmos.color = new Color(0.2f, 0.8f, 1f, 0.45f);
        Gizmos.DrawWireSphere(transform.position, boundaryRadius);
        Gizmos.color = Color.green;
        Gizmos.DrawWireCube(transform.position, spawnBounds * 2f);
    }
}
