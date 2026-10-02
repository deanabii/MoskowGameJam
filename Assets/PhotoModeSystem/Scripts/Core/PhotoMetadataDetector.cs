using System.Collections.Generic;
using UnityEngine;

namespace PhotoModeSystem
{
    public class PhotoMetadataDetector : MonoBehaviour
    {
        [Header("Detection Settings")]
        [Tooltip("Layer mask untuk pengecekan penghalang (occlusion check).")]
        public LayerMask obstacleLayerMask = ~0;

        private Camera targetCamera;

        public void Initialize(Camera cam)
        {
            targetCamera = cam;
        }

        public (List<string> visibleNames, float totalValue) DetectVisibleObjects()
        {
            if (targetCamera == null)
            {
                targetCamera = Camera.main;
            }

            List<string> names = new List<string>();
            float sumValue = 0f;

            if (targetCamera == null)
            {
                Debug.LogWarning("[PhotoMetadataDetector] Main Camera tidak ditemukan!");
                return (names, sumValue);
            }

            Plane[] frustumPlanes = GeometryUtility.CalculateFrustumPlanes(targetCamera);
            ObjectValue[] allObjects = FindObjectsOfType<ObjectValue>();

            foreach (ObjectValue obj in allObjects)
            {
                if (obj == null || !obj.gameObject.activeInHierarchy) continue;

                Bounds objBounds = obj.GetBounds();

                // 1. Frustum Check (Apakah Bounding Box masuk dalam sudut pandang kamera?)
                if (GeometryUtility.TestPlanesAABB(frustumPlanes, objBounds))
                {
                    // 2. Occlusion Check (Apakah titik pusat objek terhalang dinding/penghalang?)
                    Vector3 camPos = targetCamera.transform.position;
                    Vector3 targetPos = obj.GetWorldCenter();
                    Vector3 direction = targetPos - camPos;
                    float distance = direction.magnitude;

                    if (!Physics.Raycast(camPos, direction.normalized, out RaycastHit hit, distance, obstacleLayerMask))
                    {
                        names.Add(obj.objectName);
                        sumValue += obj.valueAmount;
                    }
                    else if (hit.transform == obj.transform || hit.transform.IsChildOf(obj.transform))
                    {
                        names.Add(obj.objectName);
                        sumValue += obj.valueAmount;
                    }
                }
            }

            return (names, sumValue);
        }
    }
}
