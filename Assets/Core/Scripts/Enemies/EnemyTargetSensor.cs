using System;
using UnityEngine;

namespace TheBureau.Gameplay.Enemies
{
    public abstract class EnemyTargetSensor : MonoBehaviour
    {
        #region Inspector

        [Header("Sensor")]

        [SerializeField]
        [Tooltip("How often this sensor evaluates potential targets.")]
        [Min(0.05f)]
        private float m_ScanInterval = 0.2f;

        #endregion

        #region Private Fields

        private Transform m_CurrentTarget;
        private float m_NextScanTime;

        #endregion

        #region Events
        public event Action<Transform, Transform> TargetChanged;

        #endregion

        #region Properties

        public Transform CurrentTarget =>
            IsValidTarget(m_CurrentTarget)
                ? m_CurrentTarget
                : null;
        public bool HasTarget => CurrentTarget != null;

        #endregion

        #region Public Methods

        public void TickSensor()
        {
            if (Time.time < m_NextScanTime)
            {
                return;
            }

            m_NextScanTime =
                Time.time + m_ScanInterval;

            Transform target =
                FindBestTarget();

            SetCurrentTarget(target);
        }

        public void ClearTarget()
        {
            SetCurrentTarget(null);
        }

        #endregion

        #region Protected Methods

        /// <returns>
        /// The perceived target, or null when no target is perceived
        /// </returns>
        protected abstract Transform FindBestTarget();

        /// <param name="target">Target to validate.</param>
        /// <returns>True when the target may still be used.</returns>
        protected virtual bool IsValidTarget(Transform target)
        {
            return target != null &&
                   target.gameObject.activeInHierarchy;
        }

        #endregion

        #region Private Methods

        private void SetCurrentTarget(
            Transform newTarget)
        {
            if (m_CurrentTarget == newTarget)
            {
                return;
            }

            Transform previousTarget =
                m_CurrentTarget;

            m_CurrentTarget =
                newTarget;

            TargetChanged?.Invoke(
                previousTarget,
                m_CurrentTarget
            );
        }

        #endregion
    }
}