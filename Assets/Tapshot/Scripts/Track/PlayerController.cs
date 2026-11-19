using System;
using UnityEngine;
using UnityEngine.InputSystem;

namespace Tapshot
{
    /// <summary>
    /// First-person aim controller: mouse look (cursor locked) and left-click hitscan shooting
    /// from the camera centre, which is where the UI crosshair sits.
    /// </summary>
    public class PlayerController : MonoBehaviour
    {
        [SerializeField] Camera playerCamera;
        [SerializeField] float sensitivity = 0.08f;      // degrees per mouse pixel
        [SerializeField] float maxYaw = 50f;
        [SerializeField] float maxPitch = 35f;
        [SerializeField] float range = 100f;
        [SerializeField] LayerMask hitMask = ~0;

        float yaw, pitch;

        /// <summary>Fired on every shot. Arguments: whether it hit a target, and the target (or null).</summary>
        public event Action<bool, TrackTarget> Shot;

        /// <summary>When false, look and shooting are ignored.</summary>
        public bool InputEnabled { get; set; } = true;

        public void Setup(Camera cam) => playerCamera = cam;

        void Awake()
        {
            if (playerCamera == null) playerCamera = GetComponentInChildren<Camera>();
            Vector3 e = transform.eulerAngles;
            yaw = e.y > 180f ? e.y - 360f : e.y;
        }

        void OnEnable() => SetCursorLocked(true);

        void OnDisable() => SetCursorLocked(false);

        public static void SetCursorLocked(bool locked)
        {
            Cursor.lockState = locked ? CursorLockMode.Locked : CursorLockMode.None;
            Cursor.visible = !locked;
        }

        void Update()
        {
            var mouse = Mouse.current;
            if (mouse == null || !InputEnabled) return;

            // Re-lock if focus was lost and regained (the first click only re-locks).
            if (Cursor.lockState != CursorLockMode.Locked)
            {
                if (mouse.leftButton.wasPressedThisFrame) SetCursorLocked(true);
                return;
            }

            Vector2 delta = mouse.delta.ReadValue() * sensitivity;
            yaw = Mathf.Clamp(yaw + delta.x, -maxYaw, maxYaw);
            pitch = Mathf.Clamp(pitch - delta.y, -maxPitch, maxPitch);
            transform.rotation = Quaternion.Euler(pitch, yaw, 0f);

            if (mouse.leftButton.wasPressedThisFrame) Fire();
        }

        void Fire()
        {
            Transform t = playerCamera != null ? playerCamera.transform : transform;
            if (Physics.Raycast(t.position, t.forward, out RaycastHit hit, range, hitMask, QueryTriggerInteraction.Ignore)
                && hit.collider.TryGetComponent(out TrackTarget target))
            {
                Shot?.Invoke(true, target);
            }
            else
            {
                Shot?.Invoke(false, null);
            }
        }
    }
}
