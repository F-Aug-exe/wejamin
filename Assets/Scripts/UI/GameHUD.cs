using UnityEngine;
using TMPro;
using Replica.Quests;
using Replica.Interaction;

namespace Replica.UI
{
    /// <summary>
    /// Administrador de la Interfaz de Usuario (HUD) del juego.
    /// Arrastra este componente al Canvas principal y asigna los textos de UI en el Inspector.
    /// </summary>
    public class GameHUD : MonoBehaviour
    {
        [Header("Elementos de UI (Inspector)")]
        [Tooltip("Texto que muestra el prompt de interacción activo (ej. '[E] Hablar...')")]
        [SerializeField] private TextMeshProUGUI interactionPromptText;
        [Tooltip("Texto que muestra el contador de misiones (ej. 'Rescates: 2 / 5')")]
        [SerializeField] private TextMeshProUGUI questCounterText;
        [Tooltip("Panel o contenedor del prompt de interacción")]
        [SerializeField] private GameObject interactionPromptPanel;

        private void Start()
        {
            var interactionManager = FindAnyObjectByType<InteractionManager>();
            if (interactionManager != null)
            {
                interactionManager.onPromptChanged.AddListener(SetInteractionPrompt);
            }

            if (Object.FindAnyObjectByType<QuestManager>() != null)
            {
                Object.FindAnyObjectByType<QuestManager>().onQuestCompletedCountChanged.AddListener(UpdateQuestCounter);
                UpdateQuestCounter(Object.FindAnyObjectByType<QuestManager>().CompletedQuestsCount);
            }
            else
            {
                UpdateQuestCounter(0);
            }

            SetInteractionPrompt(string.Empty);
        }

        public void SetInteractionPrompt(string message)
        {
            bool hasMessage = !string.IsNullOrEmpty(message);
            if (interactionPromptPanel != null)
                interactionPromptPanel.SetActive(hasMessage);

            if (interactionPromptText != null)
                interactionPromptText.text = message;
        }

        public void UpdateQuestCounter(int completed)
        {
            if (questCounterText != null)
            {
                int total = Object.FindAnyObjectByType<QuestManager>() != null ? Object.FindAnyObjectByType<QuestManager>().TotalQuestsCount : 5;
                questCounterText.text = $"Rescates: {completed} / {total}";
            }
        }

        public void SetCustomQuestStatus(string text)
        {
            if (questCounterText != null)
            {
                questCounterText.text = text;
            }
        }
    }
}

