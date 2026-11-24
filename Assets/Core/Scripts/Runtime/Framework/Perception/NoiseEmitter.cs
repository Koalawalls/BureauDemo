using Unity.Netcode;
using UnityEngine;

namespace TheBureau.Gameplay.Perception
{
    /// <summary>
    /// Provides a reusable, network-aware source for gameplay noise.
    ///
    /// Noise characteristics are defined by a reusable
    /// <see cref="NoiseProfile"/>.
    ///
    /// Client-owned gameplay objects may request emission, but the
    /// authoritative server performs the actual NoiseSystem dispatch.
    ///
    /// This component represents AI-perceivable gameplay noise only.
    /// It does not play presentation audio.
    /// </summary>
    [DisallowMultipleComponent]
    public sealed class NoiseEmitter : NetworkBehaviour
    {
        #region Inspector

        [Header("Noise")]

        [SerializeField]
        [Tooltip(
            "Reusable profile defining this noise's category, priority, " +
            "and base hearing radius."
        )]
        private NoiseProfile m_Profile;

        [SerializeField]
        [Tooltip(
            "Optional transform used as the world-space origin of the noise. " +
            "If omitted, this GameObject's transform is used."
        )]
        private Transform m_EmissionOrigin;

        [Header("Debug")]

        [SerializeField]
        [Tooltip("Logs authoritative gameplay noise emission.")]
        private bool m_LogEmission;

        #endregion

        #region Properties

        /// <summary>
        /// Gets the configured noise profile.
        /// </summary>
        public NoiseProfile Profile =>
            m_Profile;

        /// <summary>
        /// Gets the world-space position from which noise is emitted.
        /// </summary>
        public Vector3 EmissionPosition =>
            m_EmissionOrigin != null
                ? m_EmissionOrigin.position
                : transform.position;

        #endregion

        #region Unity Lifecycle

        private void Awake()
        {
            ValidateConfiguration();
        }

        #endregion

        #region Public Methods

        /// <summary>
        /// Requests emission of this emitter's configured gameplay noise.
        ///
        /// Server-owned sources emit immediately. Client-owned sources
        /// forward the request to the authoritative server.
        /// </summary>
        public void RequestEmit()
        {
            if (!CanRequestEmission())
            {
                return;
            }

            /*
             * Host/server execution requires no network round trip.
             */
            if (IsServer)
            {
                EmitAuthoritative();
                return;
            }

            /*
             * Only the owner of a client-owned NetworkObject may request
             * gameplay noise from that object.
             */
            if (!IsOwner)
            {
                if (m_LogEmission)
                {
                    Debug.LogWarning(
                        $"[{nameof(NoiseEmitter)}] '{name}' rejected a " +
                        $"noise request because this client is not its owner.",
                        this
                    );
                }

                return;
            }

            RequestEmitRpc();
        }

        #endregion

        #region RPC

        /// <summary>
        /// Forwards an owner-generated noise request to the server.
        ///
        /// The client does not provide the noise profile or emission
        /// position. The server uses its authoritative copy of both.
        /// </summary>
        [Rpc(SendTo.Server)]
        private void RequestEmitRpc(
            RpcParams rpcParams = default)
        {
            if (rpcParams.Receive.SenderClientId !=
                OwnerClientId)
            {
                if (m_LogEmission)
                {
                    Debug.LogWarning(
                        $"[{nameof(NoiseEmitter)}] '{name}' rejected " +
                        $"noise request from client " +
                        $"{rpcParams.Receive.SenderClientId}. " +
                        $"Expected owner {OwnerClientId}.",
                        this
                    );
                }

                return;
            }

            EmitAuthoritative();
        }

        #endregion

        #region Authoritative Emission

        /// <summary>
        /// Creates and distributes the configured noise stimulus from
        /// the authoritative server.
        /// </summary>
        private void EmitAuthoritative()
        {
            if (!IsServer)
            {
                return;
            }

            if (m_Profile == null)
            {
                Debug.LogError(
                    $"[{nameof(NoiseEmitter)}] '{name}' cannot emit noise " +
                    $"because no {nameof(NoiseProfile)} is assigned.",
                    this
                );

                return;
            }

            if (m_Profile.Radius <= 0f)
            {
                if (m_LogEmission)
                {
                    Debug.LogWarning(
                        $"[{nameof(NoiseEmitter)}] '{name}' has a noise " +
                        $"profile with a radius of {m_Profile.Radius}. " +
                        $"Emission was ignored.",
                        this
                    );
                }

                return;
            }

            bool emitted =
                NoiseSystem.Emit(
                    EmissionPosition,
                    m_Profile.Radius,
                    m_Profile.Priority,
                    m_Profile.Category,
                    gameObject
                );

            if (m_LogEmission)
            {
                Debug.Log(
                    $"[{nameof(NoiseEmitter)}] '{name}' emitted noise.\n" +
                    $"Profile: {m_Profile.name}\n" +
                    $"Category: {m_Profile.Category}\n" +
                    $"Priority: {m_Profile.Priority}\n" +
                    $"Radius: {m_Profile.Radius:F2}\n" +
                    $"Position: {EmissionPosition}\n" +
                    $"Distributed: {emitted}",
                    this
                );
            }
        }

        #endregion

        #region Validation

        private bool CanRequestEmission()
        {
            if (!IsSpawned)
            {
                if (m_LogEmission)
                {
                    Debug.LogWarning(
                        $"[{nameof(NoiseEmitter)}] '{name}' cannot emit " +
                        $"because its NetworkObject is not spawned.",
                        this
                    );
                }

                return false;
            }

            if (m_Profile == null)
            {
                Debug.LogError(
                    $"[{nameof(NoiseEmitter)}] '{name}' cannot emit noise " +
                    $"because no {nameof(NoiseProfile)} is assigned.",
                    this
                );

                return false;
            }

            return true;
        }

        private void ValidateConfiguration()
        {
            if (m_Profile == null)
            {
                Debug.LogWarning(
                    $"[{nameof(NoiseEmitter)}] '{name}' has no " +
                    $"{nameof(NoiseProfile)} assigned.",
                    this
                );
            }
        }

        #endregion
    }
}