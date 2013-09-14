namespace PNetC
{
    /// <summary>
    /// way to serialize the changes to the network view
    /// </summary>
    public enum NetworkStateSynchronization
    {
        /// <summary>
        /// do not run serialization
        /// </summary>
        Off = 0,
        /// <summary>
        /// only if there are changes in the stream, reliably
        /// </summary>
        ReliableDeltaCompressed = 1,
        /// <summary>
        /// always, but unreliably
        /// </summary>
        Unreliable = 2,
    }
}