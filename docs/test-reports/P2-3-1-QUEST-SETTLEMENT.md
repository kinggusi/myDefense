# P2-3-1 Quest Settlement 기반 구현

## 범위

- 서버 권위 `SessionSource`: `PRODUCTION`, `LOCAL_DEVELOPMENT`, `VALIDATION_FIXTURE`
- Settlement에 Session Source 영속
- production Settlement만 Quest 영구 진행 반영
- `(settlementId, userId, questConditionId)` 정확히 한 번 적용 장부
- 참가, 승리, 행성 승리, 완료 Wave, Kill, Support Kill, Boss Kill 사실 누적
- Shared `BattleSessionSnapshot.mapId`, schema v3, Unity/Spring 공용 canonical JSON fixture

## 신뢰 및 집계 규칙

- Session Source는 roster Adapter가 서버 내부에서 정하며 요청 DTO에는 노출하지 않는다.
- pre-SessionSource legacy Settlement의 null source는 production으로 추정하지 않고 영구 Quest에서 제외한다. 운영 schema 전환 순서는 `docs/DATABASE_MIGRATION_POLICY.md`를 따른다.
- FAILED partial Kill과 Support Kill은 Spring이 검증해 저장한 `BattlePlayerSettlement` 총계에 이미 포함되므로 `partialWaveKills`를 별도로 재가산하지 않는다.
- `abandoned=true` 참가자는 영구 Quest 진행에서 제외한다.
- local/dev와 P1VAL Fixture는 정산 동작 검증만 가능하고 Quest 진행과 적용 장부를 만들지 않는다.
- 현재 구현은 Quest 사실 카운터 기반이다. 일일/주간 Quest 조건 정의, 목표치, 보상, 초기화 주기, 조회/수령 API와 UI는 후속 범위다.

## 자동 검증

- Quest/SessionSource/Snapshot 대상 서버 테스트: PASS
- Quest 동시 재처리: 조건별 적용 장부 1건, 누적 무중복 PASS
- Settlement 핵심 회귀: PASS
- 서버 전체: 357/357 PASS
- BalanceTool: 77/77 PASS
- Unity Shared 계약: 21/21 PASS
- Unity 전체 EditMode: 496/496 PASS
- Unity 컴파일 오류: 0

## 남은 의존성

- reconnect Snapshot Builder는 authoritative Session `mapId`를 투영하고 Shared Validator는 누락을 거부한다. 실제 재접속 소비 경로의 Snapshot/Session mapId 불일치 Smoke는 Battle 후속 검증으로 유지한다.
- production JWT principal + matchmaking trusted roster Adapter
- Quest 정의/보상/초기화/조회 API 및 Lobby UI
- production source Host/Client Quest Settlement E2E
