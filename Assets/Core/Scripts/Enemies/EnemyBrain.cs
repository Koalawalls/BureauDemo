using Unity.Netcode;
using UnityEngine;

namespace TheBureau.Gameplay.Enemies
{
    /// <summary>
    /// Controls high-level server-authoritative enemy behavior.
    ///
    /// Perception, navigation, and combat execution are delegated to
    /// dedicated modular components.
    /// </summary>
    [DisallowMultipleComponent]
    [RequireComponent(typeof(EnemyPerception))]
    [RequireComponent(typeof(EnemyMotor))]
    [RequireComponent(typeof(EnemyCombat))]
    public sealed class EnemyBrain : NetworkBehaviour
    {
        #region Types

        /// <summary>
        /// Behavioral states currently available to an enemy.
        /// </summary>
        public enum EnemyState
        {
            Idle,
            Investigate,
            Chase,
            Attack,
            Dead
        }

        #endregion

        #region Inspector

        [Header("Dependencies")]

        [SerializeField]
        private EnemyPerception m_Perception;

        [Header("Navigation")]

        [SerializeField]
        [Tooltip("Minimum time between navigation path refreshes.")]
        [Min(0.05f)]
        private float m_RepathInterval = 0.2f;

        [SerializeField]
        [Tooltip(
            "Distance from the last known position at which investigation " +
            "movement is considered complete."
        )]
        [Min(0f)]
        private float m_InvestigationStoppingDistance = 0.75f;

        [Header("Debug")]

        [SerializeField]
        [Tooltip("Logs enemy state transitions.")]
        private bool m_LogStateChanges = true;

        #endregion

        #region Network State

        private readonly NetworkVariable<EnemyState> m_State =
            new(
                EnemyState.Idle,
                NetworkVariableReadPermission.Everyone,
                NetworkVariableWritePermission.Server
            );

        #endregion

        #region Private Fields

        private EnemyMotor m_Motor;
        private EnemyCombat m_Combat;

        private float m_NextRepathTime;

        #endregion

        #region Properties

        /// <summary>
        /// Gets the enemy's current replicated behavior state.
        /// </summary>
        public EnemyState CurrentState =>
            m_State.Value;

        /// <summary>
        /// Gets the target currently visible to this enemy.
        /// </summary>
        public Transform CurrentTarget =>
            m_Perception != null
                ? m_Perception.VisibleTarget
                : null;

        #endregion

        #region Unity Lifecycle

        private void Awake()
        {
            CacheComponents();
        }

        private void Update()
        {
            if (!IsSpawned ||
                !IsServer ||
                m_State.Value == EnemyState.Dead)
            {
                return;
            }

            UpdateBehaviour();
        }

        #endregion

        #region NetworkBehaviour

        /// <inheritdoc />
        public override void OnNetworkSpawn()
        {
            base.OnNetworkSpawn();

            CacheComponents();

            if (!IsServer)
            {
                return;
            }

            if (m_Perception == null)
            {
                Debug.LogError(
                    $"[{nameof(EnemyBrain)}] '{name}' has no " +
                    $"{nameof(EnemyPerception)}.",
                    this
                );
            }
        }

        #endregion

        #region Public Methods

        /// <summary>
        /// Permanently places this enemy into the dead state.
        /// </summary>
        public void EnterDeadState()
        {
            if (!IsServer ||
                m_State.Value == EnemyState.Dead)
            {
                return;
            }

            m_Motor.Stop();

            if (m_Perception != null)
            {
                m_Perception.ForgetEverything();
            }

            if (m_Combat != null)
            {
                m_Combat.enabled = false;
            }

            SetState(
                EnemyState.Dead
            );
        }

        #endregion

        #region Behaviour

        private void UpdateBehaviour()
        {
            if (m_Perception == null)
            {
                EnterIdle();
                return;
            }

            m_Perception.TickPerception();

            /*
             * Direct visual contact takes priority over remembered
             * information.
             */
            if (m_Perception.HasVisualContact)
            {
                Transform target =
                    m_Perception.VisibleTarget;

                if (target != null &&
                    m_Combat.IsTargetInRange(target))
                {
                    UpdateAttack(target);
                    return;
                }

                if (target != null)
                {
                    UpdateChase(target);
                    return;
                }
            }

            /*
             * When sight is lost, investigate the most recently
             * observed position instead of immediately returning idle.
             */
            if (m_Perception.HasRecentMemory)
            {
                UpdateInvestigate(
                    m_Perception.LastKnownPosition
                );

                return;
            }

            EnterIdle();
        }

        private void EnterIdle()
        {
            SetState(
                EnemyState.Idle
            );

            m_Motor.Stop();
        }

        private void UpdateChase(
            Transform target)
        {
            SetState(
                EnemyState.Chase
            );

            TryUpdateDestination(
                target.position,
                0f
            );
        }

        private void UpdateAttack(
            Transform target)
        {
            SetState(
                EnemyState.Attack
            );

            m_Motor.Stop();

            m_Combat.TryAttack(
                target
            );
        }

        private void UpdateInvestigate(
            Vector3 lastKnownPosition)
        {
            SetState(
                EnemyState.Investigate
            );

            Vector3 difference =
                lastKnownPosition -
                transform.position;

            float stoppingDistanceSquared =
                m_InvestigationStoppingDistance *
                m_InvestigationStoppingDistance;

            if (difference.sqrMagnitude <=
                stoppingDistanceSquared)
            {
                m_Motor.Stop();
                return;
            }

            TryUpdateDestination(
                lastKnownPosition,
                m_InvestigationStoppingDistance
            );
        }

        #endregion

        #region Navigation

        private void TryUpdateDestination(
            Vector3 destination,
            float stoppingDistance)
        {
            if (Time.time <
                m_NextRepathTime)
            {
                return;
            }

            m_NextRepathTime =
                Time.time +
                m_RepathInterval;

            m_Motor.TrySetDestination(
                destination,
                stoppingDistance
            );
        }

        #endregion

        #region State

        private void SetState(
            EnemyState newState)
        {
            if (m_State.Value == newState)
            {
                return;
            }

            EnemyState previousState =
                m_State.Value;

            m_State.Value =
                newState;

            if (m_LogStateChanges)
            {
                Debug.Log(
                    $"[{nameof(EnemyBrain)}] '{name}' changed state: " +
                    $"{previousState} -> {newState}",
                    this
                );
            }
        }

        #endregion

        #region Private Methods

        private void CacheComponents()
        {
            if (m_Perception == null)
            {
                m_Perception =
                    GetComponent<EnemyPerception>();
            }

            if (m_Motor == null)
            {
                m_Motor =
                    GetComponent<EnemyMotor>();
            }

            if (m_Combat == null)
            {
                m_Combat =
                    GetComponent<EnemyCombat>();
            }
        }

        #endregion
    }
}