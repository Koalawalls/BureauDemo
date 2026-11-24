using System.Collections.Generic;
using Unity.Netcode;
using UnityEngine;

namespace TheBureau.Gameplay.Enemies
{
    [DisallowMultipleComponent]
    public sealed class NetworkPlayerTargetProvider
        : EnemyTargetProvider
    {
        #region Public Methods

        /// <inheritdoc />
        public override void GetTargets(
            List<Transform> results)
        {
            if (results == null)
            {
                return;
            }

            results.Clear();

            NetworkManager networkManager =
                NetworkManager.Singleton;

            if (networkManager == null ||
                !networkManager.IsListening ||
                !networkManager.IsServer)
            {
                return;
            }

            foreach (
                KeyValuePair<ulong, NetworkClient> pair
                in networkManager.ConnectedClients)
            {
                NetworkObject playerObject =
                    pair.Value.PlayerObject;

                if (playerObject == null ||
                    !playerObject.IsSpawned)
                {
                    continue;
                }

                results.Add(
                    playerObject.transform
                );
            }
        }

        #endregion
    }
}