namespace TheLastSasak.Items.Definitions
{
    public abstract class EquipmentDefinitionSO : ItemDefinitionSO
    {
        protected override void OnValidate()
        {
            // Aturan v1.0: Weapon, Armor, dan Artifact tidak dapat ditumpuk (MaxStackSize = 1)
            SetMaxStackSize(1);
            base.OnValidate();
        }
    }
}