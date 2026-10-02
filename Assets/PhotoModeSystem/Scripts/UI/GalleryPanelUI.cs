using System.Collections.Generic;
using UnityEngine;
using UnityEngine.UI;

namespace PhotoModeSystem
{
    public class GalleryPanelUI : MonoBehaviour
    {
        [Header("Grid References")]
        public Transform gridContentParent;
        public GameObject photoItemPrefab;

        [Header("Detail View Modal")]
        public PhotoDetailUI photoDetailModal;

        private List<GameObject> spawnedItems = new List<GameObject>();

        private void OnEnable()
        {
            RefreshGalleryGrid();
        }

        public void RefreshGalleryGrid()
        {
            // Clear existing items
            foreach (GameObject obj in spawnedItems)
            {
                if (obj != null) Destroy(obj);
            }
            spawnedItems.Clear();

            if (PhotoStorageManager.Instance == null || gridContentParent == null) return;

            List<PhotoData> allPhotos = PhotoStorageManager.Instance.LoadAllPhotos();

            foreach (PhotoData data in allPhotos)
            {
                GameObject itemObj;
                if (photoItemPrefab != null)
                {
                    itemObj = Instantiate(photoItemPrefab, gridContentParent);
                }
                else
                {
                    // Fallback item creation dynamically
                    itemObj = CreateDefaultGridItem();
                    itemObj.transform.SetParent(gridContentParent, false);
                }

                spawnedItems.Add(itemObj);

                RawImage rawImg = itemObj.GetComponentInChildren<RawImage>();
                Texture2D tex = PhotoStorageManager.Instance.LoadPhotoTexture(data);
                if (rawImg != null && tex != null)
                {
                    rawImg.texture = tex;
                }

                Button btn = itemObj.GetComponent<Button>();
                if (btn == null) btn = itemObj.AddComponent<Button>();

                PhotoData currentData = data;
                btn.onClick.RemoveAllListeners();
                btn.onClick.AddListener(() =>
                {
                    if (photoDetailModal != null)
                    {
                        photoDetailModal.OpenDetail(currentData, tex);
                    }
                });
            }
        }

        private GameObject CreateDefaultGridItem()
        {
            GameObject go = new GameObject("PhotoGridItem", typeof(RectTransform), typeof(CanvasRenderer), typeof(Image), typeof(Button));
            Image bg = go.GetComponent<Image>();
            bg.color = new Color(0.2f, 0.2f, 0.2f, 1f);

            GameObject childRaw = new GameObject("Thumbnail", typeof(RectTransform), typeof(CanvasRenderer), typeof(RawImage));
            childRaw.transform.SetParent(go.transform, false);
            RectTransform rect = childRaw.GetComponent<RectTransform>();
            rect.anchorMin = Vector2.zero;
            rect.anchorMax = Vector2.one;
            rect.offsetMin = new Vector2(5, 5);
            rect.offsetMax = new Vector2(-5, -5);

            return go;
        }
    }
}
