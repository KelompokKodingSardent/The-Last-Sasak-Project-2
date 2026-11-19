using UnityEngine;
using UnityEngine.UI;

public class HUDController : MonoBehaviour
{
    public Health PlayerHealth;
    public HealthBarUI HealthBar;
    public Text SoulsLabel;
    public Text ExpLabel;
    public Text RoomLabel;
    public InventoryUI InventoryView;

    void Start()
    {
        if (PlayerHealth == null)
        {
            GameObject player = GameObject.FindGameObjectWithTag("Player");
            if (player != null)
                PlayerHealth = player.GetComponent<Health>();
        }

        HealthBar?.Bind(PlayerHealth);
        InventoryView?.Bind(PlayerHealth != null ? PlayerHealth.GetComponent<Inventory>() : null);
    }

    void OnEnable()
    {
        GameEvents.OnCurrencyChanged += HandleSouls;
        GameEvents.OnExperienceChanged += HandleExp;
        GameEvents.OnRoomCleared += HandleCleared;
    }

    void OnDisable()
    {
        GameEvents.OnCurrencyChanged -= HandleSouls;
        GameEvents.OnExperienceChanged -= HandleExp;
        GameEvents.OnRoomCleared -= HandleCleared;
    }

    void HandleSouls(int value)
    {
        if (SoulsLabel != null)
            SoulsLabel.text = $"Souls {value}";
    }

    void HandleExp(int exp, int level)
    {
        if (ExpLabel != null)
            ExpLabel.text = $"Lv {level}  EXP {exp}";
    }

    void HandleCleared()
    {
        if (RoomLabel != null)
            RoomLabel.text = "Room Cleared";
    }
}
