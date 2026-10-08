using System;
using UnityEngine;
using UnityEngine.Events;

namespace Replica.Quests
{
    public enum QuestState
    {
        NotStarted,
        InProgress,       // Dueno contactado, buscando mascota
        CompanionRecruited,// Mascota encontrada, siguiendo al jugador
        Completed          // Mascota entregada con exito
    }

    [System.Serializable]
    public class DialogueLine
    {
        [TextArea(2, 5)]
        public string text;
        [Tooltip("Sonido opcional que se reproduce al mostrar esta lnea")]
        public AudioClip voiceOrSfx;
    }

    [System.Serializable]
    public class DialogueData
    {
        public string speakerName;
        public DialogueLine[] lines;
    }

    /// <summary>
    /// Configuracion de una mision de rescate (ScriptableObject).
    /// Permite configurar dialogos, textos y eventos de audio desde el Inspector de Unity.
    /// </summary>
    [CreateAssetMenu(fileName = "NewQuestData", menuName = "Replica/Quest Data", order = 1)]
    public class QuestData : ScriptableObject
    {
        [Header("Informacion Basica")]
        public string questId = "Q1";
        public string questTitle = "Madre y Cachorro";

        [Header("Dialogo 1: Dueno pidiendo ayuda")]
        public DialogueData ownerInitialDialogue;

        [Header("Dialogo 2: Mascota encontrada")]
        public DialogueData petFoundDialogue;

        [Header("Dialogo 3: Entrega al dueno")]
        public DialogueData ownerDeliveredDialogue;
        public DialogueData petDeliveredDialogue;

        [Header("Eventos de Audio / Cinematicas")]
        public UnityEvent onQuestStarted;
        public UnityEvent onCompanionRecruited;
        public UnityEvent onQuestCompleted;
    }
}
