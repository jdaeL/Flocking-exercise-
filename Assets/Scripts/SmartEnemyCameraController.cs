using UnityEngine;
using UnityEngine.InputSystem;

/// <summary>Tracks the midpoint between the player and pursuer group.</summary>
public sealed class SmartEnemyCameraController : MonoBehaviour
{
    [SerializeField] private PlayerTarget player;
    [SerializeField] private Transform enemyGroup;
    [SerializeField] private Vector3 followOffset = new Vector3(0f, 18f, -22f);
    [SerializeField, Min(0.01f)] private float followResponsiveness = 4f;
    [SerializeField, Min(1f)] private float zoomSpeed = 2f;
    [SerializeField] private float minimumHeight = 10f;
    [SerializeField] private float maximumHeight = 32f;

    private Vector3 positionVelocity;

    private void LateUpdate()
    {
        if (player == null || enemyGroup == null) return;

        Vector3 center = (player.transform.position + enemyGroup.position) * 0.5f;
        Mouse mouse = Mouse.current;
        if (mouse != null)
        {
            float scroll = mouse.scroll.ReadValue().y;
            followOffset.y = Mathf.Clamp(followOffset.y - scroll * zoomSpeed * 0.01f,
                minimumHeight, maximumHeight);
        }

        Vector3 desiredPosition = center + followOffset;
        transform.position = Vector3.SmoothDamp(transform.position, desiredPosition,
            ref positionVelocity, 1f / Mathf.Max(0.01f, followResponsiveness));
        transform.rotation = Quaternion.LookRotation(center - transform.position, Vector3.up);
    }

    public void Configure(PlayerTarget targetPlayer, Transform group)
    {
        player = targetPlayer;
        enemyGroup = group;
    }
}
