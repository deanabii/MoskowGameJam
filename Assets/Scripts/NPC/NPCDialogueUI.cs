using System.Collections;
using UnityEngine;
using TMPro;

#if ENABLE_INPUT_SYSTEM
using UnityEngine.InputSystem;
#endif

namespace MoskowGameJam.NPC
{
    public class NPCDialogueUI : MonoBehaviour
    {
        public static NPCDialogueUI Instance { get; private set; }

        [Header("Referensi UI (TextMeshPro)")]
        [Tooltip("Root GameObject Panel Dialog")]
        [SerializeField] private GameObject dialoguePanel;

        [Tooltip("Komponen teks untuk Nama NPC")]
        [SerializeField] private TMP_Text nameText;

        [Tooltip("Komponen teks untuk Isi Dialog")]
        [SerializeField] private TMP_Text dialogueText;

        [Tooltip("Komponen teks petunjuk tombol lanjut")]
        [SerializeField] private TMP_Text continueHintText;

        [Header("Pengaturan Dialog")]
        [Tooltip("Kecepatan efek ketik (detik per huruf). Set 0 untuk teks langsung muncul")]
        [SerializeField] private float typingSpeed = 0.03f;

        [Tooltip("Tombol keyboard untuk melanjutkan dialog")]
        [SerializeField] private KeyCode continueKey = KeyCode.Space;

        private NPCInteractable activeNPC;
        private string[] activeLines;
        private int lineIndex = 0;
        private bool isTyping = false;
        private Coroutine typingCoroutine;
        private bool isDialogueActive = false;

        public bool IsDialogueActive => isDialogueActive;

        private void Awake()
        {
            if (Instance == null)
            {
                Instance = this;
            }
            else if (Instance != this)
            {
                Destroy(gameObject);
                return;
            }

            AutoResolveReferences();

            if (dialoguePanel != null)
            {
                dialoguePanel.SetActive(false);
            }
        }

        private void AutoResolveReferences()
        {
            if (dialoguePanel == null)
            {
                Transform panelTr = transform.Find("DialogueBoxPanel");
                if (panelTr != null) dialoguePanel = panelTr.gameObject;
            }

            TMP_Text[] allTexts = GetComponentsInChildren<TMP_Text>(true);
            foreach (var t in allTexts)
            {
                string lower = t.gameObject.name.ToLower();
                if (nameText == null && (lower.Contains("name") || lower.Contains("nama")))
                    nameText = t;
                else if (dialogueText == null && (lower.Contains("dialogue") || lower.Contains("dialog") || lower.Contains("text") || lower.Contains("subtitle")))
                    dialogueText = t;
                else if (continueHintText == null && (lower.Contains("hint") || lower.Contains("continue") || lower.Contains("lanjut")))
                    continueHintText = t;
            }
        }

        private void OnEnable()
        {
            NPCInteractable.OnInteractionRequested += OnStartDialogue;
        }

        private void OnDisable()
        {
            NPCInteractable.OnInteractionRequested -= OnStartDialogue;
        }

        private void Update()
        {
            if (!isDialogueActive) return;

            if (IsContinuePressed())
            {
                AdvanceDialogue();
            }
        }

        private bool IsContinuePressed()
        {
#if ENABLE_INPUT_SYSTEM
            if (Keyboard.current != null)
            {
                if (Keyboard.current.spaceKey.wasPressedThisFrame || Keyboard.current.enterKey.wasPressedThisFrame)
                {
                    return true;
                }
                if (continueKey != KeyCode.None && continueKey != KeyCode.E &&
                    System.Enum.TryParse(continueKey.ToString(), out Key key))
                {
                    if (key != Key.None && Keyboard.current[key].wasPressedThisFrame) return true;
                }
            }
#endif

#if ENABLE_LEGACY_INPUT_MANAGER
            try
            {
                if (Input.GetKeyDown(KeyCode.Space) || Input.GetKeyDown(KeyCode.Return))
                {
                    return true;
                }
                if (continueKey != KeyCode.None && continueKey != KeyCode.E && Input.GetKeyDown(continueKey))
                {
                    return true;
                }
            }
            catch {}
#endif
            return false;
        }

        private void OnStartDialogue(NPCInteractable npc, Transform player)
        {
            activeNPC = npc;
            activeLines = npc.CurrentDialogueLines;
            lineIndex = 0;

            if (activeLines == null || activeLines.Length == 0)
            {
                activeLines = new string[] { "..." };
            }

            if (nameText != null)
            {
                nameText.text = npc.NPCName;
            }

            if (continueHintText != null)
            {
                continueHintText.text = "[Space] Lanjut";
            }

            if (dialoguePanel != null)
            {
                dialoguePanel.SetActive(true);
            }

            isDialogueActive = true;
            ShowLine();
        }

        public void AdvanceDialogue()
        {
            if (isTyping)
            {
                // Selesaikan efek ketik seketika
                if (typingCoroutine != null) StopCoroutine(typingCoroutine);
                isTyping = false;
                if (dialogueText != null)
                {
                    dialogueText.text = activeLines[lineIndex];
                }
                return;
            }

            lineIndex++;
            if (lineIndex < activeLines.Length)
            {
                ShowLine();
            }
            else
            {
                CloseDialogue();
            }
        }

        private void ShowLine()
        {
            if (lineIndex >= activeLines.Length) return;

            if (typingCoroutine != null) StopCoroutine(typingCoroutine);

            if (typingSpeed > 0f)
            {
                typingCoroutine = StartCoroutine(TypewriterRoutine(activeLines[lineIndex]));
            }
            else
            {
                if (dialogueText != null)
                {
                    dialogueText.text = activeLines[lineIndex];
                }
            }
        }

        private IEnumerator TypewriterRoutine(string sentence)
        {
            isTyping = true;
            if (dialogueText != null) dialogueText.text = "";

            foreach (char letter in sentence)
            {
                if (dialogueText != null) dialogueText.text += letter;
                yield return new WaitForSeconds(typingSpeed);
            }

            isTyping = false;
        }

        public void CloseDialogue()
        {
            if (typingCoroutine != null) StopCoroutine(typingCoroutine);
            isTyping = false;
            isDialogueActive = false;

            if (dialoguePanel != null)
            {
                dialoguePanel.SetActive(false);
            }

            if (activeNPC != null)
            {
                activeNPC.EndInteraction();
                activeNPC = null;
            }
        }
    }
}
