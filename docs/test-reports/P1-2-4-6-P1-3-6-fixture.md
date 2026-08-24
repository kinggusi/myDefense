# P1 Mythic / Mutation Development Fixture

## 목적

P1-2-4~6과 Mythic P1-3-6을 확률 기반 Kidnap/Merge 반복 없이 결정적으로 검증한다.

## 안전 경계

- `UNITY_EDITOR` 또는 `DEVELOPMENT_BUILD`에서만 컴파일된다.
- Production Build에는 패널, RPC, 직접 생성 로직이 포함되지 않는다.
- State Authority가 요청한 플레이어의 첫 빈 슬롯에만 적용한다.
- canonical `alien-spec.json`의 해금된 Mythic과 `mutation-spec.json`의 활성 Mutation만 허용한다.
- 피해량, 공격속도, 범위, DoT 등의 수치는 Fixture가 만들지 않고 canonical Balance를 그대로 사용한다.

## 사용법

1. Battle Scene에서 Host와 Client를 동일 Session에 연결한다.
2. 각 화면 좌측 상단의 `Show P1 Fixture`를 누른다.
3. 해금된 Mythic ID `29`~`32` 중 하나를 입력하고 `Apply ID`를 누른다.
4. 필요한 Mutation을 선택한다.
5. `Spawn Alien <ID> + <Mutation>`을 누른다.
6. 해당 플레이어 보드의 첫 빈 슬롯과 양쪽 화면에 동일한 결과가 복제됐는지 확인한다.

`NONE`은 Mutation 없는 순수 Mythic을 생성해 Mythic Snapshot 검증에 사용한다.

## Task별 권장 생성값

| Task | 생성값 | 검증 |
|---|---|---|
| P1-2-4 | Mythic 29 + `TOXIC` | DoT Tick 수·간격·총 피해 |
| P1-2-5 | Mythic 29 + `SWIFT` | 공격속도 반영 |
| P1-2-5 | Mythic 29 + `FROZEN` | 이동속도 감소와 지속시간 |
| P1-2-5 | Mythic 29 + `GREEDY` | 적중 Gold 권위 장부 |
| P1-2-6 | Mythic 29 + `GIANT` | 적중점 광역 피해 |
| P1-2-6 | Mythic 29 + `BERSERK` | Boss 대상 피해 배율 |
| P1-2-6 | Mythic 29 + `OBESE` | 결정적 약/강 피해 분기 |
| P1-3-6 | Mythic 29 + `NONE` | Mythic 강화 Snapshot 적용 |

## 자동 검증

- `DevelopmentMythicFixtureRulesTests`: 13/13 PASS
- Unity 전체 EditMode: 391/391 PASS
- Battle Scene Validate: issue 0, Missing Script 0, Broken Prefab 0
- Play Mode에서 Fixture Component 자동 부착 확인
- Console Error 0

## 사람 검증 게이트

동료 Battle 담당은 위 Fixture를 이용해 Host/Client 양쪽의 실제 전투 효과, 카메라 가독성, 다수 Unit 성능을 확인한다. 이 검증 전까지 P1-2-4~6 및 Mythic P1-3-6은 `검증 대기`를 유지한다.
