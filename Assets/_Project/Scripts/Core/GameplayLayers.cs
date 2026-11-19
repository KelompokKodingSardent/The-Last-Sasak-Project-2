using UnityEngine;

public static class GameplayLayers
{
    public static int Player => LayerMask.NameToLayer("Player");
    public static int Enemy => LayerMask.NameToLayer("Enemy");
    public static int Environment => LayerMask.NameToLayer("Environment");
    public static int PlayerHitbox => LayerMask.NameToLayer("PlayerHitbox");
    public static int EnemyHitbox => LayerMask.NameToLayer("EnemyHitbox");
    public static int Projectile => LayerMask.NameToLayer("Projectile");
    public static int Collectible => LayerMask.NameToLayer("Collectible");
    public static int Interactable => LayerMask.NameToLayer("Interactable");

    public static int EnemyMask => Mask(Enemy);
    public static int PlayerMask => Mask(Player);

    public static int Mask(int layer)
    {
        return layer >= 0 ? 1 << layer : 0;
    }

    public static void TrySet(GameObject go, int layer)
    {
        if (go != null && layer >= 0)
            go.layer = layer;
    }
}
