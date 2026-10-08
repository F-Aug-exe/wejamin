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
    public class DialogueSystem : MonoBehaviour
    {
        public static DialogueSystem Instance { get; private set; }

        [Header("Referencias de UI (Inspector)")]
        [SerializeField] private GameObject dialoguePanel;
        [SerializeField] private TextMeshProUGUI speakerNameText;
        [SerializeField] private TextMeshProUGUI dialogueContentText;
        [SerializeField] private GameObject continueIndicator;

        [Header("Audio")]
        [SerializeField] private AudioSource audioSource;

        [Header("Efecto de Escritura")]
        [SerializeField] private float typingSpeed = 0.02f;

        public UnityEvent onDialogueStarted;
        public UnityEvent onDialogueEnded;

        private readonly Queue<Replica.Quests.DialogueLine> currentLines = new Queue<Replica.Quests.DialogueLine>();
        private Replica.Quests.DialogueLine currentFullLine;
        private Coroutine typingCoroutine;
        private bool isTyping = false;
        private UnityAction onCompleteCallback;
        private Replica.Player.PlayerController cachedPlayerController;

        public bool IsInDialogue { get; private set; } = false;
        private float dialogueStartTime;

        private void Awake()
        {
            
            Instance = this;

            if (dialoguePanel != null)
                dialoguePanel.SetActive(false);

            if (audioSource == null) audioSource = gameObject.AddComponent<AudioSource>();
        }

        private void Start()
        {
            cachedPlayerController = FindAnyObjectByType<Replica.Player.PlayerController>();
        }

        private void Update()
        {
            if (!IsInDialogue || Time.time - dialogueStartTime < 0.2f) return;

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
                    CompleteCurrentLineInstantly();
                }
                else
                {
                    DisplayNextLine();
                }
            }
        }

        public void StartDialogue(string speaker, Replica.Quests.DialogueLine[] lines, UnityAction onComplete = null)
        {
            if (lines == null || lines.Length == 0)
            {
                onComplete?.Invoke();
                return;
            }

            if (cachedPlayerController == null)
                cachedPlayerController = FindAnyObjectByType<Replica.Player.PlayerController>();

            if (cachedPlayerController != null)
                cachedPlayerController.CanMove = false;

            IsInDialogue = true;
            dialogueStartTime = Time.time;
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

        public void StartDialogue(Replica.Quests.DialogueLine[] lines, UnityAction onComplete = null)
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

            if (currentFullLine.voiceOrSfx != null && audioSource != null) {
                audioSource.PlayOneShot(currentFullLine.voiceOrSfx);
            }

            if (typingSpeed > 0f)
            {
                typingCoroutine = StartCoroutine(TypeLine(currentFullLine.text));
            }
            else
            {
                if (dialogueContentText != null)
                    dialogueContentText.text = currentFullLine.text;
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

            if (string.IsNullOrEmpty(line)) line = "";

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

            if (dialogueContentText != null && currentFullLine != null)
                dialogueContentText.text = currentFullLine.text;

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

