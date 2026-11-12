# UI-MIDNIGHT — 미드나잇 관측소 로비

> 커밋 범위(2026-11-12): 이번 UI 커밋은 누적 로비/상점 표현 변경과 v7 보급 문구 제거만 포함한다. 별도의 Auth/JWT·서버 검증 fixture·Battle canonical hash 기대값 수정·Photon/ProjectSettings는 제외했다. 아래 전체928/928 결과는 이 별도 미커밋 변경도 존재하는 누적 작업 트리에서 실행한 결과이며, UI-only 커밋의 깨끗한 checkout 전체 검사 결과가 아니다. UI 전용101/101 및 실제 화면·독립 리뷰 결과와 구분한다. 기존 Battle hash 기대값 수정은 별도 반영 전까지 깨끗한 checkout의 알려진 테스트 잔여다.

## 최신 v7 — 오늘의 보급 잔여 문구 숨김 / 자동·실화면·독립 리뷰 PASS

- 최종 [reference-v7-supply-label.xml](../../Client/_localbuild/MidnightValidation/reference-v7-supply-label.xml) **전체928/928·전용101/101 PASS**, 독립 최종 리뷰 PASS. 실제9:16 상점 휠 스크롤로 보급7개 카드에서 잔여 문구가 없고 가격은 유지됨을 확인했다. [실제 확인 화면](../../Client/_localbuild/MidnightValidation/screenshots-v2/v7-supply-label-removed-window.png). 실제 구매는 수행하지 않았다.
- 사용자 요청으로 SUPPLY(RESOURCE+DAILY) 미구매 카드의 `남은 N회` Label 생성만 제거했다. 실제 RESOURCE 구매 한도3/2/3 및 기타 remaining 값, 가격·상품·callback은 그대로다. 구매 완료 표시와 한도0 구매불가, 별도 LUCKY 잔여, 주간 할인·광고 리롤 잔여는 유지한다. 카드 높이/가격 위치도 바꾸지 않았다.
- 수정은 `ShopCatalogMidnightView.cs` 표시 조건1개 및 `LobbyMidnightUiTests.cs` 회귀1개, 문서2개다. 신규 회귀는 보급 미구매 Limit 부재/구매완료/한도0비활성/LUCKY·주간·광고 표시/실제3·2·3·callback 보존을 검사하며 최종928/101 실행에 포함되어 PASS했다. 소스freeze·diff--checkPASS. 아래927/100 PASS는 이 후속 전 기준선이다. 커밋·푸시 없음, 사람 인수 잔여 유지.
- PM 마무리: Play=false, 원래9:16 복원, SampleScene 선행별표를 저장하지 않고 보존. 작업용 임시 서버 PID41716만 종료했으며 사용자8080은 불변이다. 실제 구매/광고 소모·터치드래그·최종 사람 인수 등 기존 잔여는 해소로 간주하지 않는다.

> **직전 검증 기준선은 v6 `reference-v6-chip-final.xml`: 전체927/927·전용100/100 PASS, 독립 최종 리뷰 PASS.** 표시1~5 및 Gacha 버튼을 보정하고 실제 오류→refresh 복구도 확인했다. 현재 최종 결과는 위 v7의928/101 PASS다. 실제 구매/광고 소모·터치 드래그·사람 최종 인수는 별도 잔여이며 UI 전체 완료가 아니다. 아래 v5와 초기 `full-v5.xml` 등은 과거 이력이다.

## 최신 v6 — 자동·실화면·독립 리뷰 PASS / 사람 인수 대기

- 최종 Unity 검사: [reference-v6-chip-final.xml](../../Client/_localbuild/MidnightValidation/reference-v6-chip-final.xml) **전체927/927 PASS·전용100/100 PASS**. 독립 최종 리뷰 PASS. 아래926/99는 할인배지 후속 전 1차 이력이다.
- 최신 native [상점1080×2160](../../Client/_localbuild/MidnightValidation/screenshots-v2/shop-20261112-002317-325.png)에서 최종 long-slant 할인배지, rounded 광고·가격 버튼,8점 Gacha, 정상 refresh 숨김 및 빈 XP를 확인했다.
- PM 실제 복구 검증 PASS: 정상 refresh 숨김 → 작업용 loopback18085 서버만 종료 → 상점 재진입 조회 실패·refresh 노출 → 같은 DB로 서버 resume → 기존 refresh 클릭으로 재조회 복구·다시 숨김. [조회 실패 화면](../../Client/_localbuild/MidnightValidation/screenshots-v2/v6-shop-recovery-error-window.png), [복구 성공 화면](../../Client/_localbuild/MidnightValidation/screenshots-v2/v6-shop-recovery-success-window.png). 사용자 서버8080 및 구매 데이터 정책은 변경하지 않았다.
- PM은 원래 Galaxy1080×1920 비율로 복원한 뒤9:16에서 목록 휠 스크롤로 하단 가격 버튼 접근을 확인했다. 모든 상품이 첫 화면에 들어오도록 압축하지 않고 기존 세로 스크롤 구조를 유지한다. 최종 Play=false, SampleScene의 선행 별표는 그대로 두고 저장하지 않았다. 작업용 `ShopPersistenceUiServer` PID41472만 command line 신원 확인 후 종료했으며 사용자8080은 불변이다. 최종 `git diff --check` exit0.
- v6 1차 실행 `reference-v6-final.xml`은 **전체926/926·전용99/99 PASS**였다. PM native `screenshots-v2/shop-20261112-001441-758.png`에서 rounded 리롤/가격, 정상 refresh 숨김, 빈 XP,8점 Gacha를 확인했다. 다만 할인 배지가 작은 chamfer rectangle에 가까워 별도 후속 보정을 승인했다. **이 1차 PASS는 아래 최종 배지 보정 후 실행 결과가 아니다.**
- 배지 후속: `DiscountBadge`만 decorative opt-in을 사용하여 높이38%만큼 양끝이 길게 기울어진4점 parallelogram으로 변경했다. matte gradient·얇은rim, raycast=false, Button 컴포넌트 없음. 다른 배지/8점 Gacha/rounded 가격·리롤은 불변이다. 단독 opt-in/기하/비상호작용 회귀1건은 위 최종 검사에 포함되어 PASS했다.
- 사용자 표시 이미지 `A0CF0F6E-63C7-4C93-9115-429F0BD92285/1-사진-1.jpg` 및 같은 폴더의 `2-사진-2.jpg`를 기준으로 승인 범위만 변경했다. hero/제목/상품 구성·그림/nav/메인·교배·일일·팝업/HUD+는 그대로다.
- 상점 버튼 전용 `ShopButtonShape` opt-in: 뽑기1회/10회는 긴 사선 끝과 짧은 네 chamfer를 가진8점 윤곽, 광고 리롤·상품 가격은 작은 rounded rectangle 및 얇은 bluegray bevel. 기존 기본 버튼 및 status의 보존된 mesh 경로는 변경하지 않는다.
- 광고 리롤은 제목 오른쪽에서 폭을 줄이고 play/refresh 아이콘 좌우 여백을 맞췄다. 주간 할인은 짧은 slanted chip으로 정리했다. DTO에는 최대 횟수 필드가 없으므로 시안 샘플3/3을 하드코딩하지 않고 실제 잔여를 `광고 리롤 N회`/`주간 할인 N회`로 표시한다. 서버 정책·DTO·가격·보상·category·callback은 불변이다.
- 독립 refresh는 정상 화면에서 숨기고 조회 실패/응답 파싱 실패/결과 재확인이 필요한 Controller 경로에서 명시적으로 재노출한다. 기존 Button 객체·listener와 JWT/pending 동일 요청 재시도 흐름은 유지한다. 표시 문구 내용으로 오류 여부를 추정하거나 새 구매 요청을 자동 발급하지 않는다.
- XP 계약과 지급 규칙은 현재 없다. 이름/레벨/아바타/지갑 위치는 유지하고, **빈 비상호작용 track와 `—/—`만** 표시한다. 기존 Slider의 authored75/500 및 fraction은 실제 경험치처럼 노출하지 않고 객체·값·listener를 보존한 채 계속 숨긴다. 새 `MidnightExperiencePlaceholder`는 캐시된 TMP의 실제 preferred width 뒤12 Canvas 단위부터 배치한다. 레벨 문자열/프로필 폭 변화 때만 갱신하며 극단적으로 긴 레벨로 공간이 부족하면 겹치기보다 placeholder를 숨긴다. 실제 XP 기능을 구현했다는 뜻이 아니다.
- 변경 파일: `LobbyNeonGraphic.cs`, `MidnightSurfaceMesh.cs`, `ShopCatalogMidnightView.cs`, `ShopCatalogController.cs`, `MidnightHeaderPresentation.cs`, 신규 `MidnightExperiencePlaceholder.cs`, `LobbyMidnightUiTests.cs`, 본 보고서 및 작업표. Scene/Prefab/서버/데이터/수기meta는 수정하지 않았다.
- 신규 총11case: 실제 잔여0/1/3 및 offer callback/shape3건,8점 convex 기하1건, recovery visibility/listener1건, XP 레벨1/123/123456789 및 극단적 길이3건, non-shop mesh 동일1건, null level 최초생성 방지/재bind 숨김/복구1건, 할인chip 단독 opt-in/기하/비상호작용1건. 기존 HUD vertex/color 동일 회귀도 유지하며 최종 전용100/전체927 검사에 모두 포함됐다. `git diff --check` PASS. 커밋·푸시 없음.
- 검증 잔여: 실제 구매/광고 소모·모든 + 버튼 재검증·터치 드래그·Legacy 전환·최종 사람 시각/조작 인수는 이번 자동/화면 PASS로 대체하지 않는다. UI 전체 완료로 처리하지 않았다.

## 직전 v5 이력 — 자동 검사·실화면·독립 최종 리뷰 PASS / 사람 인수 대기

- 최종 Unity 검사: [reference-v5-final.xml](../../Client/_localbuild/MidnightValidation/reference-v5-final.xml) **전체916/916 PASS, 실패0·스킵0, 전용89/89 PASS**. 독립 최종 리뷰가 소스·최신 XML·실제 이미지7장을 직접 확인하여 **PASS/Approve, 필수수정0**으로 승인했다. source freeze 유지, 사용자 최종 시각 인수는 별개로 남아 있다.
- 1:2 실제 초안 대조 후 추가 보정: 제목120/높이160, 뽑기 hero890/제목120·설명 상향, 본문bottom245(nav225 유지), 메인 CTA105/좌우대칭 육각형으로 변경했다. 구매 버튼의 사선형은 그대로 구분한다. 결정이 기존 상품 viewport350에 잘리지 않도록 상단145~하단245+safe의 **별도 RectMask2D·non-raycast 장식 레이어**에서 한 개의 scroll placeholder 위치만 추적한다. 4개corner 캐시, 재렌더 레이어재사용, target해제숨김, 전체 Canvas 순회 없음. 상품 viewport/데이터/scroll은 유지한다.
- 초기914개 실행에서14건 실패는 잘못된 기본NPOT import2건과 Unity6 `OnPopulateMesh` reflection overload 모호성12건이었다. importer version을 올리지 않고 정확7PNG만 재임포트하는 `Tools/MyDefense/Lobby/Reimport Midnight Artwork` 메뉴를 추가했다. reflection에 `VertexHelper` 시그니처를 명시했으며 검사조건은 약화하지 않았다. 재임포트는 AssetDatabase API만 사용하고 SaveAssets/Scene/Prefab 저장을 하지 않는다.
- 사용자가 v4 실제 화면을 승인 시안과 다르다고 판단하여 재작업을 요청했다. v4가 시안 정합 완료라는 의미는 아니다. 이번 기준은 `89C55B45-791F-4E76-940A-8832C5AC7C74/1-붙여넣은-이미지-1.jpg`, 425×853에 가까운1:2 비율이며 **유닛 그림·상단 status는 유지하고 UI만 변경**한다.
- 제목/CTA: 기존 LegacyRuntime 한글 fallback의 가는 synthetic bold/italic 대신 실제 굵은 `BlackHanSans-Regular.ttf`와 Italic만 사용한다. 본문 폰트와 상단은 그대로다. PM이 공식 [Google Fonts Black Han Sans](https://github.com/google/fonts/tree/main/ofl/blackhansans)에서 받은 TTF와 `OFL.txt`를 `Client/Assets/Resources/Lobby/Midnight/Fonts/`에 함께 보존했다. SIL OFL1.1, Copyright2015 Black Han Sans Project Authors. TTF cmap 직접 확인으로 `왹` glyph1471 및 대상5문구의 모든 글자가 있음을 확인했다. FontEngine 회귀도 추가했다.
- 버튼: 상단 기존mesh는 유지하며 다른 버튼에 길게 기울어진6점 윤곽, 얇은 상단 bevel/하단edge, 부드러운 파랑 그라데이션을 적용한다. 작은 팔각 모서리나 두꺼운 은색 cap을 재사용하지 않는다. 실제 callback/비활성 상태는 유지한다.
- 상점 hero: `gacha-prism-cutout-v5.png`(1024×1536 투명)를 독립 상자 안에 FitInParent로 표시하여 결정 끝/받침을 자르지 않는다. 기존 hero배경의 중복 결정은 제거하고 공통 배경 위에 표시한다. PM builtin imagegen 원본 `exec-aad36178-4bdb-48a5-a365-4485ea30b642.png`; 프롬프트 핵심은 기존 hero에서 결정·쌍둥이·궤도·받침만 추출하고 UI/글자 없이 배경 투명화. Resources 키 `Lobby/Midnight/gacha-prism-cutout-v5`. importer version3은 유지하고 이 PNG만 whitelist에 추가한다.
- 최종 배치: 제목 상단200/하단360, 상점본문350, hero높이890/상품380, 컬렉션재료350–480/목록505 Canvas 단위. 하단nav225/본문bottom245를 사용하며 Legacy값180/225는 보존한다. Header 아래에만 낮은 알파의 남색 shade를 넣고 Header 자체에는 영향이 없게 한다. 첫 초안의 제목225–325/hero950/content270은 위 최종 수치로 대체했다.
- nav5개 모두 코드 도형으로 시안 형태를 다시 제작:3분할 상자/U잠금, 기울어진 큰 Alien얼굴/눈2개/작은귀, 교차검, 사람없는 이중방패, 얇은3노드. 기존 전신 mascot만 runtime숨김, 유닛portrait와 무관하다. 실제선택 탭에만 낮은청록glow/밑줄, 나머지회색. 기존button/listener는 재사용한다. Quest clipboard/Daily calendar/Tutorial cap/광고 play-square/refresh pictogram도 non-raycast 자식으로 추가했다.
- 최종 전용89건: 첫 초안87개에 메인hex대칭 및 hero추적/마스크/재렌더 회귀2개 추가. `git diff --check` PASS. 기존 그림·canonical mapping·실가격·경제·미연동 상태는 그대로다. Scene/Prefab 저장·수기meta·커밋·푸시는 하지 않았다.
- 최신 변경 파일 요약: `MidnightLobbyTheme`, `MidnightLobbyPresentation`, `LobbyFinalPresentation`, `LobbyNeonGraphic`, `MidnightSurfaceMesh`, `MidnightNavGraphic`, `ShopCatalogMidnightView`, 신규 `MidnightHeroArtworkFollower`, `LobbyMidnightUiMenu`, `MidnightArtworkImporter`, `LobbyMidnightUiTests` 및 docs98/본 보고서. 에셋은 `gacha-prism-cutout-v5.png`, 폰트 `Fonts/BlackHanSans-Regular.ttf`·라이선스 `Fonts/OFL.txt`·출처 안내 `Fonts/README.txt`. `.meta`는 Unity가 자동 생성하며 직접 작성하지 않는다. 기존 다른 dirty 변경은 보존한다.

### v5 실제 화면·입력 확인과 잔여

- 최신1080×2160 native3장: [상점](../../Client/_localbuild/MidnightValidation/screenshots-v2/shop-20261111-233732-503.png), [내 유닛](../../Client/_localbuild/MidnightValidation/screenshots-v2/collection-20261111-233918-965.png), [메인](../../Client/_localbuild/MidnightValidation/screenshots-v2/main-20261111-233621-646.png). 합성 시안이 아닌 실제 실행 캡처다.
- PM 확인 PASS: Shop/Collection/Main 탭 전환, 상점 휠 스크롤로 하단 상품 확인 및 hero 마스크, 컬렉션 마지막20번째 Mythic 하단 여백, 기존 Legendary portrait 유지. 유닛 상세 진입과 Back은 PM 실제 관찰로 확인했으며 별도 스크린샷 증거는 없다.
- 증거: [상점 스크롤](../../Client/_localbuild/MidnightValidation/screenshots-v2/v5-shop-scroll-window.png), [컬렉션 최하단](../../Client/_localbuild/MidnightValidation/screenshots-v2/v5-collection-bottom-window.png), [컬렉션1080×2340](../../Client/_localbuild/MidnightValidation/screenshots-v2/v5-collection-1080x2340-window.png), [컬렉션1080×1920](../../Client/_localbuild/MidnightValidation/screenshots-v2/v5-collection-1080x1920-window.png).
- 잔여: 자동 drag는 click으로 인식되어 상세에 진입했으므로 **touch drag 성공은 미확정**이다. + 구매/실재화 소비는 재실행하지 않았고 Legacy 전환 실동작은 미확인이다. nav5형상/선택/콜백 자동검사는 통과했지만 Clan/Content 탭의 이번 실화면 전수 확인은 하지 않았다. 모든팝업/실구매/기기별 반복터치/사용자 시각 최종 인수는 남는다. 시안100%동일이나 기능 전체 완료로 주장하지 않는다.
- 종료 상태: Play=false, Game View9:16 복귀. Scene의 기존별표는 저장하지 않고 보존했다. 커밋·푸시하지 않았다.

## 이전 v4 — 사용자 시각 인수 미통과 이력

- 사용자 승인 범위: 현재 상단 status/profile/wallet/+는 그대로 유지하고, 상점·내 유닛·메인 버튼/색감/크기/상품 그림을 `BC2309BB-EA75-4AB5-970C-DDC611E79FE0/1-사진-1.jpg`에 가깝게 조정한다. 아래 gloss/satin PASS는 이전 구현 이력이며 **이번 v4의 테스트·실행 화면·독립 리뷰는 아직 대기**다. 시안 1개 화면은 약425×853 비율이며 같은 비율과19.5:9/9:16에서 비교한다.
- 버튼: 상단 `tab_bar` 아래 버튼은 이전 `DrawButton` 출력을 그대로 사용한다. 그 외 실제 버튼만 `DrawReferenceButton`의 청록/남색 상하 그라데이션·얇은 rim·낮은 알파의 작은 코너 반사로 교체하여 두꺼운 은색 cap을 제거했다. 보라색 신화 교배 accent는 유지한다. 일반 Panel/Card/Clan 표면과 버튼 callback/hit area는 변경하지 않는다.
- 상점: 히어로 높이900→810, 상품행400→350 Canvas 단위. RESOURCE와 실제 DAILY 상품을 **표시상 SUPPLY 4열**로 묶고 원래 ShopOffer/category/slotKey/가격/수량/제한/구매 callback 참조는 그대로 둔다. DAILY 카드의 `일일` 표식, `일일·럭키 상품만 변경` 광고 리롤 안내, 별도 LUCKY 영역을 유지한다. 빈칸을 채우기 위한 가짜 상품은 만들지 않는다. 서버 category/정책/재화 계산은 수정하지 않는다.
- 내 유닛: 4열·세로형 카드·미해금 Mythic 하단 분리 유지. 짙은 남색 카드와 등급 ribbon/그림영역/이름/레벨/수량 크기를 시안에 맞게 정리했다. 실제 portrait와 실제 수량은 유지하며 미등록 캐릭터에 새 일러스트를 임의 매핑하지 않는다.
- 메인: 새 장식 배경에 행성과 하단 양쪽 캐릭터를 넣고 이름·진행 안내를 중앙 빈 밴드에 배치한다. 전투 CTA 높이를 최대200 Canvas 단위로 제한하고 하단 기능 버튼과 구분한다. 전투 버튼은 계속 `interactable=false`, callback0, 연결 준비 안내 유지. 이 버튼만 disabled tint를 밝은 청록 표현으로 조정했으며 실제 매칭/전투 진입, 가짜 기록/하트 비용을 추가하지 않는다.
- 상단 보존: `MidnightHeaderPresentation.cs`와 `MidnightIconAtlas.cs`를 이번 v4에서 수정하지 않았다. + 버튼의 이전 mesh vertices/colors/triangles 완전 동일 비교, 기존9자리수량/참조/상위transform/44×44 시각·44×88 클릭 영역 회귀를 포함한다. 단위는 Canvas이며44dp 보장이 아니다.
- 신규 상점 에셋: `Client/Assets/Resources/Lobby/Midnight/shop-item-icons-v4.png` (1254×1254 투명, 2×2 각627×627). 키 `Lobby/Midnight/shop-item-icons-v4`; 위 coin stack/DNA, 아래 cells/shards. `MidnightShopIconAtlas`가 4개 Sprite를 캐시하고 반복 Image 재사용/Play 종료 정리를 한다. 기존 status atlas는 불변. 실제 등록 유닛 portrait가 있는 상품은 그 이미지를 우선 사용한다.
- 신규 메인 에셋: `Client/Assets/Resources/Lobby/Midnight/main-observatory-v4.png` (1086×1448). 키 `Lobby/Midnight/main-observatory-v4`; UI/글자/레벨/수량 없는 **장식 배경**이다. 기존 `neptune-observatory.png`는 삭제/덮어쓰기하지 않는다. 실제 유닛 데이터 연결 에셋이 아니다.
- 에셋 생성: PM이 builtin `image_gen`으로 생성. 상점 원본 `exec-ef759069-aae7-4410-9c69-8cd1b9f42a2f.png`, 프롬프트 핵심은 “승인 시안 톤의 투명2×2 shop-only coin stack/DNA/cells/shards, 텍스트·프레임 없음”. 메인 원본 `exec-00189ff3-b7de-4139-ac3d-f2f314148550.png`, 핵심은 “승인 오른쪽 화면의 행성·우주선 관측창·하단 양쪽 장식 캐릭터, 중앙 안내용 빈 공간, UI/글자 없음”. 원본은 `.codex/generated_images/`에 보존한다.
- importer whitelist를 신규2PNG까지 총6개로 확장(NPOT None/2048/Clamp/mipmap off/Uncompressed), `.meta`는 Unity import가 생성한다. Scene/Prefab 저장·수기 meta 편집 없음.
- 소스: `MidnightSurfaceMesh.cs`, `LobbyNeonGraphic.cs`, `MidnightLobbyPresentation.cs`, `MidnightLobbyTheme.cs`, `Shop/ShopCatalogMidnightView.cs`, 신규 `MidnightShopIconAtlas.cs`, `Editor/MidnightArtworkImporter.cs`, `Editor/Tests/LobbyMidnightUiTests.cs`. 문서는 본 보고서와98작업표. 기존 다른 미커밋 변경은 보존한다.
- 검증 준비: `git diff --check` PASS. `LobbyMidnightUiTests` 전용69건(기존50+신규19) 작성. 상단mesh동일/softgradient·purple·opacity/새PNG실제비율/4tile캐시/CTA4비율/원상품정확1회·원category·callback·focus/가짜4번째상품금지/portrait보존 포함. 실행 결과와 최신3화면 캡처는 PM 검증 후 기록한다. 커밋·푸시 없음.
- 기존 입력WARNING·메인미연동·미등록portrait placeholder·모든팝업/실구매/기기 반복터치/사람인수 잔여는 유지한다. 디자인 변경을 기능 전체 완료로 처리하지 않는다.

## 이전 버튼 광택 후속 — `247f519` 기준

- 사용자 요청: 시안 대비 버튼이 여전히 단색으로 보여 버튼에 명확한 입체감/반짝임을 보완한다. **이번 후속은 자동 검사·실제 상점 화면·독립 최종 리뷰 PASS**다. 독립 리뷰가 최신 XML과 실제 캡처 2장을 직접 확인했고 코드 필수 수정은 없다. 아래 satin-v2의 PASS는 이전 상태에 대한 결과다. 이번 광택 후속은 미커밋이며 전체 사람 인수는 남아 있다.
- 기존 `MidnightSurfaceMesh.Draw`의 매트 표면과 Clan/Card는 그대로 두고 `DrawButton`을 별도로 추가했다. 실제 Button owner를 가진 프레임만 opt-in하며 장식 할인 badge, 카드 및 일반 패널은 광택을 추가하지 않는다.
- 버튼 상단 약 31% 영역의 부드러운 반사, 선명한 top specular strip, 짙은 lower bevel/depth, 낮은 알파의 정적 대각선 facet을 추가했다. 텍스트 중앙 영역에는 광택을 그리지 않는다. 청록 primary와 남색 secondary의 반사 강도를 구분한다. 반사/음영은 기존 버튼 윤곽으로 polygon clip하고 전체 tint/alpha를 한 번만 적용한다.
- 축소 화면에서 반사/깊이가 사라지지 않도록 Canvas scale을 고려한다. 상단 strip 약 1.3 screen pixel·하단 depth 약 2.5 screen pixel을 목표로 하되 작은 버튼에서는 높이 비율로 제한한다. 아래 두 비율의 실제 상점 화면에서 최종 강도와 라벨 대비를 확인했다.
- 렌더링 경로만 수정: RectTransform·클릭 영역·Button 상태·실제 가격/수량·callback 불변. disabled 상태는 기존 Selectable tint를 유지한다. + 시각44×44/클릭44×88 Canvas 단위도 유지한다. 움직이는 광택 애니메이션, 새 이미지/셰이더, 게임 기능은 추가하지 않는다.
- 수정 파일: `MidnightSurfaceMesh.cs`, `LobbyNeonGraphic.cs`, `Shop/ShopCatalogView.cs`, `MidnightHeaderPresentation.cs`, `Editor/Tests/LobbyMidnightUiTests.cs`. Scene/Prefab/수기 meta 변경 없음. 커밋·푸시는 하지 않았다.
- 전용 **50/50 PASS**: 기존44개 + primary/secondary/rounded 버튼의 실제 specular/depth 및 윤곽 clip·중앙 영역 보존 3개, alpha0/.5 모든 레이어 검사 2개, 반복 opt-in의 상태/rect/callback 보존과 badge/card 제외 1개.
- Unity GUI TestRunner `gloss-v1`: 전체 **877개 중 876 PASS**. 기존 `BattleCanonicalBalanceTests.CanonicalBundle_LoadsManifestRegistriesAndMappings` 1 FAIL만 유지. [최신 XML](../../Client/_localbuild/MidnightValidation/gloss-v1.xml). PM `git diff --check` PASS.
- 실제 상점 1080×2340 native 및 1080×1920에서 대표 1회/10회 버튼의 광택과 라벨 대비 확인 PASS. [최신 상점 native](../../Client/_localbuild/MidnightValidation/screenshots-v2/shop-20261111-204835-652.png), [9:16 Unity 창 캡처](../../Client/_localbuild/MidnightValidation/screenshots-v2/gloss-shop-9x16-window.png). 합성 시안이 아닌 실제 실행 화면이며 독립 최종 시각 리뷰도 두 장을 직접 확인하여 가독성/광택 PASS.
- 종료/보존: Play=false 확인. Unity Editor 제목에 저장 변경 별표가 있어 **저장하거나 닫지 않고 열어뒀다**. 기존 사용자 상태를 덮어쓰지 않았다. 이번 작업용 격리 서버 PID44188만 종료했다. Editor 별표가 없다고 보고하지 않으며 Scene/Prefab 저장은 하지 않았다.
- 이전 잔여(스크롤 입력 미확인·일반 click 누락·재료 + 승인 제한·사람 인수)는 이 표현 변경으로 해소됐다고 간주하지 않는다.

## v3 — 표면 질감·compact status 이전 검증 이력

- 기준: `ea45c16`. 사용자 추가 시안 `BC2309BB-EA75-4AB5-970C-DDC611E79FE0/1-사진-1.jpg`와 프로필 재디자인 명시 승인에 따른 후속이다. **기존 프로필 디자인 불변 조건은 이번 승인으로 철회**되었지만, 실제 데이터/컴포넌트 참조/이벤트 및 Canvas·tab_bar 상위 transform 보존은 유지한다.
- 현재 상태: 최신 `satin-v2` **컴파일 성공**, 전체 **871개 중 870 PASS**, 전용 **44/44 PASS**. 실패 1건은 기존 Battle canonical hash 불일치다. **독립 코드 리뷰 및 최신 native 3장 시각 리뷰 PASS**. 아래 입력 WARNING·검증 제한과 사람 인수를 유지하며 기능 전체 완료 또는 시안 100% 동일로 판정하지 않는다.
- 작은 둥근 모서리의 매트 남색, 낮은 대비의 상단 반사 테두리, 안쪽 하이라이트와 아래 그림자를 `UIVertex` 공통 표면으로 구현했다. 주요 버튼의 사선은 유지하면서 cut 크기는 줄였다. Clan Panel은 공통 표면을 사용하고 Shield/Circle은 기존 표현을 유지한다. 새 셰이더나 배경 이미지 편집은 없다.
- 프로필은 공통 원형 avatar 장식과 실제 이름/레벨을 한 줄 status 높이에 compact 배치한다. 재화는 큰 패널 테두리를 숨기고 기존 TMP/버튼을 그대로 재배치한다. profile 범위 x=.025~.35, wallet x=.365부터 동일 .202폭/.208간격. 숫자는 오른쪽 정렬·max30/min18 자동 크기이며 원래 숫자 문자열을 변경하지 않는다.
- UserDto에는 XP 필드가 없으며 기존 `75/500`은 씬의 고정 값이었다. **XP 숫자뿐 아니라 Slider 전체(track/fill 포함)를 숨긴다.** Slider 객체·값·listener는 보존하며 가짜 XP 수치를 만들지 않는다. 실제 XP 연동은 별도 작업이다.
- + 버튼은 **44×44 Canvas 단위 시각 크기**, 가로 wallet 영역을 침범하지 않는 **44×88 Canvas 단위 투명 클릭 영역**이다. 이를 모바일 44dp 터치 영역이라고 주장하지 않는다. 기기 배율별 실제 터치는 사람 인수 대상이다.
- nav 단순 아이콘과 시안의 `02` 문구 삭제는 유지. 기존 유닛 portrait/canonical ID를 임의로 바꾸지 않으며 미등록 문자는 유지한다. 메인 전투/진행/일일 미연동, 첫 클릭 불확실성 및 전체 인수 잔여도 유지한다.

### v3 atlas와 구현 파일

- 신규 프로젝트 에셋: `Client/Assets/Resources/Lobby/Midnight/status-material-icons-v3.png` (1536×1024, 투명 3열×2행). Resources 키 `Lobby/Midnight/status-material-icons-v3`.
- top row: Heart / Coin / Diamond. bottom row: DNA / Cells / Avatar. 512×512 cell 좌표를 명시적으로 사용하며 `Sprite.Create` 결과 6개를 캐시한다. 중복 적용 시 자식 Image를 재사용하고 종횡비 유지·raycast false. Play 재시작/종료 및 Editor Play 종료 시 캐시 정리. 프로필 계정별 avatar 계약이 아닌 공통 장식이다.
- 적용: 상단 하트/코인/젬, 재료 DNA/성장세포, 신화 교배 DNA, 상점 재료/골드와 가격 통화 아이콘. 원본 유닛 portrait는 별개로 유지한다.
- `MidnightArtworkImporter` 정책을 위 atlas까지 **지정 4개 파일에만** 확장(NPOT None, max2048, Clamp, mipmap off, Uncompressed, alpha FromInput). `.meta`는 Unity가 생성하며 수기 작성하지 않는다.
- 생성 도구: builtin `image_gen`. 원본: `.codex/generated_images/.../exec-81aa07f2-a380-431d-8eed-9ae94df98246.png`.
- 생성 프롬프트 핵심: “production game UI icon atlas ONLY, transparent THREE COLUMN by TWO ROW, equal square cells; top heart/gold coin/cyan diamond, bottom lavender DNA/green cells/lavender alien profile medallion. Centers x=1/6,1/2,5/6 y=1/4,3/4, art70% cell. Tasteful painterly smooth gradients, delicate material highlights, subtle depth, not flat vector or neon glare. No text/numbers/UI/background/grid/watermark. Attached UI is style reference only.”
- 신규 소스: `MidnightSurfaceMesh.cs`, `MidnightIconAtlas.cs`, `MidnightHeaderPresentation.cs` (`Client/Assets/Scripts/Ui/`).
- 수정 소스: `LobbyNeonGraphic.cs`, `MidnightLobbyPresentation.cs`, `MidnightLobbyTheme.cs`, `Clan/ClanSurfaceGraphic.cs`, `Shop/ShopCatalogMidnightView.cs`, `Editor/MidnightArtworkImporter.cs`, `Editor/Tests/LobbyMidnightUiTests.cs`.
- 전용 회귀 **44/44 PASS**: 기존 프로필 디자인 고정 검사를 데이터/참조/상위 transform 보존 검사로 변경, 6개 tile 좌표/캐시 재사용, 4번째 PNG import 크기, 반복 적용 Image 재사용, 3개 비율에서 9자리 wallet/콜백/XP 보존, rounded 표면의 비퇴화 mesh/낮은 밝기, Clan alpha 0/.5 검사 포함.
- 리뷰 보완: Clan Panel의 원래 `color.a`가 fill에만 적용되고 rim/shadow/inner에는 남는 문제가 있었다. fill alpha를 1로 정규화한 뒤 공통 tint에 원래 alpha를 한 번만 적용했다. 각 vertex alpha가 opaque 기준×opacity인지와 최대 fill alpha가 제곱되지 않는지 회귀 검증 PASS. 기존 Shield/Circle 경로는 변경하지 않았다.
- 최신 실행 증거: [satin-v2 XML](../../Client/_localbuild/MidnightValidation/satin-v2.xml) / [Unity 로그](../../Client/_localbuild/MidnightValidation/satin-v2.log). 이전 `satin-v1`은 전체 868/869 및 전용 42/42 PASS였으며 alpha 회귀 보완 후 위 최신 결과로 대체한다.

### v3 최종 화면 확인과 입력 WARNING

- 1080×2340 native Game View에서 compact HUD와 atlas, 매트 표면의 메인/내 유닛/상점을 확인했다. 독립 리뷰가 최신 소스와 아래 3장을 직접 확인해 시각 PASS. 실제 실행 캡처이며 합성 시안이 아니다.
- [메인](../../Client/_localbuild/MidnightValidation/screenshots-v2/main-20261111-203017-844.png)
- [내 유닛](../../Client/_localbuild/MidnightValidation/screenshots-v2/collection-20261111-203200-672.png)
- [상점](../../Client/_localbuild/MidnightValidation/screenshots-v2/shop-20261111-203305-972.png)
- 상점 1080×1920/1080×1440에서 HUD 겹침·잘림 없음 확인. [9:16 창 캡처](../../Client/_localbuild/MidnightValidation/screenshots-v2/satin-shop-9x16-window.png), [3:4 창 캡처](../../Client/_localbuild/MidnightValidation/screenshots-v2/satin-shop-3x4-window.png). 하단 목록의 viewport clipping은 스크롤 성공 판정과 별개다.
- 입력 WARNING: 일반 click은 Unity에서 놓쳤고 pointer를 누른 채 1px 이동하는 방식으로 상점/내 유닛 탭 전환은 성공했다. 내 유닛 wheel 및 325px drag의 목록 이동은 **확인하지 못했다**. 입력 도구/포커스/실제 UI 중 원인 미확정이며, 독립 diff 리뷰에서 이번 v3에 ScrollRect 변경이 없는 점만 확인했다. 이를 근거로 스크롤 동작 PASS라고 간주하지 않는다.
- 재료 + 실제 입력은 auto-review가 구매 가능성을 이유로 차단했다. 우회하거나 재시도하지 않았으며 **사람 승인 및 검증 잔여**로 남긴다. 이번 화면 검증 중 재료 구매를 실행하지 않았다.
- 잔여: 위 입력 재현·탭/스크롤/재료 + 사람 확인, 모바일 실제 터치 크기, 로그인/Legacy 복귀·모든 팝업/모든 +·실구매 전수, 최종 사람 인수. 이전 v2에서 성공한 기능 항목과 이번 v3에서 확인하지 못한 항목은 별개다. 미등록 portrait placeholder·메인 미연동 경계도 유지한다.

## v2 완료 이력 — 당시 상태와 범위

- 담당: User/System. 사용자 선택 시안에 따른 표현 계층 변경.
- 당시 상태: 사람 인수 대기. 과거 `full-v5` 컴파일 성공, 전체857개 중856 PASS·전용30/30 PASS, 당시 Battle canonical hash1 FAIL. 독립 리뷰가 당시 실행 결과/메인 캡처를 직접 확인하여 PASS·Approve·필수수정0을 받았다. 계정연동/Quest 겹침·재화+라벨·RESOURCE3상품 배치 수정 후 재검증했고 메인 진행 문구 대비를9:16/19.5:9에서 확인했다. 일부 첫 클릭/중앙 클릭은 재클릭 후 동작해 반복 터치 인수를 남겼다. 이 문단은 v2 당시 이력이며 현재 판정은 맨 위 reference-v5-final916/916 및 사람인수대기다.
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

## v1/v2 당시 변경 파일 이력

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
- `Client/Assets/Editor/Tests/LobbyMidnightUiTests.cs` — 당시 전용 회귀30개, 과거 full-v5에서30/30 PASS. 현재는 위 reference-v5-final의89/89이다.

추가로 위 표의 PNG 3개와 Unity가 자동 생성한 관련 `.meta`, `docs/98_IMPLEMENTATION_TASKS.md`, 본 보고서가 변경되었다. 기존 서버/AuthSession 등 앞선 dirty 변경은 이번 작업에 편입·덮어쓰지 않았고 원본 Scene/Prefab 변경도 없다.

## v1/v2 당시 검증 구성·실행 이력

- `LobbyMidnightUiTests`: 임시 GameObject 생성 후 폐기만 수행. Scene/Prefab 저장 없음, HTTP/계정 데이터 접근 없음.
- profile/상위 transform·텍스트·실제 renderer style 보존, 중앙 crop, legacy 설정, 서버 가격/callback, nested 상품 focus, safe bottom, Resources texture 로드.
- 기존 LobbyFinal/Collection/Navigation/Shop/Clan/Auth/Quest 집중 회귀 재실행 PASS.
- Feature Test Hub `UI-MIDNIGHT`에 EditMode 케이스 등록. 표시 Scene은 기존 격리 상점 fixture이며 **새 전용 Scene은 생성하지 않음**. MCP/Editor 연결을 통한 전용 화면 fixture 후속 작업은 별도.
- **과거 v2 full-v5 이력**: 당시 PM Unity 6000.3.4f1 batch 컴파일 성공. 전체 EditMode **857개 중856 PASS**, 전용30/30 PASS. 당시 Battle canonical hash1 FAIL(expected `1-2c24f2a29fd63b81`, actual `1-e37af4a5ff9b7204`)이 남아 있었다. 이후 사용자가 별도 승인한 기대값 현행화로 해소되었고 현재 reference-v5-final은916/916이다.
- 검증 수정 이력: full-v3에서 + glyph 테스트가 terminal quad 포함을 가정해 실제 4 vertices를 실패 처리했다. 실제 Canvas/폰트를 준비하고 XY 면적·UV 면적·alpha·버튼 안 bounds를 가진 단일 가시 quad를 검증하도록 수정했다. full-v4의 NUnit `GreaterThanOrEqual`/`LessThanOrEqual` API 오타로 컴파일이 실패하여 각각 `GreaterThanOrEqualTo`/`LessThanOrEqualTo`로 수정했고 full-v5에서 해소했다. 단순 assertion 완화나 UI 실패 은폐가 아니다.
- 과거 v1 이력: 집중 **213/213 PASS**(전용 **20/20**), 전체 **846/847 PASS**, 독립 소스 리뷰/실행증거 확인 **PASS·Approve·필수 수정 0**. 이는 당시 소스에 대한 결과이며 최신 마지막 캡처의 독립 승인과 구분한다.
- v1 PM 백업 대조에서 기존 Scene/Prefab 및 `AuthSession`·서버의 선행 dirty 변경 byte 동일을 확인했다. 신규 소스/PNG `.meta`는 Unity import로 자동 생성되었으며 수기로 만들지 않았다. 이번 후속 역시 Scene/Prefab builder 실행 없이 소스 표현 계층만 보완했다.
- 당시 독립 리뷰: 과거 full-v5의 전용30/30·전체856/857 및 당시 메인 캡처 직접 확인 PASS·Approve·필수수정0. 당시 PM Play=false/Scene별표없음 관찰이며 현재 Editor상태와 혼동하지 않는다.

실행 증거:

- 과거 v2 full-v5 [XML](../../Client/_localbuild/MidnightValidation/full-v5.xml) / [Unity 로그](../../Client/_localbuild/MidnightValidation/full-v5.log)
- 과거 v1 집중 [XML](../../Client/_localbuild/MidnightValidation/focused-final.xml) / [Unity 로그](../../Client/_localbuild/MidnightValidation/focused-final.log)
- 과거 v1 전체 [XML](../../Client/_localbuild/MidnightValidation/full-final.xml) / [Unity 로그](../../Client/_localbuild/MidnightValidation/full-final.log)

## v2 당시 시안 보완과 화면 검증 이력

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
