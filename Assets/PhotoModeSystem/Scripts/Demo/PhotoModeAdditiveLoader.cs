using UnityEngine;
using UnityEngine.SceneManagement;

namespace PhotoModeSystem
{
    public class PhotoModeAdditiveLoader : MonoBehaviour
    {
        [Header("Additive Scene Config")]
        [Tooltip("Nama scene Photo Mode yang akan di-load secara additive.")]
        public string photoModeSceneName = "PhotoModeUI";

        [Tooltip("Auto-load scene saat Start jika belum di-load.")]
        public bool autoLoadOnStart = true;

        private void Start()
        {
            if (autoLoadOnStart)
            {
                LoadPhotoModeSceneAdditive();
            }
        }

        public void LoadPhotoModeSceneAdditive()
        {
            if (!SceneManager.GetSceneByName(photoModeSceneName).isLoaded)
            {
                SceneManager.LoadSceneAsync(photoModeSceneName, LoadSceneMode.Additive);
                Debug.Log($"[PhotoModeAdditiveLoader] Scene '{photoModeSceneName}' di-load secara additive.");
            }
        }
    }
}
