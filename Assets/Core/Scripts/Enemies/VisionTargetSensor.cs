using System.Collections.Generic;
using UnityEngine;

namespace TheBureau.Gameplay.Enemies
{
    [DisallowMultipleComponent]
    [RequireComponent(typeof(EnemyTargetProvider))]
    public sealed class VisionTargetSensor
        : EnemyTargetSensor
    {
        #region Inspector

        [Header("Dependencies")]

        [SerializeField]
        [Tooltip(
            "Provider supplying potential targets to evaluate. " +
            "Defaults to a provider on this GameObject."
        )]
        private EnemyTargetProvider m_TargetProvider;

        [Header("Vision")]

        [SerializeField]
        [Tooltip(
            "Optional transform representing the enemy's eyes. " +
            "When empty, the enemy transform plus Eye Height is used."
        )]
        private Transform m_SensorOrigin;

        [SerializeField]
        [Tooltip(
            "Fallback vertical eye offset when no Sensor Origin is assigned."
        )]
        [Min(0f)]
        private float m_EyeHeight = 1.5f;

        [SerializeField]
        [Tooltip("Maximum distance at which a target may be seen.")]
        [Min(0.1f)]
        private float m_VisionDistance = 20f;

        [SerializeField]
        [Tooltip("Horizontal field of view in degrees.")]
        [Range(1f, 360f)]
        private float m_FieldOfView = 110f;

        [SerializeField]
        [Tooltip(
            "Vertical offset applied to runtime player positions when " +
            "performing visibility checks."
        )]
        private float m_TargetHeightOffset = 1f;

        [SerializeField]
        [Tooltip(
            "Layers containing environment geometry that blocks vision."
        )]
        private LayerMask m_OcclusionLayers;

        [Header("Debug")]

        [SerializeField]
        [Tooltip("Draws vision distance and FOV guides in the Scene view.")]
        private bool m_DrawVision = true;

        [SerializeField]
        [Tooltip("Logs target acquisition information.")]
        private bool m_LogDetection;

        #endregion

        #region Private Fields

        private readonly List<Transform> m_Candidates =
            new(4);

        #endregion

        #region Unity Lifecycle

        private void Awake()
        {
            CacheDependencies();
        }

        #endregion

        #region EnemyTargetSensor

        /// <inheritdoc />
        protected override Transform FindBestTarget()
        {
            CacheDependencies();

            if (m_TargetProvider == null)
            {
                return null;
            }

            m_TargetProvider.GetTargets(
                m_Candidates
            );

            Vector3 origin =
                GetSensorPosition();

            Vector3 forward =
                GetSensorForward();

            Transform bestTarget = null;

            float bestDistanceSquared =
                float.PositiveInfinity;

            float visionDistanceSquared =
                m_VisionDistance *
                m_VisionDistance;

            for (int i = 0;
                 i < m_Candidates.Count;
                 i++)
            {
                Transform candidate =
                    m_Candidates[i];

                if (!IsValidTarget(candidate))
                {
                    continue;
                }

                Vector3 targetPosition =
                    GetTargetPosition(candidate);

                Vector3 toTarget =
                    targetPosition - origin;

                float distanceSquared =
                    toTarget.sqrMagnitude;

                if (distanceSquared >
                    visionDistanceSquared)
                {
                    continue;
                }

                if (!IsWithinFieldOfView(
                        forward,
                        toTarget))
                {
                    continue;
                }

                if (!HasLineOfSight(
                        origin,
                        targetPosition))
                {
                    continue;
                }

                if (distanceSquared >=
                    bestDistanceSquared)
                {
                    continue;
                }

                bestTarget =
                    candidate;

                bestDistanceSquared =
                    distanceSquared;
            }

            if (m_LogDetection &&
                bestTarget != null)
            {
                Debug.Log(
                    $"[{nameof(VisionTargetSensor)}] '{name}' can see " +
                    $"'{bestTarget.name}'.",
                    this
                );
            }

            return bestTarget;
        }

        #endregion

        #region Vision

        private bool IsWithinFieldOfView(
            Vector3 forward,
            Vector3 toTarget)
        {
            Vector3 planarForward =
                Vector3.ProjectOnPlane(
                    forward,
                    Vector3.up
                );

            Vector3 planarDirection =
                Vector3.ProjectOnPlane(
                    toTarget,
                    Vector3.up
                );

            if (planarForward.sqrMagnitude <=
                    Mathf.Epsilon ||
                planarDirection.sqrMagnitude <=
                    Mathf.Epsilon)
            {
                return true;
            }

            float angle =
                Vector3.Angle(
                    planarForward,
                    planarDirection
                );

            return angle <=
                   m_FieldOfView * 0.5f;
        }

        private bool HasLineOfSight(
            Vector3 origin,
            Vector3 targetPosition)
        {
            return !Physics.Linecast(
                origin,
                targetPosition,
                m_OcclusionLayers,
                QueryTriggerInteraction.Ignore
            );
        }

        #endregion

        #region Private Methods

        private void CacheDependencies()
        {
            if (m_TargetProvider == null)
            {
                m_TargetProvider =
                    GetComponent<EnemyTargetProvider>();
            }
        }

        private Vector3 GetSensorPosition()
        {
            if (m_SensorOrigin != null)
            {
                return m_SensorOrigin.position;
            }

            return transform.position +
                   Vector3.up * m_EyeHeight;
        }

        private Vector3 GetSensorForward()
        {
            return m_SensorOrigin != null
                ? m_SensorOrigin.forward
                : transform.forward;
        }

        private Vector3 GetTargetPosition(
            Transform target)
        {
            return target.position +
                   Vector3.up *
                   m_TargetHeightOffset;
        }

        #endregion

        #region Debug

        private void OnDrawGizmosSelected()
        {
            if (!m_DrawVision)
            {
                return;
            }

            Vector3 origin =
                GetSensorPosition();

            Vector3 forward =
                GetSensorForward();

            Vector3 planarForward =
                Vector3.ProjectOnPlane(
                    forward,
                    Vector3.up
                ).normalized;

            Gizmos.DrawWireSphere(
                origin,
                m_VisionDistance
            );

            Vector3 leftBoundary =
                Quaternion.AngleAxis(
                    -m_FieldOfView * 0.5f,
                    Vector3.up
                ) *
                planarForward;

            Vector3 rightBoundary =
                Quaternion.AngleAxis(
                    m_FieldOfView * 0.5f,
                    Vector3.up
                ) *
                planarForward;

            Gizmos.DrawRay(
                origin,
                leftBoundary *
                m_VisionDistance
            );

            Gizmos.DrawRay(
                origin,
                rightBoundary *
                m_VisionDistance
            );
        }

        #endregion
    }
}