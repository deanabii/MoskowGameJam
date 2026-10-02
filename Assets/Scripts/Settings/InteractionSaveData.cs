using System;
using UnityEngine;

namespace MoskowGameJam.Settings
{
    [Serializable]
    public class InteractionSaveData
    {
        [Header("Keybindings")]
        public KeyCode interactKey = KeyCode.E;

        [Header("Raycast & Reach")]
        public float interactDistance = 3.5f;

        [Header("Drop Settings")]
        public float maxDropDistance = 4.0f;
        public bool enableRayBasedDrop = true;

        public string ToJson()
        {
            return JsonUtility.ToJson(this, true);
        }

        public static InteractionSaveData FromJson(string json)
        {
            if (string.IsNullOrEmpty(json)) return new InteractionSaveData();
            try
            {
                return JsonUtility.FromJson<InteractionSaveData>(json);
            }
            catch
            {
                return new InteractionSaveData();
            }
        }
    }
}
