using UnityEngine;
using Unity.Netcode;
using Unity.Cinemachine;
using Blocks.Gameplay.Core;
using TheBureau.Gameplay.Perception;

namespace Blocks.Gameplay.Shooter
{
    /// <summary>
    /// A modular weapon system that handles firing, reloading, spread,
    /// obstruction detection, and gameplay noise emission.
    ///
    /// Uses a component-based architecture with
    /// <see cref="IFiringMechanism"/> and <see cref="IShootingBehavior"/>
    /// to support different weapon types and firing patterns.
    /// </summary>
    [RequireComponent(typeof(AmmoHandler), typeof(SpreadHandler))]
    [RequireComponent(typeof(WeaponStateManager))]
    public class ModularWeapon : NetworkBehaviour, IWeapon
    {
        #region Fields & Properties

        [Header("Data")]
        [Tooltip(
            "The weapon data ScriptableObject containing weapon configuration."
        )]
        [SerializeField]
        private WeaponData weaponData;

        [Header("References")]
        [Tooltip(
            "The transform representing the muzzle point where projectiles spawn."
        )]
        [SerializeField]
        private Transform muzzle;

        [Tooltip(
            "The point where bullet shell casings are ejected from."
        )]
        [SerializeField]
        private Transform bulletShellEjectPoint;

        [Tooltip(
            "Gameplay noise emitter used when this weapon fires."
        )]
        [SerializeField]
        private NoiseEmitter noiseEmitter;

        [Header("Obstruction Check")]
        [Tooltip(
            "Layer mask used to detect obstructions between chest and muzzle."
        )]
        [SerializeField]
        private LayerMask obstructionMask;

        [Tooltip(
            "Vertical offset from player position to chest height for obstruction checks."
        )]
        [SerializeField]
        private float chestHeightOffset = 1.2f;

        [Header("Broadcasting on")]
        [Tooltip(
            "Event raised when the weapon fires, passing the fire rate as a parameter."
        )]
        [SerializeField]
        private FloatEvent onWeaponFiredEvent;

        private static readonly int k_AnimIDIsColliding =
            Animator.StringToHash("IsColliding");

        private bool m_FireButtonHeld;
        private bool m_WasMuzzleObstructed;
        private bool m_HasCachedObstructionData;

        private float m_CachedObstructionDistance;

        private Vector3 m_CachedObstructionDirection;
        private Vector3 m_LiveFireDirection;

        private Transform m_AimTransform;

        private AmmoHandler m_AmmoHandler;
        private SpreadHandler m_SpreadHandler;
        private WeaponStateManager m_StateManager;

        private IFiringMechanism m_FiringMechanism;
        private IShootingBehavior m_ShootingBehavior;

        private ShooterAddon m_OwnerPlayerManager;

        private ShootingContext m_ShootingContext =
            new ShootingContext();

        /// <summary>
        /// Gets the transform representing the muzzle point of the weapon.
        /// </summary>
        public Transform Muzzle =>
            muzzle;

        #endregion

        #region Unity Methods

        private void Awake()
        {
            m_AmmoHandler =
                GetComponent<AmmoHandler>();

            m_SpreadHandler =
                GetComponent<SpreadHandler>();

            m_StateManager =
                GetComponent<WeaponStateManager>();

            m_FiringMechanism =
                GetComponent<IFiringMechanism>();

            m_ShootingBehavior =
                GetComponent<IShootingBehavior>();

            if (noiseEmitter == null)
            {
                noiseEmitter =
                    GetComponent<NoiseEmitter>();
            }

            if (weaponData != null)
            {
                m_AmmoHandler.Initialize(
                    m_StateManager,
                    this,
                    weaponData.clipSize,
                    weaponData.reloadTime
                );

                m_SpreadHandler.Initialize(
                    weaponData.minSpreadAngle,
                    weaponData.maxSpreadAngle,
                    weaponData.spreadIncreasePerShot,
                    weaponData.spreadRecoveryRate,
                    weaponData.spreadRecoveryDelay
                );
            }
            else
            {
                Debug.LogError(
                    "WeaponData is not assigned in ModularWeapon!",
                    this
                );
            }

            m_StateManager.Initialize(
                m_AmmoHandler,
                m_FiringMechanism
            );

            if (m_FiringMechanism != null)
            {
                m_FiringMechanism.OnShouldFire +=
                    HandleFiringMechanismShot;
            }
        }

        /// <inheritdoc />
        public override void OnNetworkSpawn()
        {
            base.OnNetworkSpawn();

            if (IsOwner)
            {
                var ownerObject =
                    NetworkManager.Singleton
                        .SpawnManager
                        .GetPlayerNetworkObject(
                            OwnerClientId
                        );

                if (ownerObject != null)
                {
                    m_OwnerPlayerManager =
                        ownerObject.GetComponent<ShooterAddon>();

                    if (m_OwnerPlayerManager?.AimController != null)
                    {
                        m_AimTransform =
                            m_OwnerPlayerManager
                                .AimController
                                .AimTransform;
                    }
                }
            }
        }

        /// <inheritdoc />
        public override void OnNetworkDespawn()
        {
            if (m_FiringMechanism != null)
            {
                m_FiringMechanism.OnShouldFire -=
                    HandleFiringMechanismShot;
            }

            base.OnNetworkDespawn();
        }

        private void Update()
        {
            if (!HasAuthority)
            {
                return;
            }

            UpdateMuzzleObstructionState();

            if (m_FireButtonHeld &&
                m_OwnerPlayerManager?.AimController != null)
            {
                Transform muzzleTransform =
                    Muzzle ??
                    m_OwnerPlayerManager
                        .AimController
                        .AimTransform;

                Vector3 aimTargetPosition =
                    m_OwnerPlayerManager
                        .AimController
                        .PreciseAimTargetPosition;

                m_LiveFireDirection =
                    (
                        aimTargetPosition -
                        muzzleTransform.position
                    ).normalized;
            }

            if (m_FiringMechanism != null &&
                m_StateManager.CurrentState ==
                WeaponState.ReadyToFire)
            {
                m_FiringMechanism.UpdateFiring(
                    Time.deltaTime
                );
            }

            if (m_ShootingBehavior != null &&
                m_FireButtonHeld)
            {
                m_ShootingBehavior.UpdateShooting(
                    m_LiveFireDirection,
                    Time.deltaTime
                );
            }
        }

        #endregion

        #region Public Methods

        /// <summary>
        /// Initializes the weapon with the provided data.
        /// </summary>
        /// <param name="data">
        /// The weapon configuration data.
        /// </param>
        public void Initialize(
            WeaponData data)
        {
            if (data == null)
            {
                Debug.LogError(
                    "Attempted to initialize ModularWeapon with null data!",
                    this
                );

                return;
            }

            weaponData =
                data;

            if (m_AmmoHandler != null)
            {
                m_AmmoHandler.Initialize(
                    m_StateManager,
                    this,
                    weaponData.clipSize,
                    weaponData.reloadTime
                );
            }

            if (m_SpreadHandler != null)
            {
                m_SpreadHandler.Initialize(
                    weaponData.minSpreadAngle,
                    weaponData.maxSpreadAngle,
                    weaponData.spreadIncreasePerShot,
                    weaponData.spreadRecoveryRate,
                    weaponData.spreadRecoveryDelay
                );
            }

            m_StateManager.Initialize(
                m_AmmoHandler,
                m_FiringMechanism
            );

            if (m_FiringMechanism != null)
            {
                /*
                 * Unsubscribe first to avoid duplicate event
                 * registration if Initialize is called again.
                 */
                m_FiringMechanism.OnShouldFire -=
                    HandleFiringMechanismShot;

                m_FiringMechanism.OnShouldFire +=
                    HandleFiringMechanismShot;
            }
        }

        /// <summary>
        /// Initiates firing of the weapon.
        /// This should only be called on the owner's instance.
        /// </summary>
        /// <param name="ownerCharacter">
        /// The character firing the weapon.
        /// </param>
        /// <param name="fireOrigin">
        /// The origin point of the fire ray.
        /// </param>
        /// <param name="fireDirection">
        /// The direction to fire in.
        /// </param>
        public void Fire(
            GameObject ownerCharacter,
            Vector3 fireOrigin,
            Vector3 fireDirection)
        {
            if (!IsOwner)
            {
                return;
            }

            m_FireButtonHeld = true;
            m_LiveFireDirection = fireDirection;

            if (!CanFire())
            {
                if (!m_AmmoHandler.HasAmmo() &&
                    m_StateManager.CurrentState ==
                    WeaponState.Empty)
                {
                    TryReload();
                }

                return;
            }

            m_FiringMechanism?.StartFiring();
        }

        /// <summary>
        /// Stops the weapon from firing.
        /// </summary>
        public void StopFiring()
        {
            m_FireButtonHeld = false;

            m_FiringMechanism?.StopFiring();
            m_ShootingBehavior?.StopShooting();
        }

        /// <summary>
        /// Checks if the weapon can currently fire.
        /// </summary>
        /// <returns>
        /// True if the weapon can fire; otherwise false.
        /// </returns>
        public bool CanFire()
        {
            return
                m_StateManager.CanFire() &&
                m_ShootingBehavior != null &&
                m_ShootingBehavior.CanShoot() &&
                !IsMuzzleObstructed();
        }

        /// <summary>
        /// Attempts to reload the weapon.
        /// This should only be called on the owner's instance.
        /// </summary>
        public void TryReload()
        {
            if (!IsOwner)
            {
                return;
            }

            if (m_AmmoHandler.TryReload())
            {
                StopFiring();
                m_SpreadHandler.Reset();
            }
        }

        /// <summary>
        /// Checks if this weapon is currently equipped by the owner.
        /// </summary>
        /// <returns>
        /// True if equipped; otherwise false.
        /// </returns>
        public bool IsEquipped()
        {
            if (m_OwnerPlayerManager == null ||
                m_OwnerPlayerManager.WeaponController == null)
            {
                return false;
            }

            return ReferenceEquals(
                m_OwnerPlayerManager
                    .WeaponController
                    .CurrentWeapon,
                this
            );
        }

        /// <summary>
        /// Gets the weapon data ScriptableObject.
        /// </summary>
        /// <returns>
        /// The weapon data.
        /// </returns>
        public WeaponData GetWeaponData()
        {
            return weaponData;
        }

        /// <summary>
        /// Gets the current weapon state.
        /// </summary>
        /// <returns>
        /// The current weapon state.
        /// </returns>
        public WeaponState GetCurrentState()
        {
            return m_StateManager.CurrentState;
        }

        /// <summary>
        /// Gets the weapon name.
        /// </summary>
        /// <returns>
        /// The weapon name.
        /// </returns>
        public string GetWeaponName()
        {
            return weaponData.weaponName;
        }

        /// <summary>
        /// Gets the current spread angle.
        /// </summary>
        /// <returns>
        /// Current spread angle in degrees.
        /// </returns>
        public float GetCurrentSpreadAngle()
        {
            return m_SpreadHandler.CurrentSpreadAngle;
        }

        /// <summary>
        /// Checks if the weapon needs to be reloaded.
        /// </summary>
        /// <returns>
        /// True if reload is needed; otherwise false.
        /// </returns>
        public bool NeedsReload()
        {
            return m_AmmoHandler.NeedsReload();
        }

        /// <summary>
        /// Gets the current ammo count and maximum capacity.
        /// </summary>
        /// <param name="current">
        /// Current ammunition.
        /// </param>
        /// <param name="max">
        /// Maximum ammunition capacity.
        /// </param>
        public void GetAmmoInfo(
            out int current,
            out int max)
        {
            m_AmmoHandler.GetAmmoInfo(
                out current,
                out max
            );
        }

        /// <summary>
        /// Resets the weapon obstruction state to default values.
        /// Call when switching away from this weapon.
        /// </summary>
        public void ResetObstructionState()
        {
            if (!m_WasMuzzleObstructed)
            {
                return;
            }

            m_WasMuzzleObstructed = false;
            m_HasCachedObstructionData = false;

            m_OwnerPlayerManager?
                .ShooterAnimator?
                .Animator
                .SetBool(
                    k_AnimIDIsColliding,
                    false
                );

            m_OwnerPlayerManager?
                .AimController?
                .SetWeaponObstructed(false);
        }

        /// <summary>
        /// Plays a tracer effect from start to end position.
        /// </summary>
        /// <param name="start">
        /// The start of the tracer.
        /// </param>
        /// <param name="end">
        /// The end of the tracer.
        /// </param>
        public void PlayTracerEffect(
            Vector3 start,
            Vector3 end)
        {
            PlayTracerEffectRpc(
                start,
                end
            );
        }

        /// <summary>
        /// Plays an impact effect at the specified position.
        /// </summary>
        /// <param name="position">
        /// Impact position.
        /// </param>
        /// <param name="normal">
        /// Impact surface normal.
        /// </param>
        /// <param name="damage">
        /// Damage dealt by the impact.
        /// </param>
        /// <param name="parentRef">
        /// Optional parent network object reference.
        /// </param>
        public void PlayImpactEffect(
            Vector3 position,
            Vector3 normal,
            float damage,
            NetworkObjectReference parentRef)
        {
            PlayImpactEffectRpc(
                position,
                normal,
                damage,
                parentRef
            );
        }

        #endregion

        #region Private Methods

        private void HandleFiringMechanismShot()
        {
            if (!CanFire())
            {
                return;
            }

            // Populate shooting context with all necessary data.
            m_ShootingContext.owner =
                m_OwnerPlayerManager.gameObject;

            m_ShootingContext.origin =
                muzzle != null
                    ? muzzle.position
                    : m_AimTransform.position;

            m_ShootingContext.direction =
                m_LiveFireDirection;

            m_ShootingContext.muzzle =
                muzzle;

            m_ShootingContext.damage =
                weaponData.weaponDamage;

            m_ShootingContext.ownerClientId =
                OwnerClientId;

            m_ShootingContext.hitMask =
                weaponData.hitMask;

            m_ShootingContext.currentSpread =
                m_SpreadHandler.CurrentSpreadAngle;

            m_ShootingContext.Weapon =
                this;

            m_ShootingContext.OnAmmoConsumed =
                m_AmmoHandler.ConsumeAmmo;

            m_ShootingContext.HasAmmoCheck =
                m_AmmoHandler.HasAmmo;

            m_ShootingContext.OnTargetHit =
                null;

            m_ShootingContext.OnHitPointCalculated =
                null;

            /*
             * Execute the actual weapon shot first.
             *
             * At this point the firing mechanism has produced a legitimate
             * shot and CanFire has already passed.
             */
            m_ShootingBehavior.Shoot(
                m_ShootingContext
            );

            /*
             * Gameplay noise is deliberately separate from presentation
             * audio. NoiseEmitter forwards client-owned weapon noise to
             * the authoritative server before NoiseSystem distributes it
             * to AI hearing sensors.
             */
            noiseEmitter?.RequestEmit();

            m_SpreadHandler.IncreaseSpread();

            PlayMuzzleFlashRpc(
                muzzle.position,
                muzzle.forward
            );

            CoreDirector
                .RequestCameraShake()
                .WithImpulseDefinition(
                    CinemachineImpulseDefinition
                        .ImpulseShapes.Recoil,
                    CinemachineImpulseDefinition
                        .ImpulseTypes.Propagating,
                    0.2f
                )
                .WithVelocity(
                    weaponData.cameraShakeIntensity
                )
                .Execute();

            m_StateManager.TransitionToState(
                WeaponState.Firing,
                0.05f
            );

            onWeaponFiredEvent?.Raise(
                m_FiringMechanism.FireRate
            );
        }

        private bool IsMuzzleObstructed()
        {
            if (m_OwnerPlayerManager == null ||
                muzzle == null)
            {
                return false;
            }

            Vector3 chestPosition =
                m_OwnerPlayerManager.transform.position +
                Vector3.up * chestHeightOffset;

            /*
             * Cache muzzle direction and distance while unobstructed.
             * This allows consistent local-space obstruction checks
             * even while the character rotates.
             */
            if (!m_WasMuzzleObstructed)
            {
                Vector3 toMuzzle =
                    muzzle.position -
                    chestPosition;

                m_CachedObstructionDirection =
                    m_OwnerPlayerManager.transform
                        .InverseTransformDirection(
                            toMuzzle.normalized
                        );

                m_CachedObstructionDistance =
                    toMuzzle.magnitude;

                m_HasCachedObstructionData =
                    true;
            }

            if (!m_HasCachedObstructionData)
            {
                return false;
            }

            Vector3 worldDirection =
                m_OwnerPlayerManager.transform
                    .TransformDirection(
                        m_CachedObstructionDirection
                    );

            return Physics.Raycast(
                chestPosition,
                worldDirection,
                m_CachedObstructionDistance,
                obstructionMask
            );
        }

        private void UpdateMuzzleObstructionState()
        {
            if (m_OwnerPlayerManager?.AimController == null ||
                !m_OwnerPlayerManager.AimController.IsAiming)
            {
                if (m_WasMuzzleObstructed)
                {
                    m_WasMuzzleObstructed = false;

                    m_OwnerPlayerManager?
                        .ShooterAnimator?
                        .Animator
                        .SetBool(
                            k_AnimIDIsColliding,
                            false
                        );

                    m_OwnerPlayerManager?
                        .AimController?
                        .SetWeaponObstructed(false);
                }

                return;
            }

            bool isObstructed =
                IsMuzzleObstructed();

            if (isObstructed ==
                m_WasMuzzleObstructed)
            {
                return;
            }

            m_WasMuzzleObstructed =
                isObstructed;

            m_OwnerPlayerManager?
                .ShooterAnimator?
                .Animator
                .SetBool(
                    k_AnimIDIsColliding,
                    isObstructed
                );

            m_OwnerPlayerManager?
                .AimController?
                .SetWeaponObstructed(
                    isObstructed
                );
        }

        #endregion

        #region RPCs

        [Rpc(SendTo.Everyone)]
        private void PlayMuzzleFlashRpc(
            Vector3 position,
            Vector3 muzzleDirection)
        {
            CoreDirector
                .RequestAudio(
                    weaponData.soundDefShoot
                )
                .WithPosition(position)
                .AsReserved(
                    SoundEmitter.ReservedInfo
                        .ReservedEmitter
                )
                .Play();

            CoreDirector
                .CreatePrefabEffect(
                    weaponData.muzzleFlashPrefab
                )
                .WithPosition(position)
                .WithParent(transform)
                .WithLookDirection(
                    -muzzleDirection
                )
                .WithName("MuzzleFlash")
                .WithDuration(0.3f)
                .Create();

            CoreDirector
                .CreatePrefabEffect(
                    weaponData.bulletShellPrefab
                )
                .WithPosition(
                    bulletShellEjectPoint.position
                )
                .WithLookDirection(
                    -muzzleDirection
                )
                .WithName("Shell")
                .WithDuration(1f)
                .Create();
        }

        [Rpc(SendTo.Everyone)]
        public void PlayTracerEffectRpc(
            Vector3 start,
            Vector3 end)
        {
            CoreDirector
                .CreateTracer(
                    start,
                    end
                )
                .WithScale(
                    new Vector3(
                        weaponData.tracerRadius * 2f,
                        Vector3.Distance(
                            start,
                            end
                        ) * 0.5f,
                        weaponData.tracerRadius * 2f
                    )
                )
                .WithMaterial(
                    weaponData.tracerMaterial
                )
                .WithDuration(
                    weaponData.tracerDuration
                )
                .Create();
        }

        [Rpc(SendTo.Everyone)]
        public void PlayImpactEffectRpc(
            Vector3 position,
            Vector3 normal,
            float damage,
            NetworkObjectReference parentRef)
        {
            Transform parent =
                null;

            if (parentRef.TryGet(
                    out var netObj))
            {
                parent =
                    netObj.transform;
            }

            CoreDirector
                .CreatePrefabEffect(
                    weaponData.impactEffectPrefab
                )
                .WithPosition(position)
                .WithParent(parent)
                .WithLookDirection(normal)
                .WithDuration(
                    weaponData.impactEffectDuration
                )
                .Create();

            CoreDirector
                .RequestAudio(
                    weaponData.soundDefRicochet
                )
                .WithPosition(position)
                .Play();
        }

        #endregion
    }
}

