using UnityEngine;
using UnityEngine.Events;
using MoskowGameJam.ToolInteraction.UI;

#if ENABLE_INPUT_SYSTEM
using UnityEngine.InputSystem;
#endif

namespace MoskowGameJam.ToolInteraction.SpecificInteractions
{
    [RequireComponent(typeof(ToolInteractable))]
    public class ToolTimingMiniGameInteraction : MonoBehaviour, IToolMiniGameInteraction
    {
        [Header("Timing Meter Settings")]
        [Tooltip("Kecepatan gerak jarum indikator")]
        [SerializeField] private float needleSpeed = 1.2f;

        [Tooltip("Jumlah penekanan tombol sukses di area hijau yang dibutuhkan (misal: 3 kali)")]
        [SerializeField] private int requiredSuccessHits = 3;

        [Header("Zone Bounds (Normalized 0.0 - 1.0)")]
        [Range(0f, 1f)] [SerializeField] private float greenZoneMin = 0.40f;
        [Range(0f, 1f)] [SerializeField] private float greenZoneMax = 0.60f;
        [Range(0f, 1f)] [SerializeField] private float yellowZoneMin = 0.25f;
        [Range(0f, 1f)] [SerializeField] private float yellowZoneMax = 0.75f;

        [Header("Events")]
        public UnityEvent OnPerfectHit;
        public UnityEvent OnGoodHit;
        public UnityEvent OnMissHit;
        public UnityEvent OnTimingCompleted;

        private bool isActive;
        private bool isCompleted;
        private int currentSuccessCount;
        private float needlePosition; // 0.0 to 1.0
        private bool movingRight = true;

        public bool IsActive => isActive;
        public bool IsCompleted => isCompleted;
        public float ProgressNormalized => Mathf.Clamp01((float)currentSuccessCount / requiredSuccessHits);

        public void OnBeginInteraction()
        {
            isActive = true;
            isCompleted = false;
            currentSuccessCount = 0;
            needlePosition = 0f;
            movingRight = true;

            if (TimingMeterUI.Instance != null)
            {
                TimingMeterUI.Instance.ShowMeterUI();
                TimingMeterUI.Instance.ConfigureZones(greenZoneMin, greenZoneMax, yellowZoneMin);
                TimingMeterUI.Instance.UpdateProgress(ProgressNormalized);
                TimingMeterUI.Instance.SetFeedback("Tekan [SPASI] saat jarum di area HIJAU!", Color.white);
            }

            Debug.Log($"[ToolTimingMiniGameInteraction] Mini-game timing dimulai pada {gameObject.name}");
        }

        public void OnUpdateInteraction()
        {
            if (!isActive || isCompleted) return;

            // Oscillate needle position between 0.0 and 1.0
            float step = needleSpeed * Time.deltaTime;
            if (movingRight)
            {
                needlePosition += step;
                if (needlePosition >= 1.0f)
                {
                    needlePosition = 1.0f;
                    movingRight = false;
                }
            }
            else
            {
                needlePosition -= step;
                if (needlePosition <= 0.0f)
                {
                    needlePosition = 0.0f;
                    movingRight = true;
                }
            }

            if (TimingMeterUI.Instance != null)
            {
                TimingMeterUI.Instance.UpdateNeedlePosition(needlePosition);
            }

            // Check Space key input
            if (IsSpacePressedThisFrame())
            {
                EvaluateTimingHit();
            }
        }

        public void OnEndInteraction()
        {
            isActive = false;

            if (TimingMeterUI.Instance != null)
            {
                TimingMeterUI.Instance.HideMeterUI();
            }

            Debug.Log($"[ToolTimingMiniGameInteraction] Mini-game timing diakhiri pada {gameObject.name}");
        }

        private void EvaluateTimingHit()
        {
            if (needlePosition >= greenZoneMin && needlePosition <= greenZoneMax)
            {
                // Perfect Green Zone
                currentSuccessCount++;
                OnPerfectHit?.Invoke();

                if (TimingMeterUI.Instance != null)
                {
                    TimingMeterUI.Instance.SetFeedback("SEMPURNA! 🎯", Color.green);
                    TimingMeterUI.Instance.UpdateProgress(ProgressNormalized);
                }

                Debug.Log($"[ToolTimingMiniGameInteraction] Perfect Hit! ({currentSuccessCount}/{requiredSuccessHits})");

                if (currentSuccessCount >= requiredSuccessHits)
                {
                    isCompleted = true;
                    OnTimingCompleted?.Invoke();
                    if (TimingMeterUI.Instance != null)
                    {
                        TimingMeterUI.Instance.SetFeedback("SELESAI! SANGAT BAGUS!", Color.cyan);
                    }
                    Debug.Log($"[ToolTimingMiniGameInteraction] Sukses! Mini-game timing selesai pada {gameObject.name}");
                }
            }
            else if (needlePosition >= yellowZoneMin && needlePosition <= yellowZoneMax)
            {
                // Good Yellow Zone
                OnGoodHit?.Invoke();
                if (TimingMeterUI.Instance != null)
                {
                    TimingMeterUI.Instance.SetFeedback("BAGUS! Coba pas di hijau!", Color.yellow);
                }
                Debug.Log("[ToolTimingMiniGameInteraction] Good Hit!");
            }
            else
            {
                // Miss Red Zone
                if (currentSuccessCount > 0) currentSuccessCount--;
                OnMissHit?.Invoke();

                if (TimingMeterUI.Instance != null)
                {
                    TimingMeterUI.Instance.SetFeedback("MELESET! ❌", Color.red);
                    TimingMeterUI.Instance.UpdateProgress(ProgressNormalized);
                }
                Debug.Log("[ToolTimingMiniGameInteraction] Miss Hit!");
            }
        }

        private bool IsSpacePressedThisFrame()
        {
#if ENABLE_INPUT_SYSTEM
            if (Keyboard.current != null && Keyboard.current.spaceKey.wasPressedThisFrame)
            {
                return true;
            }
#endif

#if ENABLE_LEGACY_INPUT_MANAGER
            try
            {
                return Input.GetKeyDown(KeyCode.Space);
            }
            catch {}
#endif

            return false;
        }
    }
}
