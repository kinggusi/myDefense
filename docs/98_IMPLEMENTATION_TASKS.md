# 왹져 디펜스 구현 작업 분해표

> 기준 문서: `docs/99_MASTER.md`
> 기준일: 2026-07-18
> 목적: 두 명의 담당자가 Codex에 한 작업씩 전달할 수 있도록 남은 구현을 겹치지 않는 단위로 분리한다.

## 0. 사용 방법

### PM 자동 오케스트레이션

이 문서는 User/System Codex와 Battle Codex가 공통으로 사용하는 작업 관리 기준이다. 사용자가 `작업하자`라고 말하면 현재 Codex는 아래 절차를 자동으로 수행한다.

#### 1. 담당 역할 판별

다음 순서로 현재 작업의 담당 역할을 판별한다.

1. 사용자가 Task 또는 담당을 명시했으면 그 지시를 따른다.
2. 현재 브랜치가 `feature/user-*`이면 `jjangash(User/System)` 담당으로 판별한다.
3. 현재 브랜치가 `feature/battle-*`이면 `kinggusi(Battle)` 담당으로 판별한다.
4. 관련 역할 문서가 명시되어 있으면 `docs/ai/User.md` 또는 `docs/ai/Battle.md`를 따른다.
5. 그래도 모호할 때만 사용자에게 담당 역할을 한 번 확인한다.

#### 2. 다음 Task 선택

현재 담당자의 Task 중 아래 기준을 모두 만족하는 가장 앞 Task를 선택한다.

1. 우선순위는 `P0 → P1 → P2` 순서다.
2. 같은 우선순위에서는 Task ID가 빠른 항목을 먼저 선택한다.
3. 상태가 `부분 완료`인 Task를 먼저 마감하고 그다음 `대기` Task를 선택한다.
4. 모든 선행 Task가 완료됐거나 현재 Task 수행에 필요한 수준까지 준비되어 있어야 한다.
5. `정책 선행` Task는 정책이 확정되기 전에는 구현 Task로 선택하지 않는다.
6. 상위 Task가 차단되면 차단 원인을 기록하고 다음으로 수행 가능한 Task를 제안한다.

Task를 선택한 뒤 사용자에게 다음 네 가지만 짧게 보고한다.

- 선택 Task와 담당
- 지금 선택한 이유
- 완료 목표
- 영향을 받는 도메인

#### 3. 계획 보고서 단계

PM Codex가 별도 구현 Thread 또는 Subagent를 생성하고 먼저 계획 보고서만 요청한다. 사용자는 구현 Subagent를 직접 만들거나 보고서를 옮기지 않는다. 이 단계에서는 파일을 수정하지 않는다.

계획 보고서 필수 항목:

1. 현재 코드 구현 상태와 사용처
2. 정책·문서·코드의 불일치
3. 변경 예정 파일과 변경 이유
4. 도메인 및 Shared 계약 영향
5. 구현 순서
6. 테스트 계획
7. 호환성, 회귀, 네트워크 위험
8. 구현 전에 확정할 질문

#### 4. PM 계획 검토 단계

PM Codex가 구현 Thread의 보고서를 직접 읽고 다음을 확인한다.

- Task 범위를 벗어난 수정이 없는가
- User/System과 Battle 소유권이 충돌하지 않는가
- Shared 계약 변경 주체가 올바른가
- `docs/99_MASTER.md` 정책과 일치하는가
- 기존 구현을 중복하거나 우회하지 않는가
- 테스트가 완료 기준을 충분히 검증하는가

문제가 있으면 PM Codex가 구현 Thread에 수정된 계획을 직접 전달한다. 사용자는 보고서를 복사해 전달하지 않는다.

#### 5. 구현·검증 단계

PM 검토를 통과한 뒤에만 같은 구현 Thread에 구현을 지시한다.

구현 Thread는 다음 결과 보고서를 제출한다.

1. 실제 변경 파일
2. 핵심 구현 내용
3. 컴파일 결과
4. 자동 테스트와 수동 확인 결과
5. 미해결 경고와 위험
6. 후속 Task 또는 Shared 요청

Unity Scene, Prefab, UI 또는 상호작용을 변경하는 Task는 추가로 다음을 보고한다.

1. Unity MCP 호출 가능 여부와 연결된 프로젝트
2. 사용한 Task 전용 테스트 Scene 또는 자동 테스트
3. Feature Test Hub 등록 여부
4. Missing Script/Reference 확인 결과
5. 테스트 Scene의 Production Build 제외 여부

#### 6. 독립 리뷰 단계

구현 Thread는 자기 구현을 최종 승인할 수 없다. PM Codex는 구현 완료 후 별도의 읽기 전용 리뷰 Thread 또는 Subagent를 생성한다.

독립 리뷰 필수 항목:

1. 실제 Diff와 Task 범위 일치 여부
2. 도메인 소유권과 Shared 경계
3. 정책 및 네트워크 권한 위반
4. 테스트 누락과 회귀 위험
5. Unity Scene/Prefab 안전, Missing Reference, Build 포함 여부
6. `PASS`, `WARNING`, `FAIL` 판정과 필수 수정 사항

독립 리뷰 Subagent는 파일을 수정하지 않는다. 수정은 원래 구현 Subagent만 수행한다.

#### 7. PM 결과 검토와 수정 반복

PM Codex는 구현 및 독립 리뷰 보고서만 믿지 않고 실제 Diff와 관련 코드를 확인한다. 문제가 있으면 같은 구현 Thread에 수정 지시를 보내고 아래 순환을 반복한다.

```text
계획 보고서 → PM 검토 → 구현 → 자체 테스트 → 독립 리뷰 → PM 코드 검토
     ↑                                                    ↓
     └──────────────── 수정 필요 시 재지시 ─────────────────┘
```

#### 8. Unity 사람 검증 단계

Unity UI, Scene, 입력, 연출 또는 2인 네트워크 상호작용 Task는 AI 검토 통과 후에도 바로 완료하지 않는다.

1. PM Codex가 `docs/04_TEST_STRATEGY.md` 형식으로 검증 체크리스트를 만든다.
2. Task 상태를 `검증 대기`로 변경한다.
3. jjangash와 kinggusi가 Feature Test Hub 또는 지정 Scene에서 각각 테스트한다.
4. 두 사람의 `PASS`가 기록돼야 `완료`로 변경한다.
5. 한 명이라도 `FAIL`이면 원래 구현 Subagent에 수정 지시하고 독립 리뷰부터 반복한다.

커밋과 Push는 `AGENTS.md`에 따라 사용자의 명시적 승인 후 수행한다. 다른 담당자의 사람 검증에 원격 브랜치가 필요하면 검증용 Push 승인을 먼저 받는다.

#### 9. Thread 도구가 없는 환경

별도 Thread를 만들거나 읽을 수 없는 Codex 환경에서는 동일한 절차를 유지하되, 다음 단계에 전달할 완성된 Prompt를 출력한다. 이 경우에만 사용자가 Prompt를 복사한다.

#### 10. 사용자 단축 명령

| 사용자 명령 | Codex 동작 |
|---|---|
| `작업하자` | 현재 역할의 다음 수행 가능 Task를 선택하고 자동 오케스트레이션 시작 |
| `jjangash 작업하자` | 다음 User/System Task 선택 |
| `kinggusi 작업하자` | 다음 Battle Task 선택 |
| `현황 보여줘` | 역할별 완료·진행·대기·차단 Task 요약 |
| `Task ID 작업하자` | 지정 Task의 선행조건을 검사한 뒤 진행 |
| `커밋푸시해` | 검토 통과한 변경만 커밋하고 현재 기능 브랜치에 Push |

### 담당 표기

- **jjangash(User/System)**: 게임 규칙, 경제, Alien, Kidnap, Merge, Mutation, StatCalculator, Spring Boot, Fusion 시스템 상태
- **kinggusi(Battle)**: Monster, Boss, Wave, Target, Projectile, Hit, Battle Scene, NetworkTransform
- Shared 파일의 실제 수정자는 **jjangash(User/System)**로 고정하고 kinggusi는 검토 의견만 전달한다.

### 상태 표기

- `완료`: 현재 코드에 구현과 검증 기반이 존재한다.
- `검증 대기`: AI 구현·독립 리뷰는 통과했으나 필수 사람 검증이 남았다.
- `부분 완료`: 타입이나 로컬 기반은 있으나 Fusion 통합 또는 최종 정책 반영이 남았다.
- `대기`: 본 구현이 필요하다.
- `정책 선행`: 구현 전 정책 확정이 필요하다.

### 브랜치 원칙

- jjangash: `feature/user-*`
- kinggusi: `feature/battle-*`
- 한 브랜치에는 이 문서의 Task 한 개 또는 강하게 결합된 연속 Task만 포함한다.
- Shared 변경이 필요하면 kinggusi가 직접 수정하지 않고 필요한 필드·이유·사용처를 보고한다.
- 각 작업은 최신 `dev`를 기준으로 시작한다.
- 커밋과 Push는 사용자 승인 후에만 수행한다.

---

## 1. P0 — 2인 멀티 전투 성립

P0가 완료되어야 실제 2클라이언트 협동 매치를 처음부터 Settlement까지 실행할 수 있다.

### P0-1 Shared 전투 계약

| Task ID | 담당 | 상태 | Codex 작업 | 완료 기준 |
|---|---|---|---|---|
| P0-1-1 | jjangash | 완료 | 기존 `PlayerBattleState`, `MatchState`를 정책과 대조하고 `PlayerConnectionState` 추가 | 전투 상태와 연결 상태가 분리된 Shared Enum 및 테스트 |
| P0-1-2 | jjangash | 완료 | `BattleSessionSnapshot` DTO 설계 | Player, Wave, Gold, 보드 복구 필드 포함 |
| P0-1-3 | jjangash | 완료 | `LegendaryChoiceState` DTO 설계 | 재료 ID, 후보 3종, 리롤, 선택 상태 포함 |
| P0-1-4 | jjangash | 완료 | 서버 Settlement DTO와 Unity `BattleSummary`를 대조해 공통 `BattleSettlementSummary` 계약 작성 | Unity와 Spring 필드가 1:1 대응 |
| P0-1-5 | kinggusi | 완료 | P0-1-1~4 계약의 Battle 사용처를 읽기 전용으로 검토 | 누락 필드, 불필요 필드, 사용처 보고서 제출 |
| P0-1-6 | jjangash | 완료 | kinggusi 검토 결과를 Shared 계약에 반영 | 양쪽 컴파일 및 계약 테스트 통과 |

> P0-1-1은 Battle API 정합화(`MonsterStat.InitializeBattleContext`, Lane별 `RegisterMonsterKilled`)와 `SharedBattleStateContractTests`를 포함한 Unity EditMode 전체 회귀 테스트 242/242 통과를 확인해 `완료`로 승격했다.

> P0-1-4는 Unity/Spring 필드명·타입 계약, nullable `eliminatedWave` 전용 JSON 직렬화, Settlement 결과값 검증, Spring JSON 역직렬화 테스트를 포함한다. Unity EditMode 252/252와 Spring 265/265 통과를 확인해 `완료`로 승격했다.

진행 순서:

```text
P0-1-1~4 → P0-1-5 → P0-1-6
```

### P0-2 Fusion 2인 Battle Session

| Task ID | 담당 | 상태 | Codex 작업 | 선행 |
|---|---|---|---|---|
| P0-2-1 | jjangash | 완료 | Fusion Runner 생성·종료 관리 구현 | P0-1-6 |
| P0-2-2 | jjangash | 완료 | Player 1/2 참가, PlayerRef와 User ID 연결 | P0-2-1 |
| P0-2-3 | jjangash | 완료 | 기존 `BattleSessionContext`를 Fusion Battle Session ID 및 MatchState와 연결 | P0-2-2 |
| P0-2-4 | jjangash | 완료 | 2인 준비 완료 후 전투 시작 처리 | P0-2-3 |
| P0-2-5 | kinggusi | 부분 완료 | Battle Scene이 Session 정보를 받는 Adapter 완성 | P0-2-3 |
| P0-2-6 | kinggusi | 부분 완료 | 두 개인 필드와 공용 Lane을 Session 기준으로 초기화 | P0-2-5 |

### P0-3 Networked Wave·Monster·Boss

| Task ID | 담당 | 상태 | Codex 작업 | 선행 |
|---|---|---|---|---|
| P0-3-1 | kinggusi | 완료 | 현재 `BattleWaveExecutor`를 Fusion State Authority 구조로 분리 | P0-1-6 |
| P0-3-2 | kinggusi | 완료 | 현재 Wave와 Wave 진행 상태를 `[Networked]`로 구현 | P0-3-1 |
| P0-3-3 | kinggusi | 완료 | Monster를 `Runner.Spawn`으로 생성 | P0-3-2 |
| P0-3-4 | kinggusi | 완료 | 기존 Runtime Identity·HP·사망 기반을 Fusion Network 상태와 연결 | P0-3-3 |
| P0-3-5 | kinggusi | 대기 | Boss NetworkObject Spawn 구현 | P0-3-3 |
| P0-3-6 | kinggusi | 대기 | Monster·Boss NetworkTransform 적용 | P0-3-3 |
| P0-3-7 | jjangash | 대기 | Wave 시작·종료 시 MatchState 검증 API 제공 | P0-2-3 |

### P0-4 탈락·관전·매치 상태

| Task ID | 담당 | 상태 | Codex 작업 | 선행 |
|---|---|---|---|---|
| P0-4-1 | kinggusi | 부분 완료 | 개인 필드별 살아 있는 Monster 수를 authoritative하게 집계 | P0-3-4 |
| P0-4-2 | kinggusi | 부분 완료 | 80/90 경고 이벤트와 100마리 탈락 이벤트 발생 | P0-4-1 |
| P0-4-3 | jjangash | 대기 | 탈락 이벤트를 받아 Networked PlayerBattleState 변경 | P0-4-2 |
| P0-4-4 | jjangash | 대기 | 탈락 플레이어의 Kidnap·Merge·Mutation·강화 차단 | P0-4-3 |
| P0-4-5 | kinggusi | 대기 | 탈락 필드 신규 Monster Spawn 중단 | P0-4-3 |
| P0-4-6 | kinggusi | 대기 | 탈락 플레이어 관전 카메라·UI 전환 | P0-4-3 |
| P0-4-7 | jjangash | 대기 | 두 플레이어 탈락 시 MatchState를 `FAILED`로 변경 | P0-4-3 |
| P0-4-8 | jjangash | 대기 | 80 Wave 완료 시 MatchState를 `CLEARED`로 변경 | P0-3-7 |

### P0-5 State Authority Kidnap·Merge·Gold

| Task ID | 담당 | 상태 | Codex 작업 | 선행 |
|---|---|---|---|---|
| P0-5-1 | jjangash | 대기 | 플레이어별 Networked inGameGold 구현 | P0-2-3 |
| P0-5-2 | jjangash | 대기 | Monster 처치 시 양쪽 플레이어에게 각각 100% 골드 지급 | P0-5-1 |
| P0-5-3 | jjangash | 대기 | State Authority Kidnap 요청·검증 RPC 구현 | P0-5-1 |
| P0-5-4 | jjangash | 부분 완료 | 기존 24칸·첫 빈칸·누적 비용 규칙을 Fusion 검증으로 이전 | P0-5-3 |
| P0-5-5 | jjangash | 대기 | State Authority 일반 Merge 요청·검증 RPC 구현 | P0-5-1 |
| P0-5-6 | jjangash | 부분 완료 | 기존 동일 종·동일 등급·다음 등급 풀 규칙을 Fusion으로 이전 | P0-5-5 |
| P0-5-7 | jjangash | 부분 완료 | 기존 Pending Mutation DNA 계승을 Fusion 상태에 적용 | P0-5-6 |
| P0-5-8 | kinggusi | 대기 | authoritative Monster 사망 이벤트를 Gold 지급 API에 전달 | P0-3-4, P0-5-2 |

### P0-6 Boss Timer·공용 Lane

| Task ID | 담당 | 상태 | Codex 작업 | 선행 |
|---|---|---|---|---|
| P0-6-1 | kinggusi | 대기 | Boss 제한시간을 Fusion `TickTimer`로 변경 | P0-3-5 |
| P0-6-2 | kinggusi | 부분 완료 | 기존 공용 Lane 경로를 NetworkTransform과 연결 | P0-3-6 |
| P0-6-3 | kinggusi | 부분 완료 | authoritative Boss 처치 이벤트 구현 | P0-6-1 |
| P0-6-4 | kinggusi | 부분 완료 | authoritative Boss 시간 초과 이벤트 구현 | P0-6-1 |
| P0-6-5 | jjangash | 대기 | Boss 시간 초과 시 MatchState를 `FAILED`로 변경 | P0-6-4 |
| P0-6-6 | jjangash | 대기 | Boss 처치 후 다음 Wave 진행 승인 | P0-6-3 |

### P0-7 Legendary 후보·리롤

| Task ID | 담당 | 상태 | Codex 작업 | 선행 |
|---|---|---|---|---|
| P0-7-1 | jjangash | 대기 | Legendary Merge 재료 잠금 상태 구현 | P0-5-7 |
| P0-7-2 | jjangash | 부분 완료 | 기존 Mythic Choice Balance를 사용해 해금 풀 후보 3종 생성 | P0-7-1 |
| P0-7-3 | jjangash | 대기 | Networked 후보 3종과 남은 리롤 횟수 저장 | P0-7-2 |
| P0-7-4 | jjangash | 대기 | 후보 전체 리롤 RPC 구현 | P0-7-3 |
| P0-7-5 | jjangash | 대기 | 최종 Mythic 선택 RPC 구현 | P0-7-3 |
| P0-7-6 | jjangash | 대기 | 선택된 Mythic에 계승 DNA Mutation 무료 자동 활성화 | P0-7-5 |
| P0-7-7 | jjangash | 대기 | Legendary 후보 선택 Unity UI 구현 | P0-7-3 |
| P0-7-8 | kinggusi | 대기 | 선택 대기 중 잠긴 재료 Alien의 공격·이동 중단 | P0-7-1 |

### P0-8 Damage·Projectile·Hit

| Task ID | 담당 | 상태 | Codex 작업 | 선행 |
|---|---|---|---|---|
| P0-8-1 | jjangash | 부분 완료 | 기존 DamagePayload에 최종 Hit·Mutation 필드 확정 | P0-1-6 |
| P0-8-2 | jjangash | 부분 완료 | StatCalculator 결과로 AlienAttackSnapshot 생성 | P0-8-1 |
| P0-8-3 | kinggusi | 부분 완료 | Target Search를 State Authority 기준으로 전환 | P0-3-4 |
| P0-8-4 | kinggusi | 대기 | Networked Projectile Spawn 구현 | P0-8-3 |
| P0-8-5 | kinggusi | 부분 완료 | Projectile 충돌 시 DamagePayload 적용을 authoritative하게 처리 | P0-8-1, P0-8-4 |
| P0-8-6 | kinggusi | 부분 완료 | 기존 Kill Deduplicator를 Kill·Support Kill 이벤트와 연결 | P0-8-5 |
| P0-8-7 | jjangash | 대기 | Kill·Support Kill을 골드가 아닌 통계 장부에 기록 | P0-8-6 |

### P0-9 재접속과 복구

| Task ID | 담당 | 상태 | Codex 작업 | 선행 |
|---|---|---|---|---|
| P0-9-1 | jjangash | 대기 | PlayerConnectionState와 연결 종료 감지 | P0-2-2 |
| P0-9-2 | jjangash | 대기 | 연결 종료 중 Session·Gold 장부 유지 | P0-9-1 |
| P0-9-3 | jjangash | 대기 | 보드·Alien·Injector Snapshot 생성 | P0-5-7 |
| P0-9-4 | jjangash | 대기 | Legendary 선택·Mutation 상태 Snapshot 생성 | P0-7-6 |
| P0-9-5 | kinggusi | 부분 완료 | Wave·Monster·Boss Snapshot 제공 | P0-3-6 |
| P0-9-6 | kinggusi | 대기 | Boss TickTimer 복구 정보 제공 | P0-6-1 |
| P0-9-7 | jjangash | 대기 | 재접속 시 User/System·Battle Snapshot 재적용 조정 | P0-9-3~6 |
| P0-9-8 | kinggusi | 대기 | 복구된 Battle Object의 시각 상태 재생성 | P0-9-7 |

### P0-10 Settlement

| Task ID | 담당 | 상태 | Codex 작업 | 선행 |
|---|---|---|---|---|
| P0-10-1 | kinggusi | 부분 완료 | 기존 BattleSummary에 Wave·Monster·Boss Kill 장부 연결 | P0-8-6 |
| P0-10-2 | kinggusi | 부분 완료 | 플레이어별 Kill·Support Kill·Boss Kill 집계 완성 | P0-10-1 |
| P0-10-3 | jjangash | 대기 | 플레이어별 Gold 초기·획득·소비·최종 장부 구현 | P0-5-2 |
| P0-10-4 | kinggusi | 부분 완료 | Match 종료 시 서버 DTO와 일치하는 BattleSettlementSummary 생성 | P0-10-1~3 |
| P0-10-5 | jjangash | 대기 | Settlement 서버 전송 클라이언트 구현 | P0-10-4 |
| P0-10-6 | jjangash | 부분 완료 | 기존 Settlement 서버의 검증·멱등 저장 완성 | P0-10-5 |
| P0-10-7 | jjangash | 대기 | 이탈·관전·미복귀 보상 자격 판정 | P0-10-6 |

### P0-11 Unity Feature Test Hub

모든 기능을 하나의 거대한 Scene에 합치지 않는다. Task별 테스트 Scene은 격리하고, 중앙 Hub가 목록·실행·검증 기록을 연결한다. 상세 기준은 `docs/04_TEST_STRATEGY.md`를 따른다.

| Task ID | 담당 | 상태 | Codex 작업 | 선행 |
|---|---|---|---|---|
| P0-11-1 | kinggusi | 대기 | 기존 1차 Scene·테스트 Scene·Editor 테스트를 조사하고 Task 연결 가능 여부 목록 작성 | 없음 |
| P0-11-2 | jjangash | 대기 | Feature Test Case 메타데이터, Catalog, Task ID·담당·Scene 경로·체크리스트 계약 구현 | P0-11-1 |
| P0-11-3 | kinggusi | 대기 | Unity MCP 또는 Editor API로 중앙 `FeatureTestHub` Scene과 격리 Scene 실행 UI 구현 | P0-11-2 |
| P0-11-4 | kinggusi | 대기 | 기존 `TestGameScene`과 Battle 검증 Scene을 정리하고 Catalog에 등록 | P0-11-3 |
| P0-11-5 | jjangash | 대기 | Catalog 경로·중복 Task ID·Missing Scene·Production Build 포함을 검사하는 Editor 테스트 구현 | P0-11-3 |
| P0-11-6 | jjangash | 대기 | Fusion 2클라이언트 기능 검증 실행 절차와 테스트 데이터 초기화 방식 구현 | P0-2-4, P0-11-3 |
| P0-11-7 | jjangash | 대기 | Hub에서 jjangash·kinggusi 공동 Smoke Test를 수행하고 검증 기록 확정 | P0-11-4~6 |

---

## 2. P1 — Mutation·강화·보상

### P1-1 Mutation

| Task ID | 담당 | 상태 | Codex 작업 |
|---|---|---|---|
| P1-1-1 | jjangash | 대기 | 순수 Mythic Mutation 버튼 상태 구현 |
| P1-1-2 | jjangash | 대기 | 최초 Mutation 300골드 차감·랜덤 추첨 |
| P1-1-3 | jjangash | 대기 | 재변이 비용 `600→1,200→2,400→4,800` 구현 |
| P1-1-4 | jjangash | 대기 | 현재 Mutation을 재추첨 후보에서 제외 |
| P1-1-5 | jjangash | 대기 | Mutation된 Mythic에 Injector 사용 시 무료 교체 |
| P1-1-6 | kinggusi | 대기 | Mutation별 외형·Animation·Effect 연결 |

### P1-2 Mutation StatCalculator

| Task ID | 담당 | 상태 | Codex 작업 |
|---|---|---|---|
| P1-2-1 | jjangash | 대기 | 8종 Mutation 스탯 데이터 스키마 정의 |
| P1-2-2 | jjangash | 대기 | 공격력·공격속도·사거리 계산 구현 |
| P1-2-3 | jjangash | 대기 | 지속 피해·경제형·도박형 계산 계약 구현 |
| P1-2-4 | kinggusi | 대기 | 지속 피해 Hit 적용 |
| P1-2-5 | kinggusi | 대기 | 상태 이상 이동·공격 효과 적용 |
| P1-2-6 | kinggusi | 대기 | 광역·단일 Boss형 공격 메커니즘 구현 |

### P1-3 영구·인게임 강화

| Task ID | 담당 | 상태 | Codex 작업 |
|---|---|---|---|
| P1-3-1 | jjangash | 부분 완료 | 영구 강화 레벨·공격 성장 공식 최종 확정 |
| P1-3-2 | jjangash | 부분 완료 | 레벨별 Gold·조각·성장 세포 Balance 최종 조정 |
| P1-3-3 | jjangash | 부분 완료 | 영구 강화 서버 검증·Transaction 마감 |
| P1-3-4 | jjangash | 대기 | 일반 공명 인게임 강화 구현 |
| P1-3-5 | jjangash | 대기 | 신화 공명 인게임 강화 구현 |
| P1-3-6 | kinggusi | 부분 완료 | 강화된 Snapshot을 공격 동작에 적용 |

### P1-4 행성·Monster·Boss 밸런스

| Task ID | 담당 | 상태 | Codex 작업 |
|---|---|---|---|
| P1-4-1 | kinggusi | 대기 | 수성~태양 Monster·Boss 구성안 작성 |
| P1-4-2 | kinggusi | 대기 | 행성별 HP·속도·Boss 패턴 초안 작성 |
| P1-4-3 | jjangash | 완료 | Battle·Monster·Wave Balance JSON 스키마 기반 제공 |
| P1-4-4 | jjangash | 완료 | Battle Excel 변환·검증·Manifest 기반 제공 |
| P1-4-5 | kinggusi | 부분 완료 | Canonical Balance를 Battle 실행기에 최종 연결 |
| P1-4-6 | kinggusi | 대기 | 권장 스펙 기준 전투 난이도 플레이 테스트 |

### P1-5 Settlement 보상

| Task ID | 담당 | 상태 | Codex 작업 |
|---|---|---|---|
| P1-5-1 | jjangash | 정책 선행 | 클리어·패배 보상 계산 정책 구현 |
| P1-5-2 | jjangash | 정책 선행 | 행성별 accountGold 보상 테이블 작성 |
| P1-5-3 | jjangash | 대기 | 관전·이탈·미복귀 자격 판정 구현 |
| P1-5-4 | jjangash | 대기 | 영구 재화 지급 Transaction 구현 |
| P1-5-5 | jjangash | 부분 완료 | 기존 멱등 저장을 영구 보상 중복 지급 방지까지 확장 |
| P1-5-6 | kinggusi | 대기 | 실제 전투 종료 결과와 서버 응답 대조 테스트 |

---

## 3. P2 — 후속 콘텐츠

| Task ID | 담당 | 상태 | Codex 작업 |
|---|---|---|---|
| P2-1-1 | jjangash | 정책 선행 | 행성 Stage 해금·입장·보상 서버 구현 |
| P2-1-2 | kinggusi | 정책 선행 | 행성별 Map·Waypoint·Boss Scene 구현 |
| P2-2-1 | jjangash | 정책 선행 | 일일 콘텐츠 횟수·초기화·보상 서버 구현 |
| P2-2-2 | kinggusi | 정책 선행 | 배양 구역 5 Stage Battle 구현 |
| P2-2-3 | kinggusi | 정책 선행 | 변이 연구소 5 Stage Battle 구현 |
| P2-3-1 | jjangash | 정책 선행 | Quest·Achievement 조건·보상 서버 구현 |
| P2-3-2 | kinggusi | 대기 | Battle Quest 진행 이벤트 제공 |
| P2-4-1 | jjangash | 정책 선행 | 무한 Wave 시즌·랭킹·구간 보상 서버 구현 |
| P2-4-2 | kinggusi | 정책 선행 | 무한 Wave 전투 모드 구현 |
| P2-4-3 | kinggusi | 정책 선행 | 무한 Wave 난이도 증가 공식 구현 |
| P2-5-1 | jjangash | 부분 완료 | Breeding Unity UI·API 연결 |
| P2-6-1 | jjangash | 정책 선행 | Shop·스킨·편의 상품 서버·UI 구현 |
| P2-6-2 | kinggusi | 대기 | 스킨·Projectile·처치 Effect 적용 |

---

## 4. 권장 실행 순서

### 1차: 계약 고정

1. jjangash: `P0-1-1`
2. jjangash: `P0-1-2`
3. jjangash: `P0-1-3`
4. jjangash: `P0-1-4`
5. kinggusi: `P0-1-5`
6. jjangash: `P0-1-6`

### 상시 QA 기반 작업

- kinggusi: `P0-11-1 → P0-11-4`
- jjangash: `P0-11-2, P0-11-5 → P0-11-7`
- `P0-11`은 제품 기능 구현을 대체하지 않으며, Scene/Prefab 편집과 충돌하지 않는 시점에 순차 진행한다.

### 2차: 첫 병렬 작업

- jjangash: `P0-2-1 → P0-2-4`
- kinggusi: `P0-3-1 → P0-3-6`

첫 통합 목표:

> 두 클라이언트가 같은 방에서 같은 Wave·Monster·Boss를 본다.

### 3차: 규칙과 전투 병렬 작업

- jjangash: `P0-5 → P0-7`
- kinggusi: `P0-6 → P0-8`

두 번째 통합 목표:

> 두 플레이어가 Kidnap·Merge하고 같은 Monster를 공격하며 탈락 상태가 일치한다.

### 4차: 복구와 정산

- jjangash: `P0-9-1~4, P0-9-7, P0-10-3, P0-10-5~7`
- kinggusi: `P0-9-5~6, P0-9-8, P0-10-1~2, P0-10-4`

최종 P0 통합 목표:

> 2인 입장부터 80 Wave 종료, 재접속, Settlement 저장까지 한 흐름으로 동작한다.

---

## 5. Codex 작업 요청 템플릿

### jjangash(User/System)용

```text
docs/98_IMPLEMENTATION_TASKS.md의 [Task ID] 작업을 수행해줘.

목표:
[해당 행의 Codex 작업]

반드시 읽을 문서:
- AGENTS.md
- docs/99_MASTER.md
- docs/98_IMPLEMENTATION_TASKS.md
- docs/ai/User.md
- docs/ai/Shared.md

담당:
- User/System

금지:
- Battle 담당 파일 수정 금지
- Scene/Prefab YAML 직접 수정 금지
- 임의 GUID/.meta 생성 금지
- commit/push 금지

작업 전:
- 계획
- 예상 변경 파일
- Battle/Shared 영향 보고

완료 후:
- 변경 파일
- 컴파일 결과
- 테스트 결과
- 위험과 후속 Task 보고
```

### kinggusi(Battle)용

```text
docs/98_IMPLEMENTATION_TASKS.md의 [Task ID] 작업을 수행해줘.

목표:
[해당 행의 Codex 작업]

반드시 읽을 문서:
- AGENTS.md
- docs/99_MASTER.md
- docs/98_IMPLEMENTATION_TASKS.md
- docs/ai/Battle.md
- docs/ai/Shared.md

담당:
- Battle

금지:
- Economy, Kidnap, Merge, Mutation 규칙 수정 금지
- Spring Boot 비즈니스 로직 수정 금지
- Shared 계약 직접 변경 금지
- Scene/Prefab YAML 직접 수정 금지
- commit/push 금지

Shared 변경이 필요하면:
- 필요한 필드
- 사용 위치
- 변경 이유
- Breaking Change 여부만 보고

작업 전:
- 계획
- 예상 변경 파일
- User/System/Shared 영향 보고

완료 후:
- 변경 파일
- Unity 컴파일 결과
- 테스트 결과
- Missing Reference 여부
- 위험과 후속 Task 보고
```

---

## 6. 작업 완료 갱신 규칙

각 Task 완료 후 해당 행의 상태를 갱신한다.

```text
대기 → 부분 완료 → 검증 대기 → 완료
```

완료로 변경할 조건:

1. 담당 도메인 컴파일 성공
2. 관련 자동 테스트 성공
3. Shared 변경이면 양쪽 컴파일 성공
4. Unity 작업이면 Missing Script/Reference 확인
5. 코드 변경은 독립 읽기 전용 리뷰에서 필수 수정 사항 없음
6. Unity 기능이면 Task 전용 자동 테스트 또는 테스트 Scene과 Feature Test Hub 등록
7. Unity UI·Scene·입력·연출·2인 상호작용이면 jjangash와 kinggusi 모두 PASS
8. 후속 Task가 사용할 공개 API 또는 인수인계 기록 작성

Task 상태만 바꾸는 커밋은 구현 커밋에 함께 포함할 수 있다.
