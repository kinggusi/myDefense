# P0 통합 테스트 시나리오

이 문서는 P0 구현의 자동 검증이 끝난 뒤 사용자가 Unity Editor Host와 Standalone Client를 함께 실행해 최종 승인하는 절차다. 자동 테스트와 Unity MCP 검증은 결함 범위를 줄이지만, 실제 포인터 입력·두 창의 시각 동기화·재접속 체감은 이 시나리오로 확정한다.

## 1. 준비

1. Unity에서 `Assets/Scenes/Battle.unity`를 연다.
2. 최신 Standalone 빌드가 `Client/_localbuild/FusionClient/Client.exe`에 있는지 확인한다.
3. Editor Play Mode를 시작한다. Editor는 Host로 `MyDefense-Dev` 세션을 연다.
4. 다음 환경으로 Standalone Client를 실행한다.

```powershell
$env:MYDEFENSE_FUSION_SESSION="MyDefense-Dev"
$env:MYDEFENSE_FUSION_ROLE="client"
$env:MYDEFENSE_FUSION_USER_ID="dev-client"
E:\study\MyDefenseGame\Client\_localbuild\FusionClient\Client.exe
```

5. Editor Console에서 Player 1/2 등록과 Battle session 초기화 로그를 확인한다.
6. 개발 Smoke Test에서 두 화면의 시작 Gold가 각각 `100,000`이며, 상대의 소비가 내 Gold를 차감하지 않는지 확인한다. 정식 시작 Gold는 canonical Balance 외부화 후 별도 검증한다.

## 2. 세션·필드·Wave (P0-2, P0-3)

- Editor와 Client가 같은 세션에 입장한다.
- 각 화면에서 자신의 보드는 아래쪽, 상대 보드는 위쪽이다.
- 각 플레이어의 일반 몬스터가 자기 화면의 아래쪽 진입점에서 시작하고 상대 화면에서는 위쪽에 대칭 표시된다.
- 양쪽 alive count가 같은 권위 상태를 표시한다.
- 몬스터가 한 바퀴를 돌아도 Client에서 사라지거나 순간이동하지 않는다.
- Console과 Development Console에 신규 Error가 없다.

## 3. Kidnap·이동·Merge·Mutation (P0-5)

각 창에서 따로 수행한다.

1. `왹져 소환`을 3회 누른다.
2. 자신의 Gold만 누적 비용만큼 감소하고, 첫 빈 슬롯부터 왼쪽→오른쪽·위→아래 순서로 배치되는지 확인한다.
3. 빈 슬롯으로 드래그한 뒤 다시 다른 빈 슬롯으로 이동한다.
4. 서로 다른 Alien을 겹치면 Merge되지 않고 두 슬롯이 교환된다.
5. 같은 species·grade Alien을 겹치면 정확히 한 번 Merge되고 다음 grade 전체 풀에서 결과가 생성된다.
6. Injector가 있는 경우 빈 슬롯 이동, 대상 적용, Pending DNA 계승을 확인한다.
7. 한쪽 조작이 상대 화면에 동일한 권위 결과로 보이는지 확인한다.

## 4. Boss·탈락·관전·종료 (P0-4, P0-6)

- Boss Wave에서 개인 Lane 복제가 아니라 공용 Lane Boss 한 마리만 생성된다.
- Boss 제한시간이 양쪽에서 같은 값으로 감소한다.
- Boss 처치 시 Boss kill이 한 번만 기록되고 다음 Wave로 진행한다.
- 제한시간 만료 시 Match가 `FAILED`로 한 번만 전환된다.
- 개인 필드 alive count가 100에 도달한 플레이어만 `ELIMINATED`가 된다.
- 한 명만 탈락하면 Match는 계속 `RUNNING`이고 탈락자는 상대 필드를 관전한다.
- 두 명 모두 탈락하면 `FAILED`, 최종 Wave를 완료하면 `CLEARED`가 표시된다.

## 5. Legendary→Mythic 선택 (P0-7)

1. 같은 Legendary 두 개를 Merge한다.
2. 재료 슬롯이 잠기고 후보 3종 UI가 양쪽에서 일관되게 표시되는지 확인한다.
3. 선택 대기 재료는 이동·공격하지 않는다.
4. `리롤`은 선택으로 오인되지 않고 후보 전체를 바꾸며 남은 횟수와 10초 Timer를 갱신한다.
5. 후보를 선택하면 Mythic 한 개만 생성된다.
6. 잠금 Mythic을 선택하면 계승 DNA는 `SEALED`, 해금 Mythic이면 `ACTIVE`로 적용된다.

## 6. 공격·Projectile·Gold 장부 (P0-5-8, P0-8)

- Alien이 State Authority에서 목표를 선택하고 Network Projectile이 양쪽에 보인다.
- Projectile이 Monster에 닿을 때 HP가 양쪽에서 같은 값으로 감소한다.
- 동일 Projectile이 같은 대상을 중복 타격하지 않는다.
- 일반·Elite·공용 Boss 처치 시 Host와 Client의 개인 지갑이 canonical `killGold`만큼 동일하게 각각 한 번 증가한다.
- 필드 소유자나 마지막 공격자가 달라도 양쪽 지급액은 동일하며, 이후 소비는 서로 독립적이다.
- Kill·Support Kill·Boss Kill 수치가 Gold와 별도 통계로 유지된다.

## 7. 재접속 (P0-9)

1. Client의 현재 Gold, 보드, Pending Mutation, Mythic 선택, Wave, Boss Timer, Monster HP/위치를 기록한다.
2. Client만 종료한다. Host의 세션·Gold·보드는 유지되어야 한다.
3. 같은 `dev-client` User ID로 Client를 다시 실행한다.
4. Player 2 슬롯을 다른 사용자에게 빼앗기지 않고 기존 슬롯으로 복귀하는지 확인한다.
5. 보드·Alien·Injector·Mutation·Mythic 선택·Wave·Monster·Boss Timer가 권위 상태와 일치하는지 확인한다.
6. 복구 후 Monster와 Projectile 시각 상태가 중복 생성되지 않는지 확인한다.

## 8. Settlement (P0-10)

- Match 종료 시 Settlement 요청이 한 번 생성된다.
- Player 2명의 Gold 산식 `initial + earned - spent = final`이 맞는다.
- player kill 합과 monster kill 합, canonical killGold 집계가 맞는다.
- 이탈 후 미복귀 플레이어는 `abandoned=true`, 관전 중 남아 있던 탈락자는 보상 대상이다.
- 같은 request/session/summary 재전송은 중복 저장·중복 지급 없이 기존 결과를 반환한다.
- Spring 응답 실패 시 자동 무한 재시도하지 않고 pending 상태에서 명시적 Retry만 허용한다.

## 9. 최종 합격 기준

- Unity Console 신규 Error 0.
- Host/Client Development Console 신규 Error 0.
- Scene Dirty=false, validate issue 0, Missing Script 0, Broken Prefab 0.
- 두 클라이언트의 권위 상태가 일치한다.
- 본 문서 각 절차를 통과한 뒤 `docs/98_IMPLEMENTATION_TASKS.md`의 `검증 대기` Task를 `완료`로 승격한다.
