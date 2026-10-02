using System;
using UnityEngine;

namespace MoskowGameJam.Settings
{
    public class InteractionSettingsManager : MonoBehaviour
    {
        public static InteractionSettingsManager Instance { get; private set; }

        private const string SAVE_KEY = "MoskowGameJam_InteractionSettings";

        [Header("Default Configuration Asset (Fallback)")]
        [SerializeField] private InteractionSaveData defaultData = new InteractionSaveData();

        private InteractionSaveData currentData;

        public static event Action<InteractionSaveData> OnSettingsChanged;

        public InteractionSaveData CurrentData => currentData ?? defaultData;

        private void Awake()
        {
            if (Instance != null && Instance != this)
            {
                Destroy(gameObject);
                return;
            }

            Instance = this;
            DontDestroyOnLoad(gameObject);

            LoadSettings();
        }

        public void LoadSettings()
        {
            if (PlayerPrefs.HasKey(SAVE_KEY))
            {
                string json = PlayerPrefs.GetString(SAVE_KEY, string.Empty);
                currentData = InteractionSaveData.FromJson(json);
            }
            else
            {
                currentData = new InteractionSaveData();
            }

            NotifySettingsChanged();
        }

        public void SaveSettings(InteractionSaveData newData)
        {
            if (newData == null) return;

            currentData = newData;
            string json = currentData.ToJson();

            PlayerPrefs.SetString(SAVE_KEY, json);
            PlayerPrefs.Save(); // Sinkronisasi otomatis untuk Desktop & WebGL (IndexedDB)

            NotifySettingsChanged();
        }

        public void ResetToDefault()
        {
            currentData = new InteractionSaveData();
            SaveSettings(currentData);
        }

        public void UpdateInteractKey(KeyCode newKey)
        {
            CurrentData.interactKey = newKey;
            SaveSettings(CurrentData);
        }

        public void UpdateInteractDistance(float newDistance)
        {
            CurrentData.interactDistance = newDistance;
            SaveSettings(CurrentData);
        }

        public void UpdateMaxDropDistance(float newDropDistance)
        {
            CurrentData.maxDropDistance = newDropDistance;
            SaveSettings(CurrentData);
        }

        public void UpdateEnableRayBasedDrop(bool enable)
        {
            CurrentData.enableRayBasedDrop = enable;
            SaveSettings(CurrentData);
        }

        private void NotifySettingsChanged()
        {
            OnSettingsChanged?.Invoke(CurrentData);
        }
    }
}
