using System.Text;
using TMPro;
using UnityEngine;
using UnityEngine.UI;

namespace PhotoModeSystem
{
    public class PhotoDetailUI : MonoBehaviour
    {
        [Header("UI Display")]
        public GameObject modalRoot;
        public RawImage zoomedRawImage;
        public TextMeshProUGUI metadataText;
        public Text fallbackMetadataText;

        [Header("Tools / Action Buttons")]
        public Button deleteButton;
        public Button shareButton;
        public Button backButton;
        public Button closeButtonX;

        [Header("References")]
        public MainTabMenuUI tabMenuUI;
        public GalleryPanelUI galleryPanel;

        private PhotoData currentPhotoData;
        private Texture2D currentTexture;

        private void Start()
        {
            if (modalRoot != null) modalRoot.SetActive(false);

            if (deleteButton != null) deleteButton.onClick.AddListener(OnClickDelete);
            if (shareButton != null) shareButton.onClick.AddListener(OnClickShare);
            if (backButton != null) backButton.onClick.AddListener(CloseDetail);
            if (closeButtonX != null) closeButtonX.onClick.AddListener(CloseDetail);
        }

        public void OpenDetail(PhotoData data, Texture2D tex)
        {
            currentPhotoData = data;
            currentTexture = tex;

            if (modalRoot != null) modalRoot.SetActive(true);

            if (zoomedRawImage != null)
            {
                zoomedRawImage.texture = currentTexture;
            }

            StringBuilder sb = new StringBuilder();
            sb.AppendLine($"<b>Tanggal:</b> {data.timestamp}");
            sb.AppendLine($"<b>Poin Value:</b> {data.totalValue:N0}");
            sb.AppendLine("<b>Objek Terpotret:</b>");
            if (data.visibleObjectNames != null && data.visibleObjectNames.Count > 0)
            {
                foreach (string name in data.visibleObjectNames)
                {
                    sb.AppendLine($" • {name}");
                }
            }
            else
            {
                sb.AppendLine(" • <i>(Tidak ada objek ObjectValue)</i>");
            }

            if (metadataText != null) metadataText.text = sb.ToString();
            if (fallbackMetadataText != null) fallbackMetadataText.text = sb.ToString();
        }

        public void CloseDetail()
        {
            if (modalRoot != null) modalRoot.SetActive(false);
            currentPhotoData = null;
            currentTexture = null;
        }

        private void OnClickDelete()
        {
            if (currentPhotoData == null) return;

            if (PhotoStorageManager.Instance != null)
            {
                PhotoStorageManager.Instance.DeletePhoto(currentPhotoData);
            }

            CloseDetail();

            if (galleryPanel != null)
            {
                galleryPanel.RefreshGalleryGrid();
            }
        }

        private void OnClickShare()
        {
            if (currentPhotoData == null) return;

            if (SocialMediaManager.Instance != null)
            {
                SocialMediaManager.Instance.SharePhoto(currentPhotoData);
            }

            CloseDetail();

            if (tabMenuUI != null)
            {
                tabMenuUI.OpenSocialTab();
            }
        }
    }
}
