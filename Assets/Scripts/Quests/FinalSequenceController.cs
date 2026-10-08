using System.Collections;
using UnityEngine;
using UnityEngine.Events;
using Replica.Dialogue;

namespace Replica.Quests
{
    /// <summary>
    /// Controlador de la Cinemática y Secuencia Final (Q6).
    /// Arrastra este script al GameObject 'FinalSequenceManager' en la escena y asigna las cámaras, audios y eventos en el Inspector.
    /// </summary>
    public class FinalSequenceController : MonoBehaviour
    {
        [Header("Referencias de Escena")]
        [Tooltip("Cámara principal del juego")]
        [SerializeField] private GameObject gameplayCamera;
        [Tooltip("Cámara cinemática para el final")]
        [SerializeField] private GameObject cinematicCamera;
        [Tooltip("Transform del dueño falso (Q6F3)")]
        [SerializeField] private Transform falseOwnerTransform;
        [Tooltip("Transform del dueño verdadero (Q6F4)")]
        [SerializeField] private Transform realOwnerTransform;
        [Tooltip("GameObject / Canvas de pantalla final o créditos")]
        [SerializeField] private GameObject endScreenCanvas;

        [Header("Diálogos del Final (Q6)")]
        [Tooltip("Q6F1: El cuervo avisa al perro")]
        [SerializeField] private DialogueData q6f1CrowWarning = new DialogueData
        {
            speakerName = "Cuervo",
            lines = new string[] { "¡Oye, perro! ¡Encontré a tu humano al final de la avenida! ¡Sígueme!" }
        };

        [Tooltip("Q6F3: Falsa réplica / encuentro equivocado")]
        [SerializeField] private DialogueData q6f3FalseOwner = new DialogueData
        {
            speakerName = "Humano Desconocido",
            lines = new string[] { "¿Qué? Ese no es mi perro... sigue buscando más adelante." }
        };

        [Tooltip("Q6F4: Encuentro con el verdadero dueño")]
        [SerializeField] private DialogueData q6f4RealOwner = new DialogueData
        {
            speakerName = "Dueño de Toby",
            lines = new string[] { "¡Toby! ¡Estás vivo! ¡Ven aquí, amigo mío!" }
        };

        [Tooltip("Q6F5: Narración final del Cuervo")]
        [SerializeField] private DialogueData q6f5CrowEnding = new DialogueData
        {
            speakerName = "Cuervo",
            lines = new string[] { "Y así, entre las ruinas de la ciudad, cada alma perdida encontró su camino de vuelta a casa." }
        };

        [Header("Eventos de Audio y Efectos")]
        [Tooltip("Disparado cuando inicia la secuencia final")]
        public UnityEvent onSequenceStarted;
        [Tooltip("Disparado en el clímax del encuentro con el dueño")]
        public UnityEvent onEmotionalClimax;
        [Tooltip("Disparado al terminar los créditos / fin del juego")]
        public UnityEvent onGameFinished;

        public void StartFinalSequence()
        {
            StartCoroutine(RunFinalSequenceRoutine());
        }

        private IEnumerator RunFinalSequenceRoutine()
        {
            onSequenceStarted?.Invoke();

            // Desactivar cámara de juego y activar cinemática si existe
            if (gameplayCamera != null && cinematicCamera != null)
            {
                gameplayCamera.SetActive(false);
                cinematicCamera.SetActive(true);
            }

            // Paso 1: Q6F1 - Aviso del cuervo
            bool step1Done = false;
            DialogueSystem.Instance.StartDialogue(q6f1CrowWarning.speakerName, q6f1CrowWarning.lines, () => step1Done = true);
            yield return new WaitUntil(() => step1Done);
            yield return new WaitForSeconds(1.0f);

            // Paso 2: Q6F3 - Réplica falsa
            bool step2Done = false;
            DialogueSystem.Instance.StartDialogue(q6f3FalseOwner.speakerName, q6f3FalseOwner.lines, () => step2Done = true);
            yield return new WaitUntil(() => step2Done);
            yield return new WaitForSeconds(1.0f);

            // Paso 3: Q6F4 - Salto congelado / clímax emotivo
            onEmotionalClimax?.Invoke();
            bool step3Done = false;
            DialogueSystem.Instance.StartDialogue(q6f4RealOwner.speakerName, q6f4RealOwner.lines, () => step3Done = true);
            yield return new WaitUntil(() => step3Done);
            yield return new WaitForSeconds(1.5f);

            // Paso 4: Q6F5 - Narración de cierre
            bool step4Done = false;
            DialogueSystem.Instance.StartDialogue(q6f5CrowEnding.speakerName, q6f5CrowEnding.lines, () => step4Done = true);
            yield return new WaitUntil(() => step4Done);

            // Fin del juego
            if (endScreenCanvas != null)
            {
                endScreenCanvas.SetActive(true);
            }
            onGameFinished?.Invoke();
        }
    }
}
