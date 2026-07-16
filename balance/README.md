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

`convertBalance`는 Excel 전체를 검증한 뒤 13개 JSON과 manifest를 한 묶음으로 교체합니다.
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

숫자는 Excel `NUMERIC`, 플래그는 `BOOLEAN` 타입이어야 합니다. 병합 셀, 중복 헤더, 필수 값 누락,
문자열로 저장된 숫자와 Boolean은 변환 실패 대상입니다.

## 전투 임시 Balance 값

8-1I-C에 추가된 다음 수치는 현재 전투 구현을 옮긴 MVP placeholder이며 플레이테스트 후 조정 대상입니다.

- Monster: Normal HP 30/속도 5/Gold 20, Elite HP 60/속도 4/Gold 40, Wave Boss HP 300/속도 2/Gold 200
- Wave: 10 Wave, Wave당 HP 배율 +0.10, Wave 간격 3초, 10 Wave Boss, Boss 제한 30초
- Spawn: 기본 필드당 10마리, 5·8 Wave는 Normal 8 + Elite 2, Boss는 팀 공용 Lane에 1마리
- Field limit: 최대 100, warning 80, danger 90, 플레이어 2명
- Kidnap: 기본 50 Gold, 성공당 +10, `maxUses=-1`은 무제한
- MYTHIC 선택: 후보 3, 무료 Reroll 1, 유료 Reroll 1, 비용 100, 제한 8초, 시간 초과 시 첫 후보

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

## Generated JSON

기존 6종:

- `game-reward.json`
- `alien-upgrade-cost.json`
- `alien-level-stat.json`
- `alien-spec.json`
- `shop-products.json`
- `gacha-pools.json`

전투 계약 7종:

- `monster-spec.json`
- `wave-spec.json`
- `wave-spawn.json`
- `field-limit.json`
- `summon-balance.json`
- `merge-rules.json`
- `mythic-choice-balance.json`

전투 계약 JSON은 Unity parser 호환성을 위해 배열을 직접 root로 사용하지 않고 wrapper object를 사용합니다.
모든 JSON은 UTF-8(BOM 없음), LF, 결정적 파일명·행 순서와 기존 pretty-print 규칙을 사용합니다.

## Manifest 규칙

`balance-manifest.json`은 위 13개 JSON의 파일명, byte 크기, SHA-256과 전체 `contentHash`를 기록합니다.
manifest 자신은 `files`에서 제외하고 timestamp, OS, 사용자나 빌드 환경 정보는 포함하지 않습니다.
서버는 시작할 때 manifest와 실제 파일을 검증한 뒤에만 Registry를 초기화합니다.

## 서버와 Unity 공유

현재 canonical 산출물 위치는 `server/src/main/resources/balance/generated`입니다. 8-1I-D에서 검증된
Gradle sync task가 manifest와 generated 파일을 `Client/Assets/StreamingAssets/Balance/generated`로
byte-for-byte 복사할 예정입니다. 이번 단계에서는 Unity 파일이나 sync task를 추가하지 않습니다.
