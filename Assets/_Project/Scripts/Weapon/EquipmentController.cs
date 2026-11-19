using UnityEngine;

public class EquipmentController : MonoBehaviour
{
    public AttackDefinitionSO[] Loadout;
    ItemSwitching switching;

    void Awake() => switching = GetComponent<ItemSwitching>();

    public void SetLoadout(AttackDefinitionSO[] attacks)
    {
        Loadout = attacks;
        if (switching != null)
            switching.Attacks = attacks;
    }
}
