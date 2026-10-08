using UnityEngine;
using UnityEngine.Events;
using Replica.AI;
using Replica.Dialogue;
using Replica.Quests;

namespace Replica.Interaction
{
    public enum NPCRole
    {
        Owner,
        LostPet
    }

    public class InteractableNPC : MonoBehaviour, IInteractable
    {
        [Header("Rol y Configuracion de Mision")]
        [Tooltip("Rol del NPC en la mision")]
        [SerializeField] private NPCRole role = NPCRole.Owner;
        [Tooltip("Identificador de la mision asociada (ej. Q1, Q2, Q3, Q4, Q5)")]
        [SerializeField] private string questId = "Q1";

        public NPCRole Role => role;
        public string QuestId => questId;

        [Header("Referencias")]
        [Tooltip("ScriptableObject de la mision (opcional, si se asigna toma los dialogos de aqu)")]
        [SerializeField] private QuestData questData;
        [Tooltip("Nombre a mostrar si no hay datos de mision")]
        [SerializeField] private string npcDisplayName = "Humano";

        [Header("Conexion y Entrega")]
        [Tooltip("Acompanante asociado a esta mision")]
        [SerializeField] private CompanionAI companionAI;
        [Tooltip("Punto donde se ubicara la mascota al ser entregada (solo util para el dueno)")]
        [SerializeField] private Transform deliveryDestinationPoint;

        [Header("Audio")]
        [SerializeField] private AudioSource audioSource;

        private void Start()
        {
            if (audioSource == null) audioSource = GetComponent<AudioSource>();
            if (audioSource == null) audioSource = gameObject.AddComponent<AudioSource>();
        }

        public string GetInteractionPrompt()
        {
            QuestState state = GetCurrentQuestState();

            if (role == NPCRole.Owner)
            {
                if (state == QuestState.CompanionRecruited) return "[E] Hablar (Entregar)";
                if (state == QuestState.NotStarted) return "[E] Hablar";
                return "";
            }
            else
            {
                if (state == QuestState.InProgress) return "[E] Hablar (Rescatar)";
                return "";
            }
        }

        public bool CanInteract()
        {
            QuestState state = GetCurrentQuestState();
            if (role == NPCRole.Owner)
                return state == QuestState.NotStarted || state == QuestState.CompanionRecruited || state == QuestState.Completed;
            else
                return state == QuestState.InProgress;
        }

        public void Interact(GameObject interactor)
        {
            if (!CanInteract()) return;

            // Orientar NPC hacia el jugador
            Vector3 look = (interactor.transform.position - transform.position).normalized;
            look.y = 0;
            if (look.sqrMagnitude > 0.01f)
            {
                transform.rotation = Quaternion.LookRotation(look);
            }

            if (role == NPCRole.Owner)
            {
                HandleOwnerInteraction();
            }
            else
            {
                HandlePetInteraction(interactor);
            }
        }

        private void HandleOwnerInteraction()
        {
            QuestState state = GetCurrentQuestState();
            QuestData data = questData != null ? questData : Object.FindAnyObjectByType<QuestManager>().GetQuestData(questId);

            if (state == QuestState.CompanionRecruited)
            {
                string speaker = data != null && !string.IsNullOrEmpty(data.ownerDeliveredDialogue.speakerName) ? data.ownerDeliveredDialogue.speakerName : npcDisplayName;
                DialogueLine[] lines = data != null && data.ownerDeliveredDialogue.lines != null && data.ownerDeliveredDialogue.lines.Length > 0 ? data.ownerDeliveredDialogue.lines : new DialogueLine[] { new DialogueLine { text = "Muchas gracias por traer a mi mascota de vuelta!" } };

                DialogueSystem.Instance.StartDialogue(speaker, lines, () =>
                {
                    if (companionAI != null)
                    {
                        companionAI.Deliver(deliveryDestinationPoint);
                    }
                    Object.FindAnyObjectByType<QuestManager>().SetQuestState(questId, QuestState.Completed);
                });
            }
            else if (state == QuestState.NotStarted)
            {
                string speaker = data != null && !string.IsNullOrEmpty(data.ownerInitialDialogue.speakerName) ? data.ownerInitialDialogue.speakerName : npcDisplayName;
                DialogueLine[] lines = data != null && data.ownerInitialDialogue.lines != null && data.ownerInitialDialogue.lines.Length > 0 ? data.ownerInitialDialogue.lines : new DialogueLine[] { new DialogueLine { text = "Por favor, ayudame a encontrar a mi companero perdido." } };

                string cachedQuestId = questId;
                DialogueSystem.Instance.StartDialogue(speaker, lines, () =>
                {
                    if (state == QuestState.NotStarted)
                    {
                        if (QuestManager.Instance == null) {
                            UnityEngine.Debug.LogError("QuestManager.Instance ES NULL EN EL CALLBACK!");
                            return;
                        }
                        Object.FindAnyObjectByType<QuestManager>().SetQuestState(cachedQuestId, QuestState.InProgress);
                    }
                });
            }
            else
            {
                DialogueSystem.Instance.StartDialogue(npcDisplayName, new DialogueLine[] { new DialogueLine { text = "Gracias de nuevo por todo, perrito." } });
            }
        }

        private void HandlePetInteraction(GameObject player)
        {
            QuestState state = GetCurrentQuestState();
            QuestData data = questData != null ? questData : Object.FindAnyObjectByType<QuestManager>().GetQuestData(questId);

            if (state == QuestState.InProgress)
            {
                string speaker = data != null && !string.IsNullOrEmpty(data.petFoundDialogue.speakerName) ? data.petFoundDialogue.speakerName : npcDisplayName;
                DialogueLine[] lines = data != null && data.petFoundDialogue.lines != null && data.petFoundDialogue.lines.Length > 0 ? data.petFoundDialogue.lines : new DialogueLine[] { new DialogueLine { text = "Hola! Me puedes guiar de vuelta?" } };

                DialogueSystem.Instance.StartDialogue(speaker, lines, () =>
                {
                    if (companionAI != null)
                    {
                        companionAI.StartFollowing(player.transform);
                    }
                    Object.FindAnyObjectByType<QuestManager>().SetQuestState(questId, QuestState.CompanionRecruited);
                });
            }
            else if (state == QuestState.CompanionRecruited)
            {
                DialogueSystem.Instance.StartDialogue(npcDisplayName, new DialogueLine[] { new DialogueLine { text = "Te sigo de cerca." } });
            }
        }

        private QuestState GetCurrentQuestState()
        {
            return QuestManager.Instance != null ? Object.FindAnyObjectByType<QuestManager>().GetQuestState(questId) : QuestState.NotStarted;
        }
    }
}


