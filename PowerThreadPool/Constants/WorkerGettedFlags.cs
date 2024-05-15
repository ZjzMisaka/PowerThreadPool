namespace PowerThreadPool.Constants
{
    internal enum WorkerGettedFlags
    {
        Unlocked = 0,
        Locked = 1,
        ToBeDisabled = 2,
        Disabled = -1,
    }
}
