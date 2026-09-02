# 운영 DB Migration 정책

## 현재 상태

현재 Spring 설정은 H2 in-memory와 `hibernate.ddl-auto=create-drop`만 사용한다. MySQL/PostgreSQL 운영 Driver와 Flyway/Liquibase는 아직 연결되지 않았으므로 이 문서는 production DB 도입 시 적용할 schema 변경 순서를 고정한다. Hibernate 자동 DDL을 운영 Migration으로 사용하지 않는다.

## P2-3 Quest Settlement Migration 순서

운영 배포는 애플리케이션보다 호환 가능한 schema를 먼저 확장하는 expand-first 순서를 따른다.

1. `battle_settlements.session_source VARCHAR(32) NULL`을 추가한다.
2. 기존 행의 source는 `PRODUCTION`으로 추정하지 않는다. 기존 행은 `NULL`로 유지하고 영구 Quest 진행 대상에서 제외한다.
3. `quest_progresses`를 생성한다.
   - PK `id`
   - FK `user_id -> users.id`
   - `quest_condition_id VARCHAR(128) NOT NULL`
   - `progress BIGINT NOT NULL`
   - UNIQUE `(user_id, quest_condition_id)`
4. `quest_settlement_applications`를 생성한다.
   - PK `id`
   - FK `battle_settlement_id -> battle_settlements.id`
   - FK `user_id -> users.id`
   - `quest_condition_id VARCHAR(128) NOT NULL`
   - `applied_amount BIGINT NOT NULL`
   - `applied_at TIMESTAMP NOT NULL`
   - UNIQUE `(battle_settlement_id, user_id, quest_condition_id)`
5. FK 열과 조회 열에 index를 생성한다.
   - `quest_progresses(user_id)`
   - `quest_settlement_applications(battle_settlement_id)`
   - `quest_settlement_applications(user_id)`
6. 중복 행과 orphan FK가 0건인지 사전 검사한 뒤 unique/FK를 활성화한다.
7. 위 schema 확장이 완료된 뒤 신규 애플리케이션을 배포한다. `ddl-auto=validate` 환경에서도 애플리케이션 기동 전에 Quest 테이블과 제약이 존재해야 한다.
8. 모든 새 Settlement가 `PRODUCTION`, `LOCAL_DEVELOPMENT`, `VALIDATION_FIXTURE` 중 하나를 기록하는지와 production Quest 처리가 정상인지 관측한다.
9. 배포 rollback 시 새 테이블을 즉시 삭제하지 않는다. 구버전 애플리케이션이 새 열/테이블을 무시하도록 먼저 rollback하고, 데이터 보존 여부를 별도로 승인받는다.

## Non-null 전환 조건

모든 지원 환경에서 legacy Settlement 보존·백업 정책과 production JWT/matchmaking Adapter가 확정되기 전에는 `session_source`를 non-null로 바꾸지 않는다. 전환 시에도 기존 null 행은 Quest 제외 상태를 유지할 별도 archive/backfill 정책을 먼저 정한다.
