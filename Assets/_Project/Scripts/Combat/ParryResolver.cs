using UnityEngine;

public class ParryResolver : MonoBehaviour
{
    public bool TryResolve(GameObject defender, DamageInfo info, out IParryable parryable)
    {
        parryable = null;
        if (defender == null || !info.CanBeParried)
            return false;

        ParryWindow window = defender.GetComponent<ParryWindow>();
        if (window == null || !window.IsActive)
            return false;

        if (info.Instigator != null)
        {
            info.Instigator.TryGetComponent(out parryable);
            if (parryable == null)
                parryable = info.Instigator.GetComponentInParent<IParryable>();
        }

        return true;
    }
}
