using System.Collections;
using UnityEngine;
using UnityEngine.Events;
using Replica.Dialogue;
using Replica.AI;
using Replica.Interaction;
using Replica.Player;
using Replica.UI;

namespace Replica.Quests
{
    public class FinalSequenceController : MonoBehaviour
    {
        [Header("Referencias de Personajes y Objetivos")]
        [SerializeField] private CompanionAI crowCompanion;
        [SerializeField] private GameObject falseOwnerTarget;
        [SerializeField] private GameObject realOwnerTarget;

        [Header("Camaras Cinemáticas")]
        [SerializeField] private Camera gameplayCamera;
        [Tooltip("Cámara 1: Enfoca a Toby junto a su dueño")]
        [SerializeField] private Camera cameraTobyWithOwner;
        [Tooltip("Cámara 2: Enfoca al cuervo marchándose volando")]
        [SerializeField] private Camera cameraCrowFlyingAway;
        [SerializeField] private GameObject endScreenCanvas;

        [Header("Dialogos Q6")]
        [SerializeField] private DialogueData q6f1CrowWarning = new DialogueData
        {
            speakerName = "Cuervo",
            lines = new DialogueLine[]
            {
                new DialogueLine { text = "¡Oye, perrito! Hay un humano preguntando por ti, sigueme" }
            }
        };

        [SerializeField] private DialogueData q6f3FalseOwner = new DialogueData
        {
            speakerName = "Humano Desconocido",
            lines = new DialogueLine[]
            {
                new DialogueLine { text = "¿Qué? Ese no es mi perro..." }
            }
        };

        [SerializeField] private DialogueData q6f4RealOwner = new DialogueData
        {
            speakerName = "Dueño de Toby",
            lines = new DialogueLine[]
            {
                new DialogueLine { text = "¡Toby! ¡Estás vivo! ¡Ven aquí, amigo mío!" }
            }
        };

        [SerializeField] private DialogueData q6f5CrowEndingPart1 = new DialogueData
        {
            speakerName = "Cuervo",
            lines = new DialogueLine[]
            {
                new DialogueLine { text = "Seremos compañeros en desgracia y haremos mas livianas nuestras cargas" }
            }
        };

        [SerializeField] private DialogueData q6f5CrowEndingPart2 = new DialogueData
        {
            speakerName = "Cuervo",
            lines = new DialogueLine[]
            {
                new DialogueLine { text = "Bendiciones y sigan resistiendo" }
            }
        };

        [Header("Eventos")]
        public UnityEvent onSequenceStarted;
        public UnityEvent onEmotionalClimax;
        public UnityEvent onSequenceEnded;

        private bool sequenceTriggered = false;
        private bool falseOwnerVisited = false;
        private bool realOwnerVisited = false;
        private Coroutine cameraFollowCoroutine;

        private void Start()
        {
            FindDefaultReferences();
        }

        private void FindDefaultReferences()
        {
            if (crowCompanion == null)
            {
                var crowObj = GameObject.Find("Cuervo");
                if (crowObj != null) crowCompanion = crowObj.GetComponent<CompanionAI>();
            }

            if (gameplayCamera == null)
            {
                gameplayCamera = Camera.main;
            }

            if (endScreenCanvas == null)
            {
                endScreenCanvas = GameObject.Find("EndScreenCanvas");
            }
        }

        /// <summary>
        /// Inicia Q6 tras completar las 5 misiones principales.
        /// El cuervo da el warning al jugador y se recluta como acompañante volador.
        /// </summary>
        public void TriggerFinalSequence()
        {
            if (sequenceTriggered) return;
            sequenceTriggered = true;
            StartCoroutine(StartQ6Routine());
        }

        private IEnumerator StartQ6Routine()
        {
            onSequenceStarted?.Invoke();

            // Reclutar y posicionar al cuervo si existe
            var player = Object.FindAnyObjectByType<PlayerController>();
            if (crowCompanion != null && player != null)
            {
                crowCompanion.StartFollowing(player.transform);
            }

            // Actualizar HUD con la nueva misión en curso
            UpdateHUDQuestText("Misión: Seguir al cuervo y buscar al humano sospechoso");

            // Diálogo de warning del cuervo
            bool warningDone = false;
            DialogueSystem.Instance.StartDialogue(q6f1CrowWarning.speakerName, q6f1CrowWarning.lines, () => warningDone = true);
            yield return new WaitUntil(() => warningDone);

            // Habilitar la interacción con el falso dueño (Punto A)
            if (falseOwnerTarget != null)
            {
                falseOwnerTarget.SetActive(true);
            }
            else
            {
                yield return new WaitForSeconds(1.0f);
                OnFalseOwnerInteracted();
            }
        }

        public void OnFalseOwnerInteracted()
        {
            if (falseOwnerVisited) return;
            falseOwnerVisited = true;
            StartCoroutine(FalseOwnerRoutine());
        }

        private IEnumerator FalseOwnerRoutine()
        {
            bool falseOwnerDone = false;
            DialogueSystem.Instance.StartDialogue(q6f3FalseOwner.speakerName, q6f3FalseOwner.lines, () => falseOwnerDone = true);
            yield return new WaitUntil(() => falseOwnerDone);

            UpdateHUDQuestText("Misión: Buscar al verdadero dueño de Toby");

            // Habilitar la interacción con el dueño real (Punto B)
            if (realOwnerTarget != null)
            {
                realOwnerTarget.SetActive(true);
            }
            else
            {
                yield return new WaitForSeconds(1.0f);
                OnRealOwnerInteracted();
            }
        }

        public void OnRealOwnerInteracted()
        {
            if (realOwnerVisited) return;
            realOwnerVisited = true;
            StartCoroutine(CinematicEndingRoutine());
        }

        private IEnumerator CinematicEndingRoutine()
        {
            onEmotionalClimax?.Invoke();

            // Bloquear control del jugador permanentemente para el final
            var player = Object.FindAnyObjectByType<PlayerController>();
            if (player != null) player.CanMove = false;
            if (DialogueSystem.Instance != null) DialogueSystem.Instance.KeepPlayerLocked = true;

            // Diálogo del dueño de Toby
            bool realOwnerDone = false;
            DialogueSystem.Instance.StartDialogue(q6f4RealOwner.speakerName, q6f4RealOwner.lines, () => realOwnerDone = true);
            yield return new WaitUntil(() => realOwnerDone);

            // Activar Cámara 1: Toby con su humano
            SwitchCamera(cameraTobyWithOwner);

            // Ending 1 del Cuervo (el jugador solo avanza con [E])
            bool ending1Done = false;
            DialogueSystem.Instance.StartDialogue(q6f5CrowEndingPart1.speakerName, q6f5CrowEndingPart1.lines, () => ending1Done = true);
            yield return new WaitUntil(() => ending1Done);

            // Activar Cámara 2: Cuervo marchándose volando
            SwitchCamera(cameraCrowFlyingAway);

            if (crowCompanion != null)
            {
                crowCompanion.Deliver(null);
                Transform crowT = crowCompanion.transform;
                StartCoroutine(FlyAwayRoutine(crowT));
                cameraFollowCoroutine = StartCoroutine(CameraTrackCrowRoutine(cameraCrowFlyingAway, crowT));
            }

            // Ending 2 del Cuervo (el jugador avanza con [E])
            bool ending2Done = false;
            DialogueSystem.Instance.StartDialogue(q6f5CrowEndingPart2.speakerName, q6f5CrowEndingPart2.lines, () => ending2Done = true);
            yield return new WaitUntil(() => ending2Done);

            yield return new WaitForSeconds(1.0f);

            if (cameraFollowCoroutine != null)
            {
                StopCoroutine(cameraFollowCoroutine);
            }

            // Pantalla final
            if (endScreenCanvas != null) endScreenCanvas.SetActive(true);
            onSequenceEnded?.Invoke();
        }

        private void SwitchCamera(Camera targetCam)
        {
            if (targetCam == null) return;

            if (gameplayCamera != null) gameplayCamera.gameObject.SetActive(false);
            if (cameraTobyWithOwner != null) cameraTobyWithOwner.gameObject.SetActive(false);
            if (cameraCrowFlyingAway != null) cameraCrowFlyingAway.gameObject.SetActive(false);

            targetCam.gameObject.SetActive(true);
        }

        private IEnumerator FlyAwayRoutine(Transform crowTransform)
        {
            float elapsed = 0f;
            Vector3 startPos = crowTransform.position;
            Vector3 endPos = startPos + new Vector3(20f, 30f, 40f);

            while (elapsed < 12f && crowTransform != null)
            {
                elapsed += Time.deltaTime;
                crowTransform.position = Vector3.Lerp(startPos, endPos, elapsed / 12f);
                yield return null;
            }
        }

        private IEnumerator CameraTrackCrowRoutine(Camera cam, Transform crowTransform)
        {
            if (cam == null || crowTransform == null) yield break;

            Vector3 relativeOffset = new Vector3(-2f, 1.5f, -5f);

            while (crowTransform != null && cam.gameObject.activeInHierarchy)
            {
                Vector3 targetCamPos = crowTransform.position + relativeOffset;
                cam.transform.position = Vector3.Lerp(cam.transform.position, targetCamPos, Time.deltaTime * 3.5f);
                cam.transform.LookAt(crowTransform.position + Vector3.up * 0.3f);
                yield return null;
            }
        }

        private void UpdateHUDQuestText(string text)
        {
            var hud = Object.FindAnyObjectByType<GameHUD>();
            if (hud != null)
            {
                hud.SetCustomQuestStatus(text);
            }
        }
    }
}
