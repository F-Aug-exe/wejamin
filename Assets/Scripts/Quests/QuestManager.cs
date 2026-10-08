using System;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.Events;
using Replica.Dialogue;

namespace Replica.Quests
{
    public class QuestManager : MonoBehaviour
    {
        public static QuestManager Instance { get; private set; }

        [Header("Configuracion de Misiones")]
        [Tooltip("Lista de ScriptableObjects con las 5 misiones principales (Q1 a Q5)")]
        [SerializeField] private List<QuestData> quests = new List<QuestData>();

        [Header("Cinematica / Secuencia Final")]
        [Tooltip("Referencia al controlador de la secuencia final (Q6)")]
        [SerializeField] private FinalSequenceController finalSequenceController;

        [Header("Introduccion (Q0.1)")]
        [Tooltip("Reproducir la narracion inicial al iniciar la escena?")]
        [SerializeField] private bool playIntroOnStart = true;
        [Tooltip("Dialogo del prologo")]
        [SerializeField] private DialogueData introDialogue = new DialogueData
        {
            speakerName = "Narrador",
            lines = new DialogueLine[] { new DialogueLine { text = "Un da un perrito paseaba con su humano, cuando fueron interrumpidos por un temblor..." } }
        };

        [Header("Eventos")]
        [Tooltip("Disparado cada vez que se completa una mision (parametro: numero de misiones completadas)")]
        public UnityEvent<int> onQuestCompletedCountChanged;
        [Tooltip("Disparado cuando se han completado las 5 misiones principales")]
        public UnityEvent onAllQuestsCompleted;

        private Dictionary<string, QuestState> questStates = new Dictionary<string, QuestState>();
        private int completedQuestsCount = 0;
        public int CompletedQuestsCount => completedQuestsCount;
        public int TotalQuestsCount => quests.Count;

        private void Awake()
        {
            UnityEngine.Debug.Log("QuestManager Awake EJECUTADO! Instance = this");
            Instance = this;

            foreach (var q in quests)
            {
                if (q != null)
                {
                    questStates[q.questId] = QuestState.NotStarted;
                }
            }
        }

        private void Start()
        {
            if (playIntroOnStart)
            {
                // Un pequeno delay para que todo cargue antes del dialogo
                Invoke(nameof(PlayIntro), 1f);
            }
        }

        private void PlayIntro()
        {
            if (DialogueSystem.Instance != null && introDialogue != null && introDialogue.lines.Length > 0)
            {
                DialogueSystem.Instance.StartDialogue(introDialogue.speakerName, introDialogue.lines);
            }
        }

        public QuestState GetQuestState(string questId)
        {
            if (questStates.TryGetValue(questId, out QuestState state))
                return state;
            return QuestState.NotStarted;
        }

        public QuestData GetQuestData(string questId)
        {
            return quests.Find(q => q != null && q.questId == questId);
        }

        public void SetQuestState(string questId, QuestState newState)
        {
            if (questStates.ContainsKey(questId))
            {
                QuestState oldState = questStates[questId];
                questStates[questId] = newState;
                Debug.Log($"Mision {questId} actualiz a estado: {newState}");

                var data = GetQuestData(questId);

                if (newState == QuestState.InProgress && oldState == QuestState.NotStarted)
                {
                    data?.onQuestStarted?.Invoke();
                }
                else if (newState == QuestState.CompanionRecruited && oldState != QuestState.CompanionRecruited)
                {
                    data?.onCompanionRecruited?.Invoke();
                }
                else if (newState == QuestState.Completed && oldState != QuestState.Completed)
                {
                    data?.onQuestCompleted?.Invoke();
                    completedQuestsCount++;
                    onQuestCompletedCountChanged?.Invoke(completedQuestsCount);

                    CheckAllQuestsCompleted();
                }
            }
        }

        private void CheckAllQuestsCompleted()
        {
            int total = quests.Count;
            if (total > 0 && completedQuestsCount >= total)
            {
                Debug.Log("Todas las misiones principales completadas!");
                onAllQuestsCompleted?.Invoke();
                
                if (finalSequenceController != null)
                {
                    finalSequenceController.TriggerFinalSequence();
                }
            }
        }
    }
}


