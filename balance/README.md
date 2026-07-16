# Wak-jeo Defense Balance Data

`balance/source/balance-data.xlsx`가 밸런스 데이터의 canonical source입니다. 변환된 JSON과
`balance-manifest.json`은 파생 산출물이며 직접 수정하지 않습니다.

## 변환

JDK 17 이상에서 서버 디렉터리 기준으로 실행합니다.

```powershell
cd server
.\gradlew balanceToolTest
.\gradlew convertBalance
.\gradlew test
```

`convertBalance`는 Excel을 엄격하게 검증한 뒤 아래 파일을 생성합니다.

- `game-reward.json`
- `alien-upgrade-cost.json`
- `alien-level-stat.json`
- `alien-spec.json`
- `shop-products.json`
- `gacha-pools.json`
- `balance-manifest.json`

## Excel 시트

- `GameReward`: 전투 보상 기본값
- `AlienSpec`: Alien 48종의 기본 능력치와 출시 상태
- `ShopProduct`: Gacha 상품
- `GachaPool`: 등급별 확률과 Alien 풀
- `AlienUpgradeCost`: 1→50 강화 비용 49행
- `AlienLevelStat`: 레벨별 능력치 배율 50행

숫자는 Excel `NUMERIC`, 플래그는 `BOOLEAN` 타입이어야 합니다. 병합 셀, 중복 헤더,
필수 값 누락, 텍스트로 저장된 숫자는 변환 실패 대상입니다.

레거시 `AlienUpgrade` 시트는 `AlienUpgradeCost`와 중복되어 제거했습니다. 범용 key/value
`Config` 시트도 제거했습니다. 유일한 값이었던 `maxLevel`은 이미 `AlienLevelStat`의 최대
레벨에서 도출되므로 별도 원천을 유지하지 않습니다. 향후 전역 설정은 책임이 명확한 전용
시트로 추가합니다.

## Manifest 규칙

`balance-manifest.json`은 서버와 향후 Unity 복사본이 동일한 스냅샷인지 확인하는 무결성
계약입니다.

- `schemaVersion`: 현재 `1`
- `files`: manifest 자신을 제외한 generated JSON의 파일명, byte 기반 SHA-256, byte 크기
- 파일명은 사전순이며 경로가 아닌 안전한 basename만 허용
- `contentHash`: 정렬된 각 항목을 `name + NUL + sha256 + NUL + size + LF`로 이어 붙인
  UTF-8 바이트의 SHA-256
- `balanceVersion`: `schemaVersion-contentHash앞16자리`
- timestamp, 빌드 시각, OS, 사용자 정보는 포함하지 않음

모든 JSON 생성과 교체가 끝난 뒤 manifest를 임시 파일에 기록하고 atomic replace를
시도합니다. JSON 생성·검증 실패 또는 필수 JSON 누락 시 manifest는 갱신하지 않습니다.
서버는 시작 시 manifest와 실제 6개 JSON의 파일명, 크기, SHA-256, `contentHash`를 모두
검증한 후에만 기존 Balance Registry를 초기화합니다.

## 서버와 Unity 공유 원칙

현재 canonical 산출물은 `server/src/main/resources/balance/generated`입니다. Unity는 서버
프로젝트 밖 파일을 런타임에 직접 참조하면 빌드 재현성이 깨지므로, 향후 전용 Gradle sync
task가 manifest를 포함한 전체 generated 디렉터리를
`Client/Assets/StreamingAssets/Balance/generated`로 byte-for-byte 복사하는 방식을 권장합니다.
복사본은 파생 산출물이며 수동 수정하지 않고 CI에서 canonical과 전체 diff를 검사합니다.
이번 단계에서는 Unity 파일이나 복사 task를 추가하지 않습니다.

전투 입장 시에는 `balanceVersion`과 `contentHash`를 GameSession 스냅샷에 한 번 고정하고,
전투 중 Spring Boot 반복 조회를 하지 않는 구조를 사용합니다. 이번 단계에서는 전투/API를
변경하지 않습니다.

## CI 실패 조건

`.github/workflows/balance-sync.yml`은 `balanceToolTest → convertBalance → generated 전체 diff
→ server test` 순서로 실행합니다. manifest가 generated 디렉터리에 있으므로 별도 예외 없이
diff 검사에 포함됩니다. Excel과 파생 JSON/manifest가 다르거나 manifest 무결성 검증이
실패하면 CI도 실패합니다.
