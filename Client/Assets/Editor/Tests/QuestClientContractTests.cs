using NUnit.Framework;
using UnityEditor;
using UnityEngine;
using UnityEngine.UI;
using UnityEngine.EventSystems;
using System.Collections.Generic;

public sealed class QuestClientContractTests
{
    [UnityEngine.TestTools.UnityTest]
    public System.Collections.IEnumerator QuestHeaderDoesNotOverlapRewardsAndCloseReceivesPointer()
    {
        var canvas = new GameObject("QuestInputTest", typeof(Canvas), typeof(GraphicRaycaster));
        var eventObject = new GameObject("QuestEventTest", typeof(EventSystem));
        var cameraObject = new GameObject("QuestRenderTest", typeof(Camera));
        var target = new RenderTexture(1080, 1920, 24);
        try
        {
            var camera = cameraObject.GetComponent<Camera>();
            camera.enabled = false;
            camera.targetTexture = target;
            var testCanvas = canvas.GetComponent<Canvas>();
            testCanvas.renderMode = RenderMode.ScreenSpaceCamera;
            testCanvas.worldCamera = camera;
            testCanvas.planeDistance = 1f;
            var prefab = AssetDatabase.LoadAssetAtPath<GameObject>("Assets/Resources/Prefabs/Lobby/Quest/QuestScreen.prefab");
            var instance = Object.Instantiate(prefab, canvas.transform);
            var view = instance.GetComponent<QuestUiView>();
            var shortcut = Object.Instantiate(AssetDatabase.LoadAssetAtPath<GameObject>(
                "Assets/Resources/Prefabs/Lobby/Quest/QuestShortcut.prefab"), canvas.transform).GetComponent<QuestShortcutView>();
            shortcut.gameObject.SetActive(false);
            var controller = canvas.AddComponent<QuestController>();
            const System.Reflection.BindingFlags flags = System.Reflection.BindingFlags.Instance | System.Reflection.BindingFlags.NonPublic;
            typeof(QuestController).GetField("view", flags).SetValue(controller, view);
            typeof(QuestController).GetField("shortcut", flags).SetValue(controller, shortcut);
            typeof(QuestController).GetMethod("WireEvents", flags).Invoke(controller, null);
            // Explicit rendering supplies Graphic depth even when the Game view is not focused.
            yield return null;
            Canvas.ForceUpdateCanvases();
            camera.Render();
            var header = (RectTransform)view.closeButton.transform.parent;
            Assert.That(header.offsetMax.y, Is.EqualTo(0f));
            var milestone = (RectTransform)view.milestoneRoot;
            Assert.That(view.questScrollRect.offsetMax.y, Is.LessThan(milestone.offsetMin.y - 10f));
            Assert.That(view.closeButton.GetComponent<RectTransform>().rect.height, Is.GreaterThanOrEqualTo(86f));
            var corners = new Vector3[4];
            view.closeButton.GetComponent<RectTransform>().GetWorldCorners(corners);
            var pointer = new PointerEventData(eventObject.GetComponent<EventSystem>())
            { position = RectTransformUtility.WorldToScreenPoint(camera, (corners[0] + corners[2]) * .5f) };
            var hits = new List<RaycastResult>();
            canvas.GetComponent<GraphicRaycaster>().Raycast(pointer, hits);
            Assert.That(hits, Is.Not.Empty);
            Assert.That(hits[0].gameObject.GetComponentInParent<Button>(), Is.SameAs(view.closeButton));
            // Request-in-flight cannot disable close; pointer click reaches the real controller handler.
            typeof(QuestController).GetMethod("SetBusy", flags).Invoke(controller, new object[] { true });
            ExecuteEvents.Execute(view.closeButton.gameObject, pointer, ExecuteEvents.pointerClickHandler);
            Assert.That(instance.activeSelf, Is.False);
            instance.SetActive(true);
            ExecuteEvents.Execute(view.closeButton.gameObject, pointer, ExecuteEvents.pointerClickHandler);
            Assert.That(instance.activeSelf, Is.False);
        }
        finally
        {
            Object.DestroyImmediate(canvas);
            Object.DestroyImmediate(eventObject);
            Object.DestroyImmediate(cameraObject);
            target.Release();
            Object.DestroyImmediate(target);
        }
    }

    [Test]
    public void CountClaimable_IncludesQuestAndMilestoneRewards()
    {
        var cycle = new QuestCycleDto
        {
            quests = new[]
            {
                new QuestItemDto { completed = true, claimed = false },
                new QuestItemDto { completed = true, claimed = true },
                new QuestItemDto { completed = false, claimed = false }
            },
            milestones = new[]
            {
                new QuestMilestoneDto { unlocked = true, claimed = false },
                new QuestMilestoneDto { unlocked = false, claimed = false }
            }
        };

        Assert.That(QuestClientContract.CountClaimable(cycle), Is.EqualTo(2));
    }

    [Test]
    public void CountClaimable_IncludesCompletedUnclaimedAchievements()
    {
        var achievements = new[]
        {
            new QuestAchievementDto { completed = true, claimed = false },
            new QuestAchievementDto { completed = true, claimed = true },
            new QuestAchievementDto { completed = false, claimed = false }
        };

        Assert.That(QuestClientContract.CountClaimable(achievements), Is.EqualTo(1));
    }

    [Test]
    public void FormatReward_UsesPlayerFacingCurrencyNames()
    {
        string value = QuestClientContract.FormatReward(new QuestRewardDto
        {
            gold = 1000, universalPiece = 10, diamond = 300
        });

        Assert.That(value, Does.Contain("Gold 1,000"));
        Assert.That(value, Does.Contain("왹져 DNA 10"));
        Assert.That(value, Does.Contain("Gem 300"));
    }

    [Test]
    public void FormatProgress_ClampsDisplayAtTarget()
    {
        Assert.That(QuestClientContract.FormatProgress(120, 100), Is.EqualTo("100 / 100"));
    }

    [Test]
    public void FormatReset_UsesServerTimeInsteadOfDeviceClock()
    {
        string value = QuestClientContract.FormatReset(
            "2026-09-03T00:00:00+09:00",
            "2026-09-02T22:30:00+09:00");

        Assert.That(value, Is.EqualTo("1시간 30분 후"));
    }

    [Test]
    public void FormatReset_InvalidServerTimeFallsBackToServerLabel()
    {
        Assert.That(QuestClientContract.FormatReset("2026-09-03T00:00:00+09:00", "invalid"),
            Is.EqualTo("서버 기준"));
    }

    [Test]
    public void QuestPrefabs_HaveAllRequiredReferences()
    {
        var screen = AssetDatabase.LoadAssetAtPath<UnityEngine.GameObject>(
            "Assets/Resources/Prefabs/Lobby/Quest/QuestScreen.prefab");
        var shortcut = AssetDatabase.LoadAssetAtPath<UnityEngine.GameObject>(
            "Assets/Resources/Prefabs/Lobby/Quest/QuestShortcut.prefab");

        Assert.That(screen, Is.Not.Null);
        Assert.That(shortcut, Is.Not.Null);
        QuestUiView view = screen.GetComponent<QuestUiView>();
        QuestShortcutView shortcutView = shortcut.GetComponent<QuestShortcutView>();
        Assert.That(view, Is.Not.Null);
        Assert.That(view.closeButton, Is.Not.Null);
        Assert.That(view.dailyTabButton, Is.Not.Null);
        Assert.That(view.weeklyTabButton, Is.Not.Null);
        Assert.That(view.achievementTabButton, Is.Not.Null);
        Assert.That(view.activitySlider, Is.Not.Null);
        Assert.That(view.milestoneTemplate, Is.Not.Null);
        Assert.That(view.questTemplate, Is.Not.Null);
        Assert.That(view.questScrollRect, Is.Not.Null);
        Assert.That(shortcutView, Is.Not.Null);
        Assert.That(shortcutView.button, Is.Not.Null);
    }
}
