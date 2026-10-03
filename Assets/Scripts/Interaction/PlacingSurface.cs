using UnityEngine;

namespace MoskowGameJam.Interaction
{
    public enum LocalAxis
    {
        Up,
        Down,
        Forward,
        Back,
        Right,
        Left
    }

    [DisallowMultipleComponent]
    [RequireComponent(typeof(Collider))]
    public class PlacingSurface : MonoBehaviour
    {
        [Header("Local Placement Axis Settings")]
        [Tooltip("Sumbu lokal permukaan yang diizinkan untuk menaruh objek (contoh: Up = transform.up)")]
        [SerializeField] private LocalAxis localPlacementAxis = LocalAxis.Up;

        [Range(0f, 90f)]
        [Tooltip("Toleransi sudut maksimum (derajat) dari sumbu lokal yang ditentukan")]
        [SerializeField] private float maxAngleToleranceDegrees = 45f;

        [Header("Placement Offset & Alignment")]
        [Tooltip("Offset tambahan posisi penempatan pada permukaan (dalam local offset atau normal offset)")]
        [SerializeField] private Vector3 surfaceOffset = Vector3.zero;

        [Tooltip("Offset rotasi tambahan (Euler X, Y, Z dalam derajat) untuk objek yang ditaruh di permukaan ini")]
        [SerializeField] private Vector3 surfaceRotationOffset = Vector3.zero;

        [Tooltip("Apakah rotasi objek/impostor akan otomatis diputar mengikuti sudut normal permukaan?")]
        [SerializeField] private bool alignObjectRotationToNormal = true;

        public LocalAxis LocalPlacementAxis => localPlacementAxis;
        public Vector3 SurfaceOffset => surfaceOffset;
        public Vector3 SurfaceRotationOffset => surfaceRotationOffset;
        public bool AlignObjectRotationToNormal => alignObjectRotationToNormal;

        /// <summary>
        /// Mengembalikan vektor arah penempatan yang diizinkan dalam ruang dunia global (World Space)
        /// berdasarkan rotasi Transform lokal saat ini.
        /// </summary>
        public Vector3 GetWorldAllowedDirection()
        {
            Vector3 localVec = localPlacementAxis switch
            {
                LocalAxis.Up => Vector3.up,
                LocalAxis.Down => Vector3.down,
                LocalAxis.Forward => Vector3.forward,
                LocalAxis.Back => Vector3.back,
                LocalAxis.Right => Vector3.right,
                LocalAxis.Left => Vector3.left,
                _ => Vector3.up
            };

            return transform.TransformDirection(localVec);
        }

        /// <summary>
        /// Memeriksa apakah hit.normal benturan cocok dengan arah sumbu lokal permukaan yang diizinkan
        /// (mempertimbangkan toleransi sudut).
        /// </summary>
        public bool IsValidHitNormal(Vector3 hitNormal)
        {
            Vector3 allowedWorldDir = GetWorldAllowedDirection().normalized;
            float dot = Vector3.Dot(hitNormal.normalized, allowedWorldDir);
            float minDotThreshold = Mathf.Cos(maxAngleToleranceDegrees * Mathf.Deg2Rad);
            return dot >= minDotThreshold;
        }

        private void OnDrawGizmosSelected()
        {
            Gizmos.color = Color.cyan;
            Vector3 worldDir = GetWorldAllowedDirection();
            Gizmos.DrawRay(transform.position, worldDir * 1.0f);
        }
    }
}
