# 로비 네온 컬렉션 UI 검증 (2026-09-07)

## 범위
- 사용자 지정 디자인 작업: SampleScene 내 유닛 목록 4열, 전체 48종 표시, 미보유 20종의 어두운 카드/미보유 문구.
- 청록/보라 절삭 모서리 프레임을 유닛 카드, 강화 상세, 교배, Quest/Achievement 팝업에 적용.
- 현재 유닛 일러스트가 없으므로 등급 문자 placeholder 사용. 실제 일러스트 최종 적용/사용자 미감 인수는 별도다.
- Battle Scene, 강화/교배 경제 정책 변경 없음. 기존 미커밋 Quest 작업과 사용자 로컬 변경 보존. 후속 오류 수정에서 미등록 HTTP 경로의 응답만 500에서 404로 바로잡았다.
- 후속 요청: 작은 유닛 카드 테두리에 Normal 흰색 / Epic 보라 / Unique 노랑 / Legendary 초록 / Mythic 로즈 레드를 저채도 발광으로 적용. 큰 프레임과 초상화 영역 색상은 유지.
- 내 유닛 탭 재료 표시를 DNA/성장세포 절차적 아이콘과 숫자로 변경하고 각각 `+` 구매 안내 팝업을 연결.

## 결함 및 수정
- 첫 실행에서 `LobbyNeonFrame`에 CanvasRenderer가 없어 MissingComponentException 발생.
- LobbyNeonGraphic의 RequireComponent와 Builder의 기존 프레임 Renderer 보정으로 수정.
- 동적 교배 SlotsPanel의 자식 정리로 장식 프레임이 삭제되는 문제는 목록 자체 배경과 바깥 프레임을 분리해 수정.
- 단순 데이터/컴포넌트 테스트만으로 첫 오류를 발견하지 못했으므로 실제 6종 prefab의 Renderer 존재 및 Canvas Rebuild 검사를 추가.

## 자동 검증 및 독립 리뷰
- 전용 LobbyCollectionUiTests: 19/19 PASS (등급별 색상 및 재료 아이콘/중복 생성 방지/구매 안내 테스트 포함).
- Unity 전체 EditMode: 534/534 PASS, failed/skipped 0 (job `a69093bcef8b491981c1beb66f71623f`).
- 서버 오류 회귀 전용: 5/5 PASS. 기본 프로필 Quest 404, local 정상 API 200/미등록 경로 404, prod 개발용 Controller 차단 확인.
- `compileJava test balanceToolTest`: BUILD SUCCESSFUL. 서버 376/376, BalanceTool 82/82, failed/error/skipped 0.
- SampleScene 직접 배치 프레임 8개: Renderer 누락 0.
- PlayMode 전체 프레임: Renderer 누락 0.
- 독립 읽기 전용 재리뷰: PASS, 추가 차단 결함 없음.
- C# `git diff --check`: PASS. 전체 diff는 Unity가 저장한 prefab/Scene의 빈 값 뒤 trailing whitespace로 경고가 남는다. YAML 직접 수정 금지 규칙을 지켜 수동 정리하지 않았다.

## 실제 화면 조작 검증
Windows computer-use 실제 클릭/스크롤과 Unity MCP 읽기 검사를 조합했다. 데이터 확인만으로 화면 조작을 대체하지 않았다.

1. 내 유닛 탭 클릭: 4열 카드 및 상단 교배 진입 표시.
2. 보유 카드 클릭/뒤로: 기존 상세/강화 UI 정상 표시, 조각 부족 상태 유지.
3. 목록 스크롤: Normal~Legend 및 미보유 Mythic까지 순서대로 표시.
4. 미보유 Mythic 클릭/뒤로: 상세 열림, 미보유 안내와 강화 차단 유지.
5. 런타임 집계: 카드 48개, 미보유 20개, grid constraintCount=4.
6. 교배 팝업 열기/닫기/재진입: 슬롯 3개, 바깥 프레임, slot 배경 alpha 0.92 유지.
7. 메인 탭 복귀: 유닛 목록/전용 재료 표시 숨김.
8. Quest 팝업: 일일/주간/업적 탭 전환 및 닫기 정상.
9. 검증용 서버로 전환한 이후 신규 Unity error 0.
10. 후속 실제 클릭: DNA `+` → DNA 아이콘/제목 구매창 → 닫기, 성장세포 `+` → 성장세포 아이콘/제목 구매창 → 닫기 정상. 구매 버튼은 정책 미확정으로 비활성화.
11. 후속 실제 스크롤: Epic 보라 / Unique 노랑 / Legendary 초록 / 미보유 Mythic 어두운 로즈 테두리 확인. Normal 흰 테두리는 첫 페이지에서 확인. 후속 UI 조작 중 신규 Unity error 0 (진입 시 기존 8080 Quest 오류는 아래 원인으로 별도).

## 환경 이슈 및 남은 확인
- 기존 사용자 서버 `localhost:8080`의 GET `/api/quests?username=sh1`은 HTTP 500 / INTERNAL_SERVER_ERROR를 반환했다. IntelliJ 실제 Run 콘솔에서 `NoResourceFoundException: No static resource api/quests.`를 확보했다. 기존 `DefenseApi` 실행 설정에 local/dev 프로필이 없어 `@Profile({local, dev})` QuestController가 등록되지 않은 것이 라우팅 실패 원인이며, generic Exception handler가 404를 500으로 오분류했다.
- NoResourceFoundException/NoHandlerFoundException 전용 404 handler와 RESOURCE_NOT_FOUND 응답을 추가했다. 기본/prod에서 개발용 Quest API를 계속 차단한다.
- 공유 IntelliJ 실행 설정 `.run/DefenseApi Local.run.xml`을 추가했다. `local` 프로필 및 `127.0.0.1` 바인딩을 명시한다. 기존 실행 설정/프로세스는 수정하지 않았다.
- 현재 소스의 별도 Spring 프로세스(`local`, 포트 18080, 별도 메모리 H2)에서는 같은 요청 HTTP 200이며 팝업 렌더링 정상.
- Unity NetworkManager.BaseUrl은 PlayMode에서만 18080으로 일시 변경했다. Scene/환경설정에 저장하지 않았으며 PlayMode 종료로 원복된다.
- 8080 사용자 프로세스는 종료/변경하지 않았다. 새 코드와 `DefenseApi Local` 설정을 적용하려면 재시작해야 한다. H2 메모리 테스트 데이터가 초기화되므로 사용자 승인을 요청한 상태다. 현재 실행 중인 8080의 오류까지 해소되었다고 판정하지 않는다.
- 재료 다이아 구매는 판매 수량/가격/일일·주간 한도 정책 미확정이다. `+` 안내 UI만 구현했고 실제 구매 API·재화 차감·지급은 구현하지 않았다. 클라이언트 임의 지급도 없다.
- 이번 검증은 시각/탐색 회귀이며 재화 소비, 교배 시작·즉시완료·보상 수령을 새로 검증했다는 의미가 아니다.
- 최종 사용자 디자인 인수는 대기. stage/commit/push 미수행.

## 후속: 목록 여백 및 상단 HUD 통일 (2026-09-07)

- 요청 범위: 컬렉션 좌우 벽과 카드 사이 여백, DNA/성장세포 테두리, 레벨·하트·골드·젬의 통일된 네온 디자인.
- 좌우 padding 10 → 36, 4열·48종·등급별 테두리 유지. 폭에 맞춰 카드 크기를 재계산한다.
- 재료 카드는 남색 바탕·청록/보라 코너 회로 프레임을 사용한다. 원래 아이콘·수량·`+` 연결 유지.
- 상단 상태바는 상대 anchor 배치로 통일하고 기존 TMP 참조, Button 리스너, Slider 값·fill/handle 참조·onValueChanged를 보존한다. 새 재화 지급/경제 규칙은 없다.
- 기존 Gem에 sprite가 없어 청록 다이아몬드 모양의 임시 아이콘으로 표시한다. 하트·골드의 기존 sprite는 유지한다.
- 실제 화면에서 중첩 Gold/Gem 프리팹의 자식 reparent 실패로 폭이 0이 되는 문제를 발견했다. `tab_bar` 내부 해당 Scene 인스턴스만 Unity PrefabUtility로 unpack하고 재배치했다. 원본 프리팹/Battle Asset은 수정하지 않았다.
- 이를 탐지하는 실제 SampleScene 부모/폭/프레임/버튼 검사 및 헤더 반복 적용·값/이벤트 보존 테스트를 추가했다. Scene 테스트는 이미 로드된 경우 메모리 Scene을 읽으므로 Editor API 저장 후 실행했다.
- Unity 전체 EditMode **536/536 PASS**, failed/skipped 0. job `a0d083163b3a497a8c7a290112ea27c4`. 전용 20/20 통과 후 실제 Scene 검사 1건을 더 추가하여 전체 회귀에 포함했다.
- 독립 읽기 전용 재리뷰 PASS, P0/P1 지적 없음. 사람의 최종 미감/다양한 화면비 인수는 계속 대기.
- 실제 Game View(1080×1920) 내 유닛 탭 클릭으로 4열 좌우 여백·상단 실제 계정값·재료 프레임 표시 확인. DNA `+` 구매창 열기/닫기 및 성장세포 `+` 진입 확인. PlayMode 신규 error 0.
- 기존 Quest API는 이번 확인 시 HTTP 200이었다. 서버 재시작/데이터 초기화는 이번 디자인 작업에서 수행하지 않았다.
- C#/Markdown diff check PASS. 기존 Unity 생성 YAML trailing whitespace 경고는 별도이며 직접 편집하지 않는다. 커밋/푸시 없음.

## 후속: Quest 겹침·닫기 및 컬렉션 클리핑 (2026-09-08)

- Quest header 상단의 55 여백을 제거하고 고정 헤더(0~110), 탭(120~188), 활동도, 보상 구간(280~420), 목록(450~)을 분리했다. 업적 목록은 210부터 시작한다. Builder와 런타임이 같은 상수를 사용한다.
- 기존 보상 구간은330~480인데 목록이425부터 시작해 겹쳤다. 활동도 Slider의 잘못된 stretch/position도 top-band offsets로 수정했다.
- 사용자 닫기 실패는 현재 상태에서 동일 재현되지 않았다. 수정 전 중앙 실제 클릭으로 닫혔다. 닫기 영역을150×86으로 확대하고 헤더를 마지막 sibling으로 두어 가림을 예방했다. 수정 후 중앙 및 우측 가장자리 실제 클릭, 재진입 후 닫기 모두 정상. 원인 확정 해결로 과장하지 않는다.
- `SetBusy(true)` 상태의 포인터 판정과 실제 Controller.Close 리스너 실행 회귀 테스트를 추가했다. 실제 네트워크 지연·응답 경쟁까지 자동 검증했다는 의미는 아니다.
- 첫 raycast 테스트는 신규 Canvas가 렌더링되기 전 검사해 실패했다. UnityTest에서1프레임 렌더링 후 검사하도록 테스트 수명주기를 수정했다. 실제 화면 입력 검증으로 별도 확인했다.
- 재화 `+` 우측 여백 확보: 상단 anchorMax.x=.94, 재료=.93. 숫자 영역도 축소해 버튼과 분리.
- 컬렉션 전용 CollectionViewport/RectMask2D를 만들어 위18/아래30만큼 프레임 안쪽에서 클리핑한다. 실제 첫 페이지와 마지막 Mythic20까지 스크롤했을 때 카드가 아래 테두리 밖으로 나오지 않음.
- 기존 로컬 `Space_Exploration_GUI_Kit/Icons/gem-1-64.png`를 젬 아이콘으로 연결. 원본 프리팹 Gem의 sprite는 비어 있었고 PNG는 Default Texture였다. Unity TextureImporter로 Sprite Single로 변경하여 참조했으며 `.meta` 수동 작성 없음.
- 전용29/29 및 전체 EditMode537/537 PASS, failed/skipped0. 전체 job `c27ac1a2cafc4122b1bf4d19a5af294c`.
- 독립 리뷰: 코드 P0/P1 없음, Approve(실제 화면비/네트워크 요청 수명주기 검증 한계 WARNING).
- 실제 PlayMode 조작: Quest 열기→일일→주간→업적→스크롤→닫기→재진입→닫기 가장자리, 내 유닛→목록 끝 스크롤. 신규 Console error0. 요청 시 사용 중이던 PlayMode는 수정 후 재실행해 확인했다.
- 경제·서버·Battle·Shared 계약 변경 없음. C#/MD diff check PASS. 커밋/푸시 없음. 최종 사용자 디자인 인수 대기.

## 후속: 재화 아이콘 선명도·바 비율·정사각형 버튼 (2026-09-08)

- 하트는 선명한 핑크, 코인은 따뜻한 골드 tint로 조정했다. 기존 sprite와 젬 sprite는 유지한다.
- 상단 하트/코인/젬 폭을 기존 코인과 같은 anchor 폭 .19로 통일했다. 프로필 폭을 소폭 줄이고 세 바의 아이콘·숫자·우측 여백 비율을 동일하게 유지했다.
- 상단 3개와 DNA/성장세포 2개 `+`에 AspectRatioFitter(WidthControlsHeight, 1:1)를 적용했다. 런타임 실측은 상단 34.884×34.884, 재료 37.40001×37.40001이다. 기존 리스너·수량·구매 정책은 변경하지 않았다.
- 전용 Lobby 검사 21/21 PASS. 전체 실행에서 기존 Quest 포인터 검사가 화면 렌더링 타이밍 때문에 hits empty로 실패했다. 테스트용 Camera/RenderTexture로 명시적으로 렌더링하도록 보완했고 실제 raycast 및 닫기 핸들러 검증은 유지했다.
- 보완 후 전체 EditMode 537/537 두 번 연속 PASS, failed/skipped0. jobs `999bd6f1d78c402e9359396ba39588b5`, `bb77d8414dde4d2d8e3017493a54807e`. 비포커스/포커스 Editor 모두 통과했다. `-nographics` 환경은 검증하지 않았다.
- 독립 읽기 전용 리뷰 및 테스트 보완 재리뷰 PASS, 필수 수정 없음.
- 실제 Windows 화면 조작: 내 유닛 탭→DNA `+`→닫기→성장세포 `+`→닫기. 정사각형 표시·우측 여백·상단 세 바 확인, Console error0. 내 유닛 화면 PlayMode를 유지했다.
- 커밋/푸시 없음. 최종 색감·다양한 화면비·긴 사용자명 디자인 인수는 사용자 확인 대기.

## 후속: 빨간 하트·수량 우측 정렬·미해금 미스틱 구역 (2026-09-08)

- 하트 색상 RGB(1,.20,.25), 세 재화 숫자 MidlineRight 및 오른쪽 anchor .70, `+` 시작 .77로7% 간격 확보.
- 기존 하트는 원본 프리팹 연결에서 색상/배경 override가 저장되지 않아 Play 재진입 시 흰색으로 복원됨을 확인했다. 상단 bar 내부 해당 Scene 인스턴스를 다른 재화와 같이 unpack한 뒤 Unity API 저장했다. 원본 패키지 프리팹은 수정하지 않았다. Play 재진입에서도 빨간 하트·동일 네온 배경 유지 확인.
- MainSectionAlienIds는 모든 하위 등급 및 보유 Mythic만 포함하고, LockedMythicAlienIds를 별도 헤더 아래 배치한다. 기존 owned 값을 사용하며 클라이언트가 보유 상태를 꾸며내지 않는다.
- LobbySectionGridLayout은4열 카드 크기·간격을 유지하고 미완성 보유 행을 종료한 뒤 전체 폭76높이의 미해금 헤더를 배치한다. 해금 시 보유 구역으로 이동하며 전부 해금하면 헤더도 사라진다.
- 새 반복 생성 EditMode 테스트에서 foreach+DestroyImmediate 자식 건너뜀을 발견해 역순 삭제로 수정했다. 기존 Runtime 지연 삭제에서는 먼저 비활성화하는 정책 유지.
- 서버 StarterAlienCollectionService는 local/dev 및 명시 테스트flag의 이중 조건으로ID29/30만 Lv1·조각0 지급한다. prod/production 혼합 프로필은 거부. 기존 레벨·조각 보존, 반복 멱등, 나머지18종 미해금 유지. 운영 튜토리얼 완료 보상 연동은 후속이며 이 프리뷰를 운영 무조건 지급으로 사용하지 않는다.
- 자동검증: Unity 전체542/542 PASS(job `23ec421a74e14017918f938ee2672ef4`), 서버 전용19/19·전체391/391 PASS. 컴파일 오류0. 새 UI검사에는640/940/1200폭, 보유Mythic0/2/4/7종, 반복 생성, 전체해금시헤더제거가 포함된다. 독립 리뷰 발견사항 수정 후 PASS.
- 실제 검증은 기존8080(PID2956)를 유지하고 별도 local 서버18080(PID48976, DB `lobby_preview_18080`)를 사용했다. 로그 `.tmp/lobby-preview-18080-20260908-002408.out.log`. 서버응답에서29/30 owned=true,31 owned=false 확인.
- Unity PlayMode에만 BaseUrl을 `http://localhost:8080/api` → `http://127.0.0.1:18080/api`로 바꿨다. Scene/설정파일에 저장하지 않았으므로 PlayMode를 종료하면 원래 주소로 복귀한다. 사용자 확인을 위해 별도 서버와 미스틱 목록 화면을 켜 둔다. 별도 서버의 테스트 진행은 기존8080 DB에 반영되지 않는다.
- 실제 클릭: 내유닛→스크롤→보유Mythic1 상세(Lv1)→뒤로→미해금Mythic3 상세(획득안내/강화차단)→뒤로→목록마지막Mythic20. 카드48개·보유30개 뒤 헤더1개·미해금18종, 하단클리핑 정상, Console error0.
- 기존 서버에 새 테스트 지급을 반영하려면 서버 재시작이 필요하며 H2 메모리 진행 초기화 가능성에 대해 사용자 확인 대기. 종료 필요 시 PID48976 명령행/포트 확인 후 별도 서버만 종료한다. 커밋/푸시 없음, 최종 사용자 UI 인수 대기.

## 최종 시안 적용 (2026-09-08)

### 구현 및 경계

- MCP 연결: `E:/study/MyDefenseGame/Client`, `Assets/Scenes/SampleScene.unity`. EditMode에서 전용 `LobbyFinalUiBuilder.Apply()`로 Scene/Prefab을 저장했다. YAML/메타 직접 편집 없음.
- 프로필은 기존 TMP/Image/Slider 참조, 이름·레벨·XP 표시, 프레임 색상·글꼴·RectTransform 및 tab_bar/Canvas 부모 배치를 유지했다. 적용 전후 전체 표시/배치 fingerprint 동일. serialized 비교도 새 `finalStyle=false` 필드가 추가된 것 외에 기존 프로필 속성은 동일하다.
- 재화 3개 고정 열, 수량 우측 정렬/자동 글자 맞춤/버튼 간격. DNA·성장세포를 145 높이의 공통 2행 패널로 묶고 정확히 `왹져DNA`, `성장세포` 표시. 신화 교배도 같은 높이/위치이며 기존 실제 슬롯 안내/진입 연결 유지.
- 교배 완료 배지와 화살표가 겹치는 리뷰 지적을 수정했다. 제목·배지·화살표를 별도 열로 두고 readyCount 0/1/3 회귀 검사를 추가했다.
- 배경은 첨부 PNG 원본과 SHA256 동일. RawImage + EnvelopeParent로 aspect-fill/crop, raycastTarget=false. 이미지 한 장으로 UI를 덮지 않는다.
- 4열 및 카드 높이/폭 1.42 유지, 실제 목록 48개/보유·미해금 구역 유지. 제목은 `내 유닛`, 보유 라벨은 숨김. 등급 테두리/등급명/구분선은 NORMAL #DCE5E8, EPIC #BD65F2, UNIQUE #F5C451, LEGEND/LEGENDARY #42E887, MYTHIC #FF4F83. 이름/수량은 흰색.
- 기존 portrait Sprite는 보존하고 없을 때만 기존 문자 fallback을 쓴다. 현재 테스트 데이터/프리팹에는 실제 portrait가 없어 N/E/U/L/M이 보인다. 샘플 유닛/수량을 하드코딩하지 않았다. 미해금 requiredPieces=0은 임의 5를 만들지 않고 `실제수량/—`로 표시한다.
- 하단 5개 기존 OpenTab 이벤트를 유지하고 SF 상자/원본 마스코트/교차검/사람 방패/노드 아이콘 및 실제 선택 상태의 컬러·밑줄을 연결했다. 비선택 마스코트는 saturation shader로 회색 표시. 안전 영역만큼 메뉴와 목록 하단을 함께 올린다.
- 짧은 화면의 서브픽셀 선 끊김을 발견하여 새 finalStyle 외곽선만 화면 기준 최소 1.25px로 보완했다. 프로필 기존 스타일에는 적용하지 않는다.

### 변경 파일

- 신규: `Client/Assets/Editor/LobbyFinalUiBuilder.cs`, `Client/Assets/Scripts/Ui/LobbyFinalPresentation.cs`, `LobbyNavIconGraphic.cs`, `LobbyIconSaturation.shader`, `Client/Assets/Editor/Tests/LobbyFinalUiTests.cs`.
- 변경: `LobbyManager.cs`, `MythicBreedingController.cs`, `UnitCardUI.cs`, `LobbyNeonGraphic.cs`, `LobbyCollectionUiTests.cs`.
- Unity API 저장: `SampleScene.unity`, `Prefabs/Lobby/UnitCard.prefab`, `Resources/Prefabs/Lobby/MythicBreeding/MythicBreedingShortcut.prefab`.
- 원본 에셋: `Client/Assets/Art/Lobby/Final/SpaceshipBackground.png`, `WakjeoMascot.jpg` 및 Unity 생성 meta. 별도 원본 JPG와도 SHA256 동일.
- 문서: 이 기록과 `98_IMPLEMENTATION_TASKS.md`. 기존 미커밋 서버/Balance/Quest 변경은 이번 UI 작업 변경으로 집계하지 않는다.

### 검증

- Unity 컴파일 error 0. 전체 EditMode **555/555 PASS**, failed/skipped 0. 최종 job `c7d1b3fe22e749b08369db154f395c83`.
- 테스트: 프로필/재화 값·리스너 보존, 정확 HEX, 2행 열 정렬, 기존 portrait/실제 수량, 선택 탭, 0/1/3 완료 배지 비중첩, safe inset 0/45/120, 작은 프레임 최소 픽셀 두께.
- 독립 읽기 리뷰: 배지 겹침 수정 후 PASS, 교차검 및 최소 픽셀 두께 후속도 PASS. 구현 담당과 리뷰 담당 분리.
- 실제 Windows UI 클릭: 내 유닛, DNA + 및 닫기, 성장세포 + 및 닫기, 신화 교배 진입/뒤로, NORMAL 1 카드 상세/뒤로, 보유 Mythic·미해금 구역까지 스크롤, 마지막 MYTHIC 20 아래 여백, 상점/클랜/콘텐츠와 선택 밑줄 확인. 전투 탭은 로비 기본 진입 화면으로 확인했다(실제 Battle 진입 테스트 아님).
- 상단 + 3개는 실제 EventSystem raycast 최상위 대상임을 확인했다. 기존 persistent listener는 모두 0개이므로 구매 기능 PASS로 판정하지 않는다. 재료 +는 기존 안내 창만 열고 실제 구매는 정책 미정으로 비활성이다.
- GameView 1080×1920, 1080×2340, 1080×1440에서 실제 렌더 캡처 확인. 배경 crop, 카드 비율, 고정 상단/하단, 스크롤 클리핑 확인. 4:3 프로필 이름 누락 의심은 재확인 시 정상 sh1 렌더링으로 확인되어 프로필 변경하지 않았다. 물리 기기 터치/노치·가로 화면은 미검증.
- 검증 API는 기존 별도 local 서버 `127.0.0.1:18080/api` 사용. UI 탐색 GET만 수행하며 구매/강화/교배 시작·즉시완료·보상 수령은 수행하지 않았다. 이번 작업은 Spring/Balance/Battle 로직을 변경하지 않았다.
- 전체/상단/화면비 증거: `.tmp/final-lobby-ui/collection-1080x1920.png`, `collection-top.png`, `collection-1080x2340.png`, `collection-1080x1440.png` (로컬 검증 산출물).
- 최종 전달용 전체 캡처는 `collection-final.png`에도 보관했다. 검증에만 사용한 Unity 프로세스의 `MYDEFENSE_API_BASE_URL` 환경변수는 원래 값(null)으로 복구했으며 현재 Play 세션의 프리뷰 주소만 18080을 유지한다. 내 유닛 목록을 맨 위로 복원했다.

### 남은 승인·검증

- 이미지 편집 스킬/built-in imagegen으로 원본의 흰 배경만 제거하도록 요청했으나 생성 결과가 원본 그림을 바꿔서 프로젝트에 채택하지 않았다. 사용 프롬프트 요지: `background-extraction; change only white background to transparent alpha; preserve original green mascot drawing, eyes, pose, antenna and yellow zigzag exactly; no redesign`.
- 원본 JPG를 그대로 사용 중이다. 가장자리와 연결된 흰 배경만 Unity에서 비파괴 투명 처리하는 방법은 사용자 승인 대기이며, 흰 사각 배경은 아직 남아 있다. 사용자가 별도 투명 원본 PNG를 제공해도 교체 가능하다.
- 재화 판매 정책/API, 실제 기기 검증 및 최종 사용자 디자인 인수는 이번 UI 자동 검증으로 완료 처리하지 않는다. 상태는 검증 대기, 커밋/푸시 없음.

## 제공 하단 아이콘·마스코트 투명 배경 적용 (2026-09-08)

- 사용자 제공 `D:/users/게임에셋`의 상점/홈(교차검)/클랜/콘텐츠 활성·비활성 8개 PNG를 `Assets/Art/Lobby/Final/Navigation`에 복사했다. 8개 모두 SHA256이 원본과 동일하다.
- `LobbyNavigationAssetBuilder`의 nav-only 적용으로 SampleScene의 기존 Button/OpenTab 이벤트와 배치를 유지하고 실제 선택 탭에 따라 Sprite를 교체한다. 기존 전체 디자인 메뉴의 재적용도 새 아이콘을 유지한다.
- 마스코트 흰 배경 제거는 이번 사용자 지시로 진행했다. 원본 JPG는 그대로 두고 외곽에 연결된 밝은 무채색 영역의 알파만 제거한 `WakjeoMascot.png`를 생성했다. RGB·눈 안의 흰 하이라이트는 보존한다. JPG RGB24로 인한 알파 손실을 막기 위해 별도 RGBA32 텍스처로 출력한다.
- 비선택 마스코트는 실제 런타임 Shader `_Saturation=0`, 선택 시 1이며 원본 초록색으로 표시한다. 다른 네 탭은 제공받은 활성/비활성 원본 그림을 그대로 사용한다.
- 코드 변경: `LobbyFinalPresentation.cs`, `LobbyFinalUiBuilder.cs`; 신규 `LobbyNavigationAssetBuilder.cs`, `LobbyNavigationAssetTests.cs`. Unity API로 SampleScene 및 Sprite import 설정을 저장했다. YAML/meta 수동 편집 없음. 프로필 적용 전후 직렬화 비교 동일. Battle/Shared/서버/재화 로직 변경 없음.
- MCP 도구 직접 노출은 없었으나 이미 실행 중인 `127.0.0.1:8081/mcp`의 표준 JSON-RPC로 접속했다. 연결 프로젝트 `E:/study/MyDefenseGame/Client`, 인스턴스 `Client@0689e9f51f1b7464`, SampleScene 저장 상태 확인 후 적용.
- 전용 테스트 5/5 PASS (`d5289f654832493a9e6546594fb3b606`), 전체 EditMode 560/560 PASS (`718e6f3b4aad42ec8c25edd01c4a52cb`), 실패/스킵0. 독립 읽기 전용 리뷰 PASS.
- 실제 Apply 2회 뒤 Sprite GUID/localID 동일, 누락 Sprite 0, PNG 투명 픽셀 98,716 확인. Unity 컴파일/실제 탐색 후 Console error0.
- 실제 Windows 클릭: 내 유닛→상점→클랜→콘텐츠→전투→내 유닛. 활성 그림/밑줄 및 마스코트 초록/회색 전환 확인. 전투는 로비 탭 전환만 확인했으며 Battle 세션 시작은 하지 않았다. 최종 내 유닛 화면 PlayMode 유지.
- 캡처: `.tmp/final-lobby-ui/nav-unit-final.png`, `nav-unit-bottom.png`, `nav-battle-bottom.png`. 이번에는 1080×1920 Editor 화면에서 확인했으며 실제 기기 및 EXE 재빌드는 수행하지 않았다.
- 사용자 최종 디자인 인수 대기. 커밋/푸시 없음.

## 카드 선 축소 표시 누락 보완 (2026-09-08)

- 사용자 표시 V 영역의 카드 우측/하단 및 구분선을 코드로 조사했다. 8각 Ring은 이미 닫힌 대칭 도형이나, 220px 카드/Canvas1/0.37배 미리보기에서 기존 외곽 약0.678px·내곽0.317px·구분선0.452px로 축소되어 픽셀 위치에 따라 선이 누락돼 보일 수 있었다.
- `LobbyNeonGraphic`의 `finalStyle && Card`에만 외곽·내곽·수량 구분선 최소3 Canvas 픽셀을 적용했다(0.37배 표시에서1.11px). 색상/alpha/좌표는 유지했다. Panel/Button의 기존 외곽 최소1.25px와 내곽두께는 보존하며 기존 프로필 분기도 변경하지 않았다.
- 코드 변경은 `LobbyNeonGraphic.cs`, `LobbyFinalUiTests.cs`. Scene/Prefab/메타/에셋/경제/게임 데이터 변경 없음.
- 자동 테스트17케이스 추가: 실제 mesh8변폐쇄·좌우/상하대칭, 수량구분선위치, 축소배율/픽셀위상별CPUcoverage, 비카드두께회귀. Unity 전체577/577 PASS, failed/skipped0, job `617194a47d234cb0b858909fa0b28053`. 컴파일error0, 독립 재리뷰PASS, scoped diffcheckPASS.
- 기존 프로필형 legacy Panel(360×94) mesh triangle stream SHA256은 전후 `9A60771043C9B14CBF8E564FEEE6F3B2AA037A4374461F17B2312E25AF09A89E`로 동일했다.
- 이번 사용자 지시에 따라 화면 조작·스크린샷·실제 GPU 표시 검토는 생략했다. CPU기하/커버리지 테스트를 실제 화면 인수PASS로 간주하지 않는다. Unity EditMode 유지, 커밋/푸시 없음.

## 레전더리 7종 컬렉션 일러스트 적용 (2026-09-08)

- 사용자 승인 순서: LEGEND 1 불꽃형, 2 쌍검형, 3 골렘형, 4 팔 대포형, 5 보라 구체형, 6 붉은 칼날형, 7 초록 부유형. 실제 canonical ID는 1~7이며 이름/등급/게임 데이터는 변경하지 않았다.
- `Assets/Art/Lobby/Units/Legendary` PNG 7종은 제공 원본과 SHA-256이 동일하다. 전체 정사각 캔버스와 alpha를 유지하고, Sprite import는 512px/중앙 pivot/FullRect/no mipmap으로 설정했다. 원본1254px은 그대로 보관하며 런타임 텍스처는 합계 약7MiB다.
- 신규 `LobbyUnitPortraitCatalog.cs`, `Editor/LobbyUnitPortraitAssetBuilder.cs`, `Editor/Tests/LobbyUnitPortraitTests.cs`, `Resources/Lobby/UnitPortraitCatalog.asset` 및 Unity 생성 meta. `UnitCardUI.cs`에 ID+등급별 Sprite 연결과 preserveAspect를 추가했다. 카드 재바인딩 시 이전 이미지가 남지 않으며 미매핑 카드의 기존 Sprite/문자 폴백을 보존한다.
- Unity MCP는 `127.0.0.1:8081/mcp` 표준 HTTP 연결로 실제 `E:/study/MyDefenseGame/Client/Assets`를 확인했다. Play 종료 후 AssetDatabase/TextureImporter로만 에셋을 생성했다. YAML/meta 수동 편집 없음.
- Scene/Prefab 변경 없음: SampleScene SHA-256 `6FF90AB9451A4777A32DEC702192841EBD82D04D300ECD856DD0254E90453AA4`, UnitCard.prefab `F8FB183D078D5CC276544D2F013D3FA4E458C7252940FF3DEF373BC84A84B8F3` 전후 동일. 프로필·프레임·경제·Battle·Shared 변경 없음. 상세창의 기존 placeholder는 이번 범위 밖으로 유지했다.
- 전용10케이스와 최종 전체 EditMode **587/587 PASS**, failed/skipped0. 최종 job `f23cb59c4ce64e978d92f49242ff3c7f`. 독립 읽기 전용 리뷰PASS, 필수수정 없음.
- 실제 Windows UI에서 내 유닛 진입 → 레전더리 구역 스크롤 → 7종 전체 실루엣/투명 배경/이름/수량 확인 → LEGEND 1 클릭 및 올바른 상세 진입 → Back 복귀를 확인했다. 1080×1920 GameView, 신규 Console error0. 구매/강화 등 영구데이터 쓰기는 수행하지 않았다.
- 화면 증거: `Client/_localbuild/UIValidation/legendary-collection.png`. 실제 기기·다른 화면비·EXE 재빌드는 이번에 수행하지 않았다. 기존 SampleScene을 사용해 검증했으며 신규 테스트 Scene/Build 항목은 추가하지 않았다.
- 사용자 최종 디자인 인수 대기. 현재 PlayMode의 레전더리 목록 구역을 열어 두었으며 커밋/푸시 없음.
