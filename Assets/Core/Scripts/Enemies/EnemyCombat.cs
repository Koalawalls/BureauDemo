using System;
using Blocks.Gameplay.Core;
using Unity.Netcode;
using UnityEngine;

namespace TheBureau.Gameplay.Enemies
{
    /// <summary>
    /// Handles attack validation, timing, and hit submission for an enemy.
    ///
    /// EnemyBrain decides which target should be attacked.
    /// EnemyCombat determines whether the attack is valid and submits
    /// damage through the shared IHittable / HitProcessor pipeline.
    /// </summary>
    [DisallowMultipleComponent]
    public sealed class EnemyCombat : MonoBehaviour
    {
        #region Constants

        /*
         * HitInfo currently identifies attackers using a client ID.
         *
         * Server-owned NPCs cannot use OwnerClientId because the server's
         * client ID is normally 0, which would conflict with the host player.
         *
         * Until HitInfo supports a more general damage-source identity,
         * ulong.MaxValue represents a non-player/NPC attacker.
         */
        private const ulong k_NpcAttackerId = ulong.MaxValue;

        #endregion

        #region Inspector

        [Header("Attack")]

        [SerializeField]
        [Tooltip("Maximum distance from a target at which this enemy may attack.")]
        [Min(0.1f)]
        private float m_AttackRange = 2f;

        [SerializeField]
        [Tooltip("Damage dealt by each successful attack.")]
        [Min(0f)]
        private float m_AttackDamage = 15f;

        [SerializeField]
        [Tooltip("Minimum time in seconds between attacks.")]
        [Min(0.05f)]
        private float m_AttackCooldown = 1f;

        [SerializeField]
        [Tooltip(
            "Vertical offset from the target root used as the melee impact point."
        )]
        private float m_HitHeightOffset = 1f;

        [Header("Debug")]

        [SerializeField]
        [Tooltip("Logs successful enemy attacks.")]
        private bool m_LogAttacks = true;

        #endregion

        #region Private Fields

        private float m_NextAttackTime;

        #endregion

        #region Events

        /// <summary>
        /// Raised whenever this enemy successfully performs an attack.
        /// </summary>
        public event Action<Transform> AttackPerformed;

        #endregion

        #region Properties

        /// <summary>
        /// Gets the maximum attack range.
        /// </summary>
        public float AttackRange => m_AttackRange;

        /// <summary>
        /// Gets the damage dealt by this attack.
        /// </summary>
        public float AttackDamage => m_AttackDamage;

        /// <summary>
        /// Gets whether the attack cooldown has elapsed.
        /// </summary>
        public bool IsAttackReady =>
            Time.time >= m_NextAttackTime;

        #endregion

        #region Public Methods

        /// <summary>
        /// Determines whether the supplied target is currently within
        /// attack range.
        /// </summary>
        /// <param name="target">Target to evaluate.</param>
        /// <returns>
        /// True when the target exists and is within attack range.
        /// </returns>
        public bool IsTargetInRange(Transform target)
        {
            if (target == null)
            {
                return false;
            }

            Vector3 difference =
                target.position - transform.position;

            float attackRangeSquared =
                m_AttackRange * m_AttackRange;

            return difference.sqrMagnitude <=
                   attackRangeSquared;
        }

        /// <summary>
        /// Attempts to perform an authoritative attack against the
        /// supplied target.
        /// </summary>
        /// <param name="target">Target to attack.</param>
        /// <returns>
        /// True when the attack was performed and submitted to the
        /// target's hit processor.
        /// </returns>
        public bool TryAttack(Transform target)
        {
            if (target == null)
            {
                return false;
            }

            /*
             * EnemyBrain should already execute only on the server,
             * but retaining this check makes EnemyCombat safe if another
             * system attempts to invoke it later.
             */
            NetworkManager networkManager =
                NetworkManager.Singleton;

            if (networkManager == null ||
                !networkManager.IsListening ||
                !networkManager.IsServer)
            {
                return false;
            }

            if (!IsTargetInRange(target))
            {
                return false;
            }

            if (!IsAttackReady)
            {
                return false;
            }

            /*
             * Consume the attack cooldown here even if the target turns
             * out not to contain a HitProcessor. This prevents a broken
             * target configuration from causing an attack attempt every frame.
             */
            m_NextAttackTime =
                Time.time + m_AttackCooldown;

            if (!TryGetHitProcessor(
                    target,
                    out HitProcessor hitProcessor))
            {
                Debug.LogWarning(
                    $"[{nameof(EnemyCombat)}] '{name}' attempted to attack " +
                    $"'{target.name}', but no {nameof(HitProcessor)} " +
                    $"could be found on its hierarchy.",
                    target
                );

                return false;
            }

            Vector3 hitPoint =
                target.position +
                Vector3.up * m_HitHeightOffset;

            HitInfo hitInfo = new HitInfo
            {
                amount = m_AttackDamage,
                attackerId = k_NpcAttackerId,
                hitPoint = hitPoint
            };

            /*
             * We are already executing on the server.
             *
             * HitProcessor.OnHit() will therefore enter its authoritative
             * HandleHit() path without requiring the enemy to know how
             * player health is implemented.
             */
            hitProcessor.OnHit(hitInfo);

            if (m_LogAttacks)
            {
                Debug.Log(
                    $"[{nameof(EnemyCombat)}] '{name}' attacked " +
                    $"'{target.name}' for {m_AttackDamage} damage.",
                    this
                );
            }

            AttackPerformed?.Invoke(target);

            return true;
        }

        #endregion

        #region Private Methods

        private bool TryGetHitProcessor(
            Transform target,
            out HitProcessor hitProcessor)
        {
            hitProcessor = null;

            if (target == null)
            {
                return false;
            }

            // Most likely location when the provider returns PlayerObject.
            hitProcessor =
                target.GetComponent<HitProcessor>();

            if (hitProcessor != null)
            {
                return true;
            }

            // Covers player architectures where gameplay components
            // live underneath the network player root.
            hitProcessor =
                target.GetComponentInChildren<HitProcessor>(true);

            if (hitProcessor != null)
            {
                return true;
            }

            // Defensive fallback for targets represented by a child transform.
            hitProcessor =
                target.GetComponentInParent<HitProcessor>();

            return hitProcessor != null;
        }

        #endregion
    }
}