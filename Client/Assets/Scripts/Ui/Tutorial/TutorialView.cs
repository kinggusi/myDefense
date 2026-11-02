using System;
using UnityEngine;
using UnityEngine.UI;
using MyDefense.Lobby;

namespace MyDefense.Tutorial
{
    public sealed class TutorialView : MonoBehaviour
    {
        public Text titleText,bodyText,statusText;
        public Button primaryButton,refreshButton,closeButton,verifyButton;
        public static TutorialView Build(Transform parent)
        {
            var root=ShopCatalogView.Rect("TutorialOverlay",parent);ShopCatalogView.Stretch(root,0,0,1,1);
            root.gameObject.AddComponent<Image>().color=new Color(.0f,.01f,.025f,.92f);
            var view=root.gameObject.AddComponent<TutorialView>();
            var panel=ShopCatalogView.Rect("Panel",root);ShopCatalogView.Stretch(panel,.075f,.22f,.925f,.79f);
            ShopCatalogView.Frame(panel,LobbyNeonGraphic.FrameStyle.Panel);
            view.titleText=ShopCatalogView.Label(panel,"Title","왹져 첫걸음",42,.06f,.83f,.94f,.96f);
            view.bodyText=ShopCatalogView.Label(panel,"Body","",31,.08f,.34f,.92f,.80f,TextAnchor.UpperLeft);
            view.statusText=ShopCatalogView.Label(panel,"Status","",23,.08f,.245f,.92f,.34f);
            view.primaryButton=ShopCatalogView.Button(panel,"Primary","안내 시작",.08f,.14f,.92f,.23f);
            view.verifyButton=ShopCatalogView.Button(panel,"Verify","연습 완료 확인",.08f,.035f,.48f,.115f);
            view.refreshButton=ShopCatalogView.Button(panel,"Refresh","다시 조회",.52f,.035f,.92f,.115f);
            view.closeButton=ShopCatalogView.Button(root,"Close","나중에 하기 / 닫기",.24f,.135f,.76f,.19f);
            return view;
        }
        public void Render(TutorialStatus status,bool busy,bool clientAvailable,bool pending)
        {
            string step=status?.step;
            titleText.text=TutorialPresentation.Title(step);bodyText.text=TutorialPresentation.Message(step);
            primaryButton.GetComponentInChildren<Text>().text=pending?"같은 요청 재시도":TutorialPresentation.Action(step);
            primaryButton.interactable=!busy&&status!=null&&TutorialPresentation.Known(step);
            refreshButton.interactable=!busy;
            verifyButton.gameObject.SetActive(step=="PRACTICE_PENDING");
            verifyButton.interactable=!busy&&!pending&&status!=null&&!string.IsNullOrEmpty(status.practiceRunId)&&status.practiceAvailable;
            closeButton.interactable=true; // Closing never cancels or rewinds the server transaction.
            statusText.text=busy?"서버에서 진행을 확인하는 중...":pending?"요청 결과를 확인하지 못했어요. 재시도 또는 다시 조회해 주세요.":
                step=="PRACTICE_PENDING"&&(!status.practiceAvailable||!clientAvailable)?"연습 전투 연결 준비 중 · 진행 저장됨":"";
        }
    }
}
