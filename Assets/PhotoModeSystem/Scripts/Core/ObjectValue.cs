using MoskowGameJam.ToolInteraction;
using MoskowGameJam.ToolInteraction.SpecificInteractions;
using UnityEngine;

namespace MoskowGameJam.Interaction
{
    [DisallowMultipleComponent]
    public class ObjectValue : MonoBehaviour
    {
        public ToolInteractable tool;
        [Header("Object Value Metadata")]
        [Tooltip("Nama objek yang akan dicatat pada metadata foto.")]
        public string objectName = "Special Object";

        [Tooltip("Nilai / poin yang disumbangkan objek ini jika terpotret.")]
        public float valueAmount = 100f;
        public float maxvalue = 300f;

        [Header("Detection Settings")]
        [Tooltip("Offset pusat objek untuk pengecekan raycast pandangan kamera.")]
        public Vector3 boundsCenterOffset = Vector3.zero;

        private void Start()
        {
            if (tool!=null)
            {
                valueAmount = maxvalue * tool.FinalAverageScore;
            }
        }
        private void Update()
        {
            if (tool != null)
            {
                valueAmount = maxvalue * tool.FinalAverageScore;
            }
        }
        public Vector3 GetWorldCenter()
        {
            Collider col = GetComponent<Collider>();
            if (col != null)
            {
                return col.bounds.center;
            }
            Renderer rend = GetComponent<Renderer>();
            if (rend != null)
            {
                return rend.bounds.center;
            }
            return transform.position + boundsCenterOffset;
        }

        public Bounds GetBounds()
        {
            Collider col = GetComponent<Collider>();
            if (col != null)
            {
                return col.bounds;
            }
            Renderer rend = GetComponent<Renderer>();
            if (rend != null)
            {
                return rend.bounds;
            }
            return new Bounds(transform.position + boundsCenterOffset, Vector3.one);
        }
    }
}
