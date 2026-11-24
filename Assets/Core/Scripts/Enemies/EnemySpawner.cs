using Unity.Netcode;
using UnityEngine;

namespace TheBureau.Gameplay.Enemies
{
    /// <summary>
    /// Spawns a networked enemy when the authoritative server starts.
    /// </summary>
    [DisallowMultipleComponent]
    public sealed class EnemySpawner : MonoBehaviour
    {
        #region Inspector

        [Header("Spawn")]

        [SerializeField]
        [Tooltip("Network enemy prefab to spawn.")]
        private NetworkObject m_EnemyPrefab;

        [SerializeField]
        [Tooltip(
            "Optional spawn point. Defaults to this object's transform."
        )]
        private Transform m_SpawnPoint;

        [SerializeField]
        [Tooltip("Automatically spawn the configured enemy when the server starts.")]
        private bool m_SpawnOnServerStart = true;

        [Header("Debug")]

        [SerializeField]
        [Tooltip("Logs enemy spawning information.")]
        private bool m_LogSpawning = true;

        #endregion

        #region Private Fields

        private NetworkManager m_NetworkManager;
        private bool m_HasSpawned;

        #endregion

        #region Unity Lifecycle

        private void Start()
        {
            HookNetworkManager();
        }

        private void OnDestroy()
        {
            UnhookNetworkManager();
        }

        #endregion

        #region Network Lifecycle

        private void HookNetworkManager()
        {
            m_NetworkManager =
                NetworkManager.Singleton;

            if (m_NetworkManager == null)
            {
                Debug.LogError(
                    $"[{nameof(EnemySpawner)}] No " +
                    $"{nameof(NetworkManager)} exists in the scene.",
                    this
                );

                return;
            }

            m_NetworkManager.OnServerStarted +=
                HandleServerStarted;

            /*
             * Handles cases where this component becomes active
             * after the server has already started.
             */
            if (m_NetworkManager.IsListening &&
                m_NetworkManager.IsServer)
            {
                HandleServerStarted();
            }
        }

        private void UnhookNetworkManager()
        {
            if (m_NetworkManager == null)
            {
                return;
            }

            m_NetworkManager.OnServerStarted -=
                HandleServerStarted;
        }

        private void HandleServerStarted()
        {
            if (!m_SpawnOnServerStart ||
                m_HasSpawned)
            {
                return;
            }

            SpawnEnemy();
        }

        #endregion

        #region Public Methods

        /// <summary>
        /// Spawns the configured enemy on the authoritative server.
        /// </summary>
        [ContextMenu("Spawn Enemy")]
        public void SpawnEnemy()
        {
            if (m_NetworkManager == null)
            {
                m_NetworkManager =
                    NetworkManager.Singleton;
            }

            if (m_NetworkManager == null ||
                !m_NetworkManager.IsListening ||
                !m_NetworkManager.IsServer)
            {
                Debug.LogWarning(
                    $"[{nameof(EnemySpawner)}] Enemy spawning requires " +
                    $"an active server or host.",
                    this
                );

                return;
            }

            if (m_EnemyPrefab == null)
            {
                Debug.LogError(
                    $"[{nameof(EnemySpawner)}] No enemy prefab assigned.",
                    this
                );

                return;
            }

            Vector3 spawnPosition =
                m_SpawnPoint != null
                    ? m_SpawnPoint.position
                    : transform.position;

            Quaternion spawnRotation =
                m_SpawnPoint != null
                    ? m_SpawnPoint.rotation
                    : transform.rotation;

            NetworkObject enemy =
                Instantiate(
                    m_EnemyPrefab,
                    spawnPosition,
                    spawnRotation
                );

            enemy.Spawn();

            m_HasSpawned = true;

            if (m_LogSpawning)
            {
                Debug.Log(
                    $"[{nameof(EnemySpawner)}] Spawned " +
                    $"'{enemy.name}' with NetworkObjectId " +
                    $"{enemy.NetworkObjectId}.",
                    enemy
                );
            }
        }

        #endregion
    }
}