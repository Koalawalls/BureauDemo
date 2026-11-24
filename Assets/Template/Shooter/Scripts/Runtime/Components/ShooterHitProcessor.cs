using UnityEngine;
using Unity.Cinemachine;
using Blocks.Gameplay.Core;

namespace Blocks.Gameplay.Shooter
{
    /// <summary>
    /// Processes hits received by a shooter player character.
    /// Handles player-specific damage rules and reactions while delegating
    /// generic hit presentation to HitFeedbackController.
    /// </summary>
    [DisallowMultipleComponent]
    [RequireComponent(typeof(CoreStatsHandler))]
    public class ShooterHitProcessor : HitProcessor
    {
        #region Inspector

        [Header("Component Dependencies")]

        [SerializeField]
        private CoreStatsHandler coreStats;

        [SerializeField]
        private CorePlayerState corePlayerState;

        [SerializeField]
        private ShooterAnimator shooterAnimator;

        [SerializeField]
        [Tooltip("Shared component responsible for networked hit presentation.")]
        private HitFeedbackController hitFeedback;

        [Header("Damage Modifiers")]

        [SerializeField]
        private float headshotMultiplier = 2.0f;

        [SerializeField]
        private float armorDamageReduction = 0.3f;

        [SerializeField]
        private bool hasArmor;

        #endregion

        #region Private Fields

        private readonly int m_AnimIDPlayerHit =
            Animator.StringToHash("PlayerHit");

        #endregion

        #region Unity Lifecycle

        private void Awake()
        {
            if (coreStats == null)
            {
                coreStats =
                    GetComponent<CoreStatsHandler>();
            }

            if (corePlayerState == null)
            {
                corePlayerState =
                    GetComponent<CorePlayerState>();
            }

            if (shooterAnimator == null)
            {
                shooterAnimator =
                    GetComponentInChildren<ShooterAnimator>();
            }

            if (hitFeedback == null)
            {
                hitFeedback =
                    GetComponent<HitFeedbackController>();
            }
        }

        #endregion

        #region HitProcessor

        /// <inheritdoc />
        protected override void HandleHit(HitInfo info)
        {
            if (info.attackerId ==
                OwnerClientId)
            {
                return;
            }

            if (corePlayerState != null &&
                !corePlayerState.IsActive)
            {
                return;
            }

            if (coreStats == null ||
                !coreStats.IsAlive)
            {
                return;
            }

            float finalDamage =
                CalculateDamage(info);

            coreStats.ModifyStat(
                StatKeys.Health,
                -finalDamage,
                info.attackerId,
                ModificationSource.Damage
            );

            PlayHitAnimation();

            PlayLocalDamageReaction();

            hitFeedback?.BroadcastHitFeedback(
                info.attackerId,
                finalDamage,
                info.hitPoint
            );
        }

        #endregion

        #region Damage

        private float CalculateDamage(
            HitInfo info)
        {
            float damage =
                info.amount;

            if (hasArmor)
            {
                damage *=
                    1f - armorDamageReduction;
            }

            if (info.hitPoint.y >
                transform.position.y + 1.5f)
            {
                damage *=
                    headshotMultiplier;
            }

            return damage;
        }

        #endregion

        #region Presentation

        private void PlayHitAnimation()
        {
            if (shooterAnimator == null ||
                shooterAnimator.Animator == null)
            {
                return;
            }

            shooterAnimator.Animator.ResetTrigger(
                m_AnimIDPlayerHit
            );

            shooterAnimator.Animator.SetTrigger(
                m_AnimIDPlayerHit
            );
        }

        private void PlayLocalDamageReaction()
        {
            CoreDirector
                .RequestCameraShake()
                .WithImpulseDefinition(
                    CinemachineImpulseDefinition
                        .ImpulseShapes.Explosion,
                    CinemachineImpulseDefinition
                        .ImpulseTypes.Dissipating,
                    0.2f
                )
                .WithVelocity(0.02f)
                .Execute();
        }

        #endregion
    }
}