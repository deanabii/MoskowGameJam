using UnityEngine;
using MoskowGameJam.Interaction;

namespace MoskowGameJam.ToolInteraction.UI
{
    /// <summary>
    /// Wrapper Bridge untuk menyalurkan sinyal UI Tool Interaction ke PlayerObjectInteraction (Horizontal Layout UI Manager).
    /// Mencegah adanya redundansi Canvas UI ganda.
    /// </summary>
    public class ToolInteractionUI : MonoBehaviour
    {
        public static ToolInteractionUI Instance { get; private set; }

        private void Awake()
        {
            if (Instance == null)
            {
                Instance = this;
            }
            else if (Instance != this)
            {
                Destroy(gameObject);
                return;
            }
        }

        public void ShowDualPrompts(string actionText, Sprite actionSprite, string placingText, Sprite placingSprite)
        {
            if (PlayerObjectInteraction.Instance != null)
            {
                PlayerObjectInteraction.Instance.SetCustomPrompts(
                    showAction: !string.IsNullOrEmpty(actionText), actionText: actionText,
                    showPlacing: !string.IsNullOrEmpty(placingText), placingText: placingText
                );
            }
        }

        public void HideDualPrompts()
        {
            if (PlayerObjectInteraction.Instance != null)
            {
                PlayerObjectInteraction.Instance.ClearCustomPrompts();
            }
        }

        public void ShowExitPrompt(string text, Sprite exitSprite = null)
        {
            if (PlayerObjectInteraction.Instance != null)
            {
                PlayerObjectInteraction.Instance.SetExitPrompt(text);
            }
        }

        public void HideExitPrompt()
        {
            if (PlayerObjectInteraction.Instance != null)
            {
                PlayerObjectInteraction.Instance.ClearCustomPrompts();
            }
        }
    }
}
