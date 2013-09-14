namespace PNetC
{
    /// <summary>
    /// how to send the rpc
    /// </summary>
    public enum RPCMode
    {
        /// <summary>
        /// to the server
        /// </summary>
        Server = 0,
        /// <summary>
        /// to everyone but me
        /// </summary>
        Others = 1,
        /// <summary>
        /// to everyone
        /// </summary>
        All = 2,
        /// <summary>
        /// to everyone but me, buffered to new players after the initial call
        /// </summary>
        OthersBuffered = 5,
        /// <summary>
        /// to everyone, buffered to new players after the initial call
        /// </summary>
        AllBuffered = 6,
    }
}