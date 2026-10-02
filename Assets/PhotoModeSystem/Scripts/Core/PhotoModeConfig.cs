using UnityEngine;

namespace PhotoModeSystem
{
    [CreateAssetMenu(fileName = "PhotoModeConfig", menuName = "Photo Mode/Config", order = 1)]
    public class PhotoModeConfig : ScriptableObject
    {
        [Header("Follower Calculation Ratio")]
        [Tooltip("Rasio minimum penambahan followers (Default: 0.5 = 1/2)")]
        [Range(0f, 5f)] public float minFollowerRatio = 0.5f;

        [Tooltip("Rasio maksimum penambahan followers (Default: 0.75 = 3/4)")]
        [Range(0f, 5f)] public float maxFollowerRatio = 0.75f;

        [Header("Like Calculation Ratio")]
        [Tooltip("Rasio minimum penambahan likes berdasarkan follower (Default: 0.75 = 3/4)")]
        [Range(0f, 10f)] public float minLikeRatio = 0.75f;

        [Tooltip("Rasio maksimum penambahan likes berdasarkan follower (Default: 2.0)")]
        [Range(0f, 10f)] public float maxLikeRatio = 2.0f;

        [Header("Gold Donation Ratio")]
        [Tooltip("Rasio minimum penambahan gold donasi berdasarkan follower (Default: 0.75 = 3/4)")]
        [Range(0f, 10f)] public float minGoldRatio = 0.75f;

        [Tooltip("Rasio maksimum penambahan gold donasi berdasarkan follower (Default: 2.0)")]
        [Range(0f, 10f)] public float maxGoldRatio = 2.0f;

        [Tooltip("Pengali dasar donasi gold (Default: 100)")]
        public float goldBaseMultiplier = 100f;
    }
}
