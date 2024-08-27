namespace PowerThreadPool.Constants
{
    internal enum CanGetWork
    {
        Allowed = 0,
        NotAllowed = 1,
        ToBeDisabled = 2,
        Disabled = -1,
    }
}
