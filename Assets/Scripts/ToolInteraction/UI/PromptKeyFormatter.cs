using UnityEngine;

namespace MoskowGameJam.ToolInteraction.UI
{
    public static class PromptKeyFormatter
    {
        public static string GetKeyName(KeyCode key)
        {
            switch (key)
            {
                case KeyCode.Space: return "SPASI";
                case KeyCode.Escape: return "ESC";
                case KeyCode.Return: return "ENTER";
                case KeyCode.LeftShift:
                case KeyCode.RightShift: return "SHIFT";
                case KeyCode.LeftControl:
                case KeyCode.RightControl: return "CTRL";
                case KeyCode.LeftAlt:
                case KeyCode.RightAlt: return "ALT";
                default: return key.ToString().ToUpper();
            }
        }

        public static string FormatPromptText(KeyCode key, string rawText)
        {
            if (string.IsNullOrWhiteSpace(rawText)) return "";

            string trimmed = rawText.Trim();

            // Jika rawText sudah diawali dengan format "[Key]", hapus bracket lama dan ganti dengan key saat ini
            if (trimmed.StartsWith("["))
            {
                int closeBracketIndex = trimmed.IndexOf(']');
                if (closeBracketIndex >= 0 && closeBracketIndex < trimmed.Length - 1)
                {
                    trimmed = trimmed.Substring(closeBracketIndex + 1).Trim();
                }
            }

            return $"[{GetKeyName(key)}] {trimmed}";
        }
    }
}
