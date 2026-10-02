using System.Collections.Generic;
using UnityEngine;
using UnityEngine.Events;
using MoskowGameJam.ToolInteraction.UI;

#if UNITY_2023_1_OR_NEWER
using Unity.Cinemachine;
#endif

namespace MoskowGameJam.ToolInteraction
{
    public class ToolInteractable : MonoBehaviour
    {
        [Header("Cinemachine Camera Settings")]
        [Tooltip("Cinemachine Camera khusus untuk alat ini")]
        [SerializeField] private GameObject toolCinemachineCamera;

        [Tooltip("Priority saat interaksi aktif")]
        [SerializeField] private int activePriority = 20;

        [Tooltip("Priority saat interaksi tidak aktif")]
        [SerializeField] private int inactivePriority = 0;

        [Header("Glow & Highlight Settings")]
        [SerializeField] private Color glowColor = Color.yellow;
        [SerializeField] private float glowIntensity = 1.5f;

        [Header("Dual & Exit UI Prompt Settings")]
        [Tooltip("Teks untuk tombol aksi interaksi [R]")]
        [SerializeField] private string actionPromptText = "Aduk Adonan";
        [SerializeField] private Sprite actionPromptSprite;

        [Tooltip("Teks untuk tombol penempatan objek [E]")]
        [SerializeField] private string placingPromptText = "Masukkan ke adonan";
        [SerializeField] private Sprite placingPromptSprite;

        [Tooltip("Teks untuk tombol selesai/keluar interaksi [R]")]
        [SerializeField] private string exitPromptText = "Selesai";
        [SerializeField] private Sprite exitPromptSprite;

        [Header("Object Container Settings")]
        [Tooltip("Daftar objek yang dimasukkan/ditempatkan ke dalam alat ini")]
        [SerializeField] private List<GameObject> placedObjectsList = new List<GameObject>();
        [SerializeField] private Transform itemPlacementPoint;

        [Header("Events")]
        public UnityEvent<GameObject> OnItemPlaced;
        public UnityEvent OnToolActionTriggered;
        public UnityEvent OnInteractionEnter;
        public UnityEvent OnInteractionExit;

        private Renderer[] objectRenderers;
        private MaterialPropertyBlock propBlock;
        private bool isGlowing;
        private IToolMiniGameInteraction miniGameInteraction;

        public string ActionPromptText => actionPromptText;
        public Sprite ActionPromptSprite => actionPromptSprite;
        public string PlacingPromptText => placingPromptText;
        public Sprite PlacingPromptSprite => placingPromptSprite;
        public string ExitPromptText => exitPromptText;
        public Sprite ExitPromptSprite => exitPromptSprite;
        public List<GameObject> PlacedObjectsList => placedObjectsList;
        public IToolMiniGameInteraction SpecificInteraction => miniGameInteraction;

        public string GetFormattedActionPrompt(KeyCode key) => PromptKeyFormatter.FormatPromptText(key, actionPromptText);
        public string GetFormattedPlacingPrompt(KeyCode key) => PromptKeyFormatter.FormatPromptText(key, placingPromptText);
        public string GetFormattedExitPrompt(KeyCode key) => PromptKeyFormatter.FormatPromptText(key, exitPromptText);

        private void Awake()
        {
            objectRenderers = GetComponentsInChildren<Renderer>();
            propBlock = new MaterialPropertyBlock();
            miniGameInteraction = GetComponent<IToolMiniGameInteraction>();

            SetCinemachinePriority(inactivePriority);
        }

        public void SetGlow(bool active)
        {
            if (isGlowing == active) return;
            isGlowing = active;

            if (objectRenderers == null) return;

            Color finalColor = active ? (glowColor * glowIntensity) : Color.black;

            foreach (Renderer rend in objectRenderers)
            {
                if (rend == null) continue;

                rend.GetPropertyBlock(propBlock);

                if (active)
                {
                    rend.material.EnableKeyword("_EMISSION");
                    propBlock.SetColor("_EmissionColor", finalColor);
                }
                else
                {
                    propBlock.SetColor("_EmissionColor", Color.black);
                }

                rend.SetPropertyBlock(propBlock);
            }
        }

        public void SetCameraActive(bool active)
        {
            int targetPriority = active ? activePriority : inactivePriority;
            SetCinemachinePriority(targetPriority);

            if (active)
            {
                OnInteractionEnter?.Invoke();
                miniGameInteraction?.OnBeginInteraction();
            }
            else
            {
                OnInteractionExit?.Invoke();
                miniGameInteraction?.OnEndInteraction();
            }
        }

        public bool AddItemToTool(GameObject item)
        {
            if (item == null) return false;

            if (!placedObjectsList.Contains(item))
            {
                placedObjectsList.Add(item);

                if (itemPlacementPoint == null)
                {
                    Transform existingIsi = transform.Find("IsiTool");
                    if (existingIsi != null)
                    {
                        itemPlacementPoint = existingIsi;
                    }
                    else
                    {
                        GameObject isiObj = new GameObject("IsiTool");
                        isiObj.transform.SetParent(transform, false);
                        isiObj.transform.localPosition = Vector3.zero;
                        isiObj.transform.localRotation = Quaternion.identity;
                        itemPlacementPoint = isiObj.transform;
                    }
                }

                item.transform.SetParent(itemPlacementPoint, false);
                item.transform.localPosition = Vector3.zero;
                item.transform.localRotation = Quaternion.identity;

                // Sembunyikan objek setelah dimasukkan ke alat (masuk ke dalam wadah)
                item.SetActive(false);

                OnItemPlaced?.Invoke(item);
                Debug.Log($"[ToolInteractable] Objek {item.name} berhasil dimasukkan ke {gameObject.name} (IsiTool). Total item: {placedObjectsList.Count}");
                return true;
            }

            return false;
        }

        public void TriggerAction()
        {
            OnToolActionTriggered?.Invoke();
            Debug.Log($"[ToolInteractable] Action dipicu pada {gameObject.name}");
        }

        private void SetCinemachinePriority(int priority)
        {
            if (toolCinemachineCamera == null) return;

#if UNITY_2023_1_OR_NEWER
            var cinemachineCam = toolCinemachineCamera.GetComponent<CinemachineCamera>();
            if (cinemachineCam != null)
            {
                cinemachineCam.Priority.Value = priority;
                return;
            }
#endif

            // Fallback via reflection atau parameter Priority standar Cinemachine
            Component vcam = toolCinemachineCamera.GetComponent("CinemachineVirtualCamera") ??
                             toolCinemachineCamera.GetComponent("CinemachineCamera");

            if (vcam != null)
            {
                var priorityProp = vcam.GetType().GetProperty("Priority");
                if (priorityProp != null)
                {
                    priorityProp.SetValue(vcam, priority);
                }
            }
        }
    }
}
