using System.Linq;
using NUnit.Framework;
using MyDefenseGame.Editor.FeatureTesting;
#if UNITY_EDITOR
using UnityEditor;
#endif

namespace MyDefenseGame.Editor.Tests
{
    public sealed class FeatureTestCatalogTests
    {
        [Test]
        public void DefaultCatalogHasUniqueTaskIdsAndCompleteMetadata()
        {
            var catalog = FeatureTestCatalog.CreateDefault();
            var errors = FeatureTestCatalogValidator.Validate(catalog.Cases, _ => true, new string[0]);

            Assert.That(errors, Is.Empty, string.Join("\n", errors));
            Assert.That(catalog.Cases.Select(testCase => testCase.TaskId).Distinct().Count(), Is.EqualTo(catalog.Cases.Count));
        }

        [Test]
        public void FusionCasesExposeDeterministicTwoClientLaunchProfile()
        {
            var fusionCases = FeatureTestCatalog.CreateDefault().Cases
                .Where(testCase => testCase.TestType == FeatureTestType.FusionTwoClient)
                .ToArray();

            Assert.That(fusionCases, Is.Not.Empty);
            Assert.That(fusionCases.All(testCase => testCase.LaunchProfile != null), Is.True);
            Assert.That(fusionCases.Select(testCase => testCase.LaunchProfile.SessionName).Distinct().Single(), Is.EqualTo("MyDefense-Dev"));
            Assert.That(fusionCases.All(testCase => testCase.LaunchProfile.HostUserId == "dev-host"), Is.True);
            Assert.That(fusionCases.All(testCase => testCase.LaunchProfile.ClientUserId == "dev-client"), Is.True);
            Assert.That(fusionCases.All(testCase => testCase.LaunchProfile.EnvironmentOverrides.ContainsKey("MYDEFENSE_FUSION_ROLE")), Is.True);
        }

        [Test]
        public void FusionCaseWithoutLaunchProfileIsRejected()
        {
            var testCase = new FeatureTestCase("P0-X", "A", "jjangash", "Assets/Scenes/Battle.unity", FeatureTestType.FusionTwoClient,
                new[] { "precondition" }, new[] { "reset" }, new[] { "test" }, new[] { "check" }, "docs/test-reports/P0-X.md");

            var errors = FeatureTestCatalogValidator.Validate(new[] { testCase }, _ => true, new string[0]);

            Assert.That(errors, Does.Contain("P0-X: FusionTwoClient launch profile is required."));
        }

#if UNITY_EDITOR
        [Test]
        public void DefaultCatalogMatchesEditorAssetsAndBuildPolicy()
        {
            var errors = FeatureTestCatalogValidator.ValidateDefaultForEditor();

            Assert.That(errors, Is.Empty, string.Join("\n", errors));
            Assert.That(EditorBuildSettings.scenes.Any(scene => scene.enabled && scene.path == "Assets/Scenes/Battle.unity"), Is.True);
        }

        [Test]
        public void LegacyTestScenesAreCatalogedButExcludedFromProductionBuild()
        {
            var catalog = FeatureTestCatalog.CreateDefault();
            var testGameScene = catalog.Cases.Single(testCase => testCase.ScenePath == "Assets/Scenes/Tests/TestGameScene.unity");
            var connectionScene = catalog.Cases.Single(testCase => testCase.ScenePath == "Assets/Scenes/Tests/FusionConnectionTest.unity");

            Assert.That(testGameScene.TestType, Is.EqualTo(FeatureTestType.Scene));
            Assert.That(connectionScene.TestType, Is.EqualTo(FeatureTestType.FusionTwoClient));
            Assert.That(EditorBuildSettings.scenes.Any(scene => scene.enabled && scene.path == testGameScene.ScenePath), Is.False);
            Assert.That(EditorBuildSettings.scenes.Any(scene => scene.enabled && scene.path == connectionScene.ScenePath), Is.False);
        }
#endif

        [Test]
        public void DuplicateTaskIdIsRejected()
        {
            var cases = new[]
            {
                new FeatureTestCase("P0-X", "A", "jjangash", "Assets/Scenes/Battle.unity", FeatureTestType.Scene,
                    new[] { "precondition" }, new[] { "reset" }, new[] { "test" }, new[] { "check" }, "docs/test-reports/P0-X.md"),
                new FeatureTestCase("P0-X", "B", "kinggusi", "Assets/Scenes/Battle.unity", FeatureTestType.Scene,
                    new[] { "precondition" }, new[] { "reset" }, new[] { "test" }, new[] { "check" }, "docs/test-reports/P0-X.md")
            };

            var errors = FeatureTestCatalogValidator.Validate(cases, _ => true, new string[0]);

            Assert.That(errors, Does.Contain("Duplicate Task ID: P0-X"));
        }

        [Test]
        public void MissingSceneAndProductionSceneAreRejected()
        {
            var testCase = new FeatureTestCase("P0-X", "A", "jjangash", "Assets/Scenes/Missing.unity", FeatureTestType.Scene,
                new[] { "precondition" }, new[] { "reset" }, new[] { "test" }, new[] { "check" }, "docs/test-reports/P0-X.md");

            var errors = FeatureTestCatalogValidator.Validate(new[] { testCase }, _ => false, new[] { "Assets/Scenes/Missing.unity" });

            Assert.That(errors, Does.Contain("P0-X: missing Scene Assets/Scenes/Missing.unity"));
            Assert.That(errors, Does.Contain("P0-X: test Scene must not be in Production Build Settings: Assets/Scenes/Missing.unity"));
        }
    }
}
