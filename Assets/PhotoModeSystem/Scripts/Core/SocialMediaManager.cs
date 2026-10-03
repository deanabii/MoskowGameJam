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

        // Melacak berapa kali setiap objek telah diposting ke sosial media
        private Dictionary<string, int> sharedObjectCounts = new Dictionary<string, int>();

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

        public int GetObjectSharedCount(string objectName)
        {
            if (string.IsNullOrEmpty(objectName)) return 0;
            if (sharedObjectCounts.TryGetValue(objectName, out int count))
            {
                return count;
            }
            return 0;
        }

        public float CalculateEffectivePhotoValue(PhotoData photo)
        {
            if (photo == null) return 0f;

            float effectiveValue = 0f;

            if (photo.visibleObjects != null && photo.visibleObjects.Count > 0)
            {
                foreach (var entry in photo.visibleObjects)
                {
                    if (string.IsNullOrEmpty(entry.objectName)) continue;

                    int count = GetObjectSharedCount(entry.objectName);
                    float multiplier = 0f;
                    if (count == 0)
                    {
                        multiplier = 1.0f; // Ke-1: Nilai Penuh (100%)
                    }
                    else if (count == 1)
                    {
                        multiplier = 0.5f; // Ke-2: Setengah Nilai (50%)
                    }
                    else
                    {
                        multiplier = 0.0f; // Ke-3 dst: Nilai 0 (0%)
                    }

                    effectiveValue += entry.valueAmount * multiplier;
                }
            }
            else if (photo.visibleObjectNames != null && photo.visibleObjectNames.Count > 0)
            {
                float avgValue = photo.totalValue / photo.visibleObjectNames.Count;
                foreach (string name in photo.visibleObjectNames)
                {
                    if (string.IsNullOrEmpty(name)) continue;

                    int count = GetObjectSharedCount(name);
                    float multiplier = count == 0 ? 1.0f : (count == 1 ? 0.5f : 0.0f);
                    effectiveValue += avgValue * multiplier;
                }
            }
            else
            {
                effectiveValue = photo.totalValue;
            }

            return effectiveValue;
        }

        public void SharePhoto(PhotoData photo)
        {
            if (photo == null || postedPhotos.Contains(photo)) return;

            // 1. Hitung Nilai Efektif Foto berdasarkan aturan repitisi objek (1st = 100%, 2nd = 50%, 3rd+ = 0%)
            float effectiveValue = CalculateEffectivePhotoValue(photo);

            // 2. Perbarui catatan jumlah posting objek
            if (photo.visibleObjects != null && photo.visibleObjects.Count > 0)
            {
                foreach (var entry in photo.visibleObjects)
                {
                    if (string.IsNullOrEmpty(entry.objectName)) continue;
                    sharedObjectCounts[entry.objectName] = GetObjectSharedCount(entry.objectName) + 1;
                }
            }
            else if (photo.visibleObjectNames != null)
            {
                foreach (string name in photo.visibleObjectNames)
                {
                    if (string.IsNullOrEmpty(name)) continue;
                    sharedObjectCounts[name] = GetObjectSharedCount(name) + 1;
                }
            }

            postedPhotos.Add(photo);

            // 3. Hitung Penambahan Followers (Min 1/2 hingga 3/4 dari Effective Value)
            float followerRatio = UnityEngine.Random.Range(config.minFollowerRatio, config.maxFollowerRatio);
            int newFollowers = Mathf.RoundToInt(effectiveValue * followerRatio);
            currentFollowers += newFollowers;

            // 4. Hitung Penambahan Likes (Min 3/4 hingga 2.0 dari Total Followers)
            float likeRatio = UnityEngine.Random.Range(config.minLikeRatio, config.maxLikeRatio);
            int newLikes = Mathf.Max(1, Mathf.RoundToInt(currentFollowers * likeRatio));
            totalLikes += newLikes;

            // 5. Hitung Donasi Gold (Min 3/4 hingga 2.0 dikali baseMultiplier dikali Total Followers)
            float goldRatio = UnityEngine.Random.Range(config.minGoldRatio, config.maxGoldRatio);
            int newGold = Mathf.Max(10, Mathf.RoundToInt(goldRatio * config.goldBaseMultiplier * (currentFollowers / 10f)));
            currentGold += newGold;

            // 6. Buat Notifikasi Baru
            NotificationData notif = new NotificationData
            {
                gainedFollowers = newFollowers,
                gainedLikes = newLikes,
                gainedGold = newGold,
                message = $"Foto di-share! (Nilai Efektif: {effectiveValue:N0}) +{newFollowers} Followers, +{newLikes} Likes, +{newGold} Gold Donasi!"
            };

            notifications.Insert(0, notif);

            OnProfileStatsUpdated?.Invoke();
            OnNotificationAdded?.Invoke(notif);

            Debug.Log($"[SocialMediaManager] Photo Shared! Effective Value: {effectiveValue}, New Followers: +{newFollowers}, Likes: +{newLikes}, Gold: +{newGold}");
        }
    }
}
