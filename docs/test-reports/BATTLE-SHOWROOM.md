# Battle 쇼룸 구현 계획과 검증 기록

기준은 2026-11-11 확인한 `origin/dev` `54e3ee7`이다. 별도 `feature/battle-development-showroom`에서 작업한다.

## 재사용할 구성

| 현재 구현 | 쇼룸 연결 |
|---|---|
| Feature Test Hub와 `FeatureTestCatalog` | 독립된 캐릭터·전투·맵 씬을 등록한다. Development Player에는 컴파일된 쇼룸 목록을 제공하고, Editor는 기존 Catalog의 테스트 항목에도 계속 진입한다. |
| 공용 `AlienUnit.prefab`, `MutationAuraView`, 기존 Battle 카메라 | 원본과 Mutation 외형을 함께 표시한다. 확인된 공용 Battle 표현은 공용으로 표시하고, 종별 모델·Sprite·Prefab·Animator·클립이 연결되지 않은 항목은 누락을 표시한다. |
| `BattleWaveStateAuthority.Request*`, `BattleWaveExecutor`, 기존 Mythic Fixture | 로컬 Fusion State Authority에서 실게임 명령을 처리한다. 환경 준비와 명령 입력은 별도 UI로 제공하고, 판정은 적용된 보드·재화·전투 상태를 읽는다. |
| `PlanetContentApplicator`, 행성·Daily Catalog, `WaypointGroup` | 실제 Profile·Lane·Waypoint를 검증하고, 로컬·상대 배치를 표시한다. Wave 진입은 새 세션과 기존 초기화 경로로 연결한다. |

## 예정 변경 파일

| 위치 | 변경 이유 |
|---|---|
| `Client/Assets/Scripts/FeatureTesting/FeatureTestHubController.cs`, `Client/Assets/Editor/FeatureTesting/FeatureTestCatalog.cs` | Editor와 Development Player에서 세 공간 진입 및 안내 |
| `Client/Assets/Scripts/Battle/Showroom/` | 공통 씬 목록·설정 저장·초기화, 캐릭터·전투·맵 컨트롤러, 전투 관측 |
| `Client/Assets/Editor/FeatureTesting/BattleShowroomSceneBuilder.cs`, `Client/Assets/Scenes/Tests/Battle/` | Editor API로 씬 생성, 개발 빌드의 씬 목록 지정 및 Release 빌드 유입 검증 |
| `Client/Assets/Editor/Tests/` | 씬·Hub 경로, 설정 복원과 초기화, 빌드 가드와 실제 승인 경로 회귀 검사 |

1. 테스트 Hub, 세 씬, 개발 빌드와 공간별 설정 저장·초기화를 준비한다.
2. 기존 에셋으로 캐릭터 외형과 환경·경로를 연결한다.
3. 로컬 Fusion에 실제 요청을 연결하고 현재 지원 가능한 Fixture와 프리셋을 추가한다.
4. 기존 Wave 시작 경로의 권위·초기화 전제를 확인한 뒤 지정 Wave와 Boss를 연결한다. 시작 시점의 Wave 카탈로그, 타이머, 생존 상태, 보드, Gold·Kill 장부는 새 세션이 초기화한다.
5. EditMode/PlayMode, Windows Development Build, 씬 Missing Reference, 실제 UI, 독립 리뷰를 확인한다. 2클라이언트 검증은 별도 세션에서 권위와 복제를 비교한다.

## User/System·Shared 협의 요청

| 담당 | 필요한 계약 |
|---|---|
| User/System | 개발 전용 State Authority 준비 경로. 새 로컬 검증 세션에서만 플레이어 슬롯별 보드, Alien·Injector, Pending/활성/봉인 DNA, 시작 Gold, 해금 후보를 한 번 초기화하고 실패 사유를 반환한다. 세션 도중 재설정과 원격 클라이언트의 초기화 요청은 거부한다. |
| User/System | 격리된 테스트 계정의 공격 Snapshot 또는 로컬 계산 인터페이스. Alien ID·등급·레벨별 값을 기존 StatCalculator로 만들고 Battle은 받은 결과를 사용한다. 배치·해금 초기화 기능과 기존 Kidnap·Merge·신화 선택·리롤·Mutation·공명 명령은 각각 유지한다. |
| User/System + Shared | 검증 서버가 테스트 계정·세션을 발급하고 서버가 SessionSource를 `VALIDATION_FIXTURE`로 확정하는 경로. 현재 local/dev roster 어댑터는 클라이언트가 전달한 세션 이름 접두어로 분류하고 입장 예약도 호출하므로 그대로 연결할 수 없다. 격리 테스트에서는 계정과 영구 Gold·Heart·보상·Quest·업적·랭킹 쓰기를 금지한다. |
| Battle·아트 담당 | ID별 Battle Prefab·Sprite, 대기·이동·기본 공격·스킬 클립, 실제 Effect 연결 정보를 제공한다. 현재 공용 Battle Unit Prefab에 종별 Animator가 연결돼 있지 않다. |

기본 쇼룸은 로그인, 클라우드 매칭, HTTP Snapshot 요청, roster 등록, Daily 결과 및 Settlement 전송 없이 시작한다. 기존 전투에서 입력이 허용되는 상태는 실제 승인 결과로 확인한다. 현재 쇼룸 브랜치의 기준 dev에는 별도 작업 폴더의 종료 후 Kidnap 수정이 들어있지 않다.

## 검증 기준

- Editor와 Development Player에서 Hub → 각 씬 → Hub, 재진입 시 설정 복원, Reset 후 기본값을 확인한다.
- 실제 권위 승인 전후 보드·재화·횟수, 전투 모드별 Wave·Monster·Boss·공격 Snapshot을 확인한다.
- Development Player에서 로그인/HTTP가 발생하지 않고 비개발 빌드에서 쇼룸 씬을 거부하는지 확인한다.
- Unity MCP 호출 도구가 현재 노출되지 않는다. 씬 생성 도구는 Unity Editor API를 사용하며, 최종 메뉴·씬 조작과 화면 인수는 두 담당자의 테스트 기록으로 남긴다.

## 실행 방법

1. Unity 6000.3.4f1에서 이 작업 폴더의 `Client`를 연다.
2. `Tools → MyDefense → Battle Showroom → Open Feature Test Hub`를 누르고 Play한다. 독립 Windows 실행 파일이 필요하면 같은 메뉴의 `Build Windows Development Client`로 만든다. 이 빌드는 Hub를 첫 씬으로 실행한다.
3. Hub에서 `Character`, `Combat`, `Map & Content` 중 하나를 연다. `Feature Test Hub`로 복귀할 수 있으며 `Reset`으로 선택과 실행 상태를 초기화한다.
4. `Map & Content`에서 행성 Wave 또는 Daily Stage와 Wave를 고른 뒤 새 로컬 세션을 준비한다. `Combat`의 시작 버튼을 누르기 전에는 Wave 0에서 대기한다. Daily는 선택한 Wave부터 기존 타이머와 Wave 진행을 사용한다.

## 전투 프리셋과 관측

| 프리셋 | 준비 결과 |
|---|---|
| Empty board | 빈 보드와 지정한 시작 Gold |
| Matching pair / Different pair | 실제 Kidnap 승인 경로로 배치한 동일 종 또는 서로 다른 종을 선택한다. 설정한 Gold와 뽑기 결과에 따라 준비가 실패할 수 있다. |
| Full board / Insufficient Gold | 기존 Kidnap으로 24칸 채우기 시도 또는 Gold 0인 새 세션 |
| Mythic + Mutation | 기존 Development Mythic Fixture로 지정 ID·Mutation을 준비한다. |
| FAILED | 기존 Development 종료 전이로 FAILED를 만든 뒤 실제 명령을 시도한다. |
| Monster Wave / Boss Wave | 새 행성 세션에서 canonical W001 또는 W010을 준비한다. |

실제 명령 버튼에서 Kidnap, Move, Merge/Swap, Injector 사용, Mutation, 공명을 요청한다. 신화 선택과 리롤은 기존 선택 상태가 열려야 표시된다. Injector 강제 roll은 준비 영역에 있다. 보드에 배치한 Alien을 고르고 직접 입력한 공격 Snapshot으로 `UnitAttack`과 기존 Projectile을 실행할 수 있다. 관측 패널은 Authority의 발사·명중, 발사 간격, 명중 payload, 선택한 Monster HP 감소와 이동 상태를 표시한다. 입력한 Snapshot은 레벨별 서버 계산 결과를 대신하는 로컬 검증값이다.

## 실제 실행 기록

960×640 Windows Development 빌드에서 로그인 없이 Hub를 열었다. 공용 프리팹에서 Active TOXIC 오라가 보이고 Pending DNA로 전환하면 오라가 사라지는 것과 Battle 카메라 각도 전환을 확인했다. 맵에서는 변이 연구소 Profile과 Player 1 보드·Lane·스폰 위치, 재진입 시 선택 복원, Reset의 NEPTUNE 복귀, NEPTUNE Player 1·2 시점 전환을 확인했다. 이 해상도의 전투 패널 일부 값은 가로 스크롤로 확인했다.

- `RUNNING` Kidnap 이후 Gold 100,000 → 99,950, Kidnap 1회, 보드 1칸을 확인했다. Gold 0 프리셋에서는 요청 후 Gold·횟수·보드가 모두 0이었다.
- 최종 빌드에서 동일 종 Merge로 보드 3 → 2칸, 서로 다른 종의 Swap으로 보드 2칸 유지, 24칸 보드의 추가 Kidnap 거부를 확인했다. 가득 찬 보드는 Gold 96,040과 Kidnap 24회를 유지했다. 지정 Mythic 29·TOXIC 프리셋은 기존 Fixture에서 첫 슬롯에 생성됐다.
- `FAILED` 프리셋에서 종료 후 Gold 50이 차감되고 보드 1칸이 생겼다. Daily `CLEARED` 후에도 Gold 60이 차감되고 Kidnap 1 → 2회, 보드 1 → 2칸이 됐다. 기준 `dev`의 종료 입력 결함이며 이전 종료 가드 수정은 별도 작업 폴더에 있다.
- W010 Boss를 Wave 0에서 준비하고 명령으로 시작했다. 30초 후 실제 Boss 제한시간으로 FAILED가 됐고, Reset 이후 Wave 0·Gold 100,000·빈 보드로 복구됐다.
- 변이 연구소 S1을 Wave 0에서 준비해 W3 Boss로 바로 시작했다. 로컬 입력 공격력 5,000·기본 공속 1에서 Daily 감공으로 공속 0.9가 적용됐다. 실제 State Authority 투사체 1회가 HP 450의 Boss에 명중해 처치했다. 그 뒤 다음 Wave 요청에서 기존 카탈로그 종료로 CLEARED가 됐다.
- 초기 검증에서 Executor 중복과 화면에 이전 배경이 남는 현상을 발견해 수정했다. 두 번째 빌드에서 캐릭터·전투, 마지막 빌드에서 맵·Daily 지정 Wave와 실제 공격을 다시 확인했다.

## 자동 검증과 변경 파일

- Unity EditMode 전체 **828/829**, 실패 1, skip 0. 쇼룸 씬·설정 5건, Daily 시작 Wave 6건, 실제 씬 Fusion Single Kidnap 1건 모두 PASS. 최종 UI 표시 보정 후에도 집중 **12/12 PASS**를 확인했다. 초기 테스트 코드의 Assert 이름 충돌과 배치의 임시 dirty 씬 검사 실패는 보정 후 재실행했다.
- 전체의 유일한 실패는 기존 `CanonicalBundle_LoadsManifestRegistriesAndMappings`다. 기준 dev의 테스트는 `1-2c24f2a29fd63b81`을 기대하지만 같은 커밋의 Manifest는 `1-e37af4a5ff9b7204`다.
- Windows Development 빌드 성공, Unity exit 0. 독립 코드 리뷰 PASS. 로그와 XML은 `C:\myDefense\_localbuild\P2Validation`의 `showroom-editmode-final-20261111.xml`, `showroom-targeted-final-20261111.xml`, `showroom-build-final-20261111.log`, `showroom-player-r2-20261111.log`, `showroom-player-r3-20261111.log`, `showroom-player-final-20261111.log`다.
- 비개발 Windows 빌드에 Hub와 Combat 씬을 직접 넣는 부정 테스트에서 빌드 가드가 거부했으며 실행 파일이 생성되지 않았다. `showroom-release-negative-final-20261111.log`에서 거부 메시지와 산출물 부재를 확인했다.
- 런타임 변경은 `Battle/Showroom`의 공통 UI·설정과 세 공간 컨트롤러 및 전투 Probe, `BattleWaveExecutor.cs`의 Development Daily 진입, `BattleProjectileSpawner.cs`의 Development 관측 이벤트, `FeatureTestHubController.cs`다.
- Editor 변경은 `FeatureTestCatalog.cs`, `BattleShowroomSceneBuilder.cs`, `BattleShowroomTests.cs`, `DailyCultivationBattleTests.cs`와 Editor API로 생성한 3개 독립 씬이다. 신규 기능은 세 공간 중 담당 컨트롤러를 확장하고 기존 `Request*`와 읽기 전용 관측 이벤트를 연결한다.

## 추가 검증이 필요한 범위

- 종별 Battle 모델·Sprite·Prefab과 대기·이동·기본 공격·스킬 Animation은 연결 에셋이 필요하다.
- 임의 슬롯·해금·Legendary 2기, 임의 Monster/Boss 개체 수, 방어·무적, 자연 치명타, Support Kill, 두 번째 플레이어 탈락·관전은 기존 계약 또는 별도 검증 세션이 필요하다.
- UI 일시정지·속도 조절은 지원 범위 밖이다. 실제 2클라이언트 동기화와 격리 서버의 `VALIDATION_FIXTURE` 발급·영구 상태 무변경은 별도 검증 항목이다.
- 최종 사람 인수는 jjangash·kinggusi가 각각 기록한다.

## 콘셉트 유닛 프리뷰

첨부 콘셉트 시트를 참고해 기본 이미지 생성 도구에서 투명 스프라이트 3개를 만들었다. 저장 경로는 `Client/Assets/Art/Battle/ShowroomConcepts`다. 두 퍼펫티어 프리셋이 `Puppeteer.png`의 같은 조종사·인형 스프라이트를 사용한다.

| 프리셋 | 기본 공격 | 스킬 |
|---|---|---|
| 디아드 | 왼쪽 시안 레이저와 오른쪽 마젠타 슬래시 교대 | 보스를 겨냥한 양색 초승달 참격 **의식의 절단** |
| 메카론 | 한 표적을 향한 집중 연속 포격 | 여러 표적에 퍼지는 확산 미사일 |
| 퍼펫티어 1안 | 인형 3기를 배치해 조종하고 재배치 | 각 인형 위치에서 동시 폭발 |
| 퍼펫티어 2안 | 인형 1기를 전방으로 보내고 곡선으로 회수 | 인형을 위로 던져 정점에서 폭발 |

Windows Development 빌드의 `Feature Test Hub → Character`에서 `Diad`, `Mecharon`, `Puppeteer 1`, `Puppeteer 2`를 선택한다. `Idle`, `Move`, `Basic`, `Skill`을 눌러 재생한다. `Loop selected action`은 선택한 동작을 반복한다. 퍼펫티어 `Automatic Puppet preview (10 s)`는 1안에서 추가 소환, 2안에서 정점 폭발을 예약한다. 1안은 반복 비교를 위해 화면에서 최대 6기로 제한한다.

- 최종 Windows Development 빌드 성공. 960×640 창에서 네 프리셋 선택, 디아드 교대 기본 공격과 양색 참격, 메카론 집중 포격·확산 미사일, 퍼펫티어 1안의 3 → 6기와 동시 폭발, 2안의 단일 인형 왕복·정점 폭발을 확인했다. 해당 크기에서는 짧은 설명 문구의 끝부분이 잘릴 수 있다.
- 신규 콘셉트 EditMode **12/12 PASS**와 독립 리뷰 PASS. 전체 **840/841 PASS**, 실패 1·skip 0. 유일한 실패는 기존 `CanonicalBundle_LoadsManifestRegistriesAndMappings`의 Balance Manifest 불일치다.
- 변경은 `BattleShowroomCharacter.cs`, `BattleShowroomConceptPlayback.cs`, `BattleShowroomConceptPreview.cs`, Character 씬과 Editor 생성 도구, `BattleShowroomConceptTests.cs`, 스프라이트 3개다. 이미지 참조는 Character 테스트 씬에만 연결돼 있다.
- 결과물은 2D 스프라이트 기반의 쇼룸 시연용이다. 유닛 ID 등록, 실제 게임 내 스킬 피해와 배율, 3D 모델과 리깅은 별도 범위다.

<details>
<summary>에셋 제작 프롬프트</summary>

- **Diad** · From the supplied Diad design sheet, extract/rebuild only the single central Diad character as a high-fidelity transparent game sprite for a Unity diorama. Exactly two fused floating alien faces, left pale lilac with ice cyan eye/crystal and large cyan-lilac crescent horns, right vivid pink-magenta with rose eye/crystal and magenta crescent horns. One blue-and-pink diamond core, layered midnight-purple flowing petal body ending in tapered points, icy blue on left and vivid orchid on right. Preserve the sharp adorable chibi faceted anime style, vivid but clean cel shading, both faces visible in a symmetrical front-three-quarter game view, full silhouette centered with 9% padding. No attack arcs or separate particles; the game will animate them. No board, text, letters, border or ground. True transparent alpha, crisp cutout.
- **Mecharon** · From the supplied Mecharon reference, extract/rebuild only the main mounted mech pilot as a high-fidelity transparent Unity game sprite. Match the reference identity: small tan-golden alien pilot with white swept-up crest, two gold-lensed purple aviator goggles on forehead and fierce cyan eyes, seated in a squat bulky lilac and violet mech; wide plated mechanical feet, cyan luminous hexagonal barrels with golden rims, cyan front armor panel, low-poly-shaped beveled armor with clean anime cel shading. Centered full body at front-three-quarter game view, entire feet and antenna with 8% padding. Symmetrical idle barrel position. No lightning, muzzle flash, fire or separate particles; these will be animated separately. No ground, text, framing or inset views. True transparent alpha, crisp silhouette.
- **Puppeteer** · Create a transparent 2D Unity sprite atlas faithful to the exact two supplied images of the SAME Puppeteer design. The output is one square atlas made of two equal vertical cells, each empty transparent except its own full-body centered isolated subject. Left cell contains ONLY the Puppeteer body: angular chibi jester with red, navy, cream, and deep-purple diamond patterned three-pronged hood, warm yellow star brooch and trim, glowing orange eyes on dark face, pointed fan-like skirt / cape. Right cell contains ONLY its separate companion marionette, an ivory-blush round stitched head with curved crimson and gold horns, vivid warm orange eyes and shadow face mask, short patchwork jester body, little clawed arms and legs. Center left subject at 25% x and right at 75% x, each strictly within own half with 5% horizontal margin to edges and seam; both centered vertically. Front three-quarter poses, crisp high-quality faceted cel shading like the references. No strings between cells, no shared aura, no text or frames, no floor or shadows, no symbols. True transparent alpha. Puppet will be animated independently and the two gameplay presets share both of these exact same sprites.

</details>



## 2026-11-12 한 칸 캐릭터 쇼룸

첨부 자료의 작은 기본 형태(턴어라운드)를 기준으로 디아드, 탑승 메카론, 퍼펫티어 본체와 인형의 정면·측면·후면을 제작했다. 첫 번째 스크린샷은 타일 대비 유닛 크기와 내려다보는 시점의 참고로 사용했다. 기존 Battle 씬의 `GridManager/Tile_0_0` 메시·재질·크기를 Character 전용 씬에 한 개 복제하고, 이 인스턴스의 색을 차분한 회청색으로 조정했다. 50° 직교 카메라와 바닥 접촉 그림자를 사용한다.

`Feature Test Hub → Battle Showroom | Character`에서 `Diad`, `Mecharon`, `Puppeteer 1`, `Puppeteer 2`를 선택한다. `Front / Side / Rear`는 방향별 스프라이트와 연출 방향을 함께 바꾼다. `Basic / Skill`로 재생하고, `Loop selected action`으로 반복한다. `Tile camera zoom`으로 화면을 확대하고 `Restart preview`로 현재 프리셋의 동작 상태를 다시 시작한다. `Reset`은 저장된 선택도 초기화한다.

| 프리셋 | 타일에서 확인할 동작 |
|---|---|
| 디아드 | 시안 레이저와 마젠타 참격 교대, 양색 의식의 절단 |
| 메카론 | 한 방향에 집중 포격, 이후 확산 미사일 |
| 퍼펫티어 1 | 인형 3기 배치·재배치·동시 폭발, 10초 추가 소환 선택 |
| 퍼펫티어 2 | 동일한 조종사·인형 외형, 인형 1기의 왕복 공격과 정점 폭발 |

이 개정의 변경 파일은 `BattleShowroomCharacter.cs`, `BattleShowroomConceptPreview.cs`, `BattleShowroomTileAtlas.cs`, `BattleShowroomSceneBuilder.cs`, `BattleShowroomConceptTests.cs`, Editor API로 생성한 `BattleShowroomCharacter.unity`, Character 씬 전용 스프라이트 7개 및 Unity가 생성한 메타데이터다. 스프라이트는 `Assets/Art/Battle/ShowroomConcepts`의 `DiadTile{Views,Attack}`, `MecharonTile{Views,Attack}`, `PuppeteerTile{Views,Attack}`, `PuppeteerDollTileViews`에 있다.

<details>
<summary>타일 스프라이트 제작 기준</summary>

- 세 칸에 정면·측면·후면 한 기씩을 배치하고, 원본 시트의 작은 턴어라운드와 같은 비율·색·실루엣을 유지한다. 살짝 높은 보드 카메라에 맞는 2D 셀 셰이딩과 투명 배경을 적용한다. 문구, 프레임, 타일, 배경과 공격 이펙트는 스프라이트에 그리지 않는다.
- 공격용 시트는 같은 대기 스프라이트를 참고해 발 위치와 방향을 유지하고, 디아드는 응집 자세, 메카론은 포격 자세, 퍼펫티어는 손을 들어 조종하는 자세를 표현한다. 두 퍼펫티어 프리셋은 동일한 본체와 인형 파일을 사용한다.

</details>




### 최종 확인

- Windows Development 빌드 종료 코드 0, 컴파일 오류 0. 960×640 Standalone에서 네 프리셋 진입과 정면·측면·후면 전환을 확인했다. 디아드 측면 참격, 메카론 정면·후면 포격 진행, 퍼펫티어 1안 인형 세 기의 바닥 배치와 추가 소환, 퍼펫티어 2안 단일 인형의 측면 스킬 전개를 확인했다. 1안 후면은 가운데 인형이 시점에 따라 본체 뒤에 가린다. 최종 화면은 메카론 정면으로 열어 뒀다.
- EditMode 타일 쇼룸 **14/14 PASS**. 전체 **842/843 PASS**, 실패 1·skip 0. 유일한 실패는 기존 `CanonicalBundle_LoadsManifestRegistriesAndMappings`의 Balance Manifest 버전 기대값 불일치다. 두 차례 독립 코드 검토 PASS. 이 빌드의 목적은 로컬 외형과 공격 연출의 비교다.
- 증거 파일은 `C:\myDefense\_localbuild\P2Validation`의 `showroom-tile-generate-20261112.log`, `showroom-tile-build-final-20261112.log`, `showroom-tile-editmode-final-20261112.xml`, `showroom-tile-player-final-20261112.log`다.




## 2026-11-12 4×6 보드 2.5D 캐릭터 쇼룸

첨부 MP4 8.95초를 실제로 디코딩해 프레임을 비교했다. 참고한 항목은 내려다보는 고정 시점, 타일 한 칸 안의 유닛 크기, 준비·타격·착지·복귀의 연결이다. 실제 Battle `GridManager`의 24개 타일을 위치·메시·재질 기준으로 복제했다. 63° 직교 카메라에서 `Tile_2_1` 한 칸에 본체를 놓는다. 아트를 적용한 얕은 곡면 메시, 알파 깊이 처리, 타일 위 접지 그림자와 실제 높이 변화로 보드에 정렬했다. 유닛 시점은 `Front / Side / Rear`를 사용한다.

- 퍼펫티어 1안의 `Basic`은 배치된 인형이 각각 준비·돌진·타격·복귀한다. `Deploy dolls`로 추가 소환하고 `Move`로 재배치하고 `Skill`로 동시 폭발한다. 조종실은 동작에 따라 휘고 가늘어지는 곡선이다.
- 퍼펫티어 2안은 동일한 본체와 인형 아트를 사용한다. 한 기가 곡선을 따라 왕복하고 스킬에서 상승해 정점에서 폭발한다.
- 메카론은 별도 포구 섬광, 포탄, 미사일, 불꽃·배기, 착탄, 그을음 아트를 사용한다. 발사마다 포구의 출발점을 고정한다.
- 디아드는 양쪽 의식의 교대와 의식의 절단을 기존 방향·동작 버튼으로 재생한다.

Higgsfield 연결과 사용자 제공 PNG 4개 업로드는 성공했다. 생성 요청 6개는 선택 모델의 `Requires basic plan or higher`로 모두 접수되지 않았다. 조회한 10 크레딧은 유지됐다. 같은 원본 시트를 입력한 이미지 생성 도구로 방향별 대기·공격 포즈와 전용 이펙트를 만들었다. 턴어라운드의 색·실루엣·장식·포구·봉합선에 맞춰 재생성한 게임용 스프라이트이며 원본 픽셀 그대로를 가져온 것은 아니다.

변경 파일은 `BattleShowroomSceneBuilder.cs`, `BattleShowroomCharacter.cs`, `BattleShowroomConceptPreview.cs`, `BattleShowroomConceptPlayback.cs`, `BattleShowroomRelief.cs`와 `.shader`, `BattleShowroomConceptTests.cs`, Editor API로 생성한 Character 씬이다. Character 씬 전용 아트는 `Assets/Art/Battle/ShowroomConcepts`의 `{Diad,Mecharon,Puppeteer,PuppeteerDoll}Board{Views,Attack}.png` 8장과 `{Mecharon,Puppeteer}BoardEffects.png` 2장이다.

- 960×640 Windows Development 실행에서 보드 24칸, 네 프리셋, 정면·측면·후면 전환, 퍼펫티어 1안의 개별 기본 타격 및 소환 3→6, 퍼펫티어 2안의 상승, 디아드 후면 스킬 재생, 메카론의 집중탄과 확산 미사일·착탄을 확인했다.
- 컴파일 오류 0, Windows 빌드 종료 코드 0, 콘셉트 EditMode **16/16 PASS**, 독립 리뷰 PASS. 전체 EditMode **844/845 PASS**, 기존 Balance Manifest 버전 불일치 한 건. 연출과 움직임의 사람 확인은 대기다.
- 로그는 `C:\myDefense\_localbuild\P2Validation`의 `showroom-board25d-generate-20261112.log`, `showroom-board25d-build-20261112.log`, `showroom-board25d-editmode-20261112.xml`, `showroom-board25d-player-20261112.log`에 있다.




## 2026-11-22 페인트 타일·유닛 크기·대기 자세 보완

사용자가 요청한 유닛 가독성과 타일 그림체를 보완했다. 본체 외형을 기존 대비 1.6배, 퍼펫티어 인형을 1.3배로 확대하고 포구·조종하는 손·그림자 위치를 비례 보정했다. 본체 Idle의 주기적 좌우 회전·상하 진동과 인형 Idle 회전을 제거했다. 이동·발사·타격 동작은 선택한 행동의 시간 안에서만 적용한다.

4×6 24칸의 원래 중심 위치에 상면을 수평으로 놓고 손그림 돌 타일을 적용했다. 모서리의 명암과 칩·균열을 가진 청록 돌 두 종류, 금속 테두리 점유 칸, 짙은 보드 프레임을 2×2 아틀라스로 사용한다. 타일 사이 간격을 줄이고 UV 가장자리 여백·Bilinear·mipmap 비활성으로 이웃 셀의 색 번짐을 제한한다. `Full board`는 24칸, `Unit detail`은 점유 칸을 중심으로 2.6배 상세 확대를 보여준다. 확대 화면에서는 주변 보드와 인형 일부가 화면 밖으로 나갈 수 있다.

Higgsfield 연결은 확인했다. 현재 계정으로 `gpt_image_2_6`와 공식 카탈로그의 `flux_2`를 각각 견적·생성 호출했지만 두 요청 모두 `Requires basic plan or higher`로 생성 전 거부됐다. 선택 모델의 생성 결과는 없다. 타일은 같은 작업 설명으로 기본 이미지 생성 도구를 사용해 만들었고 `Assets/Art/Battle/ShowroomConcepts/PaintedBoardTiles.png`에 저장했다.

- 생성 요청은 2×2 정방형 아틀라스, 위 두 칸은 조용한 중심·모서리 명암의 청록 돌, 좌하단은 구리금색 점유 테두리, 우하단은 짙은 남색 프레임을 지정했다. 텍스처는 정사영 위에서 본 평면으로 그리며, 유닛·문구·UI·배경을 넣지 않는다. 카메라가 실제 바닥 투영을 만든다.
- 이 개정의 소스 변경은 `BattleShowroomConceptPreview.cs`, `BattleShowroomCharacter.cs`, `BattleShowroomSceneBuilder.cs`, `BattleShowroomConceptTests.cs`다. Character 씬 재생성과 PNG 임포트·메타 생성은 Unity Editor API로 수행했다. Unity MCP는 현재 도구 목록에 없었다.
- Windows Development 컴파일 오류 0, 빌드 종료 코드 0. 관련 쇼룸 EditMode **22/22 PASS**, 독립 소스 리뷰 PASS. 전체 EditMode 재실행은 이번 범위에 포함하지 않았다.
- 1280×800 실제 창에서 네 프리셋의 확대 외형, 고정 Idle, 24칸 손그림 바닥, Full board/Unit detail 전환, 메카론 측면 공격 포즈와 확대된 포구를 확인했다. Player 로그 예외/오류 0. 메카론 정면 상세 확대를 최종 화면으로 열었다. 사람 비주얼 인수 상태는 검증 대기다.
- 증거는 `C:\myDefense\_localbuild\P2Validation`의 `showroom-painted-generate-final-20261122.log`, `showroom-painted-build-final-20261122.log`, `showroom-painted-editmode-20261122.xml`, `showroom-painted-player-final-20261122.log`다.




## 2026-11-22 SF 바닥·사선 시점·4포즈 공격 보완

사용자 지적에 따라 바닥을 금속 패널, 작은 청록 표시등, 얇은 보라 회로 테두리를 가진 SF 아트로 바꿨다. `SFBoardTiles.png`는 기존 2×2 UV 계약을 사용하며 Character 씬에만 연결한다. 24칸은 원래 타일 중심을 유지한다.

기본 카메라의 방위각은 23°이고 높이는 50~80°로 조절한다. `Oblique`는 65°, `Overhead`는 78°다. 돌린 보드와 포즈의 실제 모서리를 카메라 방향으로 투영해 화면 범위를 맞춘다. 캐릭터 평면 기울기를 30°로 줄여 카메라를 거의 정면으로 따라보던 이전 표시를 조정했다. 최종 창은 Overhead 78°·중간 확대·Diad Side·Skill 반복으로 열어두었다.

Idle, Basic, Skill, Deploy의 본체 위치·회전·크기를 고정했다. 준비·발사·회복의 구분은 새 Windup/Recover와 기존 Idle/Attack 포즈로 표현한다. Move는 선택된 이동 동작 안에서만 지면 위 1회 왕복한다. 메카론 준비·회복 포즈의 폭 차이는 메시 생성 시 Idle 폭으로 고정 등록하며, 발·높이·원본 UV와 Attack 프레임은 유지한다. 퍼펫티어 본체의 전체 기울기와 공격 중 상하 진동을 제거하고 인형의 실제 타격·회수 경로는 유지한다.

디아드는 시안 레이저와 마젠타 참격을 교대로 재생한다. 스킬은 코어 충전 후 하나의 큰 양색 낫 참격이 날아가고 잔상과 적중이 이어진다. 기존 절차적 선·원 기반 디아드 공격은 전용 아틀라스로 교체했다. 후면의 디아드 얼굴·코어와 메카론 포구는 본체 뒤 깊이로 옮겨 등에 발사광이 붙는 경우를 줄였다.

### 아트 및 도구

Higgsfield의 기존 사용자 제공 디아드 참조를 사용해 견적과 생성 호출을 수행했다. 생성 요청은 다시 `Requires basic plan or higher`로 접수 전 거부됐다. 현재 계정으로 Higgsfield 생성 결과를 얻지 못했다. 기본 이미지 생성 도구에서 원본 시트와 기존 방향별 아트를 참조해 이번 에셋을 만들었다.

추가 PNG는 `Assets/Art/Battle/ShowroomConcepts/SFBoardTiles.png`, `DiadBoardEffects.png`, `{Diad,Mecharon,Puppeteer,PuppeteerDoll}Board{Windup,Recover}.png` 8개다. 메카론 포즈는 원본의 넓고 낮은 비율에 맞춰 재생성한 버전을 사용했다. 포즈 시트의 행 분리는 픽셀을 바꾸지 않는 아틀라스 분할로 수행했다. 메타데이터와 씬은 Unity Editor API로 생성했다. Unity MCP는 현재 도구 목록에 없다.

- SF 바닥 프롬프트는 정사영 위에서 본 2×2 금속 패널 아틀라스, 두 일반 칸·선택 칸·짙은 프레임, 넓고 조용한 중앙, 작은 청록/보라 표시등, 얇은 패널 이음새를 지정했다. 돌·균열·판타지 유적과 문구는 제외했다.
- 디아드 이펙트 프롬프트는 원본 팔레트의 독립된 3×2 투명 아틀라스에 충전·시안 레이저·마젠타 참격·한 개의 큰 양색 낫·적중·잔상을 순서대로 지정했다.
- 포즈 프롬프트는 원본의 색·얼굴·복장·몸통·발 위치를 유지한 3방향 준비/회복 2행 시트다. 손·포구·꽃잎의 행동 변화만 표현하고 전신 기울기와 별도 배경·효과를 넣지 않도록 지정했다.

### 검증

- Windows Development 최종 빌드 성공, 컴파일 오류 0, 종료 코드 0. 쇼룸 관련 EditMode **30/30 PASS**, 독립 코드 리뷰 PASS. 전체 EditMode는 이번 변경에서 재실행하지 않았다.
- 1280×800 실행에서 SF 24칸의 사선 전체 보기, Overhead 전환, 중간 확대, 디아드 참격 비행과 멀리 떨어진 타격점의 적중, 메카론 연사 포즈, 퍼펫티어 두 안의 준비/공격 포즈를 확인했다. Player 로그 오류/예외 0.
- 컴퓨터 사용 연결이 일시적으로 다른 창을 반환해 입력을 중단하고 대상 창을 새로 선택·활성화했다. 실제 쇼룸 프로세스는 정상 실행 중이었다. 이 연결 문제를 빌드나 게임 오류로 판정하지 않았다.
- 주요 소스 변경은 `BattleShowroomConceptPreview.cs`, `BattleShowroomConceptPlayback.cs`, `BattleShowroomCharacter.cs`, `BattleShowroomSceneBuilder.cs`, `BattleShowroomConceptTests.cs`다. Character 씬과 신규 PNG/Unity 생성 메타가 함께 바뀐다. 최종 사람 비주얼 인수는 검증 대기다.
- 증거는 `C:\myDefense\_localbuild\P2Validation`의 `showroom-sf-generate-20261122.log`, `showroom-sf-build-final-20261122.log`, `showroom-sf-editmode-final-20261122.xml`, `showroom-sf-player-final-20261122.log`다.

## 2026-11-23 정면 상단 시점 보정

사용자가 지적한 측면 구도를 수정했다. 카메라 좌우 회전을 23°에서 0°로 바꾸고 기본 내려다보는 각도를 60°, 캐릭터 평면 기울기를 25°로 맞췄다. 보드 행은 수평, 열은 수직으로 보인다. `Front overhead`는 60°, `Top down`은 75°다. SF 바닥과 공격 아트는 그대로 사용한다.

- 이번 소스 변경은 `BattleShowroomConceptPreview.cs`, `BattleShowroomCharacter.cs`, `BattleShowroomConceptTests.cs`다. 씬/Prefab 변경은 없다. Unity MCP는 현재 제공 도구에 없다.
- Windows Development 빌드 성공(종료 코드 0), 관련 EditMode **31/31 PASS**. 독립 소스 리뷰 PASS. 카메라 실제 진입점을 호출해 50/60/75/80°에서도 좌우 회전이 없고 기본 각도가 60°인지 검사했다.
- 960×640 Standalone에서 메카론 `Front`, `Front overhead`, `Full board`를 선택하고 `Save preview settings`로 저장했다. 4×6 전체 보드와 수평·수직 정렬, 정면 유닛을 실제 화면으로 확인했다. 사람의 최종 비주얼 승인은 대기다.
- Player 로그에 게임 예외는 없으며 D3D12 info queue 조회 경고(0x80004002)가 한 건 있다. Unity가 자동 변경한 ProjectSettings·솔루션·Fusion sample meta 삭제는 복구했다. 커밋·push는 하지 않았다.
- 실행 파일은 `C:\myDefense\_localbuild\P2Validation\BattleShowroomFront_20261123\BattleShowroomFront.exe`다. 증거는 `showroom-front-build-20261123.log`, `showroom-front-editmode-20261123.xml`, `showroom-front-player-fit-20261123.log`에 있다.

## 2026-11-24 어두운 타일·굵은 외곽선 A/B 비교

사용자가 요청한 비교용 룩을 Character 쇼룸의 `Original / Dark + Bold` 선택으로 추가했다. 원본 PNG 17장과 이전 실행 파일을 보존한다. 이 변경의 최종 디자인 인수는 사용자 확인 대기다.

### 이미지 처리와 복귀

- 사용자에게 타일 1장과 유닛·인형 방향/동작 PNG 16장의 Higgsfield 외부 전송을 구체적으로 확인받은 뒤 업로드했다. Higgsfield 연결의 이미지 가공 sandbox에서 Pillow/NumPy로 처리했으며, AI 재생성이나 유료 생성 모델은 사용하지 않았다.
- SF 타일을 차콜·남색 중심으로 낮추고 청록/보라 표시등의 색을 유지했다. 원본 8bit 평균 명도 114.64 → 후보 51.17이다. 유닛은 기존 렌더링 알파 경계 안쪽 약 6px에 짙은 먹선을 적용한다. 안쪽 얼굴·장식·색·포즈와 Effects 아트는 유지한다.
- 후보는 `Assets/Art/Battle/ShowroomConcepts/VisualVariants/DarkBold`에 별도 PNG 17장으로 저장했다. 원본과 모든 알파 바이트·캔버스 크기가 동일하다. 기존 프레임 rect·발 피벗·PPU를 그대로 재사용한다.
- 룩 전환은 텍스처와 룩별 Sprite/Mesh 캐시를 사용하며 재생 상태·시간·카메라·방향·인형 수를 초기화하지 않는다. `Original`로 즉시 기존 룩에 복귀한다. Reset 기본값도 Original이다. 후보 누락·치수/알파 불일치는 unavailable로 알리며 자동 대체로 통과 처리하지 않는다.

### 변경과 검증

- 이번 코드 변경 5개: `BattleShowroomCharacter.cs`, `BattleShowroomConceptPreview.cs`, `BattleShowroomTileAtlas.cs`, `BattleShowroomSceneBuilder.cs`, `BattleShowroomConceptTests.cs`. Character 씬과 신규 PNG/메타는 Unity Editor API로 임포트·생성했다. Unity MCP 제공 도구는 없었고 대상 worktree `Client`의 Unity 6000.3.4f1을 사용했다.
- 컴파일 오류 0, Character 생성 및 Windows Development 빌드 종료 코드 0, 관련 EditMode **34/34 PASS**(skip 0). 새 검사는 후보 치수/전체 alpha, 동일 프레임·발 피벗, A→B→A 상태 유지와 missing 거부다. 전체 EditMode는 이번 외형 변경에서 재실행하지 않았다.
- 로컬 전수검사와 독립 리뷰에서 후보 17/17의 알파·치수 동일 및 원본 SHA-256 불변을 재확인했다. 후보 참조는 격리 Character 테스트 씬만 포함한다. Production Build Settings는 Battle 씬만 유지하고 Development build guard를 유지한다. 독립 코드·증거 리뷰 PASS(필수 수정 0).
- 960×640 실제 Standalone에서 4개 프리셋의 후보 표시, 정면/측면/후면 외곽선, 디아드/인형 공격 도중 A/B 전환, 인형 수 유지, Original 복귀, 메카론 공격과 퍼펫티어 2안 스킬 재생을 확인했다. 최종 창은 메카론 Front·Front overhead 60°·Dark + Bold·Idle로 설정 저장 후 열어두었다.
- Player 로그 게임 예외 0. 기존 D3D12 info queue 조회 경고(0x80004002) 한 건이다. Unity가 자동 변경한 ProjectSettings·솔루션·Fusion sample meta 삭제는 복구했고 staged/commit/push 0이다. User/System·Shared·서버·경제 변경은 없다.
- 실행 파일: `C:\myDefense\_localbuild\P2Validation\BattleShowroomDarkBold_20261124\BattleShowroomDarkBold.exe`. 증거: `showroom-darkbold-generate-20261124.log`, `showroom-darkbold-editmode-20261124.xml`, `showroom-darkbold-build-20261124.log`, `showroom-darkbold-player-20261124.log`. 이미지 전수검사·원본 해시·정적 비교는 `P2Validation\DarkBold_20261124\asset-validation.json`, `baseline\original-art-sha256.json`, `comparison.jpg`다.


## 2026-11-24 조종실 가독성과 다중 유닛 배치

Character 쇼룸에 `Single / Board placement`를 추가했다. 4×6의 원하는 칸마다 디아드·메카론·퍼펫티어 1안·2안을 혼합 또는 중복 배치할 수 있다. 각 칸에는 본체 한 기만 배치하며 최대 24기다. 이 화면은 외형/동작 설정용이며 실제 Kidnap·Merge·전투 판정 경로를 대체하지 않는다.

### 진입과 조작

- Feature Test Hub → Character → `Board placement`.
- 유닛 종류와 01~24 칸 선택 → `Place / replace`. 칸 순서는 실제 보드 좌상단부터 우하단이다. `Remove selected`와 `Clear all`로 제거한다.
- `4-unit preset`은 네 종류를 8·11·14·17번 칸에 한 기씩 놓고 첫 배치 칸을 선택한다. Canonical ID 외형은 기존 Single에서 확인한다.
- `Actions / direction: all placed units`로 선택 칸/전체 적용을 구분한다. Idle/Move/Basic/Skill·방향·Loop를 적용하고, `Deploy P1 dolls`는 퍼펫티어 1안에만 적용한다.
- 배치·모드·방향·룩을 저장하며 Hub 재진입 시 복원한다. `Save layout and view`는 현재 확대/각도까지 저장하고, `Reset`은 단일 기본 상태와 빈 배치로 돌아간다. 기존 Original/Dark + Bold 룩 비교도 전체 배치에 적용된다.

### 연출과 구조

- 조종실을 보라 광채·짙은 테두리·밝은 심선 3겹의 곡선으로 바꿨다. 화면 기준 광채 6.2px, 테두리 3.5px, 심선 1.6px이며 인형 쪽 끝까지 최소 폭을 유지한다. 대기 심선 alpha는 0.82 이상이고 조종실을 따라 움직이는 빛과 손·인형 연결점에 기존 HandMagic 아트를 사용한다.
- 카메라와 24칸 보드는 owner 한 개만 관리한다. 배치 유닛은 전용 stage와 기존 playback·포즈·공격 렌더를 재사용한다. 다중 모드에서는 기존 단일 본체·인형·그림자를 숨긴다. Full board는 모든 배치 본체의 포즈 bounds를 포함한다.
- 제거/Clear/OnDestroy는 동일한 멱등 소유자원 해제 경로를 사용한다. 공유 원본 아트·카메라·타일·보드 재질을 삭제하지 않는다. 단일/다중 모드 전환 시 배치 인스턴스를 중복 생성하지 않는다.

### 검증 결과

- 변경 4개 코드 파일: `BattleShowroomConceptPreview.cs`, `BattleShowroomCharacter.cs`, 신규 `BattleShowroomPlacement.cs`, `BattleShowroomConceptTests.cs`. 신규 meta는 Unity가 생성했다. SceneBuilder/Shader/PNG/Scene/Prefab 직접 변경은 없다. User/System·Shared·서버·경제 변경은 없다. Unity MCP 제공 도구는 없고 해당 worktree Client를 Unity 6000.3.4f1 batchmode로 실행했다.
- 첫 집중 검증 35/37에서 PreviewScene의 SendMessage 프레임 호출 assert와 child OnDestroy 콜백에 의존한 메시 해제 검사가 실패했다. 실제 Update를 reflection으로 호출하고 명시적 멱등 자원 해제로 보완했으며, 원래 파괴 검사와 검증 범위를 유지했다.
- 최종 관련 EditMode **37/37 PASS**, 실패/skip 0. 실제 24기 혼합 배치·상한·교체·삭제, 카메라/보드 단일 유지, JSON 손상·중복 정규화, A/B·방향·옵션 전환의 재생 상태 보존, 가장자리 프레이밍을 확인했다. 추가로 24기 퍼펫티어 1안 × 6인형 = 144인형·432 활성 조종실의 그래프 상한, 반복 렌더 후 증가 없음, Clear 후 동적 메시·재질 정리를 검사했다. 이는 최대 배치 FPS 측정 결과가 아니다.
- 컴파일 오류 0, Windows Development 빌드 종료 코드 0, 독립 최종 코드·증거 리뷰 PASS(필수 수정 0). 전체 EditMode는 이번 변경에서 재실행하지 않았다.
- 960×640 실제 창에서 네 종류 프리셋과 추가 두 기(총6기), 좌상단/우하단 배치와 잘림 없음, 선택 교체·삭제, Clear all의 0기/잔여 선 없음, 전체 Basic 재생과 Original/Dark + Bold 전환, 단일/다중 모드 복원, Hub를 통한 저장 배치 4기 복원을 확인했다. 최종 창은 Dark + Bold·4종 배치·Idle로 열어두었다. 사람의 최종 디자인 인수와 최대144인형의 실제 FPS 측정은 대기다.
- Player 로그 게임 예외 0, 기존 D3D12 info queue 경고(0x80004002) 한 건. ProjectSettings·솔루션·Fusion sample meta의 Unity 자동 변경은 복구했고 staged/commit/push 0이다.
- 실행 파일: `C:\myDefense\_localbuild\P2Validation\BattleShowroomMulti_20261124\BattleShowroomMulti.exe`. 증거는 `showroom-multi-editmode-final-20261124.xml`, `showroom-multi-build-20261124.log`, `showroom-multi-player-20261124.log`이며 이전 소스 비교본은 `MultiShowroom_20261124\baseline`에 보존한다.

## 2026-11-24 외곽선 누끼 정리와 실제 Battle 아트 연결

사용자는 개발용 Battle에 먼저 외형을 연결하는 범위와, Higgsfield 생성의 `Requires basic plan or higher` 거부 후 기본 이미지 생성 도구로 행성 배경을 제작하는 것을 승인했다. 디아드·메카론·퍼펫티어의 정식 Alien ID/고유 스킬 계약은 아직 없다. Development `-battleArt=MIXED`는 기존 종 ID를 네 외형으로 표시하는 임시 alias다. 기존 기본 공격의 실제 타깃·발사·적중만 표현하며 콘셉트 고유 스킬이 구현됐다는 판정은 하지 않는다.

### 이미지와 실제 연결

- `Assets/Art/Battle/ShowroomConcepts/VisualVariants/DarkBold`의 본체·인형 16개 포즈 PNG를 정리했다. Higgsfield 작업 공간에서 원래 alpha와 1536×512 canvas를 유지하고 외곽·내부 구멍의 RGB matte만 짙은 잉크로 보정했다. 깊은 내부 픽셀은 동일하다. alpha cutoff 바깥 밝은 경계 픽셀은 전수검사에서 **42,895 → 0**이다. 원본 아트는 보존하고 이전 DarkBold 후보는 로컬 baseline에 백업했다. 정당한 흰 장식과 얼굴을 통째로 검게 지우지 않았다.
- 쇼룸의 짙은 SF 타일을 두 보드의 실제 타일 중심 48칸에 연결했다. Collider·슬롯 순서·Lane·Waypoint는 바꾸지 않는다. Original/DarkBold 비교와 유닛 외형 선택은 Development 패널에 있다.
- `Assets/Art/Battle/Planets/{mapId}_Background.png` 9개를 생성했다. NEPTUNE/URANUS/SATURN/JUPITER/MARS/EARTH/VENUS/MERCURY/SUN 각 1672×941이며, 행성은 바깥쪽에 배치하고 전투 보드 뒤 중앙은 어둡고 조용하게 구성했다. Unity Editor API로 9개 환경 Prefab/Profile을 갱신했고 기존 기하학 행성 placeholder를 교체했다. canonical mapId와 Profile fail-fast 검증은 유지한다.
- 유닛별 Windup/Attack/Recover는 실제 발사 이벤트로 재생한다. 메카론 탄·포구광, 디아드 교대 색 연출, 퍼펫티어 인형·3겹 곡선 조종실과 이동하는 빛은 전용 아트를 사용한다. 인형은 기존 1회 투사체를 표현하며 추가 공격/피해를 발생시키지 않는다. 이동·Merge·원본 삭제 이후 비행도 발사 당시 immutable 표현 정보를 사용한다. 승인된 Primary 적중만 persistent Spawner의 일회성 표현 이벤트로 알린다.
- 같은 프레임에 생성·소멸해 발사 정보를 관찰하지 못한 Client는 현재 슬롯에서 외형을 추측하지 않고 해당 보조 FX를 생략한다. 이 경우에도 실제 피해·타깃 상태는 네트워크 전투가 확정한다.
- 실제 2프로세스 진입 중 기존 불투명 `Lab_Ground`가 배경을 가리는 결함을 발견했다. 같은 `Battle.unity`의 정확한 루트·컴포넌트 조합에 해당하는 Renderer만 가역 숨김하며, 실패/placeholder 전환/Clear/Disable에서 원래 enabled 상태를 복원한다. 실제 Scene을 연 회귀에서 발견한 SpriteRenderer의 Editor fake-null 처리도 명시적 Unity null 검사로 고쳤다. 세로 창에서는 중앙 cover가 행성을 잘라내므로 균일 scale을 유지하면서 우상단 focal을 보존하도록 정렬했다. 16:9/4:3/9:16/21:9와 실행 중 zoom/aspect 변경 회귀를 포함한다.

### 변경 영역

| 영역 | 파일/에셋 |
|---|---|
| 실제 Battle 표현 | `BattleUnitArtCatalog.cs`, `BattleSpriteArt.cs`, `BattleUnitArtView.cs`, `BattleUnitArtShot.cs`, `BattlePaintedProjectileView.cs`, `BattleBoardSurfaceView.cs`, 두 전용 Shader |
| Development 진입 | `BattleDevelopmentArtOverride.cs`, `BattleDevelopmentPhotonApplication.cs` |
| 기존 표현 이벤트 연결 | `BattleProjectileSpawner.cs`, `BattleProjectileNetworkState.cs`, `FusionKidnapBoardView.cs` |
| 환경 적용/생성 | `PlanetContentApplicator.cs`, `PlanetContentAssetGenerator.cs`, `BattleArtAssetGenerator.cs`, 9개 환경 Prefab/Profile, presentation Catalog |
| 검증 | `BattleArtPresentationTests.cs`, `PlanetContentTests.cs`, 생성된 Character 쇼룸 Scene과 관련 PNG/meta |

작업 checkout은 `C:\myDefense\_localbuild\worktrees\battle-development-showroom`, 브랜치는 `feature/battle-development-showroom`이다. 기존 미커밋 쇼룸 변경은 보존했다. Unity MCP 제공 도구는 없었으며 Unity 6000.3.4f1의 AssetDatabase/PrefabUtility/EditorSceneManager로 에셋을 생성했다. Scene/Prefab YAML과 meta를 직접 작성하지 않았다.

### 자동 검증과 실제 조작 기록

- 최종 집중 EditMode **116/116 PASS**, 실패/skip 0. 범위는 BattleArtPresentation/PlanetContent/BattleShowroom/BattleProjectile/BattleDamage다. 전체 EditMode를 재실행했다는 의미는 아니다. 최초 추가 Ground 회귀 2건의 Editor SpriteRenderer 실패는 수정 후 동일 검사를 유지해 재실행했다.
- 독립 read-only 리뷰 PASS, 필수 수정 0건. 발사 원본 수명, Session 초기화, 일회성 적중, ground 복원과 실제 XML 결과를 확인했다.
- 최초 동일 PC Host/Client `P1VAL-NEPTUNE-W009-38b937185e04`에서 양쪽 Snapshot 48종 로드, Host의 ID 29~32 생성과 상대 화면 복제, 실제 W009 시작/완료를 확인했다. 이 실행에서 발견한 회색 배경은 위 Ground 보완 대상이었다. 초기 숨겨진 Client 재실행으로 끝난 별도 예비 세션은 최종 접속 성공 증거로 사용하지 않는다.
- Ground 보완 후 `P1VAL-NEPTUNE-W080-e95d88f58daf`의 720×960 실제 창에서 페인트 배경 노출, Host의 네 외형 및 Client의 29번 유닛 생성·상대 보드 복제, 공격 시 방향/포즈 변경, Boss HP 감소와 처치·소멸을 확인했다. Host 로그에 `Boss defeated! Next round criteria cleared.`가 남았다. 이 실행 로그는 `BattleArt_20261124/20261124-182158-{host,client}.log`다. 양 창 HP의 매 프레임 수치 일치나 각 투사체의 명중 프레임 동시 대조, 비행 중 실제 Merge까지 수동 PASS로 확대 해석하지 않는다.
- 127.0.0.1:18082 서버는 별도 프로세스의 `jdbc:h2:mem:BattleArtValidation20261124` 전용 DB다. 실제 계정 DB를 사용하지 않는다. P1VAL에서는 Settlement coordinator 생성·POST를 차단하므로 이번 검사는 보상/Quest/정산 E2E가 아니다.
- Development Standalone 실행 옵션의 Photon AppId는 루트 프로젝트의 현재 설정을 읽어 프로세스 메모리에만 적용한다. Editor/Production에서 이 override를 적용하지 않으며 `PhotonAppSettings.asset`은 수정하지 않는다. 키 값은 보고서와 실행 스크립트에 하드코딩하지 않았다.

이미지 검증·이전 후보·비교 이미지는 `C:\myDefense\_localbuild\P2Validation\BattleArt_20261124`의 `asset-validation.json`, `baseline`, `fringe-comparison.jpg`에 있다. 9개 배경의 생성 주체·정확한 프롬프트·소스/저장 경로는 `planet-generation-manifest.json`에 보존했다. 최종 테스트 증거는 `P2Validation\battle-art-editmode-final-r3-20261124.xml`과 같은 이름의 log다.

실제 두 대 PC·장시간 부하·재접속과 사람의 최종 비주얼 인수는 별도 검증이다. 현재 범위를 정식 캐릭터 고유 스킬 또는 production 통합 완료로 표시하지 않는다. Editor Host의 나머지 MonoBehaviour 부착 경로도 후속 진입 점검에 남긴다. commit/push는 수행하지 않는다.

### 최종 실행 파일과 직접 확인 방법

- Battle Development: `C:\myDefense\_localbuild\P2Validation\BattleArtDev_20261124\Client.exe`.
- 새 쇼룸: `C:\myDefense\_localbuild\P2Validation\BattleShowroomClean_20261124\BattleShowroomClean.exe`. 실행 후 Hub → Character에서 Original/DarkBold와 Single/Board placement를 비교한다. 예전 Multi/DarkBold 실행 파일에는 이번 누끼 보정이 들어 있지 않으므로 새 파일을 사용한다. 새 쇼룸은 빌드 성공을 확인했고, 이번 실제 조작 검사는 위 Battle 두 창에서 수행했다.
- Production 컴파일 검사 산출물: `C:\myDefense\_localbuild\P2Validation\BattleArtProductionCompile_20261124\Client.exe`. Battle.unity만 포함하며 실행 검사용으로 띄우지는 않았다.
- 위 세 최종 빌드 모두 종료 코드 0, C# 컴파일 오류 0. 로그는 `battle-art-dev-build-final-r3-20261124.log`, `battle-art-production-build-final-r3-20261124.log`, `showroom-clean-build-20261124.log`다.
- 최종 사용자 확인용 Session `P1VAL-NEPTUNE-W080-4dbfc2621223`은 Host에 29~32 네 기, Client에 29번 한 기를 배치해 **Wave 시작 전**으로 열어두었다. 양쪽 48칸 SF 타일과 해왕성 우상단 구도, 상대 네 외형 복제를 확인했다. Host의 `Show P1 Fixture` 패널에서 **Start P1 Wave 080 (Host only)**를 누르면 실제 Boss 공격을 시작한다. 이 마지막 새 세션을 이미 Boss 처치까지 완료한 앞선 세션과 혼동하지 않는다.
- 두 창을 닫고 새 행성을 확인할 때는 아래 스크립트를 실행한다. 현재 열어둔 18082 메모리 검증 서버가 필요하며, 종료/재부팅 후에는 서버 재준비가 필요하다. 스크립트는 지정 서버 소유권과 메모리 DB 인자를 확인하고, 다른 서버로 자동 대체하지 않는다. 임의의 기존 Client를 종료하지 않으므로 실행 전에 이전 검증 두 창을 닫는다.

```powershell
& 'C:\myDefense\_localbuild\P2Validation\BattleArt_20261124\Start-BattleArtPair.ps1' -Map NEPTUNE -Wave 80
```

Map 인자는 9개 canonical 행성 중 선택한다. 양 창 접속 후 `Show P1 Fixture`에서 ID **29/30/31/32**를 차례로 Apply ID → NONE → Spawn하면 임시 외형 **디아드/메카론/퍼펫티어1/퍼펫티어2**를 비교할 수 있다. Host에서 지정 Wave 시작을 누르기 전에는 Monster를 자동 생성하지 않는다. `-Width 960 -Height 640`으로 가로 창 비교도 가능하다. 외형 선택 패널은 로컬 Development 설정이므로 양쪽을 같은 Mixed/DarkBold로 맞춰 비교한다.

최종 `final-validation.json`에 116개 테스트, 세 빌드 결과, 준비한 Session, 양쪽 현재 로그의 지정 게임 예외/500 오류 0, 금지 영역 변경 0, staged 0을 기록했다. 기존 D3D12 info queue 조회 경고(0x80004002)는 남아 있다. Git 가드에서 Packages/ProjectSettings/PhotonAppSettings/Fusion sample meta/서버/Shared의 이번 변경은 없으며 Unity 자동 생성 변경은 복원했다. 소스와 에셋은 아직 로컬 미커밋 상태다.

## 2026-11-24 행성 지표형 전장과 유닛 그림체 통합

사용자는 큰 행성 배경과 공통 SF 보드가 유닛과 분리되어 보인다고 지적하고, 행성 특성에 맞는 개별 맵 제작을 요청했다. 앞 절의 우주 배경 판정과 실행 파일은 이 후속 변경의 최종 결과가 아니다. 이번에는 단일 Battle.unity/공통 4×6/Waypoint 정책을 유지하면서 행성마다 지표·배치 타일·실제 Lane·보드 테두리를 같은 재질 세트로 바꾼다. 고유 기믹·난이도·Boss 패턴·정산 계약은 이 시각 작업에서 확장하지 않는다.

### 아트와 적용 범위

| canonical mapId | 전장 표현 |
|---|---|
| NEPTUNE | 짙은 해양 얼음판, 파도형 빙하 외곽과 SF 관측 설비 |
| URANUS | 민트색 각진 빙결 지형, 결정과 냉각 패널 |
| SATURN | 고리 잔해를 이용한 층진 암석·광맥·채굴 기지 |
| JUPITER | 폭풍 무늬, 압력 하우징과 구리색 관측 설비 |
| MARS | 붉은 현무암·철질 지표와 개척 기지 |
| EARTH | 식생·물길이 SF 시설과 이어지는 녹지 복원 기지 |
| VENUS | 황산 웅덩이·황 광물·산화 부식과 밀폐 배관 |
| MERCURY | 크레이터·차폐판·저상 방열 설비 |
| SUN | 검은 내열 세라믹·흑요석·플라즈마 제련 설비 |

이는 게임용 SF 전장 해석이다. 각 행성에 새로운 전투 규칙을 부여하거나 canonical Balance를 수정한 것은 아니다. 외곽선·면 단위 명암·손그림 재질을 기존 chibi Alien과 맞추고, 유닛 아래 중앙은 어둡고 저채도로 유지했다. 기존 유닛 포즈 16종 SHA-256은 앞 절의 누끼 보정 결과와 모두 동일하다.

`Assets/Art/Battle/Planets/Battlefields`에 `{mapId}_Terrain.png` 9개(1024×1536)와 `{mapId}_Surfaces.png` 9개(1254×1254)를 추가했다. 기존 우주 PNG·원본 유닛·SF 타일은 보존한다. 이번 18개 원화는 승인된 기본 이미지 생성 도구로 각각 생성했다. Surfaces 아틀라스의 위 두 칸은 배치 타일 A/B, 아래 왼쪽은 Lane, 아래 오른쪽은 테두리다. VENUS의 실제 가로 경계가 위에서 589px이므로 원본 픽셀을 편집하지 않고 Profile의 UV split 665/1254를 사용한다. 최종 R1은 새 18개 텍스처에 mipmap/Trilinear/aniso4와 16px UV 여백을 적용해 실제 보드 축소에서 alias와 인접 셀 번짐을 줄인다. Unit·기존 SF 텍스처 임포트는 변경하지 않았다.

로컬 읽기 전용 이미지 분석에서 18/18 불투명·독립 SHA를 확인했다. 밝기/명암 정규화 후 구조 비교의 최대 상관은 약 0.465다. 이 수치는 단순 재색칠·동일 이미지 중복을 점검하는 보조 증거이며, 최종 그림체와 아트 승인을 대체하지 않는다.

신규 `PlanetBattlefieldView`는 원래 48개 슬롯 중심과 세 Lane을 읽고 수동 Mesh를 생성한다. 넓은 quiet fill과 연결된 지표 9-slice, 48개 타일, Lane ribbon 및 보드 프레임을 추가 두 Renderer/두 Material로 표현한다. 기존 SF surface 48개는 아래에 남으므로 전체 전장이 Renderer 두 개라는 의미는 아니다. 실제 Transform/Collider/Waypoint/NetworkObject는 추가·변경하지 않는다. 테마 누락은 기존 SF나 우주 배경으로 숨기지 않고 실패 처리한다. Daily와 캐릭터 쇼룸은 기존 적용 범위를 유지한다.

생성 파일과 정확한 프롬프트·출력 경로는 `C:\myDefense\_localbuild\P2Validation\PlanetBattlefields_20261124\generation-manifest.json`에, 치수·해시·구조 분석은 `asset-validation.json`에 보존한다. `PlanetContentAssetGenerator.GenerateBattlefieldContent`의 Unity 실행 로그는 `P2Validation\planet-battlefield-generate-20261124.log`이며 종료 코드 0, C# 오류 0이다. 구현 Thread에 제공된 Unity MCP 도구는 없었고, 지정 worktree Client를 Unity 6000.3.4f1에서 열어 Editor API로 Profile/Prefab/Material/meta를 생성했다. 손으로 YAML/GUID/meta를 작성하지 않았다.

### 검증과 시각 보완

- R0 집중 회귀 199/199 PASS 후 실제 1920×1080 Standalone의 Hub → Map & Content에서 9행성을 모두 눌러 각 지표/타일/주변 설비와 같은 네 유닛을 확인했다. Geometry 표시를 끄고 Comparison units를 켜서 판단했다. 초기 창/화면 연결 실패는 첫 프로세스 종료를 확인한 뒤 `-monitor 1 -force-d3d11 -screen-fullscreen 1 -window-mode borderless`로 재실행해 복구됐다. 자동 화면 연결 실패를 성공으로 기록하지 않았다.
- 이 실제 화면에서 Lane 텍스처의 폭/진행 UV 축이 뒤바뀐 것, 인접 행의 네 비교 유닛이 몸통을 가리는 것, 축소된 타일의 세부 무늬 alias를 발견했다. R1에서 Lane/Border를 U=폭·V=진행/위쪽 normal로 교정하고 회귀를 추가했다. 비교 유닛은 슬롯 7/10/19/22로 두 행 간격을 두었다. 임포트/여백 보완도 위에 기록했다. 이는 Content 비교 배치 변경이며 실제 Battle의 슬롯이나 유닛 위치 규칙 변경이 아니다.
- 최종 R1 집중 EditMode **200/200 PASS**, 실패/skip 0. 범위는 PlanetBattlefield/PlanetContent/BattleArtPresentation/BattleShowroom/BattleProjectile/BattleDamage/Daily다. 실제 Battle preview의 Transform/Collider/세 경로 보존, 48 타일, 실패·교체·legacy 복귀·Clear 정리, UV 방향, 새 텍스처 샘플링, 네 비전투 유닛/전용 60° 미리보기를 검사했다. 전체 EditMode 재실행 결과는 아니다.
- 독립 read-only 코드·XML·18PNG 리뷰 PASS, 필수 수정 0건. 렌더 수는 기존 SF 48개에 추가 두 Renderer이며 FPS 최적화 완료를 뜻하지 않는다.
- 새 Editor 전용 `PlanetBattlefieldPreviewExporter.cs`는 실제 Content PreviewScene·같은 Profile·Shader·4유닛을 720×960으로 렌더링한다. GUI는 제외한다. 초기 이미지는 `PlanetBattlefields_20261124/Preview`에 보존하며, 최종 R1의 9 PNG도 `PlanetBattlefields_20261124/PreviewR1`에 생성 완료했다(exit 0). 원본 PNG를 합성해 Unity 실행인 것처럼 표시하지 않는다. PreviewScene/RT/readback/환경/생성 유닛/Prefs는 finally에서 복원한다. 최종 실행 로그는 `planet-battlefield-preview-render-r1-20261124.log`다.

최종 회귀 증거는 `P2Validation\planet-battlefield-editmode-r1-20261124.xml`과 log다. 소스는 같은 feature 브랜치의 로컬 미커밋 상태이며 commit/push는 승인 전 수행하지 않는다. 사람의 최종 디자인 인수와 실제 2PC 검증은 별도다.

### 최종 빌드·실제 조작 결과

다음 세 R1 빌드가 모두 종료 코드 0, C# 컴파일 오류 0으로 완료됐다. Production은 컴파일/패키징 검사이며 실제 운영 접속은 수행하지 않았다.

| 용도 | 최종 실행 파일 | 빌드 로그 (`P2Validation` 기준) |
|---|---|---|
| 격리 Development 쇼룸 | `C:\myDefense\_localbuild\P2Validation\PlanetBattlefieldShowroom_20261124\Showroom.exe` | `planet-battlefield-showroom-build-r1-20261124.log` |
| 실제 Battle Development, Battle.unity 단독 | `C:\myDefense\_localbuild\P2Validation\PlanetBattlefieldClient_20261124\Client.exe` | `planet-battlefield-client-build-r1-20261124.log` |
| Production 컴파일 검사, Battle.unity 단독 | `C:\myDefense\_localbuild\P2Validation\PlanetBattlefieldProduction_20261124\Client.exe` | `planet-battlefield-production-build-r1-20261124.log` |

- 최종 R1 Standalone에서 SUN과 NEPTUNE을 직접 선택해 Lane 방향, 비교 유닛의 몸통 간격, 타일 축소 표시를 확인했다. 앞선 R0의 9행성 실제 전환에 더해 최종 R1 9행성 모두를 같은 카메라/네 유닛으로 Unity 렌더링했다. 시각 결함 수정 확인과 사용자의 최종 아트 승인은 구분한다.
- 새 세션 `P1VAL-NEPTUNE-W009-65ce056627e9`에서 동일 PC 두 Standalone을 연결했다. 양쪽의 실제 Mythic 29 NONE 생성과 상대 화면 복제를 확인했다. Host에서 W009를 시작하고 Monster 이동·공격 포즈·처치를 관찰했다. Host 로그에 `Round 9 (REGULAR) started`, `regular wave spawn completed`, `Round 9 regular wave completed`가 남았다. 양쪽 Attack Snapshot catalog 로딩과 Client Lane remap도 확인했다.
- 해당 실행의 신규 게임 예외·HTTP 500·PlanetContent 생성 실패는 없었다. 증거는 `PlanetBattlefields_20261124/20261124-210316-host.log`, `20261124-210316-client.log`, `pair-smoke-65ce056627e9.json`이다. 양쪽 P1VAL에서 정산 제출은 차단됐으며 실제 보상/Quest E2E 검사는 하지 않았다. 서버는 loopback 18082의 전용 H2 메모리 DB만 사용했다. Photon 키는 기존 키를 Development 메모리에만 주입했고 설정 asset을 수정하지 않았다.
- 이 결과는 **동일 PC 2클라이언트 Smoke Test**다. 실제 2PC·장시간·매 프레임 HP/Projectile 시점 일치·9행성 난이도 승인까지 통과했다는 의미는 아니다.
- 실제 Battle 카메라와 플레이 영역은 그대로다. 세로 Battle 화면에서는 바깥 장식 일부가 잘리고 지표·타일·Lane을 중심으로 보인다. Content 쇼룸의 60° 전용 카메라는 주변 지형까지 맞춰 보여준다.

### 다시 실행·비교하는 방법

최종 쇼룸의 `Map & Content`에서 NEPTUNE과 네 비교 유닛을 표시한 상태로 열어두었다. 왼쪽 행성 버튼을 누르면 같은 배치에서 행성별 아트를 비교한다. `Show board slots, lanes and spawn points`는 끄고 `Show four comparison units (visual only)`는 켜면 디버그 표시 없이 비교할 수 있다. 비교 유닛은 비전투 외형이며 실제 전투 검사는 별도 Battle 세션을 사용한다.

```powershell
& 'C:\myDefense\_localbuild\P2Validation\PlanetBattlefields_20261124\Start-PlanetShowroom.ps1'
# 창 모드가 필요할 때:
& 'C:\myDefense\_localbuild\P2Validation\PlanetBattlefields_20261124\Start-PlanetShowroom.ps1' -Windowed
```

Hub로 시작되면 `Map & Content`로 들어간다. 이 비교에는 로그인·매칭·서버가 필요 없다. 실제 Battle 재현은 같은 폴더의 `Start-PlanetBattlePair.ps1 -Map NEPTUNE -Wave 9`를 사용한다. 이 스크립트는 기록된 격리 메모리 서버가 실행 중일 때만 동작하며, 일반 서버로 대체 접속하지 않는다. P1VAL의 Wave는 기존 Fixture에서 수동 시작한다.

### 변경 파일과 남은 판정

이번 지표형 맵의 주요 코드 변경은 `PlanetContentProfile.cs`, `PlanetContentCatalog.cs`, `PlanetContentApplicator.cs`, 신규 `PlanetBattlefieldView.cs`, `PlanetContentAssetGenerator.cs`, `BattleShowroomContent.cs`, `PlanetContentTests.cs`, 신규 `PlanetBattlefieldTests.cs` 및 `PlanetBattlefieldPreviewExporter.cs`다. 신규 아트 18 PNG, 9 Battlefield Material, 9 Profile과 9 환경 Prefab의 Editor API 생성 결과가 함께 포함된다. 보고서는 이 파일이며 작업 현황은 `docs/98_IMPLEMENTATION_TASKS.md`에 기록한다. 앞선 쇼룸·실제 Battle 외형 작업의 미커밋 파일은 보존했다.

User/System·Shared·Spring·경제·Balance 데이터는 이번 작업에서 변경하지 않았다. Packages/ProjectSettings/PhotonAppSettings 및 Fusion sample meta 삭제 등 Unity 자동 변경은 최종 Git 가드에서 복원 완료했다. 금지 경로 변경 0, `git diff --check` 통과, staged 0, 기존 HEAD `54e3ee736028e34efcefc708065c8b721c99bbe9` 유지, commit/push 미수행이다. 최종 가드/빌드/테스트 경로는 `PlanetBattlefields_20261124/final-validation.json`에 기록했다.

남은 인수는 사용자의 행성별 색·유닛 가독성·그림체 확인, 실제 Battle 세로 프레임 확인, 기존 실제 2PC·장시간 게이트다. 정식 Alien ID/고유 스킬 연결, Daily 신규 맵 제작, 신규 Boss/지형 기믹은 이번 9행성 시각 변경의 완료 범위가 아니다.

### 실제 2창 Battle 화면으로 전환 — 2026-11-24 후속

사용자가 원하는 확인 화면은 Map & Content 쇼룸이 아니라 기존 Kidnap/Fixture 버튼이 있는 실제 Battle 두 창임을 재확인했다. 비교 쇼룸을 닫고 위 최종 Battle Development 실행 파일로 새 세션 `P1VAL-NEPTUNE-W009-ddceb80cb1f0`를 실행했다. Host `dev-art-host` / Client `dev-art-client`가 같은 Fusion 세션에 접속했다. 실제 Client Fixture에서 Alien 29 NONE(개발용 디아드 외형), Host Fixture에서 Alien 30 NONE(개발용 메카론 외형)을 생성했고 양쪽 원격 복제를 확인했다. Host의 `Start P1 Wave 009`로 실제 Monster 이동·공격·처치를 진행했으며 W009 완료 로그를 확인했다. 두 창은 사용자 조작을 위해 실행 상태로 남겼다. 화면에 Wave 9와 잔존 수 0이 보이면 방금 시작한 검증 Wave가 클리어된 상태다.

로그는 `PlanetBattlefields_20261124/20261124-211924-host.log`, `20261124-211924-client.log`, 현재 실행 정보는 `pair.json`이다. 이전 검증 세션의 실행 기록은 위 `pair-smoke-65ce056627e9.json`에 보존했다. 게임 예외/HTTP 500은 0이다. 이 후속은 기존 최종 빌드의 실제 화면 실행이며 소스·에셋을 추가로 바꾸거나 재빌드하지 않았다.

`Mixed`는 기존 canonical 종의 외형을 개발용으로 대응시키는 옵션이다. Fixture ID 29=디아드, 30=메카론, 31=퍼펫티어 1, 32=퍼펫티어 2로 비교할 수 있지만 정식 종/스킬 계약은 아니다. 기본 공격·Projectile·Hit·HP는 실제 전투 경로를 사용한다. 쇼룸의 고유 스킬 시연을 해당 canonical 종의 실전 스킬로 새로 편입했다는 의미는 아니다. 일반 소환은 하단 `왹져 소환` 버튼을 사용하고, 지정 비교는 `Show P1 Fixture → ID → Apply ID → Mutation → Spawn`을 사용한다. 새 Wave 검증은 기존 두 창을 닫고 위 Pair 스크립트를 다시 실행해 새 세션에서 시작한다. 기존 실행 중 Wave 숫자만 변경하지 않는다.
