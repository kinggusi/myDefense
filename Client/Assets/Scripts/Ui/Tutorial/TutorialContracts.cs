using System;

namespace MyDefense.Tutorial
{
    [Serializable] public sealed class TutorialStatus
    {
        public long userId;
        public string step;
        public long revision;
        public string practiceRunId;
        public bool practiceAvailable;
        public bool rewardGranted;
    }
    [Serializable] public sealed class TutorialCommand
    {
        public string requestId;
        public long expectedRevision;
        public string operation;
        public string practiceRunId;
    }
    /// <summary>User/System-only seam. A Battle-owned adapter is required; no local completion claim.</summary>
    public interface ITutorialPracticeLauncher
    {
        bool Available { get; }
        void Launch(long userId,string runId,Action returnedToLobby,Action<string> failure);
    }
    public sealed class UnavailableTutorialPracticeLauncher : ITutorialPracticeLauncher
    {
        public bool Available => false;
        public void Launch(long userId,string runId,Action returnedToLobby,Action<string> failure)
            => failure?.Invoke("연습 전투 연결을 준비 중입니다. 진행은 저장되어 있습니다.");
    }
    public static class TutorialPresentation
    {
        public static bool AccountMatches(bool sameSession,long expectedUser,long actualUser,int expectedGeneration,int actualGeneration,string expectedEndpoint,string actualEndpoint)
            => sameSession&&expectedUser>0&&expectedUser==actualUser&&expectedGeneration==actualGeneration&&!string.IsNullOrEmpty(expectedEndpoint)&&expectedEndpoint==actualEndpoint;
        public static bool Known(string step) => step=="NOT_STARTED"||step=="PRACTICE_PENDING"||step=="COLLECTION_GUIDE"||step=="UPGRADE_GUIDE"||step=="REWARD_READY"||step=="BREEDING_GUIDE"||step=="COMPLETED";
        public static string Title(string step) => step=="COLLECTION_GUIDE"?"내 유닛":step=="UPGRADE_GUIDE"?"강화 알아보기":step=="REWARD_READY"?"튜토리얼 완료 보상":step=="BREEDING_GUIDE"?"신화 교배 위치":"왹져 첫걸음";
        public static string Message(string step)
        {
            switch(step)
            {
                case "NOT_STARTED": return "무료 연습에서 납치 → 빈칸 이동 → 같은 종 머지를 배워요.\n\n이후 내 유닛과 강화를 알아보면 신화 1·2가 해금됩니다.\n언제든 나중에 이어서 할 수 있어요.";
                case "PRACTICE_PENDING": return "같은 종·같은 등급 유닛 두 개를 합치면 다음 등급이 됩니다.\n\n연습 완료는 서버에서 확인합니다. 일반 전투 보상이나 퀘스트 진행에는 포함되지 않아요.";
                case "COLLECTION_GUIDE": return "하단의 초록 왹져 아이콘이 ‘내 유닛’이에요.\n\n카드를 누르면 유닛 정보와 보유 조각을 확인할 수 있어요. 미해금 신화는 목록 아래에 모여 있습니다.";
                case "UPGRADE_GUIDE": return "유닛 상세 화면에서 강화에 필요한 조각과 재료를 확인할 수 있어요.\n\n왹져DNA는 조각 대체 재료, 성장세포는 고레벨 강화 재료입니다. 이번 안내에서는 재료를 소비하지 않아요.";
                case "REWARD_READY": return "신화 1·2를 영구 해금할 준비가 되었어요.\n\n이미 보유한 유닛의 레벨과 조각은 그대로 유지됩니다. 보상은 계정당 한 번만 받을 수 있어요.";
                case "BREEDING_GUIDE": return "‘내 유닛’ 화면 오른쪽 위의 ‘신화 교배’ 패널에서 진입할 수 있어요.\n\n부모 유닛은 소모되지 않습니다. 지금은 위치만 안내하며 교배를 시작하거나 재료를 사용하지 않아요.";
                case "COMPLETED": return "안내를 모두 마쳤어요!\n신화 1·2 보상은 이미 처리되었습니다.\n\n자유롭게 왹져 디펜스를 즐겨 보세요.";
                default:return "진행 상태를 다시 조회해 주세요.";
            }
        }
        public static string Operation(string step) => step=="NOT_STARTED"?"START":step=="COLLECTION_GUIDE"?"COLLECTION_SEEN":step=="UPGRADE_GUIDE"?"UPGRADE_SEEN":step=="REWARD_READY"?"CLAIM":step=="BREEDING_GUIDE"?"BREEDING_SEEN":null;
        public static string Action(string step) => step=="NOT_STARTED"?"안내 시작":step=="PRACTICE_PENDING"?"연습 시작 / 이어하기":step=="REWARD_READY"?"신화 1·2 받기":step=="COMPLETED"?"자유 플레이":"확인했어요";
    }
}
