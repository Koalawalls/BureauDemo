using Unity.Netcode;
using UnityEngine;

namespace TheBureau.Gameplay.Enemies
{
    /// <summary>
    /// Coordinates the network lifecycle and authority of an enemy entity.
    ///
    /// Enemy decision making and navigation are server-authoritative.
    /// Higher-level behavior is handled by EnemyBrain, while movement
    /// is handled by EnemyMotor.
    /// </summary>
    [DisallowMultipleComponent]
    [RequireComponent(typeof(NetworkObject))]
    [RequireComponent(typeof(EnemyMotor))]
    [RequireComponent(typeof(EnemyBrain))]
    public sealed class EnemyController : NetworkBehaviour
    {
        #region Inspector

        [Header("Debug")]

        [SerializeField]
        [Tooltip("Logs network spawning and authority information.")]
        private bool m_LogNetworkState;

        #endregion

        #region Private Fields

        private EnemyMotor m_Motor;
        private EnemyBrain m_Brain;

        #endregion

        #region Properties

        /// <summary>
        /// Gets the navigation motor belonging to this enemy.
        /// </summary>
        public EnemyMotor Motor => m_Motor;

        /// <summary>
        /// Gets the behavior brain belonging to this enemy.
        /// </summary>
        public EnemyBrain Brain => m_Brain;

        /// <summary>
        /// Gets whether this instance has authority to run enemy gameplay.
        /// </summary>
        public bool HasAuthority => IsSpawned && IsServer;

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
    ConfigureAuthority();

    NetworkObject networkObject = NetworkObject;

    Debug.Log(
        $"[{nameof(EnemyController)}] '{name}' network spawned.\n" +
        $"IsServer: {IsServer}\n" +
        $"IsClient: {IsClient}\n" +
        $"IsHost: {IsHost}\n" +
        $"IsOwner: {IsOwner}\n" +
        $"OwnerClientId: {OwnerClientId}\n" +
        $"NetworkObjectId: {networkObject.NetworkObjectId}\n" +
        $"Position: {transform.position}",
        this
    );
}

        /// <inheritdoc />
        public override void OnNetworkDespawn()
        {
            if (m_Motor != null)
            {
                m_Motor.Stop();
                m_Motor.SetNavigationEnabled(false);
            }

            base.OnNetworkDespawn();
        }

        #endregion

        #region Private Methods

        private void CacheComponents()
        {
            if (m_Motor == null)
            {
                m_Motor = GetComponent<EnemyMotor>();
            }

            if (m_Brain == null)
            {
                m_Brain = GetComponent<EnemyBrain>();
            }
        }

        private void ConfigureAuthority()
        {
            if (m_Motor == null)
            {
                Debug.LogError(
                    $"[{nameof(EnemyController)}] Missing " +
                    $"{nameof(EnemyMotor)} on '{name}'.",
                    this
                );

                return;
            }

            if (m_Brain == null)
            {
                Debug.LogError(
                    $"[{nameof(EnemyController)}] Missing " +
                    $"{nameof(EnemyBrain)} on '{name}'.",
                    this
                );

                return;
            }

            /*
             * Only the server runs NavMesh navigation.
             *
             * EnemyBrain also checks IsServer before performing any
             * decision-making, so remote clients never run AI logic.
             *
             * Remote clients receive the resulting movement through
             * NetworkTransform.
             */
            m_Motor.SetNavigationEnabled(IsServer);
        }

        #endregion
    }
}