using TheBureau.Gameplay.Perception;
using UnityEngine;

namespace TheBureau.Gameplay.Enemies
{
    /// <summary>
    /// Maintains an enemy's interpreted knowledge of perceived stimuli.
    ///
    /// Sensors provide immediate observations while this component owns
    /// short-term perception memory and resolves which remembered stimulus
    /// is currently most relevant for investigation.
    /// </summary>
    [DisallowMultipleComponent]
    public sealed class EnemyPerception : MonoBehaviour
    {
        #region Inspector

        [Header("Sensors")]

        [SerializeField]
        [Tooltip("Vision sensor used to acquire visible player targets.")]
        private EnemyTargetSensor m_VisionSensor;

        [SerializeField]
        [Tooltip("Hearing sensor used to receive evaluated gameplay noises.")]
        private HearingSensor m_HearingSensor;

        [Header("Visual Memory")]

        [SerializeField]
        [Tooltip(
            "How long the enemy remembers the last visually observed " +
            "position after losing direct sight."
        )]
        [Min(0f)]
        private float m_VisualMemoryDuration = 6f;

        [Header("Auditory Memory")]

        [SerializeField]
        [Tooltip(
            "Maximum amount of time an accepted auditory stimulus remains " +
            "relevant to this enemy."
        )]
        [Min(0f)]
        private float m_AuditoryMemoryDuration = 8f;

        [SerializeField]
        [Tooltip(
            "How strongly noise priority contributes to the importance " +
            "of an auditory observation."
        )]
        [Min(0f)]
        private float m_AuditoryPriorityWeight = 0.65f;

        [SerializeField]
        [Tooltip(
            "How strongly perceived audibility contributes to the " +
            "importance of an auditory observation."
        )]
        [Min(0f)]
        private float m_AuditoryAudibilityWeight = 0.35f;

        [Header("Debug")]

        [SerializeField]
        [Tooltip("Logs important perception and memory changes.")]
        private bool m_LogPerception;

        #endregion

        #region Private Fields

        private Transform m_VisibleTarget;

        private Vector3 m_LastSeenPosition;

        private float m_LastVisualContactTime =
            float.NegativeInfinity;

        private bool m_HasVisualMemory;

        private Vector3 m_LastHeardPosition;

        private float m_LastHeardTime =
            float.NegativeInfinity;

        private float m_LastHeardSalience;

        private float m_LastHeardAudibility;

        private NoisePriority m_LastHeardPriority;

        private NoiseCategory m_LastHeardCategory;

        private bool m_HasAuditoryMemory;

        private HearingSensor m_SubscribedHearingSensor;

        #endregion

        #region Properties

        /// <summary>
        /// Gets the target currently visible to this enemy.
        /// </summary>
        public Transform VisibleTarget =>
            m_VisibleTarget;

        /// <summary>
        /// Gets whether the enemy currently has direct visual contact.
        /// </summary>
        public bool HasVisualContact =>
            m_VisibleTarget != null;

        /// <summary>
        /// Gets whether a recent visual memory currently exists.
        /// </summary>
        public bool HasRecentVisualMemory =>
            m_HasVisualMemory &&
            Time.time - m_LastVisualContactTime <=
            m_VisualMemoryDuration;

        /// <summary>
        /// Gets whether a recent auditory memory currently exists.
        /// </summary>
        public bool HasRecentAuditoryMemory =>
            m_HasAuditoryMemory &&
            Time.time - m_LastHeardTime <=
            m_AuditoryMemoryDuration;

        /// <summary>
        /// Gets whether this enemy currently has any useful remembered
        /// stimulus to investigate.
        ///
        /// This property is retained for compatibility with EnemyBrain.
        /// </summary>
        public bool HasRecentMemory =>
            HasRecentVisualMemory ||
            HasRecentAuditoryMemory;

        /// <summary>
        /// Gets the currently resolved position this enemy should
        /// investigate when it has no direct visual target.
        /// </summary>
        public Vector3 InvestigationPosition
        {
            get
            {
                if (TryGetInvestigationPosition(
                        out Vector3 position))
                {
                    return position;
                }

                return transform.position;
            }
        }

        /// <summary>
        /// Gets the currently most relevant remembered position.
        ///
        /// Retained for compatibility with existing EnemyBrain code.
        /// Prefer InvestigationPosition for new code.
        /// </summary>
        public Vector3 LastKnownPosition =>
            InvestigationPosition;

        /// <summary>
        /// Gets the most recent visually observed target position.
        /// </summary>
        public Vector3 LastSeenPosition =>
            m_LastSeenPosition;

        /// <summary>
        /// Gets the most recent accepted auditory stimulus position.
        /// </summary>
        public Vector3 LastHeardPosition =>
            m_LastHeardPosition;

        /// <summary>
        /// Gets the priority of the most recent accepted auditory memory.
        /// </summary>
        public NoisePriority LastHeardPriority =>
            m_LastHeardPriority;

        /// <summary>
        /// Gets the category of the most recent accepted auditory memory.
        /// </summary>
        public NoiseCategory LastHeardCategory =>
            m_LastHeardCategory;

        /// <summary>
        /// Gets the elapsed time since visual contact was last confirmed.
        /// </summary>
        public float TimeSinceVisualContact =>
            Time.time -
            m_LastVisualContactTime;

        /// <summary>
        /// Gets the elapsed time since an auditory observation was last
        /// accepted into memory.
        /// </summary>
        public float TimeSinceAuditoryContact =>
            Time.time -
            m_LastHeardTime;

        #endregion

        #region Unity Lifecycle

        private void Awake()
        {
            CacheComponents();
        }

        private void OnEnable()
        {
            CacheComponents();
            RefreshHearingSubscription();
        }

        private void OnDisable()
        {
            UnsubscribeFromHearingSensor();
        }

        #endregion

        #region Public Methods

        /// <summary>
        /// Updates actively polled perception sources and expires stale
        /// perception memory.
        ///
        /// This should only be called by the authoritative enemy brain.
        /// Hearing itself is event-driven and does not require polling.
        /// </summary>
        public void TickPerception()
        {
            CacheComponents();
            RefreshHearingSubscription();

            UpdateVision();
            ExpireMemories();
        }

        /// <summary>
        /// Immediately clears all current perception and memory.
        /// </summary>
        public void ForgetEverything()
        {
            m_VisibleTarget = null;

            m_HasVisualMemory = false;
            m_LastVisualContactTime =
                float.NegativeInfinity;

            m_HasAuditoryMemory = false;
            m_LastHeardTime =
                float.NegativeInfinity;

            m_LastHeardSalience = 0f;
            m_LastHeardAudibility = 0f;

            if (m_VisionSensor != null)
            {
                m_VisionSensor.ClearTarget();
            }

            if (m_LogPerception)
            {
                Debug.Log(
                    $"[{nameof(EnemyPerception)}] '{name}' cleared all " +
                    $"perception memory.",
                    this
                );
            }
        }

        #endregion

        #region Vision

        private void UpdateVision()
        {
            if (m_VisionSensor == null)
            {
                LoseVisualContact();
                return;
            }

            m_VisionSensor.TickSensor();

            Transform observedTarget =
                m_VisionSensor.CurrentTarget;

            if (observedTarget != null)
            {
                UpdateVisualContact(
                    observedTarget
                );

                return;
            }

            LoseVisualContact();
        }

        private void UpdateVisualContact(
            Transform target)
        {
            bool newlyAcquired =
                m_VisibleTarget != target;

            m_VisibleTarget =
                target;

            m_LastSeenPosition =
                target.position;

            m_LastVisualContactTime =
                Time.time;

            m_HasVisualMemory =
                true;

            if (m_LogPerception &&
                newlyAcquired)
            {
                Debug.Log(
                    $"[{nameof(EnemyPerception)}] '{name}' acquired " +
                    $"visual contact with '{target.name}'.",
                    this
                );
            }
        }

        private void LoseVisualContact()
        {
            if (m_VisibleTarget == null)
            {
                return;
            }

            if (m_LogPerception)
            {
                Debug.Log(
                    $"[{nameof(EnemyPerception)}] '{name}' lost visual " +
                    $"contact. Last seen position: " +
                    $"{m_LastSeenPosition}.",
                    this
                );
            }

            m_VisibleTarget =
                null;
        }

        #endregion

        #region Hearing

        private void HandleNoiseHeard(
            in NoiseStimulus stimulus,
            float audibility)
        {
            float salience =
                CalculateAuditorySalience(
                    stimulus.Priority,
                    audibility
                );

            /*
             * Compare the new observation against the CURRENT value of
             * auditory memory rather than its original strength.
             *
             * This allows an older important sound to gradually become
             * less relevant and eventually be replaced by a newer,
             * weaker stimulus.
             */
            float currentMemoryScore =
                GetAuditoryMemoryScore();

            if (HasRecentAuditoryMemory &&
                salience < currentMemoryScore)
            {
                if (m_LogPerception)
                {
                    Debug.Log(
                        $"[{nameof(EnemyPerception)}] '{name}' ignored " +
                        $"{stimulus.Category} noise.\n" +
                        $"Incoming Score: {salience:F2}\n" +
                        $"Current Auditory Score: {currentMemoryScore:F2}",
                        this
                    );
                }

                return;
            }

            m_LastHeardPosition =
                stimulus.Position;

            m_LastHeardTime =
                Time.time;

            m_LastHeardSalience =
                salience;

            m_LastHeardAudibility =
                audibility;

            m_LastHeardPriority =
                stimulus.Priority;

            m_LastHeardCategory =
                stimulus.Category;

            m_HasAuditoryMemory =
                true;

            if (m_LogPerception)
            {
                Debug.Log(
                    $"[{nameof(EnemyPerception)}] '{name}' remembered " +
                    $"auditory stimulus.\n" +
                    $"Category: {stimulus.Category}\n" +
                    $"Priority: {stimulus.Priority}\n" +
                    $"Position: {stimulus.Position}\n" +
                    $"Audibility: {audibility:F2}\n" +
                    $"Salience: {salience:F2}",
                    this
                );
            }
        }

        #endregion

        #region Memory Resolution

        private bool TryGetInvestigationPosition(
            out Vector3 position)
        {
            bool hasVisualMemory =
                HasRecentVisualMemory;

            bool hasAuditoryMemory =
                HasRecentAuditoryMemory;

            if (!hasVisualMemory &&
                !hasAuditoryMemory)
            {
                position =
                    transform.position;

                return false;
            }

            if (hasVisualMemory &&
                !hasAuditoryMemory)
            {
                position =
                    m_LastSeenPosition;

                return true;
            }

            if (!hasVisualMemory &&
                hasAuditoryMemory)
            {
                position =
                    m_LastHeardPosition;

                return true;
            }

            /*
             * Both memories are currently relevant.
             *
             * Recent visual information begins at maximum confidence and
             * decays over its memory duration.
             *
             * Auditory information begins with its calculated salience
             * and also decays with time.
             *
             * This means recent visual contact generally wins, but an
             * important/new auditory event can redirect investigation
             * after visual information becomes stale.
             */
            float visualScore =
                GetVisualMemoryScore();

            float auditoryScore =
                GetAuditoryMemoryScore();

            position =
                visualScore >= auditoryScore
                    ? m_LastSeenPosition
                    : m_LastHeardPosition;

            return true;
        }

        private float GetVisualMemoryScore()
        {
            if (!HasRecentVisualMemory)
            {
                return 0f;
            }

            if (m_VisualMemoryDuration <= 0f)
            {
                return 0f;
            }

            float age =
                Time.time -
                m_LastVisualContactTime;

            return Mathf.Clamp01(
                1f -
                age / m_VisualMemoryDuration
            );
        }

        private float GetAuditoryMemoryScore()
        {
            if (!HasRecentAuditoryMemory)
            {
                return 0f;
            }

            if (m_AuditoryMemoryDuration <= 0f)
            {
                return 0f;
            }

            float age =
                Time.time -
                m_LastHeardTime;

            float recency =
                Mathf.Clamp01(
                    1f -
                    age / m_AuditoryMemoryDuration
                );

            return m_LastHeardSalience *
                   recency;
        }

        private float CalculateAuditorySalience(
            NoisePriority priority,
            float audibility)
        {
            float priorityValue =
                GetPriorityValue(priority);

            float priorityWeight =
                Mathf.Max(
                    0f,
                    m_AuditoryPriorityWeight
                );

            float audibilityWeight =
                Mathf.Max(
                    0f,
                    m_AuditoryAudibilityWeight
                );

            float totalWeight =
                priorityWeight +
                audibilityWeight;

            if (totalWeight <=
                Mathf.Epsilon)
            {
                return 0f;
            }

            float weightedScore =
                priorityValue *
                priorityWeight +
                Mathf.Clamp01(audibility) *
                audibilityWeight;

            return Mathf.Clamp01(
                weightedScore /
                totalWeight
            );
        }

        private static float GetPriorityValue(
            NoisePriority priority)
        {
            return priority switch
            {
                NoisePriority.Low => 0.25f,
                NoisePriority.Medium => 0.50f,
                NoisePriority.High => 0.75f,
                NoisePriority.Critical => 1.00f,
                _ => 0f
            };
        }

        private void ExpireMemories()
        {
            if (m_HasVisualMemory &&
                !HasRecentVisualMemory)
            {
                m_HasVisualMemory =
                    false;

                if (m_LogPerception)
                {
                    Debug.Log(
                        $"[{nameof(EnemyPerception)}] '{name}' forgot " +
                        $"its visual memory.",
                        this
                    );
                }
            }

            if (m_HasAuditoryMemory &&
                !HasRecentAuditoryMemory)
            {
                m_HasAuditoryMemory =
                    false;

                m_LastHeardSalience =
                    0f;

                m_LastHeardAudibility =
                    0f;

                if (m_LogPerception)
                {
                    Debug.Log(
                        $"[{nameof(EnemyPerception)}] '{name}' forgot " +
                        $"its auditory memory.",
                        this
                    );
                }
            }
        }

        #endregion

        #region Sensor Subscription

        private void RefreshHearingSubscription()
        {
            if (m_SubscribedHearingSensor ==
                m_HearingSensor)
            {
                return;
            }

            UnsubscribeFromHearingSensor();

            if (m_HearingSensor == null)
            {
                return;
            }

            m_HearingSensor.NoiseHeard +=
                HandleNoiseHeard;

            m_SubscribedHearingSensor =
                m_HearingSensor;
        }

        private void UnsubscribeFromHearingSensor()
        {
            if (m_SubscribedHearingSensor == null)
            {
                return;
            }

            m_SubscribedHearingSensor.NoiseHeard -=
                HandleNoiseHeard;

            m_SubscribedHearingSensor =
                null;
        }

        #endregion

        #region Private Methods

        private void CacheComponents()
        {
            if (m_VisionSensor == null)
            {
                m_VisionSensor =
                    GetComponent<EnemyTargetSensor>();
            }

            if (m_HearingSensor == null)
            {
                m_HearingSensor =
                    GetComponent<HearingSensor>();
            }
        }

        #endregion
    }
}