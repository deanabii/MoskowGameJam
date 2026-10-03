using System.Collections;
using UnityEngine;
using UnityEngine.SceneManagement;
using UnityEngine.UI;
using TMPro;

namespace MoskowGameJam.UI
{
    public class MainMenuController : MonoBehaviour
    {
        [Header("Scene Transition Settings")]
        [Tooltip("Nama scene gameplay yang akan di-load saat tombol Play diklik")]
        [SerializeField] private string targetSceneName = "NPC Test";

        [Tooltip("Mode transisi scene (Single = menggantikan scene menu)")]
        [SerializeField] private LoadSceneMode sceneLoadMode = LoadSceneMode.Single;

        [Tooltip("Gunakan pemuatan asynchronous di background")]
        [SerializeField] private bool useAsyncLoad = true;

        [Tooltip("Jeda singkat sebelum loading scene (memberikan waktu untuk suara klik/animasi)")]
        [SerializeField] private float transitionDelay = 0.15f;

        [Header("Loading Screen (Opsional)")]
        [Tooltip("Panel loading yang akan diaktifkan saat transisi dimulai")]
        [SerializeField] private GameObject loadingPanel;

        [Tooltip("Slider progress bar loading")]
        [SerializeField] private Slider loadingProgressBar;

        [Tooltip("Teks status loading persentase")]
        [SerializeField] private TextMeshProUGUI loadingStatusText;

        [Header("Panel Navigation (Opsional)")]
        [SerializeField] private GameObject mainPanel;
        [SerializeField] private GameObject settingsPanel;
        [SerializeField] private GameObject creditsPanel;
        [SerializeField] private GameObject quitConfirmPanel;

        [Header("Audio (Opsional)")]
        [SerializeField] private AudioSource audioSource;
        [SerializeField] private AudioClip buttonClickSFX;
        [SerializeField] private AudioClip startGameSFX;

        private bool isTransitioning = false;

        private void Awake()
        {
            // Pastikan kursor mouse terlihat dan bebas bergerak di menu
            Cursor.lockState = CursorLockMode.None;
            Cursor.visible = true;

            // Sembunyikan panel loading di awal jika ada
            if (loadingPanel != null)
            {
                loadingPanel.SetActive(false);
            }
        }

        /// <summary>
        /// Memulai permainan dengan memuat scene target yang sudah dikonfigurasi.
        /// </summary>
        public void PlayGame()
        {
            PlayGame(targetSceneName);
        }

        /// <summary>
        /// Memulai permainan dengan memuat scene spesifik berdasarkan nama.
        /// </summary>
        public void PlayGame(string sceneName)
        {
            if (isTransitioning) return;

            if (string.IsNullOrWhiteSpace(sceneName))
            {
                Debug.LogError("[MainMenuController] Target scene name kosong atau tidak valid!");
                return;
            }

            PlaySound(startGameSFX != null ? startGameSFX : buttonClickSFX);
            StartCoroutine(LoadSceneRoutine(sceneName));
        }

        private IEnumerator LoadSceneRoutine(string sceneName)
        {
            isTransitioning = true;

            // Jeda singkat untuk feedback klik
            if (transitionDelay > 0f)
            {
                yield return new WaitForSecondsRealtime(transitionDelay);
            }

            // Tampilkan loading panel jika tersedia
            if (loadingPanel != null)
            {
                loadingPanel.SetActive(true);
            }

            if (useAsyncLoad)
            {
                AsyncOperation asyncOp = SceneManager.LoadSceneAsync(sceneName, sceneLoadMode);

                if (asyncOp == null)
                {
                    Debug.LogError($"[MainMenuController] Gagal memuat scene '{sceneName}'. Pastikan scene sudah terdaftar di File -> Build Settings!");
                    isTransitioning = false;
                    if (loadingPanel != null) loadingPanel.SetActive(false);
                    yield break;
                }

                while (!asyncOp.isDone)
                {
                    // asyncOp.progress bernilai antara 0.0 hingga 0.9 selama loading
                    float progress = Mathf.Clamp01(asyncOp.progress / 0.9f);

                    if (loadingProgressBar != null)
                    {
                        loadingProgressBar.value = progress;
                    }

                    if (loadingStatusText != null)
                    {
                        loadingStatusText.text = $"Loading... {Mathf.RoundToInt(progress * 100f)}%";
                    }

                    yield return null;
                }
            }
            else
            {
                SceneManager.LoadScene(sceneName, sceneLoadMode);
            }
        }

        /// <summary>
        /// Membuka panel tertentu dan menutup panel lainnya jika diperlukan.
        /// </summary>
        public void OpenPanel(GameObject panelToOpen)
        {
            PlaySound(buttonClickSFX);
            if (panelToOpen != null)
            {
                panelToOpen.SetActive(true);
            }
        }

        /// <summary>
        /// Menutup panel tertentu.
        /// </summary>
        public void ClosePanel(GameObject panelToClose)
        {
            PlaySound(buttonClickSFX);
            if (panelToClose != null)
            {
                panelToClose.SetActive(false);
            }
        }

        /// <summary>
        /// Membuka panel pengaturan.
        /// </summary>
        public void OpenSettings()
        {
            OpenPanel(settingsPanel);
        }

        /// <summary>
        /// Menutup panel pengaturan.
        /// </summary>
        public void CloseSettings()
        {
            ClosePanel(settingsPanel);
        }

        /// <summary>
        /// Membuka panel credits.
        /// </summary>
        public void OpenCredits()
        {
            OpenPanel(creditsPanel);
        }

        /// <summary>
        /// Menutup panel credits.
        /// </summary>
        public void CloseCredits()
        {
            ClosePanel(creditsPanel);
        }

        /// <summary>
        /// Keluar dari game (berfungsi di Build standalone & Editor).
        /// </summary>
        public void QuitGame()
        {
            PlaySound(buttonClickSFX);
            Debug.Log("[MainMenuController] Keluar dari aplikasi...");

#if UNITY_EDITOR
            UnityEditor.EditorApplication.isPlaying = false;
#else
            Application.Quit();
#endif
        }

        /// <summary>
        /// Membuka link URL browser eksternal.
        /// </summary>
        public void OpenURL(string url)
        {
            PlaySound(buttonClickSFX);
            if (!string.IsNullOrEmpty(url))
            {
                Application.OpenURL(url);
            }
        }

        private void PlaySound(AudioClip clip)
        {
            if (clip != null && audioSource != null)
            {
                audioSource.PlayOneShot(clip);
            }
        }
    }
}
