using UnityEngine;
using UnityEngine.AI;

public static class EntityFactory
{
    public static GameObject CreatePlayer(Vector3 position, PlayerMovementConfig config, AttackDefinitionSO attack)
    {
        GameObject player = GameObject.CreatePrimitive(PrimitiveType.Capsule);
        player.name = "Player";
        player.tag = "Player";
        GameplayLayers.TrySet(player, GameplayLayers.Player);
        Object.Destroy(player.GetComponent<CapsuleCollider>());
        player.transform.position = position;

        CharacterController cc = player.AddComponent<CharacterController>();
        cc.height = 2f;
        cc.radius = 0.4f;
        cc.center = Vector3.up;

        PlayerInputReader input = player.AddComponent<PlayerInputReader>();
        PlayerMotor motor = player.AddComponent<PlayerMotor>();
        motor.Config = config;
        motor.ApplyControllerSettings();
        PlayerFacingController facing = player.AddComponent<PlayerFacingController>();
        facing.Config = config;
        player.AddComponent<PlayerAnimationController>();
        player.AddComponent<InvulnerabilityHandler>();
        Health health = player.AddComponent<Health>();
        health.Configure(100f);
        player.AddComponent<ParryWindow>();
        player.AddComponent<HitboxController>();
        player.AddComponent<PlayerActionController>();
        DodgeController dodge = player.AddComponent<DodgeController>();
        dodge.BindConfig(config);
        player.AddComponent<ParryController>();
        AttackController attackController = player.AddComponent<AttackController>();
        attackController.Definition = attack;
        attackController.TargetMask = GameplayLayers.EnemyMask;
        player.AddComponent<PlayerDeathHandler>();
        player.AddComponent<Inventory>();
        player.AddComponent<PlayerInteract>();
        player.AddComponent<PlayerProgression>();
        player.AddComponent<ArtifactController>();
        player.AddComponent<ItemSwitching>();
        player.AddComponent<EquipmentController>();
        if (SaveSystem.Instance != null && SaveSystem.Instance.PermanentHealth > 0f)
            health.Configure(100f + SaveSystem.Instance.PermanentHealth);

        GameObject cameraTarget = new GameObject("CameraTarget");
        cameraTarget.transform.SetParent(player.transform);
        cameraTarget.transform.localPosition = Vector3.up * 1.4f;

        Colorize(player, new Color(0.2f, 0.55f, 0.9f));
        return player;
    }

    public static GameObject CreateEnemyTemplate(EnemyDefinitionSO definition)
    {
        GameObject enemy = GameObject.CreatePrimitive(PrimitiveType.Capsule);
        enemy.name = definition.DisplayName;
        enemy.tag = "Enemy";
        GameplayLayers.TrySet(enemy, GameplayLayers.Enemy);
        Object.Destroy(enemy.GetComponent<CapsuleCollider>());

        CapsuleCollider col = enemy.AddComponent<CapsuleCollider>();
        col.height = 2f;
        col.radius = 0.4f;
        col.center = Vector3.up;
        col.isTrigger = false;

        NavMeshAgent agent = enemy.AddComponent<NavMeshAgent>();
        agent.speed = definition.MoveSpeed;
        agent.radius = 0.4f;
        agent.height = 2f;
        agent.stoppingDistance = Mathf.Max(0.2f, definition.AttackRange * 0.6f);
        agent.acceleration = 10f;
        agent.angularSpeed = 720f;

        enemy.AddComponent<Health>().Configure(definition.MaxHealth);
        enemy.AddComponent<EnemyMovement>();
        EnemyAttackController attack = enemy.AddComponent<EnemyAttackController>();
        attack.Definition = definition;
        EnemyBrain brain = enemy.AddComponent<EnemyBrain>();
        brain.Definition = definition;
        if (definition.Archetype == EnemyArchetype.Elite)
            enemy.AddComponent<StaggerComponent>();

        Color color = definition.Archetype == EnemyArchetype.Ranged
            ? new Color(0.85f, 0.45f, 0.15f)
            : definition.Archetype == EnemyArchetype.Elite
                ? new Color(0.6f, 0.1f, 0.7f)
                : new Color(0.75f, 0.2f, 0.2f);
        Colorize(enemy, color);
        enemy.SetActive(false);
        return enemy;
    }

    public static GameObject CreateProjectileTemplate()
    {
        GameObject ball = GameObject.CreatePrimitive(PrimitiveType.Sphere);
        ball.name = "Projectile";
        ball.tag = "Projectile";
        GameplayLayers.TrySet(ball, GameplayLayers.Projectile);
        ball.transform.localScale = Vector3.one * 0.35f;
        SphereCollider col = ball.GetComponent<SphereCollider>();
        col.isTrigger = true;
        Rigidbody rb = ball.AddComponent<Rigidbody>();
        rb.isKinematic = true;
        rb.useGravity = false;
        ball.AddComponent<Projectile>();
        Colorize(ball, Color.yellow);
        ball.SetActive(false);
        return ball;
    }

    static void Colorize(GameObject go, Color color)
    {
        Renderer renderer = go.GetComponent<Renderer>();
        if (renderer == null)
            return;
        renderer.material = new Material(renderer.sharedMaterial);
        renderer.material.color = color;
    }
}
