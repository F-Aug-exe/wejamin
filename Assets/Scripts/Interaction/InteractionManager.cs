using System.Collections.Generic;
using UnityEngine;
using UnityEngine.Events;
#if ENABLE_INPUT_SYSTEM
using UnityEngine.InputSystem;
#endif

namespace Replica.Interaction
{
    /// <summary>
    /// Manejador de interacciones del jugador.
    /// Arrastra este componente al GameObject del Jugador (junto a un SphereCollider en modo 'Is Trigger').
    /// </summary>
    public class InteractionManager : MonoBehaviour
    {
        [Header("Detección")]
        [Tooltip("Radio de interacción si no se utiliza un Trigger Collider")]
        [SerializeField] private float detectionRadius = 2.5f;
        [Tooltip("Capa de los objetos interactuables")]
        [SerializeField] private LayerMask interactableLayer = ~0;

        [Header("Eventos de UI")]
        [Tooltip("Evento disparado cuando cambia el prompt en pantalla (string: mensaje a mostrar o vacío)")]
        public UnityEvent<string> onPromptChanged;

        private readonly List<IInteractable> nearbyInteractables = new List<IInteractable>();
        private IInteractable currentInteractable;
        private float lastInteractTime;

        private void Update()
        {
            UpdateCurrentInteractable();
            CheckInteractionInput();
        }

        private void UpdateCurrentInteractable()
        {
            // Limpiar nulos o destruidos
            nearbyInteractables.RemoveAll(item => item == null || (item is MonoBehaviour mb && mb == null));

            IInteractable closest = null;
            float minDistance = float.MaxValue;

            // Prioridad a los que están dentro del trigger
            foreach (var interactable in nearbyInteractables)
            {
                if (interactable.CanInteract() && interactable is MonoBehaviour mb)
                {
                    float dist = Vector3.Distance(transform.position, mb.transform.position);
                    if (dist < minDistance)
                    {
                        minDistance = dist;
                        closest = interactable;
                    }
                }
            }

            // Si no hay en trigger, buscar por OverlapSphere
            if (closest == null)
            {
                Collider[] hits = Physics.OverlapSphere(transform.position, detectionRadius, interactableLayer);
                foreach (var hit in hits)
                {
                    if (hit.TryGetComponent<IInteractable>(out var interactable) && interactable.CanInteract())
                    {
                        float dist = Vector3.Distance(transform.position, hit.transform.position);
                        if (dist < minDistance)
                        {
                            minDistance = dist;
                            closest = interactable;
                        }
                    }
                }
            }

            if (closest != currentInteractable)
            {
                currentInteractable = closest;
                string prompt = currentInteractable != null ? currentInteractable.GetInteractionPrompt() : string.Empty;
                onPromptChanged?.Invoke(prompt);
            }
        }

        private void CheckInteractionInput()
        {
            if (currentInteractable == null) return;

            bool interactPressed = false;
#if ENABLE_INPUT_SYSTEM
            if (Keyboard.current != null && Keyboard.current.eKey.wasPressedThisFrame)
                interactPressed = true;
            else if (Gamepad.current != null && Gamepad.current.buttonWest.wasPressedThisFrame)
                interactPressed = true;
#endif
            if (!interactPressed && Input.GetKeyDown(KeyCode.E))
            {
                interactPressed = true;
            }

            if (interactPressed && currentInteractable.CanInteract() && Time.time - lastInteractTime > 0.2f && (Replica.Dialogue.DialogueSystem.Instance == null || !Replica.Dialogue.DialogueSystem.Instance.IsInDialogue))
            {
                currentInteractable.Interact(gameObject);
                lastInteractTime = Time.time;
            }
        }

        private void OnTriggerEnter(Collider other)
        {
            if (other.TryGetComponent<IInteractable>(out var interactable))
            {
                if (!nearbyInteractables.Contains(interactable))
                    nearbyInteractables.Add(interactable);
            }
        }

        private void OnTriggerExit(Collider other)
        {
            if (other.TryGetComponent<IInteractable>(out var interactable))
            {
                nearbyInteractables.Remove(interactable);
            }
        }

        private void OnDrawGizmosSelected()
        {
            Gizmos.color = Color.yellow;
            Gizmos.DrawWireSphere(transform.position, detectionRadius);
        }
    }
}
