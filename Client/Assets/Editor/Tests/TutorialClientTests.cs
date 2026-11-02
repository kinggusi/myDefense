using NUnit.Framework;
using UnityEngine;
using UnityEngine.UI;
using MyDefense.Tutorial;

public class TutorialClientTests
{
    [TestCase("NOT_STARTED","START")]
    [TestCase("COLLECTION_GUIDE","COLLECTION_SEEN")]
    [TestCase("UPGRADE_GUIDE","UPGRADE_SEEN")]
    [TestCase("REWARD_READY","CLAIM")]
    [TestCase("BREEDING_GUIDE","BREEDING_SEEN")]
    public void GuidanceMapsToExplicitOperation(string step,string operation) { Assert.AreEqual(operation,TutorialPresentation.Operation(step)); }
    [TestCase("PRACTICE_PENDING")][TestCase("COMPLETED")][TestCase("UNKNOWN")]
    public void PracticeAndUnknownHaveNoCompletionShortcut(string step) { Assert.IsNull(TutorialPresentation.Operation(step)); }
    [Test] public void DefaultLauncherIsFailClosed() {var launcher=new UnavailableTutorialPracticeLauncher();Assert.IsFalse(launcher.Available);bool returned=false,failed=false;launcher.Launch(1,"run",()=>returned=true,_=>failed=true);Assert.IsFalse(returned);Assert.IsTrue(failed);}
    [Test] public void UpgradeGuideClearlyConsumesNothing() {StringAssert.Contains("소비하지",TutorialPresentation.Message("UPGRADE_GUIDE"));}
    [Test] public void CommandDoesNotContainUserOrCompletionClaim() {var json=JsonUtility.ToJson(new TutorialCommand{requestId="id",expectedRevision=3,operation="VERIFY_PRACTICE",practiceRunId="run"});StringAssert.DoesNotContain("completed",json);StringAssert.DoesNotContain("userId",json);StringAssert.Contains("expectedRevision",json);}
    [Test] public void InvalidAccountGuardRejectsUnboundSession() {Assert.IsFalse(TutorialController.SameAccount(null,1,0,"http://127.0.0.1:8080/api"));}
    [TestCase(false,1,1,1,1,"a","a")]
    [TestCase(true,1,2,1,1,"a","a")]
    [TestCase(true,1,1,1,2,"a","a")]
    [TestCase(true,1,1,1,1,"a","b")]
    public void StaleAccountGenerationEndpointAreRejected(bool same,long expected,long actual,int expectedGen,int actualGen,string expectedUrl,string actualUrl)
    {Assert.IsFalse(TutorialPresentation.AccountMatches(same,expected,actual,expectedGen,actualGen,expectedUrl,actualUrl));}
    [Test] public void CurrentAccountSnapshotIsAccepted() {Assert.IsTrue(TutorialPresentation.AccountMatches(true,1,1,2,2,"a","a"));}
    [Test] public void ClosedControllerDoesNotReopenOnRecoveredResponseAndClearsPending()
    {
        var root=new GameObject("ControllerTest",typeof(Canvas));
        try
        {
            var c=root.AddComponent<TutorialController>();var v=TutorialView.Build(root.transform);
            Set(c,"view",v);Set(c,"shown",true);Set(c,"userId",1L);Set(c,"status",Status("NOT_STARTED"));Set(c,"pending",new TutorialCommand{requestId="uncertain"});
            c.Close();Call(c,"Reconcile",new TutorialStatus{userId=1,step="PRACTICE_PENDING",revision=2});
            Assert.IsFalse(c.IsShown);Assert.IsFalse(v.gameObject.activeSelf);Assert.IsFalse(c.HasPending);Assert.AreEqual("PRACTICE_PENDING",c.Status.step);
            // Avoid the runtime deferred-destroy hook when the editor tears this fixture down.
            Set(c,"view",null);
        }
        finally {Object.DestroyImmediate(root);}
    }
    [Test] public void InvalidRecoveredResponseKeepsPendingRequest()
    {
        var root=new GameObject("ControllerTest");
        try
        {
            var c=root.AddComponent<TutorialController>();Set(c,"userId",1L);Set(c,"status",Status("PRACTICE_PENDING"));Set(c,"pending",new TutorialCommand{requestId="uncertain"});
            Assert.Throws<System.Reflection.TargetInvocationException>(()=>Call(c,"Reconcile",new TutorialStatus{userId=2,step="COMPLETED",revision=2}));
            Assert.IsTrue(c.HasPending);Assert.AreEqual("PRACTICE_PENDING",c.Status.step);
        }
        finally {Object.DestroyImmediate(root);}
    }
    [Test] public void BusyStillAllowsCloseButNotAction() {WithView(v=>{v.Render(Status("REWARD_READY"),true,false,false);Assert.IsTrue(v.closeButton.interactable);Assert.IsFalse(v.primaryButton.interactable);Assert.IsFalse(v.refreshButton.interactable);});}
    [Test] public void UnavailablePracticeExplainsAndDisablesVerify() {WithView(v=>{v.Render(Status("PRACTICE_PENDING"),false,false,false);StringAssert.Contains("준비 중",v.statusText.text);Assert.IsFalse(v.verifyButton.interactable);Assert.IsTrue(v.primaryButton.interactable);});}
    [Test] public void UncertainPostOffersSameRequestRetry() {WithView(v=>{v.Render(Status("REWARD_READY"),false,false,true);StringAssert.Contains("같은 요청",v.primaryButton.GetComponentInChildren<Text>().text);Assert.IsTrue(v.refreshButton.interactable);});}
    [Test] public void UnknownStateCannotAct() {WithView(v=>{v.Render(Status("BAD"),false,false,false);Assert.IsFalse(v.primaryButton.interactable);});}
    [Test] public void FixtureExcludedFromProductionBuild() {foreach(var scene in UnityEditor.EditorBuildSettings.scenes)Assert.IsFalse(scene.enabled&&scene.path==TutorialAssetBuilder.FixturePath);}
    private static TutorialStatus Status(string step)=>new TutorialStatus{userId=1,step=step,revision=1};
    private static void Set(object target,string field,object value)=>target.GetType().GetField(field,System.Reflection.BindingFlags.Instance|System.Reflection.BindingFlags.NonPublic).SetValue(target,value);
    private static void Call(object target,string method,object value)=>target.GetType().GetMethod(method,System.Reflection.BindingFlags.Instance|System.Reflection.BindingFlags.NonPublic).Invoke(target,new[]{value});
    private static void WithView(System.Action<TutorialView> test) {var root=new GameObject("Test",typeof(Canvas));try{test(TutorialView.Build(root.transform));}finally{Object.DestroyImmediate(root);}}
}
