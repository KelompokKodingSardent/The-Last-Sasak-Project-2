using UnityEngine;

public class StaggerComponent : MonoBehaviour
{
    public float MaxStagger = 100f;
    public float Current;
    public System.Action OnStaggered;

    public void Add(float amount)
    {
        if (Current >= MaxStagger)
            return;

        Current += amount;
        if (Current >= MaxStagger)
        {
            Current = MaxStagger;
            OnStaggered?.Invoke();
            EnemyBrain brain = GetComponent<EnemyBrain>();
            brain?.ResetBrain();
        }
    }

    public void ResetStagger()
    {
        Current = 0f;
    }
}
