using System;
using MyDefense.Battle.Runtime;
using NUnit.Framework;

namespace MyDefense.Battle.Tests
{
    public sealed class BattleRunnerLifecycleTests
    {
        [Test]
        public void LifecycleStartsStopped()
        {
            var gameObject = new UnityEngine.GameObject("runner-test");
            try
            {
                var lifecycle = gameObject.AddComponent<BattleRunnerLifecycle>();
                Assert.That(lifecycle.State, Is.EqualTo(BattleRunnerLifecycleState.STOPPED));
                Assert.That(lifecycle.Runner, Is.Null);
            }
            finally
            {
                UnityEngine.Object.DestroyImmediate(gameObject);
            }
        }

        [Test]
        public void EmptySerializedMapUsesCanonicalFirstPlanet()
        {
            var gameObject = new UnityEngine.GameObject("runner-map-test");
            try
            {
                var lifecycle = gameObject.AddComponent<BattleRunnerLifecycle>();
                Assert.That(lifecycle.MapId, Is.EqualTo(BattleRunnerLifecycle.DefaultMapId));
                Assert.That(lifecycle.MapId, Is.EqualTo("NEPTUNE"));
            }
            finally
            {
                UnityEngine.Object.DestroyImmediate(gameObject);
            }
        }

        [Test]
        public void EmptySessionNameIsRejectedBeforeRunnerCreation()
        {
            var gameObject = new UnityEngine.GameObject("runner-test");
            try
            {
                var lifecycle = gameObject.AddComponent<BattleRunnerLifecycle>();
                Assert.ThrowsAsync<ArgumentException>(() => lifecycle.StartHostAsync(" "));
                Assert.That(lifecycle.State, Is.EqualTo(BattleRunnerLifecycleState.STOPPED));
                Assert.That(lifecycle.Runner, Is.Null);
            }
            finally
            {
                UnityEngine.Object.DestroyImmediate(gameObject);
            }
        }
    }
}
