using System;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.Events;
using Replica.Dialogue;

namespace Replica.Quests
{
    /// <summary>
    /// Administrador Singleton del progreso de misiones y del flujo general del juego.
    /// Arrastra este componente al objeto 'GameManager' o 'QuestManager' en la escena.
    /// </summary>
    public class QuestManager : MonoBehaviour
    {
        public static QuestManager Instance { get; private set; }

        [Header("Configuración de Misiones")]
        [Tooltip("Lista de ScriptableObjects con las 5 misiones principales (Q1 a Q5)")]
        [SerializeField] private List<QuestData> quests = new List<QuestData>();

        [Header("Cinemática / Secuencia Final")]
        [Tooltip("Referencia al controlador de la secuencia final (Q6)")]
        [SerializeField] private FinalSequenceController finalSequenceController;

        [Header("Introducción (Q0.1)")]
        [Tooltip("¿Reproducir la narración inicial al iniciar la escena?")]
        [SerializeField] private bool playIntroOnStart = true;
        [Tooltip("Diálogo del prólogo")]
        [SerializeField] private DialogueData introDialogue = new DialogueData
        {
            speakerName = "Narrador",
            lines = new string[] { "Un día un perrito paseaba con su humano, cuando fueron interrumpidos por un temblor..." }
        };

        [Header("Eventos")]
        [Tooltip("Disparado cada vez que se completa una misión (parámetro: número de misiones completadas)")]
        public UnityEvent<int> onQuestCompletedCountChanged;
        [Tooltip("Disparado cuando se han completado las 5 misiones principales")]
        public UnityEvent onAllQuestsCompleted;

        private readonly Dictionary<string, QuestState> questStates = new Dictionary<string, QuestState>();
        private int completedCount = 0;

        public int CompletedQuestsCount => completedCount;
        public int TotalQuestsCount => quests.Count > 0 ? quests.Count : 5;

        private void Awake()
        {
            if (Instance != null && Instance != this)
            {
                Destroy(gameObject);
                return;
            }
            Instance = this;

            InitializeDefaultQuests();
        }

        private void Start()
        {
            if (playIntroOnStart && DialogueSystem.Instance != null && introDialogue.lines != null && introDialogue.lines.Length > 0)
            {
                DialogueSystem.Instance.StartDialogue(introDialogue.speakerName, introDialogue.lines);
            }
        }

        private void InitializeDefaultQuests()
        {
            // Inicializar estados de Q1 a Q5
            for (int i = 1; i <= 5; i++)
            {
                string id = "Q" + i;
                if (!questStates.ContainsKey(id))
                {
                    questStates[id] = QuestState.NotStarted;
                }
            }

            foreach (var q in quests)
            {
                if (q != null && !questStates.ContainsKey(q.questId))
                {
                    questStates[q.questId] = QuestState.NotStarted;
                }
            }
        }

        public QuestState GetQuestState(string questId)
        {
            return questStates.TryGetValue(questId, out var state) ? state : QuestState.NotStarted;
        }

        public void SetQuestState(string questId, QuestState state)
        {
            questStates[questId] = state;

            var questData = GetQuestData(questId);
            if (questData != null)
            {
                switch (state)
                {
                    case QuestState.InProgress:
                        questData.onQuestStarted?.Invoke();
                        break;
                    case QuestState.CompanionRecruited:
                        questData.onCompanionRecruited?.Invoke();
                        break;
                    case QuestState.Completed:
                        questData.onQuestCompleted?.Invoke();
                        break;
                }
            }

            if (state == QuestState.Completed)
            {
                RecalculateCompletedQuests();
            }
        }

        public QuestData GetQuestData(string questId)
        {
            return quests.Find(q => q != null && q.questId.Equals(questId, StringComparison.OrdinalIgnoreCase));
        }

        private void RecalculateCompletedQuests()
        {
            int count = 0;
            foreach (var kvp in questStates)
            {
                if (kvp.Value == QuestState.Completed)
                    count++;
            }
            completedCount = count;
            onQuestCompletedCountChanged?.Invoke(completedCount);

            if (completedCount >= 5)
            {
                onAllQuestsCompleted?.Invoke();
                TriggerFinalSequence();
            }
        }

        /// <summary>
        /// Activa la secuencia final Q6
        /// </summary>
        public void TriggerFinalSequence()
        {
            if (finalSequenceController != null)
            {
                finalSequenceController.StartFinalSequence();
            }
            else
            {
                Debug.Log("[QuestManager] ¡Las 5 misiones han sido completadas! Asigna un FinalSequenceController para reproducir la cinemática final.");
            }
        }
    }
}
