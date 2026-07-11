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

    [MenuItem("AI Tools/5. [전투] 2인 협동 HUD 생성 (이미지 분위기 반영) ⚔️")]
    public static void CreateCoopBattleHUD()
    {
        GameObject canvas = GameObject.Find("Canvas");
        if (canvas == null) { Debug.LogError("⚠️ 씬에 Canvas가 필요합니다!"); return; }

        // 1. HUD 루트 (전체 화면, 터치 투과)
        GameObject hudRoot = new GameObject("UI_Coop_HUD");
        hudRoot.transform.SetParent(canvas.transform, false);
        RectTransform rootRect = hudRoot.AddComponent<RectTransform>();
        rootRect.anchorMin = Vector2.zero; rootRect.anchorMax = Vector2.one;
        rootRect.sizeDelta = Vector2.zero;
        // 터치 이벤트가 실제 3D 필드로 전달되도록 GraphicsRaycaster는 넣지 않습니다.

        // 2. 상단 상태바 (좌 레드 / 우 그린)
        GameObject topPanel = new GameObject("Top_Status_Bar");
        topPanel.transform.SetParent(hudRoot.transform, false);
        RectTransform topRect = topPanel.AddComponent<RectTransform>();
        topRect.anchorMin = new Vector2(0, 1); topRect.anchorMax = new Vector2(1, 1);
        topRect.pivot = new Vector2(0.5f, 1);
        topRect.sizeDelta = new Vector2(0, 150); // 높이 약간 늘림

        // 2-a. 플레이어 1 (왼쪽 - RED)
        CreatePlayerStatus(topPanel.transform, "P1_Red", new Vector2(0, 0.5f), new Vector2(0, 0.5f), new Color(0.8f, 0.2f, 0.2f, 0.8f), "왹져 납치범 A");

        // 2-b. 플레이어 2 (오른쪽 - GREEN)
        CreatePlayerStatus(topPanel.transform, "P2_Green", new Vector2(1, 0.5f), new Vector2(1, 0.5f), new Color(0.2f, 0.8f, 0.2f, 0.8f), "왹져 납치범 B");

        // 2-c. 중앙 웨이브/보스 정보
        GameObject waveObj = new GameObject("Wave_Info");
        waveObj.transform.SetParent(topPanel.transform, false);
        UnityEngine.UI.Text waveText = waveObj.AddComponent<UnityEngine.UI.Text>();
        waveText.text = "WAVE 1\n(보스 등장까지 3트)" ;
        waveText.font = Resources.GetBuiltinResource<Font>("LegacyRuntime.ttf");
        waveText.fontSize = 35; waveText.color = Color.white; waveText.alignment = TextAnchor.MiddleCenter;
        RectTransform waveRect = waveObj.GetComponent<RectTransform>();
        waveRect.anchorMin = new Vector2(0.5f, 0.5f); waveRect.anchorMax = new Vector2(0.5f, 0.5f);
        waveRect.sizeDelta = new Vector2(300, 100);

        // 3. 하단 컨트롤 패널 (재화 표시 및 소환 버튼)
        GameObject bottomPanel = new GameObject("Bottom_Control_Panel");
        bottomPanel.transform.SetParent(hudRoot.transform, false);
        RectTransform bottomRect = bottomPanel.AddComponent<RectTransform>();
        bottomRect.anchorMin = new Vector2(0, 0); bottomRect.anchorMax = new Vector2(1, 0);
        bottomRect.pivot = new Vector2(0.5f, 0);
        bottomRect.sizeDelta = new Vector2(0, 250);
        bottomPanel.AddComponent<UnityEngine.UI.Image>().color = new Color(0f, 0f, 0f, 0.7f); // 반투명 검정

        // 3-a. 인게임 골드 표시 (GameManager 관리)
        GameObject goldObj = new GameObject("Text_InGame_Gold");
        goldObj.transform.SetParent(bottomPanel.transform, false);
        UnityEngine.UI.Text goldText = goldObj.AddComponent<UnityEngine.UI.Text>();
        goldText.text = "💰 1,200";
        goldText.font = Resources.GetBuiltinResource<Font>("LegacyRuntime.ttf");
        goldText.fontSize = 50; goldText.color = new Color(1f, 0.9f, 0.3f);
        goldText.alignment = TextAnchor.MiddleLeft;
        RectTransform goldRect = goldObj.GetComponent<RectTransform>();
        goldRect.anchorMin = new Vector2(0, 0.5f); goldRect.anchorMax = new Vector2(0, 0.5f);
        goldRect.anchoredPosition = new Vector2(50, 0); goldRect.sizeDelta = new Vector2(300, 100);

        // 3-b. 소환 버튼 (중앙)
        GameObject summonBtnObj = new GameObject("Btn_Summon");
        summonBtnObj.transform.SetParent(bottomPanel.transform, false);
        UnityEngine.UI.Image btnImg = summonBtnObj.AddComponent<UnityEngine.UI.Image>();
        btnImg.color = new Color(0.2f, 0.7f, 0.3f); // 녹색 버튼
        UnityEngine.UI.Button btn = summonBtnObj.AddComponent<UnityEngine.UI.Button>();
        RectTransform btnRect = summonBtnObj.GetComponent<RectTransform>();
        btnRect.anchorMin = new Vector2(0.5f, 0.5f); btnRect.anchorMax = new Vector2(0.5f, 0.5f);
        btnRect.sizeDelta = new Vector2(400, 150);
        
        GameObject btnTextObj = new GameObject("Text");
        btnTextObj.transform.SetParent(summonBtnObj.transform, false);
        UnityEngine.UI.Text btnText = btnTextObj.AddComponent<UnityEngine.UI.Text>();
        btnText.text = "👽 왹져 소환 (100 G)";
        btnText.font = Resources.GetBuiltinResource<Font>("LegacyRuntime.ttf");
        btnText.fontSize = 40; btnText.color = Color.white; btnText.alignment = TextAnchor.MiddleCenter;
        RectTransform btnTextRect = btnTextObj.GetComponent<RectTransform>();
        btnTextRect.anchorMin = Vector2.zero; btnTextRect.anchorMax = Vector2.one; btnTextRect.sizeDelta = Vector2.zero;

        Debug.Log("⚔️ [왹져 디펜스] 사진 분위기를 반영한 협동 HUD가 생성되었습니다. 메뉴 1번으로 프리팹화 하세요!");
    }

    // 플레이어 상태창 생성을 위한 헬퍼 함수
    private static void CreatePlayerStatus(Transform parent, string name, Vector2 anchor, Vector2 pivot, Color bgColor, string playerName)
    {
        GameObject pObj = new GameObject(name);
        pObj.transform.SetParent(parent, false);
        RectTransform rect = pObj.AddComponent<RectTransform>();
        rect.anchorMin = anchor; rect.anchorMax = anchor; rect.pivot = pivot;
        rect.sizeDelta = new Vector2(400, 120);
        rect.anchoredPosition = new Vector2(anchor.x == 0 ? 20 : -20, 0); // 좌우 여백
        pObj.AddComponent<UnityEngine.UI.Image>().color = bgColor;

        GameObject textObj = new GameObject("Text_Info");
        textObj.transform.SetParent(pObj.transform, false);
        UnityEngine.UI.Text text = textObj.AddComponent<UnityEngine.UI.Text>();
        text.text = $"{playerName}\nLIFE: 10"; // 웹의 Template Literal처럼 사용
        text.font = Resources.GetBuiltinResource<Font>("LegacyRuntime.ttf");
        text.fontSize = 30; text.color = Color.white; text.alignment = anchor.x == 0 ? TextAnchor.MiddleLeft : TextAnchor.MiddleRight;
        
        RectTransform textRect = textObj.GetComponent<RectTransform>();
        textRect.anchorMin = Vector2.zero; textRect.anchorMax = Vector2.one;
        textRect.sizeDelta = new Vector2(-20, -10); // 안쪽 여백
    }

    [MenuItem("AI Tools/5. [전투] 상하 대칭 세로형 HUD 생성 (도면 반영) 📱")]
    public static void CreateVerticalBattleHUD()
    {
        GameObject canvas = GameObject.Find("Canvas");
        if (canvas == null) { Debug.LogError("⚠️ 씬에 Canvas가 필요합니다!"); return; }

        // 1. 전체 화면을 덮는 투명 루트
        GameObject hudRoot = new GameObject("UI_Vertical_HUD");
        hudRoot.transform.SetParent(canvas.transform, false);
        RectTransform rootRect = hudRoot.AddComponent<RectTransform>();
        rootRect.anchorMin = Vector2.zero; rootRect.anchorMax = Vector2.one;
        rootRect.sizeDelta = Vector2.zero;

        // 2. 상단 패널 (다른 사용자 정보)
        GameObject topPanel = new GameObject("Top_Opponent_Panel");
        topPanel.transform.SetParent(hudRoot.transform, false);
        RectTransform topRect = topPanel.AddComponent<RectTransform>();
        topRect.anchorMin = new Vector2(0, 1); topRect.anchorMax = new Vector2(1, 1);
        topRect.pivot = new Vector2(0.5f, 1);
        topRect.sizeDelta = new Vector2(0, 120);
        topPanel.AddComponent<UnityEngine.UI.Image>().color = new Color(0.8f, 0.2f, 0.2f, 0.8f); // 적군 느낌의 붉은색

        GameObject oppTextObj = new GameObject("Text_Opponent");
        oppTextObj.transform.SetParent(topPanel.transform, false);
        UnityEngine.UI.Text oppText = oppTextObj.AddComponent<UnityEngine.UI.Text>();
        oppText.text = "👾 파트너 (Other User)";
        oppText.font = Resources.GetBuiltinResource<Font>("LegacyRuntime.ttf");
        oppText.fontSize = 40; oppText.color = Color.white; oppText.alignment = TextAnchor.MiddleCenter;
        RectTransform oppTextRect = oppTextObj.GetComponent<RectTransform>();
        oppTextRect.anchorMin = Vector2.zero; oppTextRect.anchorMax = Vector2.one;
        oppTextRect.sizeDelta = Vector2.zero;

        // 3. 하단 패널 (내 영역 및 유닛 소환 버튼)
        GameObject bottomPanel = new GameObject("Bottom_My_Panel");
        bottomPanel.transform.SetParent(hudRoot.transform, false);
        RectTransform bottomRect = bottomPanel.AddComponent<RectTransform>();
        bottomRect.anchorMin = new Vector2(0, 0); bottomRect.anchorMax = new Vector2(1, 0);
        bottomRect.pivot = new Vector2(0.5f, 0);
        bottomRect.sizeDelta = new Vector2(0, 200);
        bottomPanel.AddComponent<UnityEngine.UI.Image>().color = new Color(0.1f, 0.1f, 0.1f, 0.9f); // 내 영역 어두운 회색

        // 3-a. 유닛 소환 버튼 (도면의 '유닛 소환')
        GameObject summonBtnObj = new GameObject("Btn_Summon_Unit");
        summonBtnObj.transform.SetParent(bottomPanel.transform, false);
        UnityEngine.UI.Image btnImg = summonBtnObj.AddComponent<UnityEngine.UI.Image>();
        btnImg.color = new Color(0.9f, 0.9f, 0.9f); // 도면처럼 밝은 버튼
        UnityEngine.UI.Button btn = summonBtnObj.AddComponent<UnityEngine.UI.Button>();
        
        RectTransform btnRect = summonBtnObj.GetComponent<RectTransform>();
        btnRect.anchorMin = new Vector2(0.5f, 0.5f); btnRect.anchorMax = new Vector2(0.5f, 0.5f);
        btnRect.sizeDelta = new Vector2(500, 120); // 크고 누르기 쉽게

        GameObject btnTextObj = new GameObject("Text_Summon");
        btnTextObj.transform.SetParent(summonBtnObj.transform, false);
        UnityEngine.UI.Text btnText = btnTextObj.AddComponent<UnityEngine.UI.Text>();
        btnText.text = "유닛 소환"; // 도면 텍스트와 동일
        btnText.font = Resources.GetBuiltinResource<Font>("LegacyRuntime.ttf");
        btnText.fontSize = 50; btnText.color = Color.black; btnText.alignment = TextAnchor.MiddleCenter;
        RectTransform btnTextRect = btnTextObj.GetComponent<RectTransform>();
        btnTextRect.anchorMin = Vector2.zero; btnTextRect.anchorMax = Vector2.one;
        btnTextRect.sizeDelta = Vector2.zero;

        Debug.Log("📱 [왹져 디펜스] 상하 대칭형 전투 화면 UI가 생성되었습니다.");
    }
}