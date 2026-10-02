using UnityEngine;
using UnityEngine.UI;
using TMPro;

namespace MoskowGameJam.ToolInteraction.UI
{
    public class TimingMeterUI : MonoBehaviour
    {
        public static TimingMeterUI Instance { get; private set; }

        [Header("Meter Panel Root")]
        [SerializeField] private GameObject meterPanelRoot;

        [Header("Needle & Meter Track")]
        [SerializeField] private RectTransform needleTransform;
        [SerializeField] private RectTransform trackBar;

        [Header("Zone Indicators (Anchored)")]
        [SerializeField] private RectTransform greenZoneArea;
        [SerializeField] private RectTransform yellowZoneAreaLeft;
        [SerializeField] private RectTransform yellowZoneAreaRight;

        [Header("Feedback Text")]
        [SerializeField] private TMP_Text feedbackText;
        [SerializeField] private TMP_Text progressText;
        [SerializeField] private Image progressBarFill;

        // Cached zone boundaries (normalized 0-1) for accurate hit detection
        private float cachedGreenMin;
        private float cachedGreenMax;
        private float cachedYellowMin;
        private float cachedYellowMax;

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

            HideMeterUI();
        }

        public void ShowMeterUI()
        {
            if (meterPanelRoot != null)
            {
                meterPanelRoot.SetActive(true);
            }
        }

        public void HideMeterUI()
        {
            if (meterPanelRoot != null)
            {
                meterPanelRoot.SetActive(false);
            }
        }

        /// <summary>
        /// Configure zone visuals using anchors. All offsets are set to zero,
        /// so the zone fills exactly from anchorMin to anchorMax relative to parent.
        /// This guarantees the visual zones and hit detection use the exact same 0-1 values.
        /// </summary>
        public void ConfigureZones(float greenMin, float greenMax, float yellowMin, float yellowMax)
        {
            // Clamp and validate
            greenMin = Mathf.Clamp01(greenMin);
            greenMax = Mathf.Clamp01(greenMax);
            yellowMin = Mathf.Clamp01(yellowMin);
            yellowMax = Mathf.Clamp01(yellowMax);

            // Ensure correct ordering
            if (greenMin > greenMax) (greenMin, greenMax) = (greenMax, greenMin);
            if (yellowMin > yellowMax) (yellowMin, yellowMax) = (yellowMax, yellowMin);

            // Yellow must enclose green
            yellowMin = Mathf.Min(yellowMin, greenMin);
            yellowMax = Mathf.Max(yellowMax, greenMax);

            // Cache for hit detection
            cachedGreenMin = greenMin;
            cachedGreenMax = greenMax;
            cachedYellowMin = yellowMin;
            cachedYellowMax = yellowMax;

            // --- Green zone: anchor X from greenMin to greenMax ---
            if (greenZoneArea != null)
            {
                SetAnchorStretch(greenZoneArea, greenMin, greenMax);
            }

            // --- Yellow zone left: anchor X from yellowMin to greenMin ---
            if (yellowZoneAreaLeft != null)
            {
                SetAnchorStretch(yellowZoneAreaLeft, yellowMin, greenMin);
            }

            // --- Yellow zone right: anchor X from greenMax to yellowMax ---
            if (yellowZoneAreaRight != null)
            {
                SetAnchorStretch(yellowZoneAreaRight, greenMax, yellowMax);
            }
        }

        /// <summary>
        /// Sets a RectTransform to stretch-fill from xMin to xMax (normalized 0-1)
        /// with all offsets at zero, so it fills exactly the anchor range.
        /// Y anchors stretch full height (0 to 1).
        /// </summary>
        private void SetAnchorStretch(RectTransform rt, float xMin, float xMax)
        {
            rt.anchorMin = new Vector2(xMin, 0f);
            rt.anchorMax = new Vector2(xMax, 1f);
            rt.offsetMin = Vector2.zero; // Left, Bottom = 0
            rt.offsetMax = Vector2.zero; // Right, Top = 0
        }

        /// <summary>
        /// Evaluates a normalized needle position (0-1) against the cached zone boundaries.
        /// Returns: 2 = green (perfect), 1 = yellow (good), 0 = red (miss).
        /// </summary>
        public int EvaluateHit(float normalizedPosition)
        {
            if (normalizedPosition >= cachedGreenMin && normalizedPosition <= cachedGreenMax)
                return 2; // Green / Perfect
            if (normalizedPosition >= cachedYellowMin && normalizedPosition <= cachedYellowMax)
                return 1; // Yellow / Good
            return 0; // Red / Miss
        }

        /// <summary>
        /// Moves the needle by setting its anchor X to the normalized position.
        /// Since zones also use anchors in the same 0-1 space, alignment is guaranteed.
        /// </summary>
        public void UpdateNeedlePosition(float normalizedPosition)
        {
            if (needleTransform == null) return;

            normalizedPosition = Mathf.Clamp01(normalizedPosition);

            // Anchor X = normalized position, keep Y anchors unchanged
            needleTransform.anchorMin = new Vector2(normalizedPosition, needleTransform.anchorMin.y);
            needleTransform.anchorMax = new Vector2(normalizedPosition, needleTransform.anchorMax.y);

            // Zero X offset so needle sits exactly at the anchor line
            needleTransform.anchoredPosition = new Vector2(0f, needleTransform.anchoredPosition.y);
        }

        public void UpdateProgress(float progressNormalized)
        {
            if (progressBarFill != null)
            {
                progressBarFill.fillAmount = Mathf.Clamp01(progressNormalized);
            }

            if (progressText != null)
            {
                progressText.text = $"Progress: {Mathf.RoundToInt(progressNormalized * 100)}%";
            }
        }

        public void SetFeedback(string text, Color color)
        {
            if (feedbackText != null)
            {
                feedbackText.text = text;
                feedbackText.color = color;
            }
        }
    }
}
