using UnityEngine;

/// <summary>Fish agent applying separation, alignment, cohesion and obstacle steering.</summary>
public sealed class FlockUnit : MonoBehaviour
{
    private static readonly Vector3[] AvoidanceDirections =
    {
        Vector3.right, Vector3.left, Vector3.up, Vector3.down,
        new Vector3(1f, 0f, 1f).normalized, new Vector3(-1f, 0f, 1f).normalized
    };
    private Flock flock;
    private float speed;
    private Vector3 steeringVelocity;

    public void AssignFlock(Flock owner, float initialSpeed)
    {
        flock = owner;
        speed = initialSpeed;
    }

    public void MoveUnit()
    {
        if (flock == null || flock.AllUnits == null) return;

        Vector3 separation = Vector3.zero;
        Vector3 alignment = Vector3.zero;
        Vector3 center = Vector3.zero;
        int neighbors = 0;
        int closeNeighbors = 0;
        float neighborRadiusSqr = flock.NeighborRadius * flock.NeighborRadius;
        float separationSqr = flock.MinimumSeparationDistance * flock.MinimumSeparationDistance;
        Vector3 position = transform.position;

        // O(n²) scan is allocation-free and adequate for this lab's 100 agents.
        FlockUnit[] units = flock.AllUnits;
        for (int i = 0; i < units.Length; i++)
        {
            FlockUnit other = units[i];
            if (other == null || other == this) continue;
            Vector3 offset = other.transform.position - position;
            float distanceSqr = offset.sqrMagnitude;
            if (distanceSqr > neighborRadiusSqr || distanceSqr < 0.0001f) continue;
            if (Vector3.Angle(transform.forward, offset) > flock.FieldOfView) continue;

            neighbors++;
            center += other.transform.position;
            alignment += other.transform.forward;
            if (distanceSqr < separationSqr)
            {
                closeNeighbors++;
                separation -= offset / distanceSqr;
            }
        }

        Vector3 desired = Vector3.zero;
        if (closeNeighbors > 0) desired += separation.normalized * flock.SeparationWeight;
        if (neighbors > 0)
        {
            desired += (alignment / neighbors).normalized * flock.AlignmentWeight;
            desired += ((center / neighbors) - position).normalized * flock.CohesionWeight;
        }

        Vector3 fromCenter = position - flock.transform.position;
        if (fromCenter.sqrMagnitude > flock.BoundaryRadius * flock.BoundaryRadius)
            desired -= fromCenter.normalized * flock.BoundaryWeight;

        desired += CalculateObstacleAvoidance() * flock.ObstacleWeight;
        if (desired.sqrMagnitude < 0.0001f) desired = transform.forward;

        Vector3 nextForward = Vector3.SmoothDamp(transform.forward, desired.normalized,
            ref steeringVelocity, 1f / flock.SteeringResponsiveness);
        if (nextForward.sqrMagnitude > 0.0001f)
            transform.rotation = Quaternion.LookRotation(nextForward.normalized, Vector3.up);
        transform.position += transform.forward * speed * Time.deltaTime;
    }

    private Vector3 CalculateObstacleAvoidance()
    {
        float distance = flock.ObstacleDetectionDistance;
        if (!Physics.SphereCast(transform.position, flock.ObstacleProbeRadius, transform.forward,
                out RaycastHit hit, distance, flock.ObstacleMask, QueryTriggerInteraction.Ignore))
            return Vector3.zero;

        // Probe a fan of directions and take the first clear heading; obstacle
        // avoidance remains distinct from agent-to-agent separation.
        for (int i = 0; i < AvoidanceDirections.Length; i++)
        {
            Vector3 direction = transform.TransformDirection(AvoidanceDirections[i]);
            if (!Physics.SphereCast(transform.position, flock.ObstacleProbeRadius, direction,
                    out _, distance, flock.ObstacleMask, QueryTriggerInteraction.Ignore))
                return direction;
        }
        return Vector3.Reflect(transform.forward, hit.normal).normalized;
    }
}
