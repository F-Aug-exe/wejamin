using System.Collections;
using UnityEngine;
using UnityEngine.Events;
using Replica.Dialogue;

namespace Replica.Quests
{
    public class FinalSequenceController : MonoBehaviour
    {
        [Header("Configuracion")]
        [Tooltip("Lugar donde debe ocurrir la cinematica (final de la avenida)")]
        [SerializeField] private Transform sequenceTriggerArea;

        [Header("Dialogos del Final (Q6)")]
        [Tooltip("Q6F1: El cuervo avisa al perro")]
        [SerializeField] private DialogueData q6f1CrowWarning = new DialogueData
        {
            speakerName = "Cuervo",
            lines = new DialogueLine[] { new DialogueLine { text = "Oye, perro! Encontre a tu humano al final de la avenida! Sigueme!" } }
        };

        [Tooltip("Q6F3: Falsa replica / encuentro equivocado")]
        [SerializeField] private DialogueData q6f3FalseOwner = new DialogueData
        {
            speakerName = "Humano Desconocido",
            lines = new DialogueLine[] { new DialogueLine { text = "Que? Ese no es mi perro... sigue buscando mas adelante." } }
        };

        [Tooltip("Q6F4: Encuentro con el verdadero dueno")]
        [SerializeField] private DialogueData q6f4RealOwner = new DialogueData
        {
            speakerName = "Dueno de Toby",
            lines = new DialogueLine[] { new DialogueLine { text = "Toby! Estas vivo! Ven aqu, amigo mo!" } }
        };

        [Tooltip("Q6F5: Narracion final del Cuervo")]
        [SerializeField] private DialogueData q6f5CrowEnding = new DialogueData
        {
            speakerName = "Cuervo",
            lines = new DialogueLine[] { new DialogueLine { text = "Y as, entre las ruinas de la ciudad, cada alma perdida encontro su camino de vuelta a casa." } }
        };

        [Header("Eventos")]
        public UnityEvent onSequenceStarted;
        public UnityEvent onEmotionalClimax;
        public UnityEvent onSequenceEnded;

        private bool sequenceTriggered = false;

        public void TriggerFinalSequence()
        {
            if (sequenceTriggered) return;
            sequenceTriggered = true;
            StartCoroutine(SequenceRoutine());
        }

        private IEnumerator SequenceRoutine()
        {
            onSequenceStarted?.Invoke();

            // Paso 1: Q6F1 - Aviso del cuervo
            bool step1Done = false;
            DialogueSystem.Instance.StartDialogue(q6f1CrowWarning.speakerName, q6f1CrowWarning.lines, () => step1Done = true);
            yield return new WaitUntil(() => step1Done);
            yield return new WaitForSeconds(1.0f);

            // Paso 2: Q6F3 - Replica falsa
            bool step2Done = false;
            DialogueSystem.Instance.StartDialogue(q6f3FalseOwner.speakerName, q6f3FalseOwner.lines, () => step2Done = true);
            yield return new WaitUntil(() => step2Done);
            yield return new WaitForSeconds(1.0f);

            // Paso 3: Q6F4 - Salto congelado / clmax emotivo
            onEmotionalClimax?.Invoke();
            bool step3Done = false;
            DialogueSystem.Instance.StartDialogue(q6f4RealOwner.speakerName, q6f4RealOwner.lines, () => step3Done = true);
            yield return new WaitUntil(() => step3Done);
            yield return new WaitForSeconds(1.5f);

            // Paso 4: Q6F5 - Narracion de cierre
            bool step4Done = false;
            DialogueSystem.Instance.StartDialogue(q6f5CrowEnding.speakerName, q6f5CrowEnding.lines, () => step4Done = true);
            yield return new WaitUntil(() => step4Done);

            // Fin del juego
            onSequenceEnded?.Invoke();
            Debug.Log("Juego Terminado: Secuencia final completada.");
        }
    }
}
