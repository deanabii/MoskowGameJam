using System;
using UnityEngine;
using UnityEngine.Events;

namespace MoskowGameJam.NPC
{
    [RequireComponent(typeof(NPCMovement))]
    public class NPCInteractable : MonoBehaviour
    {
        [Header("Identitas NPC")]
        [Tooltip("Nama NPC yang akan ditampilkan di kotak dialog")]
        [SerializeField] private string npcName = "Warga Moskow";

        [Tooltip("Teks prompt yang muncul saat player mendekat (misal: '[E] Talk')")]
        [SerializeField] private string promptText = "[E] Talk";

        [Header("Kalimat Dialog")]
        [Tooltip("Kalimat dialog yang akan selalu diucapkan NPC setiap kali diajak bicara")]
        [TextArea(2, 5)]
        [SerializeField] private string[] dialogueLines = new string[]
        {
            "Halo! Senang bertemu denganmu.",
            "Cuaca hari ini cukup dingin, bukan?",
            "Semoga perjalananmu menyenangkan!"
        };

        [Header("Fitur Khusus NPC Tertentu (HANYA untuk Stall Blini / Anastasia)")]
        [Tooltip("Centang HANYA jika NPC ini memiliki fitur khusus lanjutan seperti melihat resep (contoh: Ms. Anastasia)")]
        [SerializeField] private bool hasSpecialSubsequentInteraction = false;

        [Tooltip("Teks prompt setelah obrolan awal selesai (Hanya aktif jika hasSpecialSubsequentInteraction dicentang)")]
        [SerializeField] private string subsequentPromptText = "[E] View Recipe";

        [Tooltip("Dialog khusus saat diajak bicara lagi setelah obrolan awal (Hanya jika hasSpecialSubsequentInteraction dicentang)")]
        [TextArea(2, 5)]
        [SerializeField] private string[] subsequentDialogueLines = new string[]
        {
            "Here is the Blini recipe note again whenever you need to check the steps!"
        };

        [Tooltip("Jika dicentang, setelah obrolan awal selesai, interaksi berikutnya langsung membuka resep tanpa melalui kotak dialog")]
        [SerializeField] private bool directActionOnSubsequent = false;

        [Header("Pengaturan Interaksi")]
        [Tooltip("Jarak maksimal untuk berinteraksi")]
        [SerializeField] private float interactDistance = 3.0f;

        [Header("Audio & Event (Opsional)")]
        [SerializeField] private AudioClip greetingAudio;
        [SerializeField] private UnityEvent onDialogueStarted;
        [SerializeField] private UnityEvent onDialogueFinished;

        private NPCMovement npcMovement;
        private bool isInteracting = false;
        private Transform currentPlayer;
        private bool hasCompletedInitialDialogue = false;

        public string NPCName => npcName;
        public float InteractDistance => interactDistance;
        public bool IsInteracting => isInteracting;
        public bool HasCompletedInitialDialogue => hasCompletedInitialDialogue;
        public bool HasSpecialSubsequentInteraction => hasSpecialSubsequentInteraction;
        public UnityEvent OnDialogueFinishedEvent => onDialogueFinished;

        public string PromptText
        {
            get
            {
                // HANYA NPC khusus (seperti Anastasia) yang mengubah prompt teks setelah dialog pertama
                if (hasSpecialSubsequentInteraction && hasCompletedInitialDialogue && !string.IsNullOrEmpty(subsequentPromptText))
                {
                    return subsequentPromptText;
                }
                return promptText;
            }
        }

        public string[] DialogueLines => dialogueLines;

        public string[] CurrentDialogueLines
        {
            get
            {
                // HANYA NPC khusus (seperti Anastasia) yang mengganti kalimat dialog setelah dialog pertama
                if (hasSpecialSubsequentInteraction && hasCompletedInitialDialogue && subsequentDialogueLines != null && subsequentDialogueLines.Length > 0)
                {
                    return subsequentDialogueLines;
                }
                // Untuk semua NPC lain: SELALU mengulang dialog yang sama
                return dialogueLines;
            }
        }

        public static event Action<NPCInteractable, Transform> OnInteractionRequested;
        public static event Action<NPCInteractable> OnInteractionClosed;

        private void Awake()
        {
            npcMovement = GetComponent<NPCMovement>();
        }

        public void StartInteraction(Transform player)
        {
            if (isInteracting) return;

            isInteracting = true;
            currentPlayer = player;

            if (npcMovement != null)
            {
                npcMovement.PauseForInteraction(player);
            }

            if (greetingAudio != null)
            {
                AudioSource.PlayClipAtPoint(greetingAudio, transform.position);
            }

            onDialogueStarted?.Invoke();

            // Jika NPC khusus diatur langsung aksi tanpa kotak dialog setelah dialog pertama selesai
            if (hasSpecialSubsequentInteraction && hasCompletedInitialDialogue && directActionOnSubsequent)
            {
                EndInteraction();
                return;
            }

            OnInteractionRequested?.Invoke(this, player);
        }

        public void EndInteraction()
        {
            if (!isInteracting) return;

            isInteracting = false;
            currentPlayer = null;
            hasCompletedInitialDialogue = true;

            if (npcMovement != null)
            {
                npcMovement.ResumeFromInteraction();
            }

            onDialogueFinished?.Invoke();
            OnInteractionClosed?.Invoke(this);
        }

        private void OnDrawGizmosSelected()
        {
            Gizmos.color = Color.green;
            Gizmos.DrawWireSphere(transform.position, interactDistance);
        }
    }
}
