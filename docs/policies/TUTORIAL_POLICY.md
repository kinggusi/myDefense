# 튜토리얼 정책 — P2-7

> 기록일: 2026-10-15
> 상태: User/System 1차 구현 / Battle 연습 Adapter 연결·통합 검증 대기
> 담당: User/System(jjangash), 연습 전투는 Battle(kinggusi) 협의
> 사용자 후속 승인으로 정책 기록에 이어 실제 진행 API·영속 보상·로비 안내 UI를 구현했다. 연습 전투 Adapter는 아직 없으므로 전체 튜토리얼 완주 구현 완료를 뜻하지 않는다.

## 1. 승인된 기본 흐름

1. 짧은 연습 전투에서 Kidnap → 빈칸 이동 → 동일 종·동일 등급 Merge를 안내한다.
2. 로비로 돌아와 내 유닛과 강화 기능을 안내한다.
3. 튜토리얼 완료 보상으로 Mythic 1·2(canonical Alien ID 29·30)를 영구 해금한다.
4. 신화 교배 진입 위치를 안내한 뒤 자유 플레이로 전환한다.

중단 후 이어서 진행할 수 있어야 한다. 완료 보상은 계정당 한 번만 지급하며, 이미 보유한 유닛의 레벨·조각은 초기화하거나 덮어쓰지 않는다. 재요청·동시 요청·서버 재시작으로 보상을 중복 지급해서는 안 된다.

추가 승인 사항:

- 연습 전투는 무료이며 일반 전투 보상과 Quest 진행에서 분리한다.
- 강화는 실제 재료 소비 없이 안내만 한다.
- `나중에 하기`를 제공하되 실제 튜토리얼 완료 조건을 충족해야 신화 1·2 보상을 지급한다.
- 기존 계정은 튜토리얼에 선택 진입하며, 이미 보유한 신화의 레벨·조각은 보존한다.

단계별 완료 판정과 중단 후 복원 단위 등 아래 미정 항목은 확정한 뒤 구현한다. 교배 시작은 승인된 필수 행동으로 간주하지 않는다.

## 2. 구현 전 확정할 세부 정책

아래는 남은 결정 또는 첫 구현에서 선택한 기본값을 구분한 표다. Adapter 관련 미정은 임의로 완료 처리하지 않는다.

| 항목 | 미정 사항 | 권장안 |
|---|---|---|
| 재진입 UX | 1차 구현 기본값 | 로비 전투 탭의 튜토리얼 버튼으로 신규/기존 계정 모두 선택 진입. 진행을 보유 목록에서 역산하지 않음. 신규 계정 자동 강제 진입은 구현하지 않음 |
| 연습 전투 세부 | 연습 Gold·유닛 초기값, 업적 처리 | 무료·일반 보상/Quest 분리 원칙을 유지하고 업적 진행도 분리하는 안을 검토 |
| 교육용 유닛 | 동일 종 2개 확보 방식과 연습 전투 구성 | 일반 Kidnap 확률을 바꾸지 않는 전용 교육 설정을 Battle 담당과 합의 |
| 재개 단위 | 완료 단계 저장 구현 / 연습 보드 정밀 복원 미정 | 서버 단계·활성 runId를 보존. 미완료 보드 복원/재실행은 Battle Adapter 합의 후 결정 |
| 완료 증거 | 각 단계의 서버 검증 기준과 Battle 전달 계약 | 클라이언트의 `completed=true` 주장만으로 보상을 지급하지 않고 계정·연습 실행 문맥·순서를 검증 |
| 보상 초기 상태 | 승인된 실제 구현 선택 | 미보유 29·30은 기존 `UserAlien` 생성기(Lv.1·조각 0) 재사용. 기보유는 레벨·조각 보존, 추가 대체 보상 없음. 개발 preview를 완료 증거로 쓰지 않음 |
| 보상과 마지막 안내 | 1차 구현 기본값 | 보상 지급 상태와 안내 완료 상태를 분리하여 교배 안내만 재개하고 보상은 다시 지급하지 않음 |

## 3. 현재 구현과 개발용 미리보기의 구분

- 현재 `StarterAlienCollectionService`는 기본 28종을 보완하며, local/dev에서 명시적으로 활성화한 `mydefense.collection.tutorial-mythic-preview-enabled` 설정으로만 29·30을 테스트 지급한다.
- 미리보기 지급은 튜토리얼 진행·완료 증거 또는 운영 보상 장부가 아니다. 이미 미리보기로 지급한 보유 정보를 자동 삭제하지 않는다.
- 현재 User/System 1차에는 JWT 보호 진행 API, `tutorial_progress`·`tutorial_receipt` 영속 장부, 단계 안내 Controller/View와 선택 진입 버튼을 추가했다. 기존 preview 지급은 그대로 분리되어 있다.
- 실제 운영 로그인/매칭 작업의 실오픈 준비 시점 유예는 유지한다. 기존 게스트/JWT와 계정 보호, 미구성 운영 Adapter의 fail-closed는 변경하지 않는다.

## 4. 담당 경계와 후속 계약

| 영역 | 후속 책임 |
|---|---|
| User/System | 인증된 계정의 진행 저장·재개, 단계 검증, 완료 보상 트랜잭션·멱등 장부, 로비 안내 UI |
| Battle | 연습 전투 Scene/실행 문맥·연출·입력 안내 연계, 권위적 연습 결과 제공. 구현 범위와 소유권을 합의한 뒤 착수 |
| Shared | 필요한 진행/완료 증거 DTO·interface의 버전·필드·권한·재전송 계약을 양쪽 담당과 합의 |

기존 `AccountAccess.requirePrincipal()`, 계정 잠금과 `UserAlien` 중복 방지 제약을 재사용하는 방향으로 검토한다. 튜토리얼 완료 상태를 영구 보유 목록에서 역산하지 않는다.

이번 구현은 User/System 전용 API/주입 interface만 추가했다. Shared 공용 DTO, SessionSource·mapId, Balance 수치나 Battle 코드는 수정하지 않는다. 연습 전투 증거 계약이 준비되지 않은 상태에서 임의 완료 버튼으로 운영 지급을 우회하지 않는다.

참고: 과거 문서의 Mythic 해금 풀/후보 3종 표현과 현재 구현 사이에 별도 정합성 확인이 필요할 수 있다. 신화 2종 보상 정책을 이유로 기존 전투 후보 정책·풀을 이번 작업에서 변경하지 않는다.

## 5. 작업 순서와 완료 조건

1. **P2-7-1 — 부분 완료:** 기본 흐름과 1차 기술 기본값 기록. 연습 완료 증거·보드 복원 등 Battle 계약은 후속 합의한다.
2. **P2-7-2 — 부분 완료:** 진행 영속화·단계 검증·멱등 보상 구현. 신뢰 완료 Verifier 실연결 전에는 연습 완료와 보상 진입이 차단된다.
3. **P2-7-3 — 부분 완료:** 선택 진입·안내·닫기·조회 복구·보상·교배 위치 UI 구현. 실제 Battle Launcher 연결과 전체 사용자 여정 검증은 남는다.
4. 구현 → 자동 테스트 → 실제 화면 조작 → 문제 수정·재검증 → 독립 리뷰 → AI 사용자 시나리오 확인 → 최종 사람 인수 순으로 검증한다. 발견된 UI 오류나 합의와 다른 동작은 보고 후 승인된 범위로 수정한다.

## 6. 후속 검증 계획

- 서버: 무인증/타계정 거절, 단계 건너뛰기, 중복·동시 완료, 보상 중간 실패 롤백, 재시작 후 재개, 29·30 중 일부/전체 기보유 보존, 개발 preview와 운영 장부 독립.
- Unity: 첫 진입·중단·재진입·API 오류 후 복구, 단계별 입력 범위, 다양한 화면비, 보상 후 교배 안내 재개, 안내만으로 강화·교배가 실행되지 않는지 확인.
- Battle/Shared: 교육 설정의 일반 매치 유입 차단, 완료 증거의 세션/계정 불일치·재전송·위조 거부, 합의된 자원·정산·Quest 분리 확인.
- Task별 자동 테스트 또는 격리 Fixture를 Feature Test Hub에 등록하고 테스트 Scene은 production Build에서 제외한다.
- `docs/04_TEST_STRATEGY.md`에 따른 사람 인수는 사용자의 최종 통합테스트 시점에 별도로 남긴다. UI Fixture 상태 전환은 실제 서버 연습 완료나 보상 지급 증거가 아니다.

## 7. User/System 1차 구현 계약

- `GET /api/tutorial`: 인증된 현재 계정의 진행 상태 조회. `POST /api/tutorial/commands`: `requestId`, `expectedRevision`, `operation`, 필요 시 `practiceRunId` 전송. 사용자 ID와 완료 boolean은 입력으로 받지 않는다.
- 단계: `NOT_STARTED → PRACTICE_PENDING → COLLECTION_GUIDE → UPGRADE_GUIDE → REWARD_READY → BREEDING_GUIDE → COMPLETED`.
- `START`, `DEFER`는 이미 진행한 단계를 되돌리지 않는다. `START_PRACTICE`는 서버가 발급한 계정별 실행 ID를 유지한다. `VERIFY_PRACTICE`는 현재 계정/실행 ID와 일치하는 서버 Verifier 증거만 인정한다.
- `COLLECTION_SEEN`, `UPGRADE_SEEN`은 설명 확인만 기록하며 강화 API를 호출하지 않는다. `CLAIM`은 보상과 지급 기록을 하나의 트랜잭션으로 처리한다. `BREEDING_SEEN`은 위치 안내 확인이며 교배 시작 API를 호출하지 않는다.
- 계정 잠금 아래 요청 식별자와 payload fingerprint를 확인한다. 동일 요청은 최신 진행 상태를 반환하고 재지급/과거 단계 복귀를 하지 않는다. 다른 payload의 식별자 재사용과 stale revision은 409로 거절한다.
- POST 응답 유실 시 같은 요청 재시도 또는 GET으로 복구한다. Unity는 계정·인증 세대·endpoint가 달라진 콜백을 폐기하며 닫힌 안내를 늦은 응답으로 다시 열지 않는다.
- `TutorialPracticeVerifier`와 `ITutorialPracticeLauncher`는 User/System 주입 경계다. 기본 구현은 unavailable/fail-closed. 개발 HTTP 완료 우회는 만들지 않았다. 성공 Verifier는 `src/test`에만 있다.
- Editor-only UI Fixture는 NO HTTP/NO REWARD이며 전용 Scene과 Feature Test Hub에 등록한다. production Build에는 포함하지 않는다.
