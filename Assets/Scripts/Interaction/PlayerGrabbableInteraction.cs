using UnityEngine;
using UnityEngine.UI;
using TMPro;
using UnityEngine.Rendering.Universal;

#if ENABLE_INPUT_SYSTEM
using UnityEngine.InputSystem;
#endif

using MoskowGameJam.Settings;

namespace MoskowGameJam.Interaction
{
    public class PlayerGrabbableInteraction : MonoBehaviour
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

        [Header("UI Prompt References")]
        [Tooltip("Root Canvas UI Panel untuk menampung Sprite dan Teks")]
        [SerializeField] private GameObject uiPromptPanel;

        [Tooltip("Sprite/Image container UI")]
        [SerializeField] private Image uiPromptImage;

        [Tooltip("Teks UI penanda aksi interaksi (TextMeshPro)")]
        [SerializeField] private TMP_Text uiPromptText;

        private Grabbable currentTarget;
        private Grabbable heldObject;
        private Camera heldOverlayCamera;
        private int heldLayerIndex = -1;

        private void Awake()
        {
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

            HandleRaycast();
            HandleInput();
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

        private void HandleRaycast()
        {
            // Jika sedang memegang objek, tampilkan prompt lepas
            if (heldObject != null)
            {
                if (currentTarget != null)
                {
                    currentTarget.SetHighlighted(false);
                    currentTarget = null;
                }
                ShowUIPrompt("[E] Lepas");
                return;
            }

            Ray ray = new Ray(playerCamera.transform.position, playerCamera.transform.forward);
            if (Physics.Raycast(ray, out RaycastHit hit, interactDistance, interactableLayer))
            {
                Grabbable grabbable = hit.collider.GetComponentInParent<Grabbable>();
                if (grabbable != null && !grabbable.IsHeld)
                {
                    if (currentTarget != grabbable)
                    {
                        ClearCurrentTarget();
                        currentTarget = grabbable;
                        currentTarget.SetHighlighted(true);
                    }

                    ShowUIPrompt(currentTarget.GrabPromptText);
                    return;
                }
            }

            // Jika raycast tidak mengenai Grabbable
            ClearCurrentTarget();
            HideUIPrompt();
        }

        private void ClearCurrentTarget()
        {
            if (currentTarget != null)
            {
                currentTarget.SetHighlighted(false);
                currentTarget = null;
            }
        }

        private void HandleInput()
        {
            if (IsInteractKeyPressed())
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
        }

        private bool IsInteractKeyPressed()
        {
#if ENABLE_INPUT_SYSTEM
            if (Keyboard.current != null)
            {
                if (System.Enum.TryParse(interactKey.ToString(), out Key key))
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
                return Input.GetKeyDown(interactKey);
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
                Debug.LogWarning("[PlayerGrabbableInteraction] Gagal membuat grabHoldTransform secara realtime!", this);
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
            // Jika tidak mengenai permukaan apapun -> Tetap pada posisi grabHoldTransform (Drop Biasa)

            if (heldCollider != null)
            {
                heldCollider.enabled = wasColliderEnabled;
            }

            heldObject.Drop();
            heldObject = null;
            HideUIPrompt();
        }

        private void ShowUIPrompt(string promptMessage)
        {
            if (uiPromptPanel != null)
            {
                uiPromptPanel.SetActive(true);
            }

            if (uiPromptText != null)
            {
                uiPromptText.text = promptMessage;
            }
        }

        private void HideUIPrompt()
        {
            if (uiPromptPanel != null)
            {
                uiPromptPanel.SetActive(false);
            }
        }
    }
}
