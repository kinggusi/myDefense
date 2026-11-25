# 실제 Battle HUD 디자인 통합 — 2026-11-24

상태: 구현·집중 자동 검증·독립 리뷰·Windows 빌드 통과. 실제 두 창의 납치/공명/Mutation 조작과 화면 배치를 확인했다. 사용자 최종 디자인 인수와 실제 손으로 하는 World 드래그는 검증 대기다. 대상은 쇼룸이 아닌 기존 `Assets/Scenes/Battle.unity`의 Host/Standalone Client 전투 화면이다.

## 요청과 변경 경계

사용자가 제공한 메인·상점·내 유닛 디자인의 짙은 남색, 얇은 금속 테두리, 청록색 주요 버튼, 흰색 타이포를 Battle에 맞춘다. 기존 빨강/초록 대형 참가자 바와 고정 크기 소환·변이·공명 UI의 겹침을 정리한다.

- 상단: 실제 Session의 행성·Wave·참가자·필드별 Monster 수와 상태.
- 하단: 영구 재화와 구분한 전투 Gold·Kidnap 버튼·Mutation/공명 상세.
- 맵: 기존 9행성 지표와 4×6/Waypoint를 보존하고 HUD 예약 공간·전장 프레임을 정리.
- 개발 버튼: Fixture와 외형 설정을 제품 HUD와 구분된 접힘 영역에 배치.
- 입력: UI를 조작할 때 뒤의 World 유닛 선택·드래그가 시작되지 않게 차단.

기존 `RequestKidnap`, `RequestMutation`, `RequestResonanceUpgrade`, `RequestMythicChoice`, `RequestMythicReroll` 및 State Authority의 비용/확률/승인 경로를 재사용한다. 서버·Shared·경제·캐릭터 수치·Merge·Injector 규칙은 이 변경의 수정 대상이 아니다. 정식 캐릭터 ID나 고유 스킬을 신규 등록하지 않는다.

계획 단계와 구현을 분리했으며, PM이 읽기 전용 조사·파일 계획을 검토한 뒤 구현 담당에게 편집을 승인했다. 구현자는 자기 작업의 최종 승인을 하지 않고 독립 read-only 리뷰를 받는다.

## 구현 및 검증 기록

신규 코드는 `BattleHudLayout.cs`, `BattleHudGraphic.cs`, `BattleHudPresentation.cs`, `BattleHudPresentationTests.cs`다. 실제 UI 진입은 기존 `FusionBattleUiController.Awake`로 연결하며 Editor/Development에만 제한하지 않는다. 기존 요청 adapter는 같은 controller 안에 유지한다. `FusionKidnapUnitDrag.cs`는 HUD/Modal/Drawer 위의 시작·진행·release에서 World 명령 대신 원위치 취소를 수행한다. `FusionKidnapBoardView.cs`의 기존 디버그 Grade/Mutation marker Renderer는 같은 Scene HUD가 상태를 저장해 가역 숨긴다.

`BattleSpectatorCameraController.cs`는 새 HUD가 있을 때 기존 회색 관전 OnGUI box만 생략한다. 카메라 관전 판정/이동은 그대로다. `DevelopmentMythicMutationFixturePanel.cs`와 `BattleDevelopmentArtOverride.cs`는 토글 위치·상호배타 drawer·스크롤만 정리하며 Fixture 생성/권위 규칙은 유지한다. 기존 `FeatureTestCatalog`의 실제 Battle `P0-2-5` case에 자동 검사·사람 체크리스트를 추가한다.

새 HUD는 원본 exact legacy Graphic/Selectable 상태, camera rect/orthoSize, 임시 font/Canvas/EventSystem 소유권을 저장·복원한다. Scene/Prefab/YAML을 편집하지 않았고 새 C# meta는 지정 worktree Client의 Unity 6000.3.4f1 임포트가 생성했다. 현재 Thread의 Unity MCP 제공 도구는 없었다. 기존 행성 PNG 18/18 SHA-256 불변은 `planet-art-preservation.json`에 기록했다. 로비 소스·원화·ProjectSettings·Packages·Shared·서버는 구현자가 수정하지 않았다.

최종 집중 EditMode **282/282 PASS**, 실패/skip 0, C# 컴파일 오류 0. 범위는 새 HUD 17case와 WaveHud·관전·Authority·Art/Showroom·Planet·Projectile/Damage·Daily·FeatureTest 회귀다. 전체 EditMode 실행 결과는 아니다. 최종 증거는 `C:\myDefense\_localbuild\P2Validation\battle-hud-editmode-r4-20261124.xml`과 `.log`다. 과거 200/200 행성 맵 결과를 이번 HUD의 실행 결과로 재사용하지 않았다.

초기 새 검사 10case는 PreviewScene의 EventSystem 등록 전 current 설정 때문에 Setup에서 실패했다. 로그를 무시하지 않고 실제 OnEnable/OnDisable 등록·해제를 테스트 하네스에 적용해 R1 275/275를 통과했다. 독립 검토에서 발견한 Production의 보이지 않는 Dev 입력 영역, legacy 경고색 소실, Destroy된 camera 교체 복원도 보완했다. 경고색은 도달 이력 flag가 아닌 현재 Networked count와 기존 canonical threshold를 사용한다. P2 내/동료 반전, Daily 비활성, 탈락 우선, 93→85→79 감소 및 임의 canonical 300/400 threshold를 검사한다. R2 280/280, 실제 camera Destroy→replacement→Hide 복원 회귀를 추가한 R3 281/281 후, 실제 화면에서 발견한 제품/개발 drawer 동시 열림을 수정했다. 양방향 배타·stale 입력 영역 해제·실제 Button.onClick·비활성 muted 문구·+/− 탭 상태 검사를 추가한 최종 R4가 위 282/282다. 독립 읽기 전용 최종 소스/XML 리뷰 PASS, 필수 수정 0건이다.

Windows에서는 설치된 Malgun Gothic이 있으면 임시 runtime font로 한글을 표시하고 정리한다. 없으면 기존 LegacyRuntime font를 사용한다. 현재 Windows의 필수 한글 글리프 검사는 통과했다. 모바일용 공통 내장 font Asset 확정·기기 검사는 별도다.

## 최종 빌드와 실제 조작

- 실제 Battle Development: `C:\myDefense\_localbuild\P2Validation\BattleHudClient_20261124\Client.exe`.
- Production 컴파일/패키징 검사: `C:\myDefense\_localbuild\P2Validation\BattleHudProduction_20261124\Client.exe`. Production은 실행하지 않았다.
- 최종 R4 로그: `P2Validation\battle-hud-client-build-r4-20261124.log`, `battle-hud-production-build-r4-20261124.log`. 양쪽 종료 코드 0, C# 오류 0이며 BuildPlayer의 Scene 목록은 `Battle.unity`만이다. Build Settings 자체를 바꾸지 않았고 테스트 Scene은 포함하지 않는다.

R3 실제 두 창 세션 `P1VAL-NEPTUNE-W009-533d734f275f`에서 다음을 직접 조작했다. 실행 기록은 `pair-r3-533d734f275f.json`, 로그는 `20261124-215953-host.log`, `20261124-215953-client.log`다.

| 조작 | 실제 결과 |
|---|---|
| Client 납치 1회 | 전투 Gold 100,000→99,950, 다음 비용 50→60 G, Host 화면에 복제 |
| Host 납치 1회 | Host Gold 100,000→99,950, 다음 비용 60 G. Client 지갑과 독립 |
| Host 일반 공명 | 400 G 차감, 99,950→99,550, Lv.0→1 및 다음 비용 800 G 표시 |
| Mythic Fixture 29 NONE | 첫 빈 슬롯 생성, Gold 99,550 유지, 상세 Slot 2/Alien29 표시 |
| 실제 Mutation 활성화 | 300 G 차감, 99,550→99,250, 권위 응답 OBESE 활성/재변이 비용 600 G 표시 |
| 제품·개발 패널 동시 열기 | 겹침 발견. R4 배타 처리로 수정하고 새 회귀/실제 재확인 |

최종 R4의 960×540 새 2클라이언트 세션 `P1VAL-NEPTUNE-W009-79fa7d28335a`에서 가로 Header/CTA/World 표시, 공명 열기→Fixture로 전환 시 제품 패널 닫힘, Fixture→생체변이 전환 시 개발 패널 닫힘, 신화 없음의 disabled muted 표시를 직접 확인했다. Fixture 내용은 World 높이에 맞춘 스크롤 안에 있으므로 작은 가로 창의 아래쪽 버튼은 스크롤해 사용한다. 실행 기록은 `pair-r4-landscape-79fa7d28335a.json`이다.

현재는 최종 R4 세로 800×960 두 창을 새 세션 `P1VAL-NEPTUNE-W009-f5f8044b21ed`로 열어두었다. 각 창의 실제 Kidnap을 1회씩 수행해 Gold 99,950/다음 60 G와 상대 복제를 재확인했다. 현재 실행 기록은 `pair.json`, 로그는 `20261124-221323-host.log`, `20261124-221323-client.log`다. 선택 Wave는 W009이나 자동 시작은 보류하므로 `WAVE 000`이면 연결 실패가 아니라 아직 Fixture에서 시작하지 않은 상태다. Host가 `Show P1 Fixture → Start P1 Wave 009`를 눌러 시작한다.

이 작업의 실제 검사는 동일 PC 두 Standalone이다. 실 2PC·장시간/재접속·새 UI에서 신화 선택 실전 Merge·종료/관전 전체 E2E·모바일 기기까지 통과했다는 의미는 아니다. 해당 상태들의 자동 UI 검사와 사람/실전 검사는 구분한다. P1VAL은 Settlement POST가 차단되고 loopback 격리 메모리 서버만 사용하므로 실제 보상/Quest 검사는 하지 않았다.

### World 드래그 미확인

자동 Native 드래그에서 시작 로그/이동을 확인하지 못했다. 원본 primitive 외형과 새 외형에서 여러 시작 좌표로 시도했지만 결과를 확정하지 못했으므로 PASS로 기록하지 않는다. 구현자와 독립 리뷰어가 실제 Battle Scene 계층을 읽기 전용으로 조사한 결과, raycastTarget Graphic 11개는 모두 숨김 대상 legacy HUD 하위이며 별도 전체 화면 투명 UI는 없었다. 새 WorldFrame/Matte/Label은 raycastTarget=false이고 Unit Collider 및 기존 입력 모듈은 활성이다. 따라서 UI 전체 차단을 원인으로 확정하지 않았다. 좁은 원본 Collider 적중 위치와 자동 입력의 누름/유지 프레임 전달을 분리해야 하며, 사용자에게 직접 빈 타일 드래그 확인을 요청했다. 추가 trace나 임의 조작 우회 로직은 넣지 않았다.

재실행은 기존 두 창을 닫고 다음을 사용한다.

```powershell
& 'C:\myDefense\_localbuild\P2Validation\BattleHud_20261124\Start-BattleHudPair.ps1' -Map NEPTUNE -Wave 9
```

이 스크립트는 지정된 loopback H2 메모리 서버가 실행 중일 때만 동작하며 다른 서버로 대체하지 않는다. 기존 Photon 키는 Development 메모리 인자로만 사용하고 설정 asset은 편집하지 않는다. 이후 사용자 승인 전까지 commit/push하지 않는다.

최종 Git 가드에서 Unity 자동 변경인 sample meta 삭제·솔루션/ProjectSettings/`.vsconfig`만 초기 상태로 복원했다. 금지 경로(Packages/ProjectSettings/Photon/Shared/서버) 변경 0, staged 0, `git diff --check` PASS, HEAD `54e3ee736028e34efcefc708065c8b721c99bbe9` 유지다. 이번 HUD 이전의 미커밋 Battle/쇼룸/행성 아트 작업은 보존했다. 세 검증 세션의 양쪽 로그 6개에서 게임 예외·MissingReference·HTTP 500은 0이다. 최종 기계 판독 기록은 로컬 증거 폴더의 `final-validation.json`에 남긴다.

로컬 증거 폴더: `C:\myDefense\_localbuild\P2Validation\BattleHud_20261124`.

## 사용자 확인 체크리스트

1. 실제 Battle 두 창에서 상단 행성/Wave와 내 필드·동료 필드 수치가 맞는지 확인.
2. 납치 버튼의 비용·전투 Gold·납치 후 새 비용을 확인하고 상대 창에 유닛이 복제되는지 확인.
3. Mutation/공명 패널을 열고 닫아 전장을 덮는 범위와 버튼 가독성을 확인.
4. UI 클릭·드래그 시 뒤의 유닛이 움직이지 않고, 패널 바깥의 정상 유닛 드래그는 동작하는지 확인.
5. 좁은 세로와 가로 창에서 상단·하단 버튼·개발 도구·신화 후보가 겹치거나 잘리지 않는지 확인.
6. 전투 종료·관전·연결 대기 상태에서 수치·안내와 버튼 상태가 맞는지 확인.

`jjangash`: 검증 대기. `kinggusi`: 검증 대기. 자동/AI 검증과 두 사람의 디자인·조작 인수는 구분한다. commit/push는 별도 명시 승인 전까지 하지 않는다.

## 이후 작업 후보와 선행 조건

| 순서 | 작업 | 선행 조건 / 담당 |
|---|---|---|
| 1 | 이번 실제 Battle HUD 디자인·조작 인수와 동료 메인 UI의 최종 스타일 맞춤 | jjangash/kinggusi 사람 확인, 승인 후 Git 반영 |
| 2 | 디아드·메카론·퍼펫티어의 정식 Alien ID·고유 공격/스킬 연결 | User/System의 종 ID·스킬 수치/발동 계약, 퍼펫티어 두 안의 채택/비교 정책 |
| 3 | 실제 전투 Monster/Boss·공격·처치 연출의 아트 통합 | Battle 소유 범위, 사용할 최종 Asset·프레임/Effect 기준 확정 |
| 4 | Daily S1~S5의 일반 구성 난이도·DoT/Splash 실피해·종료 조작 회귀 | 기존 canonical/격리 검증 세션 사용, trusted Daily Result E2E는 Adapter 제공 후 |
| 5 | 튜토리얼 Battle 연습 Launcher/완료 증거 Adapter 연결 | User/System·Shared와 연습 진행/재개·완료 증거 계약 협의 |
| 6 | 9행성 난이도 실플레이 및 실제 2PC·장시간/재접속 | 사용자 일정에 맞춰 진행. 앞선 사용자 요청으로 실플레이/실제 2PC는 후순위 유지 |
| 7 | 무한 Wave·스킨 상품 연결 등 신규 P2 | 모드/시즌/보상 공식, ID/API/Asset 계약 제공 후 |

Production JWT/matchmaking·모바일 인증 E2E는 기존 사용자 결정에 따라 실오픈 준비 시점까지 유예하며, 준비 전 fail-closed 경계는 유지한다.
