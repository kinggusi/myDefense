# Wak-jeo Defense Balance Data

`balance/source/balance-data.xlsx`가 Balance 데이터의 canonical source입니다. 변환된 JSON과
`balance-manifest.json`은 파생 산출물이므로 직접 수정하지 않습니다.

## 변환과 검증

JDK 17 이상에서 서버 디렉터리를 기준으로 실행합니다.

```powershell
cd server
.\gradlew balanceToolTest
.\gradlew convertBalance
.\gradlew test
```

`convertBalance`는 Excel 전체를 검증한 뒤 canonical JSON과 manifest를 한 묶음으로 교체합니다.
검증이나 파일 생성이 실패하면 기존 정상 generated 파일과 manifest를 유지합니다.

## Excel 시트

기존 영구 계정 Balance:

- `GameReward`: 전투 보상 기본값
- `AlienSpec`: Alien 48종의 기본 능력치와 출시 상태
- `ShopProduct`: Gacha 상품
- `GachaPool`: 등급별 Gacha 확률과 Alien Pool
- `AlienUpgradeCost`: Lv.1→50 강화 비용 49행
- `AlienLevelStat`: 레벨별 능력치 배율 50행

전투 공통 계약:

- `MonsterSpec`: Monster ID, 타입, 기본 HP, 이동 속도, 처치 Gold
- `WaveSpec`: 모드별 Wave 순서, HP 배율, 간격, Boss 여부와 Spawn Group
- `WaveSpawn`: Spawn Group별 Monster 구성, 수량과 `EACH_FIELD`/`BOSS_SHARED` Lane 정책
- `FieldLimitBalance`: 필드별 생존 Monster 한도와 UI 경고 구간
- `SummonBalance`: Kidnap 비용 증가 및 결과 Pool 식별자
- `MergeRule`: 등급별 Merge 재료 수와 결과 방식
- `MythicChoiceBalance`: LEGEND Merge의 MYTHIC 후보·Reroll·제한 시간 정책

로비 신화 교배 계약:

- `MythicBreedingConfig`: 24시간, 슬롯 해금, 중복 조각, 10분당 Diamond 100 가속 비용
- `MythicBreedingResult`: 신화 20종의 일반/교배 전용 획득 구분
- `MythicBreedingRecipe`: 무순서 부모 조합 190개의 일반 후보 5종과 확률 가중치
- `BreedingCombinationPublic`: 공개용 조합표이며 서버 JSON에는 포함하지 않음

숫자는 Excel `NUMERIC`, 플래그는 `BOOLEAN` 타입이어야 합니다. 병합 셀, 중복 헤더, 필수 값 누락,
문자열로 저장된 숫자와 Boolean은 변환 실패 대상입니다.

## 전투 임시 Balance 값

8-1I-C에 추가된 다음 수치는 현재 전투 구현을 옮긴 MVP placeholder이며 플레이테스트 후 조정 대상입니다.

- Monster: Normal HP 30/속도 5/Gold 20, Elite HP 60/속도 4/Gold 40, Wave Boss HP 300/속도 2/Gold 200
- Wave: 10 Wave, Wave당 HP 배율 +0.10, Wave 간격 3초, 10 Wave Boss, Boss 제한 30초
- Spawn: 기본 필드당 10마리, 5·8 Wave는 Normal 8 + Elite 2, Boss는 팀 공용 Lane에 1마리
- Field limit: 최대 100, warning 80, danger 90, 플레이어 2명
- Kidnap: 기본 50 Gold, 성공당 +10, `maxUses=-1`은 무제한
- MYTHIC 선택: 후보 3, 무료 Reroll 1, 유료 Reroll 1, 비용 100, 제한 10초, 리롤 성공 시 제한시간 10초 재설정, 시간 초과 시 첫 후보

## 전투 코드 계약

- 개인 탈락: 각 플레이어 필드의 `aliveMonsterCountPerField >= maxAliveMonsterCountPerField`이면 해당 플레이어만 `ELIMINATED`
- 한 플레이어만 `ELIMINATED`이면 Match는 `RUNNING`을 유지하고, 모든 플레이어가 `ELIMINATED`일 때 최종 `FAILED`
- 필드별 생존 Monster 수는 독립적으로 관리하며 두 필드 수를 합산하지 않음
- Wave Clear: 모든 Spawn 완료 **AND** 해당 Wave 생존 Monster 수가 0
- 일반 Wave는 `lanePolicy=EACH_FIELD`이며 ACTIVE 플레이어의 개인 필드마다 Spawn하고 탈락 플레이어 필드에는 신규 Spawn하지 않음
- `EACH_FIELD`에서 `spawnCountPerField`는 각 개인 필드에 생성할 수량
- Boss Wave는 `lanePolicy=BOSS_SHARED`이며 플레이어 수와 무관하게 팀 공용 Boss Lane에 정확히 1마리만 Spawn
- `BOSS_SHARED`에서 `spawnCountPerField`는 공용 Lane의 총 수량이며 현재 반드시 `1`
- 공용 Boss는 두 플레이어가 모두 공격할 수 있고, 처치 및 제한 시간 초과 결과 처리는 Battle Runtime 책임
- 같은 등급이면서 같은 `alienId`인 Alien 두 개만 Merge 가능하며 `sameSpeciesRequired=true`
- NORMAL→EPIC→UNIQUE→LEGEND는 다음 등급 전체 Pool에서 무작위
- LEGEND Merge는 즉시 결과를 만들지 않고 `MYTHIC_CHOICE` transaction을 시작
- MYTHIC은 최종 등급이며 `DISABLED`
- MYTHIC 후보 Pool은 `AlienSpec.grade == MYTHIC`인 20종 전체에서 자동 파생
- 후보는 서로 달라야 하고 owned/isLocked/specLocked는 후보 확률이나 필터에 사용하지 않음
- owned=true MYTHIC만 Mutation 가능
- 전투 중 Spring Boot API를 반복 호출하지 않음
- Fusion State Authority가 Kidnap, Merge, 후보, Reroll, 선택과 Gold 사용을 검증할 예정

`SummonBalance.resultPoolId`가 참조할 `SummonPool`은 후속 시트입니다. 후보 컬럼은
`resultPoolId`, `entryOrder`, `resultType`, `grade`, `alienId`, `mutationType`, `weight`, `enabled`입니다.
Skill/Mutation 실행 계약도 후속 작업입니다.

Battle Kidnap uses a dedicated `STANDARD_SUMMON_POOL`; it must not reuse the lobby
`STANDARD_ALIEN_POOL`. The pool currently contains NORMAL Aliens only and its entry
weights total 10000. `alien-spec.json` remains the shared Alien catalog.

### Mutation Injector balance

The `MutationSpec`, `MutationConfig`, and `InjectorPool` sheets define the battle
Mutation Injector contract. `BLANK` is valid for random Mythic activation but is
not injector-enabled. Injector results use the seven non-BLANK mutation types with
equal pool weight. The current canonical costs are 300 Gold for initial activation,
600/1200/2400/4800 for the first four rerolls, and 4800 after the fourth reroll;
injector replacement is free. The BalanceTool emits `mutation-spec.json`,
`mutation-config.json`, and `injector-pool.json`, and includes them in the manifest.
Battle Kidnap selection reserves 50 of 10000 weight for an Injector (0.5%); the
remaining 9950 weight selects from `STANDARD_SUMMON_POOL` (99.5%).

### BattleReward balance

The `BattleReward` sheet and `battle-reward.json` define the 80-wave permanent
reward contract. `CONFIG` stores `maxWave=80`, the failure reward base/cap, and
the minimum reward wave. `CHECKPOINT` rows at waves 10, 20, 30, 40, 50, 60,
70, and 80 grant the configured Gold and Universal Piece once per map and user.
`MAP_FIRST_CLEAR` rows grant the planet's first Wave-80 Diamond reward once.
The settlement server uses `finalWave` as the highest fully cleared wave; a
failure during Wave 70 therefore submits 69, while a failure after clearing
Wave 70 submits 70. Re-clears receive repeatable victory/failure Gold only.

## Generated JSON

기존 6종:

- `game-reward.json`
- `alien-upgrade-cost.json`
- `alien-level-stat.json`
- `alien-spec.json`
- `shop-products.json`
- `gacha-pools.json`
- `summon-pools.json`

전투 계약 7종:

- `monster-spec.json`
- `wave-spec.json`
- `wave-spawn.json`
- `field-limit.json`
- `summon-balance.json`
- `merge-rules.json`
- `mythic-choice-balance.json`
- `battle-reward.json`

로비 신화 교배 2종:

- `mythic-breeding-config.json`
- `mythic-breeding-results.json` (`results`와 `recipes` 포함)

전투 계약 JSON은 Unity parser 호환성을 위해 배열을 직접 root로 사용하지 않고 wrapper object를 사용합니다.
모든 JSON은 UTF-8(BOM 없음), LF, 결정적 파일명·행 순서와 기존 pretty-print 규칙을 사용합니다.

## Manifest 규칙

`balance-manifest.json`은 모든 canonical JSON의 파일명, byte 크기, SHA-256과 전체 `contentHash`를 기록합니다.
manifest 자신은 `files`에서 제외하고 timestamp, OS, 사용자나 빌드 환경 정보는 포함하지 않습니다.
서버는 시작할 때 manifest와 실제 파일을 검증한 뒤에만 Registry를 초기화합니다.

## 서버와 Unity 공유

현재 canonical 산출물 위치는 `server/src/main/resources/balance/generated`입니다. 8-1I-D에서 검증된
Gradle sync task가 manifest와 generated 파일을 `Client/Assets/StreamingAssets/Balance/generated`로
byte-for-byte 복사합니다.
