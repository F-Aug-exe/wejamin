using UnityEngine;
using UnityEngine.Events;
using Replica.AI;
using Replica.Dialogue;
using Replica.Quests;

namespace Replica.Interaction
{
    public enum NPCRole
    {
        Owner,   // Dueño que pide ayuda y recibe la mascota
        LostPet  // Mascota perdida que acompaña al jugador al ser encontrada
    }

    /// <summary>
    /// Componente de interacción para NPCs (Humanos dueños y Mascotas perdidas).
    /// Arrastra este script a cada NPC de la escena y asigna sus datos de misión en el Inspector.
    /// </summary>
    public class InteractableNPC : MonoBehaviour, IInteractable
    {
        [Header("Rol y Configuración de Misión")]
        [Tooltip("Rol del NPC en la misión")]
        [SerializeField] private NPCRole role = NPCRole.Owner;
        [Tooltip("Identificador de la misión asociada (ej. Q1, Q2, Q3, Q4, Q5)")]
        [SerializeField] private string questId = "Q1";
        [Tooltip("ScriptableObject de la misión (opcional, si se asigna toma los diálogos de aquí)")]
        [SerializeField] private QuestData questData;

        [Header("Textos de Interacción")]
        [Tooltip("Nombre a mostrar en el prompt")]
        [SerializeField] private string npcDisplayName = "NPC";

        [Header("Referencias de Acompañante")]
        [Tooltip("Componente CompanionAI de la mascota")]
        [SerializeField] private CompanionAI companionAI;
        [Tooltip("Punto de entrega en el dueño donde se posicionará la mascota")]
        [SerializeField] private Transform deliveryDestinationPoint;

        [Header("Audios y Eventos")]
        [Tooltip("AudioSource para reproducir voces o efectos")]
        [SerializeField] private AudioSource audioSource;
        [Tooltip("Evento disparado al interactuar")]
        public UnityEvent onInteract;
        [Tooltip("Evento disparado al reclutar o entregar la mascota")]
        public UnityEvent onActionSuccess;

        private void Awake()
        {
            if (audioSource == null)
                audioSource = GetComponent<AudioSource>();
            if (companionAI == null && role == NPCRole.LostPet)
                companionAI = GetComponent<CompanionAI>();
        }

        public string GetInteractionPrompt()
        {
            QuestState state = GetCurrentQuestState();

            if (role == NPCRole.Owner)
            {
                if (state == QuestState.NotStarted)
                    return $"[E] Hablar con {npcDisplayName}";
                if (state == QuestState.CompanionRecruited)
                    return $"[E] Entregar mascota a {npcDisplayName}";
                if (state == QuestState.InProgress)
                    return $"[E] Hablar con {npcDisplayName}";
                return $"[E] Conversar con {npcDisplayName}";
            }
            else // LostPet
            {
                if (state == QuestState.Completed)
                    return string.Empty;
                if (state == QuestState.CompanionRecruited)
                    return $"[E] Acariciar a {npcDisplayName}";
                return $"[E] Ayudar a {npcDisplayName}";
            }
        }

        public bool CanInteract()
        {
            QuestState state = GetCurrentQuestState();

            if (role == NPCRole.LostPet && state == QuestState.Completed)
                return false;

            return DialogueSystem.Instance != null && !DialogueSystem.Instance.IsInDialogue;
        }

        public void Interact(GameObject interactor)
        {
            onInteract?.Invoke();
            QuestState state = GetCurrentQuestState();
            QuestData data = GetResolvedQuestData();

            if (role == NPCRole.Owner)
            {
                HandleOwnerInteraction(state, data);
            }
            else
            {
                HandlePetInteraction(interactor, state, data);
            }
        }

        private void HandleOwnerInteraction(QuestState state, QuestData data)
        {
            if (state == QuestState.CompanionRecruited)
            {
                // Entregar mascota
                string speaker = data != null && !string.IsNullOrEmpty(data.ownerDeliveredDialogue.speakerName)
                    ? data.ownerDeliveredDialogue.speakerName
                    : npcDisplayName;

                string[] lines = data != null && data.ownerDeliveredDialogue.lines != null && data.ownerDeliveredDialogue.lines.Length > 0
                    ? data.ownerDeliveredDialogue.lines
                    : new string[] { "¡Muchas gracias por traer a mi mascota de vuelta!" };

                PlayAudio(data?.ownerDeliveredDialogue?.voiceOrSfx);

                DialogueSystem.Instance.StartDialogue(speaker, lines, () =>
                {
                    if (companionAI != null)
                    {
                        companionAI.Deliver(deliveryDestinationPoint != null ? deliveryDestinationPoint : transform);
                    }
                    QuestManager.Instance.SetQuestState(questId, QuestState.Completed);
                    onActionSuccess?.Invoke();
                });
            }
            else if (state == QuestState.NotStarted || state == QuestState.InProgress)
            {
                // Pedir ayuda
                string speaker = data != null && !string.IsNullOrEmpty(data.ownerInitialDialogue.speakerName)
                    ? data.ownerInitialDialogue.speakerName
                    : npcDisplayName;

                string[] lines = data != null && data.ownerInitialDialogue.lines != null && data.ownerInitialDialogue.lines.Length > 0
                    ? data.ownerInitialDialogue.lines
                    : new string[] { "Por favor, ayúdame a encontrar a mi compañero perdido." };

                PlayAudio(data?.ownerInitialDialogue?.voiceOrSfx);

                DialogueSystem.Instance.StartDialogue(speaker, lines, () =>
                {
                    if (state == QuestState.NotStarted)
                    {
                        QuestManager.Instance.SetQuestState(questId, QuestState.InProgress);
                    }
                });
            }
            else
            {
                // Diálogo después de completada la misión
                DialogueSystem.Instance.StartDialogue(npcDisplayName, new string[] { "Gracias de nuevo por todo, perrito." });
            }
        }

        private void HandlePetInteraction(GameObject interactor, QuestState state, QuestData data)
        {
            if (state == QuestState.InProgress || state == QuestState.NotStarted)
            {
                string speaker = data != null && !string.IsNullOrEmpty(data.petFoundDialogue.speakerName)
                    ? data.petFoundDialogue.speakerName
                    : npcDisplayName;

                string[] lines = data != null && data.petFoundDialogue.lines != null && data.petFoundDialogue.lines.Length > 0
                    ? data.petFoundDialogue.lines
                    : new string[] { "¡Hola! ¿Me puedes guiar de vuelta?" };

                PlayAudio(data?.petFoundDialogue?.voiceOrSfx);

                DialogueSystem.Instance.StartDialogue(speaker, lines, () =>
                {
                    if (companionAI != null)
                    {
                        companionAI.StartFollowing(interactor.transform);
                    }
                    QuestManager.Instance.SetQuestState(questId, QuestState.CompanionRecruited);
                    onActionSuccess?.Invoke();
                });
            }
            else if (state == QuestState.CompanionRecruited)
            {
                DialogueSystem.Instance.StartDialogue(npcDisplayName, new string[] { "Te sigo de cerca." });
            }
        }

        private void PlayAudio(AudioClip clip)
        {
            if (audioSource != null && clip != null)
            {
                audioSource.PlayOneShot(clip);
            }
        }

        private QuestState GetCurrentQuestState()
        {
            return QuestManager.Instance != null 
                ? QuestManager.Instance.GetQuestState(questId) 
                : QuestState.NotStarted;
        }

        private QuestData GetResolvedQuestData()
        {
            if (questData != null) return questData;
            return QuestManager.Instance != null ? QuestManager.Instance.GetQuestData(questId) : null;
        }
    }
}
