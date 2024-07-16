namespace PowerThreadPool.Constants
{
    internal enum WorkerGettedFlags
    {
        Free = 0,
        Getted = 1,
        ToBeDisabled = 2,
        Disabled = -1,
    }
}
