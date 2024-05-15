namespace PowerThreadPool.Constants
{
    //internal static class WorkerGettedFlags
    //{
    //    internal const int Unlocked = 0;
    //    internal const int Locked = 1;
    //    internal const int ToBeDisabled = 2;
    //    internal const int Disabled = -1;
    //}

    internal enum WorkerGettedFlags
    {
        Unlocked = 0,
        Locked = 1,
        ToBeDisabled = 2,
        Disabled = -1,
    }
}
