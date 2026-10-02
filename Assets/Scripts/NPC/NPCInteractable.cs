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

        [Tooltip("Teks prompt yang muncul saat player mendekat")]
        [SerializeField] private string promptText = "[E] Bicara";

        [Header("Kalimat Dialog")]
        [TextArea(2, 5)]
        [SerializeField] private string[] dialogueLines = new string[]
        {
            "Halo! Senang bertemu denganmu.",
            "Cuaca hari ini cukup dingin, bukan?",
            "Semoga perjalananmu menyenangkan!"
        };

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

        public string NPCName => npcName;
        public string PromptText => promptText;
        public string[] DialogueLines => dialogueLines;
        public float InteractDistance => interactDistance;
        public bool IsInteracting => isInteracting;

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
            OnInteractionRequested?.Invoke(this, player);
        }

        public void EndInteraction()
        {
            if (!isInteracting) return;

            isInteracting = false;
            currentPlayer = null;

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
