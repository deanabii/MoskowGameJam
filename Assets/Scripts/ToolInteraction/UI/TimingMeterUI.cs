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

        [Header("Zone Indicators")]
        [SerializeField] private RectTransform greenZoneArea;
        [SerializeField] private RectTransform yellowZoneAreaLeft;
        [SerializeField] private RectTransform yellowZoneAreaRight;

        [Header("Feedback Text")]
        [SerializeField] private TMP_Text feedbackText;
        [SerializeField] private TMP_Text progressText;
        [SerializeField] private Image progressBarFill;

        private float trackWidth = 300f;

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

            if (trackBar != null)
            {
                trackWidth = trackBar.rect.width;
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

        public void UpdateNeedlePosition(float normalizedPosition)
        {
            if (needleTransform == null) return;

            normalizedPosition = Mathf.Clamp01(normalizedPosition);
            if (trackBar != null) trackWidth = trackBar.rect.width;

            // X-position from -trackWidth/2 to +trackWidth/2
            float localX = (normalizedPosition - 0.5f) * trackWidth;
            needleTransform.anchoredPosition = new Vector2(localX, needleTransform.anchoredPosition.y);
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

        public void ConfigureZones(float greenMinNormalized, float greenMaxNormalized, float yellowPaddingNormalized)
        {
            if (trackBar == null) return;
            trackWidth = trackBar.rect.width;

            if (greenZoneArea != null)
            {
                float greenWidth = (greenMaxNormalized - greenMinNormalized) * trackWidth;
                float greenCenterX = ((greenMinNormalized + greenMaxNormalized) / 2f - 0.5f) * trackWidth;
                greenZoneArea.sizeDelta = new Vector2(greenWidth, greenZoneArea.sizeDelta.y);
                greenZoneArea.anchoredPosition = new Vector2(greenCenterX, greenZoneArea.anchoredPosition.y);
            }
        }
    }
}
