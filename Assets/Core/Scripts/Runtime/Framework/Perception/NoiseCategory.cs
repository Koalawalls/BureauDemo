namespace TheBureau.Gameplay.Perception
{
    /// <summary>
    /// The categories allow AI systems to distinguish between different kinds
    /// of sounds without coupling hearing logic directly to weapons,
    /// movement, abilities, or environmental systems.
    /// </summary>
    public enum NoiseCategory : byte
    {
        /// <summary>
        /// Ordinary walking or movement-related footstep noise.
        /// </summary>
        Footstep = 0,

        /// <summary>
        /// General movement noise such as sprinting, sliding,
        /// vaulting, or other louder locomotion.
        /// </summary>
        Movement = 1,

        /// <summary>
        /// Physical impacts such as objects striking surfaces,
        /// melee impacts, or environmental collisions.
        /// </summary>
        Impact = 2,

        /// <summary>
        /// Firearm discharge or similar weapon-generated noise.
        /// </summary>
        Gunshot = 3,

        /// <summary>
        /// Noise created by character abilities or gameplay powers.
        /// </summary>
        Ability = 4,

        /// <summary>
        /// Explosive events such as grenades, detonations,
        /// or other high-energy impacts.
        /// </summary>
        Explosion = 5,

        /// <summary>
        /// Persistent or deliberate alerting sounds such as alarms,
        /// sirens, or warning devices.
        /// </summary>
        Alarm = 6,

        /// <summary>
        /// General environmental noise that does not fit another category.
        /// </summary>
        Environment = 7
    }
}