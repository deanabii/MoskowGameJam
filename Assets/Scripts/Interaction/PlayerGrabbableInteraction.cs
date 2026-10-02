using UnityEngine;
using UnityEngine.UI;
using TMPro;

#if ENABLE_INPUT_SYSTEM
using UnityEngine.InputSystem;
#endif

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

        [Header("Interaction Settings")]
        [SerializeField] private float interactDistance = 3.5f;
        [SerializeField] private LayerMask interactableLayer = ~0;
        [SerializeField] private KeyCode interactKey = KeyCode.E;

        [Header("UI Prompt References")]
        [Tooltip("Root Canvas UI Panel untuk menampung Sprite dan Teks")]
        [SerializeField] private GameObject uiPromptPanel;

        [Tooltip("Sprite/Image container UI")]
        [SerializeField] private Image uiPromptImage;

        [Tooltip("Teks UI penanda aksi interaksi (TextMeshPro)")]
        [SerializeField] private TMP_Text uiPromptText;

        private Grabbable currentTarget;
        private Grabbable heldObject;

        private void Awake()
        {
            AutoAssignReferences();
            EnsureGrabHoldTransform();
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
            heldObject.Grab(grabHoldTransform);
            ClearCurrentTarget();
        }

        private void DropObject()
        {
            if (heldObject != null)
            {
                heldObject.Drop();
                heldObject = null;
                HideUIPrompt();
            }
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
