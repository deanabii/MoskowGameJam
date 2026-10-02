using System;
using UnityEngine;

namespace PhotoModeSystem
{
    public class PhotoModeStateBridge : MonoBehaviour
    {
        public static PhotoModeStateBridge Instance { get; private set; }

        public static event Action<bool> OnPhotoModeToggle;

        [Header("Pause & Control Settings")]
        [Tooltip("Jeda permainan (Time.timeScale = 0) saat menu TAB terbuka agar pemain tidak bisa bergerak/melihat sekitar.")]
        public bool pauseGameWhenMenuOpen = true;

        [Tooltip("Cari dan nonaktifkan skrip PlayerInput / CharacterController secara otomatis saat menu terbuka.")]
        public bool autoDisablePlayerInputComponent = true;

        public bool isMenuOpen { get; private set; }

        private CursorLockMode previousLockState;
        private bool previousCursorVisible;
        private float previousTimeScale = 1f;

        private MonoBehaviour[] cachedPlayerScripts;

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
            }
        }

        public void SetPhotoModeOpen(bool open)
        {
            if (isMenuOpen == open) return;

            isMenuOpen = open;

            if (isMenuOpen)
            {
                // 1. Simpan status kursor & timescale sebelumnya
                previousLockState = Cursor.lockState;
                previousCursorVisible = Cursor.visible;
                previousTimeScale = Time.timeScale > 0f ? Time.timeScale : 1f;

                // 2. Buka kursor mouse
                Cursor.lockState = CursorLockMode.None;
                Cursor.visible = true;

                // 3. Pause game time jika diizinkan
                if (pauseGameWhenMenuOpen)
                {
                    Time.timeScale = 0f;
                }

                // 4. Nonaktifkan komponen input player (jika ada)
                if (autoDisablePlayerInputComponent)
                {
                    TogglePlayerComponents(false);
                }
            }
            else
            {
                // 1. Kembalikan timescale
                if (pauseGameWhenMenuOpen)
                {
                    Time.timeScale = previousTimeScale;
                }

                // 2. Kembalikan kursor mouse
                Cursor.lockState = previousLockState != CursorLockMode.None ? previousLockState : CursorLockMode.Locked;
                Cursor.visible = previousCursorVisible;

                // 3. Aktifkan kembali komponen player input
                if (autoDisablePlayerInputComponent)
                {
                    TogglePlayerComponents(true);
                }
            }

            OnPhotoModeToggle?.Invoke(isMenuOpen);
            Debug.Log($"[PhotoModeStateBridge] Menu Open = {isMenuOpen} | TimeScale = {Time.timeScale} | CursorVisible = {Cursor.visible}");
        }

        private void TogglePlayerComponents(bool enable)
        {
            // Cari objek dengan tag "Player" atau cari PlayerInput component jika New Input System digunakan
            GameObject playerObj = GameObject.FindGameObjectWithTag("Player");
            if (playerObj != null)
            {
                // Matikan / aktifkan PlayerInput jika ada
                var playerInput = playerObj.GetComponent("UnityEngine.InputSystem.PlayerInput") as MonoBehaviour;
                if (playerInput != null)
                {
                    playerInput.enabled = enable;
                }
            }
        }
    }
}
