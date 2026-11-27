namespace TheLastSasak.Systems.Save
{
    public static class SaveValidator
    {
        public const int CurrentVersion = 1;

        public static bool IsValid(TLS_SaveData data)
        {
            return data != null
                && data.SaveVersion > 0
                && data.CurrencyData != null
                && data.CurrencyData.Souls >= 0;
        }
    }
}