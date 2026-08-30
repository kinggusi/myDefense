# Shared Contract

## Purpose
User/System과 Battle이 함께 사용하는 계약만 둡니다.

## Shared Types
- DamagePayload
- IDamageable
- ITargetProvider
- HitEvent
- PlayerBattleState
- PlayerConnectionState
- MatchState
- GridPosition
- 공통 DTO, Enum, Network contract

## Battle Settlement Contract
- Unity 런타임 누적 장부 `BattleSummary`와 서버 전송 계약 `BattleSettlementSummary`를 분리한다.
- 전송 결과 문자열은 `VICTORY`, `DEFEAT`, `ABORTED`만 허용한다.
- 최상위 필드: `requestId`, `battleSessionId`, `balanceVersion`, `contentHash`, `result`, `finalWave`, `mapId`, `startedAt`, `finishedAt`, `players`, `monsterKills`, `waveSpawnFacts`, `partialWaveKills`, `summaryHash`
- 참가자 필드: `playerId`, `playerSlot`, `eliminated`, `eliminatedWave`, `kills`, `supportKills`, `bossKills`, `initialInGameGold`, `inGameGoldEarned`, `inGameGoldSpent`, `finalInGameGold`, `abandoned`
- Monster 필드: `monsterSpecId`, `totalKills`, `bossKills`, `totalKillGold`
- 미완료 Wave Spawn 사실 필드: `runtimeMonsterId`, `spawnWave`, `spawnGroupId`, `monsterSpecId`, `lanePolicy`, nullable `fieldOwnerPlayerSlot`, `spawnOrder`, `spawnOrdinal`
- 미완료 Wave 처치 필드: Spawn 사실 필드 전체 + `killerPlayerSlot`, nullable `supportPlayerSlot`
- 응답 필드: `battleSessionId`, `status`, `alreadyProcessed`, `rewards`
- 보상 필드: `userId`, `rewardKey`, `rewardType`, `gold`, `universalPiece`, `diamond`
- `eliminatedWave`는 미탈락 시 JSON `null`, 탈락 시 양의 정수다.
- 시간 필드는 ISO-8601 local date-time 문자열로 전송한다.
- Unity `JsonUtility`는 nullable 정수를 지원하지 않으므로 `BattleSettlementSummaryJson`을 사용한다.
- `waveSpawnFacts`는 `DEFEAT`에서 `spawnWave == finalWave + 1`인 미완료 Wave에 State Authority가 실제 Spawn한 모든 Monster를 담고, `partialWaveKills`는 그중 처치된 Monster만 담는다. `VICTORY`와 `ABORTED`에서는 두 배열 모두 비어야 한다.
- `runtimeMonsterId`는 Fusion `ulong` 전체 범위를 보존하기 위해 JSON decimal string으로 전송하며, 0이 아닌 정규 decimal 표현을 사용한다.
- 두 미완료 Wave 장부는 각각 unsigned `runtimeMonsterId` 오름차순으로 정렬한다. 서버는 Runtime ID, canonical Spawn 위치(`spawnGroupId`, `spawnOrder`, `spawnOrdinal`, `fieldOwnerPlayerSlot`) 중복을 모두 거부한다.
- `EACH_FIELD`는 `fieldOwnerPlayerSlot`이 필수이고 해당 Wave에 활성 Lane이어야 한다. `BOSS_SHARED`는 `fieldOwnerPlayerSlot=null`이고 공용 Spawn 수량을 따른다.
- 모든 `partialWaveKills` 항목은 같은 `runtimeMonsterId`의 `waveSpawnFacts` 항목과 Spawn identity가 정확히 일치해야 한다.
- `DEFEAT`의 `eliminatedWave`는 미완료 시도 Wave인 `finalWave + 1`까지 허용한다. 이후 완료 Wave Kill 기대치는 탈락 Wave 다음부터 해당 Lane을 제외한다.
- 완료 Wave의 개인 Kill/Support/Boss 귀속은 Photon State Authority의 집계값을 신뢰한다. 서버는 canonical 팀 총계, Boss 총계, Monster별 killGold와 미완료 Wave Spawn 증거를 검증한다.
- 미완료 Wave 장부의 killer/support/Boss 개인별 건수는 각 `players` 집계 이하이어야 한다. 따라서 partial 증거와 Player 집계가 서로 다른 귀속을 주장할 수 없다.
- Spring은 Fusion의 실제 Spawn 이력을 독립 보유하지 않으므로 `runtimeMonsterId`가 실제 생성됐다는 사실 자체와 개인 귀속은 trusted State Authority를 신뢰한다. 대신 canonical Wave의 허용 Monster/Lane/Spawn row/ordinal 상한과 중복을 검증해 임의 범위를 제한한다.

### Settlement Canonical JSON / Hash
- JSON 필드 순서는 위 최상위 필드 순서를 고정하고 `waveSpawnFacts`, `partialWaveKills`를 `monsterKills` 다음, `summaryHash` 전에 둔다.
- `summaryHash` 계산 시 `summaryHash` 속성 자체를 제외한 정확한 UTF-8 JSON 바이트의 SHA-256 lowercase hex를 사용한다. 빈 문자열 속성을 넣는 구버전 방식은 허용하지 않는다.
- 배열 순서도 hash 입력이다. Player/Monster 기존 정렬과 `waveSpawnFacts`/`partialWaveKills`의 unsigned Runtime ID 정렬을 전송 전에 고정한다.
- Spring은 동일 규칙으로 hash를 재계산하고 canonical payload 불일치 또는 구버전 직렬화 계약을 `BATTLE_SUMMARY_INVALID`로 거부한다. 이 무키 SHA-256은 인증 수단이 아니며, 송신자 신뢰는 JWT와 trusted roster/State Authority 경계에서 별도로 보장해야 한다.
- 이 필드 추가는 Unity와 Spring의 동시 배포가 필요한 전송 Breaking Change다. 구버전 Client는 새 서버 Settlement를 전송할 수 없다.
- 서버는 신규 hash 검증 후 기존 Settlement 멱등 조회를 수행하므로, 구버전 canonical hash로 이미 승인된 in-flight retry는 신규 서버에서 거부된다. 배포 시 Unity/Spring을 동시에 전환하고 구버전 pending Settlement retry를 폐기하거나 사전 배출한다.

## Trusted Battle Roster Registration
- Settlement 전 Spring은 `battleSessionId`, `mapId`, Balance version/hash, 정확한 두 참가자와 slot을 신뢰 roster로 등록받아야 한다.
- Unity Settlement는 `IBattleSessionRosterRegistration`에만 의존한다.
- local/dev에서는 Fusion State Authority가 `/api/dev/battle/session-rosters`로 등록하며, 이 경로는 local/dev Profile과 loopback 요청에서만 활성화한다.
- `dev-*` 사용자의 자동 생성은 로컬 2인 E2E 검증 전용이다. 운영 계정을 이 방식으로 만들지 않는다.
- `FUTURE_AUTH_REPLACEMENT`: 운영 인증 도입 시 이 인터페이스의 구현만 JWT principal과 matchmaking 결과를 검증하는 Adapter로 교체한다. Settlement DTO, Summary hash, 보상 Transaction은 변경하지 않는다.
- production에서 JWT Adapter가 없으면 roster 등록과 영구 보상은 안전하게 거부되어야 한다.

## Battle State
- PlayerBattleState: `ACTIVE → ELIMINATED → SPECTATING`
- PlayerConnectionState: `CONNECTED ↔ DISCONNECTED`
- MatchState: `RUNNING → CLEARED` 또는 `RUNNING → FAILED`
- `ELIMINATED`는 탈락 확정, `SPECTATING`은 조작 차단 후 관전 상태다.
- `ELIMINATED`와 `SPECTATING`은 모두 전투 조작 및 해당 필드 신규 Monster Spawn이 불가능하다.
- 일시적인 연결 종료는 PlayerBattleState나 MatchState를 변경하지 않는다.
- 명시적 이탈과 매치 종료 미복귀에 따른 보상 자격은 Settlement 계약에서 별도로 판정한다.

## Grid
- 4 x 6
- 24 slots
- Alien과 Mutation Injector 모두 1칸
- Kidnap은 첫 빈칸 순차 배치

## Merge
- 동일 등급 동일 종만 가능
- Normal~Legendary 결과는 다음 등급 전체 풀 랜덤
- Legendary Merge는 현재 플레이어가 해금한 Mythic 풀에서 서로 다른 후보 3종 제시
- Legendary Merge 후보는 최대 3회 리롤 후 1종 선택
- 결과는 드롭 대상 위치에 생성

## Mutation
- PendingMutationType: 모든 등급에서 보유 가능
- ActiveMutationType: Mythic에서만 활성화
- A + B DNA는 둘 중 하나를 각각 50% 확률로 계승
- DNA를 계승한 Mythic은 생성 즉시 해당 Mutation을 무료로 자동 활성화
- DNA가 없는 Mythic은 개인 인게임 골드를 지불해 랜덤 Mutation 활성화
- 최초 랜덤 Mutation 비용은 300 인게임 골드
- 재변이 비용은 `600 → 1,200 → 2,400 → 4,800`, 이후 4,800 고정
- 재변이 시 현재 Mutation을 후보에서 제외하여 반드시 다른 Mutation 획득
- Mutation된 Mythic에 Injector를 사용하면 기존 Mutation을 Injector Mutation으로 즉시 무료 교체

## Damage
```text
StatCalculator
→ DamagePayload
→ Battle hit/collision
→ IDamageable.ApplyDamage(payload)
```

## Cooperative In-Game Gold
- 몬스터 처치 위치나 마지막 공격자와 무관하게 양쪽 플레이어에게 동일한 처치 골드 100%를 각각 지급
- Kill/Support Kill은 골드 분배가 아닌 통계와 Settlement 기록에만 사용
- 탈락 관전 및 일시적인 연결 종료 중에도 매치가 진행되는 동안 골드 장부 유지
- 재접속 시 누적 인게임 골드와 전투 상태 복구
- 명시적 나가기 또는 매치 종료 시점까지 미복귀한 경우에만 최종 이탈 판정

## Data
```text
CSV/Excel
→ Common JSON
├→ Unity ScriptableObject
└→ Spring Boot
```

## Change Rule
Shared 계약 변경 시:
1. 양쪽 담당자에게 알림
2. Breaking Change 여부 표시
3. 사용처 검색
4. 양쪽 컴파일 확인
5. PR에 마이그레이션 방법 작성
