# Unity 기능 검증 및 Feature Test Hub 전략

> 기준 Task: `docs/98_IMPLEMENTATION_TASKS.md`
> 적용 대상: Unity 코드, Scene, Prefab, UI, 입력, 연출, Photon Fusion 상호작용

## 1. 목적

Task별 기능을 격리된 환경에서 반복 검증하고, 중앙 Hub에서 테스트 위치와 상태를 찾을 수 있게 한다. 자동 테스트, AI 독립 리뷰, jjangash·kinggusi 사람 검증을 서로 대체하지 않고 순서대로 수행한다.

## 2. 현재 확인된 기반

1차 프로젝트 Scene:

- `Client/Assets/Pages/Main.unity`
- `Client/Assets/Scenes/Battle.unity`
- `Client/Assets/Scenes/SampleScene.unity`
- `Client/Assets/Scenes/Tests/TestGameScene.unity`

`Client/Assets/Editor/Tests`에는 Battle Balance, Path, Runtime Identity, Summary, Wave Executor, HUD 관련 EditMode 테스트가 존재한다.

에셋 패키지의 Demo Scene은 프로젝트 기능 테스트로 간주하지 않고 Feature Test Hub 등록 대상에서 제외한다.

## 3. 핵심 구조

모든 기능을 하나의 거대한 테스트 Scene에 배치하지 않는다. 중앙 Hub는 Catalog를 보여주고 선택한 Task의 격리 Scene을 연다.

```text
FeatureTestHub
├─ Task ID / 담당 / 상태 필터
├─ 테스트 Scene 열기
├─ Play / Reset 안내
├─ 자동 테스트 위치
├─ 사람 검증 체크리스트
└─ 검증 보고서 위치
```

권장 경로:

```text
Client/Assets/Scenes/Tests/
├─ FeatureTestHub.unity
├─ UserSystem/
├─ Battle/
└─ Shared/

Client/Assets/Editor/FeatureTesting/
├─ Catalog 및 검증 코드
└─ Hub 생성·갱신 Editor Tool

docs/test-reports/
└─ <Task-ID>.md
```

실제 Scene, Prefab, ScriptableObject와 `.meta`는 Unity MCP 또는 Unity Editor API가 생성한다. Markdown 작업에서 빈 Asset이나 GUID를 미리 만들지 않는다.

## 4. Task별 테스트 자산 규칙

Unity 동작을 변경하는 Task는 다음 중 하나 이상을 가진다.

- 순수 로직: EditMode 테스트
- 프레임·코루틴·입력·Scene 생명주기: PlayMode 테스트
- UI·시각·드래그·배치·연출: 격리된 Feature Test Scene
- Fusion 권한·복제·재접속: 2클라이언트 테스트 절차

Feature Test Case 최소 메타데이터:

| 필드 | 의미 |
|---|---|
| Task ID | `docs/98_IMPLEMENTATION_TASKS.md`의 ID |
| Title | 테스트 기능 이름 |
| Owner | `jjangash` 또는 `kinggusi` |
| Scene Path | 격리 테스트 Scene 경로 |
| Test Type | EditMode, PlayMode, Scene, Fusion 2-Client |
| Preconditions | 서버, 데이터, 계정, 해금 상태 |
| Reset | 반복 실행 전 상태 초기화 방법 |
| Automated Tests | 관련 테스트 이름 또는 경로 |
| Human Checklist | 실제 화면 검증 항목 |

Scene 이름은 Task ID와 기능을 식별할 수 있게 한다.

```text
P0_05_02_SharedKillGold_Test.unity
P0_07_07_LegendaryChoiceUI_Test.unity
```

## 5. Feature Test Hub 원칙

- Hub는 테스트 목록과 진입점이며 모든 기능 오브젝트를 동시에 보유하지 않는다.
- 기본 동작은 선택한 격리 Scene을 단독으로 여는 것이다.
- Additive Load가 꼭 필요한 경우 Catalog에 이유와 의존 Scene을 기록한다.
- 테스트 데이터는 실행 전 Reset할 수 있어야 한다.
- 동일 Task ID, 존재하지 않는 Scene 경로, 비어 있는 체크리스트는 자동 검증에서 실패한다.
- Test Scene은 Production Build Settings에 포함하지 않는다.
- 패키지 Demo Scene과 프로젝트 Feature Test Scene을 섞지 않는다.

## 6. Unity MCP 사전 점검

Scene 또는 Prefab 변경 전에 구현 Subagent가 다음을 확인한다.

1. Unity MCP 도구가 현재 Thread에서 실제 호출 가능한가
2. 연결된 프로젝트가 저장소의 `Client`인가
3. Unity Editor가 Compile 또는 Play 중이 아닌가
4. 열려 있는 Scene에 저장되지 않은 사용자 변경이 있는가
5. 대상 Scene, Prefab, Script의 실제 경로가 존재하는가

Unity MCP가 호출되지 않으면 Scene/Prefab 구현을 시작하지 않는다. 대안으로 Editor Tool을 작성할 수 있지만, `EditorSceneManager`, `AssetDatabase`, `PrefabUtility`를 사용하고 사람이 Unity 메뉴에서 실행한 결과를 확인해야 한다.

`.unity`, `.prefab`, `.meta`, GUID 직접 작성은 항상 금지한다.

## 7. AI 검증 흐름

```text
구현 Subagent
→ Unity Compile 및 자동 테스트
→ Test Scene에서 기능 확인
→ Missing Script/Reference 검사
→ 독립 읽기 전용 리뷰 Subagent
→ PM 실제 Diff 및 증거 확인
```

독립 리뷰 Subagent는 Scene이나 코드를 고치지 않는다. `PASS`, `WARNING`, `FAIL`과 수정 요구만 반환하며, 수정은 원래 구현 Subagent가 수행한다.

## 8. 사람 검증

UI, Scene, 입력, 연출, 해상도 대응 또는 2인 상호작용 Task는 jjangash와 kinggusi가 각각 검증한다.

jjangash 기본 확인:

- 정책과 버튼 동작
- 비용·재화·상태 표시
- 서버 저장 및 복구
- User/System 흐름과 예외 처리

kinggusi 기본 확인:

- Battle Scene 연결
- 위치·가독성·Animation·Effect
- 두 클라이언트 동기화
- Missing Reference와 카메라·입력 충돌

공통 확인:

- 프로젝트 기준 해상도와 서로 다른 화면비에서 겹침·잘림 없음
- 첫 진입, 반복 진입, Reset 후 재실행
- 정상 입력과 잘못된 입력
- 호스트와 클라이언트 화면 일치
- 콘솔 Error 없음

결과 형식:

```text
<Task-ID> jjangash 테스트: PASS | FAIL
- 실행 Scene:
- 확인 환경:
- 문제:

<Task-ID> kinggusi 테스트: PASS | FAIL
- 실행 Scene:
- 확인 환경:
- 문제:
```

두 명 모두 `PASS`해야 Task를 `완료`로 변경한다. 한 명이라도 `FAIL`이면 Task는 `검증 대기`를 유지하고 구현 수정과 독립 리뷰를 반복한다.

## 9. 2클라이언트 Fusion 검증

2인 기능은 한 Editor 화면만으로 완료 판정하지 않는다.

최소 확인:

1. State Authority와 Client 각 1개 실행
2. 동일 Session 참가
3. 호스트와 클라이언트의 Networked 상태 일치
4. RPC 중복·권한 거부·재전송 확인
5. 한 클라이언트 연결 종료와 재접속
6. 종료 후 Settlement 입력값 일치

구체적인 두 번째 클라이언트 실행 방식과 테스트 계정·데이터 Reset은 `P0-11-6`에서 현재 Unity 환경에 맞게 확정한다.

## 10. 검증 보고서

Unity 사람 검증이 필요한 Task는 `docs/test-reports/<Task-ID>.md`에 다음을 기록한다.

- 대상 커밋 또는 Diff 기준
- 관련 자동 테스트 결과
- Test Scene과 실행 절차
- 독립 리뷰 판정
- jjangash 결과
- kinggusi 결과
- 발견 문제와 재검증 이력
- 최종 PM 판정

보고서는 Task 구현과 함께 갱신하며, 검증 증거 없이 상태만 `완료`로 바꾸지 않는다.
