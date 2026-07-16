using System;

namespace AlienUpgrade.Core
{
    public sealed class AlienCardSelection
    {
        private long alienId;
        private Action<long> onSelected;

        public void Bind(long id, Action<long> callback)
        {
            alienId = id;
            onSelected = callback;
        }

        public void Select()
        {
            if (alienId > 0)
            {
                onSelected?.Invoke(alienId);
            }
        }
    }

    public sealed class AlienUpgradeFlow
    {
        public AlienUpgradeViewModel View { get; } = new AlienUpgradeViewModel();
        public bool IsRequesting { get; private set; }
        public bool CanClose => !IsRequesting;
        public string ErrorMessage { get; private set; }

        private bool lobbyRefreshPending;
        private bool upgradeCompletedForCurrentStatus;

        public bool BeginStatusRequest(long alienId)
        {
            if (IsRequesting || alienId <= 0)
            {
                return false;
            }

            View.AlienId = alienId;
            ErrorMessage = string.Empty;
            upgradeCompletedForCurrentStatus = false;
            IsRequesting = true;
            return true;
        }

        public bool BeginUpgradeRequest()
        {
            if (!CanStartUpgrade())
            {
                return false;
            }

            ErrorMessage = string.Empty;
            upgradeCompletedForCurrentStatus = true;
            IsRequesting = true;
            return true;
        }

        public bool CanStartUpgrade()
        {
            return !IsRequesting &&
                   !upgradeCompletedForCurrentStatus &&
                   View.CanUpgrade &&
                   View.AlienId > 0;
        }

        public void CompleteStatus(AlienUpgradeStatusDto response)
        {
            IsRequesting = false;
            if (response == null)
            {
                Fail("응답 데이터를 확인할 수 없습니다.");
                return;
            }

            View.AlienId = response.alienId;
            View.AlienName = response.alienName;
            View.Grade = response.grade;
            View.Level = response.currentLevel;
            View.MaxLevel = response.maxLevel;
            View.CurrentPieces = response.currentPieces;
            View.UniversalPiece = response.universalPiece;
            View.Gold = response.gold;
            View.GrowthCell = response.growthCell;
            View.RequiredPieces = response.requiredPieces;
            View.RequiredUniversalPiece = response.requiredUniversalPiece;
            View.RequiredGold = response.requiredGold;
            View.RequiredGrowthCell = response.requiredGrowthCell;
            View.Attack = response.currentAtk;
            View.Mp = response.currentMp;
            View.AttackSpeed = response.currentAtkSpeed;
            View.Range = response.currentRange;
            View.MaxLevelReached = response.maxLevelReached;
            View.CanUpgrade = response.canUpgrade;
            View.CannotUpgradeReason = response.cannotUpgradeReason;
            ErrorMessage = response.canUpgrade ? string.Empty : MessageForReason(response.cannotUpgradeReason);
        }

        public void CompleteUpgrade(AlienUpgradeResponseDto response)
        {
            if (response == null)
            {
                Fail("응답 데이터를 확인할 수 없습니다.");
                return;
            }

            View.AlienId = response.alienId;
            View.AlienName = response.alienName;
            View.Level = response.afterLevel;
            View.MaxLevel = response.maxLevel;
            View.CurrentPieces = response.remainingPieces;
            View.UniversalPiece = response.remainingUniversalPiece;
            View.Gold = response.remainingGold;
            View.GrowthCell = response.remainingGrowthCell;
            View.RequiredPieces = response.nextRequiredPieces;
            View.RequiredUniversalPiece = response.nextRequiredUniversalPiece;
            View.RequiredGold = response.nextRequiredGold;
            View.RequiredGrowthCell = response.nextRequiredGrowthCell;
            View.Attack = response.currentAtk;
            View.Mp = response.currentMp;
            View.AttackSpeed = response.currentAtkSpeed;
            View.Range = response.currentRange;
            View.MaxLevelReached = response.maxLevelReached;
            View.CanUpgrade = response.canUpgrade;
            View.CannotUpgradeReason = response.cannotUpgradeReason;
            ErrorMessage = response.canUpgrade ? string.Empty : MessageForReason(response.cannotUpgradeReason);
            lobbyRefreshPending = true;
        }

        public void CompleteLobbyRefresh(string errorMessage = null)
        {
            IsRequesting = false;
            if (!string.IsNullOrWhiteSpace(errorMessage))
            {
                ErrorMessage = errorMessage;
            }
        }

        public bool RearmUpgrade()
        {
            if (IsRequesting)
            {
                return false;
            }

            upgradeCompletedForCurrentStatus = false;
            return true;
        }

        public void Fail(string message)
        {
            IsRequesting = false;
            upgradeCompletedForCurrentStatus = false;
            ErrorMessage = string.IsNullOrWhiteSpace(message) ? "요청 처리 중 오류가 발생했습니다." : message;
        }

        public bool TryConsumeLobbyRefresh()
        {
            if (!lobbyRefreshPending)
            {
                return false;
            }

            lobbyRefreshPending = false;
            return true;
        }

        public static string MessageForError(string code, string fallback)
        {
            switch (code)
            {
                case "USER_ALIEN_NOT_FOUND":
                case "NOT_OWNED":
                    return "가챠에서 획득하세요.";
                case "MAX_ALIEN_LEVEL_REACHED":
                case "MAX_LEVEL":
                    return "최대 레벨입니다.";
                case "INSUFFICIENT_ALIEN_PIECES":
                case "PIECES":
                    return "Alien 조각이 부족합니다.";
                case "INSUFFICIENT_ACCOUNT_GOLD":
                case "GOLD":
                    return "Gold가 부족합니다.";
                case "INSUFFICIENT_GROWTH_CELL":
                case "GROWTH_CELL":
                    return "Growth Cell이 부족합니다.";
                default:
                    return string.IsNullOrWhiteSpace(fallback) ? "요청 처리 중 오류가 발생했습니다." : fallback;
            }
        }

        public static string MessageForReason(string reason)
        {
            switch (reason)
            {
                case null:
                case "":
                case "NONE":
                    return string.Empty;
                case "NOT_OWNED":
                    return "가챠에서 획득하세요.";
                case "MAX_LEVEL":
                    return "최대 레벨입니다.";
                case "INSUFFICIENT_PIECES":
                case "PIECES":
                    return "Alien 조각이 부족합니다.";
                case "INSUFFICIENT_GOLD":
                case "GOLD":
                    return "Gold가 부족합니다.";
                case "INSUFFICIENT_GROWTH_CELL":
                case "GROWTH_CELL":
                    return "Growth Cell이 부족합니다.";
                default:
                    return "현재 강화할 수 없습니다.";
            }
        }
    }
}
