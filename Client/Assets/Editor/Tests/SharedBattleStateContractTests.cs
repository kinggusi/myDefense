using System;
using System.IO;
using MyDefense.Shared.Contracts;
using NUnit.Framework;

namespace MyDefense.Shared.Tests
{
    public sealed class SharedBattleStateContractTests
    {
        [Test]
        public void PlayerBattleState_HasStableNamesAndNumericValues()
        {
            Assert.That(
                Enum.GetNames(typeof(PlayerBattleState)),
                Is.EqualTo(new[] { "ACTIVE", "ELIMINATED", "SPECTATING" }));
            Assert.That((int)PlayerBattleState.ACTIVE, Is.Zero);
            Assert.That((int)PlayerBattleState.ELIMINATED, Is.EqualTo(1));
            Assert.That((int)PlayerBattleState.SPECTATING, Is.EqualTo(2));
        }

        [Test]
        public void MatchState_HasStableNamesAndNumericValues()
        {
            Assert.That(
                Enum.GetNames(typeof(MatchState)),
                Is.EqualTo(new[] { "RUNNING", "CLEARED", "FAILED" }));
            Assert.That((int)MatchState.RUNNING, Is.Zero);
            Assert.That((int)MatchState.CLEARED, Is.EqualTo(1));
            Assert.That((int)MatchState.FAILED, Is.EqualTo(2));
        }

        [Test]
        public void PlayerConnectionState_HasStableNamesAndNumericValues()
        {
            Assert.That(
                Enum.GetNames(typeof(PlayerConnectionState)),
                Is.EqualTo(new[] { "CONNECTED", "DISCONNECTED" }));
            Assert.That((int)PlayerConnectionState.CONNECTED, Is.Zero);
            Assert.That((int)PlayerConnectionState.DISCONNECTED, Is.EqualTo(1));
        }

        [TestCase(typeof(PlayerBattleState), "ACTIVE", 0)]
        [TestCase(typeof(PlayerBattleState), "ELIMINATED", 1)]
        [TestCase(typeof(PlayerBattleState), "SPECTATING", 2)]
        [TestCase(typeof(MatchState), "RUNNING", 0)]
        [TestCase(typeof(MatchState), "CLEARED", 1)]
        [TestCase(typeof(MatchState), "FAILED", 2)]
        [TestCase(typeof(PlayerConnectionState), "CONNECTED", 0)]
        [TestCase(typeof(PlayerConnectionState), "DISCONNECTED", 1)]
        public void EnumContract_RoundTripsNameAndNumericValue(Type enumType, string name, int numericValue)
        {
            object parsed = Enum.Parse(enumType, name);

            Assert.That(Convert.ToInt32(parsed), Is.EqualTo(numericValue));
            Assert.That(Enum.GetName(enumType, parsed), Is.EqualTo(name));
        }

        [TestCase(typeof(PlayerBattleState), "CONNECTED")]
        [TestCase(typeof(MatchState), "ABORTED")]
        [TestCase(typeof(PlayerConnectionState), "ABANDONED")]
        public void EnumContract_RejectsUndefinedNames(Type enumType, string undefinedName)
        {
            Assert.That(Enum.IsDefined(enumType, undefinedName), Is.False);
            Assert.Throws<ArgumentException>(() => Enum.Parse(enumType, undefinedName));
        }

        [Test]
        public void BattleSessionSnapshot_ContainsResumeStateContract()
        {
            var snapshot = new BattleSessionSnapshot
            {
                battleSessionId = "session-1",
                mapId = "EARTH",
                balanceVersion = "balance-v1",
                contentHash = "content-hash",
                matchState = MatchState.RUNNING,
                currentWave = 12,
                currentWaveSpecId = "WAVE_12",
                waveType = "REGULAR",
                wavePhase = "SPAWNING",
                waveTimeRemainingSeconds = 8,
                bossTimeRemainingSeconds = 0,
                capturedAtTick = 900,
                players = new[]
                {
                    new BattleSessionPlayerSnapshot
                    {
                        playerId = "player-1",
                        playerSlot = 1,
                        battleState = PlayerBattleState.ACTIVE,
                        connectionState = PlayerConnectionState.CONNECTED,
                        inGameGold = 60,
                        currentKidnapCost = 30,
                        normalResonanceLevel = 3,
                        mythicResonanceLevel = 1,
                        eliminatedWave = null
                    },
                    new BattleSessionPlayerSnapshot
                    {
                        playerId = "player-2",
                        playerSlot = 2,
                        battleState = PlayerBattleState.ELIMINATED,
                        connectionState = PlayerConnectionState.DISCONNECTED,
                        inGameGold = 60,
                        currentKidnapCost = 30,
                        normalResonanceLevel = 2,
                        mythicResonanceLevel = 4,
                        eliminatedWave = 11
                    }
                },
                boardObjects = new[]
                {
                    new BattleBoardObjectSnapshot
                    {
                        objectId = 7,
                        ownerPlayerSlot = 1,
                        objectType = BattleBoardObjectType.ALIEN,
                        gridX = 2,
                        gridY = 3,
                        alienSpecId = 22,
                        grade = "MYTHIC",
                        pendingMutationType = "NONE",
                        activeMutationType = "NONE",
                        mutationRerollCount = 0,
                        mutationType = "DNA_A",
                        mutationState = BattleMutationState.SEALED
                    }
                },
                mythicChoices = new[]
                {
                    new BattleMythicChoiceSnapshot
                    {
                        playerSlot = 1,
                        targetBoardSlot = 7,
                        candidateAlienIds = new[] { 29L, 30L, 31L },
                        freeRerollsRemaining = 1,
                        paidRerollsRemaining = 1,
                        remainingSeconds = 10
                    }
                },
                monsters = new[]
                {
                    new BattleMonsterStateSnapshot
                    {
                        runtimeMonsterId = 1001,
                        monsterId = "MONSTER_NORMAL_DEFAULT",
                        lanePolicy = "EACH_FIELD",
                        fieldOwnerPlayerId = "player-1",
                        spawnWave = 12,
                        currentHp = 50,
                        maxHp = 100,
                        dead = false,
                        x = 1,
                        y = 2,
                        z = 3
                    }
                }
            };

            Assert.That(snapshot.schemaVersion, Is.EqualTo(BattleSessionSnapshot.CurrentSchemaVersion));
            Assert.That(snapshot.players, Has.Length.EqualTo(2));
            Assert.That(snapshot.players[1].eliminatedWave, Is.EqualTo(11));
            Assert.That(snapshot.boardObjects[0].ownerPlayerSlot, Is.EqualTo(1));
            Assert.That(snapshot.boardObjects[0].alienSpecId, Is.EqualTo(22));
            Assert.DoesNotThrow(() => BattleSessionSnapshotValidator.Validate(snapshot));
            var json = BattleSessionSnapshotJson.Serialize(snapshot);
            StringAssert.Contains("\"eliminatedWave\":null", json);
            StringAssert.Contains("\"mapId\":\"EARTH\"", json);
            StringAssert.Contains("\"normalResonanceLevel\":3", json);
            StringAssert.Contains("\"mythicResonanceLevel\":4", json);
            StringAssert.Contains("\"currentWaveSpecId\":\"WAVE_12\"", json);
            StringAssert.Contains("\"alienSpecId\":22", json);
            StringAssert.Contains("\"mutationState\":\"SEALED\"", json);
            StringAssert.Contains("\"candidateAlienIds\":[29,30,31]", json);
            StringAssert.Contains("\"runtimeMonsterId\":1001", json);
        }

        [Test]
        public void BattleSessionSnapshot_V3CanonicalJsonMatchesSharedFixture()
        {
            var snapshot = new BattleSessionSnapshot
            {
                battleSessionId = "session-v3",
                mapId = "EARTH",
                balanceVersion = "balance-v3",
                contentHash = "hash-v3",
                matchState = MatchState.RUNNING,
                currentWave = 1,
                currentWaveSpecId = "WAVE_001",
                waveType = "REGULAR",
                wavePhase = "ACTIVE",
                capturedAtTick = 42,
                players = new[]
                {
                    new BattleSessionPlayerSnapshot { playerId = "p1", playerSlot = 1, battleState = PlayerBattleState.ACTIVE, connectionState = PlayerConnectionState.CONNECTED, inGameGold = 100, currentKidnapCost = 20 },
                    new BattleSessionPlayerSnapshot { playerId = "p2", playerSlot = 2, battleState = PlayerBattleState.ACTIVE, connectionState = PlayerConnectionState.CONNECTED, inGameGold = 100, currentKidnapCost = 20 }
                },
                boardObjects = Array.Empty<BattleBoardObjectSnapshot>(),
                mythicChoices = Array.Empty<BattleMythicChoiceSnapshot>(),
                monsters = Array.Empty<BattleMonsterStateSnapshot>()
            };
            string fixturePath = Path.GetFullPath(Path.Combine(UnityEngine.Application.dataPath,
                "../../contracts/battle-session-snapshot-v3.json"));

            Assert.That(BattleSessionSnapshotJson.Serialize(snapshot),
                Is.EqualTo(File.ReadAllText(fixturePath).Trim()));
        }

        [Test]
        public void BattleSessionSnapshot_RejectsOutOfRangeResonanceLevel()
        {
            var snapshot = CreateMinimalSnapshot();
            snapshot.players[0].normalResonanceLevel = 6;
            Assert.Throws<ArgumentException>(() => BattleSessionSnapshotValidator.Validate(snapshot));

            snapshot = CreateMinimalSnapshot();
            snapshot.players[1].mythicResonanceLevel = -1;
            Assert.Throws<ArgumentException>(() => BattleSessionSnapshotValidator.Validate(snapshot));
        }

        [Test]
        public void BattleSessionSnapshot_V3RejectsMissingAuthoritativeMapId()
        {
            var snapshot = CreateMinimalSnapshot();
            snapshot.mapId = null;
            Assert.Throws<ArgumentException>(() => BattleSessionSnapshotValidator.Validate(snapshot));

            snapshot.mapId = "   ";
            Assert.Throws<ArgumentException>(() => BattleSessionSnapshotValidator.Validate(snapshot));
        }

        [Test]
        public void BattleSessionSnapshot_RejectsDuplicateMonsterIdsAndInvalidChoiceCandidates()
        {
            var snapshot = CreateMinimalSnapshot();
            snapshot.monsters = new[]
            {
                new BattleMonsterStateSnapshot { runtimeMonsterId = 7, monsterId = "MON", lanePolicy = "EACH_FIELD", spawnWave = 1, currentHp = 1, maxHp = 1 },
                new BattleMonsterStateSnapshot { runtimeMonsterId = 7, monsterId = "MON", lanePolicy = "EACH_FIELD", spawnWave = 1, currentHp = 1, maxHp = 1 }
            };
            Assert.Throws<ArgumentException>(() => BattleSessionSnapshotValidator.Validate(snapshot));

            snapshot = CreateMinimalSnapshot();
            snapshot.mythicChoices = new[]
            {
                new BattleMythicChoiceSnapshot { playerSlot = 1, targetBoardSlot = 0, candidateAlienIds = new[] { 29L, 29L, 31L }, remainingSeconds = 1 }
            };
            Assert.Throws<ArgumentException>(() => BattleSessionSnapshotValidator.Validate(snapshot));
        }

        private static BattleSessionSnapshot CreateMinimalSnapshot()
        {
            return new BattleSessionSnapshot
            {
                battleSessionId = "session",
                mapId = "NEPTUNE",
                balanceVersion = "balance",
                contentHash = "hash",
                matchState = MatchState.RUNNING,
                currentWave = 1,
                currentWaveSpecId = "WAVE_001",
                waveType = "REGULAR",
                wavePhase = "ACTIVE",
                players = new[]
                {
                    new BattleSessionPlayerSnapshot { playerId = "p1", playerSlot = 1, battleState = PlayerBattleState.ACTIVE, connectionState = PlayerConnectionState.CONNECTED },
                    new BattleSessionPlayerSnapshot { playerId = "p2", playerSlot = 2, battleState = PlayerBattleState.ACTIVE, connectionState = PlayerConnectionState.CONNECTED }
                },
                boardObjects = Array.Empty<BattleBoardObjectSnapshot>(),
                mythicChoices = Array.Empty<BattleMythicChoiceSnapshot>(),
                monsters = Array.Empty<BattleMonsterStateSnapshot>()
            };
        }

        [Test]
        public void LegendaryChoiceState_ValidatesMaterialsCandidatesAndSelection()
        {
            var state = new LegendaryChoiceState
            {
                choiceId = "choice-1",
                battleSessionId = "session-1",
                materialAlienIdA = 101,
                materialAlienIdB = 102,
                candidateAlienIds = new[] { 201L, 202L, 203L },
                selectionTimeoutSeconds = 8,
                remainingSeconds = 8,
                phase = LegendaryChoicePhase.OPEN,
                autoSelectPolicy = "FIRST",
                battleContinuesDuringSelection = true
            };

            Assert.DoesNotThrow(() => LegendaryChoiceStateValidator.Validate(state));
            state.phase = LegendaryChoicePhase.SELECTED;
            state.selectedAlienId = 202;
            Assert.DoesNotThrow(() => LegendaryChoiceStateValidator.Validate(state));
            state.candidateAlienIds[2] = 202;
            Assert.Throws<ArgumentException>(() => LegendaryChoiceStateValidator.Validate(state));
        }

        [Test]
        public void LegendaryChoiceStateJson_UsesStringPhaseAndNullableSelection()
        {
            var state = new LegendaryChoiceState
            {
                choiceId = "choice-1",
                battleSessionId = "session-1",
                materialAlienIdA = 101,
                materialAlienIdB = 102,
                candidateAlienIds = new[] { 201L, 202L, 203L },
                selectionTimeoutSeconds = 8,
                remainingSeconds = 8,
                phase = LegendaryChoicePhase.OPEN,
                autoSelectPolicy = "FIRST",
                battleContinuesDuringSelection = true
            };

            var json = LegendaryChoiceStateJson.Serialize(state);

            StringAssert.Contains("\"phase\":\"OPEN\"", json);
            StringAssert.Contains("\"selectedAlienId\":null", json);
            StringAssert.Contains("\"candidateAlienIds\":[201,202,203]", json);

            state.autoSelectPolicy = "FIRST\nSAFE";
            json = LegendaryChoiceStateJson.Serialize(state);
            StringAssert.Contains("FIRST\\nSAFE", json);
        }
    }
}
