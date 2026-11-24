using UnityEngine;

namespace TheBureau.Gameplay.Perception
{
    /// <summary>
    /// Describes a single gameplay noise event that can be perceived by AI.
    ///
    /// This data represents gameplay information only. It is intentionally
    /// separate from audible presentation such as AudioSource playback.
    /// </summary>
    public readonly struct NoiseStimulus
    {
        #region Properties

        /// <summary>
        /// Gets the world-space position where the noise originated.
        /// </summary>
        public Vector3 Position { get; }

        /// <summary>
        /// Gets the maximum base radius within which the noise may be heard.
        /// </summary>
        public float Radius { get; }

        /// <summary>
        /// Gets the relative gameplay importance of the noise.
        /// </summary>
        public NoisePriority Priority { get; }

        /// <summary>
        /// Gets the semantic category of the noise.
        /// </summary>
        public NoiseCategory Category { get; }

        /// <summary>
        /// Gets the GameObject responsible for creating the noise, if known.
        ///
        /// Hearing systems should not treat this as automatic target
        /// identification. It exists as metadata for filtering and future
        /// gameplay rules.
        /// </summary>
        public GameObject Source { get; }

        /// <summary>
        /// Gets the time at which the noise stimulus was created.
        /// </summary>
        public double TimeCreated { get; }

        #endregion

        #region Constructor

        /// <summary>
        /// Creates a new gameplay noise stimulus.
        /// </summary>
        /// <param name="position">
        /// World-space origin of the noise.
        /// </param>
        /// <param name="radius">
        /// Maximum base hearing radius of the noise.
        /// </param>
        /// <param name="priority">
        /// Relative gameplay importance of the noise.
        /// </param>
        /// <param name="category">
        /// Semantic category of the noise.
        /// </param>
        /// <param name="source">
        /// Optional GameObject responsible for creating the noise.
        /// </param>
        public NoiseStimulus(
            Vector3 position,
            float radius,
            NoisePriority priority,
            NoiseCategory category,
            GameObject source = null)
        {
            Position = position;

            Radius = Mathf.Max(
                0f,
                radius
            );

            Priority = priority;
            Category = category;
            Source = source;

            TimeCreated =
                Time.timeAsDouble;
        }

        #endregion
    }
}