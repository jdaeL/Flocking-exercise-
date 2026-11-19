using UnityEngine;
using UnityEngine.InputSystem;

/// <summary>Small keyboard-controlled target for the SmartEnemiesDemo scene.</summary>
public sealed class PlayerTarget : MonoBehaviour
{
    [SerializeField, Min(0.1f)] private float moveSpeed = 6f;
    [SerializeField, Min(0.01f)] private float turnResponsiveness = 12f;
    [SerializeField, Min(1f)] private float worldLimit = 25f;
    [SerializeField] private Color playerColor = new Color(0.12f, 0.55f, 0.95f);
    [SerializeField] private string captureMessage = "PLAYER CAPTURED";

    private Vector3 movement;
    private Material playerMaterial;
    private bool isCaptured;
    private float captureEndsAt;
    private static GUIStyle captureMessageStyle;

    public bool IsCaptured => isCaptured;

    private void Awake()
    {
        Shader shader = Shader.Find("Universal Render Pipeline/Lit");
        if (shader == null) shader = Shader.Find("Standard");
        if (shader != null)
        {
            playerMaterial = new Material(shader) { color = playerColor };
            Renderer renderer = GetComponent<Renderer>();
            if (renderer != null) renderer.sharedMaterial = playerMaterial;
        }

        Collider playerCollider = GetComponent<Collider>();
        if (playerCollider != null) playerCollider.isTrigger = true;
    }

    public bool TryCapture(float duration, Color captureColor)
    {
        if (isCaptured) return false;
        isCaptured = true;
        captureEndsAt = Time.time + Mathf.Max(0.1f, duration);
        if (playerMaterial != null) playerMaterial.color = captureColor;
        return true;
    }

    private void Update()
    {
        if (isCaptured)
        {
            if (Time.time < captureEndsAt) return;
            isCaptured = false;
            if (playerMaterial != null) playerMaterial.color = playerColor;
        }

        Keyboard keyboard = Keyboard.current;
        if (keyboard == null) return;

        movement = Vector3.zero;
        if (keyboard.wKey.isPressed) movement.z += 1f;
        if (keyboard.sKey.isPressed) movement.z -= 1f;
        if (keyboard.dKey.isPressed) movement.x += 1f;
        if (keyboard.aKey.isPressed) movement.x -= 1f;

        if (movement.sqrMagnitude < 0.001f) return;
        Vector3 direction = movement.normalized;
        transform.position += direction * moveSpeed * Time.deltaTime;
        transform.position = new Vector3(
            Mathf.Clamp(transform.position.x, -worldLimit, worldLimit), 1f,
            Mathf.Clamp(transform.position.z, -worldLimit, worldLimit));
        Quaternion targetRotation = Quaternion.LookRotation(direction, Vector3.up);
        transform.rotation = Quaternion.Slerp(transform.rotation, targetRotation,
            1f - Mathf.Exp(-turnResponsiveness * Time.deltaTime));
    }

    private void OnGUI()
    {
        if (!isCaptured) return;
        if (captureMessageStyle == null)
        {
            captureMessageStyle = new GUIStyle(GUI.skin.label)
            {
                alignment = TextAnchor.MiddleCenter,
                fontSize = 34,
                fontStyle = FontStyle.Bold,
                normal = { textColor = Color.white }
            };
        }

        Rect panel = new Rect(Screen.width * 0.5f - 230f, 32f, 460f, 72f);
        GUI.Box(panel, GUIContent.none);
        GUI.Label(panel, captureMessage, captureMessageStyle);
    }

    private void OnDestroy()
    {
        if (playerMaterial == null) return;
        if (Application.isPlaying) Destroy(playerMaterial);
        else DestroyImmediate(playerMaterial);
    }
}
