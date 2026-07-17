# Battle Balance Data

Battle 도메인 전용 Excel 원본과 Unity Resources JSON 변환 절차입니다. 기존 `balance/source/balance-data.xlsx`와 서버용 5종 JSON은 이 파이프라인의 입력이나 출력이 아닙니다.

## Source of Truth

- 원본: `balance/source/battle-balance.xlsx`
- 변환 출력: `Client/Assets/Resources/Balance/Battle/*.json`
- 스키마 버전: `1`
- 밸런스 버전: `battle-v1`

```powershell
cd server
.\gradlew convertBattleBalance
.\gradlew balanceToolTest
```

`convertBattleBalance`는 7개 document와 `battle-balance-manifest.json`을 항상 생성합니다. 데이터가 없는 시트도 Header를 유지하며 해당 JSON은 `items: []`로 생성됩니다.

## Initial Migration Content

현재 WaveSpec/WaveSpawnSpec의 Round 1~20 데이터는 기존 로컬 Battle 동작을 JSON 파이프라인으로 이전하고 E2E를 검증하기 위한 smoke 데이터입니다. 최종 밸런스 확정값이 아닙니다.

- Round 10, 20: BOSS / `BOSS_SHARED`
- 나머지 Round: REGULAR / `EACH_ACTIVE_PLAYER_LANE`
- REGULAR: 필드당 10마리, 1초 간격
- BOSS: 1마리, 제한시간 30초
- REGULAR HP 배율: Round 1의 1.0에서 Round마다 0.1 증가
- BOSS HP 배율: 해당 Round의 일반 성장 배율 × 10
- 이동속도 배율: 1.0

Monster ID는 상대 소유 MonsterDefinition의 외부 논리 ID입니다. Converter는 ID가 비어 있지 않은지만 확인하고, 실제 존재·MonsterType·lane-limit 정책은 Unity의 `IMonsterDefinitionProvider`와 TASK 9 Validator가 검증합니다.

## Deterministic Hash Contract

- 각 `contentHash`: `contentHash` 필드를 제외한 고정 필드순 compact canonical payload의 UTF-8 SHA-256
- `bundleHash`: `resourcePath` 오름차순으로 `resourcePath:contentHash\n`을 결합한 UTF-8 SHA-256
- JSON: UTF-8, LF, BOM 없음, locale 비의존 숫자, 생성 시각 없음

같은 Excel을 두 번 변환하면 JSON bytes와 hash가 같아야 합니다.
