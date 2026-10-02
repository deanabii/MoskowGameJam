using UnityEngine;
using TMPro;

#if ENABLE_INPUT_SYSTEM
using UnityEngine.InputSystem;
#endif

namespace MoskowGameJam.NPC
{
    public class PlayerNPCInteraction : MonoBehaviour
    {
        [Header("Referensi")]
        [Tooltip("Kamera Player. Jika kosong, akan otomatis mendeteksi Camera.main")]
        [SerializeField] private Camera playerCamera;

        [Header("Pengaturan Deteksi")]
        [Tooltip("Jarak maksimal deteksi interaksi ke NPC")]
        [SerializeField] private float interactDistance = 3.5f;

        [Tooltip("Layer mask untuk NPC")]
        [SerializeField] private LayerMask npcLayer = ~0;

        [Tooltip("Tombol interaksi")]
        [SerializeField] private KeyCode interactKey = KeyCode.E;

        [Header("UI Prompt")]
        [Tooltip("Root GameObject panel prompt (misal kotak teks [E] Bicara)")]
        [SerializeField] private GameObject promptPanel;

        [Tooltip("Komponen teks prompt")]
        [SerializeField] private TMP_Text promptText;

        private NPCInteractable targetNPC;

        private void Awake()
        {
            if (playerCamera == null)
            {
                playerCamera = Camera.main;
            }
        }

        private void Start()
        {
            HidePrompt();
        }

        private void Update()
        {
            // Jika jendela dialog atau popup resep sedang terbuka, sembunyikan prompt
            if ((NPCDialogueUI.Instance != null && NPCDialogueUI.Instance.IsDialogueActive) ||
                (RecipePopupUI.Instance != null && RecipePopupUI.Instance.IsOpen))
            {
                HidePrompt();
                targetNPC = null;
                return;
            }

            DetectNPC();
            HandleInput();
        }

        private void DetectNPC()
        {
            if (playerCamera == null)
            {
                playerCamera = Camera.main;
                if (playerCamera == null) return;
            }

            // 1. Deteksi lewat Raycast dari pandangan kamera
            Ray ray = new Ray(playerCamera.transform.position, playerCamera.transform.forward);
            if (Physics.Raycast(ray, out RaycastHit hit, interactDistance, npcLayer))
            {
                NPCInteractable npc = hit.collider.GetComponentInParent<NPCInteractable>();
                if (npc != null && !npc.IsInteracting)
                {
                    targetNPC = npc;
                    ShowPrompt(npc.PromptText);
                    return;
                }
            }

            // 2. Deteksi jarak dekat di sekitar player (Radius check)
            Collider[] colliders = Physics.OverlapSphere(transform.position + transform.forward * 0.8f, 1.5f, npcLayer);
            foreach (var col in colliders)
            {
                NPCInteractable npc = col.GetComponentInParent<NPCInteractable>();
                if (npc != null && !npc.IsInteracting)
                {
                    targetNPC = npc;
                    ShowPrompt(npc.PromptText);
                    return;
                }
            }

            targetNPC = null;
            HidePrompt();
        }

        private void HandleInput()
        {
            if (targetNPC != null && IsInteractPressed())
            {
                targetNPC.StartInteraction(this.transform);
                HidePrompt();
            }
        }

        private bool IsInteractPressed()
        {
#if ENABLE_INPUT_SYSTEM
            if (Keyboard.current != null)
            {
                if (System.Enum.TryParse(interactKey.ToString(), out Key key))
                {
                    if (key != Key.None && Keyboard.current[key].wasPressedThisFrame) return true;
                }
            }
#endif

#if ENABLE_LEGACY_INPUT_MANAGER
            try
            {
                return Input.GetKeyDown(interactKey);
            }
            catch {}
#endif
            return false;
        }

        private void ShowPrompt(string message)
        {
            if (promptPanel != null) promptPanel.SetActive(true);
            if (promptText != null) promptText.text = message;
        }

        private void HidePrompt()
        {
            if (promptPanel != null) promptPanel.SetActive(false);
        }
    }
}
