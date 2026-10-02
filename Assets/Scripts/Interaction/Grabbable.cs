using System.Collections.Generic;
using UnityEngine;

namespace MoskowGameJam.Interaction
{
    [RequireComponent(typeof(Collider))]
    public class Grabbable : MonoBehaviour
    {
        [Header("Glow & Highlight Settings")]
        [SerializeField] private Color glowColor = Color.yellow;
        [SerializeField] private float glowIntensity = 1.5f;

        [Header("Prompt Settings")]
        [SerializeField] private string grabPromptText = "[E] Ambil";

        private Renderer[] objectRenderers;
        private MaterialPropertyBlock propBlock;
        private Rigidbody rb;
        private Collider col;
        private bool isHighlighted;
        private bool isHeld;

        private readonly Dictionary<Transform, int> originalLayers = new Dictionary<Transform, int>();

        public string GrabPromptText => grabPromptText;
        public bool IsHeld => isHeld;

        private void Awake()
        {
            objectRenderers = GetComponentsInChildren<Renderer>();
            propBlock = new MaterialPropertyBlock();
            rb = GetComponent<Rigidbody>();
            col = GetComponent<Collider>();
        }

        public void SetHighlighted(bool active)
        {
            if (isHeld) active = false;
            if (isHighlighted == active) return;

            isHighlighted = active;
            ApplyGlowEffect(active);
        }

        private void ApplyGlowEffect(bool active)
        {
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

        public void Grab(Transform holdPoint)
        {
            int defaultHeldLayer = LayerMask.NameToLayer("HeldObject");
            if (defaultHeldLayer < 0) defaultHeldLayer = 6; // Layer 6 fallback
            Grab(holdPoint, defaultHeldLayer);
        }

        public void Grab(Transform holdPoint, int heldLayer)
        {
            SetHighlighted(false);
            isHeld = true;

            SaveAndSetHeldLayer(heldLayer);

            if (rb != null)
            {
                rb.isKinematic = true;
                rb.useGravity = false;
            }

            if (col != null)
            {
                col.enabled = false;
            }

            transform.SetParent(holdPoint);
            transform.localPosition = Vector3.zero;
            transform.localRotation = Quaternion.identity;
        }

        public void Drop()
        {
            isHeld = false;
            transform.SetParent(null);

            RestoreOriginalLayers();

            if (rb != null)
            {
                rb.isKinematic = false;
                rb.useGravity = true;
            }

            if (col != null)
            {
                col.enabled = true;
            }
        }

        private void SaveAndSetHeldLayer(int heldLayer)
        {
            if (heldLayer < 0) return;

            originalLayers.Clear();
            SaveAndSetLayerRecursive(transform, heldLayer);
        }

        private void SaveAndSetLayerRecursive(Transform t, int heldLayer)
        {
            if (t == null) return;

            originalLayers[t] = t.gameObject.layer;
            t.gameObject.layer = heldLayer;

            for (int i = 0; i < t.childCount; i++)
            {
                SaveAndSetLayerRecursive(t.GetChild(i), heldLayer);
            }
        }

        private void RestoreOriginalLayers()
        {
            foreach (var kvp in originalLayers)
            {
                if (kvp.Key != null)
                {
                    kvp.Key.gameObject.layer = kvp.Value;
                }
            }
            originalLayers.Clear();
        }
    }
}
