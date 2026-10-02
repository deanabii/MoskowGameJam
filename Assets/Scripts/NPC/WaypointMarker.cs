using UnityEngine;

namespace MoskowGameJam.NPC
{
    [SelectionBase]
    public class WaypointMarker : MonoBehaviour
    {
        [Header("Gizmo Visuals")]
        [Tooltip("Warna bola waypoint di Scene view")]
        [SerializeField] private Color markerColor = new Color(0f, 0.8f, 1f, 0.9f);

        [Tooltip("Ukuran radius bola waypoint")]
        [SerializeField] private float radius = 0.35f;

        private void OnDrawGizmos()
        {
            Gizmos.color = markerColor;
            Gizmos.DrawSphere(transform.position + Vector3.up * 0.15f, radius);
            Gizmos.DrawWireSphere(transform.position + Vector3.up * 0.15f, radius * 1.25f);
        }
    }
}
