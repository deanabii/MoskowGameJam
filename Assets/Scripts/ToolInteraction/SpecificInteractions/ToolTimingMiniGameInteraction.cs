using UnityEngine;
using UnityEngine.Events;
using MoskowGameJam.ToolInteraction.UI;

#if ENABLE_INPUT_SYSTEM
using UnityEngine.InputSystem;
#endif

namespace MoskowGameJam.ToolInteraction.SpecificInteractions
{
    public enum TimingGameMode
    {
        [Tooltip("Semua input harus Perfect (Hijau). Hit Kuning/Merah tidak dihitung sebagai sukses.")]
        PerfectOnly,

        [Tooltip("Semua input diterima (Merah=1, Kuning=2, Hijau=3). Setiap hit menambah hit count.")]
        AcceptAllHits
    }

    [RequireComponent(typeof(ToolInteractable))]
    public class ToolTimingMiniGameInteraction : MonoBehaviour, IToolMiniGameInteraction
    {
        [Header("Mode Settings")]
        [Tooltip("PerfectOnly: Hanya hit hijau yang dihitung.\nAcceptAllHits: Semua hit (Merah/Kuning/Hijau) diterima.")]
        [SerializeField] private TimingGameMode gameMode = TimingGameMode.AcceptAllHits;

        [Header("Timing Meter Settings")]
        [Tooltip("Kecepatan gerak jarum indikator")]
        [SerializeField] private float needleSpeed = 0.4f;

        [Tooltip("Jumlah penekanan tombol yang dibutuhkan untuk menyelesaikan mini-game")]
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

        [Header("Debug (Runtime)")]
        [Range(0f, 1f)] [SerializeField] private float needlePosition; // 0.0 to 1.0
        [SerializeField] private int currentHitCount;
        [SerializeField] private int totalInputScore;
        [SerializeField] private float calculatedFinalScore;

        private bool isActive;
        private bool isCompleted;
        private bool movingRight = true;
        private ToolInteractable toolInteractable;

        public TimingGameMode GameMode => gameMode;
        public bool IsActive => isActive;
        public bool IsCompleted => isCompleted;
        public float ProgressNormalized => Mathf.Clamp01((float)currentHitCount / requiredSuccessHits);
        public float FinalScore => calculatedFinalScore;

        private void Awake()
        {
            toolInteractable = GetComponent<ToolInteractable>();
        }

        public void OnBeginInteraction()
        {
            isActive = true;
            isCompleted = false;
            currentHitCount = 0;
            totalInputScore = 0;
            calculatedFinalScore = 0f;
            needlePosition = 0f;
            movingRight = true;

            if (TimingMeterUI.Instance != null)
            {
                TimingMeterUI.Instance.ShowMeterUI();
                TimingMeterUI.Instance.ConfigureZones(greenZoneMin, greenZoneMax, yellowZoneMin, yellowZoneMax);
                TimingMeterUI.Instance.UpdateProgress(ProgressNormalized);

                string modeInfo = gameMode == TimingGameMode.PerfectOnly ? " [Hanya HIJAU yang diterima]" : " [Semua Hit Diterima]";
                TimingMeterUI.Instance.SetFeedback($"Tekan [SPASI] saat jarum berayun!{modeInfo}", Color.white);
            }

            Debug.Log($"[ToolTimingMiniGameInteraction] Mini-game timing dimulai pada {gameObject.name} (Mode: {gameMode})");
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
            // Delegate hit evaluation to UI for exact matching
            int hitResult = 0; // 2 = Green, 1 = Yellow, 0 = Red
            if (TimingMeterUI.Instance != null)
            {
                hitResult = TimingMeterUI.Instance.EvaluateHit(needlePosition);
            }
            else
            {
                if (needlePosition >= greenZoneMin && needlePosition <= greenZoneMax)
                    hitResult = 2;
                else if (needlePosition >= yellowZoneMin && needlePosition <= yellowZoneMax)
                    hitResult = 1;
                else
                    hitResult = 0;
            }

            // Nilai hit: Merah = 1, Kuning = 2, Hijau = 3
            int hitScore = hitResult == 2 ? 3 : (hitResult == 1 ? 2 : 1);
            string zoneName = hitResult == 2 ? "GREEN (3 pts)" : hitResult == 1 ? "YELLOW (2 pts)" : "RED (1 pt)";

            Debug.Log($"[TimingDebug] HIT at {needlePosition * 100f:F1}% → {zoneName} | Mode={gameMode} | ResultCode={hitResult}");

            bool hitAccepted = false;

            if (gameMode == TimingGameMode.PerfectOnly)
            {
                if (hitResult == 2) // Perfect Green Only
                {
                    hitAccepted = true;
                    totalInputScore += hitScore;
                    currentHitCount++;
                    OnPerfectHit?.Invoke();

                    if (TimingMeterUI.Instance != null)
                    {
                        TimingMeterUI.Instance.SetFeedback("SEMPURNA! 🎯 (+3)", Color.green);
                    }
                }
                else if (hitResult == 1) // Yellow
                {
                    OnGoodHit?.Invoke();
                    if (TimingMeterUI.Instance != null)
                    {
                        TimingMeterUI.Instance.SetFeedback("KUNING! Mode Perfect: Coba pas di hijau!", Color.yellow);
                    }
                }
                else // Red
                {
                    if (currentHitCount > 0)
                    {
                        currentHitCount--;
                        totalInputScore = Mathf.Max(0, totalInputScore - 3);
                    }
                    OnMissHit?.Invoke();
                    if (TimingMeterUI.Instance != null)
                    {
                        TimingMeterUI.Instance.SetFeedback("MELESET! ❌ Progress berkurang", Color.red);
                    }
                }
            }
            else // AcceptAllHits Mode
            {
                hitAccepted = true;
                totalInputScore += hitScore;
                currentHitCount++;

                switch (hitResult)
                {
                    case 2:
                        OnPerfectHit?.Invoke();
                        if (TimingMeterUI.Instance != null)
                        {
                            TimingMeterUI.Instance.SetFeedback("SEMPURNA! 🎯 (+3)", Color.green);
                        }
                        break;
                    case 1:
                        OnGoodHit?.Invoke();
                        if (TimingMeterUI.Instance != null)
                        {
                            TimingMeterUI.Instance.SetFeedback("BAGUS! ⚠️ (+2)", Color.yellow);
                        }
                        break;
                    default:
                        OnMissHit?.Invoke();
                        if (TimingMeterUI.Instance != null)
                        {
                            TimingMeterUI.Instance.SetFeedback("KURANG PAS! ❌ (+1)", Color.red);
                        }
                        break;
                }
            }

            if (TimingMeterUI.Instance != null)
            {
                TimingMeterUI.Instance.UpdateProgress(ProgressNormalized);
            }

            // Check completion
            if (currentHitCount >= requiredSuccessHits)
            {
                CompleteMiniGame();
            }
        }

        private void CompleteMiniGame()
        {
            isCompleted = true;

            // Hitungan Nilai Total: Total input dari user / (jumlah required hits x 3)
            int maxPossibleScore = requiredSuccessHits * 3;
            calculatedFinalScore = Mathf.Clamp01((float)totalInputScore / maxPossibleScore);

            Debug.Log($"<color=cyan>[ToolTimingMiniGameInteraction] Mini-game SELESAI di {gameObject.name}! " +
                      $"Total Input Score: {totalInputScore}/{maxPossibleScore} | Final Score: {calculatedFinalScore:F2} ({calculatedFinalScore * 100f:F0}%)</color>");

            // Tambahkan nilai ke list di ToolInteractable:
            // 1. Skor Bahan DULU (sesuai requiredObjects)
            // 2. Skor Minigame (performa timing player)
            if (toolInteractable == null)
            {
                toolInteractable = GetComponent<ToolInteractable>();
            }

            if (toolInteractable != null)
            {
                float ingredientScore = toolInteractable.CalculateIngredientScore();
                toolInteractable.AddScore(ingredientScore); // 1. Masukkan nilai bahan dulu
                toolInteractable.AddScore(calculatedFinalScore); // 2. Masukkan nilai minigame
            }
            else
            {
                Debug.LogWarning($"[ToolTimingMiniGameInteraction] ToolInteractable tidak ditemukan pada {gameObject.name} untuk menambahkan skor!");
            }

            OnTimingCompleted?.Invoke();

            if (TimingMeterUI.Instance != null)
            {
                TimingMeterUI.Instance.SetFeedback($"SELESAI! Nilai: {calculatedFinalScore * 100f:F0}% 🏆", Color.cyan);
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
