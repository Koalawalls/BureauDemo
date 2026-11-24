using System;
using UnityEngine;

namespace TheBureau.Gameplay.Perception
{
    /// <summary>
    /// Evaluates gameplay noise stimuli from the perspective of a single
    /// listener.
    ///
    /// This component determines whether a noise was audible and reports
    /// its resulting audibility. It does not remember noises, identify
    /// targets, or decide how the listener should react.
    /// </summary>
    [DisallowMultipleComponent]
    public sealed class HearingSensor : MonoBehaviour, INoiseReceiver
    {
        #region Delegates

        /// <summary>
        /// Called when this sensor successfully perceives a noise stimulus.
        /// </summary>
        /// <param name="stimulus">
        /// Noise stimulus that was heard.
        /// </param>
        /// <param name="audibility">
        /// Perceived audibility from zero to one after distance,
        /// sensitivity, and occlusion are evaluated.
        /// </param>
        public delegate void NoiseHeardHandler(
            in NoiseStimulus stimulus,
            float audibility
        );

        #endregion

        #region Events

        /// <summary>
        /// Raised whenever this sensor successfully hears a noise.
        ///
        /// Higher-level perception systems should subscribe to this event
        /// to decide whether the observation should be remembered or acted
        /// upon.
        /// </summary>
        public event NoiseHeardHandler NoiseHeard;

        #endregion

        #region Inspector

        [Header("Hearing")]

        [SerializeField]
        [Tooltip(
            "Optional transform used as the origin of hearing checks. " +
            "If omitted, this GameObject's position plus the hearing " +
            "height offset is used."
        )]
        private Transform m_HearingOrigin;

        [SerializeField]
        [Tooltip(
            "Vertical offset used when no explicit hearing origin is assigned."
        )]
        private float m_HearingHeightOffset = 1.5f;

        [SerializeField]
        [Tooltip(
            "Multiplier applied to the base radius of incoming noise " +
            "stimuli. Values above 1 increase hearing range."
        )]
        [Min(0f)]
        private float m_HearingSensitivity = 1f;

        [SerializeField]
        [Tooltip(
            "Minimum resulting audibility required for a noise to be " +
            "considered heard."
        )]
        [Range(0f, 1f)]
        private float m_MinimumAudibility = 0.05f;

        [Header("Occlusion")]

        [SerializeField]
        [Tooltip(
            "Layers that can obstruct sound between the noise source " +
            "and this sensor."
        )]
        private LayerMask m_OcclusionLayers;

        [SerializeField]
        [Tooltip(
            "Multiplier applied to perceived audibility when an " +
            "occluding surface exists between the listener and noise."
        )]
        [Range(0f, 1f)]
        private float m_OccludedAudibilityMultiplier = 0.35f;

        [Header("Filtering")]

        [SerializeField]
        [Tooltip(
            "Prevents this listener from reacting to noise generated " +
            "by itself or another object under the same root."
        )]
        private bool m_IgnoreOwnSource = true;

        [Header("Debug")]

        [SerializeField]
        [Tooltip("Logs successfully perceived noise stimuli.")]
        private bool m_LogHeardNoises;

        [SerializeField]
        [Tooltip("Logs noise stimuli that fail hearing evaluation.")]
        private bool m_LogRejectedNoises;

        #endregion

        #region Properties

        /// <summary>
        /// Gets the world-space position used for hearing calculations.
        /// </summary>
        public Vector3 HearingPosition
        {
            get
            {
                if (m_HearingOrigin != null)
                {
                    return m_HearingOrigin.position;
                }

                return transform.position +
                       Vector3.up * m_HearingHeightOffset;
            }
        }

        /// <summary>
        /// Gets the current hearing sensitivity multiplier.
        /// </summary>
        public float HearingSensitivity =>
            m_HearingSensitivity;

        #endregion

        #region Unity Lifecycle

        private void OnEnable()
        {
            NoiseSystem.Register(this);
        }

        private void OnDisable()
        {
            NoiseSystem.Unregister(this);
        }

        private void OnDestroy()
        {
            /*
             * OnDisable normally handles this, but explicitly unregistering
             * here keeps cleanup defensive if object lifetime behavior
             * changes in the future.
             */
            NoiseSystem.Unregister(this);
        }

        #endregion

        #region INoiseReceiver

        /// <inheritdoc />
        public void ReceiveNoise(
            in NoiseStimulus stimulus)
        {
            if (!isActiveAndEnabled)
            {
                return;
            }

            if (!TryEvaluateNoise(
                    in stimulus,
                    out float audibility))
            {
                if (m_LogRejectedNoises)
                {
                    Debug.Log(
                        $"[{nameof(HearingSensor)}] '{name}' rejected " +
                        $"{stimulus.Category} noise from " +
                        $"{stimulus.Position}.",
                        this
                    );
                }

                return;
            }

            if (m_LogHeardNoises)
            {
                Debug.Log(
                    $"[{nameof(HearingSensor)}] '{name}' heard noise.\n" +
                    $"Category: {stimulus.Category}\n" +
                    $"Priority: {stimulus.Priority}\n" +
                    $"Position: {stimulus.Position}\n" +
                    $"Base Radius: {stimulus.Radius:F2}\n" +
                    $"Audibility: {audibility:F2}",
                    this
                );
            }

            NoiseHeard?.Invoke(
                in stimulus,
                audibility
            );
        }

        #endregion

        #region Public Methods

        /// <summary>
        /// Evaluates whether a noise stimulus can be perceived by this
        /// listener.
        /// </summary>
        /// <param name="stimulus">
        /// Noise stimulus being evaluated.
        /// </param>
        /// <param name="audibility">
        /// Resulting perceived audibility from zero to one.
        /// </param>
        /// <returns>
        /// True if the noise exceeds the minimum audible threshold.
        /// </returns>
        public bool TryEvaluateNoise(
            in NoiseStimulus stimulus,
            out float audibility)
        {
            audibility = 0f;

            if (stimulus.Radius <= 0f)
            {
                return false;
            }

            if (ShouldIgnoreSource(stimulus.Source))
            {
                return false;
            }

            float effectiveRadius =
                stimulus.Radius *
                m_HearingSensitivity;

            if (effectiveRadius <= 0f)
            {
                return false;
            }

            Vector3 hearingPosition =
                HearingPosition;

            float distance =
                Vector3.Distance(
                    hearingPosition,
                    stimulus.Position
                );

            if (distance > effectiveRadius)
            {
                return false;
            }

            /*
             * Linear distance attenuation:
             *
             * source             edge
             * 1.0 ---------------- 0.0
             *
             * This is intentionally kept inside HearingSensor so the
             * distribution system remains unaware of listener-specific
             * hearing rules.
             */
            float normalizedDistance =
                Mathf.Clamp01(
                    distance / effectiveRadius
                );

            audibility =
                1f - normalizedDistance;

            if (IsOccluded(
                    hearingPosition,
                    stimulus.Position))
            {
                audibility *=
                    m_OccludedAudibilityMultiplier;
            }

            audibility =
                Mathf.Clamp01(audibility);

            return audibility >=
                   m_MinimumAudibility;
        }

        #endregion

        #region Private Methods

        private bool ShouldIgnoreSource(
            GameObject source)
        {
            if (!m_IgnoreOwnSource ||
                source == null)
            {
                return false;
            }

            Transform sourceTransform =
                source.transform;

            if (sourceTransform == transform)
            {
                return true;
            }

            return sourceTransform.root ==
                   transform.root;
        }

        private bool IsOccluded(
            Vector3 listenerPosition,
            Vector3 noisePosition)
        {
            if (m_OcclusionLayers.value == 0)
            {
                return false;
            }

            return Physics.Linecast(
                listenerPosition,
                noisePosition,
                m_OcclusionLayers,
                QueryTriggerInteraction.Ignore
            );
        }

        #endregion
    }
}