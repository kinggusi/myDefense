using UnityEngine;
using UnityEditor; // 유니티 에디터를 조종하는 핵심 기능!
using System.IO;

public class AutoUIBuilder
{
    // 1. 유니티 맨 위쪽 메뉴바에 [AI Tools]라는 메뉴를 만듭니다.
    [MenuItem("AI Tools/1. 선택한 오브젝트를 프리팹으로 굽기 💾")]
    public static void SaveSelectedAsPrefab()
    {
        // 현재 Hierarchy 창에서 마우스로 선택한 녀석을 가져옵니다.
        GameObject selectedObj = Selection.activeGameObject;

        if (selectedObj == null)
        {
            Debug.LogWarning("⚠️ 아무것도 선택하지 않았습니다! 프리팹으로 만들 대상을 클릭해 주세요.");
            return;
        }

        // 프리팹을 저장할 폴더 경로 (없으면 만듭니다)
        string folderPath = "Assets/Prefabs";
        if (!Directory.Exists(folderPath))
        {
            Directory.CreateDirectory(folderPath);
            AssetDatabase.Refresh();
        }

        // 저장할 파일 이름 설정 (선택한 오브젝트 이름.prefab)
        string prefabPath = $"{folderPath}/{selectedObj.name}.prefab";

        // 진짜 프리팹으로 굽는 마법의 코드!
        // (Interactable 모드는 기존 프리팹이 있으면 덮어씌우는 옵션입니다)
        PrefabUtility.SaveAsPrefabAssetAndConnect(selectedObj, prefabPath, InteractionMode.UserAction);

        Debug.Log($"🎉 짠! [{selectedObj.name}] 프리팹이 {prefabPath} 에 성공적으로 저장되었습니다!");
    }

    // --- 1번 버튼 (선택한 오브젝트 프리팹 저장) 코드는 그대로 둡니다 ---
    // public static void SaveSelectedAsPrefab() { ... } 

    // 👇 👇 👇 여기에 스케치 분석 결과를 바탕으로 짠 진짜 코드를 넣습니다 👇 👇 👇 

    [MenuItem("AI Tools/2. [스케치] 복잡한 상점 화면 자동 생성 🛍️")]
    public static void CreateComplexShopScreen()
    {
        GameObject canvas = GameObject.Find("Canvas");
        if (canvas == null) { Debug.LogError("⚠️ 씬에 Canvas가 필요합니다!"); return; }

        GameObject shopPanelObj = new GameObject("Panel_Shop_Complex");
        shopPanelObj.transform.SetParent(canvas.transform, false);
        RectTransform shopRect = shopPanelObj.AddComponent<RectTransform>();
        shopRect.anchorMin = Vector2.zero; shopRect.anchorMax = Vector2.one; shopRect.sizeDelta = Vector2.zero; 
        shopPanelObj.AddComponent<UnityEngine.UI.Image>().color = new Color(0.1f, 0.1f, 0.15f, 0.95f); // 고급진 다크 네이비 배경

        GameObject scrollViewObj = new GameObject("Shop_ScrollView");
        scrollViewObj.transform.SetParent(shopPanelObj.transform, false);
        RectTransform scrollRectTransform = scrollViewObj.AddComponent<RectTransform>();
        scrollRectTransform.anchorMin = Vector2.zero; scrollRectTransform.anchorMax = Vector2.one;
        scrollRectTransform.sizeDelta = new Vector2(0, -200); 
        scrollRectTransform.anchoredPosition = new Vector2(0, -100);

        UnityEngine.UI.ScrollRect scrollRect = scrollViewObj.AddComponent<UnityEngine.UI.ScrollRect>();
        scrollViewObj.AddComponent<UnityEngine.UI.RectMask2D>(); // 마스크 정상 작동!

        GameObject contentObj = new GameObject("Content");
        contentObj.transform.SetParent(scrollViewObj.transform, false);
        RectTransform contentRect = contentObj.AddComponent<RectTransform>();
        contentRect.anchorMin = new Vector2(0.5f, 1); contentRect.anchorMax = new Vector2(0.5f, 1); 
        contentRect.sizeDelta = new Vector2(1000, 100); 

        // 🚀 [수정] Content의 세로 정렬 완벽 세팅
        UnityEngine.UI.VerticalLayoutGroup contentLayout = contentObj.AddComponent<UnityEngine.UI.VerticalLayoutGroup>();
        contentLayout.childAlignment = TextAnchor.UpperCenter;
        contentLayout.spacing = 80; // 섹션 사이의 여유로운 간격
        contentLayout.padding = new RectOffset(50, 50, 50, 150);
        contentLayout.childControlHeight = true; 
        contentLayout.childControlWidth = true; 
        contentLayout.childForceExpandHeight = false; // 겹침 방지 핵심 1
        contentLayout.childForceExpandWidth = true;

        contentObj.AddComponent<UnityEngine.UI.ContentSizeFitter>().verticalFit = UnityEngine.UI.ContentSizeFitter.FitMode.PreferredSize;
        scrollRect.content = contentRect;
        scrollRect.horizontal = false; 

        System.Action<string, string, int, Vector2> createSection = (sectionName, titleText, itemCount, cellSize) => {
            GameObject sectionObj = new GameObject(sectionName);
            sectionObj.transform.SetParent(contentRect.transform, false);
            
            // 🚀 [수정] 각 섹션도 알아서 높이가 늘어나도록 Fitter 추가! (겹침 방지 핵심 2)
            UnityEngine.UI.VerticalLayoutGroup sectionLayout = sectionObj.AddComponent<UnityEngine.UI.VerticalLayoutGroup>();
            sectionLayout.childAlignment = TextAnchor.UpperCenter;
            sectionLayout.spacing = 30; 
            sectionLayout.childControlHeight = true; 
            sectionLayout.childControlWidth = true;
            sectionLayout.childForceExpandHeight = false;
            sectionLayout.childForceExpandWidth = true;
            sectionObj.AddComponent<UnityEngine.UI.ContentSizeFitter>().verticalFit = UnityEngine.UI.ContentSizeFitter.FitMode.PreferredSize;

            GameObject titleObj = new GameObject("TitleText");
            titleObj.transform.SetParent(sectionObj.transform, false);
            UnityEngine.UI.Text t = titleObj.AddComponent<UnityEngine.UI.Text>();
            t.text = titleText; t.fontSize = 55; t.color = Color.white;
            t.font = Resources.GetBuiltinResource<Font>("LegacyRuntime.ttf");
            t.alignment = TextAnchor.MiddleLeft; // 제목은 왼쪽 정렬
            
            // 제목 높이 고정 (안 찌그러지게)
            UnityEngine.UI.LayoutElement titleLayout = titleObj.AddComponent<UnityEngine.UI.LayoutElement>();
            titleLayout.minHeight = 80;

            GameObject gridObj = new GameObject("Grid_Items");
            gridObj.transform.SetParent(sectionObj.transform, false);
            UnityEngine.UI.GridLayoutGroup gridLayout = gridObj.AddComponent<UnityEngine.UI.GridLayoutGroup>();
            gridLayout.cellSize = cellSize;
            gridLayout.spacing = new Vector2(40, 40); // 카드 사이 간격
            gridLayout.childAlignment = TextAnchor.UpperCenter;
            gridLayout.constraint = UnityEngine.UI.GridLayoutGroup.Constraint.FixedColumnCount;
            gridLayout.constraintCount = 3; // 3열 고정

            gridObj.AddComponent<UnityEngine.UI.ContentSizeFitter>().verticalFit = UnityEngine.UI.ContentSizeFitter.FitMode.PreferredSize;

            for (int i = 0; i < itemCount; i++)
            {
                GameObject dummyItem = new GameObject("Dummy_Item_Card");
                dummyItem.transform.SetParent(gridObj.transform, false);
                // 노란색 대신 고급스러운 반투명 패널 느낌으로!
                dummyItem.AddComponent<UnityEngine.UI.Image>().color = new Color(1f, 1f, 1f, 0.2f); 
            }
        };

        // 🚀 [수정] 1080 해상도에 맞춰서 한 줄에 3개가 쏙 들어가는 황금비율로 사이즈 축소!
        createSection("Section_TodayShop", "오늘의 상점 (Today's Shop)", 9, new Vector2(270, 320));
        createSection("Section_Gems", "Gem (다이아몬드)", 6, new Vector2(270, 350));
        createSection("Section_Gold", "골드 (Gold)", 6, new Vector2(270, 320)); 
        createSection("Section_Extra_New", "새로운 섹션 (추가 용이 *)", 3, new Vector2(270, 320)); 

        // 유니티 에디터 화면 갱신 버그 방지용 (강제 새로고침)
        UnityEngine.UI.LayoutRebuilder.ForceRebuildLayoutImmediate(contentRect);

        Debug.Log("🎉 레이아웃 완벽 수정! 상점 화면 껍데기가 완성되었습니다!");
    }
}