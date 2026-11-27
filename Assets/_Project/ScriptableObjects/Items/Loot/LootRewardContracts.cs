using UnityEngine;

namespace TheLastSasak.Items.Loot
{
    public enum LootRewardType
    {
        Item = 0,
        Currency = 1,
        Experience = 2
    }

    public enum LootPickupCondition
    {
        AutoOnTrigger = 0,
        ManualInteract = 1
    }

    /// <summary>
    /// Implementasikan pada CurrencyWallet milik proyek.
    /// </summary>
    public interface ICurrencyReceiver
    {
        void AddCurrency(int amount);
    }

    /// <summary>
    /// Implementasikan pada ExperienceSystem milik proyek.
    /// </summary>
    public interface IExperienceReceiver
    {
        void AddExperience(int amount);
    }

    /// <summary>
    /// Implementasikan pada komponen interaksi dunia agar PlayerInteract dapat memanggilnya.
    /// </summary>
    public interface ILootInteractable
    {
        string GetInteractPrompt();
        bool CanInteract(GameObject interactor);
        void Interact(GameObject interactor);
    }
}