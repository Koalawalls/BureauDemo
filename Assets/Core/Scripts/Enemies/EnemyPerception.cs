using UnityEngine;

namespace TheBureau.Gameplay.Enemies
{
    /// <summary>
    /// Maintains an enemy's knowledge about perceived targets.
    /// Sensors provide immediate observations while this component
    /// preserves short-term memory such as the last known target position.
    /// </summary>
    [DisallowMultipleComponent]
    public sealed class EnemyPerception : MonoBehaviour
    {
        #region Inspector

        [Header("Sensors")]

        [SerializeField]
        [Tooltip("Vision sensor used to acquire visible player targets.")]
        private EnemyTargetSensor m_VisionSensor;

        [Header("Memory")]

        [SerializeField]
        [Tooltip(
            "How long the enemy remembers the last observed position " +
            "after losing visual contact."
        )]
        [Min(0f)]
        private float m_VisualMemoryDuration = 6f;

        [Header("Debug")]

        [SerializeField]
        [Tooltip("Logs changes in visual contact and memory.")]
        private bool m_LogPerception;

        #endregion

        #region Private Fields

        private Transform m_VisibleTarget;

        private Vector3 m_LastKnownPosition;

        private float m_LastVisualContactTime =
            float.NegativeInfinity;

        private bool m_HasLastKnownPosition;

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
        /// Gets whether a remembered target position is still considered
        /// relevant.
        /// </summary>
        public bool HasRecentMemory =>
            m_HasLastKnownPosition &&
            Time.time - m_LastVisualContactTime <=
            m_VisualMemoryDuration;

        /// <summary>
        /// Gets the last position at which the target was observed.
        /// </summary>
        public Vector3 LastKnownPosition =>
            m_LastKnownPosition;

        /// <summary>
        /// Gets the elapsed time since visual contact was last confirmed.
        /// </summary>
        public float TimeSinceVisualContact =>
            Time.time - m_LastVisualContactTime;

        #endregion

        #region Unity Lifecycle

        private void Awake()
        {
            CacheComponents();
        }

        #endregion

        #region Public Methods

        /// <summary>
        /// Updates all currently supported perception sources and memory.
        ///
        /// This should only be called by the authoritative enemy brain.
        /// </summary>
        public void TickPerception()
        {
            CacheComponents();

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

            if (m_HasLastKnownPosition &&
                !HasRecentMemory)
            {
                ForgetLastKnownPosition();
            }
        }

        /// <summary>
        /// Immediately clears all current perception and memory.
        /// </summary>
        public void ForgetEverything()
        {
            m_VisibleTarget = null;
            m_HasLastKnownPosition = false;
            m_LastVisualContactTime =
                float.NegativeInfinity;

            if (m_VisionSensor != null)
            {
                m_VisionSensor.ClearTarget();
            }
        }

        #endregion

        #region Perception

        private void UpdateVisualContact(
            Transform target)
        {
            bool newlyAcquired =
                m_VisibleTarget != target;

            m_VisibleTarget =
                target;

            m_LastKnownPosition =
                target.position;

            m_LastVisualContactTime =
                Time.time;

            m_HasLastKnownPosition =
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
                    $"contact. Last known position: " +
                    $"{m_LastKnownPosition}.",
                    this
                );
            }

            m_VisibleTarget =
                null;
        }

        private void ForgetLastKnownPosition()
        {
            if (m_LogPerception)
            {
                Debug.Log(
                    $"[{nameof(EnemyPerception)}] '{name}' forgot its " +
                    $"last known target position.",
                    this
                );
            }

            m_HasLastKnownPosition =
                false;
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
        }

        #endregion
    }
}