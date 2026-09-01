using System;
using System.Collections.Generic;
using System.Linq;
#if UNITY_EDITOR
using UnityEditor;
using UnityEngine;
#endif

namespace MyDefenseGame.Editor.FeatureTesting
{
    public enum FeatureTestType
    {
        EditMode,
        PlayMode,
        Scene,
        FusionTwoClient
    }

    [Serializable]
    public sealed class FusionTwoClientLaunchProfile
    {
        public string SessionName { get; }
        public string HostUserId { get; }
        public string ClientUserId { get; }
        public string ScenePath { get; }
        public IReadOnlyDictionary<string, string> EnvironmentOverrides { get; }

        public FusionTwoClientLaunchProfile(
            string sessionName,
            string hostUserId,
            string clientUserId,
            string scenePath,
            IReadOnlyDictionary<string, string> environmentOverrides)
        {
            SessionName = sessionName;
            HostUserId = hostUserId;
            ClientUserId = clientUserId;
            ScenePath = scenePath;
            EnvironmentOverrides = environmentOverrides ?? new Dictionary<string, string>();
        }
    }

    [Serializable]
    public sealed class FeatureTestCase
    {
        public string TaskId { get; }
        public string Title { get; }
        public string Owner { get; }
        public string ScenePath { get; }
        public FeatureTestType TestType { get; }
        public IReadOnlyList<string> Preconditions { get; }
        public IReadOnlyList<string> ResetSteps { get; }
        public IReadOnlyList<string> AutomatedTests { get; }
        public IReadOnlyList<string> HumanChecklist { get; }
        public string ReportPath { get; }
        public FusionTwoClientLaunchProfile LaunchProfile { get; }

        public FeatureTestCase(
            string taskId,
            string title,
            string owner,
            string scenePath,
            FeatureTestType testType,
            IEnumerable<string> preconditions,
            IEnumerable<string> resetSteps,
            IEnumerable<string> automatedTests,
            IEnumerable<string> humanChecklist,
            string reportPath,
            FusionTwoClientLaunchProfile launchProfile = null)
        {
            TaskId = taskId;
            Title = title;
            Owner = owner;
            ScenePath = scenePath;
            TestType = testType;
            Preconditions = Copy(preconditions);
            ResetSteps = Copy(resetSteps);
            AutomatedTests = Copy(automatedTests);
            HumanChecklist = Copy(humanChecklist);
            ReportPath = reportPath;
            LaunchProfile = launchProfile;
        }

        private static IReadOnlyList<string> Copy(IEnumerable<string> values)
            => (values ?? Array.Empty<string>()).Where(value => value != null).ToArray();
    }

    public sealed class FeatureTestCatalog
    {
        public IReadOnlyList<FeatureTestCase> Cases { get; }

        public FeatureTestCatalog(IEnumerable<FeatureTestCase> cases)
        {
            Cases = (cases ?? Array.Empty<FeatureTestCase>()).Where(testCase => testCase != null).ToArray();
        }

        public static FeatureTestCatalog CreateDefault()
        {
            return new FeatureTestCatalog(new[]
            {
                Case("P0-2-5", "Battle Session Adapter", "kinggusi", "Assets/Scenes/Battle.unity", FeatureTestType.FusionTwoClient,
                    "Photon App ID와 동일 Session", "Host/Client 종료 후 Scene 재로드", "BattleSceneSessionAdapterTests;BattleMatchStartCoordinatorTests", "Host/Client Session·필드·공용 Lane", "docs/test-reports/P0-2-5.md"),
                Case("P0-4-6", "탈락 관전 카메라", "kinggusi", "Assets/Scenes/Battle.unity", FeatureTestType.FusionTwoClient,
                    "2인 Battle Session", "Session 종료 및 Battle Scene 재로드", "BattleSpectatorCameraControllerTests", "탈락 후 상대 필드 관전·입력 차단·복귀", "docs/test-reports/P0-4-6.md"),
                Case("P0-5-1", "Networked in-game Gold", "jjangash", "Assets/Scenes/Battle.unity", FeatureTestType.FusionTwoClient,
                    "양쪽 테스트 계정 Gold 초기화", "Session 종료 및 Gold Fixture 초기화", "BattleWaveStateAuthorityTests", "양쪽 지갑 독립 차감·복제", "docs/test-reports/P0-5-1.md"),
                Case("P0-6", "Boss Timer·결과 전환", "jjangash", "Assets/Scenes/Battle.unity", FeatureTestType.FusionTwoClient,
                    "Boss Wave 진입 가능 상태", "Session 종료 및 Wave Fixture 초기화", "BattleWaveExecutorStateTests;BattleWaveStateAuthorityTests", "Boss 처치·시간초과·FAILED/CLEARED", "docs/test-reports/P0-6.md"),
                Case("P0-7", "Legendary Merge·Mythic 선택", "jjangash", "Assets/Scenes/Battle.unity", FeatureTestType.FusionTwoClient,
                    "Legendary 2개와 Mythic Pool", "보드 초기화 및 결과 상태 리셋", "BattleWaveStateAuthorityTests", "후보 3종·리롤·최종 선택·DNA 계승", "docs/test-reports/P0-7.md"),
                Case("P0-8", "Damage·Projectile·Kill", "kinggusi", "Assets/Scenes/Battle.unity", FeatureTestType.FusionTwoClient,
                    "Monster와 Projectile Prefab", "Session 종료 및 Combat Fixture 초기화", "BattleDamageContractTests;BattleProjectilePrefabTests;BattleProjectileSpawnTests", "권위 충돌·Damage·Kill/Support Kill", "docs/test-reports/P0-8.md"),
                Case("P0-9", "재접속·Snapshot 복구", "jjangash", "Assets/Scenes/Battle.unity", FeatureTestType.FusionTwoClient,
                    "재접속 가능한 2인 Session", "Session 종료 및 Snapshot Fixture 초기화", "BattleSceneSessionAdapterTests", "연결 종료·재접속·Board·Boss Timer 복구", "docs/test-reports/P0-9.md"),
                Case("P0-10", "Settlement Summary·전송", "jjangash", "Assets/Scenes/Battle.unity", FeatureTestType.FusionTwoClient,
                    "Spring Boot와 canonical manifest", "Session 종료 및 Settlement 테스트 데이터 정리", "BattleSettlementSummaryBuilderTests;BattleSettlementCoordinatorTests", "종료 Summary·HTTP 응답·재시도", "docs/test-reports/P0-10.md"),
                Case("P0-UI-LEGACY", "기존 단일 플레이 보드", "kinggusi", "Assets/Scenes/Tests/TestGameScene.unity", FeatureTestType.Scene,
                    "Legacy 테스트 Fixture", "Scene 종료 및 보드 상태 초기화", "", "드래그·빈 슬롯 이동·동일 유닛 머지", "docs/test-reports/P0-11-1.md"),
                Case("P0-NET-CONNECT", "Fusion 접속 Smoke Test", "jjangash", "Assets/Scenes/Tests/FusionConnectionTest.unity", FeatureTestType.FusionTwoClient,
                    "Photon App ID와 Host/Client 실행 파일", "두 프로세스 종료", "BattleRunnerLifecycleTests", "동일 Session Host/Client 입장", "docs/test-reports/P0-11-1.md"),
                Case("P1-1", "Mythic Mutation 활성화·재변이", "jjangash", "Assets/Scenes/Battle.unity", FeatureTestType.FusionTwoClient,
                    "순수·해금 Mythic과 충분한 개인 Gold", "Session 종료 및 보드 초기화", "BattleWaveStateAuthorityTests", "300G 활성화·단계별 재변이·현재 Mutation 제외·Injector 교체", "docs/P1_INTEGRATION_TEST_SCENARIO.md"),
                Case("P1-2", "Mutation 전투 효과", "kinggusi", "Assets/Scenes/Battle.unity", FeatureTestType.FusionTwoClient,
                    "8종 Mutation Mythic과 Monster/Boss", "Session 종료 및 Combat 상태 초기화", "BattleDamageContractTests", "광역·Boss·DOT·Slow·경제·도박·BLANK 효과", "docs/P1_INTEGRATION_TEST_SCENARIO.md"),
                Case("P1-3", "일반·신화 공명", "jjangash", "Assets/Scenes/Battle.unity", FeatureTestType.FusionTwoClient,
                    "Normal~Legendary와 Mythic, 충분한 개인 Gold", "Session 종료하여 공명 레벨 초기화", "BattleResonanceCalculatorTests;BattleWaveStateAuthorityTests", "등급별 공명 비용·공격 Snapshot 반영·재접속·전투 종료 초기화", "docs/P1_INTEGRATION_TEST_SCENARIO.md"),
                Case("P1-4", "행성·80 Wave·Boss", "kinggusi", "Assets/Scenes/Battle.unity", FeatureTestType.FusionTwoClient,
                    "canonical Planet/Wave/Spawn Balance", "Session 종료 및 Wave 초기화", "BattleCanonicalBalanceTests;BattleWaveExecutorStateTests", "9행성 배율·10 Wave 간격 Boss·BOSS_SHARED·SUN 난이도", "docs/P1_INTEGRATION_TEST_SCENARIO.md"),
                Case("P1-5", "Battle Settlement 보상 E2E", "jjangash", "Assets/Scenes/Battle.unity", FeatureTestType.FusionTwoClient,
                    "Spring Boot와 canonical manifest, 2인 종료 Summary", "Session 종료 및 Settlement 테스트 데이터 정리", "BattleSettlementCoordinatorTests;BattleSettlementEndToEndIntegrationTest", "승리·패배·관전·이탈 자격·멱등 보상", "docs/P1_INTEGRATION_TEST_SCENARIO.md"),
                Case("P2-1-2", "행성별 Planet Content", "kinggusi", "Assets/Scenes/Battle.unity", FeatureTestType.FusionTwoClient,
                    "canonical 9행성 Profile Catalog와 동일 mapId 2인 Session", "Session 종료 및 PlanetContentApplicator Clear", "PlanetContentCatalogTests;PlanetContentApplicatorTests;BattleWaveStateAuthorityTests", "Host/Client 동일 환경·공통 Board/Lane/Waypoint/Boss 유지", "docs/test-reports/P2-1-2.md")
            });
        }

        private static FeatureTestCase Case(string taskId, string title, string owner, string scenePath, FeatureTestType testType,
            string precondition, string reset, string automated, string human, string report)
            => new FeatureTestCase(taskId, title, owner, scenePath, testType,
                new[] { precondition }, new[] { reset },
                string.IsNullOrWhiteSpace(automated) ? Array.Empty<string>() : automated.Split(';'),
                new[] { human }, report,
                testType == FeatureTestType.FusionTwoClient
                    ? new FusionTwoClientLaunchProfile(
                        "MyDefense-Dev",
                        "dev-host",
                        "dev-client",
                        scenePath,
                        new Dictionary<string, string>(StringComparer.OrdinalIgnoreCase)
                        {
                            ["MYDEFENSE_FUSION_SESSION"] = "MyDefense-Dev",
                            ["MYDEFENSE_FUSION_ROLE"] = "host/client",
                            ["MYDEFENSE_FUSION_USER_ID"] = "dev-host/dev-client"
                        })
                    : null);
    }

    public static class FeatureTestCatalogValidator
    {
        public static IReadOnlyList<string> Validate(
            IEnumerable<FeatureTestCase> cases,
            Func<string, bool> sceneExists,
            IEnumerable<string> productionScenePaths)
        {
            var errors = new List<string>();
            var list = (cases ?? Array.Empty<FeatureTestCase>()).ToArray();
            var production = new HashSet<string>(productionScenePaths ?? Array.Empty<string>(), StringComparer.OrdinalIgnoreCase);

            if (list.Length == 0)
                errors.Add("Catalog must contain at least one test case.");

            foreach (var group in list.GroupBy(testCase => testCase.TaskId ?? string.Empty, StringComparer.OrdinalIgnoreCase))
            {
                if (string.IsNullOrWhiteSpace(group.Key))
                    errors.Add("Task ID must not be blank.");
                if (group.Count() > 1)
                    errors.Add($"Duplicate Task ID: {group.Key}");
            }

            foreach (var testCase in list)
            {
                if (string.IsNullOrWhiteSpace(testCase.Title) || string.IsNullOrWhiteSpace(testCase.Owner))
                    errors.Add($"{testCase.TaskId}: title and owner are required.");
                if (string.IsNullOrWhiteSpace(testCase.ScenePath))
                    errors.Add($"{testCase.TaskId}: scene path is required.");
                else
                {
                    if (sceneExists != null && !sceneExists(testCase.ScenePath))
                        errors.Add($"{testCase.TaskId}: missing Scene {testCase.ScenePath}");
                    if (production.Contains(testCase.ScenePath) && testCase.TestType != FeatureTestType.FusionTwoClient)
                        errors.Add($"{testCase.TaskId}: test Scene must not be in Production Build Settings: {testCase.ScenePath}");
                }
                if (testCase.Preconditions.Count == 0 || testCase.ResetSteps.Count == 0 || testCase.HumanChecklist.Count == 0)
                    errors.Add($"{testCase.TaskId}: preconditions, reset steps, and human checklist are required.");
                if (string.IsNullOrWhiteSpace(testCase.ReportPath))
                    errors.Add($"{testCase.TaskId}: report path is required.");
                if (testCase.TestType == FeatureTestType.FusionTwoClient)
                {
                    var launch = testCase.LaunchProfile;
                    if (launch == null)
                        errors.Add($"{testCase.TaskId}: FusionTwoClient launch profile is required.");
                    else
                    {
                        if (string.IsNullOrWhiteSpace(launch.SessionName)
                            || string.IsNullOrWhiteSpace(launch.HostUserId)
                            || string.IsNullOrWhiteSpace(launch.ClientUserId))
                            errors.Add($"{testCase.TaskId}: FusionTwoClient session and both user IDs are required.");
                        if (!string.Equals(launch.ScenePath, testCase.ScenePath, StringComparison.OrdinalIgnoreCase))
                            errors.Add($"{testCase.TaskId}: launch scene must match catalog scene.");
                        if (launch.EnvironmentOverrides == null
                            || !launch.EnvironmentOverrides.ContainsKey("MYDEFENSE_FUSION_SESSION")
                            || !launch.EnvironmentOverrides.ContainsKey("MYDEFENSE_FUSION_ROLE")
                            || !launch.EnvironmentOverrides.ContainsKey("MYDEFENSE_FUSION_USER_ID"))
                            errors.Add($"{testCase.TaskId}: Fusion environment override keys are incomplete.");
                    }
                }
            }

            return errors;
        }

#if UNITY_EDITOR
        public static IReadOnlyList<string> ValidateDefaultForEditor()
        {
            var buildPaths = EditorBuildSettings.scenes.Where(scene => scene.enabled).Select(scene => scene.path);
            return Validate(FeatureTestCatalog.CreateDefault().Cases,
                path => AssetDatabase.LoadAssetAtPath<SceneAsset>(path) != null,
                buildPaths);
        }
#endif
    }
}
