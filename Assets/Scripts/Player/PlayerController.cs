using UnityEngine;
using Replica.Utils;
#if ENABLE_INPUT_SYSTEM
using UnityEngine.InputSystem;
#endif

namespace Replica.Player
{
    /// <summary>
    /// Controlador de movimiento en tercera persona para el perro protagonista.
    /// Arrastra este componente al GameObject del jugador (Perro). Requiere CharacterController.
    /// </summary>
    [RequireComponent(typeof(CharacterController))]
    public class PlayerController : MonoBehaviour
    {
        [Header("Configuracion de Movimiento")]
        [Tooltip("Velocidad al caminar")]
        [SerializeField] private float walkSpeed = 3.5f;
        [Tooltip("Velocidad al correr")]
        [SerializeField] private float runSpeed = 7.0f;
        [Tooltip("Suavizado de rotacion hacia la direccion de movimiento")]
        [SerializeField] private float rotationSmoothTime = 0.1f;
        [Tooltip("Gravedad aplicada al jugador")]
        [SerializeField] private float gravity = -18.0f;
        [Tooltip("Fuerza del salto")]
        [SerializeField] private float jumpHeight = 1.5f;

        [Header("Referencias Opcionales")]
        [Tooltip("Camara principal (si esta vacia, se buscara Camera.main)")]
        [SerializeField] private Transform cameraTransform;
        [Tooltip("Animator del perro (debe contener parametros 'Speed' y 'IsRunning')")]
        [SerializeField] private Animator animator;

        [Header("Estado")]
        [Tooltip("Puntos de montaje para acompanantes montados (ej. lomo, cabeza)")]
        public Transform mountPointBack;
        public Transform mountPointHead;

        private CharacterController characterController;
        private Vector3 velocity;
        private float currentVelocityY;
        private float rotationVelocity;
        private bool isRunning;
        private bool canMove = true;

        public bool CanMove
        {
            get => canMove;
            set
            {
                canMove = value;
                if (!canMove && animator != null)
                {
                    animator.TrySetFloat("Speed", 0f);
                    animator.TrySetBool("IsRunning", false);
                    animator.TrySetInteger(DogAnimationIds.ParameterName, DogAnimationIds.Idle);
                }
            }
        }

        private void Awake()
        {
            characterController = GetComponent<CharacterController>();
            if (animator == null)
                animator = GetComponentInChildren<Animator>();

            if (cameraTransform == null && Camera.main != null)
                cameraTransform = Camera.main.transform;
        }

        private void Update()
        {
            bool jumpPressed = canMove && ReadJumpInput();
            ApplyGravity(jumpPressed);

            if (!canMove) return;

            Vector2 input = ReadMoveInput();
            isRunning = ReadRunInput();

            Vector3 moveDirection = CalculateMoveDirection(input);
            float targetSpeed = (isRunning ? runSpeed : walkSpeed) * input.magnitude;

            if (moveDirection.sqrMagnitude > 0.001f)
            {
                float targetAngle = Mathf.Atan2(moveDirection.x, moveDirection.z) * Mathf.Rad2Deg;
                float angle = Mathf.SmoothDampAngle(transform.eulerAngles.y, targetAngle, ref rotationVelocity, rotationSmoothTime);
                transform.rotation = Quaternion.Euler(0f, angle, 0f);
            }

            Vector3 motion = (moveDirection * targetSpeed) + new Vector3(0f, currentVelocityY, 0f);
            characterController.Move(motion * Time.deltaTime);

            UpdateAnimator(input.magnitude, isRunning);
        }

        private Vector2 ReadMoveInput()
        {
#if ENABLE_INPUT_SYSTEM
            if (Keyboard.current != null)
            {
                float x = 0f;
                float y = 0f;
                if (Keyboard.current.wKey.isPressed || Keyboard.current.upArrowKey.isPressed) y += 1f;
                if (Keyboard.current.sKey.isPressed || Keyboard.current.downArrowKey.isPressed) y -= 1f;
                if (Keyboard.current.aKey.isPressed || Keyboard.current.leftArrowKey.isPressed) x -= 1f;
                if (Keyboard.current.dKey.isPressed || Keyboard.current.rightArrowKey.isPressed) x += 1f;

                if (Gamepad.current != null)
                {
                    Vector2 stick = Gamepad.current.leftStick.ReadValue();
                    if (stick.sqrMagnitude > 0.01f) return Vector2.ClampMagnitude(stick, 1f);
                }
                return Vector2.ClampMagnitude(new Vector2(x, y), 1f);
            }
#endif
            return Vector2.ClampMagnitude(new Vector2(Input.GetAxisRaw("Horizontal"), Input.GetAxisRaw("Vertical")), 1f);
        }

        private bool ReadRunInput()
        {
#if ENABLE_INPUT_SYSTEM
            if (Keyboard.current != null && (Keyboard.current.leftShiftKey.isPressed || Keyboard.current.rightShiftKey.isPressed))
                return true;
            if (Gamepad.current != null && Gamepad.current.buttonSouth.isPressed)
                return true;
#endif
            return Input.GetKey(KeyCode.LeftShift) || Input.GetKey(KeyCode.RightShift);
        }

        private bool ReadJumpInput()
        {
#if ENABLE_INPUT_SYSTEM
            if (Keyboard.current != null && Keyboard.current.spaceKey.wasPressedThisFrame)
                return true;
            if (Gamepad.current != null && Gamepad.current.buttonEast.wasPressedThisFrame)
                return true;
#endif
            return Input.GetKeyDown(KeyCode.Space);
        }

        private Vector3 CalculateMoveDirection(Vector2 input)
        {
            if (input.sqrMagnitude < 0.001f) return Vector3.zero;

            Vector3 forward = cameraTransform != null ? cameraTransform.forward : Vector3.forward;
            Vector3 right = cameraTransform != null ? cameraTransform.right : Vector3.right;
            forward.y = 0f;
            right.y = 0f;
            forward.Normalize();
            right.Normalize();

            return (forward * input.y + right * input.x).normalized;
        }

        private void ApplyGravity(bool jumpPressed)
        {
            if (characterController.isGrounded && currentVelocityY < 0f)
            {
                currentVelocityY = -2f;
            }
            
            if (characterController.isGrounded && jumpPressed)
            {
                currentVelocityY = Mathf.Sqrt(jumpHeight * -2f * gravity);
                // Also trigger jump animation if there is one
                if (animator != null) {
                    // Si tienes una animacion de salto
                    // animator.SetTrigger("Jump");
                }
            }

            currentVelocityY += gravity * Time.deltaTime;
        }

        private void UpdateAnimator(float inputMagnitude, bool running)
        {
            if (animator == null) return;

            bool moving = inputMagnitude > 0.1f;
            float speedValue = running ? (inputMagnitude * 2f) : inputMagnitude;
            animator.TrySetFloat("Speed", speedValue);
            animator.TrySetBool("IsRunning", running && moving);
            animator.TrySetBool("IsGrounded", characterController.isGrounded);

            // Kit "3D Stylized Animated Dogs": usa un entero AnimationID
            int animationId = !moving ? DogAnimationIds.Idle : (running ? DogAnimationIds.Run : DogAnimationIds.Walk);
            animator.TrySetInteger(DogAnimationIds.ParameterName, animationId);
        }
    }
}
