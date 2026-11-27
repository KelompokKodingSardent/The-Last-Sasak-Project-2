using System.Collections.Generic;
using NUnit.Framework;
using UnityEngine;
using TheLastSasak.Items.Modifiers;
using TheLastSasak.Systems.Save;

public class GameplayTests
{
    [Test]
    public void HealthDeathFiresOnce()
    {
        GameObject go = new GameObject("H");
        Health health = go.AddComponent<Health>();
        health.Configure(10f);
        int deaths = 0;
        health.OnDied += () => deaths++;
        health.TakeDamage(DamageInfo.Create(20f, go, false));
        health.TakeDamage(DamageInfo.Create(20f, go, false));
        Assert.AreEqual(1, deaths);
        Assert.IsTrue(health.IsDead);
        Object.DestroyImmediate(go);
    }

    [Test]
    public void InvulnerableBlocksDamage()
    {
        GameObject go = new GameObject("H");
        InvulnerabilityHandler inv = go.AddComponent<InvulnerabilityHandler>();
        Health health = go.AddComponent<Health>();
        health.Configure(50f);
        inv.Activate(1f);
        DamageResult result = health.TakeDamage(DamageInfo.Create(10f, go, false));
        Assert.AreEqual(DamageOutcome.Blocked, result.Outcome);
        Assert.AreEqual(50f, health.CurrentHealth);
        Object.DestroyImmediate(go);
    }

    [Test]
    public void SaveDataEnsuresValidStructure()
    {
        TLS_SaveData data = new TLS_SaveData();
        data.EnsureValidStructure();
        Assert.IsNotNull(data.PlayerProgressionData);
    }

    [Test]
    public void StatModifierApplies()
    {
        var baseStats = new Dictionary<StatType, float>
        {
            { StatType.AttackDamage, 10f }
        };

        var modifiers = new List<IStatModifier>
        {
            new StatModifier(StatType.AttackDamage, ModifierOperation.Additive, 15f, "test_mod")
        };

        var finalStats = new Dictionary<StatType, float>();
        ModifierCalculator.CalculateAllStats(baseStats, modifiers, finalStats);

        Assert.AreEqual(25f, finalStats[StatType.AttackDamage]);
    }

    [Test]
    public void RoomGraphKeepsRooms()
    {
        RoomGraph graph = new RoomGraph();
        graph.Add(Vector2Int.zero, RoomType.Start);
        graph.Add(Vector2Int.up, RoomType.Combat);
        Assert.AreEqual(2, graph.Rooms.Count);
    }

    [Test]
    public void ExperienceLevelsUp()
    {
        GameObject go = new GameObject("Exp");
        ExperienceSystem exp = go.AddComponent<ExperienceSystem>();
        exp.ExpPerLevel = 10;
        exp.Add(25);
        Assert.AreEqual(3, exp.Level);
        Object.DestroyImmediate(go);
    }
}