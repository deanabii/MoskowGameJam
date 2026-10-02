using UnityEngine;

#if ENABLE_INPUT_SYSTEM
using UnityEngine.InputSystem;
#endif

using MoskowGameJam.ToolInteraction.UI;
using MoskowGameJam.Interaction;

namespace MoskowGameJam.ToolInteraction
{
    public class PlayerToolInteraction : MonoBehaviour
    {
        [Header("Camera & Raycast References")]
        [Tooltip("Kamera player untuk pancaran Raycast. Jika kosong akan otomatis mengambil Camera.main")]
        [SerializeField] private Camera playerCamera;

        [Tooltip("Jarak maksimal raycast interaksi ke alat")]
        [SerializeField] private float interactDistance = 3.5f;

        [Tooltip("Layer mask untuk objek alat yang dapat di-interaksi")]
        [SerializeField] private LayerMask interactableLayer = ~0;

        [Header("Keybind Settings")]
        [Tooltip("Tombol untuk masuk / keluar dari interaksi alat (Cinemachine View)")]
        [SerializeField] private KeyCode actionKey = KeyCode.F;

        [Tooltip("Tombol untuk menempatkan objek yang dipegang ke dalam alat")]
        [SerializeField] private KeyCode placingKey = KeyCode.E;

        [Header("Player Movement Control (Optional)")]
        [Tooltip("Skrip pergerakan player yang akan di-disable saat mode interaksi alat aktif")]
        [SerializeField] private MonoBehaviour playerMovementScript;

        public static PlayerToolInteraction Instance { get; private set; }

        private ToolInteractable currentTargetTool;
        private ToolInteractable activeInteractionTool;
        private bool isInteracting;
        private bool justEnteredInteraction;

        public bool IsInteracting => isInteracting;
        public bool IsHoveringTool => currentTargetTool != null;
        public ToolInteractable CurrentTargetTool => currentTargetTool;

        private void Awake()
        {
            if (Instance == null) Instance = this;
            AutoAssignReferences();
        }

        private void AutoAssignReferences()
        {
            if (playerCamera == null)
            {
                playerCamera = Camera.main;
                if (playerCamera == null)
                {
#if UNITY_2023_1_OR_NEWER
                    playerCamera = Object.FindFirstObjectByType<Camera>();
#else
                    playerCamera = Object.FindObjectOfType<Camera>();
#endif
                }
            }

            EnsureCinemachineBrainOnCamera();
        }

        private void EnsureCinemachineBrainOnCamera()
        {
            if (playerCamera == null) return;

            Component brain = playerCamera.GetComponent("CinemachineBrain") ?? playerCamera.GetComponent("Unity.Cinemachine.CinemachineBrain");
            if (brain == null)
            {
                System.Type brainType = System.Type.GetType("Unity.Cinemachine.CinemachineBrain, Unity.Cinemachine") 
                                     ?? System.Type.GetType("Cinemachine.CinemachineBrain, Cinemachine");
                if (brainType != null)
                {
                    playerCamera.gameObject.AddComponent(brainType);
                    Debug.Log("<color=green>[PlayerToolInteraction] CinemachineBrain otomatis ditambahkan ke Main Camera.</color>");
                }
            }
        }

        private void Update()
        {
            if (playerCamera == null)
            {
                AutoAssignReferences();
                if (playerCamera == null) return;
            }

            if (isInteracting)
            {
                HandleActiveInteraction();
            }
            else
            {
                HandleRaycastHover();
                HandleInputOutsideInteraction();
            }
        }

        private void HandleRaycastHover()
        {
            Ray ray = new Ray(playerCamera.transform.position, playerCamera.transform.forward);

            if (Physics.Raycast(ray, out RaycastHit hit, interactDistance, interactableLayer))
            {
                ToolInteractable tool = hit.collider.GetComponentInParent<ToolInteractable>();

                if (tool != null)
                {
                    if (currentTargetTool != tool)
                    {
                        ClearCurrentTargetHover();
                        currentTargetTool = tool;
                        currentTargetTool.SetGlow(true);
                    }

                    // Tampilkan Dual UI Prompt dengan Format Tombol Dinamis
                    if (ToolInteractionUI.Instance != null)
                    {
                        ToolInteractionUI.Instance.ShowDualPrompts(
                            currentTargetTool.GetFormattedActionPrompt(actionKey),
                            currentTargetTool.ActionPromptSprite,
                            currentTargetTool.GetFormattedPlacingPrompt(placingKey),
                            currentTargetTool.PlacingPromptSprite
                        );
                    }
                    return;
                }
            }

            // Raycast tidak mengenai alat
            ClearCurrentTargetHover();
        }

        private void ClearCurrentTargetHover()
        {
            if (currentTargetTool != null)
            {
                currentTargetTool.SetGlow(false);
                currentTargetTool = null;
            }

            if (ToolInteractionUI.Instance != null)
            {
                ToolInteractionUI.Instance.HideDualPrompts();
            }
        }

        private void HandleInputOutsideInteraction()
        {
            if (currentTargetTool == null) return;

            // Tekan [R] -> Masuk Mode Interaksi Alat (Cinemachine View)
            if (IsKeyPressedThisFrame(actionKey))
            {
                EnterToolInteraction(currentTargetTool);
            }
            // Tekan [E] -> Masukkan Objek yang Sedang Dipegang ke Alat
            else if (IsKeyPressedThisFrame(placingKey))
            {
                TryPlaceHeldItemIntoTool(currentTargetTool);
            }
        }

        public void EnterToolInteraction(ToolInteractable tool)
        {
            if (tool == null || isInteracting) return;

            isInteracting = true;
            justEnteredInteraction = true;
            activeInteractionTool = tool;

            // Matikan glow saat mode interaksi aktif
            activeInteractionTool.SetGlow(false);

            // Sembunyikan Dual UI Prompt dan tampilkan Exit Prompt di Horizontal Layout Container
            if (ToolInteractionUI.Instance != null)
            {
                ToolInteractionUI.Instance.HideDualPrompts();
                ToolInteractionUI.Instance.ShowExitPrompt(
                    "[F] Selesai",
                    activeInteractionTool.ExitPromptSprite
                );
            }
            else if (PlayerObjectInteraction.Instance != null)
            {
                PlayerObjectInteraction.Instance.SetExitPrompt("[F] Selesai");
            }

            // Kunci pergerakan player
            SetPlayerMovementLocked(true);

            // Aktifkan Cinemachine Virtual Camera (Fokus Kamera ke Alat)
            activeInteractionTool.SetCameraActive(true);

            Debug.Log($"[PlayerToolInteraction] Masuk mode interaksi alat & minigame {activeInteractionTool.name}");
        }

        public void ExitToolInteraction()
        {
            if (!isInteracting || activeInteractionTool == null) return;

            // Deaktivasi Cinemachine Virtual Camera
            activeInteractionTool.SetCameraActive(false);

            // Sembunyikan Exit Prompt
            if (ToolInteractionUI.Instance != null)
            {
                ToolInteractionUI.Instance.HideExitPrompt();
            }
            else if (PlayerObjectInteraction.Instance != null)
            {
                PlayerObjectInteraction.Instance.ClearCustomPrompts();
            }

            // Buka penguncian pergerakan player
            SetPlayerMovementLocked(false);

            ToolInteractable exitedTool = activeInteractionTool;
            activeInteractionTool = null;
            isInteracting = false;
            justEnteredInteraction = false;

            Debug.Log($"[PlayerToolInteraction] Keluar dari mode interaksi alat {exitedTool.name}");
        }

        private void HandleActiveInteraction()
        {
            if (activeInteractionTool == null)
            {
                isInteracting = false;
                return;
            }

            // Abaikan input keluar pada frame persis saat masuk mode interaksi
            if (justEnteredInteraction)
            {
                justEnteredInteraction = false;
                return;
            }

            // Update mini-game interaksi spesifik
            if (activeInteractionTool.SpecificInteraction != null)
            {
                activeInteractionTool.SpecificInteraction.OnUpdateInteraction();
            }

            // Tekan tombol interaksi/keluar (F, R, atau Escape) untuk selesai & kembali ke kamera player
            if (IsKeyPressedThisFrame(actionKey) || IsKeyPressedThisFrame(KeyCode.F) || IsKeyPressedThisFrame(KeyCode.Escape))
            {
                ExitToolInteraction();
            }
        }

        private void TryPlaceHeldItemIntoTool(ToolInteractable tool)
        {
            if (tool == null) return;

            // Cari objek yang sedang di-grab / dipegang player di scene
            GameObject itemToPlace = FindHeldItemInScene();

            if (itemToPlace != null && itemToPlace != tool.gameObject)
            {
                if (tool.AddItemToTool(itemToPlace))
                {
                    if (PlayerObjectInteraction.Instance != null)
                    {
                        PlayerObjectInteraction.Instance.ClearHeldObject();
                    }
                }
            }
            else if (itemToPlace == tool.gameObject)
            {
                Debug.Log("[PlayerToolInteraction] Objek yang dipegang adalah alat itu sendiri.");
            }
            else
            {
                Debug.LogWarning("[PlayerToolInteraction] Tidak ada objek dipegang untuk dimasukkan ke alat.");
            }
        }

        private GameObject FindHeldItemInScene()
        {
            if (PlayerObjectInteraction.Instance != null && PlayerObjectInteraction.Instance.HeldObject != null)
            {
                return PlayerObjectInteraction.Instance.HeldObject.gameObject;
            }

            // Mencari objek child di bawah GrabHoldPoint jika ada
            Transform cameraTransform = playerCamera != null ? playerCamera.transform : transform;
            Transform holdPoint = cameraTransform.Find("[GrabHoldPoint]");

            if (holdPoint != null && holdPoint.childCount > 0)
            {
                return holdPoint.GetChild(0).gameObject;
            }

            return null;
        }

        private void SetPlayerMovementLocked(bool locked)
        {
            if (playerMovementScript != null)
            {
                playerMovementScript.enabled = !locked;
            }
        }

        private bool IsKeyPressedThisFrame(KeyCode keyCode)
        {
#if ENABLE_INPUT_SYSTEM
            if (Keyboard.current != null)
            {
                if (System.Enum.TryParse(keyCode.ToString(), out Key key))
                {
                    if (key != Key.None && Keyboard.current[key].wasPressedThisFrame)
                    {
                        return true;
                    }
                }
            }
#endif

#if ENABLE_LEGACY_INPUT_MANAGER
            try
            {
                return Input.GetKeyDown(keyCode);
            }
            catch {}
#endif

            return false;
        }
    }
}
