using UnityEngine;

/// <summary>Owns a self-contained pursuer group for SmartEnemiesDemo.</summary>
public sealed class SmartEnemyFlock : MonoBehaviour
{
    [Header("Group")]
    [SerializeField, Min(1)] private int enemyCount = 18;
    [SerializeField] private PlayerTarget player;
    [SerializeField] private Vector2 spawnRadius = new Vector2(3f, 3f);
    [SerializeField] private Color enemyColor = new Color(0.9f, 0.18f, 0.12f);

    [Header("Movement")]
    [SerializeField, Min(0.1f)] private float movementSpeed = 4f;
    [SerializeField, Min(0.01f)] private float steeringResponsiveness = 3f;
    [SerializeField, Min(0.1f)] private float neighborRadius = 7f;
    [SerializeField, Min(0.1f)] private float minimumSeparationDistance = 1.8f;
    [SerializeField, Min(0f)] private float playerKeepDistance = 2.4f;

    [Header("Behavior Weights")]
    [SerializeField, Min(0f)] private float cohesionWeight = 0.85f;
    [SerializeField, Min(0f)] private float separationWeight = 2.4f;
    [SerializeField, Min(0f)] private float alignmentWeight = 1f;
    [SerializeField, Min(0f)] private float pursuitWeight = 2f;
    [SerializeField, Min(0f)] private float obstacleAvoidanceWeight = 3f;

    [Header("Obstacle Avoidance")]
    [SerializeField, Min(0.1f)] private float obstacleDetectionDistance = 3.5f;
    [SerializeField, Min(0.05f)] private float obstacleProbeRadius = 0.4f;
    [SerializeField] private LayerMask obstacleMask;

    [Header("Collision Recovery")]
    [SerializeField, Min(0f)] private float collisionRecoveryStrength = 4f;
    [SerializeField, Min(0.05f)] private float collisionResponseDuration = 0.65f;

    [Header("Player Capture")]
    [SerializeField, Min(0.1f)] private float captureRadius = 3f;
    [SerializeField, Min(0f)] private float captureCooldown = 4f;
    [SerializeField, Min(0.1f)] private float captureDuration = 2.5f;
    [SerializeField, Min(0.1f)] private float captureRearmDistance = 5f;
    [SerializeField] private Color captureColor = new Color(1f, 0.12f, 0.06f);

    private SmartEnemyUnit[] units;
    private Material sharedEnemyMaterial;
    private bool captureLatched;
    private float nextCaptureTime;
    private Vector3 lastCapturePosition;

    public SmartEnemyUnit[] Units => units;
    public PlayerTarget Player => player;
    public float MovementSpeed => movementSpeed;
    public float SteeringResponsiveness => steeringResponsiveness;
    public float NeighborRadius => neighborRadius;
    public float MinimumSeparationDistance => minimumSeparationDistance;
    public float PlayerKeepDistance => playerKeepDistance;
    public float CohesionWeight => cohesionWeight;
    public float SeparationWeight => separationWeight;
    public float AlignmentWeight => alignmentWeight;
    public float PursuitWeight => pursuitWeight;
    public float ObstacleAvoidanceWeight => obstacleAvoidanceWeight;
    public float ObstacleDetectionDistance => obstacleDetectionDistance;
    public float ObstacleProbeRadius => obstacleProbeRadius;
    public LayerMask ObstacleMask => obstacleMask;
    public float CollisionRecoveryStrength => collisionRecoveryStrength;
    public float CollisionResponseDuration => collisionResponseDuration;
    public float CaptureRadius => captureRadius;
    public float CaptureCooldown => captureCooldown;
    public float CaptureDuration => captureDuration;
    public Color CaptureColor => captureColor;

    public bool IsObstacleLayer(int layer) => (obstacleMask.value & (1 << layer)) != 0;

    private void Start()
    {
        if (player == null)
        {
            Debug.LogError("SmartEnemyFlock needs a PlayerTarget reference.", this);
            enabled = false;
            return;
        }

        Shader shader = Shader.Find("Universal Render Pipeline/Lit");
        if (shader == null) shader = Shader.Find("Standard");
        if (shader != null)
        {
            sharedEnemyMaterial = new Material(shader) { color = enemyColor };
        }

        units = new SmartEnemyUnit[enemyCount];
        for (int i = 0; i < units.Length; i++)
        {
            float angle = Random.value * Mathf.PI * 2f;
            Vector2 radial = new Vector2(Mathf.Cos(angle) * spawnRadius.x, Mathf.Sin(angle) * spawnRadius.y);
            Vector3 spawnPosition = transform.position + new Vector3(radial.x, 1f, radial.y);
            GameObject enemy = GameObject.CreatePrimitive(PrimitiveType.Capsule);
            enemy.name = $"Smart Enemy {i + 1:00}";
            enemy.transform.SetParent(transform, true);
            enemy.transform.position = spawnPosition;
            enemy.transform.localScale = new Vector3(0.8f, 0.9f, 0.8f);
            if (sharedEnemyMaterial != null)
                enemy.GetComponent<Renderer>().sharedMaterial = sharedEnemyMaterial;
            Rigidbody body = enemy.AddComponent<Rigidbody>();
            body.useGravity = false;
            body.isKinematic = false;
            body.mass = 1f;
            body.linearDamping = 0.5f;
            body.constraints = RigidbodyConstraints.FreezePositionY |
                               RigidbodyConstraints.FreezeRotationX |
                               RigidbodyConstraints.FreezeRotationZ;
            body.collisionDetectionMode = CollisionDetectionMode.ContinuousDynamic;
            body.interpolation = RigidbodyInterpolation.Interpolate;
            units[i] = enemy.AddComponent<SmartEnemyUnit>();
            units[i].Initialize(this, i);
        }
    }

    private void Update()
    {
        if (units == null) return;
        for (int i = 0; i < units.Length; i++)
            if (units[i] != null) units[i].Steer();
        UpdateCapture();
    }

    private void UpdateCapture()
    {
        if (player == null || units == null) return;

        if (captureLatched)
        {
            float movedSqr = (player.transform.position - lastCapturePosition).sqrMagnitude;
            bool playerEscaped = movedSqr >= captureRearmDistance * captureRearmDistance;
            if (playerEscaped && Time.time >= nextCaptureTime)
                captureLatched = false;
            else
                return;
        }

        if (Time.time < nextCaptureTime || player.IsCaptured) return;
        float radiusSqr = captureRadius * captureRadius;
        Vector3 playerPosition = player.transform.position;
        for (int i = 0; i < units.Length; i++)
        {
            SmartEnemyUnit unit = units[i];
            if (unit == null) continue;
            Vector3 offset = unit.transform.position - playerPosition;
            offset.y = 0f;
            if (offset.sqrMagnitude > radiusSqr) continue;

            if (!player.TryCapture(captureDuration, captureColor)) return;
            captureLatched = true;
            lastCapturePosition = playerPosition;
            nextCaptureTime = Time.time + captureCooldown;
            return;
        }
    }

    private void OnDestroy()
    {
        if (sharedEnemyMaterial == null) return;
        if (Application.isPlaying) Destroy(sharedEnemyMaterial);
        else DestroyImmediate(sharedEnemyMaterial);
    }
}
