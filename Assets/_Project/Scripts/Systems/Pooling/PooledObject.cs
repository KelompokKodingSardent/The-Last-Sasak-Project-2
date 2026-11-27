using UnityEngine;

public class PooledObject : MonoBehaviour
{
    public GameObject SourcePrefab { get; private set; }
    public PoolLeaseId CurrentLeaseId { get; private set; }
    public bool IsRented { get; private set; }
    public int RentCount { get; private set; }

    private IPoolable[] _poolables;
    private bool _poolablesCached;

    internal void BindPrefab(GameObject sourcePrefab)
    {
        SourcePrefab = sourcePrefab;
        CachePoolables();
    }

    internal void MarkRented(PoolLeaseId leaseId)
    {
        CachePoolables();
        CurrentLeaseId = leaseId;
        IsRented = true;
        RentCount++;

        for (int i = 0; i < _poolables.Length; i++)
        {
            _poolables[i]?.OnRentFromPool(leaseId);
        }
    }

    internal void MarkReturned()
    {
        CachePoolables();
        IsRented = false;
        CurrentLeaseId = default;

        for (int i = 0; i < _poolables.Length; i++)
        {
            _poolables[i]?.OnReturnToPool();
        }
    }

    public void ReleaseSelf()
    {
        if (!IsRented)
            return;

        if (ObjectPoolManager.Instance != null)
            ObjectPoolManager.Instance.Release(gameObject);
        else
            gameObject.SetActive(false);
    }

    private void CachePoolables()
    {
        if (_poolablesCached)
            return;

        _poolables = GetComponentsInChildren<IPoolable>(true);
        _poolablesCached = true;
    }
}