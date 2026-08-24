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

현재 브랜치의 기존 upstream이 다른 User 브랜치를 가리키므로 Push 시 `feature/battle-p1-effects-playtest`를 명시한다.
