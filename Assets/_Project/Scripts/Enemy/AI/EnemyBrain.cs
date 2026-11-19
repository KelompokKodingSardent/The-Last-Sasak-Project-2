using System.Collections;
using UnityEngine;

public class EnemyBrain : MonoBehaviour
{
    public EnemyDefinitionSO Definition;
    public Transform Target;

    EnemyState state = EnemyState.Idle;
    EnemyMovement movement;
    EnemyAttackController attack;
    Health health;
    float attackTimer;

    public EnemyState State => state;

    void Awake()
    {
        movement = GetComponent<EnemyMovement>();
        attack = GetComponent<EnemyAttackController>();
        health = GetComponent<Health>();
    }

    void OnEnable()
    {
        if (health != null)
            health.OnDied += HandleDeath;
        if (Definition == null)
            Definition = EnemyDefinitionSO.CreateRuntime(name, EnemyArchetype.Melee);
        if (Target == null)
        {
            GameObject player = GameObject.FindGameObjectWithTag("Player");
            if (player != null)
                Target = player.transform;
        }
    }

    void OnDisable()
    {
        if (health != null)
            health.OnDied -= HandleDeath;
        movement?.Stop();
    }

    public void Bind(EnemyDefinitionSO definition, Transform target, ProjectilePool pool)
    {
        Definition = definition;
        Target = target;
        attack?.Bind(definition, pool);
        movement?.Configure(definition != null ? definition.MoveSpeed : 3.5f);
        if (health != null && definition != null)
            health.Configure(definition.MaxHealth);
        ResetBrain();
    }

    public void ResetBrain()
    {
        state = EnemyState.Idle;
        attackTimer = 0f;
        if (health != null && !health.IsDead)
            movement?.Stop();
    }

    void Update()
    {
        if (health != null && health.IsDead)
            return;
        if (Definition == null || Target == null || movement == null)
        {
            Enter(EnemyState.Idle);
            return;
        }

        float dist = HorizontalDistance(transform.position, Target.position);
        if (dist > Definition.DetectRange)
        {
            Enter(EnemyState.Idle);
            return;
        }

        if (dist <= Definition.AttackRange)
            Enter(EnemyState.Attack);
        else
            Enter(EnemyState.Chase);

        switch (state)
        {
            case EnemyState.Idle:
                movement.Stop();
                break;
            case EnemyState.Chase:
                movement.MoveTo(Target.position);
                break;
            case EnemyState.Attack:
                if (CombatDirector.Instance != null && !CombatDirector.Instance.RequestBrainAggro(this))
                {
                    movement.MoveTo(Target.position);
                    break;
                }

                movement.Stop();
                movement.Face(Target.position, 10f);
                attackTimer -= Time.deltaTime;
                if (attackTimer <= 0f && attack != null && attack.CanAttack)
                {
                    attack.PerformAttack(Target);
                    attackTimer = Definition.AttackCooldown;
                    CombatDirector.Instance?.ReleaseBrain(this);
                }
                break;
        }
    }

    void Enter(EnemyState next)
    {
        state = next;
    }

    void HandleDeath()
    {
        movement?.Stop();
        CombatDirector.Instance?.ReleaseBrain(this);
        StartCoroutine(ReleaseNextFrame());
    }

    IEnumerator ReleaseNextFrame()
    {
        yield return null;
        if (ObjectPoolManager.Instance != null)
            ObjectPoolManager.Instance.Release(gameObject);
        else
            gameObject.SetActive(false);
    }

    static float HorizontalDistance(Vector3 a, Vector3 b)
    {
        a.y = 0f;
        b.y = 0f;
        return Vector3.Distance(a, b);
    }
}
