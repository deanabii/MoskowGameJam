using System.Collections.Generic;
using TMPro;
using UnityEngine;
using UnityEngine.UI;

namespace PhotoModeSystem
{
    public class NotificationUIManager : MonoBehaviour
    {
        [Header("UI References")]
        public Transform notificationContainer;
        public GameObject notificationItemPrefab;

        public void RefreshNotifications()
        {
            if (notificationContainer == null || SocialMediaManager.Instance == null) return;

            foreach (Transform child in notificationContainer)
            {
                Destroy(child.gameObject);
            }

            List<NotificationData> list = SocialMediaManager.Instance.notifications;
            foreach (NotificationData item in list)
            {
                GameObject notifObj;
                if (notificationItemPrefab != null)
                {
                    notifObj = Instantiate(notificationItemPrefab, notificationContainer);
                }
                else
                {
                    notifObj = CreateDefaultNotificationItem(item.message);
                    notifObj.transform.SetParent(notificationContainer, false);
                }

                TextMeshProUGUI tmpText = notifObj.GetComponentInChildren<TextMeshProUGUI>();
                if (tmpText != null) tmpText.text = $"[{item.timestamp}] {item.message}";

                Text txt = notifObj.GetComponentInChildren<Text>();
                if (txt != null) txt.text = $"[{item.timestamp}] {item.message}";
            }
        }

        private GameObject CreateDefaultNotificationItem(string message)
        {
            GameObject go = new GameObject("NotifItem", typeof(RectTransform), typeof(CanvasRenderer), typeof(Image));
            Image bg = go.GetComponent<Image>();
            bg.color = new Color(0.1f, 0.3f, 0.2f, 0.8f);

            GameObject textObj = new GameObject("Text", typeof(RectTransform), typeof(CanvasRenderer), typeof(Text));
            textObj.transform.SetParent(go.transform, false);
            Text txt = textObj.GetComponent<Text>();
            txt.font = Resources.GetBuiltinResource<Font>("LegacyRuntime.ttf");
            txt.fontSize = 14;
            txt.color = Color.white;
            txt.alignment = TextAnchor.MiddleLeft;

            RectTransform rect = textObj.GetComponent<RectTransform>();
            rect.anchorMin = Vector2.zero;
            rect.anchorMax = Vector2.one;
            rect.offsetMin = new Vector2(10, 5);
            rect.offsetMax = new Vector2(-10, -5);

            return go;
        }
    }
}
