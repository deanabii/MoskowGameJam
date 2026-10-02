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
            SetHighlighted(false);
            isHeld = true;

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
    }
}
