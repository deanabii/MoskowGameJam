using TMPro;
using UnityEngine;
using UnityEngine.UI;

namespace PhotoModeSystem
{
    public class SocialMediaPanelUI : MonoBehaviour
    {
        [Header("Profile Display")]
        public TextMeshProUGUI usernameText;
        public TextMeshProUGUI followerCountText;
        public TextMeshProUGUI likeCountText;
        public TextMeshProUGUI goldCountText;

        public Text fallbackUsernameText;
        public Text fallbackFollowerCountText;
        public Text fallbackLikeCountText;
        public Text fallbackGoldCountText;

        [Header("Feed Grid")]
        public Transform feedGridParent;
        public GameObject feedItemPrefab;

        [Header("Notifications")]
        public NotificationUIManager notificationUI;

        private void OnEnable()
        {
            if (SocialMediaManager.Instance != null)
            {
                SocialMediaManager.Instance.OnProfileStatsUpdated += RefreshUI;
            }
            RefreshUI();
        }

        private void OnDisable()
        {
            if (SocialMediaManager.Instance != null)
            {
                SocialMediaManager.Instance.OnProfileStatsUpdated -= RefreshUI;
            }
        }

        public void RefreshUI()
        {
            if (SocialMediaManager.Instance == null) return;

            string nameStr = SocialMediaManager.Instance.username;
            string followerStr = $"{SocialMediaManager.Instance.currentFollowers:N0}";
            string likeStr = $"{SocialMediaManager.Instance.totalLikes:N0}";
            string goldStr = $"{SocialMediaManager.Instance.currentGold:N0} Gold";

            if (usernameText != null) usernameText.text = nameStr;
            if (followerCountText != null) followerCountText.text = followerStr;
            if (likeCountText != null) likeCountText.text = likeStr;
            if (goldCountText != null) goldCountText.text = goldStr;

            if (fallbackUsernameText != null) fallbackUsernameText.text = nameStr;
            if (fallbackFollowerCountText != null) fallbackFollowerCountText.text = followerStr;
            if (fallbackLikeCountText != null) fallbackLikeCountText.text = likeStr;
            if (fallbackGoldCountText != null) fallbackGoldCountText.text = goldStr;

            RefreshFeedGrid();

            if (notificationUI != null)
            {
                notificationUI.RefreshNotifications();
            }
        }

        private void RefreshFeedGrid()
        {
            if (feedGridParent == null || SocialMediaManager.Instance == null) return;

            // Clear old items
            foreach (Transform child in feedGridParent)
            {
                Destroy(child.gameObject);
            }

            foreach (PhotoData photo in SocialMediaManager.Instance.postedPhotos)
            {
                GameObject itemObj;
                if (feedItemPrefab != null)
                {
                    itemObj = Instantiate(feedItemPrefab, feedGridParent);
                }
                else
                {
                    itemObj = CreateDefaultFeedItem();
                    itemObj.transform.SetParent(feedGridParent, false);
                }

                RawImage rawImg = itemObj.GetComponentInChildren<RawImage>();
                if (rawImg != null && PhotoStorageManager.Instance != null)
                {
                    Texture2D tex = PhotoStorageManager.Instance.LoadPhotoTexture(photo);
                    if (tex != null) rawImg.texture = tex;
                }
            }
        }

        private GameObject CreateDefaultFeedItem()
        {
            GameObject go = new GameObject("FeedGridItem", typeof(RectTransform), typeof(CanvasRenderer), typeof(Image));
            Image bg = go.GetComponent<Image>();
            bg.color = new Color(0.15f, 0.15f, 0.15f, 1f);

            GameObject childRaw = new GameObject("PhotoThumbnail", typeof(RectTransform), typeof(CanvasRenderer), typeof(RawImage));
            childRaw.transform.SetParent(go.transform, false);
            RectTransform rect = childRaw.GetComponent<RectTransform>();
            rect.anchorMin = Vector2.zero;
            rect.anchorMax = Vector2.one;
            rect.offsetMin = new Vector2(4, 4);
            rect.offsetMax = new Vector2(-4, -4);

            return go;
        }
    }
}
