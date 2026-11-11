# UI-MIDNIGHT — 미드나잇 관측소 로비

## 상태와 범위

- 담당: User/System. 사용자 선택 시안에 따른 표현 계층 변경.
- 상태: **사람 인수 대기**. 최신 `full-v5` 컴파일 성공, 전체 **857개 중 856 PASS**, 전용 **30/30 PASS**. 실패 1건은 기존 Battle canonical hash 불일치다. 독립 최종 리뷰가 최신 실행 결과와 최종 메인 캡처를 직접 확인하여 **PASS·Approve·필수 수정 0**을 받았다. 계정 연동/Quest 겹침, 재화 + 라벨 누락, 실제 RESOURCE 상품 3개 배치 문제는 수정 후 화면 재검증 PASS. 메인 진행 문구는 9:16/19.5:9에서 어두운 plate와 밝은 글자의 가독성을 확인하고 최종 캡처했다. 일부 첫 클릭/중앙 클릭 무반응은 재클릭 시 정상 동작했으나 원인 미확정이므로 반복 터치 사람 인수 항목을 유지한다. 전체 완료로 처리하지 않으며 커밋·푸시는 PM 담당이다.
- Unity MCP: 현 스레드에서 호출 불가. 씬/프리팹 builder 실행 또는 YAML 직접 편집 없음. Unity가 소스/PNG를 import하고 `.meta`를 생성하도록 둠.
- 기본 새 디자인은 Play에서만 적용. 원본 SampleScene·Prefab을 덮어쓰지 않으며 실행 종료 시 런타임 변경 소멸.
- 서버·Shared·Battle·재화 계산·확률·가격·구매/광고 인증·교배 규칙·유닛 데이터 변경 없음.
- 프로필 subtree 및 Canvas/tab_bar 상위 transform은 보존. 새 제목·본문·메뉴 표현만 별도 생성/수정.

## 실제 기능 경계

현 SampleScene의 `view_Main`은 비어 있으며 로비 DTO에도 행성/최고 Wave 정보가 없다. 새 메인은 관측소 장식, 해왕성 표시, 실제 Quest/Tutorial 진입을 제공한다. 전투 시작·일일 콘텐츠는 `연결 준비 중` 비활성 버튼이다. 시안의 69/80·재화·캐릭터 수량을 하드코딩하거나 새 Battle Launcher를 만들지 않았다. 실제 로비 행성/매칭/Daily Adapter 연동은 별도 Task다.

상점은 기존 Resources 프리팹을 사용하는 경우에도 서버 응답 Render 시 새 배치로 변경한다. `ShopOffer` 가격/한도/할인/offerId와 purchase callback을 그대로 사용한다. 상점 pending/재시도/JWT 동작은 기존 Controller 소유다. 내 유닛은 기존 4열·세로비·실제 portrait catalog·미해금 Mythic 분리·선택 callback을 유지한다.

## 쉽게 돌아가기

1. Play를 종료한다.
2. Unity 메뉴 `Tools > MyDefense > Lobby > Theme > Legacy (next Play)`를 선택한다.
3. 다시 Play한다. 기존 직렬화 씬/프리팹과 legacy Build/Render 경로가 사용된다.
4. 다시 새 디자인을 보려면 같은 메뉴 `Midnight (next Play)` 후 Play를 재시작한다.

설정키는 `mydefense.ui.midnight.enabled`이며 기본값은 1이다. 실행 중 선택은 다음 Play부터 반영하고 진행 중 UI를 파괴하지 않는다. 이 설정은 계정·인증·경제 데이터가 아닌 로컬 표시 선호값이다.

작업 전 전체 백업:

- `Client/_localbuild/UiBackups/20261111-155020/before-midnight-ui.zip`
- SHA-256: `932478A72C3E3A0AE4E0969F6E5410F3C3236B9C76B616554BE13C3AA1E3BB01`
- 작업 시작 시 미커밋 변경과 SampleScene 포함. 7,491개 파일 byte/hash 대조 PASS는 PM 백업 보고 기준.
- 파일 복구가 필요하면 압축을 별도 임시 폴더에 풀고 **이번 UI 변경 파일만 비교·선별 복구**한다. 이후 사용자 작업을 덮지 않도록 프로젝트 전체 덮어쓰기, `reset --hard`, 일괄 삭제 복원은 금지한다.

## 에셋과 출처

사용자 첨부 `8EE2C5E6-1CD2-471C-ACC3-15CC931A7BEC/1-사진-1.jpg`를 참고하여 builtin imagegen으로 만든 장식이다. 완성 화면을 덮는 이미지가 아니라 별도 배경/히어로/행성 이미지이며 텍스트·가격·버튼·카드는 Unity UI다. 원본 생성물은 `.codex/generated_images`에 보존.

| 프로젝트 파일 | Resources 키 | 프롬프트 핵심 |
|---|---|---|
| `Assets/Resources/Lobby/Midnight/observatory-background.png` | `Lobby/Midnight/observatory-background` | 9:19.5 empty dark navy midnight spaceship observatory, quiet central 75%, no UI/text/characters |
| `Assets/Resources/Lobby/Midnight/gacha-prism.png` | `Lobby/Midnight/gacha-prism` | 16:9, right 52% smoky-violet full crystal/pedestal with ice-blue/rose-lilac twin spirits, left negative space, no UI/text |
| `Assets/Resources/Lobby/Midnight/neptune-observatory.png` | `Lobby/Midnight/neptune-observatory` | 3:4 blue Neptune and cyan orbital rings through navy observation window, no characters/UI/text |

`Resources.Load<Texture2D>` → RawImage로 사용한다. Sprite GUID/Importer 의존 없이 종횡비 cover crop, raycast 비활성. 기본은 중앙 crop이며 v2 프리즘만 우측 focal point를 사용한다. 기존 실제 유닛 일러스트와 마스코트는 유지하며 새 시안 캐릭터를 임의 canonical ID에 배정하지 않는다. 등록 portrait가 없는 카드의 기존 N/E/U 문자는 그대로 유지한다.

독립 리뷰에서 기본 TextureImporter의 NPOT `ToNearest`가 원본을 서로 다른 비율로 리사이즈하는 문제를 확인하여, **위 3개 파일만** 대상으로 `MidnightArtworkImporter`를 추가했다. Unity API로 NPOT None·maxSize 2048·Clamp·mipmap off·sRGB·Uncompressed를 설정하고 자동 reimport했다. 원본/Resources 크기 비교는 배경 852×1846, 프리즘 1672×941, 해왕성 1086×1448 모두 PASS다. `.meta` 수기 편집은 없으며 기존/다른 Battle 에셋 importer는 제외한다. NPOT 블록 압축에 따른 크기 변형을 피하려고 현재 무압축을 사용하며 모바일 메모리/플랫폼 압축 최적화는 실제 기기 검증 후 별도 판단한다.

## 변경 파일

기존 소스의 최소 표현 hook/보완:

- `Client/Assets/Scripts/@Core/Managers/LobbyManager.cs` — 런타임 테마 설치.
- `Client/Assets/Scripts/Ui/LobbyFinalPresentation.cs` — 재료/교배/선택 탭 테마 hook.
- `Client/Assets/Scripts/Ui/LobbyNeonGraphic.cs` — 기존 legacy/final 경로 보존, Midnight/primary 렌더링.
- `Client/Assets/Scripts/Ui/UnitCardUI.cs` — 데이터 설정 후 카드 표현만 적용.
- `Client/Assets/Scripts/Ui/Shop/ShopCatalogView.cs` — legacy 유지·Midnight 분기, 중첩 상품 focus.
- `Client/Assets/Scripts/Ui/Clan/ClanSurfaceGraphic.cs` — 렌더링 시 테마 색/모서리를 계산하여 첫 생성·재진입 동일, Shield/Circle 본래색 보존.
- `Client/Assets/Scripts/Ui/Quest/QuestController.cs`, `Client/Assets/Scripts/Ui/Tutorial/TutorialController.cs` — 기존 바로가기 위치 hook만.
- `Client/Assets/Scripts/Auth/AuthStartupView.cs` — 배경/제목 표현만, 인증 state/요청 불변.
- `Client/Assets/Scripts/Auth/AuthAccountPanel.cs` — 계정 연동 바로가기의 표현/위치 hook, 인증 기능 불변.
- `Client/Assets/Scripts/Ui/Shop/ShopCatalogFixture.cs` — 회귀 fixture의 재료 상품 category를 실제 계약 `RESOURCE`에 맞춤.
- `Client/Assets/Editor/FeatureTesting/FeatureTestCatalog.cs` — UI-MIDNIGHT 검증 등록.

신규 소스:

- `Client/Assets/Scripts/Ui/MidnightLobbyTheme.cs` — 기본/Legacy 표시 설정·리소스·카드 스타일.
- `Client/Assets/Scripts/Ui/MidnightLobbyPresentation.cs` — 프로필을 제외한 런타임 로비 shell, 안전영역·단색 nav·준비중 메인.
- `Client/Assets/Scripts/Ui/MidnightArtworkCrop.cs`, `Client/Assets/Scripts/Ui/MidnightCoinGraphic.cs` — 장식 cover crop·골드 아이콘.
- `Client/Assets/Scripts/Ui/MidnightNavGraphic.cs` — 같은 메뉴 이벤트에 연결된 채움형 하단 아이콘.
- `Client/Assets/Scripts/Ui/Shop/ShopCatalogMidnightView.cs` — 서버 상품의 히어로/4열 카드 배치, 정상가 보존, safe-bottom, Canvas cache.
- `Client/Assets/Editor/LobbyMidnightUiMenu.cs` — 다음 Play부터 Midnight/Legacy 전환 및 Play 중 Game View 캡처 메뉴. 기존 TMP 메뉴와 충돌한 캡처 단축키는 제거.
- `Client/Assets/Editor/MidnightArtworkImporter.cs` — 3개 전용 PNG import 정책.
- `Client/Assets/Editor/Tests/LobbyMidnightUiTests.cs` — 전용 회귀 30개, 최신 full-v5에서 30/30 PASS.

추가로 위 표의 PNG 3개와 Unity가 자동 생성한 관련 `.meta`, `docs/98_IMPLEMENTATION_TASKS.md`, 본 보고서가 변경되었다. 기존 서버/AuthSession 등 앞선 dirty 변경은 이번 작업에 편입·덮어쓰지 않았고 원본 Scene/Prefab 변경도 없다.

## 검증 구성

- `LobbyMidnightUiTests`: 임시 GameObject 생성 후 폐기만 수행. Scene/Prefab 저장 없음, HTTP/계정 데이터 접근 없음.
- profile/상위 transform·텍스트·실제 renderer style 보존, 중앙 crop, legacy 설정, 서버 가격/callback, nested 상품 focus, safe bottom, Resources texture 로드.
- 기존 LobbyFinal/Collection/Navigation/Shop/Clan/Auth/Quest 집중 회귀 재실행 PASS.
- Feature Test Hub `UI-MIDNIGHT`에 EditMode 케이스 등록. 표시 Scene은 기존 격리 상점 fixture이며 **새 전용 Scene은 생성하지 않음**. MCP/Editor 연결을 통한 전용 화면 fixture 후속 작업은 별도.
- **최신 full-v5**: PM Unity 6000.3.4f1 batch 컴파일 성공. 전체 EditMode **857개 중 856 PASS**, `LobbyMidnightUiTests` **30/30 PASS**. 기존 `BattleCanonicalBalanceTests.CanonicalBundle_LoadsManifestRegistriesAndMappings` **1 FAIL** 유지(expected `1-2c24f2a29fd63b81`, actual `1-e37af4a5ff9b7204`). Battle/Balance 소유권 밖 기존 hash 불일치이며 이번 UI에서 수정하지 않았다.
- 검증 수정 이력: full-v3에서 + glyph 테스트가 terminal quad 포함을 가정해 실제 4 vertices를 실패 처리했다. 실제 Canvas/폰트를 준비하고 XY 면적·UV 면적·alpha·버튼 안 bounds를 가진 단일 가시 quad를 검증하도록 수정했다. full-v4의 NUnit `GreaterThanOrEqual`/`LessThanOrEqual` API 오타로 컴파일이 실패하여 각각 `GreaterThanOrEqualTo`/`LessThanOrEqualTo`로 수정했고 full-v5에서 해소했다. 단순 assertion 완화나 UI 실패 은폐가 아니다.
- 과거 v1 이력: 집중 **213/213 PASS**(전용 **20/20**), 전체 **846/847 PASS**, 독립 소스 리뷰/실행증거 확인 **PASS·Approve·필수 수정 0**. 이는 당시 소스에 대한 결과이며 최신 마지막 캡처의 독립 승인과 구분한다.
- v1 PM 백업 대조에서 기존 Scene/Prefab 및 `AuthSession`·서버의 선행 dirty 변경 byte 동일을 확인했다. 신규 소스/PNG `.meta`는 Unity import로 자동 생성되었으며 수기로 만들지 않았다. 이번 후속 역시 Scene/Prefab builder 실행 없이 소스 표현 계층만 보완했다.
- 최신 독립 리뷰: full-v5의 전용 30/30·전체 856/857 및 마지막 메인 실제 캡처 직접 확인 **PASS·Approve·필수 수정 0**. PM은 Play=false 및 SampleScene 저장 변경 별표 없음도 확인했다. 사람 인수는 별도 대기.

실행 증거:

- 최신 full-v5 [XML](../../Client/_localbuild/MidnightValidation/full-v5.xml) / [Unity 로그](../../Client/_localbuild/MidnightValidation/full-v5.log)
- 과거 v1 집중 [XML](../../Client/_localbuild/MidnightValidation/focused-final.xml) / [Unity 로그](../../Client/_localbuild/MidnightValidation/focused-final.log)
- 과거 v1 전체 [XML](../../Client/_localbuild/MidnightValidation/full-final.xml) / [Unity 로그](../../Client/_localbuild/MidnightValidation/full-final.log)

## v2 시안 보완과 최신 화면 검증

후속 사용자 요청으로 Windows 실제 화면 조작과 캡처를 수행했다. Unity 6000.3.4f1의 SampleScene, 격리 loopback 18085 ShopPersistenceUiServer resume 및 기존 테스트 profile만 사용했다. 실제 계정·8080 서버는 변경하지 않았다.

- `02 미드나잇 관측소` 문구 제거. 제목/구매 버튼의 글자와 best-fit 상한 확대, 버튼 사선 모서리/채움 개선, 프리즘 히어로 확대·우측 crop, 불필요한 빈 등급 띠 제거, 실제 등급 리본/채움형 메뉴/보라색 교배 패널 적용. 프로필과 상위 transform은 그대로 보존한다.
- 재화 + 라벨의 좁은 영역에서 줄높이 때문에 사라지는 문제를 overflow/best-fit으로 보완. 3개 재료 상품은 실제 `RESOURCE` category 계약으로 3열 전폭 표시한다. Unity가 포커스된 동안 이전 binary로 보였던 배치는 Ctrl+R 재컴파일 후 정상임을 확인했다.
- 1080×2340(19.5:9): 메인·상점·내 유닛, 실제 RESOURCE 상품 전폭 3열, 재화 + 라벨, Quest/계정 연동 비겹침 PASS.
- 1080×1920(9:16): 메인과 재화 + PASS. 앞선 v2 상점/내 유닛 배치도 확인했다.
- 최종 대비 보완: 메인 진행 문구 뒤의 얇은 남색 plate와 밝은 글자를 9:16/19.5:9에서 실제 확인, 가독성 PASS. 장식은 raycast를 차단하지 않는다.
- 1080×1440(3:4): 메인·상점·내 유닛 배치 PASS. 행성 배경의 상단은 종횡비 유지 cover 정책에 따라 crop된다. 상품 전체를 압축하지 않으며 필요한 부분은 세로 스크롤로 확인한다.
- 실제 조작 PASS: 교배 진입→뒤로, DNA +→상점 해당 상품 focus, 컬렉션 최하단 도달, 상점 휠 스크롤, Gold 구매 확인→취소(구매 제출/재화 소비 없음), Quest 열기→닫기.
- 일부 첫 클릭/Quest 중앙 클릭에서 반응하지 않아 재클릭 후 열렸다. 입력 포커스/타이밍인지 UI 결함인지 원인 미확정이며 반복 터치는 사람 인수 잔여로 둔다. 이 동작을 무조건 정상이라고 확정하지 않는다.
- 종료 시 Play=false, SampleScene 저장 변경 표시 없음. 자동 검사나 제한된 조작 PASS는 모든 기능 무오류 판정이 아니다.

최신 native Game View 캡처(실제 실행, 시안/합성 아님):

- [상점](../../Client/_localbuild/MidnightValidation/screenshots-v2/shop-20261111-173837-688.png)
- [내 유닛](../../Client/_localbuild/MidnightValidation/screenshots-v2/collection-20261111-173936-487.png)
- [메인 — 최종 대비 보완](../../Client/_localbuild/MidnightValidation/screenshots-v2/main-20261111-175217-936.png)

같은 `screenshots-v2` 폴더에 비율별 Unity 창 캡처도 보존했다. `screenshots/main-before-layout-fix.png` 등 v1 캡처는 **수정 전 이력**이며 최신 상태가 아니다. 이전 Quest/계정 연동 겹침 보류 항목은 승인 후 수정·재검증 완료했다.

잔여: 반복 터치 사람 인수, 실제 구매/보상 결과, 모든 + 버튼, 모든 팝업 전수, 교배 완료 badge, 튜토리얼 진입/닫기, 로그인/로딩, Legacy↔Midnight 복귀 실동작, jjangash·kinggusi 최종 인수. 미등록 portrait의 기존 문자 placeholder와 메인 전투/진행/일일 미연동 경계를 유지한다. AI 자동 검사만으로 UI 완료 처리하지 않는다.
