using Blocks.Gameplay.Core;
using Unity.Netcode;
using UnityEngine;

namespace TheBureau.Gameplay.Enemies
{
    /// <summary>
    /// Processes authoritative hits received by an enemy and applies
    /// damage through the shared CoreStatsHandler.
    /// Generic hit presentation is delegated to HitFeedbackController.
    /// </summary>
    [DisallowMultipleComponent]
    [RequireComponent(typeof(CoreStatsHandler))]
    public sealed class EnemyHitProcessor : HitProcessor
    {
        #region Inspector

        [Header("Component Dependencies")]

        [SerializeField]
        [Tooltip("Stats handler containing this enemy's runtime stats.")]
        private CoreStatsHandler m_Stats;

        [SerializeField]
        [Tooltip("Shared component responsible for networked hit feedback.")]
        private HitFeedbackController m_HitFeedback;

        [Header("Debug")]

        [SerializeField]
        [Tooltip("Logs incoming hits and resulting health changes.")]
        private bool m_LogHits;

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

            if (!m_LogHits)
            {
                return;
            }

            Debug.Log(
                $"[{nameof(EnemyHitProcessor)}] '{name}' spawned.\n" +
                $"IsServer: {IsServer}\n" +
                $"IsOwner: {IsOwner}\n" +
                $"OwnerClientId: {OwnerClientId}\n" +
                $"Health: {GetCurrentHealth()}",
                this
            );
        }

        #endregion

        #region HitProcessor

        /// <inheritdoc />
        protected override void HandleHit(HitInfo info)
        {
            CacheComponents();

            if (m_Stats == null)
            {
                Debug.LogError(
                    $"[{nameof(EnemyHitProcessor)}] '{name}' has no " +
                    $"{nameof(CoreStatsHandler)}.",
                    this
                );

                return;
            }

            if (!m_Stats.IsAlive)
            {
                return;
            }

            if (info.amount <= 0f)
            {
                Debug.LogWarning(
                    $"[{nameof(EnemyHitProcessor)}] '{name}' received " +
                    $"invalid damage amount: {info.amount}.",
                    this
                );

                return;
            }

            float healthBefore =
                m_Stats.GetCurrentValue(StatKeys.Health);

            m_Stats.ModifyStat(
                StatKeys.Health,
                -info.amount,
                info.attackerId,
                ModificationSource.Damage
            );

            float healthAfter =
                m_Stats.GetCurrentValue(StatKeys.Health);

            float actualDamage =
                Mathf.Max(
                    0f,
                    healthBefore - healthAfter
                );

            if (actualDamage > 0f)
            {
                m_HitFeedback?.BroadcastHitFeedback(
                    info.attackerId,
                    actualDamage,
                    info.hitPoint
                );
            }

            if (m_LogHits)
            {
                Debug.Log(
                    $"[{nameof(EnemyHitProcessor)}] '{name}' received hit.\n" +
                    $"Requested Damage: {info.amount}\n" +
                    $"Actual Damage: {actualDamage}\n" +
                    $"Attacker: {info.attackerId}\n" +
                    $"Hit Point: {info.hitPoint}\n" +
                    $"IsServer: {IsServer}\n" +
                    $"IsOwner: {IsOwner}\n" +
                    $"Health: {healthBefore} -> {healthAfter}",
                    this
                );
            }
        }

        #endregion

        #region Debug

        /// <summary>
        /// Applies ten damage directly to this enemy's health on the server.
        /// </summary>
        [ContextMenu("Debug Apply 10 Damage")]
        private void DebugApplyDamage()
        {
            if (!Application.isPlaying)
            {
                return;
            }

            NetworkManager networkManager =
                NetworkManager.Singleton;

            if (networkManager == null ||
                !networkManager.IsServer)
            {
                Debug.LogWarning(
                    $"[{nameof(EnemyHitProcessor)}] Debug damage must " +
                    $"be executed on the server.",
                    this
                );

                return;
            }

            CacheComponents();

            if (m_Stats == null ||
                !m_Stats.IsAlive)
            {
                return;
            }

            float healthBefore =
                m_Stats.GetCurrentValue(
                    StatKeys.Health
                );

            m_Stats.ModifyStat(
                StatKeys.Health,
                -10f,
                ulong.MaxValue,
                ModificationSource.Damage
            );

            float healthAfter =
                m_Stats.GetCurrentValue(
                    StatKeys.Health
                );

            Debug.Log(
                $"[{nameof(EnemyHitProcessor)}] Direct stat test: " +
                $"{healthBefore} -> {healthAfter}. " +
                $"IsOwner: {IsOwner}",
                this
            );
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

            if (m_HitFeedback == null)
            {
                m_HitFeedback =
                    GetComponent<HitFeedbackController>();
            }
        }

        private float GetCurrentHealth()
        {
            if (m_Stats == null)
            {
                return 0f;
            }

            return m_Stats.GetCurrentValue(
                StatKeys.Health
            );
        }

        #endregion
    }
}