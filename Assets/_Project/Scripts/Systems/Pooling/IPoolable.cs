public interface IPoolable
{
    void OnRentFromPool(PoolLeaseId leaseId);
    void OnReturnToPool();
}