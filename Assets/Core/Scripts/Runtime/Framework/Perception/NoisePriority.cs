namespace TheBureau.Gameplay.Perception
{
    /// <summary>
    /// Defines the relative gameplay importance of a noise stimulus.
    ///
    /// Priority does not determine how far a noise travels. Hearing range
    /// and priority are intentionally separate so that AI can evaluate both
    /// the audibility and significance of a stimulus.
    /// </summary>
    public enum NoisePriority : byte
    {
        /// <summary>
        /// Minor stimuli such as quiet footsteps or small interactions.
        /// </summary>
        Low = 0,

        /// <summary>
        /// Noticeable stimuli such as sprinting or louder movement.
        /// </summary>
        Medium = 1,

        /// <summary>
        /// Significant stimuli such as gunfire or combat abilities.
        /// </summary>
        High = 2,

        /// <summary>
        /// Major stimuli that should strongly attract attention,
        /// such as explosions or alarms.
        /// </summary>
        Critical = 3
    }
}