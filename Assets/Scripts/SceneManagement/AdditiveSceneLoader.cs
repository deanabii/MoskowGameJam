using System.Collections;
using UnityEngine;
using UnityEngine.SceneManagement;

namespace MoskowGameJam.SceneManagement
{
    public class AdditiveSceneLoader : MonoBehaviour
    {
        [Header("Configuration")]
        [SerializeField] private SceneLoadConfig sceneConfig;

        [Header("Options")]
        [SerializeField] private bool loadOnStart = true;

        private void Start()
        {
            if (loadOnStart && sceneConfig != null)
            {
                LoadConfiguredScenes();
            }
        }

        public void LoadConfiguredScenes()
        {
            if (sceneConfig == null || sceneConfig.scenesToLoad == null) return;

            StartCoroutine(LoadScenesRoutine());
        }

        private IEnumerator LoadScenesRoutine()
        {
            foreach (string sceneName in sceneConfig.scenesToLoad)
            {
                if (string.IsNullOrEmpty(sceneName)) continue;

                // Periksa apakah scene sudah ter-load
                Scene loadedScene = SceneManager.GetSceneByName(sceneName);
                if (loadedScene.isLoaded) continue;

                if (sceneConfig.loadAsynchronously)
                {
                    AsyncOperation asyncOp = SceneManager.LoadSceneAsync(sceneName, LoadSceneMode.Additive);
                    while (!asyncOp.isDone)
                    {
                        yield return null;
                    }
                }
                else
                {
                    SceneManager.LoadScene(sceneName, LoadSceneMode.Additive);
                }
            }

            // Atur active scene jika ditentukan
            if (!string.IsNullOrEmpty(sceneConfig.activeSceneName))
            {
                Scene targetActive = SceneManager.GetSceneByName(sceneConfig.activeSceneName);
                if (targetActive.isLoaded)
                {
                    SceneManager.SetActiveScene(targetActive);
                }
            }
        }
    }
}
