using UnityEngine;

/// <summary>Combines local boid rules with a player pursuit waypoint.</summary>
public sealed class SmartEnemyUnit : MonoBehaviour
{
    private static readonly Vector3[] AvoidanceDirections =
    {
        Vector3.right, Vector3.left,
        new Vector3(1f, 0f, 1f).normalized,
        new Vector3(-1f, 0f, 1f).normalized,
        new Vector3(0f, 0.35f, 1f).normalized
    };

    private SmartEnemyFlock flock;
    private int unitIndex;
    private Vector3 steeringVelocity;
    private Vector3 smoothedDirection = Vector3.forward;
    private Vector3 collisionRecoveryDirection;
    private float collisionRecoveryUntil;
    private Rigidbody body;

    public void Initialize(SmartEnemyFlock owner, int index)
    {
        flock = owner;
        unitIndex = index;
        body = GetComponent<Rigidbody>();
        smoothedDirection = transform.forward;
    }

    public void Steer()
    {
        if (flock == null || flock.Units == null || flock.Player == null) return;

        Vector3 position = transform.position;
        Vector3 separation = Vector3.zero;
        Vector3 alignment = Vector3.zero;
        Vector3 center = Vector3.zero;
        int neighbors = 0;
        int closeNeighbors = 0;
        float radiusSqr = flock.NeighborRadius * flock.NeighborRadius;
        float separationSqr = flock.MinimumSeparationDistance * flock.MinimumSeparationDistance;

        SmartEnemyUnit[] group = flock.Units;
        for (int i = 0; i < group.Length; i++)
        {
            SmartEnemyUnit other = group[i];
            if (other == null || other == this) continue;
            Vector3 offset = other.transform.position - position;
            offset.y = 0f;
            float distanceSqr = offset.sqrMagnitude;
            if (distanceSqr > radiusSqr || distanceSqr < 0.0001f) continue;

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
        if (neighbors > 0)
        {
            Vector3 cohesion = center / neighbors - position;
            cohesion.y = 0f;
            desired += cohesion.normalized * flock.CohesionWeight;
            desired += alignment.normalized * flock.AlignmentWeight;
        }
        if (closeNeighbors > 0)
            desired += separation.normalized * flock.SeparationWeight;

        Vector3 target = flock.Player.transform.position;
        Vector3 toPlayer = target - position;
        toPlayer.y = 0f;
        float distanceToPlayer = toPlayer.magnitude;

        // Assign each pursuer a point on a ring so they do not all target one spot.
        float slotAngle = (Mathf.PI * 2f * unitIndex) / Mathf.Max(1, group.Length);
        Vector3 ringOffset = new Vector3(Mathf.Cos(slotAngle), 0f, Mathf.Sin(slotAngle)) * flock.PlayerKeepDistance;
        Vector3 waypoint = target + ringOffset;
        Vector3 toWaypoint = waypoint - position;
        toWaypoint.y = 0f;
        if (toWaypoint.sqrMagnitude > 0.25f)
            desired += toWaypoint.normalized * flock.PursuitWeight;

        // Personal-space correction keeps an enemy from entering the player's capsule.
        if (distanceToPlayer < flock.PlayerKeepDistance * 0.75f && distanceToPlayer > 0.001f)
            desired += -toPlayer.normalized * flock.SeparationWeight;

        desired += FindObstacleSteering() * flock.ObstacleAvoidanceWeight;
        float recoveryTime = collisionRecoveryUntil - Time.time;
        if (recoveryTime > 0f && flock.CollisionResponseDuration > 0f)
        {
            float recoveryBlend = Mathf.Clamp01(recoveryTime / flock.CollisionResponseDuration);
            desired += collisionRecoveryDirection * flock.CollisionRecoveryStrength * recoveryBlend;
        }
        if (desired.sqrMagnitude < 0.0001f)
            desired = transform.forward;
        desired.y = 0f;

        smoothedDirection = Vector3.SmoothDamp(smoothedDirection, desired.normalized,
            ref steeringVelocity, 1f / Mathf.Max(0.01f, flock.SteeringResponsiveness));
        smoothedDirection.y = 0f;
        if (smoothedDirection.sqrMagnitude > 0.0001f)
            smoothedDirection.Normalize();
    }

    private void FixedUpdate()
    {
        if (body == null || flock == null || smoothedDirection.sqrMagnitude < 0.0001f) return;

        Quaternion targetRotation = Quaternion.LookRotation(smoothedDirection, Vector3.up);
        body.MoveRotation(Quaternion.RotateTowards(body.rotation, targetRotation,
            flock.SteeringResponsiveness * 90f * Time.fixedDeltaTime));
        Vector3 velocity = body.rotation * Vector3.forward * flock.MovementSpeed;
        body.linearVelocity = new Vector3(velocity.x, 0f, velocity.z);
    }

    private void OnCollisionEnter(Collision collision)
    {
        RegisterObstacleContact(collision);
    }

    private void OnCollisionStay(Collision collision)
    {
        RegisterObstacleContact(collision);
    }

    private void RegisterObstacleContact(Collision collision)
    {
        if (flock == null || !flock.IsObstacleLayer(collision.gameObject.layer) || collision.contactCount == 0)
            return;

        ContactPoint contact = collision.GetContact(0);
        Vector3 away = transform.position - contact.point;
        away.y = 0f;
        if (away.sqrMagnitude < 0.0001f)
        {
            away = Vector3.ProjectOnPlane(contact.normal, Vector3.up);
            if (Vector3.Dot(away, transform.forward) > 0f) away = -away;
        }
        if (away.sqrMagnitude < 0.0001f)
            away = -Vector3.ProjectOnPlane(transform.forward, Vector3.up);

        collisionRecoveryDirection = away.normalized;
        collisionRecoveryUntil = Time.time + flock.CollisionResponseDuration;
    }

    private Vector3 FindObstacleSteering()
    {
        if (flock.ObstacleMask.value == 0) return Vector3.zero;
        Vector3 origin = transform.position + Vector3.up * 0.45f;
        float distance = flock.ObstacleDetectionDistance;
        if (!Physics.SphereCast(origin, flock.ObstacleProbeRadius, transform.forward,
                out RaycastHit hit, distance, flock.ObstacleMask, QueryTriggerInteraction.Ignore))
            return Vector3.zero;

        for (int i = 0; i < AvoidanceDirections.Length; i++)
        {
            Vector3 direction = transform.TransformDirection(AvoidanceDirections[i]);
            if (!Physics.SphereCast(origin, flock.ObstacleProbeRadius, direction,
                    out _, distance, flock.ObstacleMask, QueryTriggerInteraction.Ignore))
                return direction;
        }
        return Vector3.ProjectOnPlane(Vector3.Reflect(transform.forward, hit.normal), Vector3.up).normalized;
    }
}
