using AlienUpgrade.Core;
using NUnit.Framework;

namespace AlienUpgrade.Core.Tests
{
    public sealed class AlienUpgradeFlowTests
    {
        [Test]
        public void CardSelection_InvokesBoundAlienId()
        {
            long selected = 0;
            var selection = new AlienCardSelection();
            selection.Bind(22, id => selected = id);

            selection.Select();

            Assert.That(selected, Is.EqualTo(22));
        }

[Test]
        public void CompleteStatus_CopiesServerDtoWithoutCalculation()
        {
            var flow = NewRequestingFlow();
            var dto = Status(canUpgrade: true);
            dto.currentLevel = 9;
            dto.currentAtk = 123.45;
            dto.currentMp = 67.89;
            dto.currentAtkSpeed = 1.75;
            dto.currentRange = 4.25;
            dto.requiredPieces = 17;
            dto.requiredGold = 3000;

            flow.CompleteStatus(dto);

            Assert.That(flow.View.Level, Is.EqualTo(9));
            Assert.That(flow.View.Attack, Is.EqualTo(123.45));
            Assert.That(flow.View.Mp, Is.EqualTo(67.89));
            Assert.That(flow.View.AttackSpeed, Is.EqualTo(1.75));
            Assert.That(flow.View.Range, Is.EqualTo(4.25));
            Assert.That(flow.View.RequiredPieces, Is.EqualTo(17));
            Assert.That(flow.View.RequiredGold, Is.EqualTo(3000));
        }

        [Test]
        public void Requesting_BlocksDuplicateStatusAndUpgradeRequests()
        {
            var flow = new AlienUpgradeFlow();

            Assert.That(flow.BeginStatusRequest(22), Is.True);
            Assert.That(flow.BeginStatusRequest(22), Is.False);
            Assert.That(flow.BeginUpgradeRequest(), Is.False);
        }

        [Test]
        public void UpgradeRequest_BlocksTenConsecutiveStarts()
        {
            var flow = ReadyFlow();

            Assert.That(flow.BeginUpgradeRequest(), Is.True);
            for (int attempt = 0; attempt < 10; attempt++)
            {
                Assert.That(flow.BeginUpgradeRequest(), Is.False);
            }
        }

        [Test]
        public void UpgradeLatch_IsAcquiredSynchronouslyWhenRequestStarts()
        {
            var flow = ReadyFlow();

            Assert.That(flow.BeginUpgradeRequest(), Is.True);
            flow.CompleteLobbyRefresh();

            Assert.That(flow.BeginUpgradeRequest(), Is.False);
        }

        [Test]
        public void SuccessfulResponse_KeepsRequestLockedUntilLobbyRefreshCompletes()
        {
            var flow = ReadyFlow();
            Assert.That(flow.BeginUpgradeRequest(), Is.True);

            flow.CompleteUpgrade(Upgrade());

            Assert.That(flow.IsRequesting, Is.True);
            Assert.That(flow.CanClose, Is.False);
            Assert.That(flow.BeginUpgradeRequest(), Is.False);
        }

        [Test]
        public void LobbyRefreshCompletion_KeepsCurrentScreenUpgradeConsumed()
        {
            var flow = ReadyFlow();
            Assert.That(flow.BeginUpgradeRequest(), Is.True);
            flow.CompleteUpgrade(Upgrade());
            Assert.That(flow.TryConsumeLobbyRefresh(), Is.True);

            flow.CompleteLobbyRefresh();

            Assert.That(flow.IsRequesting, Is.False);
            Assert.That(flow.CanClose, Is.True);
            Assert.That(flow.BeginUpgradeRequest(), Is.False);
        }

        [Test]
        public void RapidClicks_AfterFastResponseRemainBlockedUntilStatusIsReloaded()
        {
            var flow = ReadyFlow();
            Assert.That(flow.BeginUpgradeRequest(), Is.True);
            flow.CompleteUpgrade(Upgrade());
            Assert.That(flow.TryConsumeLobbyRefresh(), Is.True);
            flow.CompleteLobbyRefresh();

            for (int attempt = 0; attempt < 10; attempt++)
            {
                Assert.That(flow.BeginUpgradeRequest(), Is.False);
            }
        }

        [Test]
        public void ReloadedStatus_AllowsDeliberateNextUpgrade()
        {
            var flow = ReadyFlow();
            Assert.That(flow.BeginUpgradeRequest(), Is.True);
            flow.CompleteUpgrade(Upgrade());
            Assert.That(flow.TryConsumeLobbyRefresh(), Is.True);
            flow.CompleteLobbyRefresh();

            Assert.That(flow.BeginUpgradeRequest(), Is.False);
            Assert.That(flow.BeginStatusRequest(22), Is.True);
            flow.CompleteStatus(Status(canUpgrade: true));
            Assert.That(flow.BeginUpgradeRequest(), Is.True);
        }

        [Test]
        public void ExplicitRearm_AfterCompletedRefreshAllowsDeliberateNextUpgrade()
        {
            var flow = ReadyFlow();
            Assert.That(flow.BeginUpgradeRequest(), Is.True);
            flow.CompleteUpgrade(Upgrade());
            Assert.That(flow.TryConsumeLobbyRefresh(), Is.True);
            flow.CompleteLobbyRefresh();

            Assert.That(flow.BeginUpgradeRequest(), Is.False);
            Assert.That(flow.RearmUpgrade(), Is.True);
            Assert.That(flow.BeginUpgradeRequest(), Is.True);
        }

        [Test]
        public void Rearm_DuringRequestIsRejected()
        {
            var flow = ReadyFlow();
            Assert.That(flow.BeginUpgradeRequest(), Is.True);

            Assert.That(flow.RearmUpgrade(), Is.False);
            Assert.That(flow.BeginUpgradeRequest(), Is.False);
        }

        [Test]
        public void Failure_ReleasesRequestAccordingToCurrentCanUpgradeState()
        {
            var flow = ReadyFlow();
            Assert.That(flow.BeginUpgradeRequest(), Is.True);

            flow.Fail("네트워크 오류");

            Assert.That(flow.IsRequesting, Is.False);
            Assert.That(flow.BeginUpgradeRequest(), Is.True);
        }

        [Test]
        public void CanUpgradeFalse_BlocksRequestWithoutEnteringRequestingState()
        {
            var flow = NewRequestingFlow();
            flow.CompleteStatus(Status(canUpgrade: false));

            Assert.That(flow.BeginUpgradeRequest(), Is.False);
            Assert.That(flow.IsRequesting, Is.False);
            Assert.That(flow.CanClose, Is.True);
        }

[Test]
        public void CompleteUpgrade_RefreshesAllServerValues()
        {
            var flow = ReadyFlow();
            Assert.That(flow.BeginUpgradeRequest(), Is.True);

            flow.CompleteUpgrade(Upgrade());

            Assert.That(flow.View.Level, Is.EqualTo(2));
            Assert.That(flow.View.CurrentPieces, Is.EqualTo(40));
            Assert.That(flow.View.UniversalPiece, Is.EqualTo(7));
            Assert.That(flow.View.Gold, Is.EqualTo(8500));
            Assert.That(flow.View.GrowthCell, Is.EqualTo(3));
            Assert.That(flow.View.Attack, Is.EqualTo(150));
            Assert.That(flow.View.Mp, Is.EqualTo(80));
            Assert.That(flow.View.AttackSpeed, Is.EqualTo(1.2));
            Assert.That(flow.View.Range, Is.EqualTo(3.5));
        }

        [Test]
        public void MaxLevelStatus_DisablesUpgradeAndShowsReason()
        {
            var flow = NewRequestingFlow();
            var dto = Status(canUpgrade: false);
            dto.maxLevelReached = true;
            dto.cannotUpgradeReason = "MAX_LEVEL";

            flow.CompleteStatus(dto);

            Assert.That(flow.View.CanUpgrade, Is.False);
            Assert.That(flow.ErrorMessage, Is.EqualTo("최대 레벨입니다."));
            Assert.That(flow.BeginUpgradeRequest(), Is.False);
        }

        [Test]
        public void NotOwned_ShowsGachaGuidance()
        {
            Assert.That(AlienUpgradeFlow.MessageForReason("NOT_OWNED"), Is.EqualTo("가챠에서 획득하세요."));
            Assert.That(AlienUpgradeFlow.MessageForError("NOT_OWNED", null), Is.EqualTo("가챠에서 획득하세요."));
        }

        [Test]
        public void PiecesShortage_MapsBusinessReason()
        {
            Assert.That(AlienUpgradeFlow.MessageForReason("INSUFFICIENT_PIECES"), Is.EqualTo("Alien 조각이 부족합니다."));
        }

        [Test]
        public void GoldShortage_MapsBusinessReason()
        {
            Assert.That(AlienUpgradeFlow.MessageForReason("INSUFFICIENT_GOLD"), Is.EqualTo("Gold가 부족합니다."));
        }

        [Test]
        public void GrowthCellShortage_MapsBusinessReason()
        {
            Assert.That(AlienUpgradeFlow.MessageForReason("INSUFFICIENT_GROWTH_CELL"), Is.EqualTo("Growth Cell이 부족합니다."));
        }

        [Test]
        public void NetworkFailure_StopsRequestAndShowsMessage()
        {
            var flow = NewRequestingFlow();

            flow.Fail("네트워크 오류");

            Assert.That(flow.IsRequesting, Is.False);
            Assert.That(flow.ErrorMessage, Is.EqualTo("네트워크 오류"));
        }

        [Test]
        public void SuccessfulUpgrade_RequestsLobbyRefreshExactlyOnce()
        {
            var flow = ReadyFlow();
            Assert.That(flow.BeginUpgradeRequest(), Is.True);
            flow.CompleteUpgrade(Upgrade());

            Assert.That(flow.TryConsumeLobbyRefresh(), Is.True);
            Assert.That(flow.TryConsumeLobbyRefresh(), Is.False);
            Assert.That(flow.IsRequesting, Is.True);

            flow.CompleteLobbyRefresh();

            Assert.That(flow.IsRequesting, Is.False);
        }

        private static AlienUpgradeFlow NewRequestingFlow()
        {
            var flow = new AlienUpgradeFlow();
            Assert.That(flow.BeginStatusRequest(22), Is.True);
            return flow;
        }

        private static AlienUpgradeFlow ReadyFlow()
        {
            var flow = NewRequestingFlow();
            flow.CompleteStatus(Status(canUpgrade: true));
            return flow;
        }

        private static AlienUpgradeStatusDto Status(bool canUpgrade)
        {
            return new AlienUpgradeStatusDto
            {
                alienId = 22,
                alienName = "Test Alien",
                grade = "COMMON",
                owned = true,
                currentLevel = 1,
                currentPieces = 49,
                universalPiece = 10,
                gold = 10000,
                growthCell = 5,
                maxLevel = 50,
                canUpgrade = canUpgrade,
                cannotUpgradeReason = canUpgrade ? "NONE" : "MAX_LEVEL",
                requiredPieces = 9,
                requiredUniversalPiece = 0,
                requiredGold = 1500,
                requiredGrowthCell = 2,
                currentAtk = 100,
                currentMp = 60,
                currentAtkSpeed = 1,
                currentRange = 3
            };
        }

        private static AlienUpgradeResponseDto Upgrade()
        {
            return new AlienUpgradeResponseDto
            {
                alienId = 22,
                alienName = "Test Alien",
                beforeLevel = 1,
                afterLevel = 2,
                remainingPieces = 40,
                remainingUniversalPiece = 7,
                remainingGold = 8500,
                remainingGrowthCell = 3,
                maxLevel = 50,
                canUpgrade = true,
                cannotUpgradeReason = "NONE",
                nextRequiredPieces = 10,
                nextRequiredUniversalPiece = 0,
                nextRequiredGold = 1700,
                nextRequiredGrowthCell = 2,
                currentAtk = 150,
                currentMp = 80,
                currentAtkSpeed = 1.2,
                currentRange = 3.5
            };
        }
    }
}
