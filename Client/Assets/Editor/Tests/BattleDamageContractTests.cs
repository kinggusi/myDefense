using System.Collections.Generic;
using System.Linq;
using System.Reflection;
using MyDefense.Battle.Runtime;
using MyDefense.Battle.Balance.Canonical;
using MyDefense.Battle.Presentation;
using MyDefense.Shared.Contracts;
using NUnit.Framework;
using UnityEngine;

public sealed class BattleDamageContractTests
{
    [Test]
    public void UnitSpeciesColorsUseFixedSevenColorPaletteAcrossGrades()
    {
        MethodInfo method = typeof(FusionKidnapBoardView).GetMethod(
            "ColorForAlien",
            BindingFlags.NonPublic | BindingFlags.Static);

        Assert.That(method, Is.Not.Null);
        var legendColors = Enumerable.Range(1, 7)
            .Select(id => (Color)method.Invoke(null, new object[] { (long)id }))
            .ToArray();
        var normalColors = Enumerable.Range(22, 7)
            .Select(id => (Color)method.Invoke(null, new object[] { (long)id }))
            .ToArray();

        Assert.That(legendColors.Distinct().Count(), Is.EqualTo(7));
        Assert.That(normalColors, Is.EqualTo(legendColors));
    }

    [Test]
    public void AttackSnapshotUsesPrecomputedStatsAndNormalizesMissingMutation()
    {
        AlienAttackSnapshot snapshot = AlienAttackSnapshot.FromCalculatedStats(17, 25f, 2f, 8f, " ");

        Assert.That(snapshot.AttackerServerId, Is.EqualTo(17));
        Assert.That(snapshot.Damage, Is.EqualTo(25f));
        Assert.That(snapshot.ActiveMutationType, Is.EqualTo("NONE"));
    }

    [Test]
    public void ActiveMutationAppliesCanonicalStatsAndMechanicOnlyOnce()
    {
        AlienAttackSnapshot snapshot = AlienAttackSnapshot.FromCalculatedStats(17, 100f, 2f, 8f, "TOXIC");
        var spec = new CanonicalMutationSpec(
            "TOXIC", true, true, true, 1,
            1.1f, 1f, 1f, 1f, 1f,
            "DOT", dotDamageMultiplier: 0.2f, dotTickCount: 3, dotTickIntervalSeconds: 1f);

        AlienAttackSnapshot result = MutationAttackSnapshotCalculator.Apply(snapshot, spec);

        Assert.That(result.Damage, Is.EqualTo(110f).Within(0.001f));
        Assert.That(result.DotDamagePerTick, Is.EqualTo(22f).Within(0.001f));
        Assert.That(result.DotTickCount, Is.EqualTo(3));
        Assert.That(result.DotTickIntervalSeconds, Is.EqualTo(1f));
    }

    [Test]
    public void SealedOrMissingMutationKeepsServerSnapshotUnchanged()
    {
        AlienAttackSnapshot snapshot = AlienAttackSnapshot.FromCalculatedStats(17, 100f, 2f, 8f, "NONE");
        var spec = new CanonicalMutationSpec(
            "GIANT", true, true, true, 1,
            1.35f, 1f, 0.9f, 1.1f, 1f,
            "SPLASH", splashRadius: 2.5f, splashDamageMultiplier: 0.65f);

        AlienAttackSnapshot result = MutationAttackSnapshotCalculator.Apply(snapshot, spec);

        Assert.That(result.Damage, Is.EqualTo(100f));
        Assert.That(result.SplashRadius, Is.EqualTo(0f));
    }

    [Test]
    public void BossAndGambleDamageAreDeterministicFromAuthorityProjectileId()
    {
        AlienAttackSnapshot snapshot = AlienAttackSnapshot.FromCalculatedStats(17, 100f, 2f, 8f, "OBESE");
        snapshot.BossDamageMultiplier = 2f;
        snapshot.GambleSuccessChance = 0.25f;
        snapshot.GambleSuccessMultiplier = 2.5f;
        snapshot.GambleFailureMultiplier = 0.5f;

        float first = MutationAttackSnapshotCalculator.ResolveDeterministicDamage(snapshot, 42, true);
        float retry = MutationAttackSnapshotCalculator.ResolveDeterministicDamage(snapshot, 42, true);
        float other = MutationAttackSnapshotCalculator.ResolveDeterministicDamage(snapshot, 43, true);

        Assert.That(first, Is.EqualTo(retry));
        Assert.That(first, Is.EqualTo(100f).Or.EqualTo(500f));
        Assert.That(other, Is.EqualTo(100f).Or.EqualTo(500f));
    }

    [Test]
    public void ServerCalculatedAttackCatalogMustMatchPinnedCanonicalBalance()
    {
        var response = new FusionKidnapBoardView.BattleAttackSnapshotCatalogJson
        {
            balanceVersion = "1-version",
            contentHash = "ABCDEF"
        };

        Assert.That(FusionKidnapBoardView.IsCompatibleCatalog(response, "1-version", "abcdef"), Is.True);
        Assert.That(FusionKidnapBoardView.IsCompatibleCatalog(response, "2-version", "abcdef"), Is.False);
        Assert.That(FusionKidnapBoardView.IsCompatibleCatalog(response, "1-version", "other"), Is.False);
    }

    [Test]
    public void FusionMutationStateFeedsAttackSnapshotMetadata()
    {
        GameObject unit = new GameObject("mutation-state-test");
        try
        {
            UnitData data = unit.AddComponent<UnitData>();
            FusionKidnapBoardView.ApplyMutationState(data, 3, "FROZEN");
            Assert.That(data.activeMutationType, Is.EqualTo("FROZEN"));
            Assert.That(data.pendingMutationType, Is.Null);

            FusionKidnapBoardView.ApplyMutationState(data, 4, "FROZEN");
            Assert.That(data.activeMutationType, Is.Null);
            Assert.That(data.pendingMutationType, Is.EqualTo("FROZEN"));
        }
        finally
        {
            Object.DestroyImmediate(unit);
        }
    }

    [Test]
    public void UnitAttack_TargetSearchSkipsActiveDeadFadeMonster()
    {
        GameObject unitObject = new GameObject("dead-target-search-unit");
        GameObject deadMonster = new GameObject("dead-fade-monster");
        GameObject livingMonster = new GameObject("living-monster");
        try
        {
            unitObject.AddComponent<UnitData>();
            UnitAttack attack = unitObject.AddComponent<UnitAttack>();
            deadMonster.tag = "Monster";
            livingMonster.tag = "Monster";
            deadMonster.transform.position = Vector3.right;
            livingMonster.transform.position = Vector3.right * 2f;
            deadMonster.AddComponent<MonsterStat>().ApplyNetworkState(0f, 10f, true);
            livingMonster.AddComponent<MonsterStat>().ApplyNetworkState(10f, 10f, false);

            MethodInfo updateTarget = typeof(UnitAttack).GetMethod(
                "UpdateTarget",
                BindingFlags.Instance | BindingFlags.NonPublic);
            FieldInfo targetField = typeof(UnitAttack).GetField(
                "target",
                BindingFlags.Instance | BindingFlags.NonPublic);
            Assert.That(updateTarget, Is.Not.Null);
            Assert.That(targetField, Is.Not.Null);

            updateTarget.Invoke(attack, new object[] { 10f });

            Assert.That(targetField.GetValue(attack), Is.EqualTo(livingMonster.transform));
            Assert.That(deadMonster.activeInHierarchy, Is.True, "Fade presentation remains active during this check.");
        }
        finally
        {
            Object.DestroyImmediate(livingMonster);
            Object.DestroyImmediate(deadMonster);
            Object.DestroyImmediate(unitObject);
        }
    }

    [Test]
    public void UnitAttack_AndLegacyBulletResolveChildOnlyDamageable()
    {
        GameObject unitObject = new GameObject("child-damageable-unit");
        GameObject monsterRoot = new GameObject("child-damageable-monster-root");
        GameObject monsterChild = new GameObject("child-damageable-monster-child");
        try
        {
            unitObject.AddComponent<UnitData>();
            UnitAttack attack = unitObject.AddComponent<UnitAttack>();
            monsterRoot.tag = "Monster";
            monsterRoot.transform.position = Vector3.right;
            monsterChild.transform.SetParent(monsterRoot.transform, false);
            MonsterStat childStat = monsterChild.AddComponent<MonsterStat>();
            childStat.ApplyNetworkState(10f, 10f, false);

            MethodInfo updateTarget = typeof(UnitAttack).GetMethod(
                "UpdateTarget",
                BindingFlags.Instance | BindingFlags.NonPublic);
            FieldInfo targetField = typeof(UnitAttack).GetField(
                "target",
                BindingFlags.Instance | BindingFlags.NonPublic);
            MethodInfo bulletTargetAlive = typeof(Bullet).GetMethod(
                "IsTargetAlive",
                BindingFlags.Static | BindingFlags.NonPublic);
            Assert.That(updateTarget, Is.Not.Null);
            Assert.That(targetField, Is.Not.Null);
            Assert.That(bulletTargetAlive, Is.Not.Null);

            updateTarget.Invoke(attack, new object[] { 10f });

            Assert.That(targetField.GetValue(attack), Is.EqualTo(monsterRoot.transform));
            Assert.That((bool)bulletTargetAlive.Invoke(null, new object[] { monsterRoot.transform }), Is.True);

            childStat.ApplyNetworkState(0f, 10f, true);
            Assert.That((bool)bulletTargetAlive.Invoke(null, new object[] { monsterRoot.transform }), Is.False);
        }
        finally
        {
            Object.DestroyImmediate(monsterRoot);
            Object.DestroyImmediate(unitObject);
        }
    }

    [Test]
    public void UnitAttack_ReleasesMaintainedTargetWhenItDies()
    {
        GameObject unitObject = new GameObject("dead-maintained-target-unit");
        GameObject monsterObject = new GameObject("dead-maintained-target-monster");
        try
        {
            unitObject.AddComponent<UnitData>();
            UnitAttack attack = unitObject.AddComponent<UnitAttack>();
            MonsterStat monster = monsterObject.AddComponent<MonsterStat>();
            monster.ApplyNetworkState(10f, 10f, false);

            FieldInfo targetField = typeof(UnitAttack).GetField(
                "target",
                BindingFlags.Instance | BindingFlags.NonPublic);
            FieldInfo nextSearchField = typeof(UnitAttack).GetField(
                "nextTargetSearchTime",
                BindingFlags.Instance | BindingFlags.NonPublic);
            MethodInfo update = typeof(UnitAttack).GetMethod(
                "Update",
                BindingFlags.Instance | BindingFlags.NonPublic);
            Assert.That(targetField, Is.Not.Null);
            Assert.That(nextSearchField, Is.Not.Null);
            Assert.That(update, Is.Not.Null);
            targetField.SetValue(attack, monsterObject.transform);
            nextSearchField.SetValue(attack, float.MaxValue);

            monster.ApplyNetworkState(0f, 10f, true);
            update.Invoke(attack, null);

            Assert.That(targetField.GetValue(attack), Is.Null);
        }
        finally
        {
            Object.DestroyImmediate(monsterObject);
            Object.DestroyImmediate(unitObject);
        }
    }

    [Test]
    public void UnitAttack_AndLegacyBulletRejectTargetThatDiesBeforeFire()
    {
        GameObject unitObject = new GameObject("dead-before-fire-unit");
        GameObject monsterObject = new GameObject("dead-before-fire-monster");
        try
        {
            unitObject.AddComponent<UnitData>();
            UnitAttack attack = unitObject.AddComponent<UnitAttack>();
            MonsterStat monster = monsterObject.AddComponent<MonsterStat>();
            monster.ApplyNetworkState(0f, 10f, true);

            FieldInfo targetField = typeof(UnitAttack).GetField(
                "target",
                BindingFlags.Instance | BindingFlags.NonPublic);
            MethodInfo shoot = typeof(UnitAttack).GetMethod(
                "Shoot",
                BindingFlags.Instance | BindingFlags.NonPublic);
            MethodInfo bulletTargetAlive = typeof(Bullet).GetMethod(
                "IsTargetAlive",
                BindingFlags.Static | BindingFlags.NonPublic);
            Assert.That(targetField, Is.Not.Null);
            Assert.That(shoot, Is.Not.Null);
            Assert.That(bulletTargetAlive, Is.Not.Null);
            targetField.SetValue(attack, monsterObject.transform);

            shoot.Invoke(attack, null);

            Assert.That(targetField.GetValue(attack), Is.Null);
            Assert.That((bool)bulletTargetAlive.Invoke(null, new object[] { monsterObject.transform }), Is.False);
        }
        finally
        {
            Object.DestroyImmediate(monsterObject);
            Object.DestroyImmediate(unitObject);
        }
    }

    [Test]
    public void MonsterStat_DeadTargetDoesNotChangeDamageAuditState()
    {
        GameObject monsterObject = new GameObject("dead-damage-audit-monster");
        try
        {
            MonsterStat monster = monsterObject.AddComponent<MonsterStat>();
            monster.ApplyNetworkState(0f, 10f, true);

            monster.ApplyDamage(new DamagePayload
            {
                AttackerId = 99,
                Amount = 5f
            });

            Assert.That(monster.LastDamageAttackerId, Is.Zero);
            Assert.That(monster.DamageAttackerIds, Is.Empty);
            Assert.That(monster.CurrentHp, Is.Zero);
        }
        finally
        {
            Object.DestroyImmediate(monsterObject);
        }
    }

    [Test]
    public void AuthoritativeBoardStateSync_RebindsMovedAndSwappedUnitsOnlyOnce()
    {
        MethodInfo sync = typeof(FusionKidnapBoardView).GetMethod(
            "ApplyAuthoritativeUnitState",
            BindingFlags.NonPublic | BindingFlags.Static);
        Assert.That(sync, Is.Not.Null);

        GameObject sourceObject = new GameObject("source-unit-test");
        GameObject targetObject = new GameObject("target-unit-test");
        try
        {
            UnitData source = sourceObject.AddComponent<UnitData>();
            UnitData target = targetObject.AddComponent<UnitData>();
            ApplyAuthoritativeState(sync, source, 1, 2, 22, 0, 0, null);
            ApplyAuthoritativeState(sync, target, 1, 5, 23, 1, 0, null);

            Assert.That(ApplyAuthoritativeState(sync, source, 1, 5, 22, 0, 0, null), Is.True);
            Assert.That(ApplyAuthoritativeState(sync, target, 1, 2, 23, 1, 0, null), Is.True);
            Assert.That(source.serverId, Is.EqualTo(((long)1 << 32) | 6u));
            Assert.That(target.serverId, Is.EqualTo(((long)1 << 32) | 3u));
            Assert.That(source.specId, Is.EqualTo(22));
            Assert.That(target.specId, Is.EqualTo(23));
            Assert.That(source.grade, Is.EqualTo("NORMAL"));
            Assert.That(target.grade, Is.EqualTo("EPIC"));

            Assert.That(ApplyAuthoritativeState(sync, source, 1, 5, 22, 0, 0, null), Is.False);
            Assert.That(ApplyAuthoritativeState(sync, target, 1, 2, 23, 1, 0, null), Is.False);
        }
        finally
        {
            Object.DestroyImmediate(sourceObject);
            Object.DestroyImmediate(targetObject);
        }
    }

    [Test]
    public void AuthoritativeBoardStateSync_RefreshesMergeAndActiveMutationButNotPendingDna()
    {
        MethodInfo sync = typeof(FusionKidnapBoardView).GetMethod(
            "ApplyAuthoritativeUnitState",
            BindingFlags.NonPublic | BindingFlags.Static);
        Assert.That(sync, Is.Not.Null);

        GameObject unitObject = new GameObject("merged-unit-test");
        try
        {
            UnitData data = unitObject.AddComponent<UnitData>();
            Assert.That(ApplyAuthoritativeState(sync, data, 2, 7, 15, 2, 0, null), Is.True);
            Assert.That(data.serverId, Is.EqualTo(((long)2 << 32) | 8u));
            Assert.That(data.specId, Is.EqualTo(15));
            Assert.That(data.grade, Is.EqualTo("UNIQUE"));

            Assert.That(ApplyAuthoritativeState(sync, data, 2, 7, 15, 2, 2, "TOXIC"), Is.False);
            Assert.That(data.pendingMutationType, Is.EqualTo("TOXIC"));
            Assert.That(data.activeMutationType, Is.Null);

            Assert.That(ApplyAuthoritativeState(sync, data, 2, 7, 29, 4, 3, "FROZEN"), Is.True);
            Assert.That(data.specId, Is.EqualTo(29));
            Assert.That(data.grade, Is.EqualTo("MYTHIC"));
            Assert.That(data.pendingMutationType, Is.Null);
            Assert.That(data.activeMutationType, Is.EqualTo("FROZEN"));
            Assert.That(ApplyAuthoritativeState(sync, data, 2, 7, 29, 4, 3, "FROZEN"), Is.False);
        }
        finally
        {
            Object.DestroyImmediate(unitObject);
        }
    }

    [Test]
    public void InitialMutationVisual_ShowsOnlyActiveMutationAndSealedStateClearsIt()
    {
        MethodInfo applyInitial = typeof(FusionKidnapBoardView).GetMethod(
            "ApplyInitialActiveMutationVisual",
            BindingFlags.NonPublic | BindingFlags.Static);
        MethodInfo applyGrade = typeof(FusionKidnapBoardView).GetMethod(
            "ApplyGradeVisual",
            BindingFlags.NonPublic | BindingFlags.Static);
        Assert.That(applyInitial, Is.Not.Null);
        Assert.That(applyGrade, Is.Not.Null);

        GameObject unit = new GameObject("mutation-visual-state-test");
        try
        {
            UnitData data = unit.AddComponent<UnitData>();
            applyGrade.Invoke(null, new object[] { unit.transform, (byte)4, 29L });
            FusionKidnapBoardView.ApplyMutationState(data, 2, "TOXIC");
            Assert.That((bool)applyInitial.Invoke(null, new object[] { unit }), Is.False);
            Assert.That(unit.GetComponent<MutationAuraView>(), Is.Null);
            Assert.That(unit.transform.Find("GradeLabel").GetComponent<TextMesh>().text, Is.EqualTo("M"));
            Assert.That(unit.transform.Find("MutationLabel"), Is.Null);

            FusionKidnapBoardView.ApplyMutationState(data, 3, "TOXIC");
            Assert.That((bool)applyInitial.Invoke(null, new object[] { unit }), Is.True);
            MutationAuraView aura = unit.GetComponent<MutationAuraView>();
            Assert.That(aura, Is.Not.Null);
            Assert.That(unit.transform.Find("MutationAura").gameObject.activeSelf, Is.True);
            Assert.That(unit.transform.Find("GradeLabel").GetComponent<TextMesh>().text, Is.EqualTo("M"));
            Assert.That(unit.transform.Find("MutationLabel").GetComponent<TextMesh>().text, Is.EqualTo("M:TOX"));

            FusionKidnapBoardView.ApplyMutationState(data, 4, "TOXIC");
            Assert.That((bool)applyInitial.Invoke(null, new object[] { unit }), Is.False);
            Assert.That(unit.transform.Find("MutationAura").gameObject.activeSelf, Is.False);
            Assert.That(unit.transform.Find("GradeLabel").GetComponent<TextMesh>().text, Is.EqualTo("M"));
            TextMesh mutationLabel = unit.transform.Find("MutationLabel").GetComponent<TextMesh>();
            Assert.That(mutationLabel.text, Is.Empty);
            Assert.That(mutationLabel.gameObject.activeSelf, Is.False);
        }
        finally
        {
            Object.DestroyImmediate(unit);
        }
    }

    [Test]
    public void MutationProfiles_HaveUniqueMarkersAndStaticProceduralShapes()
    {
        string[] mutationTypes =
        {
            "GIANT", "BERSERK", "SWIFT", "TOXIC", "GREEDY", "OBESE", "FROZEN", "BLANK"
        };
        string[] expectedMarkers = { "GIA", "BER", "SWI", "TOX", "GRE", "OBE", "FRO", "BLK" };
        var markers = new HashSet<string>();
        var colors = new HashSet<Color>();
        var staticShapes = new HashSet<string>();
        GameObject unit = new GameObject("mutation-profile-test");
        try
        {
            MutationAuraView aura = unit.AddComponent<MutationAuraView>();
            int stableHierarchyCount = -1;
            Renderer[] stableRenderers = null;
            Material[] stableMaterials = null;

            for (int index = 0; index < mutationTypes.Length; index++)
            {
                string mutationType = mutationTypes[index];
                Assert.That(MutationAuraView.ResolveMarker(mutationType), Is.EqualTo(expectedMarkers[index]));
                Assert.That(markers.Add(MutationAuraView.ResolveMarker(mutationType)), Is.True);
                Assert.That(colors.Add(MutationAuraView.ResolveColor(mutationType)), Is.True);

                aura.Apply(mutationType);
                Transform root = unit.transform.Find("MutationAura");
                Assert.That(root, Is.Not.Null);
                Assert.That(root.gameObject.activeSelf, Is.True);
                Assert.That(aura.ActiveMutationType, Is.EqualTo(mutationType));

                Transform activeGroup = root.Find("Profile_" + mutationType);
                Assert.That(activeGroup, Is.Not.Null);
                Assert.That(activeGroup.gameObject.activeSelf, Is.True);
                Assert.That(Enumerable.Range(0, root.childCount)
                    .Count(childIndex => root.GetChild(childIndex).gameObject.activeSelf), Is.EqualTo(1));

                string shape = string.Join("|", Enumerable.Range(0, activeGroup.childCount)
                    .Select(childIndex =>
                    {
                        Transform child = activeGroup.GetChild(childIndex);
                        MeshFilter mesh = child.GetComponent<MeshFilter>();
                        return $"{mesh?.sharedMesh?.name}:{child.localPosition}:{child.localScale}:{child.localEulerAngles}";
                    }));
                Assert.That(staticShapes.Add(shape), Is.True, mutationType + " must have a unique static silhouette.");

                int hierarchyCount = unit.GetComponentsInChildren<Transform>(true).Length;
                if (stableHierarchyCount < 0)
                {
                    stableHierarchyCount = hierarchyCount;
                    stableRenderers = unit.GetComponentsInChildren<Renderer>(true);
                    stableMaterials = stableRenderers.Select(renderer => renderer.sharedMaterial).ToArray();
                }
                Assert.That(hierarchyCount, Is.EqualTo(stableHierarchyCount));
                Assert.That(unit.GetComponentsInChildren<Collider>(true), Is.Empty);
            }

            for (int pass = 0; pass < 4; pass++)
                foreach (string mutationType in mutationTypes)
                    aura.Apply(mutationType);

            Assert.That(unit.GetComponentsInChildren<Transform>(true).Length, Is.EqualTo(stableHierarchyCount));
            Renderer[] repeatedRenderers = unit.GetComponentsInChildren<Renderer>(true);
            Assert.That(repeatedRenderers, Has.Length.EqualTo(stableRenderers.Length));
            for (int index = 0; index < repeatedRenderers.Length; index++)
                Assert.That(repeatedRenderers[index].sharedMaterial, Is.SameAs(stableMaterials[index]));

            aura.Apply(null);
            Assert.That(aura.ActiveMutationType, Is.EqualTo("NONE"));
            Assert.That(unit.transform.Find("MutationAura").gameObject.activeSelf, Is.False);
            Assert.That(unit.GetComponentsInChildren<Transform>(true).Length, Is.EqualTo(stableHierarchyCount));
        }
        finally
        {
            Object.DestroyImmediate(unit);
        }
    }

    [Test]
    public void MutationLabel_IsDedicatedAndClearsWithoutChangingGradeAcrossMoveAndReconnect()
    {
        MethodInfo applyGrade = typeof(FusionKidnapBoardView).GetMethod(
            "ApplyGradeVisual",
            BindingFlags.NonPublic | BindingFlags.Static);
        MethodInfo applyMutation = typeof(FusionKidnapBoardView).GetMethod(
            "ApplyMutationVisual",
            BindingFlags.NonPublic | BindingFlags.Static);
        Assert.That(applyGrade, Is.Not.Null);
        Assert.That(applyMutation, Is.Not.Null);

        GameObject firstTile = new GameObject("first-tile");
        GameObject secondTile = new GameObject("second-tile");
        GameObject unit = new GameObject("mutation-label-test");
        try
        {
            unit.transform.SetParent(firstTile.transform, false);
            applyGrade.Invoke(null, new object[] { unit.transform, (byte)4, 29L });
            applyMutation.Invoke(null, new object[] { unit, "GIANT" });
            int stableHierarchyCount = unit.GetComponentsInChildren<Transform>(true).Length;

            Assert.That(unit.transform.Find("GradeLabel").GetComponent<TextMesh>().text, Is.EqualTo("M"));
            Assert.That(unit.transform.Find("MutationLabel").GetComponent<TextMesh>().text, Is.EqualTo("M:GIA"));

            unit.transform.SetParent(secondTile.transform, false);
            applyMutation.Invoke(null, new object[] { unit, "GREEDY" });
            Assert.That(unit.transform.Find("GradeLabel").GetComponent<TextMesh>().text, Is.EqualTo("M"));
            Assert.That(unit.transform.Find("MutationLabel").GetComponent<TextMesh>().text, Is.EqualTo("M:GRE"));
            Assert.That(unit.GetComponentsInChildren<Transform>(true).Length, Is.EqualTo(stableHierarchyCount));

            applyMutation.Invoke(null, new object[] { unit, null });
            Assert.That(unit.transform.Find("GradeLabel").GetComponent<TextMesh>().text, Is.EqualTo("M"));
            TextMesh mutationLabel = unit.transform.Find("MutationLabel").GetComponent<TextMesh>();
            Assert.That(mutationLabel.text, Is.Empty);
            Assert.That(mutationLabel.gameObject.activeSelf, Is.False);
            Assert.That(unit.transform.Find("MutationAura").gameObject.activeSelf, Is.False);

            applyMutation.Invoke(null, new object[] { unit, "FROZEN" });
            Assert.That(unit.transform.Find("GradeLabel").GetComponent<TextMesh>().text, Is.EqualTo("M"));
            Assert.That(unit.transform.Find("MutationLabel").GetComponent<TextMesh>().text, Is.EqualTo("M:FRO"));
            Assert.That(unit.GetComponentsInChildren<Transform>(true).Length, Is.EqualTo(stableHierarchyCount));
        }
        finally
        {
            Object.DestroyImmediate(unit);
            Object.DestroyImmediate(firstTile);
            Object.DestroyImmediate(secondTile);
        }
    }

    private static bool ApplyAuthoritativeState(
        MethodInfo method,
        UnitData data,
        int playerSlot,
        int slotIndex,
        long alienId,
        byte grade,
        byte mutationState,
        string mutationType)
        => (bool)method.Invoke(null, new object[]
        {
            data, playerSlot, slotIndex, alienId, grade, mutationState, mutationType
        });

    [Test]
    public void HitEventCarriesAuthorityIdentityAndRejectsInvalidDamage()
    {
        DamagePayload payload = new DamagePayload
        {
            BattleSessionId = "session",
            RuntimeProjectileId = 4,
            TargetRuntimeId = 9,
            AttackerId = 17,
            Amount = 20f,
            ActiveMutationType = "FROZEN"
        };
        HitEvent hit = new HitEvent("session", 4, 9, 17, payload, 12);

        Assert.That(hit.Payload.ActiveMutationType, Is.EqualTo("FROZEN"));
        Assert.That(hit.TargetRuntimeId, Is.EqualTo(9));
        Assert.Throws<System.ArgumentException>(() =>
            new HitEvent("session", 4, 9, 17, new DamagePayload { Amount = 0f }, 12));
    }

    [Test]
    public void SupportKillIsRecordedOnceWithoutChangingKillerOrGoldStats()
    {
        var deduplicator = new BattleKillDeduplicator();
        BattleRuntimeMonsterKey key = new BattleRuntimeMonsterKey("session", 1);
        BattleKillAuditRecord record = new BattleKillAuditRecord(
            key, "MONSTER", "killer", "owner", BattleMonsterLanePolicy.EACH_FIELD, 1, 10);

        Assert.That(deduplicator.TryRegister(record), Is.True);
        Assert.That(deduplicator.TryAttachSupport(key, "support"), Is.True);
        Assert.That(deduplicator.TryAttachSupport(key, "support-2"), Is.False);
        Assert.That(deduplicator.Records.Single().SupportPlayerId, Is.EqualTo("support"));
    }

    [Test]
    public void SummaryCountsSupportKillsSeparately()
    {
        var session = new BattleSessionContext("session", "balance", "hash", "battle", "battle-hash", 1);
        BattleRuntimeMonsterKey key = new BattleRuntimeMonsterKey("session", 1);
        BattleKillAuditRecord record = new BattleKillAuditRecord(
            key, "MONSTER", "killer", "owner", BattleMonsterLanePolicy.EACH_FIELD, 1, 10, "support");

        BattleSummary summary = BattleSummaryBuilder.Build(
            session,
            MatchState.CLEARED,
            1,
            new[]
            {
                new BattlePlayerSummarySeed("killer", false, null, 0, 0),
                new BattlePlayerSummarySeed("support", false, null, 0, 0)
            },
            new[] { record });

        Assert.That(summary.Players.Single(player => player.PlayerId == "killer").Kills, Is.EqualTo(1));
        Assert.That(summary.Players.Single(player => player.PlayerId == "support").SupportKills, Is.EqualTo(1));
    }
}
