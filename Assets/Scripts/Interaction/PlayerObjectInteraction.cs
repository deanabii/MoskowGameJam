using UnityEngine;
using UnityEngine.UI;
using TMPro;
using UnityEngine.Rendering.Universal;

#if ENABLE_INPUT_SYSTEM
using UnityEngine.InputSystem;
#endif

using MoskowGameJam.Settings;
using MoskowGameJam.ToolInteraction.UI;

namespace MoskowGameJam.Interaction
{
    public class PlayerObjectInteraction : MonoBehaviour
    {
        [Header("References")]
        [Tooltip("Kamera player. Jika kosong, akan otomatis mengambil Camera.main")]
        [SerializeField] private Camera playerCamera;

        [Tooltip("Transform Player. Jika kosong, akan mencari objek bersimbol Tag 'Player'")]
        [SerializeField] private Transform playerTransform;

        [Header("Hold Point Settings (Realtime)")]
        [Tooltip("Offset posisi objek yang dipegang relatif terhadap Kamera")]
        [SerializeField] private Vector3 holdOffsetPosition = new Vector3(0f, -0.2f, 1.5f);

        [Tooltip("Offset rotasi (Euler Angles) objek yang dipegang")]
        [SerializeField] private Vector3 holdOffsetRotation = Vector3.zero;

        [Tooltip("Transform tempat menyimpan objek (Opsional: Jika kosong akan dibuat otomatis secara realtime)")]
        [SerializeField] private Transform grabHoldTransform;

        [Header("Overlay Camera & Layer Settings")]
        [Tooltip("Nama layer khusus untuk objek yang sedang di-grab (Default: HeldObject)")]
        [SerializeField] private string heldObjectLayerName = "HeldObject";

        [Tooltip("Otomatis buat & konfigurasi Overlay Camera untuk render objek grab paling depan")]
        [SerializeField] private bool autoSetupOverlayCamera = true;

        [Header("Interaction Settings")]
        [SerializeField] private float interactDistance = 3.5f;
        [SerializeField] private LayerMask interactableLayer = ~0;
        [SerializeField] private KeyCode interactKey = KeyCode.E;

        [Header("Drop Settings")]
        [Tooltip("Jarak maksimal Raycast untuk menempatkan objek saat di-drop")]
        [SerializeField] private float maxDropDistance = 4.0f;

        [Tooltip("Layer mask permukaan yang valid untuk menampung objek yang di-drop")]
        [SerializeField] private LayerMask dropSurfaceLayer = ~0;

        [Tooltip("Offset elevasi dari titik benturan permukaan agar collider tidak tembus permukaan")]
        [SerializeField] private Vector3 dropSurfaceOffset = new Vector3(0f, 0.05f, 0f);

        [Tooltip("Aktifkan fitur penempatan permukaan via Raycast saat di-drop")]
        [SerializeField] private bool enableRayBasedDrop = true;

        [Header("Multi-Prompt UI References (Horizontal Layout)")]
        [Tooltip("Root Container dengan Horizontal Layout Group")]
        [SerializeField] private GameObject promptContainer;

        [Header("Ambil (Grab) Prompt")]
        [SerializeField] private GameObject grabPromptPanel;
        [SerializeField] private TMP_Text grabPromptText;

        [Header("Interaksi (Tool) Prompt")]
        [SerializeField] private GameObject interactPromptPanel;
        [SerializeField] private TMP_Text interactPromptText;
        [SerializeField] private KeyCode toolInteractKey = KeyCode.F;

        [Header("Masukkan (Insert) Prompt")]
        [SerializeField] private GameObject insertPromptPanel;
        [SerializeField] private TMP_Text insertPromptText;
        [SerializeField] private KeyCode insertKey = KeyCode.Q;

        [Header("Keluar (Exit) Prompt")]
        [SerializeField] private GameObject exitPromptPanel;
        [SerializeField] private TMP_Text exitPromptText;
        [SerializeField] private KeyCode exitKey = KeyCode.F;

        [Header("Reset Prompt")]
        [SerializeField] private GameObject resetPromptPanel;
        [SerializeField] private TMP_Text resetPromptText;
        [SerializeField] private KeyCode resetKey = KeyCode.R;

        [Header("Legacy Single UI Prompt (Fallback)")]
        [Tooltip("Root Canvas UI Panel untuk menampung Sprite dan Teks")]
        [SerializeField] private GameObject uiPromptPanel;

        [Tooltip("Sprite/Image container UI")]
        [SerializeField] private Image uiPromptImage;

        [Tooltip("Teks UI penanda aksi interaksi (TextMeshPro)")]
        [SerializeField] private TMP_Text uiPromptText;

        private Grabbable currentTarget;
        private Grabbable heldObject;
        private MoskowGameJam.ToolInteraction.ToolInteractable currentTool;
        private Camera heldOverlayCamera;
        private int heldLayerIndex = -1;

        // Tool Interaction State (Standalone)
        private bool isToolInteracting;
        private bool justEnteredToolInteraction;
        private MoskowGameJam.ToolInteraction.ToolInteractable activeToolInteraction;

        public static PlayerObjectInteraction Instance { get; private set; }
        public Grabbable HeldObject => heldObject;
        public bool IsToolInteracting => isToolInteracting;

        private void Awake()
        {
            if (Instance == null) Instance = this;
            AutoAssignReferences();
            EnsureGrabHoldTransform();
        }

        private void OnEnable()
        {
            InteractionSettingsManager.OnSettingsChanged += ApplySettingsData;
            if (InteractionSettingsManager.Instance != null)
            {
                ApplySettingsData(InteractionSettingsManager.Instance.CurrentData);
            }
        }

        private void OnDisable()
        {
            InteractionSettingsManager.OnSettingsChanged -= ApplySettingsData;
        }

        private void ApplySettingsData(InteractionSaveData data)
        {
            if (data == null) return;

            interactKey = data.interactKey;
            interactDistance = data.interactDistance;
            maxDropDistance = data.maxDropDistance;
            enableRayBasedDrop = data.enableRayBasedDrop;
        }

        private void Start()
        {
            HideUIPrompt();
        }

        private void OnValidate()
        {
            EnsureGrabHoldTransform();
        }

        private void AutoAssignReferences()
        {
            if (playerCamera == null)
            {
                playerCamera = Camera.main;
                if (playerCamera == null)
                {
                    // Cari kamera aktif apapun di seluruh scene yang di-load
#if UNITY_2023_1_OR_NEWER
                    playerCamera = Object.FindFirstObjectByType<Camera>();
#else
                    playerCamera = Object.FindObjectOfType<Camera>();
#endif
                }
            }

            if (playerTransform == null && playerCamera != null)
            {
                GameObject playerObj = GameObject.FindGameObjectWithTag("Player");
                if (playerObj != null)
                {
                    playerTransform = playerObj.transform;
                }
                else
                {
                    playerTransform = playerCamera.transform;
                }
            }
        }

        public void EnsureGrabHoldTransform()
        {
            AutoAssignReferences();

            if (playerCamera == null)
            {
                // Kamera belum ditemukan di scene manapun, tunggu sampai ditemukan
                return;
            }

            SetupHeldLayerAndCamera();

            if (grabHoldTransform == null)
            {
                Transform existingHold = playerCamera.transform.Find("[GrabHoldPoint]");
                if (existingHold != null)
                {
                    grabHoldTransform = existingHold;
                }
                else
                {
                    // Periksa jika pernah dibuat tanpa parent di tempat lain
                    GameObject existingGlobal = GameObject.Find("[GrabHoldPoint]");
                    if (existingGlobal != null)
                    {
                        grabHoldTransform = existingGlobal.transform;
                    }
                    else
                    {
                        GameObject holdObj = new GameObject("[GrabHoldPoint]");
                        grabHoldTransform = holdObj.transform;
                    }
                }
            }

            // Pastikan parent SELALU di bawah playerCamera.transform
            if (grabHoldTransform.parent != playerCamera.transform)
            {
                grabHoldTransform.SetParent(playerCamera.transform, false);
            }

            grabHoldTransform.localPosition = holdOffsetPosition;
            grabHoldTransform.localEulerAngles = holdOffsetRotation;
        }

        private void SetupHeldLayerAndCamera()
        {
            if (playerCamera == null) return;

            heldLayerIndex = LayerMask.NameToLayer(heldObjectLayerName);
            if (heldLayerIndex < 0)
            {
                heldLayerIndex = 6; // Fallback to Layer 6
            }

            // Main camera tidak merender heldLayerIndex
            playerCamera.cullingMask &= ~(1 << heldLayerIndex);

            if (!autoSetupOverlayCamera) return;

            Transform overlayCamTransform = playerCamera.transform.Find("[HeldOverlayCamera]");
            if (overlayCamTransform == null)
            {
                GameObject camObj = new GameObject("[HeldOverlayCamera]");
                camObj.transform.SetParent(playerCamera.transform, false);
                camObj.transform.localPosition = Vector3.zero;
                camObj.transform.localRotation = Quaternion.identity;
                camObj.transform.localScale = Vector3.one;
                overlayCamTransform = camObj.transform;
            }

            heldOverlayCamera = overlayCamTransform.GetComponent<Camera>();
            if (heldOverlayCamera == null)
            {
                heldOverlayCamera = overlayCamTransform.gameObject.AddComponent<Camera>();
            }

            heldOverlayCamera.clearFlags = CameraClearFlags.Depth;
            heldOverlayCamera.backgroundColor = new Color(0f, 0f, 0f, 0f);
            heldOverlayCamera.cullingMask = 1 << heldLayerIndex;
            heldOverlayCamera.depth = playerCamera.depth + 1;
            heldOverlayCamera.fieldOfView = playerCamera.fieldOfView;
            heldOverlayCamera.nearClipPlane = playerCamera.nearClipPlane;
            heldOverlayCamera.farClipPlane = playerCamera.farClipPlane;
            heldOverlayCamera.orthographic = playerCamera.orthographic;
            heldOverlayCamera.orthographicSize = playerCamera.orthographicSize;
            heldOverlayCamera.allowHDR = playerCamera.allowHDR;
            heldOverlayCamera.allowMSAA = playerCamera.allowMSAA;

            SetupURPCameraStacking(playerCamera, heldOverlayCamera);
        }

        private void SetupURPCameraStacking(Camera mainCam, Camera overlayCam)
        {
            if (mainCam == null || overlayCam == null) return;

            var mainCamData = mainCam.GetComponent<UniversalAdditionalCameraData>();
            if (mainCamData == null)
            {
                mainCamData = mainCam.gameObject.AddComponent<UniversalAdditionalCameraData>();
            }
            mainCamData.renderType = CameraRenderType.Base;

            var overlayCamData = overlayCam.GetComponent<UniversalAdditionalCameraData>();
            if (overlayCamData == null)
            {
                overlayCamData = overlayCam.gameObject.AddComponent<UniversalAdditionalCameraData>();
            }
            overlayCamData.renderType = CameraRenderType.Overlay;

            if (!mainCamData.cameraStack.Contains(overlayCam))
            {
                mainCamData.cameraStack.Add(overlayCam);
            }
        }

        private void Update()
        {
            if (playerCamera == null || grabHoldTransform == null || grabHoldTransform.parent != playerCamera.transform)
            {
                EnsureGrabHoldTransform();
                if (playerCamera == null) return;
            }

            if (isToolInteracting)
            {
                HandleToolInteractionUpdate();
            }
            else
            {
                HandleRaycast();
                HandleInput();
            }
        }

        private void HandleToolInteractionUpdate()
        {
            if (activeToolInteraction == null)
            {
                isToolInteracting = false;
                return;
            }

            if (justEnteredToolInteraction)
            {
                justEnteredToolInteraction = false;
                return;
            }

            // Update mini-game interaksi spesifik
            if (activeToolInteraction.SpecificInteraction != null)
            {
                activeToolInteraction.SpecificInteraction.OnUpdateInteraction();

                // Jika mini-game telah selesai, langsung keluar dari mode fokus interaksi
                if (activeToolInteraction.SpecificInteraction.IsCompleted)
                {
                    ExitToolInteraction();
                    return;
                }
            }

            // Tekan tombol Reset ([R])
            if (IsKeyPressed(resetKey))
            {
                MoskowGameJam.ToolInteraction.ToolInteractable toolToReset = activeToolInteraction;
                ExitToolInteraction();
                toolToReset.ResetObject();
                return;
            }

            // Tekan tombol interaksi/keluar (exitKey, toolInteractKey, atau ESC) untuk selesai & kembali ke kamera player
            if (IsKeyPressed(exitKey) || IsKeyPressed(toolInteractKey) || IsKeyPressed(KeyCode.Escape))
            {
                ExitToolInteraction();
            }
        }

        private void LateUpdate()
        {
            if (heldOverlayCamera != null && playerCamera != null)
            {
                heldOverlayCamera.fieldOfView = playerCamera.fieldOfView;
                heldOverlayCamera.nearClipPlane = playerCamera.nearClipPlane;
                heldOverlayCamera.farClipPlane = playerCamera.farClipPlane;
            }
        }

        public void ClearHeldObject()
        {
            if (heldObject != null)
            {
                heldObject = null;
                HideUIPrompt();
            }
        }

        private void HandleRaycast()
        {
            if (isToolInteracting)
            {
                ClearCurrentTarget();
                return;
            }

            Ray ray = new Ray(playerCamera.transform.position, playerCamera.transform.forward);

            // JIKA SEDANG MEMEGANG OBJEK
            if (heldObject != null)
            {
                ClearCurrentTarget();

                if (Physics.Raycast(ray, out RaycastHit hitTool, interactDistance, interactableLayer))
                {
                    var tool = hitTool.collider.GetComponentInParent<MoskowGameJam.ToolInteraction.ToolInteractable>();
                    if (tool != null)
                    {
                        SetCurrentToolTarget(tool);
                        string placingMsg = !string.IsNullOrEmpty(tool.PlacingPromptText) ? $"[Q] {tool.PlacingPromptText}" : "[Q] Masukkan ke alat";
                        
                        SetPromptsVisibility(
                            showGrab: false, grabMsg: "",
                            showInteract: false, interactMsg: "",
                            showInsert: true, insertMsg: placingMsg
                        );
                        return;
                    }
                }

                SetCurrentToolTarget(null);
                SetPromptsVisibility(
                    showGrab: true, grabMsg: "[E] Lepas",
                    showInteract: false, interactMsg: "",
                    showInsert: false, insertMsg: ""
                );
                return;
            }

            // JIKA TANGAN KOSONG
            if (Physics.Raycast(ray, out RaycastHit hit, interactDistance, interactableLayer))
            {
                Grabbable grabbable = hit.collider.GetComponentInParent<Grabbable>();
                var tool = hit.collider.GetComponentInParent<MoskowGameJam.ToolInteraction.ToolInteractable>();

                if (grabbable != null || tool != null)
                {
                    if (grabbable != null && !grabbable.IsHeld)
                    {
                        if (currentTarget != grabbable)
                        {
                            ClearCurrentGrabbableTarget();
                            currentTarget = grabbable;
                            currentTarget.SetHighlighted(true);
                        }
                    }
                    else
                    {
                        ClearCurrentGrabbableTarget();
                    }

                    SetCurrentToolTarget(tool);

                    bool showGrab = (currentTarget != null);
                    string grabMsg = showGrab ? currentTarget.GrabPromptText : "";

                    bool showInteract = (currentTool != null && !string.IsNullOrEmpty(currentTool.ActionPromptText));
                    string interactMsg = showInteract ? $"[F] {currentTool.ActionPromptText}" : "";

                    SetPromptsVisibility(
                        showGrab: showGrab, grabMsg: grabMsg,
                        showInteract: showInteract, interactMsg: interactMsg,
                        showInsert: false, insertMsg: "" // Sembunyikan Masukkan saat tangan kosong
                    );
                    return;
                }
            }

            ClearCurrentTarget();
            HideUIPrompt();
        }

        private void SetCurrentToolTarget(MoskowGameJam.ToolInteraction.ToolInteractable tool)
        {
            if (currentTool != tool)
            {
                if (currentTool != null)
                {
                    currentTool.SetGlow(false);
                }
                currentTool = tool;
                if (currentTool != null)
                {
                    currentTool.SetGlow(true);
                }
            }
        }

        private void ClearCurrentGrabbableTarget()
        {
            if (currentTarget != null)
            {
                currentTarget.SetHighlighted(false);
                currentTarget = null;
            }
        }

        private void ClearCurrentTarget()
        {
            ClearCurrentGrabbableTarget();
            SetCurrentToolTarget(null);
        }

        private void HandleInput()
        {
            if (isToolInteracting)
            {
                return;
            }

            // Input Ambil / Drop ([E])
            if (IsKeyPressed(interactKey))
            {
                if (heldObject != null)
                {
                    DropObject();
                }
                else if (currentTarget != null)
                {
                    GrabObject(currentTarget);
                }
            }

            // Input Interaksi Tool ([F])
            if (IsKeyPressed(toolInteractKey))
            {
                if (heldObject == null && currentTool != null)
                {
                    EnterToolInteraction(currentTool);
                }
            }

            // Input Masukkan ke Tool Container ([Q])
            if (IsKeyPressed(insertKey))
            {
                if (heldObject != null && currentTool != null)
                {
                    GameObject itemObj = heldObject.gameObject;
                    heldObject.Drop(); // Unparent
                    heldObject = null;

                    currentTool.AddItemToTool(itemObj);
                    HideUIPrompt();
                }
            }

            // Input Reset Tool ([R])
            if (currentTool != null && IsKeyPressed(resetKey))
            {
                currentTool.ResetObject();
            }
        }

        private bool IsKeyPressed(KeyCode keyToCheck)
        {
#if ENABLE_INPUT_SYSTEM
            if (Keyboard.current != null)
            {
                if (System.Enum.TryParse(keyToCheck.ToString(), out Key key))
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
                return Input.GetKeyDown(keyToCheck);
            }
            catch {}
#endif

            return false;
        }

        private void GrabObject(Grabbable target)
        {
            EnsureGrabHoldTransform();

            if (grabHoldTransform == null)
            {
                Debug.LogWarning("[PlayerObjectInteraction] Gagal membuat grabHoldTransform secara realtime!", this);
                return;
            }

            heldObject = target;
            heldObject.Grab(grabHoldTransform, heldLayerIndex);
            ClearCurrentTarget();
        }

        private void DropObject()
        {
            if (heldObject == null) return;

            // Lakukan Raycast dari Kamera ke depan untuk mencari permukaan penempatan objek
            Ray ray = new Ray(playerCamera.transform.position, playerCamera.transform.forward);

            // Matikan sementara collider objek yang dipegang agar Raycast tidak membentur objek itu sendiri
            Collider heldCollider = heldObject.GetComponent<Collider>();
            bool wasColliderEnabled = false;
            if (heldCollider != null)
            {
                wasColliderEnabled = heldCollider.enabled;
                heldCollider.enabled = false;
            }

            if (enableRayBasedDrop && Physics.Raycast(ray, out RaycastHit hit, maxDropDistance, dropSurfaceLayer))
            {
                // Mengenai permukaan (meja/lantai/rak) -> Pindahkan posisi ke titik benturan + offset
                heldObject.transform.position = hit.point + (hit.normal * 0.02f) + dropSurfaceOffset;
            }

            if (heldCollider != null)
            {
                heldCollider.enabled = wasColliderEnabled;
            }

            heldObject.Drop();
            heldObject = null;
            HideUIPrompt();
        }

        public void EnterToolInteraction(MoskowGameJam.ToolInteraction.ToolInteractable tool)
        {
            if (tool == null || isToolInteracting) return;

            isToolInteracting = true;
            justEnteredToolInteraction = true;
            activeToolInteraction = tool;

            // Matikan glow saat mode interaksi aktif
            activeToolInteraction.SetGlow(false);

            // Format dan tampilkan Exit Prompt
            string exitMsg = activeToolInteraction.GetFormattedExitPrompt(exitKey);
            if (string.IsNullOrEmpty(exitMsg)) exitMsg = PromptKeyFormatter.FormatPromptText(exitKey, activeToolInteraction.ExitPromptText);
            if (string.IsNullOrEmpty(exitMsg)) exitMsg = $"[{exitKey}] Keluar";

            string resetMsg = PromptKeyFormatter.FormatPromptText(resetKey, "Reset");
            SetExitPrompt(exitMsg, true, resetMsg);

            // Aktifkan Cinemachine Virtual Camera (Fokus Kamera ke Alat)
            activeToolInteraction.SetCameraActive(true);

            Debug.Log($"[PlayerObjectInteraction] Masuk mode interaksi alat & minigame {activeToolInteraction.name}");
        }

        public void ExitToolInteraction()
        {
            if (!isToolInteracting || activeToolInteraction == null) return;

            // Deaktivasi Cinemachine Virtual Camera
            activeToolInteraction.SetCameraActive(false);

            // Sembunyikan Exit Prompt
            HideUIPrompt();

            MoskowGameJam.ToolInteraction.ToolInteractable exitedTool = activeToolInteraction;
            activeToolInteraction = null;
            isToolInteracting = false;
            justEnteredToolInteraction = false;

            Debug.Log($"[PlayerObjectInteraction] Keluar dari mode interaksi alat {exitedTool.name}");
        }

        public void SetCustomPrompts(bool showAction, string actionText, bool showPlacing, string placingText)
        {
            SetPromptsVisibility(
                showGrab: showPlacing, grabMsg: placingText,
                showInteract: showAction, interactMsg: actionText,
                showInsert: false, insertMsg: "",
                showExit: false, exitMsg: "",
                showReset: false, resetMsg: ""
            );
        }

        public void SetExitPrompt(string exitText, bool showReset = false, string resetText = "")
        {
            SetPromptsVisibility(
                showGrab: false, grabMsg: "",
                showInteract: false, interactMsg: "",
                showInsert: false, insertMsg: "",
                showExit: true, exitMsg: exitText,
                showReset: showReset, resetMsg: resetText
            );
        }

        public void ClearCustomPrompts()
        {
            HideUIPrompt();
        }

        public void SetPromptsVisibility(
            bool showGrab, string grabMsg, 
            bool showInteract, string interactMsg, 
            bool showInsert, string insertMsg,
            bool showExit = false, string exitMsg = "",
            bool showReset = false, string resetMsg = "")
        {
            if (grabPromptPanel != null)
            {
                grabPromptPanel.SetActive(showGrab);
                if (showGrab && grabPromptText != null) grabPromptText.text = grabMsg;
            }

            if (interactPromptPanel != null)
            {
                interactPromptPanel.SetActive(showInteract);
                if (showInteract && interactPromptText != null) interactPromptText.text = interactMsg;
            }

            if (insertPromptPanel != null)
            {
                insertPromptPanel.SetActive(showInsert);
                if (showInsert && insertPromptText != null) insertPromptText.text = insertMsg;
            }

            if (exitPromptPanel != null)
            {
                exitPromptPanel.SetActive(showExit);
                if (showExit && exitPromptText != null) exitPromptText.text = exitMsg;
            }

            if (resetPromptPanel != null)
            {
                resetPromptPanel.SetActive(showReset);
                if (showReset && resetPromptText != null) resetPromptText.text = resetMsg;
            }

            if (promptContainer != null)
            {
                promptContainer.SetActive(showGrab || showInteract || showInsert || showExit || showReset);
            }

            if (uiPromptPanel != null && promptContainer == null)
            {
                uiPromptPanel.SetActive(showGrab || showInteract || showInsert || showExit || showReset);
                if (uiPromptText != null)
                {
                    if (showGrab) uiPromptText.text = grabMsg;
                    else if (showInteract) uiPromptText.text = interactMsg;
                    else if (showInsert) uiPromptText.text = insertMsg;
                    else if (showExit) uiPromptText.text = exitMsg;
                    else if (showReset) uiPromptText.text = resetMsg;
                }
            }
        }

        private void ShowUIPrompt(string promptMessage)
        {
            SetPromptsVisibility(true, promptMessage, false, "", false, "", false, "", false, "");
        }

        private void HideUIPrompt()
        {
            SetPromptsVisibility(false, "", false, "", false, "", false, "", false, "");
        }
    }
}
