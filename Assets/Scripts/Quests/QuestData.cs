using System;
using UnityEngine;
using UnityEngine.Events;

namespace Replica.Quests
{
    public enum QuestState
    {
        NotStarted,
        InProgress,       // Dueño contactado, buscando mascota
        CompanionRecruited,// Mascota encontrada, siguiendo al jugador
        Completed          // Mascota entregada con éxito
    }

    [System.Serializable]
    public class DialogueData
    {
        public string speakerName;
        [TextArea(2, 5)]
        public string[] lines;
        public AudioClip voiceOrSfx;
    }

    /// <summary>
    /// Configuración de una misión de rescate (ScriptableObject).
    /// Permite configurar diálogos, textos y eventos de audio desde el Inspector de Unity.
    /// </summary>
    [CreateAssetMenu(fileName = "NewQuestData", menuName = "Replica/Quest Data", order = 1)]
    public class QuestData : ScriptableObject
    {
        [Header("Información Básica")]
        public string questId = "Q1";
        public string questTitle = "Madre y Cachorro";

        [Header("Diálogo 1: Dueño pidiendo ayuda")]
        public DialogueData ownerInitialDialogue;

        [Header("Diálogo 2: Mascota encontrada")]
        public DialogueData petFoundDialogue;

        [Header("Diálogo 3: Entrega al dueño")]
        public DialogueData ownerDeliveredDialogue;
        public DialogueData petDeliveredDialogue;

        [Header("Eventos de Audio / Cinemáticas")]
        public UnityEvent onQuestStarted;
        public UnityEvent onCompanionRecruited;
        public UnityEvent onQuestCompleted;
    }
}
