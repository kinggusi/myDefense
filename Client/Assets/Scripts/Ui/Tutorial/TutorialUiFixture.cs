#if UNITY_EDITOR
using UnityEngine;
using UnityEngine.UI;

namespace MyDefense.Tutorial
{
    /// <summary>Editor-only presentation fixture. No HTTP, account state, completion evidence or rewards.</summary>
    public sealed class TutorialUiFixture : MonoBehaviour
    {
        public TutorialView view;
        private int index;
        private readonly string[] steps={"NOT_STARTED","PRACTICE_PENDING","COLLECTION_GUIDE","UPGRADE_GUIDE","REWARD_READY","BREEDING_GUIDE","COMPLETED"};
        void Start()
        {
            ShopCatalogView.Label(transform,"FixtureNotice","UI 시안 테스트 · 서버/재화/진행 변경 없음",26,.04f,.91f,.96f,.97f);
            var next=ShopCatalogView.Button(transform,"NextFixture","다음 상태 보기 (테스트)",.08f,.81f,.92f,.875f);
            next.onClick.AddListener(()=>{index=(index+1)%steps.Length;Show();});
            var reopen=ShopCatalogView.Button(transform,"ReopenFixture","안내 다시 열기",.20f,.045f,.80f,.11f);
            reopen.onClick.AddListener(Show);
            view.closeButton.onClick.AddListener(()=>view.gameObject.SetActive(false));
            view.refreshButton.onClick.AddListener(Show);
            view.primaryButton.onClick.AddListener(()=>
            {
                if(steps[index]=="PRACTICE_PENDING"){view.statusText.text="연습 전투 연결 준비 중 · 우회 완료 불가";return;}
                if(steps[index]=="COMPLETED"){view.gameObject.SetActive(false);return;}
                index++;Show();
            });
            Show();
        }
        private void Show()
        {
            view.gameObject.SetActive(true);
            view.Render(new TutorialStatus{userId=1,step=steps[index],revision=index,rewardGranted=index>=5},false,false,false);
            if(steps[index]!="PRACTICE_PENDING")view.statusText.text="UI 시안 · 실제 완료/지급 없음";
        }
    }
}
#endif
