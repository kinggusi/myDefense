# 왹져 디펜스 게임 기획서 V1.1

## 1. 개요
- 장르: 2인 협동 머지 디펜스
- 핵심 재미: 전투 중 납치, 같은 종 머지, 신화 생체변이, 협동 생존

## 2. 용어
- 전투 중 소환: 납치(Kidnap)
- 로비 영구 획득: 뽑기(Gacha)
- 합성: Merge
- 생체변이: Mutation
- 생체변이 아이템: Mutation Injector
- 유닛: Alien / 왹져

## 3. 필드
- 플레이어별 개인 필드: `4 x 6`, 총 24칸
- 두 개인 필드 사이에 공용 1자 구역 존재
- 일반 몬스터는 각 플레이어 개인 영역을 순환
- 보스는 공용 1자 구역에서 이동
- 보스는 10라운드마다 등장
- 보스 제한시간 초과 시 전체 매치 즉시 패배

## 4. 슬롯과 드래그
- Alien과 Mutation Injector 모두 1칸 사용
- 빈칸이 없으면 Kidnap 불가
- Kidnap 결과는 왼쪽부터 오름차순으로 첫 빈칸에 자동 배치
- Alien은 빈칸으로 드래그 이동 가능
- B를 A에 드래그해 Merge하면 결과물은 A 위치에 생성
- Mutation Injector는 Alien 위로 드래그해 사용

## 5. 플레이어 탈락
- 개인 필드 몬스터 수가 한도에 도달하면 해당 플레이어 탈락
- 탈락 플레이어는 모든 조작 비활성화 후 관전
- 해당 플레이어의 신규 몬스터 스폰은 중단
- 이미 존재하는 몬스터는 제거하지 않음
- 남은 플레이어가 계속 처리

### PlayerBattleState
- ACTIVE
- ELIMINATED
- SPECTATING

### MatchState
- RUNNING
- CLEARED
- FAILED

## 6. 왹져 구성
총 36종:
- Normal 8
- Epic 8
- Unique 8
- Legendary 8
- Mythic 4

Normal~Legendary 역할 분배:
- 단일 공격형 5
- 광역 공격형 2
- 상태 이상형 1

## 7. Merge
- 동일 등급의 동일 종만 Merge 가능
- 다른 종 Merge 불가
- 결과는 다음 등급 전체 풀에서 랜덤
- 고정 진화 계보 없음
- Legendary Merge 시 현재 플레이어가 해금한 Mythic 풀에서 랜덤

## 8. Kidnap
- Normal Alien: 99.5%
- Mutation Injector: 0.5%
- Injector 종류는 동일 확률
- Kidnap 비용은 시도할수록 증가
- 라운드가 바뀌어도 초기화 없음
- 비용 공식은 BalanceConfig에서 관리

## 9. Mutation Injector
- 원하는 Mutation을 확정하는 매개체
- 별도 최종 형태가 아님
- 랜덤으로 같은 Mutation을 얻은 경우와 최종 결과 동일
- Normal~Mythic 모든 Alien에게 사용 가능
- 사용하면 Pending Mutation DNA를 보유
- Merge 이후에도 계승

## 10. DNA 계승
- NONE + NONE → NONE
- A + NONE → A
- NONE + A → A
- A + A → A
- A + B → A 또는 B 중 하나 랜덤

서로 다른 DNA가 만나도 Merge는 허용한다. 둘 중 하나만 남는 실수 유발 포인트로 사용한다.

## 11. Pending / Active Mutation
### PendingMutationType
- Normal~Legendary에서 보유 가능
- DNA만 계승
- 스탯, 공격 메커니즘, 외형 변화 없음

### ActiveMutationType
- Mythic에서만 활성화
- 스탯, 공격 메커니즘, 외형 변화 적용

## 12. Mythic Mutation
### DNA가 있는 Mythic
- 해당 DNA Mutation 확정

### DNA가 없는 Mythic
- 개인 인게임 골드 사용
- 랜덤 Mutation 획득

### 재변이
- 이미 변이된 Mythic도 재변이 가능
- 재변이 비용은 횟수에 따라 증가
- 같은 Mutation이 다시 나올 수 있음
- Injector로 확정한 Mutation도 재변이 가능

### TBD
- DNA가 있는 Mythic의 최초 활성화 골드 비용 여부

## 13. Mutation 8종
1. 광역형
2. 단일 보스형
3. 공격속도형
4. 도트 피해형
5. 경제형
6. 도박형
7. 상태 이상형
8. 꽝형

각 Mutation은 강한 장점 1개, 보조 장점 1개, 명확한 단점 1개, 공격 메커니즘 변화 1개, 외형 변화 1개를 가진다.

## 14. 대표 예시
### 비만한
- 공격력 증가
- 공격 범위 증가
- 공격속도 감소
- 단일 공격 일부 Splash 전환 가능
- 몸 비율 확대, 이펙트 크기 증가

### 비겁한
- 낮은 체력 적에게 피해 증가
- 마무리 성능 강화
- 높은 체력 적에게 피해 감소
- HP 비율 기반 피해 보정
- 위축된 자세, 불안정한 오라

### 약탈자
- 기본 공격 적중으로 개인 골드 획득
- 장기전 경제 강화
- 기본 전투 성능 감소
- 적중 기반 골드 생성
- 코인 이펙트와 금속 장식

## 15. 꽝형
- Mutation Injector로 직접 등장하지 않음
- 꽝형 2개를 모으면 보상 발생
- 보상 후보: 새로운 랜덤 Mythic 또는 랜덤 Mutation Mythic

### TBD
- 두 Mythic 소모 여부
- 보상 생성 위치
- 필드가 가득 찬 경우
- 개인 Mythic 해금 풀 적용 여부
- 두 보상 간 확률

## 16. 외형 변화
공통 변형:
- 몸 비율
- 색상
- 오라
- 장식
- 이펙트 크기

원칙:
- 시각 이펙트 크기와 실제 판정 범위는 별도
- Mythic 4 × Mutation 8 조합을 전부 개별 Prefab으로 만들지 않음
- 공통 변형을 기본 적용하고 극히 일부만 예외 보정

## 17. 데이터 관리
```text
Excel/CSV
    ↓
공통 JSON
   ├─→ Unity ScriptableObject 생성
   └─→ Spring Boot 밸런스 데이터 로드
```

핵심 데이터:
- Alien
- Skill
- Mutation
- MutationInjector
- Monster
- Wave
- Economy
- BalanceConfig

## 18. 배틀 프리젠테이션 (HUD & HP 표시)
- **Wave HUD**: 상단 UI는 실제 진행 중인 라운드를 `WAVE {round}` 포맷으로 실시간 동기화하여 표시한다.
- **몬스터 체력 표시**: 일반 몬스터와 보스 머리 위에 체력 바(Slider) 및 `현재 HP / 최대 HP` 텍스트를 정수 형태로 출력한다.
- **체력 스케일링**: 일반 몬스터의 최대 체력은 웨이브가 증가할 때마다 점진적으로 증가한다. (공식: `scaledMaxHp = baseMaxHp * (1f + (round - 1) * healthGrowthPerRound)`, 기본 증가율은 10%이며 Balance/Wave 설정에서 조율 가능)
- **보스 체력**: 보스는 해당 라운드의 일반 몬스터 환산 최대 체력에 별도의 보스 배율을 적용하며, 고유 타임아웃 룰을 유지한다.

## 19. 미정 사항
- 최종 라운드 수: 80 또는 100
- 일반 몬스터 라운드별 스폰 수
- 개인 필드 탈락 한도
- DNA 보유 Mythic 최초 활성화 비용
- 꽝형 상세 보상 규칙
