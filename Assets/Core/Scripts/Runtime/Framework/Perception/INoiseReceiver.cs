namespace TheBureau.Gameplay.Perception
{
    /// <summary>
    /// Represents a gameplay system capable of receiving noise stimuli.
    ///
    /// Implementations decide whether and how a received stimulus
    /// should affect their local perception state.
    /// </summary>
    public interface INoiseReceiver
    {
        /// <summary>
        /// Receives a gameplay noise stimulus.
        /// </summary>
        /// <param name="stimulus">
        /// Noise stimulus being distributed.
        /// </param>
        void ReceiveNoise(in NoiseStimulus stimulus);
    }
}