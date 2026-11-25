using MyDefense.Battle.Presentation;
using NUnit.Framework;
using UnityEngine;

namespace MyDefenseGame.Editor.Tests
{
    public sealed class BattlePresentationQualityTests
    {
        [Test]
        public void TwoBattleOwnersAndCameraReplacementRestoreOriginalRuntimeQualityExactlyOnce()
        {
            int previous=QualitySettings.antiAliasing;
            var a=new GameObject("Quality A").AddComponent<BattlePresentationQuality>();var b=new GameObject("Quality B").AddComponent<BattlePresentationQuality>();
            var first=new GameObject("First camera").AddComponent<Camera>();var second=new GameObject("Second camera").AddComponent<Camera>();
            first.renderingPath=second.renderingPath=RenderingPath.Forward;first.allowMSAA=false;second.allowMSAA=false;
            try
            {
                QualitySettings.antiAliasing=2;
                a.BeginScope(first);a.BeginScope(first);b.BeginScope(first);
                Assert.That(QualitySettings.antiAliasing,Is.EqualTo(4));Assert.That(first.allowMSAA,Is.True);
                a.EndScope();Assert.That(QualitySettings.antiAliasing,Is.EqualTo(4));Assert.That(first.allowMSAA,Is.True);
                Object.DestroyImmediate(first.gameObject);b.BeginScope(second);Assert.That(second.allowMSAA,Is.True);
                b.EndScope();b.EndScope();Assert.That(QualitySettings.antiAliasing,Is.EqualTo(2));Assert.That(second.allowMSAA,Is.False);
            }
            finally{a.EndScope();b.EndScope();if(first!=null)Object.DestroyImmediate(first.gameObject);Object.DestroyImmediate(second.gameObject);Object.DestroyImmediate(a.gameObject);Object.DestroyImmediate(b.gameObject);QualitySettings.antiAliasing=previous;}
        }
    }
}
