using UnityEngine;

public class EnemyPool : MonoBehaviour
{
    public GameObject Get(GameObject prefab, Vector3 position, Quaternion rotation)
    {
        return ObjectPoolManager.Instance != null
            ? ObjectPoolManager.Instance.Get(prefab, position, rotation)
            : Instantiate(prefab, position, rotation);
    }

    public void Release(GameObject instance)
    {
        if (ObjectPoolManager.Instance != null)
            ObjectPoolManager.Instance.Release(instance);
        else if (instance != null)
            instance.SetActive(false);
    }
}
