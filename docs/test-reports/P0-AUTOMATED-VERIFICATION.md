# P0 자동 검증 종합 보고

검증일: 2026-08-01

## 자동 테스트

| 범위 | 결과 |
|---|---:|
| Unity EditMode | 335/335 통과 |
| Spring Server | 286/286 통과 |
| BalanceTool | 70/70 통과 |
| generated JSON 결정성 | 연속 2회 SHA 동일 |
| Windows Standalone 개발 빌드 | 성공, Error 0 |

Canonical manifest는 `1-133ff341b0443dd1` / `133ff341b0443dd188421060943fd14b337f85403bd06db762b460a0525a38e1`, Battle bundle hash는 `7455fa6d5ef5833bff113695d3df37f1ec45654e8d0cc168822d9a986cceebb8`을 사용했다.

## Unity MCP

- Active Scene: `Assets/Scenes/Battle.unity`
- Loaded=true, Dirty=false, buildIndex=0
- validate issue 0
- Missing Script 0
- Broken Prefab 0
- Host State Authority와 `BattleProjectileSpawner` Prefab 참조 확인
- Editor compile Error 0
- Compiler Warning 13: deprecated API 5, 미사용 직렬화 필드 8

## 실제 Fusion Host/Client

- Editor Host와 Standalone Client가 `MyDefense-Dev` 세션에 입장했다.
- Player 1 `peer-*`와 Player 2 `dev-client`가 slot 1/2에 등록됐다.
- 양쪽 private Lane에 Monster 10마리씩, 총 Networked Monster 20개가 존재했다.
- 각 플레이어의 권위 Gold는 100,000으로 분리됐다.
- Monster NetworkTransform 위치가 시간 경과에 따라 변하는 것을 확인했다.
- Client local perspective reprojection은 Monster 20개에 한 번씩 적용됐다.
- `ClientPlayer.log` Runtime Error/Exception/Assert 0.
- State Authority가 Player 1/2 각각에 대해 서버 계산 attack snapshot 48종을 로드했다.
- 실제 Standalone Client에서 Kidnap 1회가 성공했고 Gold가 100,000에서 99,950으로 차감되며 첫 슬롯에 Unit이 배치됐다.

## 실제 재접속

1. `dev-client` 종료 후 Host roster가 1명으로 감소했다.
2. Player 2 connection state가 `DISCONNECTED`가 됐다.
3. Player 2 User ID와 Gold 100,000은 권위 상태에 보존됐다.
4. 같은 User ID로 재실행하자 새로운 PlayerRef가 기존 slot 2를 회수했다.
5. connection state `CONNECTED`, Gold 100,000, alive count 10을 유지했다.
6. 재접속 Standalone 로그 Runtime Error 0.

## 남은 사용자 통합 게이트

Unity MCP 안전 경계상 일반 uGUI 포인터 입력을 코드로 강제 호출하지 않았다. 다음 항목은 `docs/P0_INTEGRATION_TEST_SCENARIO.md`에서 사용자가 실제 조작해 확정한다.

- Kidnap 버튼, Drag, Swap, Merge, Injector
- Legendary 후보 UI, Reroll, 최종 Mythic 선택
- Network Projectile의 양쪽 시각 표시와 Monster HP 감소
- Boss 처치·시간 초과, ELIMINATED·SPECTATING·FAILED·CLEARED
- Match 종료 Spring Settlement 응답과 명시적 Retry

## 차단·잔여 위험

- P0-8-2: Spring의 canonical Battle Entry attack snapshot API와 Fusion State Authority 주입 경계가 구현됐다. manifest/version, 유한·양수 damage/attackRate/range를 검증하고, 영구 레벨 및 ACTIVE Mutation 상태를 runtime Unit에 반영한다. Fusion 전투에서는 snapshot이 없으면 공격하지 않는다.
- 운영 보안: 프로젝트 인증 계층이 아직 없어 username spoof 방지는 후속 릴리스 보안 작업이다. anonymous 개발 identity는 `local` profile에서만 허용한다.
- 운영 설정: default profile이 `local`이므로 운영 배포에서는 production profile 명시를 강제하고, profile 누락 시 시작을 거부하는 guard가 필요하다.
- 복구성: attack snapshot HTTP 실패 후 자동 Retry는 없으며 현재 세션 fault latch로 고정된다.
- Settlement는 현재 시도 중 Wave가 아니라 Networked 최고 완료 Wave를 전송한다. Wave 70 진행 중 패배는 Wave 69로 정산된다.
- 비어 있는 Scene Map ID는 첫 행성 `NEPTUNE`으로 해석되며, 120초 미복귀가 지난 플레이어만 `abandoned=true`가 된다.
- Mythic 재접속 스냅샷은 사용 횟수가 아니라 무료/유료 잔여 리롤 횟수를 보존한다.
- `git diff --check`는 Unity API가 저장한 YAML trailing whitespace 4곳 때문에 아직 통과하지 않는다. 승인 전 직접 편집하지 않는다.
  - `Client/Assets/Prefabs/Monsters/Monster.prefab:705`
  - `Client/Assets/Scenes/Battle.unity:2434, 2601, 3053`
- 사용자 통합 게이트가 남아 있어 관련 Task는 `완료`가 아니라 `검증 대기`로 유지한다.
