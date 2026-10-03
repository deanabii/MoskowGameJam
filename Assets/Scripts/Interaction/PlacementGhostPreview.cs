using System.Collections.Generic;
using UnityEngine;

namespace MoskowGameJam.Interaction
{
    public class PlacementGhostPreview : MonoBehaviour
    {
        private GameObject ghostInstance;
        private Material ghostMaterial;
        private float cachedBottomOffset;
        private Grabbable currentSourceGrabbable;

        private void OnDestroy()
        {
            DestroyGhost();
            if (ghostMaterial != null)
            {
                Destroy(ghostMaterial);
            }
        }

        public Material GetOrCreateGhostMaterial(Material customOverrideMaterial = null)
        {
            if (customOverrideMaterial != null)
            {
                return customOverrideMaterial;
            }

            if (ghostMaterial == null)
            {
                // Buat Material Transparan Biru (Impostor Material)
                Shader targetShader = Shader.Find("Universal Render Pipeline/Lit") ??
                                     Shader.Find("Standard") ??
                                     Shader.Find("Sprites/Default");

                ghostMaterial = new Material(targetShader);
                ghostMaterial.name = "GhostPreview_BlueTransparent";

                Color ghostColor = new Color(0.2f, 0.65f, 1.0f, 0.55f);

                if (ghostMaterial.HasProperty("_BaseColor"))
                {
                    ghostMaterial.SetColor("_BaseColor", ghostColor);
                }
                else if (ghostMaterial.HasProperty("_Color"))
                {
                    ghostMaterial.SetColor("_Color", ghostColor);
                }

                // Pengaturan Blend Mode Transparan
                if (ghostMaterial.HasProperty("_Surface")) ghostMaterial.SetFloat("_Surface", 1f); // Transparent surface in URP
                if (ghostMaterial.HasProperty("_Blend")) ghostMaterial.SetFloat("_Blend", 0f); // Alpha blend in URP

                ghostMaterial.SetInt("_SrcBlend", (int)UnityEngine.Rendering.BlendMode.SrcAlpha);
                ghostMaterial.SetInt("_DstBlend", (int)UnityEngine.Rendering.BlendMode.OneMinusSrcAlpha);
                ghostMaterial.SetInt("_ZWrite", 0);
                ghostMaterial.DisableKeyword("_ALPHATEST_ON");
                ghostMaterial.EnableKeyword("_ALPHABLEND_ON");
                ghostMaterial.DisableKeyword("_ALPHAPREMULTIPLY_ON");
                ghostMaterial.renderQueue = (int)UnityEngine.Rendering.RenderQueue.Transparent;
            }

            return ghostMaterial;
        }

        /// <summary>
        /// Membuat atau memperbarui Impostor Ghost Preview untuk objek yang sedang dipegang.
        /// </summary>
        public void UpdateGhost(
            Grabbable sourceObject, 
            Vector3 hitPoint, 
            Vector3 hitNormal, 
            PlacingSurface surface, 
            Camera playerCamera,
            Material customMaterial = null,
            Vector3 extraRotationOffset = default)
        {
            if (sourceObject == null || surface == null)
            {
                DestroyGhost();
                return;
            }

            // Jika objek yang dipegang berganti, buat ulang Ghost Instance
            if (currentSourceGrabbable != sourceObject || ghostInstance == null)
            {
                CreateGhostInstance(sourceObject, customMaterial);
            }

            if (ghostInstance == null) return;

            if (!ghostInstance.activeSelf)
            {
                ghostInstance.SetActive(true);
            }

            // Hitung Rotasi Alignment
            Quaternion finalRotation = CalculatePlacementRotation(sourceObject, hitNormal, surface, playerCamera, extraRotationOffset);

            // Hitung Posisi dengan Offset Bottom Bounds & Surface Offset
            Vector3 positionOffset = (hitNormal.normalized * 0.01f) + surface.SurfaceOffset;
            Vector3 targetPosition = hitPoint + (hitNormal.normalized * cachedBottomOffset) + positionOffset;

            // Apply Transform ke Ghost Preview
            ghostInstance.transform.position = targetPosition;
            ghostInstance.transform.rotation = finalRotation;
        }

        public Quaternion CalculatePlacementRotation(
            Grabbable sourceObject, 
            Vector3 hitNormal, 
            PlacingSurface surface, 
            Camera playerCamera,
            Vector3 extraRotationOffset = default)
        {
            Quaternion baseRotation = Quaternion.identity;

            if (surface != null && surface.AlignObjectRotationToNormal)
            {
                // Align sumbu Vector3.up menuju hit.normal permukaan
                Quaternion normalAlign = Quaternion.FromToRotation(Vector3.up, hitNormal.normalized);

                // Ambil rotasi Yaw dari kamera player / objek
                float playerYaw = playerCamera != null ? playerCamera.transform.eulerAngles.y : (sourceObject != null ? sourceObject.transform.eulerAngles.y : 0f);
                Quaternion yawRot = Quaternion.Euler(0f, playerYaw, 0f);

                baseRotation = normalAlign * yawRot;
            }
            else
            {
                float playerYaw = playerCamera != null ? playerCamera.transform.eulerAngles.y : (sourceObject != null ? sourceObject.transform.eulerAngles.y : 0f);
                baseRotation = Quaternion.Euler(0f, playerYaw, 0f);
            }

            // Terapkan Surface Rotation Offset jika ada pada permukaan
            if (surface != null && surface.SurfaceRotationOffset != Vector3.zero)
            {
                baseRotation *= Quaternion.Euler(surface.SurfaceRotationOffset);
            }

            // Terapkan Grabbable Placement Rotation Offset jika ada pada objek yang dipegang
            if (sourceObject != null && sourceObject.PlacementRotationOffset != Vector3.zero)
            {
                baseRotation *= Quaternion.Euler(sourceObject.PlacementRotationOffset);
            }

            // Terapkan Extra / Global / Live Manual Rotation Offset
            if (extraRotationOffset != Vector3.zero)
            {
                baseRotation *= Quaternion.Euler(extraRotationOffset);
            }

            return baseRotation;
        }

        public void DestroyGhost()
        {
            if (ghostInstance != null)
            {
                Destroy(ghostInstance);
                ghostInstance = null;
            }
            currentSourceGrabbable = null;
        }

        public bool HasActiveGhost => ghostInstance != null && ghostInstance.activeSelf;
        public Vector3 GhostPosition => ghostInstance != null ? ghostInstance.transform.position : Vector3.zero;
        public Quaternion GhostRotation => ghostInstance != null ? ghostInstance.transform.rotation : Quaternion.identity;

        private void CreateGhostInstance(Grabbable sourceObject, Material customMaterial)
        {
            DestroyGhost();

            currentSourceGrabbable = sourceObject;
            ghostInstance = Instantiate(sourceObject.gameObject);
            ghostInstance.name = $"[GhostPreview]_{sourceObject.name}";

            // Stripping: Hapus semua Komponen Physics, Collider, dan Scripts dari Ghost Clone
            StripNonVisualComponents(ghostInstance);

            // Ganti seluruh Material pada Renderer dengan Impostor Material (Transparan Biru)
            Material mat = GetOrCreateGhostMaterial(customMaterial);
            ApplyMaterialRecursive(ghostInstance.transform, mat);

            // Hitung Bottom Bounds Offset agar alas objek pas duduk di permukaan
            cachedBottomOffset = CalculateBottomBoundsOffset(ghostInstance);

            ghostInstance.SetActive(true);
        }

        private void StripNonVisualComponents(GameObject obj)
        {
            // 1. Hapus semua MonoBehaviours / Scripts TERLEBIH DAHULU 
            // agar atribut [RequireComponent] tidak menghalangi penghapusan Collider
            MonoBehaviour[] scripts = obj.GetComponentsInChildren<MonoBehaviour>(true);
            foreach (MonoBehaviour script in scripts)
            {
                if (script != null && !(script is PlacementGhostPreview))
                {
                    DestroyImmediate(script);
                }
            }

            // 2. Hapus Joint
            Joint[] joints = obj.GetComponentsInChildren<Joint>(true);
            foreach (Joint j in joints)
            {
                if (j != null) DestroyImmediate(j);
            }

            // 3. Hapus Rigidbody
            Rigidbody[] rigidbodies = obj.GetComponentsInChildren<Rigidbody>(true);
            foreach (Rigidbody r in rigidbodies)
            {
                if (r != null) DestroyImmediate(r);
            }

            // 4. Hapus Collider setelah script dependencies sudah dihancurkan
            Collider[] colliders = obj.GetComponentsInChildren<Collider>(true);
            foreach (Collider c in colliders)
            {
                if (c != null) DestroyImmediate(c);
            }
        }

        private void ApplyMaterialRecursive(Transform t, Material mat)
        {
            if (t == null) return;

            Renderer rend = t.GetComponent<Renderer>();
            if (rend != null)
            {
                Material[] materials = new Material[rend.sharedMaterials.Length];
                for (int i = 0; i < materials.Length; i++)
                {
                    materials[i] = mat;
                }
                rend.materials = materials;
            }

            for (int i = 0; i < t.childCount; i++)
            {
                ApplyMaterialRecursive(t.GetChild(i), mat);
            }
        }

        private float CalculateBottomBoundsOffset(GameObject obj)
        {
            Renderer[] renderers = obj.GetComponentsInChildren<Renderer>();
            if (renderers == null || renderers.Length == 0) return 0f;

            Bounds combinedBounds = renderers[0].bounds;
            for (int i = 1; i < renderers.Length; i++)
            {
                combinedBounds.Encapsulate(renderers[i].bounds);
            }

            // Distance from pivot (center/position) to bottom of bounds (min.y)
            float pivotY = obj.transform.position.y;
            float bottomY = combinedBounds.min.y;
            float offset = Mathf.Max(0f, pivotY - bottomY);

            return offset;
        }
    }
}
