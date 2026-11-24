using System.Collections;
using Blocks.Gameplay.Core;
using Unity.Netcode;
using UnityEngine;

namespace TheBureau.Gameplay.Enemies
{
    /// <summary>
    /// Handles the authoritative lifecycle of an enemy when its primary
    /// stat is depleted.
    ///
    /// Health and alive state remain owned by CoreStatsHandler.
    /// AI state remains owned by EnemyBrain.
    /// This component coordinates the resulting enemy death lifecycle.
    /// </summary>
    [DisallowMultipleComponent]
    [RequireComponent(typeof(NetworkObject))]
    [RequireComponent(typeof(CoreStatsHandler))]
    [RequireComponent(typeof(EnemyBrain))]
    public sealed class EnemyLifeController : NetworkBehaviour
    {
        #region Inspector

        [Header("Death")]

        [SerializeField]
        [Tooltip(
            "How long the enemy remains spawned after death before " +
            "the server despawns it."
        )]
        [Min(0f)]
        private float m_DespawnDelay = 3f;

        [Header("Debug")]

        [SerializeField]
        [Tooltip("Logs enemy death lifecycle events.")]
        private bool m_LogLifecycle;

        #endregion

        #region Private Fields

        private CoreStatsHandler m_Stats;
        private EnemyBrain m_Brain;
        private EnemyMotor m_Motor;

        private bool m_DeathHandled;

        #endregion

        #region Unity Lifecycle

        private void Awake()
        {
            CacheComponents();
        }

        #endregion

        #region NetworkBehaviour

        /// <inheritdoc />
        public override void OnNetworkSpawn()
        {
            base.OnNetworkSpawn();

            CacheComponents();

            if (m_Stats == null)
            {
                Debug.LogError(
                    $"[{nameof(EnemyLifeController)}] '{name}' has no " +
                    $"{nameof(CoreStatsHandler)}.",
                    this
                );

                return;
            }

            m_Stats.AliveStateChanged +=
                HandleAliveStateChanged;

            /*
             * Defensive check in case this component subscribes after
             * CoreStatsHandler has already evaluated the initial state.
             */
            if (!m_Stats.IsAlive)
            {
                HandleAliveStateChanged(false);
            }
        }

        /// <inheritdoc />
        public override void OnNetworkDespawn()
        {
            if (m_Stats != null)
            {
                m_Stats.AliveStateChanged -=
                    HandleAliveStateChanged;
            }

            base.OnNetworkDespawn();
        }

        #endregion

        #region Death

        private void HandleAliveStateChanged(
            bool isAlive)
        {
            if (isAlive ||
                !IsServer ||
                m_DeathHandled)
            {
                return;
            }

            HandleDeath();
        }

        private void HandleDeath()
        {
            m_DeathHandled = true;

            if (m_LogLifecycle)
            {
                Debug.Log(
                    $"[{nameof(EnemyLifeController)}] '{name}' died.",
                    this
                );
            }

            if (m_Brain != null)
            {
                m_Brain.EnterDeadState();
            }

            if (m_Motor != null)
            {
                m_Motor.Stop();
            }

            StartCoroutine(
                DespawnAfterDelay()
            );
        }

        private IEnumerator DespawnAfterDelay()
        {
            if (m_DespawnDelay > 0f)
            {
                yield return new WaitForSeconds(
                    m_DespawnDelay
                );
            }

            if (!IsServer ||
                NetworkObject == null ||
                !NetworkObject.IsSpawned)
            {
                yield break;
            }

            if (m_LogLifecycle)
            {
                Debug.Log(
                    $"[{nameof(EnemyLifeController)}] Despawning '{name}'.",
                    this
                );
            }

            NetworkObject.Despawn(true);
        }

        #endregion

        #region Private Methods

        private void CacheComponents()
        {
            if (m_Stats == null)
            {
                m_Stats =
                    GetComponent<CoreStatsHandler>();
            }

            if (m_Brain == null)
            {
                m_Brain =
                    GetComponent<EnemyBrain>();
            }

            if (m_Motor == null)
            {
                m_Motor =
                    GetComponent<EnemyMotor>();
            }
        }

        #endregion
    }
}