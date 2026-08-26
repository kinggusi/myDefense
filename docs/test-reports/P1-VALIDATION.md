# P1 Battle 통합 검증 보고서

## 1. 검증 기준

- 브랜치: `feature/battle-p1-effects-playtest`
- 기준 HEAD: `e17c702`
- 검증일: 2026-08-24
- Unity: `6000.3.4f1`
- Unity 프로젝트: `C:/myDefense/Client`
- Scene: `Assets/Scenes/Battle.unity`
- Fusion Session: `MyDefense-Dev`
- Host: Unity Editor, Player 1, `dev-host`
- Client: Windows Standalone, Player 2, `dev-client`
- 대상 Task: `P1-1-6`, `P1-2-4~6`, `P1-3-6`, `P1-4-6`

## 2. 종합 판정

**WARNING / 검증 대기**

Battle 코드 수정, 자동 테스트, Windows 빌드, 동일 PC의 Editor Host + Standalone Client 기본 동기화 검증은 통과했다. 코드 차단 결함은 없다. 다만 8종 Mutation 전체 실제 Hit, Legendary-to-Mythic 후보·리롤, Boss timeout, 9행성·80 Wave 장시간 난이도와 양쪽 담당자의 사람 검증 기록이 남아 있어 P1 Task를 `완료`로 승격하지 않는다.

| 검증 영역 | 결과 | 근거 |
|---|---|---|
| Unity MCP 호출과 프로젝트 연결 | PASS | `C:/myDefense/Client`, `Battle.unity` 확인 |
| Battle Scene 단일 Windows 빌드 | PASS | Development clean/strict build, Error 0 |
| 관련 Unity EditMode | PASS | 40/40 |
| Unity EditMode 전체 회귀 | PASS | 364/364 |
| Editor Host + Standalone Client 연결 | PASS | Player 1=`dev-host`, Player 2=`dev-client` |
| 기본 Networked 상태 동기화 | PASS | Wave, alive, Kidnap, Gold, 보드, Lane 대조 |
| Scene/Prefab 안전 | PASS | issue 0, Missing Script 0, Broken Prefab 0 |
| 독립 읽기 전용 리뷰 | WARNING | 차단 결함 없음, 필수 라이브 검증 잔여 |
| jjangash 사람 검증 | 대기 | 결과 기록 없음 |
| kinggusi 사람 검증 | 대기 | 결과 기록 없음 |

## 3. 발견 오류와 조치

### Mutation Aura EditMode 오류

- 현상: EditMode 테스트에서 생성된 Aura Collider 정리 시 `Destroy` 호출 오류.
- 조치: EditMode에서는 `DestroyImmediate`, PlayMode에서는 `Destroy`를 사용하도록 분기했다.
- 결과: 관련 테스트와 전체 회귀 통과.

### Mutation Material 인스턴스 누수

- 현상: Mutation 색상 변경 시 `renderer.material` 접근으로 Material 인스턴스가 생성됨.
- 조치: `MaterialPropertyBlock`으로 `_Color`와 `_BaseColor`를 적용했다.
- 결과: Material 누수 오류 제거, Mutation 표시 테스트 통과.

### Fusion Spawn 전 Networked 속성 접근

- 현상: Standalone 초기화 시 `UnitAttack`이 `BattleWaveStateAuthority` Spawn 전에 Mythic 선택 Networked 값을 읽어 `InvalidOperationException` 14회 발생.
- 조치: `IsSpawnedForAccess`를 확인한 뒤에만 Networked 값을 읽도록 guard를 추가했다.
- 결과: 회귀 테스트 추가, 수정 빌드 런타임에서 관련 예외 0회.

### Fusion/Legacy Drag 중복

- 현상: Fusion Unit에서 Fusion Drag와 기존 `AlienMergeHintView`가 함께 활성화되어 중복 입력 경고 발생.
- 조치: Fusion 경로에서 Instantiate한 Unit에 한해 legacy `AlienMergeHintView`를 비활성화했다.
- 결과: Fusion Drag enabled 1, legacy handler enabled 0 확인.

### Battle Gold UI 잘림

- 현상: 좁은 GameView에서 Gold 마지막 숫자가 버튼 뒤에 가려지거나 줄바꿈됨.
- 조치: Gold Text를 단일행 Overflow로 설정하고 폰트 크기를 최대 42px로 제한했다.
- 결과: 1080px Host 화면에서 `Gold: 100,090` 전체 표시 확인.

## 4. 변경 파일

- `Client/Assets/Editor/Tests/BattleDamageContractTests.cs`
  - 보드 이동·Swap·Merge 후 authoritative Unit 상태와 Active Mutation 표시 회귀 테스트.
- `Client/Assets/Editor/Tests/BattleWaveStateAuthorityTests.cs`
  - Spawn 전 Networked Mythic 선택 접근 방지 회귀 테스트.
- `Client/Assets/Scripts/Battle/Presentation/FusionBattleUiController.cs`
  - Gold 단일행·폰트 보정.
- `Client/Assets/Scripts/Battle/Presentation/FusionKidnapBoardView.cs`
  - authoritative Unit 메타데이터·Mutation 표시 동기화 및 Fusion 경로 legacy drag 비활성화.
- `Client/Assets/Scripts/Battle/Presentation/MutationAuraView.cs`
  - Play/EditMode 안전 정리와 MaterialPropertyBlock 색상 적용.
- `Client/Assets/Scripts/Units/UnitAttack.cs`
  - Fusion Spawn 완료 전 Networked 접근 차단.

Shared 계약, 서버, Settlement, 재화 공식, Excel, Balance 원천은 변경하지 않았다.

## 5. 빌드와 자동 테스트

- 출력: `Client/_localbuild/FusionClient/Client.exe`
- 구성: `Assets/Scenes/Battle.unity` 단일 Scene
- Target: Windows Standalone 64-bit Player
- Development/Clean/Strict/Detailed Report
- 최종 빌드 시간: 30.45초
- 최종 크기: 151.31MB
- Build Error: 0
- Build Warning: 14, 기존 compile/service 계열
- EXE SHA-256: `E3615DDFFAD813E2F638766EA79545239BF5CAF4C6AEC954787406DE2BF6146F`
- 관련 EditMode: 40/40 PASS
- 전체 EditMode: 364/364 PASS
- `git diff --check`: PASS

## 6. Fusion 2클라이언트 결과

### PASS

- 동일 PC에서 Unity Editor Host와 Standalone Client를 별도 프로세스로 실행.
- `MyDefense-Dev` Session에 Player 1=`dev-host`, Player 2=`dev-client` 참가.
- 양쪽 Wave와 Player 1/2 alive 상태 일치.
- Host authoritative Kidnap 후 Gold, Kidnap 횟수와 첫 슬롯 배치가 Client에 복제됨.
- 개인 Lane의 로컬 원근 remap 확인.
- Move, Swap, Merge, Active Mutation과 일반·신화 공명 상태의 양쪽 동기화 확인.
- 수정 빌드에서 `InvalidOperationException`, `NullReferenceException`, legacy drag 경고와 `UnitAttack` 관련 오류 0회.
- Editor Console Error 0.

### 라이브 검증 대기

- `GIANT`: 실제 Splash 피해와 주변 대상 HP 동기화.
- `BERSERK`: Boss 대상 단일 피해 배율.
- `SWIFT`: 실제 공격 간격 변화.
- `TOXIC`: 권위 DoT Tick과 중복 Hit 방지.
- `GREEDY`: 적중 기반 개인 Gold 1회 지급.
- `OBESE`: 성공/실패 피해 중 하나만 권위 적용.
- `FROZEN`: Slow 적용·지속·복구 및 위치 동기화.
- `BLANK`: 추가 전투 효과 없음.
- Legendary-to-Mythic 후보 3종, 무료/유료 리롤, 선택과 timeout.
- Boss timeout `FAILED`, Boss 처치 후 다음 Wave, Wave 80 종료.
- 9행성 HP·속도와 권장 스펙 장시간 난이도.
- 재접속과 종료·Settlement 전체 흐름.

정상 Settlement E2E는 `P1-5-7` trusted roster adapter 선행 미완료로 이번 Battle 수정 범위에서 제외한다.

## 7. 사람 검증 체크리스트

### jjangash

- Mutation 비용 `300 → 600 → 1,200 → 2,400 → 4,800`과 현재 Mutation 제외.
- Injector 교체 시 Gold 미차감과 SEALED/미해금 Mythic 차단.
- 일반·신화 공명 비용, 대상 등급, 최대 5단계.
- Gold 중복 차감·중복 지급 없음.
- 재접속 후 Mutation·공명·보드 복구.

결과: **대기**

### kinggusi

- Mutation 8종의 실제 공격·피격·Animation·Effect.
- Host/Client HP·Gold·DoT·Slow·위치 동기화.
- Splash·Boss 피해·Projectile 중복 Hit 없음.
- Boss 한 마리와 공용 Lane, Boss timeout.
- 카메라·드래그·입력 충돌 없음.
- 1080p 외 화면비에서 Gold와 Battle UI 잘림 없음.

결과: **대기**

두 담당자의 실제 확인 결과가 모두 `PASS`일 때만 관련 Task를 `완료`로 변경한다.

## 8. 독립 리뷰

- 판정: `WARNING`
- 코드 차단 결함: 없음
- Breaking Change: 없음
- Compile Risk: Low
- 권고: 라이브 검증과 사람 PASS 전까지 `검증 대기` 유지

## 9. Git 가드

커밋 대상에서 다음을 제외한다.

- `Client/Packages/manifest.json`
- `Client/Packages/packages-lock.json`
- `Client/ProjectSettings/**`
- `Client/Assets/Photon/Fusion/Resources/PhotonAppSettings.asset`
- 삭제 상태의 Fusion sample `.meta` 2개
- 서버·정산·재화·Excel·Balance·Shared 계약

현재 브랜치와 upstream은 모두 `feature/battle-p1-effects-playtest`다. 사용자 승인 전 commit/push하지 않는다.

## 10. P1-4-6 Development 검증 세션 추가 검증 (2026-08-26)

### 구현 계약

- 형식: `P1VAL-{MAP}-W{NNN}-{12~32자리 hex nonce}`.
- 대상: Editor 또는 Development Build 전용.
- 9개 canonical 행성과 Wave 1~80만 허용하고 형식 오류는 Runner 생성 전에 거부한다.
- MapId는 새 Fusion Session 시작 전에 한 번만 고정한다.
- 시작 전 `CurrentRound=0`, Monster 0 상태를 유지한다.
- State Authority의 첫 수동 Start만 지정 Wave를 canonical 경로로 시작한다.
- 두 번째 Start와 자동 다음 Wave를 거부한다.
- P1VAL Session에서는 `BattleSettlementCoordinator`를 생성하지 않으며 기존 component도 비활성화한다.
- P1VAL Session은 synthetic `HighestClearedWave`를 만들 수 있으므로 Settlement 또는 재접속 검증에 재사용하지 않는다.

### 자동 검증

- Unity 컴파일 오류: 0.
- 전체 EditMode: 389/389 PASS, failed 0, skipped 0.
- Windows Development 빌드: PASS.
- Build Settings: `Assets/Scenes/Battle.unity` 단일 Scene.
- 최종 사용자용 출력: `Client/_localbuild/P1VAL_Final/Client.exe`.
- 최종 Clean Build: `Succeeded`, Error 0, Warning 14, 158,671,749 bytes, 29.340초.
- 최종 `Assembly-CSharp.dll` SHA-256: `9EBC227B7FF7389BC9AAA7B4F530EEDDCDB6D9F0095A15DAEDFBDE3D12760E37`.
- `Battle.unity` 저장 변경: 없음.

### 실제 Editor Host + Standalone Client

| Session | 결과 | 핵심 대조 |
|---|---|---|
| `P1VAL-EARTH-W009-A82620090002` | PASS | 정규 Monster 32, Player Lane별 16, HP 232.2, 속도 5.75, NetworkTransform 존재 |
| `P1VAL-SUN-W080-A82620800003` | PASS | `BOSS_SHARED` Boss 정확히 1, HP 88,110, 속도 2.5, NetworkTransform 존재 |

- Host=`jjangash`, Client=`kinggusi`, 명시적 Host/Client 역할로 연결했다.
- 두 Session 모두 시작 전 Round 0, Monster 0, Settlement coordinator 0을 확인했다.
- EARTH HP는 `30 × 1.8 × 4.3 = 232.2`, 속도는 `5 × 1.15 = 5.75`와 일치했다.
- SUN Boss HP는 `300 × 8.9 × 11 × 3 = 88,110`, 속도는 `2 × 1.25 = 2.5`와 일치했다.
- 양쪽 화면에서 동일 Monster/Boss와 NetworkTransform 위치 복제를 확인했다.
- 두 번째 Start 결과는 false, 다음 Wave 증가 0, Settlement POST 0이었다.
- 기준 로그 정리 후 Host/Client 신규 Error 및 Warning은 0이었다.

증거:

- `Client/_localbuild/P1ValidationTests/live-all-editmode-results.xml`
- `Client/_localbuild/P1ValidationTests/post-review-rerun-all-editmode-results.xml`
- `Client/_localbuild/P1ValidationTests/live-build-unity.log`
- `Client/_localbuild/P1ValidationTests/live-earth2-host-wave009.png`
- `Client/_localbuild/P1ValidationTests/live-earth2-client-wave009.png`
- `Client/_localbuild/P1ValidationTests/live-sun-host-wave080.png`
- `Client/_localbuild/P1ValidationTests/live-sun-client-wave080.png`

### 9행성 구조와 사람 플레이 게이트

| 행성 | HP 배율 | 속도 배율 | Boss HP 배율 | 구조 검증 |
|---|---:|---:|---:|---|
| NEPTUNE | 1.00 | 1.00 | 3.00 | PASS |
| URANUS | 1.35 | 1.03 | 3.00 | PASS |
| SATURN | 1.80 | 1.06 | 3.00 | PASS |
| JUPITER | 2.40 | 1.09 | 3.00 | PASS |
| MARS | 3.20 | 1.12 | 3.00 | PASS |
| EARTH | 4.30 | 1.15 | 3.00 | PASS + 실제 W009 |
| VENUS | 5.80 | 1.18 | 3.00 | PASS |
| MERCURY | 7.80 | 1.21 | 3.00 | PASS |
| SUN | 11.00 | 1.25 | 3.00 | PASS + 실제 W080 Boss |

canonical 행성 등록·배율·Wave/Boss 구조는 PASS다. 다만 권장 스펙, 클리어 가능성, 피로도와 체감 난이도는 자동화로 대체하지 않으며 9행성 사람 플레이 기록 전까지 P1-4-6 전체 상태는 `검증 대기`로 유지한다.

### 보류 사항

- P1-2-4~6 및 Mythic P1-3-6은 User/System Development Fixture가 현재 작업공간에 반영된 뒤 수행한다.
- P1-5-6 기존 사전 Summary는 POST하지 않는다. 최신 Session Roster 변경 후 새 2인 Session으로 다시 검증한다.
- 독립 리뷰가 PASS여도 사용자 승인 전 commit/push하지 않는다.

### 독립 리뷰 조치

- 최초 리뷰에서 Wave 완료 후 공개 `BattleWaveStateAuthority.TryStartNextWave()`가 내부 거절과 무관하게 true를 반환할 수 있는 one-shot 경계 결함을 발견했다.
- Development 전용 validation guard를 State Authority 공개 검증 경계에 추가해 consumed Session은 false를 반환하도록 수정했다.
- P1VAL 초기화가 context mismatch 또는 Wave arm 단계에서 실패하더라도 기존 Settlement coordinator가 먼저 비활성화되도록 fail-closed 순서를 보강했다.
- 공개 경계와 초기화 실패 회귀 테스트 2개를 추가했고 전체 EditMode 389/389 PASS를 확인했다.

## 11. 최신 dev Fixture·Roster 통합 결과 (2026-08-26)

### 수신 기준

- `origin/dev`: `82dbfc68f30f6fed97f635e457b58822300543d6`
- Mythic/Mutation Fixture: `6844d52`
- Session Roster/Settlement E2E: `825bcad`
- Fixture C#과 문서는 upstream 원본을 반영했다.
- Roster Client 계약과 registrar는 upstream 원본을 반영하고, 기존 P1VAL Session Adapter 변경과 충돌부만 병합했다.
- Server/경제/Balance 원천은 현재 Battle 작업공간에서 수정하지 않았다. 서버 검증은 `origin/dev` 임시 추출본으로 수행했다.

### 자동 검증

- Unity 컴파일 오류: 0.
- Roster/Adapter/Settlement 관련 EditMode: 19/19 PASS.
- Unity 전체 EditMode: 411/411 PASS, failed 0, skipped 0.
- Battle Scene validate: issue 0, Missing Script 0, Broken Prefab 0.
- Windows Development build: PASS, Error 0, Warning 1.
- 출력: `Client/_localbuild/P1Integrated_20260826/Client.exe`.
- 최신 빌드 크기: 158,714,341 bytes(약 151.36MB).
- 최신 dev Spring Roster/Settlement 관련 통합 테스트: 9 PASS, failure/error 0.
- Roster가 미등록이거나 registrar 자체가 없는 경우 Settlement가 네트워크 호출 전에 fail-closed되는 회귀 테스트를 포함한다.

### Fixture 실제 2인 세션

- Session: `P1VAL-EARTH-W001-A82621360003`.
- Editor Host=`jjangash`, Standalone Client=`kinggusi`.
- P1 Slot 0=`Mythic 29 + NONE`, Slot 1=`Mythic 29 + TOXIC`.
- P2 Slot 0=`Mythic 29 + SWIFT`.
- State Authority 대조: P1 NONE state 0, TOXIC state 3, P2 SWIFT state 3.
- Gold 100,000, earned 0, Kidnap 0 유지.
- 양쪽 Session 연결과 P2 보드 복제, 게임 Error/Exception 0, Settlement POST 0을 확인했다.
- 단, P2 SWIFT는 Standalone 패널의 client-origin RPC를 직접 누른 것이 아니라 State Authority Fixture 적용 경로로 생성 후 복제를 확인했다. Standalone 직접 요청 경로는 사람 검증 대기다.
- TOXIC 정확 3틱과 나머지 Mutation 실제 전투 효과는 사람 통합테스트 대기다.

증거:

- `Client/_localbuild/P1ValidationTests/fixture-resume-host-both-boards.png`
- `Client/_localbuild/P1ValidationTests/fixture-resume-host-none-toxic.png`
- `Client/_localbuild/P1ValidationTests/fixture-resume-host-editor.log`
- `Client/_localbuild/P1ValidationTests/fixture-resume-client.log`

### 최신 Roster 실제 네트워크 세션

- Session: `P1SET-A82621410001` (non-P1VAL 신규 Session).
- Standalone Host=`dev-host`, Standalone Client=`dev-client`.
- Spring: 최신 `origin/dev`, `local` profile, 격리된 8082 포트와 in-memory H2 `create-drop`.
- `[BattleRoster] registered trusted local roster`가 성공한 뒤에만 Round 1이 시작됐다.
- Roster 등록 요청은 1회 성공했고 P1VAL Session에는 Roster/Settlement 요청이 없었다.
- 이 세션은 장시간 대기에도 terminal 상태로 전환되지 않아 Settlement POST가 발생하지 않았다.
- 기존 `P1-Settlement-Fail-20260824-2155` Summary는 POST하지 않았다.
- 판정: Roster 실제 등록과 Wave gate는 PASS, 새 terminal Summary POST·응답·동일 payload retry는 검증 대기.

증거:

- `Client/_localbuild/P1ValidationTests/settlement-roster-host-player.log`
- `Client/_localbuild/P1ValidationTests/settlement-roster-client-player.log`
- `Client/_localbuild/P1ValidationTests/origin-dev-server-82dbfc6/server/build/test-results/test`

### 최신 종합 판정

- 후속 독립 리뷰: 차단 결함 0, `조건부 PASS`.
- 사용자 승인 전 staged/commit/push: 0.

| 항목 | 판정 | 남은 게이트 |
|---|---|---|
| P1-1-6 Mutation 외형/표시 | 조건부 PASS | 실제 Battle 카메라의 8종 사람 시각 판정·다수 Unit 성능 |
| Fixture 규칙/빌드/네트워크 생성 | 조건부 PASS | Standalone 직접 client-origin RPC |
| P1-2-4 TOXIC | 검증 대기 | 단일 공격 격리 후 3틱·총 피해·양쪽 HP |
| P1-2-5 SWIFT/FROZEN/GREEDY | 검증 대기 | 실제 공격 간격·Slow 복구·Gold 1회 지급 |
| P1-2-6 GIANT/BERSERK/OBESE | 검증 대기 | Splash·Boss 배율·약/강 피해 단일 적용 |
| P1-3-6 Normal Snapshot | PASS | 없음 |
| P1-3-6 Mythic Snapshot | 검증 대기 | NONE 생성 후 강화 전후 실제 공격 |
| Projectile/Monster/Boss/Waypoint/NetworkTransform | 현재 PASS | 장시간·재접속 사람 회귀 |
| P1-4-6 구조/안전 Session | PASS | 9행성 체감 난이도 사람 플레이 |
| P1-5-6 Roster gate | PASS | 없음 |
| P1-5-6 Settlement | 검증 대기 | 새 terminal POST·응답·retry |

사용자 통합테스트 절차는 `docs/test-reports/P1-BATTLE-INTEGRATION-CHECKLIST.md`를 따른다.

## 12. 사용자 TOXIC 실플레이 발견 결함 및 R3 조치 (2026-08-26)

### 재현 결과

- Session: `P1MUT-TOXIC-20260826-01`.
- Fixture 양쪽 복제, 최초 공격, 지속 피해 관찰: PASS.
- Host/Client HP 수치 일치: 사람 판정 어려움.
- Player 2 Client에서 상대 Host Unit은 원격 필드에 표시되지만 Projectile은 Host canonical 필드 좌표의 허공에서 발사되는 표시 결함을 재현했다.
- Host/Client Monster 표시 크기 차이를 재현했다.
- Client 버벅임이 있었고, Client 로그에서 Monster별 lane remap 메시지 256회를 확인했다.

### 원인 및 조치

- Player 2 관점은 보드와 Monster lane을 presentation-only로 교체하지만 Projectile proxy는 canonical NetworkTransform 좌표를 그대로 표시했다.
  - State Authority의 판정 좌표·피해·Target은 유지하고 Player 2 proxy 표시 궤적만 실제 화면상 Unit/Monster 위치를 따르도록 분리했다.
- Monster scale이 `Runner.Spawn` 이후 Host에만 설정되고 prefab NetworkTransform의 Scale 동기화가 꺼져 있었다.
  - Authority가 `[Networked] PresentationScale`을 초기화하고 proxy `Render`에서 적용하도록 보강했다.
- Monster별 remap 로그를 동일 lane mapping당 1회로 제한했다.
- 독립 리뷰에서 발견한 매 프레임 문자열/HashSet 할당과 Projectile별 `FindObjectsByType` 할당을 제거했다.
  - Monster는 instance 1회 gate와 정수 mapping key를 사용한다.
  - Projectile은 활성 `FusionKidnapBoardView`의 serverId→slot 직접 조회를 사용한다.

### 자동 검증

- Unity 컴파일 오류: 0.
- 전체 EditMode: 418/418 PASS, failed 0, skipped 0.
- Windows Development Build: PASS.
- 출력: `Client/_localbuild/P1Integrated_20260826_R3/Client.exe`.
- Build Settings: `Assets/Scenes/Battle.unity` 단일 Scene.
- R3 크기: 158,711,785 bytes, Warning 14.

Projectile 원점·Monster 크기·Client 버벅임의 최종 판정은 R3 동일 PC 2-client 사람 재검증 전까지 `검증 대기`다.

## 13. R3 실플레이 후 Lane Target·Monster 정지 조치 (2026-08-26)

### 사용자 R3 재검증 결과

- Session: `P1MUT-TOXIC-R3-20260826-01`.
- 양 방향 Projectile 원점, 명중·소멸, Monster 크기, TOXIC 지속 피해, 이동·Merge 직후 Projectile: PASS.
- Host 버벅임 있음, Client 버벅임 없음.
- 추가 관찰: Client Monster가 간헐적으로 멈추며, Host와 Client 화면에서 Projectile이 서로 다른 전용 Lane을 공격하는 것처럼 보였다.

### 원인 및 조치

- R4에서는 거리 기반 탐색이 상대 `EACH_FIELD`를 선택하는 것을 결함으로 잘못 판정해 자기 소유 Lane으로 제한했다.
  - 실제 협동 규칙은 Host/Client Unit 모두 사거리 안의 양쪽 `EACH_FIELD`를 공격하는 것이므로 R4 제한은 오판으로 판정됐다.
  - R5에서 해당 제한을 제거해 기존 협동 공격을 복구했다.
  - `BOSS_SHARED` 양 플레이어 공격은 계속 유지한다.
- Player 2 proxy가 이미 remap한 root `transform.position`과 `NetworkTransform`의 canonical 좌표 적용이 같은 Transform에서 충돌할 수 있었다.
  - proxy 표시 보정은 root의 현재 위치를 재입력으로 쓰지 않고 `NetworkTransform`의 from/to snapshot buffer를 직접 보간하도록 변경했다.
  - State Authority 이동·Hit·Damage 경로는 변경하지 않았다.

### 자동 검증 및 빌드

- Unity 컴파일 오류: 0.
- 전체 EditMode: 420/420 PASS, failed 0, skipped 0.
- Windows Development Build: PASS.
- 출력: `Client/_localbuild/P1Integrated_20260826_R4/Client.exe`.
- Build Settings: `Assets/Scenes/Battle.unity` 단일 Scene.
- R4 크기: 158,713,298 bytes.
- 독립 리뷰: blocker 0, 조건부 PASS.

### 남은 사람 검증 및 절차 주의

- R4는 상대 Lane 공격 차단 및 닫힘 구간 모서리 정지로 FAIL이다. R5에서 양쪽 EACH_FIELD 공격, BOSS_SHARED 양쪽 공격, Monster 장시간 이동, HP·Projectile 대상 일치와 다수 Monster frame spike를 재검증한다.
- R4에서 추가했던 `UnitAttack.cs` owner 제한은 제거했으므로 이 오판으로 인한 허용 경로 밖 신규 변경은 남지 않는다.
- snapshot 불변 프레임의 path projection 비용은 다수 Monster 실플레이 프로파일링 대상으로 유지한다.
- 사용자 승인 전 staged/commit/push하지 않는다.

## 14. R4 사용자 재검증 및 R5 폐곡선 조치 (2026-08-27)

### R4 사용자 결과

- Session: 사용자 R4 TOXIC 세션.
- 자기 EACH_FIELD 공격, Projectile 대상·HP, Client 3분 이동, Boss 양쪽 공격, Host/Client 버벅임: PASS.
- 상대 Lane 오공격 없음은 실제로는 협동 공격이 차단된 결과이므로 FAIL로 재판정했다.
- Client 화면에서 Host Field Monster가 모서리에서 멈추는 현상을 재현했다.

### 확정 원인 및 수정

- 정규 Monster 이동은 마지막 Waypoint 다음에 첫 Waypoint로 순환하는 폐곡선이다.
- R4 presentation remap의 경로 길이·투영·평가 계산은 `Count - 1`개 구간만 처리해 마지막→첫 Waypoint 구간을 누락했다.
- 누락 구간에서 proxy 진행률이 마지막 모서리에 고정되어 Monster가 멈춘 것처럼 표시됐다.
- R5는 EACH_FIELD remap에 한해 마지막→첫 구간을 포함하는 closed-loop 계산을 사용한다. Boss shared open path와 authority 이동은 변경하지 않았다.
- R4의 자기 owner Lane 제한을 제거해 Host/Client Unit의 양쪽 필드 협동 공격을 복구했다.
- R3의 Player 2 Projectile 표시 원점·목표 remap 및 authoritative Target/Hit/Damage는 유지했다.

### 자동 검증 및 빌드

- Unity 컴파일 오류: 0.
- 전체 EditMode: 420/420 PASS, failed 0, skipped 0.
- 닫힘 구간 중간점 progress `0.875`와 역평가 위치 일치 회귀 테스트 PASS.
- Windows Development Build: PASS.
- 출력: `Client/_localbuild/P1Integrated_20260827_R5/Client.exe`.
- Build Settings: `Assets/Scenes/Battle.unity` 단일 Scene.
- R5 크기: 158,713,099 bytes.

### 남은 사람 게이트

- 양쪽 Unit이 자기/상대 EACH_FIELD를 모두 공격하는 네 방향 조합.
- Player 2 화면에서 양 Lane Monster가 마지막→첫 Waypoint를 포함해 최소 3바퀴 동안 정지·점프 없이 이동.
- 상대 Lane Projectile의 표시 원점·도착점·Target HP 양쪽 일치.
- Boss 양쪽 공격, Unit 이동·Merge 직후 Projectile, Host/Client frame spike 회귀.

### R5 사용자 TOXIC 재검증 결과

- Session: `P1MUT-TOXIC-R5-20260827-01`.
- Host→Host, Host→Client, Client→Client, Client→Host 필드 협동 공격: 모두 PASS.
- Projectile·HP 양쪽 일치, Host/Client Monster 3바퀴, 모서리 정지·점프 없음: PASS.
- Boss 양쪽 공격, Host/Client 버벅임 없음: PASS.
- R5 Projectile/Monster/Lane closed-loop 회귀는 사람 검증 PASS로 전환한다.
- TOXIC은 최초 피해 110 이후 같은 피격 Monster에 1초 간격 22 피해를 3회 적용한다. 피격 후 Monster가 멀리 이동해도 남은 DoT로 사망할 수 있으며 TOXIC Splash는 0이다.

## 15. P1VAL Standalone Wave 시작 UI 보완 (2026-08-27)

- 사용자 FROZEN 재검증 Session `P1VAL-NEPTUNE-W010-a827010001f0`은 정상 arm됐지만, 안전 정책에 따라 자동 Wave가 정지된 상태였다.
- R5 Standalone에는 공개 `TryStartNextWave()`를 호출할 UI가 없어 사용자가 target Wave를 시작할 수 없는 validation fixture 결함을 확인했다.
- Development Fixture 패널에 `Start P1 Wave NNN (Host only)` 버튼을 추가했다.
- 버튼은 Spawned + State Authority + P1VAL armed + 미소비 조건에서만 활성화되며 한 번 시작한 뒤 비활성화된다.
- 운영 빌드에서는 기존 `UNITY_EDITOR || DEVELOPMENT_BUILD` 경계로 완전히 제외된다.
- 전체 EditMode 425/425 PASS, failed 0, skipped 0.
- Windows Development Build: `Client/_localbuild/P1Integrated_20260827_R6/Client.exe`, Battle Scene 단독, 158,713,758 bytes.

## 16. Projectile 1차 피격 대상 잠금 보완 (2026-08-27)

### 사용자 통합테스트와 현상 판정

- 사용자가 TOXIC, FROZEN 및 나머지 지정 Mutation 실플레이를 수행했고, FROZEN 감속을 포함한 기존 체크 항목은 육안상 추가 결함 없이 완료했다.
- “Monster 1을 공격했는데 Monster 2가 피해를 받는 것처럼 보이는” 공통 현상을 별도 검토했다.
- 정상적으로 그렇게 보일 수 있는 경우는 다음과 같다.
  - `GIANT`: primary 대상 중심 반경 2.5의 다른 Monster에 87.75 splash 피해를 적용한다.
  - `TOXIC`: 먼저 맞은 Monster가 이후 1초 간격 DoT로, Unit이 다음 Monster를 공격하는 동안 사망할 수 있다.
  - 초기 Wave의 낮은 HP와 사망 fade 때문에 연속 공격의 귀속을 육안으로 구분하기 어려울 수 있다.
- 이와 별개로 물리 `OnTriggerEnter`/`OnCollisionEnter` 경로가 Projectile의 `TargetNetworkId`가 아닌, 우연히 겹친 다른 Monster collider를 primary 대상으로 처리할 수 있는 잠재 결함을 확인했다.

### 수정

- 모든 authoritative primary Hit가 중앙 경계에서 `TargetNetworkId`와 실제 충돌 `NetworkId` 일치를 확인하도록 잠갔다.
- `HOMING`/`INSTANT` Projectile은 유효한 `TargetNetworkId`가 없으면 spawn validation에서 거부한다.
- `GIANT` splash는 primary Hit 이후 별도 `ApplySplashDamage` 경로이므로 기존 반경 피해를 유지한다.
- `TOXIC` DoT는 최초 primary 대상의 `RuntimeMonsterId`에만 유지된다.

### 검증

- 타깃 잠금 회귀 테스트와 전체 Unity EditMode: 426/426 PASS, failed 0, skipped 0.
- 결과: `Client/_localbuild/P1ValidationTests/post-target-contract-all-editmode-results.xml`.
- Unity 컴파일 오류: 0.
- Windows Development Build: PASS.
- 출력: `Client/_localbuild/P1Integrated_20260827_R7/Client.exe`.
- Build Settings: `Assets/Scenes/Battle.unity` 단일 Scene.
- R7 전체 파일 크기: 158,713,827 bytes.

최종 사람 확인에서는 `NONE/FROZEN`은 지정 대상 외 HP 불변, `GIANT`은 지정 대상 primary와 반경 내 splash만 발생, `TOXIC`은 먼저 맞은 동일 대상에만 지연 피해가 이어지는지를 구분해 기록한다.
