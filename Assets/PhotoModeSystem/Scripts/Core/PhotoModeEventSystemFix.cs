using System;
using UnityEngine;
using UnityEngine.EventSystems;

namespace PhotoModeSystem
{
    [DefaultExecutionOrder(-100)]
    public class PhotoModeEventSystemFix : MonoBehaviour
    {
        private void Awake()
        {
            FixInputModule();
        }

        private void Start()
        {
            FixInputModule();
        }

        public static void FixInputModule()
        {
            EventSystem es = FindObjectOfType<EventSystem>();
            if (es == null) return;

            // Check if New Input System module type exists in project assemblies
            Type inputSystemModuleType = Type.GetType("UnityEngine.InputSystem.UI.InputSystemUIInputModule, Unity.InputSystem");

            if (inputSystemModuleType != null)
            {
                // New Input System package is present in project
                if (es.GetComponent(inputSystemModuleType) == null)
                {
                    es.gameObject.AddComponent(inputSystemModuleType);
                    Debug.Log("[PhotoModeEventSystemFix] Replaced UI Input Module with InputSystemUIInputModule.");
                }

                StandaloneInputModule legacyModule = es.GetComponent<StandaloneInputModule>();
                if (legacyModule != null)
                {
                    Destroy(legacyModule);
                }
            }
        }
    }
}
