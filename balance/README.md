# Wak-jeo Defense Balance Data

게임 내 밸런스 데이터를 관리하는 Excel 원본 및 변환 스크립트입니다.

## 요구 사항
- JDK 17 이상

## 실행 명령
서버 모듈 디렉터리(`server/`)에서 다음 Gradle 태스크를 실행합니다.

```bash
cd server
./gradlew convertBalance
```

이 명령은 `balance/source/balance-data.xlsx`를 읽어 유효성을 검사하고, 성공 시 아래 위치에 JSON 파일을 생성/교체합니다.
- `server/src/main/resources/balance/generated/game-reward.json`
- `server/src/main/resources/balance/generated/alien-upgrade.json`

## Excel 시트 구조 (엄격한 검증)
엑셀의 데이터는 매우 엄격하게 검증됩니다. 기획 오류를 방지하기 위해 다음 규칙을 반드시 지켜야 합니다.

1. **숫자는 반드시 `NUMERIC` 셀이어야 합니다.** (텍스트 형태의 숫자 허용 안 됨)
2. **소수점이나 음수는 허용되지 않습니다.**
3. **빈 셀, 중복 헤더, 병합 셀은 오류를 발생시킵니다.**
4. 숨김 처리된 행도 일반 데이터로 읽힙니다. 적용을 제외하려면 명시적으로 데이터를 지우거나 향후 시스템 확장을 요청하세요.

### Config 시트
- 필수 헤더: `key`, `value`
- 필수 데이터: `maxLevel` (최대 레벨 지정)

### GameReward 시트
- 필수 헤더: `baseRewardGold`, `goldPerWave`, `maxRewardGold`
- 데이터 행: **정확히 1줄**

### AlienUpgrade 시트
- 필수 헤더: `currentLevel`, `requiredPieces`, `requiredGold`, `requiredGrowthCell`
- 데이터 행: `currentLevel` 1부터 `maxLevel-1`까지 연속적으로 작성되어야 합니다.

## 검증 실패 예시
변환 실패 시 기획자가 즉시 수정할 수 있도록 에러 메시지가 출력됩니다.
> `[AlienUpgrade] 12행 'requiredGold' 열: -10 - 0 이상이어야 합니다.`
> `[Config] 2행 'value' 열: 문자열 형태의 숫자는 허용되지 않습니다.`

## 작업 순서 (매우 중요)
밸런스 데이터를 변경할 때는 **반드시 Excel과 변환된 JSON을 함께 커밋**해야 합니다. 서버 런타임은 오직 JSON만 읽습니다.

1. `balance-data.xlsx` 파일 수정
2. `server/` 디렉터리에서 `.\gradlew convertBalance` 실행
3. `git diff`를 통해 변환된 JSON 파일의 변경점 확인
4. `.\gradlew test`를 실행하여 밸런스 변경으로 인해 서버 테스트가 깨지지 않는지 확인
5. `.xlsx` 원본과 `generated/*.json` 파일들을 함께 커밋 및 푸시
