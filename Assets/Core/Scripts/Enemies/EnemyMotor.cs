using UnityEngine;
using UnityEngine.AI;

namespace TheBureau.Gameplay.Enemies
{
    /// <summary>
    /// Handles NavMesh-based navigation and movement for an enemy.
    /// This component does not decide where the enemy should move.
    /// Higher-level systems provide destinations and desired stopping
    /// distances to the motor.
    /// </summary>
    [DisallowMultipleComponent]
    [RequireComponent(typeof(NavMeshAgent))]
    public sealed class EnemyMotor : MonoBehaviour
    {
        #region Inspector

        [Header("Debug")]

        [SerializeField]
        [Tooltip("Logs navigation requests and failures.")]
        private bool m_LogNavigation;

        #endregion

        #region Private Fields

        private NavMeshAgent m_NavMeshAgent;

        #endregion

        #region Properties

        /// <summary>
        /// Gets whether the enemy currently has a navigation path.
        /// </summary>
        public bool HasPath =>
            m_NavMeshAgent != null &&
            m_NavMeshAgent.enabled &&
            m_NavMeshAgent.hasPath;

        /// <summary>
        /// Gets whether the NavMeshAgent is currently calculating a path.
        /// </summary>
        public bool IsPathPending =>
            m_NavMeshAgent != null &&
            m_NavMeshAgent.enabled &&
            m_NavMeshAgent.pathPending;

        /// <summary>
        /// Gets whether navigation is currently stopped.
        /// </summary>
        public bool IsStopped =>
            m_NavMeshAgent == null ||
            !m_NavMeshAgent.enabled ||
            m_NavMeshAgent.isStopped;

        /// <summary>
        /// Gets the remaining distance to the current destination.
        /// </summary>
        public float RemainingDistance =>
            m_NavMeshAgent != null &&
            m_NavMeshAgent.enabled
                ? m_NavMeshAgent.remainingDistance
                : float.PositiveInfinity;

        /// <summary>
        /// Gets the current NavMeshAgent stopping distance.
        /// </summary>
        public float StoppingDistance =>
            m_NavMeshAgent != null
                ? m_NavMeshAgent.stoppingDistance
                : 0f;

        #endregion

        #region Unity Lifecycle

        private void Awake()
        {
            CacheComponents();
        }

        #endregion

        #region Public Methods

        /// <summary>
        /// Enables or disables NavMesh navigation for this enemy.
        /// </summary>
        /// <param name="enabled">
        /// Whether navigation should be enabled.
        /// </param>
        public void SetNavigationEnabled(bool enabled)
        {
            CacheComponents();

            if (m_NavMeshAgent == null)
            {
                return;
            }

            if (m_NavMeshAgent.enabled == enabled)
            {
                return;
            }

            m_NavMeshAgent.enabled = enabled;
        }

        /// <summary>
        /// Requests movement toward a world-space destination.
        /// </summary>
        /// <param name="destination">
        /// World-space destination to navigate toward.
        /// </param>
        /// <param name="stoppingDistance">
        /// Desired distance from the destination at which the agent
        /// should consider navigation complete.
        /// </param>
        /// <returns>
        /// True when the NavMeshAgent accepted the destination request.
        /// </returns>
        public bool TrySetDestination(
            Vector3 destination,
            float stoppingDistance = 0f)
        {
            CacheComponents();

            if (!CanNavigate())
            {
                return false;
            }

            /*
             * Attack and idle states may previously have stopped the
             * agent. Any new movement request should explicitly resume it.
             */
            if (m_NavMeshAgent.isStopped)
            {
                m_NavMeshAgent.isStopped = false;
            }

            /*
             * Different behaviors require different tolerances.
             *
             * Chase may want to get very close to its destination,
             * whereas Investigate may stop slightly short of an exact
             * remembered point.
             */
            m_NavMeshAgent.stoppingDistance =
                Mathf.Max(0f, stoppingDistance);

            bool accepted =
                m_NavMeshAgent.SetDestination(
                    destination
                );

            if (m_LogNavigation)
            {
                Debug.Log(
                    $"[{nameof(EnemyMotor)}] '{name}' navigation request.\n" +
                    $"Destination: {destination}\n" +
                    $"Stopping Distance: {m_NavMeshAgent.stoppingDistance}\n" +
                    $"Accepted: {accepted}\n" +
                    $"Is Stopped: {m_NavMeshAgent.isStopped}",
                    this
                );
            }

            return accepted;
        }

        /// <summary>
        /// Stops the enemy and clears its current navigation path.
        /// </summary>
        public void Stop()
        {
            CacheComponents();

            if (m_NavMeshAgent == null ||
                !m_NavMeshAgent.enabled ||
                !m_NavMeshAgent.isOnNavMesh)
            {
                return;
            }

            m_NavMeshAgent.isStopped = true;

            if (m_NavMeshAgent.hasPath)
            {
                m_NavMeshAgent.ResetPath();
            }
        }

        #endregion

        #region Debug

        /// <summary>
        /// Prints the current navigation state to the Console.
        /// </summary>
        [ContextMenu("Debug Navigation State")]
        private void DebugNavigationState()
        {
            CacheComponents();

            if (m_NavMeshAgent == null)
            {
                Debug.LogError(
                    $"[{nameof(EnemyMotor)}] No " +
                    $"{nameof(NavMeshAgent)} found.",
                    this
                );

                return;
            }

            Debug.Log(
                $"[{nameof(EnemyMotor)}] Navigation State\n" +
                $"Name: {name}\n" +
                $"Position: {transform.position}\n" +
                $"Enabled: {m_NavMeshAgent.enabled}\n" +
                $"Is On NavMesh: {m_NavMeshAgent.isOnNavMesh}\n" +
                $"Is Stopped: {m_NavMeshAgent.isStopped}\n" +
                $"Update Position: {m_NavMeshAgent.updatePosition}\n" +
                $"Update Rotation: {m_NavMeshAgent.updateRotation}\n" +
                $"Speed: {m_NavMeshAgent.speed}\n" +
                $"Acceleration: {m_NavMeshAgent.acceleration}\n" +
                $"Stopping Distance: {m_NavMeshAgent.stoppingDistance}\n" +
                $"Velocity: {m_NavMeshAgent.velocity}\n" +
                $"Desired Velocity: {m_NavMeshAgent.desiredVelocity}\n" +
                $"Has Path: {m_NavMeshAgent.hasPath}\n" +
                $"Path Pending: {m_NavMeshAgent.pathPending}\n" +
                $"Path Status: {m_NavMeshAgent.pathStatus}\n" +
                $"Remaining Distance: {m_NavMeshAgent.remainingDistance}\n" +
                $"Destination: {m_NavMeshAgent.destination}",
                this
            );
        }

        #endregion

        #region Private Methods

        private void CacheComponents()
        {
            if (m_NavMeshAgent == null)
            {
                m_NavMeshAgent =
                    GetComponent<NavMeshAgent>();
            }
        }

        private bool CanNavigate()
        {
            if (m_NavMeshAgent == null)
            {
                Debug.LogError(
                    $"[{nameof(EnemyMotor)}] Missing " +
                    $"{nameof(NavMeshAgent)} on '{name}'.",
                    this
                );

                return false;
            }

            if (!m_NavMeshAgent.enabled)
            {
                if (m_LogNavigation)
                {
                    Debug.LogWarning(
                        $"[{nameof(EnemyMotor)}] Cannot move '{name}' " +
                        $"because its {nameof(NavMeshAgent)} is disabled.",
                        this
                    );
                }

                return false;
            }

            if (!m_NavMeshAgent.isOnNavMesh)
            {
                Debug.LogWarning(
                    $"[{nameof(EnemyMotor)}] Cannot move '{name}' because " +
                    $"it is not currently on a NavMesh. " +
                    $"Position: {transform.position}",
                    this
                );

                return false;
            }

            return true;
        }

        #endregion
    }
}