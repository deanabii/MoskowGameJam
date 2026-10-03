using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.UI;

namespace PhotoModeSystem
{
    public class PhotoAnimationController : MonoBehaviour
    {
        public static PhotoAnimationController Instance { get; private set; }

        [Header("UI References")]
        [Tooltip("Canvas group utama bingkai foto.")]
        public CanvasGroup flashCanvasGroup;

        [Tooltip("RectTransform dari bingkai foto putih yang akan di-animasikan.")]
        public RectTransform photoFrameRect;

        [Tooltip("RawImage untuk menampilkan snapshot pratinjau foto.")]
        public RawImage previewRawImage;

        [Header("Animation Settings")]
        public float shrinkDuration = 0.25f;
        public float slideDuration = 0.4f;
        public Vector3 shrinkScale = new Vector3(0.6f, 0.6f, 1f);
        public float slideDistance = 1200f;

        [Header("Metadata Detector")]
        public PhotoMetadataDetector detector;

        public bool isPhotoProcessing { get; private set; }

        private Vector3 initialScale;
        private Vector2 initialAnchoredPosition;

        private void Awake()
        {
            if (Instance == null)
            {
                Instance = this;
            }
            else
            {
                Destroy(gameObject);
                return;
            }

            if (photoFrameRect != null)
            {
                initialScale = photoFrameRect.localScale;
                initialAnchoredPosition = photoFrameRect.anchoredPosition;
            }

            if (flashCanvasGroup != null)
            {
                flashCanvasGroup.alpha = 0f;
                flashCanvasGroup.blocksRaycasts = false;
            }
        }

        private void Update()
        {
            // Pemicu Tombol Enter (Pemotretan)
            if (PhotoModeInputUtility.IsEnterPressed())
            {
                // Jangan ambil foto jika menu TAB sedang terbuka atau sedang dalam proses animasi
                if (PhotoModeStateBridge.Instance != null && PhotoModeStateBridge.Instance.isMenuOpen) return;
                if (isPhotoProcessing) return;

                StartCoroutine(CaptureAndAnimateRoutine());
            }
        }

        public IEnumerator CaptureAndAnimateRoutine()
        {
            isPhotoProcessing = true;

            // Wait for end of frame to ensure render is complete
            yield return new WaitForEndOfFrame();

            // 1. Snapshot Layar Ke Texture2D
            Texture2D snapshot = new Texture2D(Screen.width, Screen.height, TextureFormat.RGB24, false);
            snapshot.ReadPixels(new Rect(0, 0, Screen.width, Screen.height), 0, 0);
            snapshot.Apply();

            // 2. Deteksi Metadata (ObjectValue)
            Camera mainCam = Camera.main;
            if (detector != null && mainCam != null)
            {
                detector.Initialize(mainCam);
            }
            var (visibleNames, visibleObjects, totalValue) = detector != null ? 
                detector.DetectVisibleObjects() : 
                (new List<string>(), new List<ObjectPhotoEntry>(), 0f);

            // 3. Simpan Ke File System (Windows/WebGL)
            if (PhotoStorageManager.Instance != null)
            {
                PhotoStorageManager.Instance.SaveCapturedTexture(snapshot, visibleNames, visibleObjects, totalValue);
            }

            // 4. Set UI Pratinjau Foto
            if (previewRawImage != null)
            {
                previewRawImage.texture = snapshot;
            }

            // 5. Jalankan Animasi Canvas (Mengecil -> Slide Down)
            yield return StartCoroutine(PlayPhotoFrameAnimation());

            // Clean up preview texture
            if (previewRawImage != null) previewRawImage.texture = null;
            Destroy(snapshot);

            isPhotoProcessing = false;
        }

        private IEnumerator PlayPhotoFrameAnimation()
        {
            if (flashCanvasGroup == null || photoFrameRect == null) yield break;

            // Reset pos & scale
            photoFrameRect.localScale = initialScale;
            photoFrameRect.anchoredPosition = initialAnchoredPosition;
            flashCanvasGroup.alpha = 1f;

            // Phase A: Shrink (Mengecil dengan bingkai putih)
            float elapsed = 0f;
            while (elapsed < shrinkDuration)
            {
                elapsed += Time.deltaTime;
                float t = elapsed / shrinkDuration;
                photoFrameRect.localScale = Vector3.Lerp(initialScale, shrinkScale, t);
                yield return null;
            }

            // Phase B: Slide Down (Meluncur ke bawah keluar layar)
            elapsed = 0f;
            Vector2 targetPos = initialAnchoredPosition - new Vector2(0, slideDistance);
            Vector2 startPos = photoFrameRect.anchoredPosition;

            while (elapsed < slideDuration)
            {
                elapsed += Time.deltaTime;
                float t = elapsed / slideDuration;
                // Ease In Quad
                float easeT = t * t;
                photoFrameRect.anchoredPosition = Vector2.Lerp(startPos, targetPos, easeT);
                flashCanvasGroup.alpha = Mathf.Lerp(1f, 0f, easeT);
                yield return null;
            }

            flashCanvasGroup.alpha = 0f;
            photoFrameRect.localScale = initialScale;
            photoFrameRect.anchoredPosition = initialAnchoredPosition;
        }
    }
}
