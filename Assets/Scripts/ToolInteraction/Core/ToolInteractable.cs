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
        [SerializeField] private int activePriority = 100;

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

        [Header("Score Settings")]
        [Tooltip("Daftar nilai hasil interaksi / minigame (0.0 - 1.0)")]
        [SerializeField] private List<float> interactionScores = new List<float>();

        [Tooltip("Nilai akhir (rata-rata dari semua interactionScores)")]
        [SerializeField] private float finalAverageScore;

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
        public List<float> InteractionScores => interactionScores;
        public float FinalAverageScore => finalAverageScore;
        public IToolMiniGameInteraction SpecificInteraction => miniGameInteraction;

        public void AddScore(float score)
        {
            interactionScores.Add(score);
            RecalculateAverageScore();
            Debug.Log($"<color=green>[ToolInteractable] Nilai baru ditambahkan ke {gameObject.name}: {score:F2} | Rata-rata Nilai Akhir: {finalAverageScore:F2} (Total data: {interactionScores.Count})</color>");
        }

        public float RecalculateAverageScore()
        {
            if (interactionScores == null || interactionScores.Count == 0)
            {
                finalAverageScore = 0f;
                return 0f;
            }

            float sum = 0f;
            foreach (float s in interactionScores)
            {
                sum += s;
            }

            finalAverageScore = sum / interactionScores.Count;
            return finalAverageScore;
        }

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
            EnsureToolCameraExists();

            if (toolCinemachineCamera != null)
            {
                toolCinemachineCamera.SetActive(true);
            }

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

        private void EnsureToolCameraExists()
        {
            if (toolCinemachineCamera != null) return;

            Transform existingCam = transform.Find("[ToolCinemachineCamera]");
            if (existingCam != null)
            {
                toolCinemachineCamera = existingCam.gameObject;
                return;
            }

            GameObject vcamObj = new GameObject("[ToolCinemachineCamera]");
            vcamObj.transform.SetParent(transform, false);
            vcamObj.transform.localPosition = new Vector3(0f, 0.5f, -1.5f);
            vcamObj.transform.localRotation = Quaternion.Euler(15f, 0f, 0f);

#if UNITY_2023_1_OR_NEWER
            var vcam = vcamObj.AddComponent<Unity.Cinemachine.CinemachineCamera>();
            vcam.Priority.Value = inactivePriority;
#else
            Component vcam = vcamObj.AddComponent(System.Type.GetType("Cinemachine.CinemachineVirtualCamera, Unity.Cinemachine") ?? System.Type.GetType("Cinemachine.CinemachineVirtualCamera, Cinemachine"));
#endif

            toolCinemachineCamera = vcamObj;
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
                cinemachineCam.Priority.Enabled = true;
                cinemachineCam.Priority.Value = priority;
                Debug.Log($"<color=cyan>[ToolInteractable] Set CinemachineCamera priority pada {toolCinemachineCamera.name} ke {priority}</color>");
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
                    if (priorityProp.PropertyType == typeof(int))
                    {
                        priorityProp.SetValue(vcam, priority);
                    }
                    else
                    {
                        var prioObj = priorityProp.GetValue(vcam);
                        if (prioObj != null)
                        {
                            var enabledProp = prioObj.GetType().GetProperty("Enabled");
                            if (enabledProp != null) enabledProp.SetValue(prioObj, true);

                            var valProp = prioObj.GetType().GetProperty("Value");
                            if (valProp != null) valProp.SetValue(prioObj, priority);

                            var valField = prioObj.GetType().GetField("Value");
                            if (valField != null) valField.SetValue(prioObj, priority);

                            priorityProp.SetValue(vcam, prioObj);
                        }
                    }
                }
                Debug.Log($"<color=cyan>[ToolInteractable] Set reflection camera priority pada {toolCinemachineCamera.name} ke {priority}</color>");
            }
            else
            {
                Debug.LogWarning($"[ToolInteractable] Tidak ditemukan komponen CinemachineCamera atau CinemachineVirtualCamera di {toolCinemachineCamera.name}");
            }
        }
    }
}
