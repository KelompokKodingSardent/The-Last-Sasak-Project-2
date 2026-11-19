public static class SaveValidator
{
    public const int CurrentVersion = 1;

    public static bool IsValid(SaveData data)
    {
        return data != null && data.SaveVersion > 0 && data.SoulsCoin >= 0;
    }
}
