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
| P0-2-5 | kinggusi | 검증 대기 | Battle Scene이 Session 정보를 받는 Adapter 완성 | P0-2-3 |
| P0-2-6 | kinggusi | 검증 대기 | 두 개인 필드와 공용 Lane을 Session 기준으로 초기화 | P0-2-5 |

### P0-3 Networked Wave·Monster·Boss

| Task ID | 담당 | 상태 | Codex 작업 | 선행 |
|---|---|---|---|---|
| P0-3-1 | kinggusi | 완료 | 현재 `BattleWaveExecutor`를 Fusion State Authority 구조로 분리 | P0-1-6 |
| P0-3-2 | kinggusi | 완료 | 현재 Wave와 Wave 진행 상태를 `[Networked]`로 구현 | P0-3-1 |
| P0-3-3 | kinggusi | 완료 | Monster를 `Runner.Spawn`으로 생성 | P0-3-2 |
| P0-3-4 | kinggusi | 완료 | 기존 Runtime Identity·HP·사망 기반을 Fusion Network 상태와 연결 | P0-3-3 |
| P0-3-5 | kinggusi | 완료 | Boss NetworkObject Spawn 구현 | P0-3-3 |
| P0-3-6 | kinggusi | 완료 | Monster·Boss NetworkTransform 적용 | P0-3-3 |
| P0-3-7 | jjangash | 완료 | Wave 시작·종료 시 MatchState 검증 API 제공 | P0-2-3 |

### P0-4 탈락·관전·매치 상태

| Task ID | 담당 | 상태 | Codex 작업 | 선행 |
|---|---|---|---|---|
| P0-4-1 | kinggusi | 완료 | 개인 필드별 살아 있는 Monster 수를 authoritative하게 집계 | P0-3-4 |
| P0-4-2 | kinggusi | 완료 | 80/90 경고 이벤트와 100마리 탈락 이벤트 발생 | P0-4-1 |
| P0-4-3 | jjangash | 완료 | 탈락 이벤트를 받아 Networked PlayerBattleState 변경 | P0-4-2 |
| P0-4-4 | jjangash | 완료 | 탈락 플레이어의 Kidnap·Merge·Mutation·강화 차단 | P0-4-3 |
| P0-4-5 | kinggusi | 완료 | 탈락 필드 신규 Monster Spawn 중단 | P0-4-3 |
| P0-4-6 | kinggusi | 검증 대기 | 탈락 플레이어 관전 카메라·UI 전환 | P0-4-3 |
| P0-4-7 | jjangash | 검증 대기 | 두 플레이어 탈락 시 MatchState를 `FAILED`로 변경 | P0-4-3 |
| P0-4-8 | jjangash | 검증 대기 | 80 Wave 완료 시 MatchState를 `CLEARED`로 변경 | P0-3-7 |

### P0-5 State Authority Kidnap·Merge·Gold

| Task ID | 담당 | 상태 | Codex 작업 | 선행 |
|---|---|---|---|---|
| P0-5-1 | jjangash | 검증 대기 | 플레이어별 Networked inGameGold 구현 | P0-2-3 |
| P0-5-2 | jjangash | 검증 대기 | canonical killGold를 두 플레이어의 독립 개인 지갑에 동일하게 권위 지급 | P0-5-1 |
| P0-5-3 | jjangash | 완료 | State Authority Kidnap 요청·검증 RPC 구현 | P0-5-1 |
| P0-5-4 | jjangash | 검증 대기 | 기존 24칸·첫 빈칸·누적 비용 규칙을 Fusion 검증으로 이전 | P0-5-3 |
| P0-5-5 | jjangash | 검증 대기 | State Authority 일반 Merge 요청·검증 RPC 및 동일 종·등급 권한 검증 구현 | P0-5-1 |
| P0-5-6 | jjangash | 검증 대기 | 동일 종·등급 머지는 다음 등급 풀로 승급하고, 머지 불가 점유 슬롯은 서버 권위로 교환 | P0-5-5 |
| P0-5-7 | jjangash | 검증 대기 | Pending Mutation DNA 계승과 Mutation Injector 사용을 Fusion 상태에 적용 | P0-5-6 |
| P0-5-8 | kinggusi | 검증 대기 | authoritative Monster 사망 이벤트를 개인/팀 Gold 장부와 Kill 통계에 전달 | P0-3-4, P0-5-2 |

### P0-6 Boss Timer·공용 Lane

| Task ID | 담당 | 상태 | Codex 작업 | 선행 |
|---|---|---|---|---|
| P0-6-1 | kinggusi | 검증 대기 | Boss 제한시간을 Fusion `TickTimer`로 변경 | P0-3-5 |
| P0-6-2 | kinggusi | 검증 대기 | 기존 공용 Lane 경로를 NetworkTransform과 연결 | P0-3-6 |
| P0-6-3 | kinggusi | 검증 대기 | authoritative Boss 처치 이벤트 구현 | P0-6-1 |
| P0-6-4 | kinggusi | 검증 대기 | authoritative Boss 시간 초과 이벤트 구현 | P0-6-1 |
| P0-6-5 | jjangash | 검증 대기 | Boss 시간 초과 시 MatchState를 `FAILED`로 변경 | P0-6-4 |
| P0-6-6 | jjangash | 검증 대기 | Boss 처치 후 다음 Wave 진행 승인 | P0-6-3 |

### P0-7 Legendary 후보·리롤

| Task ID | 담당 | 상태 | Codex 작업 | 선행 |
|---|---|---|---|---|
| P0-7-1 | jjangash | 검증 대기 | Legendary Merge 재료 잠금 상태 구현 | P0-5-7 |
| P0-7-2 | jjangash | 검증 대기 | 기존 Mythic Choice Balance를 사용해 후보 3종 생성 | P0-7-1 |
| P0-7-3 | jjangash | 검증 대기 | Networked 후보 3종과 남은 리롤 횟수 저장 | P0-7-2 |
| P0-7-4 | jjangash | 검증 대기 | 후보 전체 리롤 RPC 구현 | P0-7-3 |
| P0-7-5 | jjangash | 검증 대기 | 최종 Mythic 선택 RPC 구현 | P0-7-3 |
| P0-7-6 | jjangash | 검증 대기 | 선택된 Mythic에 계승 DNA 상태 적용: 해금 Mythic은 ACTIVE, 잠금 Mythic은 SEALED로 보존하고 효과 차단 | P0-7-5 |
| P0-7-7 | jjangash | 검증 대기 | Legendary 후보 선택 Unity UI 구현 | P0-7-3 |
| P0-7-8 | kinggusi | 검증 대기 | 선택 대기 중 잠긴 재료 Alien의 공격·이동 중단 | P0-7-1 |

### P0-8 Damage·Projectile·Hit

| Task ID | 담당 | 상태 | Codex 작업 | 선행 |
|---|---|---|---|---|
| P0-8-1 | jjangash | 검증 대기 | 기존 DamagePayload에 최종 Hit·Mutation 필드 확정 | P0-1-6 |
| P0-8-2 | jjangash | 검증 대기 | StatCalculator 결과로 AlienAttackSnapshot 생성 | P0-8-1 |
| P0-8-3 | kinggusi | 검증 대기 | Target Search를 State Authority 기준으로 전환 | P0-3-4 |
| P0-8-4 | kinggusi | 검증 대기 | Networked Projectile Spawn 구현 | P0-8-3 |
| P0-8-5 | kinggusi | 검증 대기 | Projectile 충돌 시 DamagePayload 적용을 authoritative하게 처리 | P0-8-1, P0-8-4 |
| P0-8-6 | kinggusi | 검증 대기 | 기존 Kill Deduplicator를 Kill·Support Kill 이벤트와 연결 | P0-8-5 |
| P0-8-7 | jjangash | 검증 대기 | Kill·Support Kill을 골드가 아닌 통계 장부에 기록 | P0-8-6 |

### P0-9 재접속과 복구

| Task ID | 담당 | 상태 | Codex 작업 | 선행 |
|---|---|---|---|---|
| P0-9-1 | jjangash | 검증 대기 | PlayerConnectionState와 연결 종료 감지 | P0-2-2 |
| P0-9-2 | jjangash | 검증 대기 | 연결 종료 중 Session·Gold 장부 유지 | P0-9-1 |
| P0-9-3 | jjangash | 검증 대기 | 보드·Alien·Injector Snapshot 생성 | P0-5-7 |
| P0-9-4 | jjangash | 검증 대기 | Legendary 선택·Mutation 상태 Snapshot 생성 | P0-7-6 |
| P0-9-5 | kinggusi | 검증 대기 | Wave·Monster·Boss Snapshot 제공 | P0-3-6 |
| P0-9-6 | kinggusi | 검증 대기 | Boss TickTimer 복구 정보 제공 | P0-6-1 |
| P0-9-7 | jjangash | 검증 대기 | 동일 User ID 슬롯 예약과 Fusion 권위 상태 재동기화 조정 | P0-9-3~6 |
| P0-9-8 | kinggusi | 검증 대기 | 복구된 Board·Monster Object의 시각 상태 재생성 | P0-9-7 |

### P0-10 Settlement

| Task ID | 담당 | 상태 | Codex 작업 | 선행 |
|---|---|---|---|---|
| P0-10-1 | kinggusi | 검증 대기 | 기존 BattleSummary에 Wave·Monster·Boss Kill 장부 연결 | P0-8-6 |
| P0-10-2 | kinggusi | 검증 대기 | 플레이어별 Kill·Support Kill·Boss Kill 집계 완성 | P0-10-1 |
| P0-10-3 | jjangash | 검증 대기 | 플레이어별 Gold 초기·획득·소비·최종 장부 구현 | P0-5-2 |
| P0-10-4 | kinggusi | 검증 대기 | Match 종료 시 서버 DTO와 일치하는 BattleSettlementSummary 생성 | P0-10-1~3 |
| P0-10-5 | jjangash | 검증 대기 | Settlement 서버 전송 클라이언트와 명시적 Retry 경계 구현 | P0-10-4 |
| P0-10-6 | jjangash | 완료 | 기존 Settlement 서버의 검증·멱등 저장 완성 | P0-10-5 |
| P0-10-7 | jjangash | 완료 | 이탈·관전·미복귀 보상 자격 판정 | P0-10-6 |

> P0-10 implementation update (2026-08-01): Fusion State Authority snapshots per-player initial/earned/spent/final in-game Gold, canonical Kill/Support/Boss Kill audit, and disconnect-derived `abandoned`. `BattleSettlementCoordinator` builds the shared Spring DTO, posts once at Match termination, retains a failed request as pending, and permits only explicit Retry. Server validation and idempotent persistence tests are complete; real Match termination HTTP observation remains in the P0 integration gate.

> P0-10 final-wave/reward correction (2026-08-01): Settlement `finalWave` now uses the Networked `HighestClearedWave`, not the currently attempted Wave. Therefore a failure during Wave 70 settles Wave 69. An empty Battle Scene map setting resolves to the canonical first planet `NEPTUNE`. Disconnect reward exclusion uses a Networked 120-second grace timer; a short disconnect is not immediately marked `abandoned`, and reconnect clears the timer.

### P0-11 Unity Feature Test Hub

모든 기능을 하나의 거대한 Scene에 합치지 않는다. Task별 테스트 Scene은 격리하고, 중앙 Hub가 목록·실행·검증 기록을 연결한다. 상세 기준은 `docs/04_TEST_STRATEGY.md`를 따른다.

| Task ID | 담당 | 상태 | Codex 작업 | 선행 |
|---|---|---|---|---|
| P0-11-1 | kinggusi | 완료 | 기존 1차 Scene·테스트 Scene·Editor 테스트를 조사하고 Task 연결 가능 여부 목록 작성 | 없음 |
| P0-11-2 | jjangash | 완료 | Feature Test Case 메타데이터, Catalog, Task ID·담당·Scene 경로·체크리스트 계약 구현 | P0-11-1 |
| P0-11-3 | kinggusi | 완료 | Unity MCP 또는 Editor API로 중앙 `FeatureTestHub` Scene과 격리 Scene 실행 UI 구현 | P0-11-2 |
| P0-11-4 | kinggusi | 완료 | 기존 `TestGameScene`과 Battle 검증 Scene을 정리하고 Catalog에 등록 | P0-11-3 |
| P0-11-5 | jjangash | 완료 | Catalog 경로·중복 Task ID·Missing Scene·Production Build 포함을 검사하는 Editor 테스트 구현 | P0-11-3 |
| P0-11-6 | jjangash | 완료 | Fusion 2클라이언트 기능 검증 실행 절차와 테스트 데이터 초기화 방식 구현 | P0-2-4, P0-11-3 |
| P0-11-7 | jjangash | 검증 대기 | Hub와 `docs/P0_INTEGRATION_TEST_SCENARIO.md`로 공동 Smoke Test를 수행하고 검증 기록 확정 | P0-11-4~6 |

### P0 자동 검증 체크포인트 (2026-08-01)

- Latest Unity EditMode regression: **335/335 passed**, failed 0, skipped 0.
- P0-9 reconnect snapshot correction: Mythic `freeRerollsRemaining` / `paidRerollsRemaining` now store canonical remaining counts rather than used counters.
- Fusion join failure cleanup: pending User ID reservations are cleared on shutdown, disconnect, and connection failure.
- `P0-8-2` implementation is present: Spring exposes canonical per-player permanent-level attack snapshots, and Fusion State Authority validates the manifest/version before injecting damage, attack rate, and range into runtime Units. The task remains `검증 대기` until the final user integration gate.

- Unity EditMode: `328/328` 통과.
- Server: `286/286` 통과, BalanceTool `70/70` 통과.
- canonical generated JSON과 Battle generated JSON 연속 2회 변환 SHA 동일.
- Unity MCP: `Battle.unity` Loaded=true, Dirty=false, validate issue 0, Missing Script 0, Broken Prefab 0.
- 실제 Fusion Host/Standalone Client: 같은 `MyDefense-Dev` 세션에 Player 1/2 등록, 양쪽 Lane Monster 10마리, 플레이어별 Gold 100,000, canonical/battle hash 일치, NetworkTransform 이동 확인.
- 실제 재접속: Client 종료 시 Player 2 `DISCONNECTED`와 Gold/User ID 보존, 같은 `dev-client` 재실행 시 slot 2·Gold·alive count 복구, Standalone Runtime Error 0 확인.
- 사용자 통합 게이트: uGUI 포인터 입력(Kidnap/Drag/Merge/Mythic 선택), Boss 처치·시간 초과, 재접속, Match 종료 Settlement는 `docs/P0_INTEGRATION_TEST_SCENARIO.md`로 최종 확인한다.
- P0-8-2 실동작 증거: 실제 Fusion Host/Standalone Client에서 양쪽 player catalog 48종을 서버로부터 로드했고, Kidnap된 Unit이 서버 계산 snapshot을 적용했다. Fusion 전투에서는 유효 snapshot 전까지 공격하지 않으며 Battle Runtime은 계산식을 복제하거나 임시 피해 fallback을 사용하지 않는다.
- 릴리스 전 보안 위험: 현재 프로젝트 전반에 인증 계층이 없어 기존 username을 요청 파라미터로 위조할 수 있다. anonymous 개발 identity 허용은 `local` profile에만 한정했지만, 운영 배포 전 인증 principal과 playerId 바인딩이 필요하다.
- 운영 설정 안전장치: base 설정에는 default profile을 두지 않는다. local 개발은 `SPRING_PROFILES_ACTIVE=local`, 운영은 production profile을 반드시 명시하며, profile 생략/prod에서는 local roster Controller와 Adapter가 생성되지 않는 통합 테스트를 유지한다.
- 복구성 위험: attack catalog HTTP 실패는 현재 세션에서 fault latch로 고정된다. 자동 Retry를 금지하는 현재 P0 정책에는 맞지만, 후속 단계에서 사용자 명시 Retry 또는 제한된 재시도 UX를 결정해야 한다.

---

## 2. P1 — Mutation·강화·보상

### P1-1 Mutation

| Task ID | 담당 | 상태 | Codex 작업 |
|---|---|---|---|
| P1-1-1 | jjangash | 검증 대기 | 순수 Mythic Mutation 버튼 상태 구현 |
| P1-1-2 | jjangash | 검증 대기 | 최초 Mutation 300골드 차감·랜덤 추첨 |
| P1-1-3 | jjangash | 검증 대기 | 재변이 비용 `600→1,200→2,400→4,800` 구현 |
| P1-1-4 | jjangash | 검증 대기 | 현재 Mutation을 재추첨 후보에서 제외 |
| P1-1-5 | jjangash | 검증 대기 | Mutation된 Mythic에 Injector 사용 시 무료 교체 |
| P1-1-6 | kinggusi | 검증 대기 | Mutation별 외형·Animation·Effect 연결 |

### P1-2 Mutation StatCalculator

| Task ID | 담당 | 상태 | Codex 작업 |
|---|---|---|---|
| P1-2-1 | jjangash | 검증 대기 | 8종 Mutation 스탯 데이터 스키마 정의 |
| P1-2-2 | jjangash | 검증 대기 | 공격력·공격속도·사거리 계산 구현 |
| P1-2-3 | jjangash | 검증 대기 | 지속 피해·경제형·도박형 계산 계약 구현 |
| P1-2-4 | kinggusi | 검증 대기 | 지속 피해 Hit 적용 |
| P1-2-5 | kinggusi | 검증 대기 | 상태 이상 이동·공격 효과 적용 |
| P1-2-6 | kinggusi | 검증 대기 | 광역·단일 Boss형 공격 메커니즘 구현 |

### P1-3 영구·인게임 강화

| Task ID | 담당 | 상태 | Codex 작업 |
|---|---|---|---|
| P1-3-1 | jjangash | 완료 | 영구 강화 레벨·공격 성장 공식 최종 확정 |
| P1-3-2 | jjangash | 완료 | 레벨별 Gold·조각·성장 세포 Balance 최종 조정 |
| P1-3-3 | jjangash | 완료 | 영구 강화 서버 검증·Transaction 마감 |
| P1-3-4 | jjangash | 검증 대기 | 일반 공명 인게임 강화 구현 |
| P1-3-5 | jjangash | 검증 대기 | 신화 공명 인게임 강화 구현 |
| P1-3-6 | kinggusi | 검증 대기 | 강화된 Snapshot을 공격 동작에 적용 |

### P1-4 행성·Monster·Boss 밸런스

| Task ID | 담당 | 상태 | Codex 작업 |
|---|---|---|---|
| P1-4-1 | kinggusi | 완료 | 수성~태양 Monster·Boss 구성안 작성 |
| P1-4-2 | kinggusi | 완료 | 행성별 HP·속도·Boss 패턴 초안 작성 |
| P1-4-3 | jjangash | 완료 | Battle·Monster·Wave Balance JSON 스키마 기반 제공 |
| P1-4-4 | jjangash | 완료 | Battle Excel 변환·검증·Manifest 기반 제공 |
| P1-4-5 | kinggusi | 완료 | Canonical Balance를 Battle 실행기에 최종 연결 |
| P1-4-6 | kinggusi | 검증 대기 | 권장 스펙 기준 전투 난이도 플레이 테스트 |

### P1-5 Settlement 보상

| Task ID | 담당 | 상태 | Codex 작업 |
|---|---|---|---|
| P1-5-1 | jjangash | 완료 | 클리어·패배 보상 계산 정책 구현 |
| P1-5-2 | jjangash | 완료 | 행성별 accountGold 보상 테이블 작성 |
| P1-5-3 | jjangash | 완료 | 관전·이탈·미복귀 자격 판정 구현 |
| P1-5-4 | jjangash | 완료 | 영구 재화 지급 Transaction 구현 |
| P1-5-5 | jjangash | 완료 | 기존 멱등 저장을 영구 보상 중복 지급 방지까지 확장 |
| P1-5-6 | kinggusi | 완료 | 실제 전투 종료 결과와 서버 응답 대조 테스트 — local/dev Host/Client terminal·동일 payload 멱등 재처리 PASS |
| P1-5-7 | Shared | 부분 완료 | 인증된 matchmaking/Fusion Session authority를 Spring trusted roster adapter에 연결 — local/dev Adapter 완료, production JWT Adapter 선행 필요 |

> 8-1 보상 정책 구현 메모: 행성별 80 Wave를 기준으로 `highestClearedWave`(마지막으로 완전히 클리어한 Wave)에 따라 패배/재클리어 Gold를 계산한다. Wave 10~80 체크포인트는 최초 1회만 Gold·Universal Piece를 지급하고, Wave 80 최초 클리어는 행성별 Diamond를 1회 지급한다. 관전자와 연결이 유지된 탈락자는 지급 대상이며, 명시적 이탈/120초 초과 미복귀는 지급하지 않는다. 보상 Balance는 `BattleReward` Excel 시트와 `battle-reward.json`으로 관리한다. Settlement 서버는 Runtime이 확정한 `abandoned` 플래그를 영속화한다. local/dev 실제 2인 terminal Summary·저장·멱등 재처리는 P1-5-6에서 완료했고, production 인증 roster와 장시간 이탈·복귀 운영 검증은 P1-5-7 및 출시 전 2PC 통합 게이트로 유지한다.

> P1 자동 구현·검증 기록(2026-08-02 당시 상태): Mutation 활성화/재변이/Injector, 8종 전투 효과, 영구 강화 Balance, 일반·신화 공명, 9행성·80 Wave·10 Wave 간격 Boss, Settlement HTTP 계약을 구현했다. Boss 공통 패턴 템플릿은 모든 canonical Boss Wave ID로 확장되며 Spawn 시 phase 1, HP 50%에서 이동속도 1.35배가 적용된다. Settlement는 신뢰된 matchmaking/session authority가 사전 등록한 2인 roster와 canonical 완료 Wave별 Spawn/Kill 총계를 대조한 뒤에만 영구 보상을 지급한다. 공개 Attack Snapshot API는 roster를 등록하지 않으며, 신뢰된 provider가 없는 현재 런타임에서는 안전하게 보상을 거부한다. 이미 저장된 동일 정산은 roster 만료·서버 재시작 후에도 기존 결과를 멱등 반환한다. Server 296/296, BalanceTool 70/70, Unity EditMode 359/359, Battle Scene validate issue 0을 통과했다. 실제 Fusion Host/Standalone Client에서의 Mutation·공명·행성 난이도는 `docs/P1_INTEGRATION_TEST_SCENARIO.md` 사용자 통합 게이트로 남기며, 정상 Settlement E2E는 P1-5-7 선행 후 수행한다.

> P1-5-6/7 구현 기록(2026-08-24 당시 상태): Unity Settlement는 `IBattleSessionRosterRegistration` 교체 경계에만 의존한다. local/dev에서는 Fusion State Authority의 두 참가자를 loopback 개발 API로 Spring trusted roster에 원자 등록하고, 등록 성공 후 Wave와 Settlement를 진행한다. 자동 E2E는 roster 등록 → Wave 80 Summary 저장 → 두 사용자 영구 Gold/Universal Piece/Diamond 지급 → 동일 요청 재전송 무중복 지급을 검증한다. Spring Profile 생략/prod에서는 개발 Adapter가 생성되지 않는다. `FUTURE_AUTH_REPLACEMENT` 표식은 production에서 JWT principal + matchmaking 검증 Adapter로 바꿀 위치이며, 운영 Adapter가 없으면 fail-closed한다. Server 310/310, BalanceTool 77/77, Unity EditMode 378/378, Battle Scene validate issue 0. 실제 Fusion Host/Standalone Client 종료 검증 전까지 P1-5-6은 `검증 대기`, production 인증 Adapter 전까지 P1-5-7은 `부분 완료`다.

> P1-5-6 실제 종료 검증(2026-08-27): 최신 `dev`의 Windows Development Build에서 Host=`dev-host`, Client=`dev-client`가 새 non-P1VAL Session으로 W010 Boss timeout `DEFEAT`를 생성했다. Roster 등록은 W001보다 먼저 1회 성공했고, 실제 Summary는 `finalWave=9`, Normal Kill 256, Kill Gold 5,120으로 Spring canonical 검증을 통과했다. 최초 응답은 `ACCEPTED/alreadyProcessed=false`, 캡처한 동일 payload 재전송은 `alreadyProcessed=true`였으며 Settlement 1건·Player 2건·0원 Claim 2건과 양 사용자 영구재화 불변을 확인했다. timeout 뒤 Boss가 남는 결함은 권위 Despawn으로 수정하고 전체 Unity EditMode 439/439 및 실제 Host/Client W010 소멸 스모크를 통과했다. 따라서 P1-5-6을 `완료`로 승격하고 production JWT Adapter는 P1-5-7에 유지한다.

---

## 3. P2 — 후속 콘텐츠

| Task ID | 담당 | 상태 | Codex 작업 |
|---|---|---|---|
| P2-1-1 | jjangash | 완료 | 행성 Stage 해금·입장·보상 서버 구현 |
| P2-1-2 | kinggusi | 검증 대기 | 단일 `Battle.unity`의 `PlanetContentProfile + 환경 Prefab` 기반 행성 Presentation 구현 — P2-1-1 local/dev 입장·roster `mapId` 연결 호환 및 자동검증 PASS, Shared Snapshot `mapId`·production Adapter·jjangash 사람 비주얼 검증 대기 |
| P2-2-1 | jjangash | 완료 | 일일 콘텐츠 횟수·초기화·보상 서버 구현 |
| P2-2-2 | kinggusi | 대기 | 배양 구역 5 Stage Battle 구현 — Shared Session 문맥과 canonical `DailyBattleStage` 선행 계약 준비 완료 |
| P2-2-3 | kinggusi | 대기 | 변이 연구소 5 Stage Battle 구현 — Shared Session 문맥과 canonical `DailyBattleStage` 선행 계약 준비 완료 |
| P2-3-1 | jjangash | 부분 완료 | Settlement 기반 Quest 사실 장부·조건별 정확히 한 번 누적 구현 — 일일/주간 조건 정의·보상·초기화·조회 UI는 정책 및 후속 구현 대기 |
| P2-3-2 | kinggusi | 부분 완료 | Battle Quest 진행 이벤트 제공 |
| P2-4-1 | jjangash | 정책 선행 | 무한 Wave 시즌·랭킹·구간 보상 서버 구현 |
| P2-4-2 | kinggusi | 정책 선행 | 무한 Wave 전투 모드 구현 |
| P2-4-3 | kinggusi | 정책 선행 | 무한 Wave 난이도 증가 공식 구현 |
| P2-5-1 | jjangash | 검증 대기 | Breeding Unity UI·API 연결 — 190조합 Excel/JSON·공개 조합표, 서버 조합 추첨·3슬롯·사용자 단위 요청 멱등 이력·가속 구현. Unity API로 정식 Screen/Shortcut Prefab을 생성하고 Unit 탭 전용 진입, 보상 준비 상태, 조합표를 연결했다. NORMAL~LEGEND 28종 기본 보유, 24시간 타이머, 10분당 Diamond 100 가속 계약을 반영했다. Server 310/310, BalanceTool 77/77, Unity EditMode 396/396, Scene validate 0. 실제 계정의 부모 선택·시작·가속·수령 Play 검증과 최종 아트 적용이 남음 |
| P2-6-1 | jjangash | 정책 선행 | Shop·스킨·편의 상품 서버·UI 구현 |
| P2-6-2 | kinggusi | 대기 | 스킨·Projectile·처치 Effect 적용 |

> P2-1 정책 확정(2026-08-27): 행성 자체를 Stage로 사용하고 서버가 trusted roster에서 확정한 canonical `mapId`를 Fusion Session/Snapshot/Settlement까지 고정한다. Battle은 단일 `Battle.unity`와 공통 Board/Lane/Waypoint/Boss Runtime을 유지하며 `PlanetContentProfile`과 presentation-only 환경 Prefab으로 배경·재질·조명·Particle/환경 Effect만 교체한다. Additive Scene과 행성 고유 기믹·Boss 패턴·컷신은 1차 범위에서 제외한다. 알 수 없거나 비활성인 mapId, canonical PlanetBattle 대비 Profile 누락·중복·비활성은 fallback 없이 Wave 시작 전에 fail-closed한다. local/dev 구현은 진행하되 production 완료 판정에는 P1-5-7 JWT/matchmaking Adapter와 실제 2클라이언트 Smoke Test가 필요하다.

> P2-1-2 local/dev 구현 기록(2026-08-27): authoritative `NetworkString<_16>` mapId 최초 고정, Client 복제 대기·불일치 fail-closed, Spawn 초기화 race 차단, 9개 canonical Profile/환경 Prefab/Material/Effect placeholder 및 presentation allowlist 검증을 구현했다. PlanetContent targeted 14/14, Unity 전체 EditMode 455/455, compile error 0, 독립 리뷰 Blocker 0/Major 0으로 PASS했다. 현재 Shared `BattleSessionSnapshot`에는 mapId가 없으므로 Snapshot schema 확장은 Shared 담당 후속 의존성으로 남긴다. 최종 상태는 사람 비주얼 검증, 실제 2클라이언트 Smoke, P1-5-7 production Adapter 전까지 `검증 대기`다.

> P2-1-2 최신 dev 동기화 기록(2026-08-31): 완료된 P2-1-1의 local/dev 행성 입장·trusted roster `mapId` 계약과 PlanetContent의 authoritative Fusion `mapId` binding을 함께 유지한다. 서버가 승인한 canonical `mapId`가 Session Adapter를 통해 동일 Profile로 적용되고, 알 수 없거나 불일치하는 값은 Wave 시작 전에 fail-closed한다. 최신 `origin/dev` `3816fae` 병합 후 집중 EditMode 72/72, 전체 EditMode 478/478, Battle Scene 자동 검사, `Battle.unity` 단독 Windows Development Build를 통과했고 독립 리뷰 차단 0으로 판정됐다. 남은 완료 게이트는 Shared `BattleSessionSnapshot.mapId` 계약, P1-5-7 production JWT/matchmaking Adapter, 실제 production 경계 2클라이언트 Smoke, jjangash 사람 비주얼 PASS다.

> P2-1-2 Snapshot v3 후속(2026-09-02): PR #110이 병합된 `origin/dev` `e422255` 기준으로 Shared `BattleSessionSnapshot.mapId` 의존성을 해소했다. Battle reconnect Snapshot 캡처는 spawned State Authority의 `AuthoritativeMapId` 누락 및 `BattleSessionContext.MapId`와의 ordinal exact 불일치를 fail-closed하고, 검증된 authority 값을 Snapshot에 투영한다. 서버 권위 `SessionSource`는 Unity Snapshot에 추가하지 않는다. PlanetContent·Session Adapter·State Authority·reconnect 집중 EditMode 72/72, Unity 전체 EditMode 504/504, compile error 0을 통과했다. 호출 가능한 Unity MCP 도구는 0개였고 Scene/Prefab은 변경하지 않았다. 남은 완료 게이트는 P1-5-7 production JWT/matchmaking Adapter, 실제 production 경계 2클라이언트 reconnect Smoke, jjangash 사람 비주얼 PASS다.

> P2-3-2 Shared 선행 계약 보정(2026-08-30): FAILED 매치에서 `finalWave + 1` 미완료 Wave의 실제 Spawn과 처치를 분리 검증하도록 Unity/Spring `BattleSettlementSummary`에 `waveSpawnFacts`와 `partialWaveKills`를 확정했다. 두 장부는 `spawnGroupId`, canonical Spawn row/ordinal, `fieldOwnerPlayerSlot`을 공유하고 Kill 귀속은 `killerPlayerSlot`/`supportPlayerSlot`으로 표현한다. `killedAtTick`과 사용자 ID 기반 귀속은 전송 계약에서 제거했다. Fusion `ulong runtimeMonsterId`는 decimal string으로 전송하며 두 배열을 unsigned 정렬한다. `summaryHash`는 해당 속성 자체를 제외한 canonical JSON의 SHA-256으로 Unity/Spring 동일 fixture를 고정한다. Battle State Authority의 실제 장부 투영과 Quest 영속 Processor 연결은 후속 구현·2클라이언트 검증 전까지 남아 있어 `부분 완료`다.

> P2-2-1 구현 기록(2026-08-30): `CULTIVATION_ZONE`과 `MUTATION_LAB`은 각각 KST 자정 기준 하루 3회, Stage 1~5 순차 해금, 입장 즉시 차감, 일반 실패 소모, trusted 매칭·Session·서버 장애 반환 정책을 사용한다. 최초 클리어는 기본 보상과 같은 양을 1회 추가하고 클리어 Stage만 소탕한다. 사용자별 request ID와 operation/payload를 함께 저장해 입장·결과·반환·소탕을 멱등 처리하며 동일 사용자 병렬 요청은 pessimistic lock으로 직렬화한다. Excel `DailyContent` 시트에서 `daily-content.json`을 생성하고 성장 세포·변이 촉매를 영구 재화로 지급한다. local/dev 결과 API는 loopback으로 제한하고 production JWT principal Adapter 전에는 공개 Controller를 fail-closed한다. 실제 두 던전 전투는 P2-2-2·P2-2-3에서 연결한다. Server 전용 11/11, 전체 351/351, BalanceTool 77/77, Unity EditMode 461/461 및 독립 리뷰 P0/P1 0을 기준으로 완료 처리했다. 로비 재화 카드의 최종 해상도·아트는 사용자 시각 검증이 남는다.

> P2-2-2·3 Shared/User-System 선행 계약(2026-09-02): 두 일일 전투를 솔로·단일 `Battle.unity`·Player 1 Board/Lane 전용으로 확정하고 Stage 1~5를 각각 3/4/5/6/7 Wave, 120/150/180/210/240초로 정의했다. Excel `DailyBattleStage` 50행에서 `daily-battle-stage.json`을 생성해 Spring Registry와 Unity canonical loader가 동일 수치를 검증한다. Shared `DailyBattleSessionContext` schema v1은 `runId`, `battleSessionId`, `contentType`, `stage`, canonical `mapId`, Balance version/hash를 Unity/Spring 공용 fixture로 고정하고 `sessionSource`는 서버 소유로 제외했다. 배양 구역은 일반 Monster·상태 이상 없음, 변이 연구소는 일반/Elite·Wave 상태 이상·마지막 Wave Boss를 사용한다. Server 363/363, BalanceTool 82/82, Unity 계약 집중 32/32 및 전체 EditMode 506/506, `convertBalance`, Unity sync, compile error 0을 통과했다. 실제 State Authority 실행·Daily Result trusted Adapter·placeholder 환경 Profile은 kinggusi의 P2-2-2·3 Battle 구현 범위이므로 두 Task는 `대기`다.
> P2-3-2 Battle 장부 구현(2026-08-30): canonical `spawnGroupId`를 Wave runtime까지 보존하고 State Authority가 성공적으로 생성한 Monster의 Runtime ID, Spawn group/row/ordinal, owner slot을 세션 장부로 기록한다. FAILED에서 `finalWave + 1`의 실제 Spawn 전체를 `waveSpawnFacts`, 그중 실제 Kill만 `partialWaveKills`로 투영하며 player ID는 trusted Summary의 slot 1/2로 변환한다. 두 배열은 `ulong` 숫자 순서로 정렬하고 개인/팀 Kill·Support·Boss·KillGold 및 Player Gold 장부식을 Settlement 생성 전에 재검증한다. VICTORY는 두 배열을 비우며 Unity/Spring v2 hash fixture `d48e3596480b89baa9b17e71acb8e9a833cfc1eb42fe8d46aa8653250e0bb2a6`, Battle 집중 EditMode 45/45, 전체 EditMode 462/462, Battle-only Windows Standalone Build를 통과했다. 새 non-P1VAL Session의 Host/Client 접속 및 trusted roster 등록도 통과했으나 자동 실행 보드가 비어 terminal 상태가 발생하지 않아 실제 FAILED POST·응답 대조는 남았다. User/System Quest 영속 Processor 연결과 해당 Smoke 전까지 상태는 `부분 완료`로 유지한다.

> P2-3-2 최신 dev 동기화 기록(2026-08-31): PlanetContent PR #102와 P2-1-1이 포함된 `origin/dev` `f2ff276`을 충돌 없이 병합했다. authoritative Fusion `mapId` 고정·불일치 fail-closed와 State Authority Spawn/Kill audit·FAILED Settlement projection을 함께 보존했다. Settlement/PlanetContent/StateAuthority 집중 EditMode 160/160, 전체 EditMode 481/481, Battle Scene 검사(Missing Script 0, Broken Prefab 0), `Battle.unity` 단독 Windows Development Build를 통과했다. 실제 terminal FAILED POST·응답 대조와 User/System Quest 영속 Processor 연결은 아직 남아 있으므로 상태는 `부분 완료`다.

> P2-3-2 미완료 Wave Settlement 스모크 보완(2026-09-01): non-P1VAL Development Session의 State Authority가 `RUNNING`, `currentWave == highestClearedWave + 1`, 실제 Spawn/Kill audit과 미해소 Spawn을 확인한 뒤 기존 terminal 경로로 `FAILED`를 정확히 한 번 확정하는 수동 스모크 진입점을 추가했다. Production Build와 P1VAL에서는 컴파일 또는 실행되지 않는다. Fixture 규칙 29/29, 관련 집중 EditMode 117/117, 전체 EditMode 493/493, `Battle.unity` 단독 Windows Development Build(error 0), 독립 리뷰 차단 0을 통과했다. 실제 Session `P23-PARTIAL-20260901-225458`에서 `DEFEAT/finalWave=2`, Wave 3 Spawn fact 4건과 Partial Kill 2건을 전송해 최초 `ACCEPTED/alreadyProcessed=false`, Unity/Spring SHA-256 일치, 동일 payload 재전송 `alreadyProcessed=true`, H2 Settlement 1건/Player 2건을 확인했다. Battle 측 실제 HTTP 게이트는 완료됐으며 User/System Quest 영속 Processor 연결 전까지 상태는 `부분 완료`를 유지한다.

> P2-3-1 Quest Settlement 기반 구현(2026-09-02): trusted roster가 서버에서 부여한 `SessionSource(PRODUCTION/LOCAL_DEVELOPMENT/VALIDATION_FIXTURE)`를 Settlement에 영속하고, `PRODUCTION` 정산만 Quest 영구 진행에 반영한다. `QuestSettlementProcessor`는 저장된 Player Settlement 총계에서 참가·승리·행성 승리·완료 Wave·Kill·Support Kill·Boss Kill 사실을 만들며 `(settlementId, userId, questConditionId)` unique 장부와 사용자 잠금으로 동시 재처리까지 정확히 한 번 반영한다. FAILED의 미완료 Wave Kill/Support Kill은 이미 검증된 Player 총계에 포함되므로 partial 배열을 다시 더하지 않는다. Shared `BattleSessionSnapshot`은 authoritative `mapId`를 추가해 schema v3로 올리고 Builder 투영·누락 거부와 Unity/Spring 공용 JSON fixture를 검증한다. 기존 Settlement null source는 production으로 추정하지 않고 Quest에서 제외하며 운영 Migration 순서는 `docs/DATABASE_MIGRATION_POLICY.md`에 고정했다. Server 357/357, BalanceTool 77/77, Unity Shared 21/21 및 전체 EditMode 496/496를 통과했다. 일일/주간 Quest 정의·보상·초기화·조회 API/UI와 production JWT/matchmaking Adapter 및 production E2E가 남아 `부분 완료`다.

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

---

## Battle 비주얼 개선 후속 기록

현재 Battle 네트워크 검증은 기능·동기화 우선으로 진행한다. 이후 별도 비주얼 작업에서 다음 방향을 적용한다.

- 2.5D 캐릭터형 Alien/Monster 표현으로 교체
- 기존 Grid 기반 구조를 유지하면서 3D 맵 타일의 높낮이·재질·테두리·그림자 개선
- 유닛 idle/attack 애니메이션과 공격 이펙트 추가
- 카메라 원근감 및 조명으로 필드 깊이감 강화
- 공용 Boss Lane 전용 연출 추가
- Fusion NetworkObject는 상태·위치 동기화에 집중하고 모델/애니메이션은 로컬 표현으로 분리

이 항목은 현재 네트워크 연결·필드 관점 구현 완료 후 진행하는 후속 작업이며, 이번 단계에서는 구현하지 않는다.

---

## Battle P0-2/P0-5 현행화 (2026-07-23)

- **P0-2-5 / P0-2-6: 검증 대기**
- 완료된 범위: Battle Scene 세션 Adapter, 자동 Host/Client 연결, 개인 필드·공용 Lane 초기화, 플레이어별 Networked inGameGold, canonical Kidnap Pool, 24칸 첫 빈 슬롯 배치, 누적 소환 비용, 네트워크 보드 점유 상태, 드래그 이동.
- 추가 구현 범위: Alien ID·등급·Mutation 메타데이터의 Networked 보존, 동일 종·등급 Merge, 다음 등급 결과, Legendary Mythic 후보/리롤 상태, Mutation Injector, 머지 불가 점유 슬롯의 State Authority 교환.
- 자동 검증: Unity EditMode 및 서버/Balance 회귀 테스트는 기존 통과 기록을 유지한다. Standalone은 빌드 환경 Assertion으로 최신 fallback 수정 반영 여부를 별도 확인해야 한다.
- 현재 수동 검증 대기: Host/Client 양쪽 소환, Gold 독립 차감, 빈 슬롯 이동, 정상 Merge, 서로 다른 등급·종 드래그 시 Swap, Injector 적용, 두 화면의 상대 필드 표시, 반복 이동 후 위치·크기 유지.
- 기존 로비 `/game/merge` API는 Battle Runtime 경로에서 사용하지 않는다. Battle Merge/Swap은 Fusion State Authority가 판정한다.
- P0-2-5/P0-2-6과 P0-5-1/P0-5-4~7은 코드 구현 후 사람 수동 검증이 남은 `검증 대기` 상태다. 검증 완료 전 다음 P0 기능을 완료로 승격하지 않는다.

## P0 자동검증 미완료 목록 (2026-07-23)

현재 서버/Balance 자동검증은 통과했지만, 아래 항목은 Unity Editor/MCP 또는 실제 Fusion 2클라이언트 실행이 필요해 자동검증 완료로 처리하지 않는다.

### Unity Editor·MCP 미검증

- MCP Bridge 재기동 및 Codex 연결: Codex 설정은 `http://127.0.0.1:8081/mcp`를 사용하지만 현재 8081 리스너가 없어 연결 확인 불가.
- Unity 최신 변경분 스크립트 컴파일: 배치 EditMode 실행은 열린 프로젝트 충돌로 결과 XML을 생성하지 못함.
- Unity EditMode 전체 회귀 테스트: 실제 Unity Test Runner 결과 미확인.
- SampleScene/Battle Scene 로드·Dirty·Validate·Missing Script·Broken Prefab 검증 미완료.
- FeatureTestHub 및 격리 검증 Scene: 아직 구현·등록·실행 검증 전(P0-11).

### Fusion 2클라이언트 미검증

- P0-2-5/P0-2-6: Host/Client 양쪽 Session Adapter 초기화와 두 개인 필드·공용 Lane 초기화.
- P0-5-1: 플레이어별 Networked in-game Gold의 독립 차감 및 동기화.
- P0-5-2: Lane과 마지막 공격자에 관계없이 canonical `killGold`를 두 플레이어의 독립 개인 지갑에 동일하게 지급.
- P0-5-4: 24칸 첫 빈 슬롯·누적 비용 Kidnap 권위 검증.
- P0-5-5/P0-5-6: 동일 종·등급 Merge, 다음 등급 승급, 불가 조합 Swap.
- P0-5-7: Mutation Injector 사용과 Pending DNA 계승.
- P0-5-8: Monster 사망 이벤트와 Gold 장부 연결.
- P0-6 Boss Timer·공용 Lane: TickTimer, Boss 처치/시간초과, MatchState 전환.
- P0-8 Damage/Projectile/Hit: 네트워크 Projectile, 권위 충돌, DamagePayload, Kill/Support Kill 장부.
- P0-9 재접속/복구: 연결 종료, Snapshot, Boss Timer 복구, 재접속 재적용.
- P0-10 Settlement: Unity Summary 생성·전송과 서버 정산의 실제 런타임 연동.

### 자동검증 완료로 인정된 범위

- Spring 서버 테스트: 175건 통과.
- BalanceTool 테스트: 38건 통과.
- `compileJava`, `convertBalance`, Balance JSON 결정성, `git diff --check`: 통과.
- 위 결과만으로 P0 전체 완료 또는 Unity/Fusion 통합 완료로 판정하지 않는다.

### 완료 조건

1. MCP Bridge를 8081로 복구하거나 Codex 연결 포트를 실제 Bridge 포트와 일치시킨다.
2. Unity EditMode 결과 XML과 컴파일 로그를 확보한다.
3. P0-2~P0-10의 Feature Test Case를 P0-11 Hub에 등록하고 자동 실행한다.
4. Host/Client 실제 통합 테스트를 통과시킨다.
5. 독립 리뷰에서 P0/P1 지적이 없음을 확인한 후에만 해당 Task를 `완료`로 승격한다.

## 통합 검증 운영 계획 (2026-07-23)

- P0~P2를 하나의 거대한 상태 공유 Scene으로 묶지 않는다. Task별 격리 Scene/Fixture와 공통 Feature Test Hub를 사용한다.
- 각 Task는 순수 단위 테스트 → Unity EditMode/Server 테스트 → MCP 읽기 검증 → 필요한 경우 PlayMode 2인 Smoke Test 순서로 진행한다.
- Fusion 2인 검증은 Host와 Standalone Client를 별도 프로세스로 실행하고, 세션·User ID·Gold·보드·Wave 상태를 양쪽 로그와 화면에서 대조한다.
- MCP는 Unity Editor의 Scene 열기, PlayMode 전환, 버튼/입력, Hierarchy·Console·Networked 상태 읽기를 자동화할 수 있다. 실제 두 프로세스의 모든 입력과 외부 네트워크 조건은 환경에 따라 사람이 최종 확인한다.
- 자동 통합 검증을 위해 P0-11 Feature Test Hub가 필요하다. 각 Case에는 Task ID, 격리 Scene, 초기화 Fixture, 기대 상태, 정리 절차를 등록하며 Production Build에는 포함하지 않는다.

### 처치 Gold 계약 현행화

- `monster-spec.json.killGold`가 인게임 Gold의 유일한 수치 원천이다. 클라이언트 입력이나 계정 Gold를 처치 이벤트에 사용하지 않는다.
- `EACH_FIELD`와 `BOSS_SHARED` 모두 canonical `killGold`를 두 플레이어의 개인 지갑에 각각 동일하게 1회 지급한다.
- `fieldOwnerPlayerId`와 `killerPlayerId`는 지급 대상 결정에 사용하지 않고 필드·처치 통계와 감사 정보에만 사용한다.
- 지급 중복 키는 `(battleSessionId, runtimeMonsterId)`이고 State Authority에서만 소비 처리한다. 기존 레거시 `/game/enemy/kill`은 신규 Battle 흐름에서 사용하지 않는다.
- 현재 P0-5 구현의 중복 키 보관은 State Authority 메모리 장부이며, Host migration/재접속 이후 복구는 P0-9 Snapshot에서 영속화한다. 그 전까지 동일 Session의 Authority 교체는 최종 통합 검증에서 명시적으로 제외하고 위험으로 기록한다.
- 처치 Gold 소비는 플레이어별 개인 지갑에서 독립적으로 처리하며, `TeamInGameGold`는 처치 보상에 사용하지 않는다.
- P0/P1/P2 전체 통합 완료 기준은 단위·회귀 테스트 통과만이 아니라, 변경 Task의 독립 리뷰와 Host/Client 수동 Smoke Test 기록까지 포함한다.

## Codex 작업 오케스트레이션 및 기록 규칙 (2026-07-23)

- PM 메인 Thread는 이 문서에서 가장 높은 우선순위의 실행 가능한 Task를 선택하고, 의존성·소유권·변경 범위를 먼저 보고한다.
- 구현 Thread는 지정된 Task의 코드, Unity MCP 작업, 단위 테스트를 수행한다. Unity `.unity`/`.prefab` YAML은 직접 수정하지 않는다.
- 독립 리뷰 Thread는 읽기 전용으로 diff, 정책, 소유권, 테스트 누락과 위험을 검토한다. 구현 Thread는 자기 작업을 승인할 수 없다.
- 구현 완료 후 순서는 `단위 테스트 → 영향 범위 회귀 테스트 → Unity MCP 검증 → 독립 리뷰 → 필요한 사용자 통합 테스트`이다.
- Unity UI, Scene, Fusion 2인 상호작용은 사용자 통합 테스트 전까지 `검증 대기`로 기록한다.
- 자동 테스트·회귀·MCP 검증·독립 리뷰가 통과하고 사용자 검증이 필수가 아닌 Task는 별도 추가 승인 없이 기능 파일만 명시적으로 stage하여 Task 단위 commit/push를 수행한다.
- 사용자 통합 테스트가 필수인 Task는 자동 commit/push하지 않고 `검증 대기`로 보고한다. 사용자 테스트가 통과하면 해당 Task의 상태와 검증 기록을 갱신한 뒤 commit/push한다.
- 기존 로컬 변경, MCP/Packages/ProjectSettings/Photon 변경, 임시 빌드 산출물은 기능 commit에 포함하지 않는다.
- 각 Task 완료 기록에는 Task ID, 구현 요약, 변경 파일, 자동 테스트, MCP 결과, 리뷰 결과, 사용자 검증, commit SHA, push 결과, 잔여 위험과 다음 Task를 포함한다.
- P0~P2 통합 테스트 실행 절차는 별도 `docs/P0_INTEGRATION_TEST_SCENARIO.md` 문서로 관리하고, 공통 테스트 원칙은 `docs/04_TEST_STRATEGY.md`를 따른다.

## P0-8 Damage·Projectile 현행화 (2026-07-27)

- **P0-8-4: 검증 대기** — `BattleProjectileNetworkState`, `BattleProjectileSpawner`, `BattleProjectileSpawnData`와 Fusion `NetworkObject`/`NetworkTransform` 기반 `BattleProjectile.prefab`를 추가했다. State Authority 전용 Spawn, canonical ProjectileSpec 검증, RuntimeProjectileId 중복 방지, Lifetime TickTimer를 적용했다.
- **P0-8-5: 검증 대기** — Projectile의 Trigger/Collision 충돌을 State Authority에서만 처리하고, 대상 `NetworkObject`와 `IDamageable`을 확인한 뒤 `DamagePayload`를 1회 적용한다. 동일 대상 중복 Hit, Pierce, DestroyOnHit, 소비/Despawn 정책을 유지한다.
- 자동 검증: Projectile 전용 EditMode 5/5, Unity EditMode 전체 298/298, Battle Scene validate issue 0, Missing Script 0, Broken Prefab 0, 신규 Console Error 0, Windows 개발 빌드 errors 0.
- 독립 리뷰: canonical Prefab과 State Authority 경계는 PASS. 실제 Fusion 2클라이언트 Spawn/충돌 및 lifetime·pierce 런타임 검증과 일부 세부 테스트는 후속 P1 보강으로 남아 있다.
- 현재 사용자 검증 대기: Host/Client 실제 Projectile Spawn, Monster 충돌 Damage 반영, 중복 Hit 방지, Pierce/DestroyOnHit, Lifetime 만료 Despawn.
- **P0-8-6 진행 현행화** — State Authority의 Monster 사망 경로에서 `BattleKillDeduplicator`를 사용하고, `(battleSessionId, runtimeMonsterId)` 중복 키와 `BattleKillAuditRecord`를 연결했다. `MonsterStat.LastDamageAttackerId`로 권위 DamagePayload의 공격자 정보를 보존한다. Support Kill 세부 이벤트와 실제 2클라이언트 장부 검증은 남아 있으므로 상태는 `부분 완료`다.
- 다음 구현 우선순위: **P0-6 Boss Timer/결과 전환**의 남은 부분을 마무리한 뒤 P0-4 관전·종료 상태와 P0-7 Mythic 선택 흐름을 진행한다. P0-8-4/5/6은 사용자 통합 검증 전까지 `완료`로 승격하지 않는다.

### P0-6 Boss Timer·결과 전환 구현 현행화 (2026-07-27)

- `BattleWaveStateAuthority`는 State Authority에서 Boss 제한시간을 Fusion `TickTimer`로 생성하고, 만료 시 `TryResolveBossTimeoutFromAuthority()`를 호출한다.
- Boss 처치/시간초과 이벤트는 `BossTimer`를 같은 권위 이벤트에서 즉시 초기화하여 원격 피어에 stale timer가 남지 않게 한다.
- 시간초과는 `MatchState.FAILED`로 전환하고, canonical Wave Catalog이 더 이상 다음 Wave를 제공하지 않으면 하드코딩 라운드가 아닌 Catalog 종료를 기준으로 `MatchState.CLEARED`로 전환한다.
- Boss 처치 후에는 `ValidateWaveStart`를 통과한 State Authority만 다음 Wave를 시작할 수 있다. 중복 처치/시간초과는 executor의 one-shot 상태 guard로 차단한다.
- Boss 처치/시간초과 public 진입점도 `HasWaveAuthority()`를 확인하여 비권위 피어의 로컬 호출을 거부한다. 오프라인 EditMode fixture는 NetworkObject가 없는 경우 기존 검증 경로를 유지한다.
- 자동 검증: P0-6 관련 `BattleWaveExecutorStateTests` 35/35, `BattleWaveStateAuthorityTests` 13/13, `BattleWaveExecutorBalanceTests` 19/19, Unity EditMode 전체 303/303 통과. Battle Scene validate issue 0, Missing Script 0, Broken Prefab 0.
- 독립 리뷰: P0 차단 없음, 조건부 PASS. 실제 live Fusion Runner에서 TickTimer 만료를 직접 재현하는 검증은 수동 통합 테스트로 남아 있다.
- 사용자 검증 대기: Boss Wave 진입 → Timer 만료 `FAILED`, Boss 처치 → 다음 Wave 승인, 최종 Catalog 종료 → `CLEARED`.

### P0-4-6 관전 카메라·UI 구현 현행화 (2026-07-27)

- `BattleSpectatorCameraController`를 Battle Scene의 `Main Camera`에 Unity MCP로 연결했다.
- 로컬 플레이어 상태가 `ELIMINATED` 또는 `SPECTATING`이면 상대 필드(`GridManager`/`EnemyGridParent`)를 관전 대상으로 삼고, 상태가 복귀되면 원래 카메라 Transform을 복원한다.
- Fusion Networked 속성은 `BattleWaveStateAuthority.IsSpawnedForAccess`가 true인 경우에만 읽도록 보호하여 Spawn 전 접근 예외를 차단한다.
- 자동 검증: 관전 전용 EditMode 2/2, Unity EditMode 전체 303/303, Battle Scene validate issue 0, Dirty=false.
- 독립 리뷰: 조건부 PASS. 실제 2클라이언트 탈락 전환/카메라 이동과 Overlay 입력 차단은 사용자 통합 검증으로 남아 있다.
- 현재 사용자 검증 대기: 한 클라이언트 ELIMINATED 전환 → 생존자 필드 관전 및 `SPECTATING` Overlay → 원격 입력 차단 → 재접속/상태 복귀 시 카메라 복원.

### P0-4-7 전체 탈락 FAILED 전환 현행화 (2026-07-27)

- `BattleWaveExecutor`는 두 개인 필드가 모두 탈락한 순간을 한 번만 보고하고 `MatchState.FAILED`로 권위 전환한다.
- 한 명만 탈락한 경우에는 `MatchState.RUNNING`을 유지하며, 탈락 필드의 신규 Spawn만 차단한다.
- `BattleWaveStateAuthority`는 권위 이벤트에서 Networked `MatchStateValue`와 양쪽 탈락 상태를 동기화한다.
- 자동 검증: `BothPlayerLimits_FailMatchOnce`, `BothPlayersEliminated_StillFailsMatchOnce` 포함 기존 Unity EditMode 전체 303/303 통과.
- 사용자 검증 대기: 두 클라이언트에서 양쪽 필드의 100번째 생존 Monster 조건을 재현하고 양쪽 UI에 `FAILED`가 동일하게 표시되는지 확인한다.

### P0-4-8 Catalog 완료 CLEARED 전환 현행화 (2026-07-27)

- `BattleWaveExecutor`는 다음 Wave를 canonical Wave Catalog에서 찾지 못한 시점에 Catalog 종료를 기록하고 `MatchState.CLEARED`로 전환한다.
- 종료 이벤트는 one-shot guard로 중복 보고를 막으며, 종료 후 다음 Wave 시작 요청은 거부한다.
- 하드코딩된 Wave 번호가 아니라 실제 활성 Catalog의 마지막 Wave 이후를 기준으로 판정한다.
- 자동 검증: `CatalogExhausted_IsReportedOnceAndFutureStartIsRejected` 포함 Balance/State 테스트 통과, Unity EditMode 전체 303/303 통과.
- 사용자 검증 대기: 마지막 Wave 완료 후 양쪽 클라이언트에 `CLEARED` 상태가 표시되고 추가 Wave가 생성되지 않는지 확인한다.

### P0-6-2 BOSS_SHARED NetworkTransform 연결 현행화 (2026-07-27)

- Boss Spawn은 State Authority의 `NetworkRunner.Spawn` 경로를 사용하고 `BattleMonsterRuntimeIdentity`에 `BOSS_SHARED` Lane과 공용 소유자(null)를 기록한다.
- 네트워크 Monster Prefab에 `NetworkObject`와 `NetworkTransform`이 모두 없으면 실시간 Spawn을 거부하도록 보호한다.
- `BattleMonsterMovement`는 State Authority에서만 공용 Lane 경로를 진행하고, 원격 피어는 NetworkTransform 결과를 수신한다.
- 자동 검증: Boss Shared Spawn/NetworkTransform 관련 테스트와 Unity EditMode 전체 303/303 통과.
- 독립 리뷰: 조건부 PASS. 실제 Fusion 2클라이언트에서 Boss 위치가 양쪽에 동일하게 복제되는 수동 검증이 남아 있다.
- 현재 사용자 검증 대기: BOSS_SHARED Boss 1마리 Spawn, 두 클라이언트 위치 동기화, 공용 Waypoint 순회 확인.

### P0-7 Legendary 후보·리롤 구현 현행화 (2026-07-27)

- `BattleWaveStateAuthority`가 Legendary Merge 후 canonical Mythic 후보 3종을 결정하고 Fusion Networked 상태에 저장한다.
- 무료/유료 리롤 횟수·비용·선택 타이머는 `mythic-choice-balance.json`에서 읽으며, 유료 리롤은 State Authority가 개인 인게임 Gold에서 차감한다.
- 선택 RPC와 리롤 RPC는 State Authority에서만 상태를 변경하고, 타이머 만료 시 첫 후보를 자동 선택한다.
- 선택 대기 중 Kidnap 버튼·로컬 드래그·Move/Merge/Injector 요청을 차단하고, 선택 완료 후 다시 허용한다.
- 자동 검증: `BattleWaveStateAuthorityTests` 14/14, Unity EditMode 전체 304/304, Battle Scene validate issue 0.
- 독립 리뷰: 조건부 PASS. 실제 Fusion 2클라이언트 후보 동기화, 리롤 Gold 차감, 타이머 만료, 선택 후 조작 복구 수동 검증이 남아 있다.
- 현재 사용자 검증 대기: Legendary 2개 Merge → 후보 3종 → 무료/유료 리롤 → 선택/타임아웃 → 선택 후 보드 조작 복구.

### P0-7 Mythic 후보와 봉인 DNA 정책 (2026-07-27)

- Legendary Merge의 Mythic 후보 풀은 현재 해금 여부와 무관하게 canonical Mythic 후보를 사용한다. 후보 확률의 공정성을 위해 미해금 Mythic도 후보로 제시할 수 있다.
- Mutation Injector는 미해금/잠금 Mythic에 직접 사용할 수 없다. State Authority가 canonical `AlienSpec.isLocked`를 현재 해금 프록시로 확인하고 요청을 거부한다.
- Legendary Merge에서 계승된 DNA가 잠금 Mythic에 도착하면 DNA 문자열은 보존하되 `SEALED` 상태로 저장한다. 잠금 상태에서는 스탯·공격·연출 효과를 활성화하지 않는다.
- 해금된 Mythic에 계승된 DNA는 `ACTIVE`, Mythic 이전의 DNA는 `PENDING`, 인젝터 자체는 `INJECTOR` 상태로 보존한다. 동일 DNA를 삭제하거나 재추첨하지 않는다.
- 사용자별 해금 스냅샷이 Battle Entry 계약에 포함되면 `isLocked` 프록시를 사용자별 해금 판정으로 교체한다. 해금 이후 SEALED DNA의 활성화 시점과 UI는 후속 P1 Mutation 작업에서 확정한다.
- 현재 P0 구현에는 사용자별 Mythic 해금 목록이 Fusion에 전달되지 않으므로 `isLocked=false`인 전역 공개 Mythic을 해당 플레이어가 실제로 미해금인 경우까지 판별할 수 없다. Battle Entry 해금 스냅샷 연결 전까지 이 제한을 수동 검증 항목으로 유지하며, 임의로 DNA를 폐기하거나 전역 잠금으로 오판하지 않는다.
