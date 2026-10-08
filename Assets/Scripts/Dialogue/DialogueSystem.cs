using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.Events;
using TMPro;
#if ENABLE_INPUT_SYSTEM
using UnityEngine.InputSystem;
#endif

namespace Replica.Dialogue
{
    /// <summary>
    /// Sistema de Diálogo Singleton.
    /// Arrastra este componente a un GameObject en la jerarquía (ej. 'DialogueManager') y conecta los elementos de UI Canvas en el Inspector.
    /// </summary>
    public class DialogueSystem : MonoBehaviour
    {
        public static DialogueSystem Instance { get; private set; }

        [Header("Referencias de UI (Inspector)")]
        [Tooltip("Panel contenedor del cuadro de diálogo (se activará/desactivará)")]
        [SerializeField] private GameObject dialoguePanel;
        [Tooltip("Texto para el nombre del personaje que habla")]
        [SerializeField] private TextMeshProUGUI speakerNameText;
        [Tooltip("Texto principal del diálogo")]
        [SerializeField] private TextMeshProUGUI dialogueContentText;
        [Tooltip("Indicador visual para continuar (ej. flecha parpadeante o texto '[E] Continuar')")]
        [SerializeField] private GameObject continueIndicator;

        [Header("Efecto de Escritura")]
        [Tooltip("Velocidad de aparición de letras (segundos por caracter). 0 para instantáneo.")]
        [SerializeField] private float typingSpeed = 0.02f;

        [Header("Eventos Globales")]
        [Tooltip("Evento disparado al comenzar cualquier diálogo")]
        public UnityEvent onDialogueStarted;
        [Tooltip("Evento disparado al finalizar el diálogo activo")]
        public UnityEvent onDialogueEnded;

        private readonly Queue<string> currentLines = new Queue<string>();
        private string currentFullLine = "";
        private Coroutine typingCoroutine;
        private bool isTyping = false;
        private UnityAction onCompleteCallback;
        private Replica.Player.PlayerController cachedPlayerController;

        public bool IsInDialogue { get; private set; } = false;

        private void Awake()
        {
            if (Instance != null && Instance != this)
            {
                Destroy(gameObject);
                return;
            }
            Instance = this;

            if (dialoguePanel != null)
                dialoguePanel.SetActive(false);
        }

        private void Start()
        {
            cachedPlayerController = FindFirstObjectByType<Replica.Player.PlayerController>();
        }

        private void Update()
        {
            if (!IsInDialogue) return;

            bool advancePressed = false;
#if ENABLE_INPUT_SYSTEM
            if (Keyboard.current != null && (Keyboard.current.eKey.wasPressedThisFrame || Keyboard.current.spaceKey.wasPressedThisFrame || Keyboard.current.enterKey.wasPressedThisFrame))
                advancePressed = true;
            if (Gamepad.current != null && (Gamepad.current.buttonSouth.wasPressedThisFrame || Gamepad.current.buttonWest.wasPressedThisFrame))
                advancePressed = true;
#endif
            if (!advancePressed && (Input.GetKeyDown(KeyCode.E) || Input.GetKeyDown(KeyCode.Space) || Input.GetKeyDown(KeyCode.Return) || Input.GetMouseButtonDown(0)))
            {
                advancePressed = true;
            }

            if (advancePressed)
            {
                if (isTyping)
                {
                    // Completar línea instantáneamente
                    CompleteCurrentLineInstantly();
                }
                else
                {
                    DisplayNextLine();
                }
            }
        }

        /// <summary>
        /// Inicia una conversación con nombre de hablante y lista de líneas.
        /// </summary>
        public void StartDialogue(string speaker, string[] lines, UnityAction onComplete = null)
        {
            if (lines == null || lines.Length == 0)
            {
                onComplete?.Invoke();
                return;
            }

            if (cachedPlayerController == null)
                cachedPlayerController = FindFirstObjectByType<Replica.Player.PlayerController>();

            if (cachedPlayerController != null)
                cachedPlayerController.CanMove = false;

            IsInDialogue = true;
            onCompleteCallback = onComplete;

            if (dialoguePanel != null)
                dialoguePanel.SetActive(true);

            if (speakerNameText != null)
                speakerNameText.text = speaker;

            currentLines.Clear();
            foreach (var line in lines)
            {
                currentLines.Enqueue(line);
            }

            onDialogueStarted?.Invoke();
            DisplayNextLine();
        }

        /// <summary>
        /// Sobrecarga para iniciar diálogo sin especificar hablante por separado.
        /// </summary>
        public void StartDialogue(string[] lines, UnityAction onComplete = null)
        {
            StartDialogue("", lines, onComplete);
        }

        private void DisplayNextLine()
        {
            if (currentLines.Count == 0)
            {
                EndDialogue();
                return;
            }

            currentFullLine = currentLines.Dequeue();
            if (typingCoroutine != null)
                StopCoroutine(typingCoroutine);

            if (typingSpeed > 0f)
            {
                typingCoroutine = StartCoroutine(TypeLine(currentFullLine));
            }
            else
            {
                if (dialogueContentText != null)
                    dialogueContentText.text = currentFullLine;
                isTyping = false;
                if (continueIndicator != null)
                    continueIndicator.SetActive(true);
            }
        }

        private IEnumerator TypeLine(string line)
        {
            isTyping = true;
            if (continueIndicator != null)
                continueIndicator.SetActive(false);

            if (dialogueContentText != null)
                dialogueContentText.text = "";

            foreach (char letter in line.ToCharArray())
            {
                if (dialogueContentText != null)
                    dialogueContentText.text += letter;
                yield return new WaitForSeconds(typingSpeed);
            }

            isTyping = false;
            if (continueIndicator != null)
                continueIndicator.SetActive(true);
        }

        private void CompleteCurrentLineInstantly()
        {
            if (typingCoroutine != null)
                StopCoroutine(typingCoroutine);

            if (dialogueContentText != null)
                dialogueContentText.text = currentFullLine;

            isTyping = false;
            if (continueIndicator != null)
                continueIndicator.SetActive(true);
        }

        private void EndDialogue()
        {
            IsInDialogue = false;

            if (dialoguePanel != null)
                dialoguePanel.SetActive(false);

            if (cachedPlayerController != null)
                cachedPlayerController.CanMove = true;

            onDialogueEnded?.Invoke();

            var callback = onCompleteCallback;
            onCompleteCallback = null;
            callback?.Invoke();
        }
    }
}
