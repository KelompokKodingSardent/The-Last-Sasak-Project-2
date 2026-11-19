using NUnit.Framework;
using UnityEngine;

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
    public void SaveValidatorRejectsBadData()
    {
        Assert.IsFalse(SaveValidator.IsValid(null));
        Assert.IsFalse(SaveValidator.IsValid(new SaveData { SaveVersion = 0, SoulsCoin = 1 }));
        Assert.IsTrue(SaveValidator.IsValid(new SaveData { SaveVersion = 1, SoulsCoin = 0 }));
    }

    [Test]
    public void StatModifierApplies()
    {
        StatModifier mod = new StatModifier { Additive = 5f, Multiplier = 2f };
        Assert.AreEqual(30f, mod.Apply(10f));
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
