using UnityEngine;
using UnityEngine.AI;

namespace Replica.AI
{
    /// <summary>
    /// Controlador de Inteligencia Artificial para los acompañantes rescatados.
    /// Soporta seguimiento por NavMesh (Cachorro, Cachetoncito, Perro Guía), 
    /// montura en el lomo (Tortuga) o vuelo orbital sobre el jugador (Cuervo).
    /// Arrastra este componente a cada prefab o GameObject de NPC rescatable.
    /// </summary>
    [RequireComponent(typeof(NavMeshAgent))]
    public class CompanionAI : MonoBehaviour
    {
        public enum CompanionType
        {
            GroundNavMesh, // Cachorro, Cachetoncito, Perro Guía
            Mounted,       // Tortuga (en el lomo del perro)
            Flying         // Cuervo (volando/flotando sobre el perro)
        }

        [Header("Tipo de Acompañante")]
        [Tooltip("Selecciona el modo de movimiento para este acompañante")]
        public CompanionType companionType = CompanionType.GroundNavMesh;

        [Header("Seguimiento Terrestre (NavMesh)")]
        [Tooltip("Distancia a la que empieza a seguir al jugador")]
        [SerializeField] private float followDistance = 3.0f;
        [Tooltip("Distancia de parada respecto al jugador")]
        [SerializeField] private float stoppingDistance = 1.8f;
        [Tooltip("Velocidad de movimiento del acompañante")]
        [SerializeField] private float moveSpeed = 4.0f;

        [Header("Acompañante Montado / Volador")]
        [Tooltip("Offset relativo al jugador cuando está montado o volando")]
        [SerializeField] private Vector3 mountedOffset = new Vector3(0f, 0.45f, -0.2f);
        [Tooltip("Offset para vuelo sobre la cabeza")]
        [SerializeField] private Vector3 flyingOffset = new Vector3(0.5f, 1.8f, 0.3f);
        [Tooltip("Suavizado de posición para acompañantes voladores/montados")]
        [SerializeField] private float followSmoothing = 8.0f;

        [Header("Referencias Opcionales")]
        [Tooltip("Animator del acompañante")]
        [SerializeField] private Animator animator;

        private NavMeshAgent agent;
        private Transform playerTarget;
        private bool isFollowing = false;
        private bool isDelivered = false;

        public bool IsFollowing => isFollowing;
        public bool IsDelivered => isDelivered;

        private void Awake()
        {
            agent = GetComponent<NavMeshAgent>();
            if (animator == null)
                animator = GetComponentInChildren<Animator>();

            ConfigureAgent();
        }

        private void ConfigureAgent()
        {
            if (agent != null)
            {
                agent.speed = moveSpeed;
                agent.stoppingDistance = stoppingDistance;
                agent.acceleration = 12f;
                agent.angularSpeed = 360f;
            }
        }

        private void Start()
        {
            if (playerTarget == null)
            {
                var player = FindFirstObjectByType<Replica.Player.PlayerController>();
                if (player != null)
                    playerTarget = player.transform;
            }
        }

        private void Update()
        {
            if (isDelivered || !isFollowing || playerTarget == null)
            {
                UpdateIdleAnimator();
                return;
            }

            switch (companionType)
            {
                case CompanionType.GroundNavMesh:
                    UpdateNavMeshFollow();
                    break;

                case CompanionType.Mounted:
                    UpdateMountedFollow();
                    break;

                case CompanionType.Flying:
                    UpdateFlyingFollow();
                    break;
            }
        }

        /// <summary>
        /// Comienza a seguir al jugador como escolta.
        /// </summary>
        public void StartFollowing(Transform target = null)
        {
            if (target != null)
                playerTarget = target;
            else if (playerTarget == null)
            {
                var player = FindFirstObjectByType<Replica.Player.PlayerController>();
                if (player != null)
                    playerTarget = player.transform;
            }

            isFollowing = true;
            isDelivered = false;

            if (companionType != CompanionType.GroundNavMesh)
            {
                if (agent != null && agent.enabled)
                    agent.enabled = false;
            }
            else
            {
                if (agent != null && !agent.enabled)
                    agent.enabled = true;
            }
        }

        /// <summary>
        /// Entrega el acompañante a su dueño final.
        /// </summary>
        public void Deliver(Transform ownerDestination = null)
        {
            isFollowing = false;
            isDelivered = true;

            if (companionType != CompanionType.GroundNavMesh)
            {
                if (ownerDestination != null)
                {
                    transform.position = ownerDestination.position;
                    transform.rotation = ownerDestination.rotation;
                }
            }
            else
            {
                if (agent != null && agent.enabled)
                {
                    if (ownerDestination != null)
                    {
                        agent.SetDestination(ownerDestination.position);
                    }
                    else
                    {
                        agent.ResetPath();
                    }
                }
            }

            UpdateIdleAnimator();
        }

        private void UpdateNavMeshFollow()
        {
            if (agent == null || !agent.enabled || !agent.isOnNavMesh) return;

            float distanceToPlayer = Vector3.Distance(transform.position, playerTarget.position);

            if (distanceToPlayer > followDistance)
            {
                agent.SetDestination(playerTarget.position);
            }
            else if (distanceToPlayer <= stoppingDistance + 0.2f)
            {
                if (agent.hasPath)
                    agent.ResetPath();
            }

            float currentSpeed = agent.velocity.magnitude;
            if (animator != null)
            {
                animator.SetFloat("Speed", currentSpeed);
                animator.SetBool("IsMoving", currentSpeed > 0.1f);
            }
        }

        private void UpdateMountedFollow()
        {
            var playerCtrl = playerTarget.GetComponent<Replica.Player.PlayerController>();
            Transform mountPoint = playerCtrl != null && playerCtrl.mountPointBack != null 
                ? playerCtrl.mountPointBack 
                : playerTarget;

            Vector3 targetPosition = mountPoint.TransformPoint(mountedOffset);
            transform.position = Vector3.Lerp(transform.position, targetPosition, Time.deltaTime * followSmoothing);
            transform.rotation = Quaternion.Slerp(transform.rotation, mountPoint.rotation, Time.deltaTime * followSmoothing);

            if (animator != null)
            {
                animator.SetBool("IsMounted", true);
            }
        }

        private void UpdateFlyingFollow()
        {
            Vector3 targetPosition = playerTarget.TransformPoint(flyingOffset);
            // Efecto suave de flotación vertical
            targetPosition.y += Mathf.Sin(Time.time * 2.5f) * 0.15f;

            transform.position = Vector3.Lerp(transform.position, targetPosition, Time.deltaTime * followSmoothing);
            
            Vector3 lookDirection = playerTarget.forward;
            if (lookDirection != Vector3.zero)
            {
                Quaternion targetRot = Quaternion.LookRotation(lookDirection);
                transform.rotation = Quaternion.Slerp(transform.rotation, targetRot, Time.deltaTime * followSmoothing);
            }

            if (animator != null)
            {
                animator.SetBool("IsFlying", true);
            }
        }

        private void UpdateIdleAnimator()
        {
            if (animator != null)
            {
                animator.SetFloat("Speed", 0f);
                animator.SetBool("IsMoving", false);
            }
        }
    }
}
