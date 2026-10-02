using System;
using System.Collections.Generic;
using UnityEngine;

namespace PhotoModeSystem
{
    [Serializable]
    public class NotificationData
    {
        public string timestamp;
        public int gainedFollowers;
        public int gainedLikes;
        public int gainedGold;
        public string message;

        public NotificationData()
        {
            timestamp = DateTime.Now.ToString("HH:mm");
        }
    }

    public class SocialMediaManager : MonoBehaviour
    {
        public static SocialMediaManager Instance { get; private set; }

        [Header("Profile Info")]
        public string username = "PlayerCreator";
        public int currentFollowers = 100;
        public int totalLikes = 250;
        public int currentGold = 500;

        [Header("Configuration")]
        public PhotoModeConfig config;

        [Header("Shared Posts & Notifications")]
        public List<PhotoData> postedPhotos = new List<PhotoData>();
        public List<NotificationData> notifications = new List<NotificationData>();

        public event Action OnProfileStatsUpdated;
        public event Action<NotificationData> OnNotificationAdded;

        private void Awake()
        {
            if (Instance == null)
            {
                Instance = this;
                DontDestroyOnLoad(gameObject);
            }
            else
            {
                Destroy(gameObject);
                return;
            }

            if (config == null)
            {
                config = ScriptableObject.CreateInstance<PhotoModeConfig>();
            }
        }

        public void SharePhoto(PhotoData photo)
        {
            if (photo == null || postedPhotos.Contains(photo)) return;

            postedPhotos.Add(photo);

            // 1. Hitung Penambahan Followers (Min 1/2 hingga 3/4 dari Value)
            float followerRatio = UnityEngine.Random.Range(config.minFollowerRatio, config.maxFollowerRatio);
            int newFollowers = Mathf.Max(1, Mathf.RoundToInt(photo.totalValue * followerRatio));
            currentFollowers += newFollowers;

            // 2. Hitung Penambahan Likes (Min 3/4 hingga 2.0 dari Total Followers)
            float likeRatio = UnityEngine.Random.Range(config.minLikeRatio, config.maxLikeRatio);
            int newLikes = Mathf.Max(1, Mathf.RoundToInt(currentFollowers * likeRatio));
            totalLikes += newLikes;

            // 3. Hitung Donasi Gold (Min 3/4 hingga 2.0 dikali baseMultiplier dikali Total Followers)
            float goldRatio = UnityEngine.Random.Range(config.minGoldRatio, config.maxGoldRatio);
            int newGold = Mathf.Max(10, Mathf.RoundToInt(goldRatio * config.goldBaseMultiplier * (currentFollowers / 10f)));
            currentGold += newGold;

            // 4. Buat Notifikasi Baru
            NotificationData notif = new NotificationData
            {
                gainedFollowers = newFollowers,
                gainedLikes = newLikes,
                gainedGold = newGold,
                message = $"Foto di-share! +{newFollowers} Followers, +{newLikes} Likes, +{newGold} Gold Donasi!"
            };

            notifications.Insert(0, notif);

            OnProfileStatsUpdated?.Invoke();
            OnNotificationAdded?.Invoke(notif);

            Debug.Log($"[SocialMediaManager] Photo Shared! New Followers: +{newFollowers}, Likes: +{newLikes}, Gold: +{newGold}");
        }
    }
}
