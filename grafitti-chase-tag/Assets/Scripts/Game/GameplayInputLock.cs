
public static class GameplayInputLock
{
    public static bool IsLocked { get; private set; }

    public static void SetLocked(bool locked)
    {
        IsLocked = locked;
    }
}
