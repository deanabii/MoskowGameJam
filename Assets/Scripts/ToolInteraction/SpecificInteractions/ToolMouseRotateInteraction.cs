using UnityEngine;
using UnityEngine.Events;

#if ENABLE_INPUT_SYSTEM
using UnityEngine.InputSystem;
#endif

namespace MoskowGameJam.ToolInteraction.SpecificInteractions
{
    [RequireComponent(typeof(ToolInteractable))]
    public class ToolMouseRotateInteraction : MonoBehaviour, IToolMiniGameInteraction
    {
        [Header("Rotation Settings")]
        [Tooltip("Jumlah putaran penuh (360 derajat) yang dibutuhkan untuk menyelesaikan interaksi")]
        [SerializeField] private float requiredFullRotations = 5f;

        [Tooltip("Sensitivitas deteksi pergerakan mouse / analog")]
        [SerializeField] private float sensitivity = 1.0f;

        [Header("Events")]
        public UnityEvent OnRotationProgressChanged;
        public UnityEvent OnRotationCompleted;

        private bool isActive;
        private bool isCompleted;
        private float currentAccumulatedDegrees;
        private Vector2 previousMouseVector;

        public bool IsActive => isActive;
        public bool IsCompleted => isCompleted;
        public float ProgressNormalized => Mathf.Clamp01(currentAccumulatedDegrees / (requiredFullRotations * 360f));

        public void OnBeginInteraction()
        {
            isActive = true;
            isCompleted = false;
            currentAccumulatedDegrees = 0f;
            previousMouseVector = Vector2.zero;
            Debug.Log($"[ToolMouseRotateInteraction] Interaksi putar mouse dimulai pada {gameObject.name}");
        }

        public void OnUpdateInteraction()
        {
            if (!isActive || isCompleted) return;

            Vector2 inputDelta = GetInputDelta();

            if (inputDelta.sqrMagnitude > 0.001f)
            {
                // Hitung gerakan rotasi melingkar
                float deltaAngle = Mathf.Atan2(inputDelta.y, inputDelta.x) * Mathf.Rad2Deg;
                
                if (previousMouseVector != Vector2.zero)
                {
                    float angleDifference = Mathf.DeltaAngle(
                        Mathf.Atan2(previousMouseVector.y, previousMouseVector.x) * Mathf.Rad2Deg,
                        deltaAngle
                    );

                    // Tambahkan akumulasi rotasi secara absolut
                    currentAccumulatedDegrees += Mathf.Abs(angleDifference) * sensitivity;
                    OnRotationProgressChanged?.Invoke();

                    if (currentAccumulatedDegrees >= requiredFullRotations * 360f)
                    {
                        isCompleted = true;
                        OnRotationCompleted?.Invoke();
                        Debug.Log($"[ToolMouseRotateInteraction] Sukses! Putaran selesai pada {gameObject.name}");
                    }
                }

                previousMouseVector = inputDelta;
            }
        }

        public void OnEndInteraction()
        {
            isActive = false;
            Debug.Log($"[ToolMouseRotateInteraction] Interaksi putar mouse diakhiri pada {gameObject.name}");
        }

        private Vector2 GetInputDelta()
        {
            Vector2 delta = Vector2.zero;

#if ENABLE_INPUT_SYSTEM
            if (Mouse.current != null)
            {
                delta = Mouse.current.delta.ReadValue();
            }
            if (delta.sqrMagnitude < 0.01f && Gamepad.current != null)
            {
                delta = Gamepad.current.rightStick.ReadValue();
            }
#endif

#if ENABLE_LEGACY_INPUT_MANAGER
            if (delta.sqrMagnitude < 0.01f)
            {
                delta = new Vector2(Input.GetAxis("Mouse X"), Input.GetAxis("Mouse Y"));
            }
#endif

            return delta;
        }
    }
}
