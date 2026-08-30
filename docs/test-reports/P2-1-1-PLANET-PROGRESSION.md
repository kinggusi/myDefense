# P2-1-1 행성 진행도·입장 서버 검증

## 구현 계약

- 신규 계정은 canonical 첫 행성 `NEPTUNE`만 기본 해금한다.
- trusted 2인 roster의 두 사용자 모두 선택 행성을 해금해야 입장할 수 있다.
- roster 확정 시 두 사용자에게 Heart 1개를 원자적으로 차감한다.
- 동일 Session·행성·slot roster 재요청은 멱등 처리한다.
- 같은 Session을 다른 행성 또는 roster에 재사용하면 `BATTLE_ENTRY_CONFLICT`로 거부한다.
- `MATCHMAKING_FAILED`, `SESSION_FAILED`, `SERVER_ABORTED`, 내부 등록 실패만 trusted local/dev 경로에서 반환할 수 있다.
- Settlement가 존재하거나 입장이 완료된 뒤에는 Heart 반환을 거부한다.
- `ACCEPTED VICTORY + finalWave 80`은 이탈하지 않은 참가자에게 다음 canonical 행성을 멱등 해금한다.

## 영속 모델

- `user_planet_unlocks`: `UNIQUE(user_id, map_id)`
- `battle_entry_reservations`: `UNIQUE(battle_session_id)`
- 입장 예약은 slot 1/2 사용자, 행성, Heart 비용, `CHARGED/COMPLETED/REFUNDED`, 반환 사유를 저장한다.

## 자동 검증

- `BattlePlanetEntryIntegrationTest`
  - 기본 해금
  - 양 참가자 해금 조건
  - Heart 원자 차감과 부족 롤백
  - 동일 요청 재시도
  - Session 충돌
  - 반환과 중복 반환
  - 완료 후 반환 차단
  - 실제 병렬 동일 요청
  - 서로 다른 roster의 동일 Session 병렬 unique 충돌 도메인 변환
  - 다음 행성 해금과 태양 종단
- `LocalBattleSessionRosterControllerIntegrationTest`
  - HTTP roster 등록·Heart 차감
  - trusted 반환 HTTP 계약
  - roster 게시 실패 시 Heart 보상 반환과 부분 dev 계정 생성 방지
  - 행성 진행도 조회 JSON
- `BattleSettlementEndToEndIntegrationTest`
  - Wave 80 승리 후 두 사용자 천왕성 해금
  - 입장 예약 `COMPLETED` 전환
  - 예약 부재·행성·slot 참가자 불일치 fail-closed
  - 동시 완료 예약의 기존 Settlement 멱등 복구

## 최종 결과

- `compileJava`: PASS
- 서버 전체 테스트: **340/340 PASS**
- `balanceToolTest`: PASS
- `git diff --check`: PASS
- 독립 읽기 전용 리뷰: **P0/P1 잔여 없음, 승인**

비차단 후속 항목은 production JWT/matchmaking Adapter가 roster 게시 전
`BattlePlanetEntryService.reserve`를 호출하는 운영 E2E 검증이다.
