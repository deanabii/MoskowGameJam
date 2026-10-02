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

        [Header("Text References (TextMeshPro)")]
        [Tooltip("Judul resep (misal: 'Traditional Russian Blini')")]
        [SerializeField] private TMP_Text recipeTitleText;

        [Tooltip("Daftar bahan dasar (misal: Flour, Milk, Eggs)")]
        [SerializeField] private TMP_Text baseIngredientsText;

        [Tooltip("Daftar topping (misal: Powdered Sugar, Strawberries)")]
        [SerializeField] private TMP_Text toppingsText;

        [Tooltip("Catatan cara memasak singkat")]
        [SerializeField] private TMP_Text cookingNoteText;

        [Tooltip("Teks petunjuk tombol tutup")]
        [SerializeField] private TMP_Text closeHintText;

        [Header("Auto Trigger on Dialogue")]
        [Tooltip("Otomatis muncul saat NPC dengan nama ini selesai bicara")]
        [SerializeField] private bool autoTriggerOnDialogueEnd = true;
        [SerializeField] private string targetNPCKeyword = "Anastasia";

        [Header("Audio (Opsional)")]
        [Tooltip("Efek suara kertas terbuka")]
        [SerializeField] private AudioClip paperOpenSound;
        [Tooltip("Efek suara kertas tertutup")]
        [SerializeField] private AudioClip paperCloseSound;

        [Header("Key Settings")]
        [SerializeField] private KeyCode closeKey = KeyCode.E;

        private CanvasGroup canvasGroup;
        private bool isOpen = false;
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

            // Dapatkan atau tambahkan CanvasGroup untuk kontrol visibility yang aman
            canvasGroup = GetComponent<CanvasGroup>();
            if (canvasGroup == null && recipeCardPanel != null)
            {
                canvasGroup = recipeCardPanel.GetComponent<CanvasGroup>();
                if (canvasGroup == null)
                {
                    canvasGroup = recipeCardPanel.AddComponent<CanvasGroup>();
                }
            }

            // Auto-resolve referensi text jika belum terpasang
            AutoResolveReferences();

            // Sembunyikan saat inisialisasi awal
            HideVisuals();
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
                else if (cookingNoteText == null && (lower.Contains("note") || lower.Contains("tip") || lower.Contains("cara")))
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
                    Keyboard.current.escapeKey.wasPressedThisFrame ||
                    Keyboard.current.enterKey.wasPressedThisFrame ||
                    Keyboard.current.eKey.wasPressedThisFrame)
                {
                    return true;
                }
                if (closeKey != KeyCode.None && System.Enum.TryParse(closeKey.ToString(), out Key key))
                {
                    if (key != Key.None && Keyboard.current[key].wasPressedThisFrame) return true;
                }
            }
#endif

#if ENABLE_LEGACY_INPUT_MANAGER
            try
            {
                if (Input.GetKeyDown(KeyCode.Space) || Input.GetKeyDown(KeyCode.Escape) ||
                    Input.GetKeyDown(KeyCode.Return) || Input.GetKeyDown(KeyCode.E) ||
                    (closeKey != KeyCode.None && Input.GetKeyDown(closeKey)))
                {
                    return true;
                }
            }
            catch {}
#endif
            return false;
        }

        /// <summary>
        /// Memunculkan Popup Resep Blini khas Rusia
        /// </summary>
        [ContextMenu("Show Blini Recipe")]
        public void ShowBliniRecipe()
        {
            ShowRecipe(
                "Traditional Russian Blini",
                "• Flour (Tepung) - 200g\n• Milk (Susu) - 500ml\n• Eggs (Telur) - 3 pcs",
                "• Powdered Sugar (Gula Bubuk)\n• Fresh Strawberries (Stroberi Segar)",
                "Tip: Whisk batter thoroughly until silky smooth and thin. Pan-fry golden crepes on both sides, dust with powdered sugar, and garnish with fresh strawberries!"
            );
        }

        /// <summary>
        /// Memunculkan Popup Resep Kustom
        /// </summary>
        public void ShowRecipe(string title, string baseIngredients, string toppings, string cookingNote)
        {
            if (recipeTitleText != null) recipeTitleText.text = title;
            if (baseIngredientsText != null) baseIngredientsText.text = baseIngredients;
            if (toppingsText != null) toppingsText.text = toppings;
            if (cookingNoteText != null) cookingNoteText.text = cookingNote;
            if (closeHintText != null) closeHintText.text = "[E / Space / Esc] Close Recipe";

            if (paperOpenSound != null)
            {
                AudioSource.PlayClipAtPoint(paperOpenSound, Camera.main != null ? Camera.main.transform.position : transform.position);
            }

            ShowVisuals();
            isOpen = true;
        }

        public void CloseRecipe()
        {
            isOpen = false;

            if (paperCloseSound != null)
            {
                AudioSource.PlayClipAtPoint(paperCloseSound, Camera.main != null ? Camera.main.transform.position : transform.position);
            }

            HideVisuals();
        }

        private void ShowVisuals()
        {
            if (canvasGroup != null)
            {
                canvasGroup.alpha = 1f;
                canvasGroup.interactable = true;
                canvasGroup.blocksRaycasts = true;
            }
            if (recipeCardPanel != null && recipeCardPanel != gameObject)
            {
                recipeCardPanel.SetActive(true);
            }
            else if (recipeCardPanel != null)
            {
                recipeCardPanel.SetActive(true);
            }
        }

        private void HideVisuals()
        {
            if (canvasGroup != null)
            {
                canvasGroup.alpha = 0f;
                canvasGroup.interactable = false;
                canvasGroup.blocksRaycasts = false;
            }
            else if (recipeCardPanel != null && recipeCardPanel != gameObject)
            {
                recipeCardPanel.SetActive(false);
            }
        }
    }
}
