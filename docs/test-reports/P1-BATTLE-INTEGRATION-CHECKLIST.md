# P1 Battle 사용자 통합테스트 체크리스트

## 1. 준비

- 최신 `dev`가 반영된 Battle feature 브랜치를 사용한다.
- Unity 프로젝트가 `C:/myDefense/Client`인지 확인한다.
- Windows Development Build는 `Client/_localbuild/P1Integrated_20260827_R7/Client.exe`를 사용한다.
- Photon App ID는 Host/Client가 같은 값을 사용한다. `PhotonAppSettings.asset`은 로컬 설정으로만 유지하고 Git에 포함하지 않는다.
- Host와 Client는 매번 동일한 새 Fusion Session 이름을 사용한다.
- Fixture/행성 검증 Session과 Settlement Session을 절대 재사용하지 않는다.

결과 표기는 `PASS`, `FAIL`, `BLOCKED` 중 하나로 하고, FAIL이면 Session 이름·Host/Client 로그·캡처·재현 순서를 함께 남긴다.

## 2. 기본 2인 연결

1. Unity Editor에서 `Battle.unity`를 연다.
2. Editor Host는 `jjangash`, Standalone Client는 `kinggusi`로 같은 Session에 들어간다.
3. 양쪽에서 Player 1/2, Wave, Gold, 보드 24칸, 각 Lane 위치가 일치하는지 확인한다.
4. Host의 Unit 이동·Swap 결과가 Client에, Client의 조작 결과가 Host에 복제되는지 확인한다.
5. Console과 Standalone 로그에 `InvalidOperationException`, `NullReferenceException`, Fusion Networked 접근 오류가 없는지 확인한다.

판정: 양쪽 사람이 각각 자기 화면과 상대 화면의 동일 상태를 확인해야 PASS다. 같은 PC의 Editor+Standalone도 네트워크 검증에는 유효하지만, 최종 사람 판정은 두 PC 재검증을 권장한다.

## 3. Fixture 직접 요청과 복제

Host와 Client 각각 아래를 직접 수행한다.

1. 좌측 상단 `Show P1 Fixture`를 누른다.
2. Mythic ID `29`를 입력하고 `Apply ID`를 누른다.
3. Mutation을 선택한다.
4. `Spawn Alien 29 + <Mutation>`을 누른다.
5. 요청한 플레이어의 첫 빈 슬롯과 상대 화면의 같은 슬롯에 동일 Unit이 나타나는지 확인한다.
6. Spawn 전후 Gold, earned Gold, Kidnap 횟수가 변하지 않는지 확인한다.

Host와 Client가 각각 최소 1회 직접 요청해야 Fixture client-origin RPC까지 PASS다.

## 4. Mutation·Snapshot 전투 판정

ID 29의 기준 Snapshot은 Damage 100, AttackSpeed 1.0, Range 3.5다. 아래 수치는 다른 영구 성장 데이터가 섞이지 않은 local `dev-host`/`dev-client` 기준이다.

| Task | Fixture | 기대 결과 |
|---|---|---|
| P1-2-4 | `29 + TOXIC` | 첫 피해 110, 이후 1초 간격 22 피해 3회, 단발 격리 총 176 |
| P1-2-5 | `29 + SWIFT` | AttackSpeed 1.25, 공격 간격 약 0.8초 |
| P1-2-5 | `29 + FROZEN` | AttackSpeed 0.85, 공격 간격 약 1.176초, 피격 대상 이동속도 70%가 2초 후 복구 |
| P1-2-5 | `29 + GREEDY` | 권위 Primary Hit 1회당 해당 플레이어 Gold/earned +2, Splash·DoT에는 중복 지급 없음 |
| P1-2-6 | `29 + GIANT` | Damage 135, AttackSpeed 0.9, Range 3.85, 반경 2.5 보조 대상 피해 87.75 |
| P1-2-6 | `29 + BERSERK` | 일반 대상 125, Boss 대상 250, 한 공격에 Payload 1회 |
| P1-2-6 | `29 + OBESE` | Damage 80·AttackSpeed 0.8·Range 4.025, 실패 40 또는 성공 200 중 하나만 적용 |
| P1-3-6 | `29 + NONE` | 강화 전 100/1.0/3.5, Mythic Lv.1 비용 800 후 108/1.01/3.5가 실제 공격에 적용 |
| 외형 회귀 | `29 + BLANK` | Mutation 추가 전투 효과 없음 |

각 케이스에서 확인할 공통 항목:

- Host/Client의 Unit 외형, Animation, Aura/marker가 동일하다.
- Host/Client Unit 모두 사거리 안의 양쪽 `EACH_FIELD` Monster를 공격할 수 있다.
- 상대 필드 공격도 Projectile 원점·도착점과 실제 피격 Monster HP가 양 화면에서 일치한다.
- 공용 Boss는 Host/Client 양쪽 Unit이 모두 공격한다.
- Projectile 생성·추적·명중·소멸이 양쪽에서 일치한다.
- Monster/Boss HP와 위치가 양쪽에서 일치한다.
- Client Monster가 3분 이상 이동하는 동안 멈춤·waypoint 접힘·순간이동이 없다.
- 다수 Monster에서 Host/Client frame spike 또는 `응답 없음`이 반복되지 않는다.
- 같은 Projectile이 같은 대상에 중복 Hit하지 않는다.
- `GIANT` 이외 Projectile의 primary 피해는 발사 시 지정된 Monster에만 적용되고, 이동 경로에서 겹친 다른 Monster collider가 피해를 가로채지 않는다.
- `GIANT`은 지정 대상에 primary, 그 대상 중심 반경 2.5 안의 보조 대상에만 splash가 적용된다.
- `TOXIC` 지연 피해는 처음 맞은 동일 Monster에만 이어지며, 다음 공격 대상에게 이전 DoT가 이동하지 않는다.
- FROZEN은 Slow 종료 후 속도가 100%로 돌아온다.
- TOXIC 재적중은 DoT를 중첩하지 않고 갱신한다.

TOXIC는 자연 연사 중 재적중이 타이머를 갱신하므로 정확한 3틱 판정이 어렵다. Unit을 한 번만 공격하게 격리하거나 공격 전후 HP를 영상/로그로 기록한다.

## 5. 9행성·Boss 난이도

Development 전용 Session 형식은 `P1VAL-{MAP}-W{NNN}-{12~32자리 hex nonce}`다. 이 Session은 Settlement에 사용하지 않는다.

행성: `NEPTUNE`, `URANUS`, `SATURN`, `JUPITER`, `MARS`, `EARTH`, `VENUS`, `MERCURY`, `SUN`.

각 행성에서 최소 다음을 확인한다.

1. W009 정규 Wave: 양 Lane Monster 수, HP, 속도, 위치 동기화.
2. W010 Boss Wave: 공용 Boss 정확히 1, HP/속도/NetworkTransform 동기화.
3. 가능하면 W080: 최종 난이도, 클리어 가능성, 전투 피로도.
4. 지정 Wave Start는 1회만 성공하고 두 번째 Start·자동 다음 Wave는 거부된다.
5. P1VAL Session에서 Roster와 Settlement 요청은 0건이다.

P1VAL Session은 안전을 위해 자동 Wave를 시작하지 않는다. Host의 `Show P1 Fixture` 패널에서 `Start P1 Wave NNN (Host only)`를 정확히 한 번 눌러 시작한다. Client 버튼은 비활성화되며, 시작 후 같은 버튼을 다시 누를 수 없어야 한다.

행성별로 `너무 쉬움 / 적정 / 너무 어려움`, 생존 Wave, 사용한 Unit/Muation, 플레이 시간을 기록한다.

## 6. 새 Roster·Settlement

1. Spring을 최신 `dev`, `SPRING_PROFILES_ACTIVE=local`로 실행한다.
2. Unity는 `MYDEFENSE_ENV=local`을 사용한다.
3. 반드시 새 non-P1VAL Session을 만든다. 예: `P1SET-<날짜시간>-<nonce>`.
4. Host=`dev-host`, Client=`dev-client`로 연결한다.
5. Host 로그에서 `[BattleRoster] registered trusted local roster`가 Round 1 시작보다 먼저 정확히 1회 출력되는지 확인한다.
6. 두 플레이어로 정상 전투를 terminal 상태까지 진행한다.
7. Host 로그의 `[BattleSettlement] accepted`와 Session ID, status, `alreadyProcessed`를 기록한다.
8. 서버 응답의 두 플레이어, finalWave, Gold earned/spent/final 값을 양쪽 Battle Summary와 대조한다.
9. 실패 후 retry를 검증할 때는 동일 requestId·summaryHash·payload가 유지되는지 확인한다.

금지:

- `P1-Settlement-Fail-20260824-2155` 또는 그 Summary를 POST하지 않는다.
- P1VAL Session Summary를 POST하지 않는다.
- 서로 다른 Session의 Roster와 Summary를 섞지 않는다.

현재 자동/AI 검증은 Roster 등록 후 Wave gate까지 PASS다. 새 terminal Summary POST·응답·retry는 사람 통합테스트 대기다.

## 7. 최종 종료 기준

- Unity 컴파일 오류 0.
- 전체 EditMode PASS.
- Battle Scene validate issue 0.
- Host/Client 게임 Error 0.
- Mutation 8종과 NONE의 양쪽 사람 판정 PASS.
- 9행성 난이도 기록 완료.
- 새 non-P1VAL Settlement 응답 대조 PASS.
- Git 변경 목록에서 Packages, ProjectSettings, PhotonAppSettings, Fusion sample meta 삭제, `.vsconfig`, 생성된 솔루션 파일 변경을 제외.
