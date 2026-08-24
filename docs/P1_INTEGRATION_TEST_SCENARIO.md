# P1 통합 테스트 시나리오

## 목적

P1 Mutation·공명·행성 Balance·Settlement를 실제 Fusion Host/Standalone Client에서 한 번에 검증한다. 자동 테스트가 통과한 뒤 수행하는 사용자 검증 게이트이며, 통과 전 Unity UI·2인 상호작용 Task는 `검증 대기`로 유지한다.

## 준비

1. `SPRING_PROFILES_ACTIVE=local`을 명시하고 Spring Boot를 최신 코드와 canonical Balance로 `localhost:8080`에서 실행한다.
2. Unity에서 `Assets/Scenes/Battle.unity`를 열고 Console을 비운다.
3. Host와 최신 Standalone Client를 같은 `MyDefense-Dev` Session으로 실행한다.
4. local/dev 검증은 Host=`dev-host`, Client=`dev-client`로 수행한다. Fusion State Authority가 두 ID를 roster로 등록하며, 명시적인 `dev-*` 사용자는 로컬 DB에만 자동 생성된다.
5. 테스트가 끝날 때까지 Host와 Client를 명시적으로 이탈시키지 않는다.
6. Settlement 검증 중에는 Spring Boot를 재시작하지 않는다. 현재 신뢰 roster는 최대 4시간 동안 프로세스 메모리에 보존되며, 만료 또는 재시작 시 안전하게 보상을 거부한다.

## 1. Mutation 활성화·재변이

1. 해금된 순수 Mythic을 선택한다.
2. `Activate Mutation (300 G)`를 한 번 누른다.
3. 개인 Gold만 300 감소하고 8종 중 하나가 활성화되는지 확인한다.
4. 같은 Mythic에서 재변이를 반복한다.
5. 비용이 `600 → 1,200 → 2,400 → 4,800 → 4,800`인지 확인한다.
6. 재변이 직후 결과가 직전 Mutation과 다른지 확인한다.
7. Mutation Injector를 적용하면 Gold 차감 없이 Injector Mutation으로 교체되는지 확인한다.
8. SEALED 또는 미해금 Mythic에는 활성화·Injector 사용이 차단되는지 확인한다.

## 2. Mutation 전투 효과

각 Mutation을 가진 Mythic이 실제 Monster/Boss를 공격하도록 두고 아래를 확인한다.

- `GIANT`: 주 대상 주변에 Splash 피해가 적용된다.
- `BERSERK`: 일반 Monster보다 Boss 대상 단일 피해 배율이 커진다.
- `SWIFT`: 공격 간격이 짧아진다.
- `TOXIC`: 명중 후 지속 피해 Tick이 적용된다.
- `GREEDY`: 명중할 때 공격자 개인 인게임 Gold가 증가한다.
- `OBESE`: 성공/실패 피해 배율 중 하나만 권위적으로 적용된다.
- `FROZEN`: 일정 시간 Monster 이동 속도가 감소한다.
- `BLANK`: 추가 전투 효과가 없다.

Host/Client가 같은 HP·Gold·상태 효과를 보고, 중복 지급이나 이중 Hit가 없어야 한다.

## 3. 일반·신화 공명

1. 우측 하단 `BATTLE RESONANCE`에서 일반 공명을 1단계 구매한다.
2. Gold가 400만큼 한 번 차감되고 Normal~Legendary 공격 Snapshot이 즉시 갱신되는지 확인한다.
3. 신화 공명을 1단계 구매한다.
4. Gold가 800만큼 한 번 차감되고 Mythic만 갱신되는지 확인한다.
5. 단계별 비용과 최대 5단계 제한을 확인한다.
6. Client를 종료한 뒤 같은 사용자 ID로 재접속하여 공명 레벨이 복구되는지 확인한다.
7. 전투 Session을 완전히 종료하고 새 Session을 시작하면 두 공명이 Lv.0으로 초기화되는지 확인한다.

## 4. 행성·80 Wave·Boss

1. 선택한 행성 ID가 Session과 Settlement에 동일하게 유지되는지 확인한다.
2. 일반 Wave는 양쪽 개인 Lane에 Spawn되고 Wave 10 단위마다 Boss Wave가 진입하는지 확인한다.
3. Boss는 `BOSS_SHARED` 공용 Lane에 팀 전체 한 마리만 Spawn되는지 확인한다.
4. Wave가 1부터 80까지 진행되고 Wave 80 이전에 CLEARED로 끝나지 않는지 확인한다.
5. 해왕성에서 태양 방향으로 HP·속도가 증가하고, 태양은 별도 Lock 없이 가장 높은 배율을 사용하는지 확인한다.
6. 실제 난이도 체감과 권장 스펙은 행성별로 기록한다. 이 항목은 Balance 플레이 테스트이므로 자동 테스트로 대체하지 않는다.

## 5. Settlement E2E

> local/dev에서는 Fusion State Authority가 loopback 전용 개발 Adapter로 trusted roster를 등록한다. production에서는 이 경로가 비활성화되고 JWT/matchmaking Adapter로 교체되어야 한다. Console에서 `[BattleRoster] registered trusted local roster`가 Wave 시작보다 먼저 출력되어야 한다.

등록이 일시 실패하면 `BattleSceneSessionAdapter.RetryRosterRegistration()`을 Feature Test Hub 또는 MCP에서 명시적으로 한 번 호출한다. 무한 자동 Retry는 하지 않는다.

1. 한 전투는 Wave 80 클리어, 다른 전투는 중도 패배로 종료한다.
2. Host가 종료 Summary를 정확히 한 번 전송하는지 확인한다.
3. Spring 응답의 `battleSessionId`, `status`, `alreadyProcessed`, `rewards`를 Console과 DB에서 대조한다.
4. 같은 Summary를 명시적으로 재시도하면 중복 보상 없이 `alreadyProcessed=true`인지 확인한다.
5. 탈락 후 관전 상태로 남은 플레이어는 보상 대상인지 확인한다.
6. 명시적 이탈 또는 120초 초과 미복귀 플레이어는 보상에서 제외되는지 확인한다.
7. Host/Client의 최종 Wave·Kill·Gold Summary와 서버 저장값이 일치하는지 확인한다.
8. 등록되지 않은 Session 또는 canonical 완료 Wave별 Spawn/Kill 총계와 맞지 않는 Summary는 보상 없이 거부되는지 확인한다.
9. `dev-host`, `dev-client`의 Lobby API 또는 DB 영구 Gold·Universal Piece·Diamond가 응답의 `rewards` 합계만큼 증가했는지 확인한다.
10. 같은 `requestId`/`summaryHash`를 다시 보낼 때 `alreadyProcessed=true`이고 영구 재화가 두 번 증가하지 않는지 확인한다.

## 최종 합격 기준

- Host/Client 기능 신규 Error 0, 기능 신규 Warning 0
- 개인 Gold·보드·Mutation·공명·Wave 상태 양쪽 일치
- Boss 한 마리, Wave 80, Settlement 한 건 계약 유지
- 재접속 후 Mutation·공명·보드 복구
- 중복 Mutation 비용·Gold·Settlement 보상 없음
- 실패 항목은 Task ID, 재현 순서, Host/Client 로그를 함께 기록
