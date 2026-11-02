# 운영 DB Migration 정책

## 현재 상태

기본/자동 테스트 설정은 H2 in-memory와 `hibernate.ddl-auto=create-drop`을 사용한다. `local` 프로필은 2026-09-18부터 H2 파일 DB와 `update`로 개발 데이터를 보존하며, 테스트용 local 설정은 메모리 DB에 격리한다. 이는 운영 DB 전환이 아니다. MySQL/PostgreSQL 운영 Driver와 Flyway/Liquibase는 아직 연결되지 않았으므로 이 문서는 production DB 도입 시 적용할 schema 변경 순서를 고정한다. Hibernate 자동 DDL을 운영 Migration으로 사용하지 않는다. 로컬 경로·계정·백업은 `LOCAL_PERSISTENCE_GUIDE.md`를 따른다.

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
5. `quest_cycle_progresses`를 생성한다.
   - PK `id`
   - FK `user_id -> users.id`
   - `quest_id VARCHAR(64) NOT NULL`
   - `cycle_type VARCHAR(16) NOT NULL`
   - `cycle_key VARCHAR(16) NOT NULL`
   - `progress BIGINT NOT NULL`
   - UNIQUE `(user_id, quest_id, cycle_key)`
6. `quest_reward_claims`를 생성한다.
   - PK `id`
   - FK `user_id -> users.id`
   - `reward_key VARCHAR(128) NOT NULL`
   - `request_id VARCHAR(64) NOT NULL`
   - `reward_gold`, `reward_universal_piece`, `reward_diamond` INTEGER NOT NULL
   - `claimed_at TIMESTAMP NOT NULL`
   - UNIQUE `(user_id, reward_key)`
   - UNIQUE `(user_id, request_id)`
7. FK 열과 조회 열에 index를 생성한다.
   - `quest_progresses(user_id)`
   - `quest_settlement_applications(battle_settlement_id)`
   - `quest_settlement_applications(user_id)`
   - `quest_cycle_progresses(user_id, cycle_key)`
   - `quest_reward_claims(user_id, reward_key)`
8. 중복 행과 orphan FK가 0건인지 사전 검사한 뒤 unique/FK를 활성화한다.
9. 위 schema 확장이 완료된 뒤 신규 애플리케이션을 배포한다. `ddl-auto=validate` 환경에서도 애플리케이션 기동 전에 Quest 테이블과 제약이 존재해야 한다.
10. 모든 새 Settlement가 `PRODUCTION`, `LOCAL_DEVELOPMENT`, `VALIDATION_FIXTURE` 중 하나를 기록하는지와 production Quest 처리가 정상인지 관측한다.
11. 배포 rollback 시 새 테이블을 즉시 삭제하지 않는다. 구버전 애플리케이션이 새 열/테이블을 무시하도록 먼저 rollback하고, 데이터 보존 여부를 별도로 승인받는다.

## Achievement 저장 정책

- 영구 Achievement는 별도 진행 테이블을 추가하지 않고 `quest_progresses`의 `quest_condition_id`별 누적값을 조회한다. Quest와 Achievement가 같은 서버 권위 전투 사실을 공유하기 때문이다.
- Achievement 달성 여부는 배포된 Excel Balance의 조건·목표치와 영구 진행값을 결합해 계산한다.
- 보상 수령은 기존 `quest_reward_claims`를 재사용하고 `reward_key`를 `ACHIEVEMENT:<achievementId>` 형식으로 저장한다.
- Achievement 정의가 추가되더라도 DB migration은 필요 없지만, condition ID 변경·삭제는 기존 누적 진행과 보상 장부의 의미를 바꾸므로 운영 데이터 migration 승인 없이 수행하지 않는다.

## Non-null 전환 조건

모든 지원 환경에서 legacy Settlement 보존·백업 정책과 production JWT/matchmaking Adapter가 확정되기 전에는 `session_source`를 non-null로 바꾸지 않는다. 전환 시에도 기존 null 행은 Quest 제외 상태를 유지할 별도 archive/backfill 정책을 먼저 정한다.

## P3-1 클랜 Migration·보존

2026-11-02 서버 1차는 신규 테이블만 추가한다. 실개발 파일 DB를 초기화하지 않으며 검증은 임시 디렉터리의 별도 파일 H2에서 한다. 운영 DB·Migration 실행기는 여전히 미구성이므로 아래는 배포 선행 계약이지 운영 배포 완료가 아니다.

1. 애플리케이션 배포 전에 expand-first로 다음 테이블·제약을 생성한다.
   - `clans`: PK `id`, `name`, `normalized_name` UNIQUE, `description`, `notice`, `join_mode`, `leader_id`, `member_count`, `revision`, `active`, nullable `disbanded_at`.
   - `clan_account_states`: PK `user_id`(계정당 소속 최대 1개), nullable `clan_id`, `role`, `joined_at`, `rejoin_available_at`, `pending_clan_id`. `clan_id`, `pending_clan_id` 조회 인덱스.
   - `clan_command_receipts`: PK `id`, `user_id`, `request_id` UUID, `fingerprint` SHA-256 hex64, `operation`, `processed_at`, UNIQUE `(user_id, request_id)`.
2. 기존 계정은 미가입으로 간주하며 account state는 최초 인증 조회/명령에서 User row 잠금 후 생성한다. 기존 User/Wallet/Battle 테이블의 값을 backfill하지 않는다.
3. 현재 Entity는 ID scalar 참조를 사용하며 애플리케이션이 사용자·클랜 존재를 검증한다. 운영 DB 적용 시 `user_id/leader_id -> users.id`, `clan_id/pending_clan_id -> clans.id` FK와 정원/역할 check constraint를 사전 검토하고 orphan 0건 확인 후 함께 활성화한다. 운영 데이터 정리로 유저/클랜을 물리 삭제하는 기능은 이번에 제공하지 않는다.
4. 해산은 `active=false` soft-delete로 기록한다. 이름 유일 키·성공 영수증·탈퇴/추방 쿨다운은 보존한다. 해산 클랜에 대한 pending 참조는 논리 무효이며 재신청 시 교체 가능하다. 앱 rollback 시 신규 테이블을 삭제하지 않는다.
5. 운영 DB의 collation/Unicode NFKC 대소문자 중복, UTC timestamp, UUID 타입, PESSIMISTIC_WRITE 동작과 deadlock timeout을 검증한다. H2의 자동 DDL/update 결과를 운영 migration으로 재사용하지 않는다.
6. 잠금은 actor/target User ID 오름차순 후 Clan 한 행으로 통일한다. 운영 DB에서 정원 마지막 자리·동일 계정 다중 가입·동일 요청/이름 생성 경합·해산/가입·권한 위임 경쟁을 재검증한다.
