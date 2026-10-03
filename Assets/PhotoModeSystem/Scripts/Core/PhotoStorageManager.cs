using System;
using System.Collections.Generic;
using System.IO;
using UnityEngine;

namespace PhotoModeSystem
{
    public class PhotoStorageManager : MonoBehaviour
    {
        public static PhotoStorageManager Instance { get; private set; }

        private string storageFolderPath;

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

            storageFolderPath = Path.Combine(Application.persistentDataPath, "Photos");
            if (!Directory.Exists(storageFolderPath))
            {
                Directory.CreateDirectory(storageFolderPath);
            }
        }

        public PhotoData SaveCapturedTexture(Texture2D texture, List<string> visibleNames, float totalValue)
        {
            List<ObjectPhotoEntry> entries = new List<ObjectPhotoEntry>();
            if (visibleNames != null)
            {
                float avgValue = visibleNames.Count > 0 ? totalValue / visibleNames.Count : 0f;
                foreach (string name in visibleNames)
                {
                    entries.Add(new ObjectPhotoEntry(name, avgValue));
                }
            }
            return SaveCapturedTexture(texture, visibleNames, entries, totalValue);
        }

        public PhotoData SaveCapturedTexture(Texture2D texture, List<string> visibleNames, List<ObjectPhotoEntry> visibleObjects, float totalValue)
        {
            PhotoData data = new PhotoData
            {
                visibleObjectNames = visibleNames,
                visibleObjects = visibleObjects,
                totalValue = totalValue
            };

            string timeStampStr = DateTime.Now.ToString("yyyyMMdd_HHmmss_fff");

#if UNITY_WEBGL && !UNITY_EDITOR
            data.fileName = $"photo_{timeStampStr}.jpg";
            byte[] imageBytes = texture.EncodeToJPG(75);
#else
            data.fileName = $"photo_{timeStampStr}.png";
            byte[] imageBytes = texture.EncodeToPNG();
#endif

            string imageFilePath = Path.Combine(storageFolderPath, data.fileName);
            File.WriteAllBytes(imageFilePath, imageBytes);

            string jsonFileName = Path.ChangeExtension(data.fileName, ".json");
            string jsonFilePath = Path.Combine(storageFolderPath, jsonFileName);
            string jsonContent = JsonUtility.ToJson(data, true);
            File.WriteAllText(jsonFilePath, jsonContent);

            Debug.Log($"[PhotoStorageManager] Foto tersimpan ke: {imageFilePath}");
            return data;
        }

        public List<PhotoData> LoadAllPhotos()
        {
            List<PhotoData> list = new List<PhotoData>();
            if (!Directory.Exists(storageFolderPath)) return list;

            string[] jsonFiles = Directory.GetFiles(storageFolderPath, "*.json");
            foreach (string jsonPath in jsonFiles)
            {
                try
                {
                    string json = File.ReadAllText(jsonPath);
                    PhotoData data = JsonUtility.FromJson<PhotoData>(json);
                    if (data != null)
                    {
                        list.Add(data);
                    }
                }
                catch (Exception ex)
                {
                    Debug.LogError($"[PhotoStorageManager] Gagal membaca metadata JSON: {jsonPath} - {ex.Message}");
                }
            }

            list.Sort((a, b) => string.Compare(b.timestamp, a.timestamp, StringComparison.Ordinal));
            return list;
        }

        public Texture2D LoadPhotoTexture(PhotoData data)
        {
            if (data == null || string.IsNullOrEmpty(data.fileName)) return null;

            string filePath = Path.Combine(storageFolderPath, data.fileName);
            if (!File.Exists(filePath)) return null;

            byte[] bytes = File.ReadAllBytes(filePath);
            Texture2D tex = new Texture2D(2, 2, TextureFormat.RGB24, false);
            if (tex.LoadImage(bytes))
            {
                return tex;
            }
            return null;
        }

        public bool DeletePhoto(PhotoData data)
        {
            if (data == null) return false;

            string imagePath = Path.Combine(storageFolderPath, data.fileName);
            string jsonPath = Path.Combine(storageFolderPath, Path.ChangeExtension(data.fileName, ".json"));

            try
            {
                if (File.Exists(imagePath)) File.Delete(imagePath);
                if (File.Exists(jsonPath)) File.Delete(jsonPath);
                Debug.Log($"[PhotoStorageManager] Foto berhasil dihapus: {data.fileName}");
                return true;
            }
            catch (Exception ex)
            {
                Debug.LogError($"[PhotoStorageManager] Gagal menghapus foto: {ex.Message}");
                return false;
            }
        }
    }
}
