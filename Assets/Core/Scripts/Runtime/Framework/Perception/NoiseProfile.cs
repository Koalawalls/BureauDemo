using UnityEngine;

namespace TheBureau.Gameplay.Perception
{
    /// <summary>
    /// Defines reusable gameplay properties for an AI-perceivable noise.
    ///
    /// Noise profiles describe gameplay perception only and are separate
    /// from presentation audio such as SoundDef or AudioClip assets.
    /// </summary>
    [CreateAssetMenu(
        fileName = "NoiseProfile_",
        menuName = "The Bureau/Gameplay/Perception/Noise Profile"
    )]
    public sealed class NoiseProfile : ScriptableObject
    {
        #region Inspector

        [Header("Classification")]

        [SerializeField]
        [Tooltip("Semantic category of the emitted gameplay noise.")]
        private NoiseCategory m_Category =
            NoiseCategory.Environment;

        [SerializeField]
        [Tooltip("Relative gameplay importance of the emitted noise.")]
        private NoisePriority m_Priority =
            NoisePriority.Medium;

        [Header("Audibility")]

        [SerializeField]
        [Tooltip(
            "Maximum base distance at which this noise can potentially " +
            "be perceived before listener sensitivity is applied."
        )]
        [Min(0f)]
        private float m_Radius = 10f;

        #endregion

        #region Properties

        /// <summary>
        /// Gets the semantic category of this noise.
        /// </summary>
        public NoiseCategory Category =>
            m_Category;

        /// <summary>
        /// Gets the relative gameplay importance of this noise.
        /// </summary>
        public NoisePriority Priority =>
            m_Priority;

        /// <summary>
        /// Gets the base hearing radius of this noise.
        /// </summary>
        public float Radius =>
            m_Radius;

        #endregion
    }
}