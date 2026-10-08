using System.Collections.Generic;
using UnityEngine;
#if ENABLE_INPUT_SYSTEM
using UnityEngine.InputSystem;
#endif

namespace Replica.Player
{
    /// <summary>
    /// Cámara en tercera persona que sigue y orbita alrededor del perro (jugador).
    /// - Mouse: girar la cámara (cursor bloqueado; Esc lo libera, clic lo vuelve a bloquear).
    /// - Rueda del mouse: acercar / alejar.
    /// - Stick derecho del gamepad: girar la cámara.
    /// Evita atravesar edificios haciendo un SphereCast desde el perro hacia la cámara.
    /// Arrastra este componente a la Main Camera.
    /// </summary>
    public class ThirdPersonCamera : MonoBehaviour
    {
        [Header("Objetivo")]
        [Tooltip("Transform a seguir (el Player). Si está vacío se busca el PlayerController.")]
        public Transform target;
        [Tooltip("Punto de enfoque relativo al pivote del jugador (altura de la cabeza del perro)")]
        [SerializeField] private Vector3 targetOffset = new Vector3(0f, 0.6f, 0f);

        [Header("Distancia")]
        [SerializeField] private float distance = 4.0f;
        [SerializeField] private float minDistance = 1.5f;
        [SerializeField] private float maxDistance = 9.0f;
        [SerializeField] private float zoomSpeed = 1.0f;

        [Header("Rotación")]
        [SerializeField] private float pitch = 20f;
        [SerializeField] private float minPitch = -10f;
        [SerializeField] private float maxPitch = 70f;
        [Tooltip("Sensibilidad del mouse")]
        [SerializeField] private float mouseSensitivity = 0.12f;
        [Tooltip("Sensibilidad del stick derecho (grados/segundo)")]
        [SerializeField] private float gamepadSensitivity = 150f;
        [Tooltip("Si está activo, solo gira mientras mantienes el clic derecho (y no bloquea el cursor)")]
        [SerializeField] private bool requireRightMouseButton = false;

        [Header("Suavizado y Colisión")]
        [SerializeField] private float followSmoothing = 12f;
        [SerializeField] private LayerMask collisionMask = ~0;
        [SerializeField] private float collisionRadius = 0.2f;

        private float yaw;
        private Vector3 currentFocus;

        private void Start()
        {
            if (target == null)
            {
                var player = FindAnyObjectByType<PlayerController>();
                if (player != null) target = player.transform;
            }

            if (target != null)
            {
                yaw = target.eulerAngles.y;
                currentFocus = target.position + targetOffset;
                ApplyTransform(true);
                Debug.Log($"[ThirdPersonCamera] Iniciada correctamente siguiendo a {target.name}");
            }
            else
            {
                Debug.LogError("[ThirdPersonCamera] No se encontró target ni PlayerController.");
            }

            if (!requireRightMouseButton) LockCursor(true);
        }

        private void LateUpdate()
        {
            if (target == null) return;

            HandleCursor();

            Vector2 look = ReadLookInput();
            yaw += look.x;
            pitch = Mathf.Clamp(pitch - look.y, minPitch, maxPitch);

            distance = Mathf.Clamp(distance - ReadZoomInput() * zoomSpeed, minDistance, maxDistance);

            ApplyTransform(false);
        }

        private void ApplyTransform(bool instant)
        {
            Vector3 desiredFocus = target.position + targetOffset;
            float t = instant ? 1f : 1f - Mathf.Exp(-followSmoothing * Time.deltaTime);
            currentFocus = Vector3.Lerp(currentFocus, desiredFocus, t);

            Quaternion rotation = Quaternion.Euler(pitch, yaw, 0f);
            Vector3 direction = rotation * Vector3.back;
            float finalDistance = GetUnobstructedDistance(currentFocus, direction, distance);

            transform.SetPositionAndRotation(currentFocus + direction * finalDistance, rotation);
        }

        private float GetUnobstructedDistance(Vector3 origin, Vector3 direction, float maxDist)
        {
            RaycastHit[] hits = Physics.SphereCastAll(origin, collisionRadius, direction, maxDist, collisionMask, QueryTriggerInteraction.Ignore);
            float closest = maxDist;
            foreach (var hit in hits)
            {
                if (hit.collider == null || hit.transform.IsChildOf(target)) continue;
                if (hit.distance <= 0f) continue; // empezó dentro del collider
                closest = Mathf.Min(closest, hit.distance - 0.05f);
            }
            return Mathf.Max(closest, 0.3f);
        }

        private bool IsDialogueOpen()
        {
            return Replica.Dialogue.DialogueSystem.Instance != null && Replica.Dialogue.DialogueSystem.Instance.IsInDialogue;
        }

        private void HandleCursor()
        {
            if (requireRightMouseButton) return;
#if ENABLE_INPUT_SYSTEM
            if (Keyboard.current != null && Keyboard.current.escapeKey.wasPressedThisFrame) LockCursor(false);
            if (Mouse.current != null && Mouse.current.leftButton.wasPressedThisFrame && !IsDialogueOpen()) LockCursor(true);
#else
            if (Input.GetKeyDown(KeyCode.Escape)) LockCursor(false);
            if (Input.GetMouseButtonDown(0) && !IsDialogueOpen()) LockCursor(true);
#endif
        }

        private static void LockCursor(bool locked)
        {
            Cursor.lockState = locked ? CursorLockMode.Locked : CursorLockMode.None;
            Cursor.visible = !locked;
        }

        private Vector2 ReadLookInput()
        {
            Vector2 result = Vector2.zero;
            bool mouseAllowed = requireRightMouseButton ? IsRightMouseHeld() : Cursor.lockState == CursorLockMode.Locked;

#if ENABLE_INPUT_SYSTEM
            if (mouseAllowed && Mouse.current != null)
                result += Mouse.current.delta.ReadValue() * mouseSensitivity;
            if (Gamepad.current != null)
                result += Gamepad.current.rightStick.ReadValue() * gamepadSensitivity * Time.deltaTime;
#else
            if (mouseAllowed)
                result += new Vector2(Input.GetAxis("Mouse X"), Input.GetAxis("Mouse Y")) * mouseSensitivity * 20f;
#endif
            return result;
        }

        private bool IsRightMouseHeld()
        {
#if ENABLE_INPUT_SYSTEM
            return Mouse.current != null && Mouse.current.rightButton.isPressed;
#else
            return Input.GetMouseButton(1);
#endif
        }

        private float ReadZoomInput()
        {
#if ENABLE_INPUT_SYSTEM
            if (Mouse.current != null)
                return Mathf.Clamp(Mouse.current.scroll.ReadValue().y, -1f, 1f);
            return 0f;
#else
            return Input.mouseScrollDelta.y;
#endif
        }
    }
}
