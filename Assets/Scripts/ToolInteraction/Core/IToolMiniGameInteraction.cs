using UnityEngine;

namespace MoskowGameJam.ToolInteraction
{
    /// <summary>
    /// Interface untuk mini-game interaksi alat spesifik (seperti putar mouse atau timing meter)
    /// </summary>
    public interface IToolMiniGameInteraction
    {
        bool IsActive { get; }
        bool IsCompleted { get; }
        float ProgressNormalized { get; }

        void OnBeginInteraction();
        void OnUpdateInteraction();
        void OnEndInteraction();
    }
}
