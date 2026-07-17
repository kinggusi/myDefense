# 왹져 디펜스 구현 작업 분해표

> 기준 문서: `docs/99_MASTER.md`
> 기준일: 2026-07-18
> 목적: 두 명의 담당자가 Codex에 한 작업씩 전달할 수 있도록 남은 구현을 겹치지 않는 단위로 분리한다.

## 0. 사용 방법

### 담당 표기

- **나(User/System)**: 게임 규칙, 경제, Alien, Kidnap, Merge, Mutation, StatCalculator, Spring Boot, Fusion 시스템 상태
- **동료(Battle)**: Monster, Boss, Wave, Target, Projectile, Hit, Battle Scene, NetworkTransform
- Shared 파일의 실제 수정자는 **나(User/System)**로 고정하고 동료는 검토 의견만 전달한다.

### 상태 표기

- `완료`: 현재 코드에 구현과 검증 기반이 존재한다.
- `부분 완료`: 타입이나 로컬 기반은 있으나 Fusion 통합 또는 최종 정책 반영이 남았다.
- `대기`: 본 구현이 필요하다.
- `정책 선행`: 구현 전 정책 확정이 필요하다.

### 브랜치 원칙

- 나: `feature/user-*`
- 동료: `feature/battle-*`
- 한 브랜치에는 이 문서의 Task 한 개 또는 강하게 결합된 연속 Task만 포함한다.
- Shared 변경이 필요하면 동료가 직접 수정하지 않고 필요한 필드·이유·사용처를 보고한다.
- 각 작업은 최신 `dev`를 기준으로 시작한다.
- 커밋과 Push는 사용자 승인 후에만 수행한다.

---

## 1. P0 — 2인 멀티 전투 성립

P0가 완료되어야 실제 2클라이언트 협동 매치를 처음부터 Settlement까지 실행할 수 있다.

### P0-1 Shared 전투 계약

| Task ID | 담당 | 상태 | Codex 작업 | 완료 기준 |
|---|---|---|---|---|
| P0-1-1 | 나 | 부분 완료 | 기존 `PlayerBattleState`, `MatchState`를 정책과 대조하고 `PlayerConnectionState` 추가 | 전투 상태와 연결 상태가 분리된 Shared Enum 및 테스트 |
| P0-1-2 | 나 | 대기 | `BattleSessionSnapshot` DTO 설계 | Player, Wave, Gold, 보드 복구 필드 포함 |
| P0-1-3 | 나 | 대기 | `LegendaryChoiceState` DTO 설계 | 재료 ID, 후보 3종, 리롤, 선택 상태 포함 |
| P0-1-4 | 나 | 부분 완료 | 서버 Settlement DTO와 Unity `BattleSummary`를 대조해 공통 `BattleSettlementSummary` 계약 작성 | Unity와 Spring 필드가 1:1 대응 |
| P0-1-5 | 동료 | 대기 | P0-1-1~4 계약의 Battle 사용처를 읽기 전용으로 검토 | 누락 필드, 불필요 필드, 사용처 보고서 제출 |
| P0-1-6 | 나 | 대기 | 동료 검토 결과를 Shared 계약에 반영 | 양쪽 컴파일 및 계약 테스트 통과 |

진행 순서:

```text
P0-1-1~4 → P0-1-5 → P0-1-6
```

### P0-2 Fusion 2인 Battle Session

| Task ID | 담당 | 상태 | Codex 작업 | 선행 |
|---|---|---|---|---|
| P0-2-1 | 나 | 대기 | Fusion Runner 생성·종료 관리 구현 | P0-1-6 |
| P0-2-2 | 나 | 대기 | Player 1/2 참가, PlayerRef와 User ID 연결 | P0-2-1 |
| P0-2-3 | 나 | 부분 완료 | 기존 `BattleSessionContext`를 Fusion Battle Session ID 및 MatchState와 연결 | P0-2-2 |
| P0-2-4 | 나 | 대기 | 2인 준비 완료 후 전투 시작 처리 | P0-2-3 |
| P0-2-5 | 동료 | 부분 완료 | Battle Scene이 Session 정보를 받는 Adapter 완성 | P0-2-3 |
| P0-2-6 | 동료 | 부분 완료 | 두 개인 필드와 공용 Lane을 Session 기준으로 초기화 | P0-2-5 |

### P0-3 Networked Wave·Monster·Boss

| Task ID | 담당 | 상태 | Codex 작업 | 선행 |
|---|---|---|---|---|
| P0-3-1 | 동료 | 부분 완료 | 현재 `BattleWaveExecutor`를 Fusion State Authority 구조로 분리 | P0-1-6 |
| P0-3-2 | 동료 | 대기 | 현재 Wave와 Wave 진행 상태를 `[Networked]`로 구현 | P0-3-1 |
| P0-3-3 | 동료 | 대기 | Monster를 `Runner.Spawn`으로 생성 | P0-3-2 |
| P0-3-4 | 동료 | 부분 완료 | 기존 Runtime Identity·HP·사망 기반을 Fusion Network 상태와 연결 | P0-3-3 |
| P0-3-5 | 동료 | 대기 | Boss NetworkObject Spawn 구현 | P0-3-3 |
| P0-3-6 | 동료 | 대기 | Monster·Boss NetworkTransform 적용 | P0-3-3 |
| P0-3-7 | 나 | 대기 | Wave 시작·종료 시 MatchState 검증 API 제공 | P0-2-3 |

### P0-4 탈락·관전·매치 상태

| Task ID | 담당 | 상태 | Codex 작업 | 선행 |
|---|---|---|---|---|
| P0-4-1 | 동료 | 부분 완료 | 개인 필드별 살아 있는 Monster 수를 authoritative하게 집계 | P0-3-4 |
| P0-4-2 | 동료 | 부분 완료 | 80/90 경고 이벤트와 100마리 탈락 이벤트 발생 | P0-4-1 |
| P0-4-3 | 나 | 대기 | 탈락 이벤트를 받아 Networked PlayerBattleState 변경 | P0-4-2 |
| P0-4-4 | 나 | 대기 | 탈락 플레이어의 Kidnap·Merge·Mutation·강화 차단 | P0-4-3 |
| P0-4-5 | 동료 | 대기 | 탈락 필드 신규 Monster Spawn 중단 | P0-4-3 |
| P0-4-6 | 동료 | 대기 | 탈락 플레이어 관전 카메라·UI 전환 | P0-4-3 |
| P0-4-7 | 나 | 대기 | 두 플레이어 탈락 시 MatchState를 `FAILED`로 변경 | P0-4-3 |
| P0-4-8 | 나 | 대기 | 80 Wave 완료 시 MatchState를 `CLEARED`로 변경 | P0-3-7 |

### P0-5 State Authority Kidnap·Merge·Gold

| Task ID | 담당 | 상태 | Codex 작업 | 선행 |
|---|---|---|---|---|
| P0-5-1 | 나 | 대기 | 플레이어별 Networked inGameGold 구현 | P0-2-3 |
| P0-5-2 | 나 | 대기 | Monster 처치 시 양쪽 플레이어에게 각각 100% 골드 지급 | P0-5-1 |
| P0-5-3 | 나 | 대기 | State Authority Kidnap 요청·검증 RPC 구현 | P0-5-1 |
| P0-5-4 | 나 | 부분 완료 | 기존 24칸·첫 빈칸·누적 비용 규칙을 Fusion 검증으로 이전 | P0-5-3 |
| P0-5-5 | 나 | 대기 | State Authority 일반 Merge 요청·검증 RPC 구현 | P0-5-1 |
| P0-5-6 | 나 | 부분 완료 | 기존 동일 종·동일 등급·다음 등급 풀 규칙을 Fusion으로 이전 | P0-5-5 |
| P0-5-7 | 나 | 부분 완료 | 기존 Pending Mutation DNA 계승을 Fusion 상태에 적용 | P0-5-6 |
| P0-5-8 | 동료 | 대기 | authoritative Monster 사망 이벤트를 Gold 지급 API에 전달 | P0-3-4, P0-5-2 |

### P0-6 Boss Timer·공용 Lane

| Task ID | 담당 | 상태 | Codex 작업 | 선행 |
|---|---|---|---|---|
| P0-6-1 | 동료 | 대기 | Boss 제한시간을 Fusion `TickTimer`로 변경 | P0-3-5 |
| P0-6-2 | 동료 | 부분 완료 | 기존 공용 Lane 경로를 NetworkTransform과 연결 | P0-3-6 |
| P0-6-3 | 동료 | 부분 완료 | authoritative Boss 처치 이벤트 구현 | P0-6-1 |
| P0-6-4 | 동료 | 부분 완료 | authoritative Boss 시간 초과 이벤트 구현 | P0-6-1 |
| P0-6-5 | 나 | 대기 | Boss 시간 초과 시 MatchState를 `FAILED`로 변경 | P0-6-4 |
| P0-6-6 | 나 | 대기 | Boss 처치 후 다음 Wave 진행 승인 | P0-6-3 |

### P0-7 Legendary 후보·리롤

| Task ID | 담당 | 상태 | Codex 작업 | 선행 |
|---|---|---|---|---|
| P0-7-1 | 나 | 대기 | Legendary Merge 재료 잠금 상태 구현 | P0-5-7 |
| P0-7-2 | 나 | 부분 완료 | 기존 Mythic Choice Balance를 사용해 해금 풀 후보 3종 생성 | P0-7-1 |
| P0-7-3 | 나 | 대기 | Networked 후보 3종과 남은 리롤 횟수 저장 | P0-7-2 |
| P0-7-4 | 나 | 대기 | 후보 전체 리롤 RPC 구현 | P0-7-3 |
| P0-7-5 | 나 | 대기 | 최종 Mythic 선택 RPC 구현 | P0-7-3 |
| P0-7-6 | 나 | 대기 | 선택된 Mythic에 계승 DNA Mutation 무료 자동 활성화 | P0-7-5 |
| P0-7-7 | 나 | 대기 | Legendary 후보 선택 Unity UI 구현 | P0-7-3 |
| P0-7-8 | 동료 | 대기 | 선택 대기 중 잠긴 재료 Alien의 공격·이동 중단 | P0-7-1 |

### P0-8 Damage·Projectile·Hit

| Task ID | 담당 | 상태 | Codex 작업 | 선행 |
|---|---|---|---|---|
| P0-8-1 | 나 | 부분 완료 | 기존 DamagePayload에 최종 Hit·Mutation 필드 확정 | P0-1-6 |
| P0-8-2 | 나 | 부분 완료 | StatCalculator 결과로 AlienAttackSnapshot 생성 | P0-8-1 |
| P0-8-3 | 동료 | 부분 완료 | Target Search를 State Authority 기준으로 전환 | P0-3-4 |
| P0-8-4 | 동료 | 대기 | Networked Projectile Spawn 구현 | P0-8-3 |
| P0-8-5 | 동료 | 부분 완료 | Projectile 충돌 시 DamagePayload 적용을 authoritative하게 처리 | P0-8-1, P0-8-4 |
| P0-8-6 | 동료 | 부분 완료 | 기존 Kill Deduplicator를 Kill·Support Kill 이벤트와 연결 | P0-8-5 |
| P0-8-7 | 나 | 대기 | Kill·Support Kill을 골드가 아닌 통계 장부에 기록 | P0-8-6 |

### P0-9 재접속과 복구

| Task ID | 담당 | 상태 | Codex 작업 | 선행 |
|---|---|---|---|---|
| P0-9-1 | 나 | 대기 | PlayerConnectionState와 연결 종료 감지 | P0-2-2 |
| P0-9-2 | 나 | 대기 | 연결 종료 중 Session·Gold 장부 유지 | P0-9-1 |
| P0-9-3 | 나 | 대기 | 보드·Alien·Injector Snapshot 생성 | P0-5-7 |
| P0-9-4 | 나 | 대기 | Legendary 선택·Mutation 상태 Snapshot 생성 | P0-7-6 |
| P0-9-5 | 동료 | 부분 완료 | Wave·Monster·Boss Snapshot 제공 | P0-3-6 |
| P0-9-6 | 동료 | 대기 | Boss TickTimer 복구 정보 제공 | P0-6-1 |
| P0-9-7 | 나 | 대기 | 재접속 시 User/System·Battle Snapshot 재적용 조정 | P0-9-3~6 |
| P0-9-8 | 동료 | 대기 | 복구된 Battle Object의 시각 상태 재생성 | P0-9-7 |

### P0-10 Settlement

| Task ID | 담당 | 상태 | Codex 작업 | 선행 |
|---|---|---|---|---|
| P0-10-1 | 동료 | 부분 완료 | 기존 BattleSummary에 Wave·Monster·Boss Kill 장부 연결 | P0-8-6 |
| P0-10-2 | 동료 | 부분 완료 | 플레이어별 Kill·Support Kill·Boss Kill 집계 완성 | P0-10-1 |
| P0-10-3 | 나 | 대기 | 플레이어별 Gold 초기·획득·소비·최종 장부 구현 | P0-5-2 |
| P0-10-4 | 동료 | 부분 완료 | Match 종료 시 서버 DTO와 일치하는 BattleSettlementSummary 생성 | P0-10-1~3 |
| P0-10-5 | 나 | 대기 | Settlement 서버 전송 클라이언트 구현 | P0-10-4 |
| P0-10-6 | 나 | 부분 완료 | 기존 Settlement 서버의 검증·멱등 저장 완성 | P0-10-5 |
| P0-10-7 | 나 | 대기 | 이탈·관전·미복귀 보상 자격 판정 | P0-10-6 |

---

## 2. P1 — Mutation·강화·보상

### P1-1 Mutation

| Task ID | 담당 | 상태 | Codex 작업 |
|---|---|---|---|
| P1-1-1 | 나 | 대기 | 순수 Mythic Mutation 버튼 상태 구현 |
| P1-1-2 | 나 | 대기 | 최초 Mutation 300골드 차감·랜덤 추첨 |
| P1-1-3 | 나 | 대기 | 재변이 비용 `600→1,200→2,400→4,800` 구현 |
| P1-1-4 | 나 | 대기 | 현재 Mutation을 재추첨 후보에서 제외 |
| P1-1-5 | 나 | 대기 | Mutation된 Mythic에 Injector 사용 시 무료 교체 |
| P1-1-6 | 동료 | 대기 | Mutation별 외형·Animation·Effect 연결 |

### P1-2 Mutation StatCalculator

| Task ID | 담당 | 상태 | Codex 작업 |
|---|---|---|---|
| P1-2-1 | 나 | 대기 | 8종 Mutation 스탯 데이터 스키마 정의 |
| P1-2-2 | 나 | 대기 | 공격력·공격속도·사거리 계산 구현 |
| P1-2-3 | 나 | 대기 | 지속 피해·경제형·도박형 계산 계약 구현 |
| P1-2-4 | 동료 | 대기 | 지속 피해 Hit 적용 |
| P1-2-5 | 동료 | 대기 | 상태 이상 이동·공격 효과 적용 |
| P1-2-6 | 동료 | 대기 | 광역·단일 Boss형 공격 메커니즘 구현 |

### P1-3 영구·인게임 강화

| Task ID | 담당 | 상태 | Codex 작업 |
|---|---|---|---|
| P1-3-1 | 나 | 부분 완료 | 영구 강화 레벨·공격 성장 공식 최종 확정 |
| P1-3-2 | 나 | 부분 완료 | 레벨별 Gold·조각·성장 세포 Balance 최종 조정 |
| P1-3-3 | 나 | 부분 완료 | 영구 강화 서버 검증·Transaction 마감 |
| P1-3-4 | 나 | 대기 | 일반 공명 인게임 강화 구현 |
| P1-3-5 | 나 | 대기 | 신화 공명 인게임 강화 구현 |
| P1-3-6 | 동료 | 부분 완료 | 강화된 Snapshot을 공격 동작에 적용 |

### P1-4 행성·Monster·Boss 밸런스

| Task ID | 담당 | 상태 | Codex 작업 |
|---|---|---|---|
| P1-4-1 | 동료 | 대기 | 수성~태양 Monster·Boss 구성안 작성 |
| P1-4-2 | 동료 | 대기 | 행성별 HP·속도·Boss 패턴 초안 작성 |
| P1-4-3 | 나 | 완료 | Battle·Monster·Wave Balance JSON 스키마 기반 제공 |
| P1-4-4 | 나 | 완료 | Battle Excel 변환·검증·Manifest 기반 제공 |
| P1-4-5 | 동료 | 부분 완료 | Canonical Balance를 Battle 실행기에 최종 연결 |
| P1-4-6 | 동료 | 대기 | 권장 스펙 기준 전투 난이도 플레이 테스트 |

### P1-5 Settlement 보상

| Task ID | 담당 | 상태 | Codex 작업 |
|---|---|---|---|
| P1-5-1 | 나 | 정책 선행 | 클리어·패배 보상 계산 정책 구현 |
| P1-5-2 | 나 | 정책 선행 | 행성별 accountGold 보상 테이블 작성 |
| P1-5-3 | 나 | 대기 | 관전·이탈·미복귀 자격 판정 구현 |
| P1-5-4 | 나 | 대기 | 영구 재화 지급 Transaction 구현 |
| P1-5-5 | 나 | 부분 완료 | 기존 멱등 저장을 영구 보상 중복 지급 방지까지 확장 |
| P1-5-6 | 동료 | 대기 | 실제 전투 종료 결과와 서버 응답 대조 테스트 |

---

## 3. P2 — 후속 콘텐츠

| Task ID | 담당 | 상태 | Codex 작업 |
|---|---|---|---|
| P2-1-1 | 나 | 정책 선행 | 행성 Stage 해금·입장·보상 서버 구현 |
| P2-1-2 | 동료 | 정책 선행 | 행성별 Map·Waypoint·Boss Scene 구현 |
| P2-2-1 | 나 | 정책 선행 | 일일 콘텐츠 횟수·초기화·보상 서버 구현 |
| P2-2-2 | 동료 | 정책 선행 | 배양 구역 5 Stage Battle 구현 |
| P2-2-3 | 동료 | 정책 선행 | 변이 연구소 5 Stage Battle 구현 |
| P2-3-1 | 나 | 정책 선행 | Quest·Achievement 조건·보상 서버 구현 |
| P2-3-2 | 동료 | 대기 | Battle Quest 진행 이벤트 제공 |
| P2-4-1 | 나 | 정책 선행 | 무한 Wave 시즌·랭킹·구간 보상 서버 구현 |
| P2-4-2 | 동료 | 정책 선행 | 무한 Wave 전투 모드 구현 |
| P2-4-3 | 동료 | 정책 선행 | 무한 Wave 난이도 증가 공식 구현 |
| P2-5-1 | 나 | 부분 완료 | Breeding Unity UI·API 연결 |
| P2-6-1 | 나 | 정책 선행 | Shop·스킨·편의 상품 서버·UI 구현 |
| P2-6-2 | 동료 | 대기 | 스킨·Projectile·처치 Effect 적용 |

---

## 4. 권장 실행 순서

### 1차: 계약 고정

1. 나: `P0-1-1`
2. 나: `P0-1-2`
3. 나: `P0-1-3`
4. 나: `P0-1-4`
5. 동료: `P0-1-5`
6. 나: `P0-1-6`

### 2차: 첫 병렬 작업

- 나: `P0-2-1 → P0-2-4`
- 동료: `P0-3-1 → P0-3-6`

첫 통합 목표:

> 두 클라이언트가 같은 방에서 같은 Wave·Monster·Boss를 본다.

### 3차: 규칙과 전투 병렬 작업

- 나: `P0-5 → P0-7`
- 동료: `P0-6 → P0-8`

두 번째 통합 목표:

> 두 플레이어가 Kidnap·Merge하고 같은 Monster를 공격하며 탈락 상태가 일치한다.

### 4차: 복구와 정산

- 나: `P0-9-1~4, P0-9-7, P0-10-3, P0-10-5~7`
- 동료: `P0-9-5~6, P0-9-8, P0-10-1~2, P0-10-4`

최종 P0 통합 목표:

> 2인 입장부터 80 Wave 종료, 재접속, Settlement 저장까지 한 흐름으로 동작한다.

---

## 5. Codex 작업 요청 템플릿

### 나(User/System)용

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

### 동료(Battle)용

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
대기 → 부분 완료 → 완료
```

완료로 변경할 조건:

1. 담당 도메인 컴파일 성공
2. 관련 자동 테스트 성공
3. Shared 변경이면 양쪽 컴파일 성공
4. Unity 작업이면 Missing Script/Reference 확인
5. 후속 Task가 사용할 공개 API 또는 인수인계 기록 작성

Task 상태만 바꾸는 커밋은 구현 커밋에 함께 포함할 수 있다.
