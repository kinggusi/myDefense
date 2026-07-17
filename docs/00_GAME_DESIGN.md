# 왹져 디펜스 게임 기획서 V1.2

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
- 일반 협동 매치는 총 80라운드

## 4. 슬롯과 드래그
- Alien과 Mutation Injector 모두 1칸 사용
- 빈칸이 없으면 Kidnap 불가
- Kidnap 결과는 왼쪽부터 오름차순으로 첫 빈칸에 자동 배치
- Alien은 빈칸으로 드래그 이동 가능
- B를 A에 드래그해 Merge하면 결과물은 A 위치에 생성
- Mutation Injector는 Alien 위로 드래그해 사용

## 5. 플레이어 탈락
- 개인 필드 몬스터 수가 100에 도달하는 즉시 해당 플레이어 탈락
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
총 48종:
- Normal 7
- Epic 7
- Unique 7
- Legendary 7
- Mythic 20

Normal~Legendary는 등급별 7종이며 세부 역할 분배는 별도 밸런스 문서에서 확정한다.

### Mythic 영구 획득 풀
- 상점 Gacha: 일반 Mythic 18종
- 로비 Breeding: Mythic 20종
- Breeding 전용 Mythic 2종은 Gacha에서 등장하지 않음
- Breeding 전용 Mythic 2종은 낮은 확률로 등장하며 전용 연출을 사용
- Mythic 미해금 상태에서 조각을 획득하면 해금 정책에 따라 해금하고, 초과 조각은 누적 보관

## 7. Merge
- Normal~Legendary 구간에서 동일 등급의 동일 종만 Merge 가능
- 다른 종 Merge 불가
- 결과는 다음 등급 전체 풀에서 랜덤
- 고정 진화 계보 없음
- Legendary + Legendary Merge 시 현재 플레이어가 해금한 Mythic 풀에서 서로 다른 후보 3종을 제시
- 플레이어가 후보 3종 중 하나를 선택하면 해당 Mythic이 Merge 결과로 생성
- Legendary Merge 후보는 최대 3회 리롤 가능
- 리롤 시 후보 3종 전체를 교체하며 동일 리롤 화면 안에서 같은 Mythic을 중복 제시하지 않음

## 8. Kidnap
- Normal Alien: 99.5%
- Mutation Injector: 0.5%
- Injector 종류는 동일 확률
- Kidnap 비용은 시도할수록 증가
- 라운드가 바뀌어도 초기화 없음
- 비용 공식은 BalanceConfig에서 관리

### 협동 인게임 골드
- 몬스터가 어느 플레이어 필드 또는 공용 Lane에서 처치되든 양쪽 플레이어에게 동일한 처치 골드 100%를 각각 지급
- 마지막 공격자와 Support Kill 여부는 골드 지급량에 영향을 주지 않음
- Kill과 Support Kill은 전투 통계와 Settlement 기록에만 사용
- 탈락 후 관전 중인 플레이어에게도 매치가 진행되는 동안 동일하게 인게임 골드 지급
- 일시적인 연결 종료 상태에서도 세션과 인게임 골드 장부를 유지하고 동일하게 골드 지급
- 플레이어가 매치 종료 전에 재접속하면 누적된 인게임 골드와 전투 상태를 그대로 복구
- 일시적인 연결 종료만으로 PLAYER_ABANDONED 처리하지 않음
- 명시적으로 나가기를 선택하거나 매치 종료 시점까지 복귀하지 않은 경우에만 최종 이탈로 판정

## 9. Mutation Injector
- 원하는 Mutation을 확정하는 매개체
- 별도 최종 형태가 아님
- 랜덤으로 같은 Mutation을 얻은 경우와 최종 결과 동일
- Normal~Mythic 모든 Alien에게 사용 가능
- 사용하면 Pending Mutation DNA를 보유
- Merge 이후에도 계승
- Kidnap 시 낮은 확률로 등장하며 Alien과 동일하게 필드 슬롯 1칸을 차지

## 10. DNA 계승
- NONE + NONE → NONE
- A + NONE → A
- NONE + A → A
- A + A → A
- A + B → A 또는 B 중 하나 랜덤

서로 다른 DNA가 만나도 Merge는 허용한다. 둘 중 하나만 남는 실수 유발 포인트로 사용한다.
서로 다른 DNA의 선택 확률은 각각 50%다.

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
- Legendary Merge 결과로 Mythic이 생성되는 즉시 계승된 DNA Mutation이 무료로 자동 활성화
- 별도의 활성화 골드 비용이나 버튼 조작 없음

### DNA가 없는 Mythic
- 순수 Mythic이 생성되면 Mutation 버튼 활성화
- 버튼을 누르면 개인 인게임 골드를 지불하고 꽝형을 포함한 랜덤 Mutation 획득
- 최초 활성화 비용은 300 인게임 골드

### 재변이
- 이미 변이된 Mythic도 재변이 가능
- 재변이 비용은 `600 → 1,200 → 2,400 → 4,800` 인게임 골드 순서로 증가하고 이후 4,800으로 고정
- 현재 적용 중인 Mutation은 재변이 후보에서 제외하여 반드시 다른 Mutation 획득
- Injector로 확정한 Mutation도 재변이 가능
- 이미 Mutation된 Mythic에 Injector를 사용하면 기존 Mutation을 해당 Injector Mutation으로 즉시 무료 교체

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
- DNA 없는 Mythic의 최초 랜덤 Mutation과 재변이에서는 등장 가능
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
- Mythic 20 × Mutation 8 조합을 전부 개별 Prefab으로 만들지 않음
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

## 18. 협동 매치 보상 원칙
- Settlement는 종료된 매치의 결과, 도달 라운드, 참가자 상태, 전투 통계를 확정하고 영구 보상 계산에 넘기는 정산 단계
- 팀이 80라운드를 클리어하면 매치 결과는 CLEARED
- 한 플레이어가 먼저 탈락해도 관전을 유지하고 팀이 80라운드를 클리어하면 두 플레이어에게 동일한 클리어 보상 지급
- 탈락한 플레이어가 매치 종료 전에 이탈하면 해당 플레이어는 보상 미지급
- 두 플레이어 모두 매치 종료까지 남아 있다가 실패하면 최종 도달 라운드 비율에 따른 보상을 두 플레이어에게 동일하게 지급
- 구체적인 보상량과 최소 보상 지급 라운드는 Economy Balance에서 관리

## 19. 미정 사항
- 일반 몬스터 라운드별 스폰 수
- 꽝형 상세 보상 규칙
- 탈락 플레이어의 관전 유지 판정과 일시적인 네트워크 단절 처리
