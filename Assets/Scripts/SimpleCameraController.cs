using UnityEngine;
using UnityEngine.InputSystem;

namespace UnityTemplateProjects
{
    /// <summary>Free-fly demonstration camera using the project's Input System (new) setting.</summary>
    public sealed class SimpleCameraController : MonoBehaviour
    {
        [SerializeField, Min(0.1f)] private float moveSpeed = 12f;
        [SerializeField, Min(0.01f)] private float lookSensitivity = 0.12f;
        [SerializeField, Min(1f)] private float fastMoveMultiplier = 3f;
        [SerializeField, Min(1f)] private float zoomStep = 3f;

        private float pitch;
        private float yaw;
        private Camera controlledCamera;

        private void Awake()
        {
            Vector3 angles = transform.eulerAngles;
            pitch = angles.x;
            yaw = angles.y;
            controlledCamera = GetComponent<Camera>();
        }

        private void Update()
        {
            Keyboard keyboard = Keyboard.current;
            Mouse mouse = Mouse.current;
            if (keyboard == null || mouse == null) return;

            if (mouse.rightButton.wasPressedThisFrame) Cursor.lockState = CursorLockMode.Locked;
            if (mouse.rightButton.wasReleasedThisFrame) Cursor.lockState = CursorLockMode.None;

            if (mouse.rightButton.isPressed)
            {
                Vector2 delta = mouse.delta.ReadValue() * lookSensitivity;
                yaw += delta.x;
                pitch = Mathf.Clamp(pitch - delta.y, -85f, 85f);
                transform.rotation = Quaternion.Euler(pitch, yaw, 0f);
            }

            Vector3 movement = Vector3.zero;
            if (keyboard.wKey.isPressed) movement.z += 1f;
            if (keyboard.sKey.isPressed) movement.z -= 1f;
            if (keyboard.dKey.isPressed) movement.x += 1f;
            if (keyboard.aKey.isPressed) movement.x -= 1f;
            if (keyboard.eKey.isPressed) movement.y += 1f;
            if (keyboard.qKey.isPressed) movement.y -= 1f;
            float multiplier = keyboard.leftShiftKey.isPressed || keyboard.rightShiftKey.isPressed
                ? fastMoveMultiplier : 1f;
            transform.position += transform.TransformDirection(movement.normalized) * moveSpeed * multiplier * Time.deltaTime;

            if (controlledCamera != null)
            {
                float scroll = mouse.scroll.ReadValue().y;
                controlledCamera.fieldOfView = Mathf.Clamp(controlledCamera.fieldOfView - scroll * zoomStep * 0.01f,
                    25f, 80f);
            }
        }

        private void OnDisable() => Cursor.lockState = CursorLockMode.None;
    }
}
