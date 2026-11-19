using UnityEngine;

public class ProjectilePool : MonoBehaviour
{
    public GameObject Prefab;

    public Projectile Spawn(Vector3 position, Vector3 direction, GameObject owner, bool fromPlayer, float damage)
    {
        if (Prefab == null)
            return null;

        GameObject go = ObjectPoolManager.Instance != null
            ? ObjectPoolManager.Instance.Get(Prefab, position, Quaternion.LookRotation(direction))
            : Instantiate(Prefab, position, Quaternion.LookRotation(direction));

        Projectile projectile = go.GetComponent<Projectile>();
        if (projectile == null)
            return null;
        projectile.Launch(direction, owner, fromPlayer, damage);
        return projectile;
    }
}
