using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.UI;
using TMPro;

#if ENABLE_INPUT_SYSTEM
using UnityEngine.InputSystem;
#endif

namespace MoskowGameJam.NPC
{
    public class RecipePopupUI : MonoBehaviour
    {
        public static RecipePopupUI Instance { get; private set; }

        [Header("UI Root References")]
        [Tooltip("Root GameObject panel resep kertas (aktif/nonaktif)")]
        [SerializeField] private GameObject recipeCardPanel;

        [Tooltip("RectTransform kartu kertas yang akan dianimasikan (jika kosong, otomatis mencari ParchmentCard)")]
        [SerializeField] private RectTransform cardTransform;

        [Header("Text References (TextMeshPro)")]
        [Tooltip("Judul resep (misal: 'Traditional Russian Blini')")]
        [SerializeField] private TMP_Text recipeTitleText;

        [Tooltip("Daftar bahan dasar (misal: Flour, Milk, Eggs)")]
        [SerializeField] private TMP_Text baseIngredientsText;

        [Tooltip("Daftar topping (misal: Powdered Sugar, Strawberries)")]
        [SerializeField] private TMP_Text toppingsText;

        [Tooltip("Catatan cara memasak singkat / 4 langkah")]
        [SerializeField] private TMP_Text cookingNoteText;

        [Tooltip("Teks petunjuk tombol tutup")]
        [SerializeField] private TMP_Text closeHintText;

        [Header("Animation Settings")]
        [Tooltip("Durasi animasi saat membuka resep (detik)")]
        [SerializeField] private float openDuration = 0.35f;

        [Tooltip("Durasi animasi saat menutup resep (detik)")]
        [SerializeField] private float closeDuration = 0.2f;

        [Tooltip("Skala awal kertas saat mulai membuka")]
        [Range(0.1f, 1f)]
        [SerializeField] private float startScale = 0.75f;

        [Tooltip("Animasi pop-up dengan efek elastis / bounce lembut")]
        [SerializeField] private bool useBounceEffect = true;

        [Tooltip("Pergeseran vertikal halus saat animasi buka (pixel)")]
        [SerializeField] private float slideOffset = 30f;

        [Header("Anti-Spam & Input Protection")]
        [Tooltip("Jeda waktu minimum (detik) setelah popup terbuka sebelum tombol tutup bisa aktif (mencegah tertutup tak sengaja saat spam spasi)")]
        [SerializeField] private float closeInputCooldown = 0.45f;

        [Header("Auto Trigger on Dialogue")]
        [Tooltip("Otomatis muncul saat NPC dengan nama ini selesai bicara")]
        [SerializeField] private bool autoTriggerOnDialogueEnd = true;
        [SerializeField] private string targetNPCKeyword = "Anastasia";

        [Header("Audio (Opsional)")]
        [Tooltip("Efek suara kertas terbuka")]
        [SerializeField] private AudioClip paperOpenSound;
        [Tooltip("Efek suara kertas tertutup")]
        [SerializeField] private AudioClip paperCloseSound;

        private CanvasGroup canvasGroup;
        private bool isOpen = false;
        private Coroutine transitionCoroutine;
        private Vector2 cardOriginalAnchoredPosition;
        private Vector3 cardOriginalScale = Vector3.one;
        private float canCloseAfterTime = 0f;

        public bool IsOpen => isOpen;

        private void Awake()
        {
            // Pastikan instance menunjuk ke komponen yang memiliki referensi UI aktif
            if (Instance == null || Instance.recipeCardPanel == null)
            {
                Instance = this;
            }

            if (recipeCardPanel == null)
            {
                recipeCardPanel = gameObject;
            }

            // Dapatkan atau tambahkan CanvasGroup untuk kontrol visibility dan transisi
            canvasGroup = GetComponent<CanvasGroup>();
            if (canvasGroup == null && recipeCardPanel != null)
            {
                canvasGroup = recipeCardPanel.GetComponent<CanvasGroup>();
                if (canvasGroup == null)
                {
                    canvasGroup = recipeCardPanel.AddComponent<CanvasGroup>();
                }
            }

            // Auto-resolve referensi text & kartu jika belum terpasang
            AutoResolveReferences();

            if (cardTransform != null)
            {
                cardOriginalAnchoredPosition = cardTransform.anchoredPosition;
                cardOriginalScale = cardTransform.localScale;
            }

            // Sembunyikan saat inisialisasi awal
            HideVisualsInstant();
        }

        private void OnEnable()
        {
            NPCInteractable.OnInteractionClosed += HandleNPCInteractionClosed;
        }

        private void OnDisable()
        {
            NPCInteractable.OnInteractionClosed -= HandleNPCInteractionClosed;
        }

        private void AutoResolveReferences()
        {
            if (cardTransform == null)
            {
                Transform card = transform.Find("ParchmentCard");
                if (card == null && transform.childCount > 0)
                {
                    card = transform.GetChild(0);
                }
                if (card != null)
                {
                    cardTransform = card as RectTransform;
                }
            }

            TMP_Text[] allTexts = GetComponentsInChildren<TMP_Text>(true);
            foreach (var t in allTexts)
            {
                string lower = t.gameObject.name.ToLower();
                if (recipeTitleText == null && (lower.Contains("title") || lower.Contains("judul")))
                    recipeTitleText = t;
                else if (baseIngredientsText == null && (lower.Contains("base") || lower.Contains("bahan") || lower.Contains("ingredient")))
                    baseIngredientsText = t;
                else if (toppingsText == null && (lower.Contains("topping") || lower.Contains("toping")))
                    toppingsText = t;
                else if (cookingNoteText == null && (lower.Contains("note") || lower.Contains("tip") || lower.Contains("cara") || lower.Contains("step") || lower.Contains("method") || lower.Contains("instruction")))
                    cookingNoteText = t;
                else if (closeHintText == null && (lower.Contains("close") || lower.Contains("hint") || lower.Contains("tutup")))
                    closeHintText = t;
            }
        }

        private void HandleNPCInteractionClosed(NPCInteractable npc)
        {
            if (!autoTriggerOnDialogueEnd || npc == null) return;

            string nameToCheck = (npc.NPCName + " " + npc.gameObject.name).ToLower();
            string keyword = string.IsNullOrEmpty(targetNPCKeyword) ? "anastasia" : targetNPCKeyword.ToLower();

            // Cek apakah NPC adalah Anastasia atau stall Blini
            if (nameToCheck.Contains(keyword) || nameToCheck.Contains("blini"))
            {
                ShowBliniRecipe();
            }
        }

        private void Update()
        {
            if (!isOpen) return;

            // Anti-Spam protection: abaikan input penutup sebelum cooldown/animasi selesai
            if (Time.unscaledTime < canCloseAfterTime) return;

            if (IsCloseKeyPressed())
            {
                CloseRecipe();
            }
        }

        private bool IsCloseKeyPressed()
        {
#if ENABLE_INPUT_SYSTEM
            if (Keyboard.current != null)
            {
                if (Keyboard.current.spaceKey.wasPressedThisFrame ||
                    Keyboard.current.escapeKey.wasPressedThisFrame)
                {
                    return true;
                }
            }
#endif

#if ENABLE_LEGACY_INPUT_MANAGER
            try
            {
                if (Input.GetKeyDown(KeyCode.Space) || Input.GetKeyDown(KeyCode.Escape))
                {
                    return true;
                }
            }
            catch {}
#endif
            return false;
        }

        /// <summary>
        /// Memunculkan Popup Resep Blini khas Rusia dengan 4 langkah memasak berurutan
        /// </summary>
        [ContextMenu("Show Blini Recipe")]
        public void ShowBliniRecipe()
        {
            string cookingSteps =
                "1. Prepare the wet batter from eggs, milk, and flour.\n" +
                "2. Slowly pour the measured ingredients into the Mixing Bowl until all ingredients are ready to be processed.\n" +
                "3. Stir all ingredients evenly, ensuring there are no flour lumps or unmixed leftovers.\n" +
                "4. Heat a flat pan or non-stick skillet. Pour a small amount of batter to form small pancakes. Cook until the surface bubbles and the edges become slightly dry, then flip and cook the other side until golden brown.";

            ShowRecipe(
                "Traditional Russian Blini",
                "• Flour (Tepung) - 200g\n• Milk (Susu) - 500ml\n• Eggs (Telur) - 3 pcs",
                "• Powdered Sugar (Gula Bubuk)\n• Fresh Strawberries (Stroberi Segar)",
                cookingSteps
            );
        }

        /// <summary>
        /// Memunculkan Popup Resep Kustom dengan Animasi Transisi Halus
        /// </summary>
        public void ShowRecipe(string title, string baseIngredients, string toppings, string cookingNote)
        {
            if (recipeTitleText != null) recipeTitleText.text = title;
            if (baseIngredientsText != null) baseIngredientsText.text = baseIngredients;
            if (toppingsText != null) toppingsText.text = toppings;
            if (cookingNoteText != null) cookingNoteText.text = cookingNote;
            if (closeHintText != null) closeHintText.text = "[Space / Esc] Close Recipe";

            if (paperOpenSound != null)
            {
                AudioSource.PlayClipAtPoint(paperOpenSound, Camera.main != null ? Camera.main.transform.position : transform.position);
            }

            isOpen = true;
            // Berikan jeda proteksi input agar spam spasi pada akhir dialog tidak langsung menutup popup
            canCloseAfterTime = Time.unscaledTime + Mathf.Max(openDuration, closeInputCooldown);

            if (transitionCoroutine != null) StopCoroutine(transitionCoroutine);
            transitionCoroutine = StartCoroutine(AnimateOpenRoutine());
        }

        public void CloseRecipe()
        {
            if (!isOpen) return;
            isOpen = false;

            if (paperCloseSound != null)
            {
                AudioSource.PlayClipAtPoint(paperCloseSound, Camera.main != null ? Camera.main.transform.position : transform.position);
            }

            if (transitionCoroutine != null) StopCoroutine(transitionCoroutine);
            transitionCoroutine = StartCoroutine(AnimateCloseRoutine());
        }

        private IEnumerator AnimateOpenRoutine()
        {
            if (recipeCardPanel != null) recipeCardPanel.SetActive(true);

            float duration = Mathf.Max(0.05f, openDuration);
            float elapsed = 0f;

            float initialAlpha = canvasGroup != null ? canvasGroup.alpha : 0f;
            Vector3 initialScale = cardTransform != null ? cardTransform.localScale : Vector3.one * startScale;
            Vector2 initialPos = cardTransform != null ? cardTransform.anchoredPosition : cardOriginalAnchoredPosition;
            Vector2 startPos = cardOriginalAnchoredPosition + new Vector2(0f, -slideOffset);

            if (canvasGroup != null)
            {
                canvasGroup.blocksRaycasts = true;
                canvasGroup.interactable = true;
            }

            while (elapsed < duration)
            {
                elapsed += Time.unscaledDeltaTime;
                float t = Mathf.Clamp01(elapsed / duration);

                // Fade In
                if (canvasGroup != null)
                {
                    canvasGroup.alpha = Mathf.Lerp(initialAlpha, 1f, EaseOutCubic(t));
                }

                // Scale + Slide Up
                if (cardTransform != null)
                {
                    float scaleProgress = useBounceEffect ? EaseOutBack(t) : EaseOutCubic(t);
                    cardTransform.localScale = Vector3.LerpUnclamped(Vector3.one * startScale, cardOriginalScale, scaleProgress);
                    cardTransform.anchoredPosition = Vector2.Lerp(startPos, cardOriginalAnchoredPosition, EaseOutCubic(t));
                }

                yield return null;
            }

            // Final state
            if (canvasGroup != null) canvasGroup.alpha = 1f;
            if (cardTransform != null)
            {
                cardTransform.localScale = cardOriginalScale;
                cardTransform.anchoredPosition = cardOriginalAnchoredPosition;
            }
        }

        private IEnumerator AnimateCloseRoutine()
        {
            float duration = Mathf.Max(0.05f, closeDuration);
            float elapsed = 0f;

            float initialAlpha = canvasGroup != null ? canvasGroup.alpha : 1f;
            Vector3 initialScale = cardTransform != null ? cardTransform.localScale : cardOriginalScale;
            Vector2 initialPos = cardTransform != null ? cardTransform.anchoredPosition : cardOriginalAnchoredPosition;
            Vector2 targetPos = cardOriginalAnchoredPosition + new Vector2(0f, -slideOffset * 0.5f);

            if (canvasGroup != null)
            {
                canvasGroup.blocksRaycasts = false;
                canvasGroup.interactable = false;
            }

            while (elapsed < duration)
            {
                elapsed += Time.unscaledDeltaTime;
                float t = Mathf.Clamp01(elapsed / duration);

                // Fade Out
                if (canvasGroup != null)
                {
                    canvasGroup.alpha = Mathf.Lerp(initialAlpha, 0f, EaseInCubic(t));
                }

                // Scale down slightly
                if (cardTransform != null)
                {
                    cardTransform.localScale = Vector3.Lerp(initialScale, cardOriginalScale * 0.85f, EaseInCubic(t));
                    cardTransform.anchoredPosition = Vector2.Lerp(initialPos, targetPos, EaseInCubic(t));
                }

                yield return null;
            }

            HideVisualsInstant();
        }

        private void HideVisualsInstant()
        {
            if (canvasGroup != null)
            {
                canvasGroup.alpha = 0f;
                canvasGroup.interactable = false;
                canvasGroup.blocksRaycasts = false;
            }

            if (cardTransform != null)
            {
                cardTransform.localScale = cardOriginalScale * startScale;
                cardTransform.anchoredPosition = cardOriginalAnchoredPosition;
            }

            if (recipeCardPanel != null && recipeCardPanel != gameObject)
            {
                recipeCardPanel.SetActive(false);
            }
        }

        #region Easing Math Functions
        private float EaseOutCubic(float t)
        {
            return 1f - Mathf.Pow(1f - t, 3);
        }

        private float EaseInCubic(float t)
        {
            return t * t * t;
        }

        private float EaseOutBack(float t)
        {
            const float c1 = 1.70158f;
            const float c3 = c1 + 1f;
            return 1f + c3 * Mathf.Pow(t - 1f, 3) + c1 * Mathf.Pow(t - 1f, 2);
        }
        #endregion
    }
}
