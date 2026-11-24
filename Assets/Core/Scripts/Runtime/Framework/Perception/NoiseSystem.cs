using System.Collections.Generic;
using Unity.Netcode;
using UnityEngine;

namespace TheBureau.Gameplay.Perception
{
    /// <summary>
    /// Central gameplay service responsible for distributing noise stimuli
    /// to registered noise receivers.
    ///
    /// NoiseSystem does not determine whether a receiver actually hears
    /// a stimulus. Distance, sensitivity, occlusion, priority, and other
    /// perception rules belong to the receiving sensor.
    ///
    /// In multiplayer sessions, stimuli are distributed only by the
    /// authoritative server.
    /// </summary>
    public static class NoiseSystem
    {
        #region Private Fields

        private static readonly HashSet<INoiseReceiver> s_Receivers =
            new HashSet<INoiseReceiver>();

        /*
         * Registration can change while a stimulus is being distributed.
         * Queue those changes until dispatch completes so the receiver
         * collection is never modified during enumeration.
         */
        private static readonly HashSet<INoiseReceiver> s_PendingRegistrations =
            new HashSet<INoiseReceiver>();

        private static readonly HashSet<INoiseReceiver> s_PendingUnregistrations =
            new HashSet<INoiseReceiver>();

        /*
         * A depth counter is used instead of a simple bool so nested noise
         * emissions remain safe if a receiver indirectly creates another
         * stimulus while processing the first one.
         */
        private static int s_DispatchDepth;

        #endregion

        #region Properties

        /// <summary>
        /// Gets the number of currently registered noise receivers.
        /// Primarily useful for debugging and diagnostics.
        /// </summary>
        public static int ReceiverCount =>
            s_Receivers.Count;

        #endregion

        #region Runtime Initialization

        /// <summary>
        /// Clears static state whenever Unity initializes the runtime.
        ///
        /// This also protects against stale registrations when Enter Play
        /// Mode Options are configured without domain reload.
        /// </summary>
        [RuntimeInitializeOnLoadMethod(
            RuntimeInitializeLoadType.SubsystemRegistration)]
        private static void ResetRuntimeState()
        {
            s_Receivers.Clear();
            s_PendingRegistrations.Clear();
            s_PendingUnregistrations.Clear();

            s_DispatchDepth = 0;
        }

        #endregion

        #region Registration

        /// <summary>
        /// Registers a receiver to receive future gameplay noise stimuli.
        /// Registering the same receiver more than once has no effect.
        /// </summary>
        /// <param name="receiver">
        /// Receiver to register.
        /// </param>
        public static void Register(
            INoiseReceiver receiver)
        {
            if (!IsReceiverValid(receiver))
            {
                return;
            }

            if (s_DispatchDepth > 0)
            {
                /*
                 * If this receiver was scheduled for removal during the
                 * current dispatch, registration cancels that removal.
                 */
                s_PendingUnregistrations.Remove(receiver);
                s_PendingRegistrations.Add(receiver);

                return;
            }

            s_Receivers.Add(receiver);
        }

        /// <summary>
        /// Removes a receiver from future gameplay noise distribution.
        /// </summary>
        /// <param name="receiver">
        /// Receiver to unregister.
        /// </param>
        public static void Unregister(
            INoiseReceiver receiver)
        {
            if (receiver == null)
            {
                return;
            }

            if (s_DispatchDepth > 0)
            {
                /*
                 * Cancel a registration made during the same dispatch,
                 * then schedule the receiver for removal.
                 */
                s_PendingRegistrations.Remove(receiver);
                s_PendingUnregistrations.Add(receiver);

                return;
            }

            s_Receivers.Remove(receiver);
        }

        #endregion

        #region Emission

        /// <summary>
        /// Creates and emits a gameplay noise stimulus.
        ///
        /// In an active multiplayer session, only the authoritative server
        /// is permitted to distribute gameplay noise.
        /// </summary>
        /// <param name="position">
        /// World-space origin of the noise.
        /// </param>
        /// <param name="radius">
        /// Maximum base hearing radius.
        /// </param>
        /// <param name="priority">
        /// Relative gameplay importance of the noise.
        /// </param>
        /// <param name="category">
        /// Semantic category of the noise.
        /// </param>
        /// <param name="source">
        /// Optional GameObject responsible for producing the noise.
        /// </param>
        /// <returns>
        /// True if the stimulus was distributed; otherwise false.
        /// </returns>
        public static bool Emit(
            Vector3 position,
            float radius,
            NoisePriority priority,
            NoiseCategory category,
            GameObject source = null)
        {
            NoiseStimulus stimulus =
                new NoiseStimulus(
                    position,
                    radius,
                    priority,
                    category,
                    source
                );

            return Emit(stimulus);
        }

        /// <summary>
        /// Emits an existing gameplay noise stimulus to all registered
        /// receivers.
        ///
        /// Distribution does not guarantee that every receiver perceives
        /// the stimulus. Each receiver independently evaluates whether the
        /// noise can actually be heard.
        /// </summary>
        /// <param name="stimulus">
        /// Noise stimulus to distribute.
        /// </param>
        /// <returns>
        /// True if the stimulus was distributed; otherwise false.
        /// </returns>
        public static bool Emit(
            in NoiseStimulus stimulus)
        {
            if (!CanDispatch())
            {
                return false;
            }

            /*
             * A zero-radius stimulus cannot meaningfully be heard and does
             * not need to be distributed.
             */
            if (stimulus.Radius <= 0f)
            {
                return false;
            }

            s_DispatchDepth++;

            try
            {
                foreach (INoiseReceiver receiver in s_Receivers)
                {
                    if (!IsReceiverValid(receiver))
                    {
                        /*
                         * Unity objects may be destroyed without having
                         * unregistered correctly. Clean them up after this
                         * dispatch rather than mutating the collection now.
                         */
                        s_PendingUnregistrations.Add(receiver);
                        continue;
                    }

                    receiver.ReceiveNoise(
                        in stimulus
                    );
                }
            }
            finally
            {
                s_DispatchDepth--;

                if (s_DispatchDepth == 0)
                {
                    ApplyPendingRegistrationChanges();
                }
            }

            return true;
        }

        #endregion

        #region Private Methods

        /// <summary>
        /// Determines whether gameplay noise may currently be distributed.
        /// </summary>
        private static bool CanDispatch()
        {
            NetworkManager networkManager =
                NetworkManager.Singleton;

            /*
             * This project uses authoritative networked gameplay.
             * Clients must never independently drive AI perception.
             */
            if (networkManager == null ||
                !networkManager.IsListening)
            {
                return false;
            }

            return networkManager.IsServer;
        }

        /// <summary>
        /// Determines whether a receiver reference is still usable.
        ///
        /// UnityEngine.Object requires special handling because destroyed
        /// MonoBehaviours can remain as managed interface references while
        /// comparing equal to null through Unity's object lifetime rules.
        /// </summary>
        private static bool IsReceiverValid(
            INoiseReceiver receiver)
        {
            if (receiver == null)
            {
                return false;
            }

            if (receiver is Object unityObject)
            {
                return unityObject != null;
            }

            return true;
        }

        /// <summary>
        /// Applies receiver changes that occurred during noise distribution.
        /// </summary>
        private static void ApplyPendingRegistrationChanges()
        {
            if (s_PendingUnregistrations.Count > 0)
            {
                foreach (INoiseReceiver receiver
                         in s_PendingUnregistrations)
                {
                    s_Receivers.Remove(receiver);
                }

                s_PendingUnregistrations.Clear();
            }

            if (s_PendingRegistrations.Count > 0)
            {
                foreach (INoiseReceiver receiver
                         in s_PendingRegistrations)
                {
                    if (IsReceiverValid(receiver))
                    {
                        s_Receivers.Add(receiver);
                    }
                }

                s_PendingRegistrations.Clear();
            }
        }

        #endregion
    }
}