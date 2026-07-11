# 왹져 디펜스 게임 기획서 V1.1

## 1. 게임 개요
- 장르: 2인 협동 머지 디펜스
- 핵심 재미:
  1. 전투 중 납치
  2. 같은 종 머지
  3. 신화 왹져 생체변이
  4. 두 플레이어의 협동 생존
- 플랫폼 및 기술:
  - Unity 6
  - Photon Fusion
  - Spring Boot

## 2. 용어
- 전투 중 소환: 납치(Kidnap)
- 로비 영구 획득: 뽑기(Gacha)
- 합성: Merge
- 생체변이: Mutation
- 생체변이 매개체: Mutation Injector
- 유닛: Alien / 왹져

## 3. 필드 구조
- 플레이어별 개인 필드: `4 x 6`, 총 24칸
- 각 플레이어는 동일한 크기의 독립 필드를 가짐
- 두 개인 필드 사이에 공용 1자 전투 구역이 존재
- 일반 몬스터는 각 플레이어 영역을 순환
- 보스는 공용 1자 구역에서 이동
- 보스는 10라운드마다 등장
- 보스 제한시간 초과 시 전체 매치 즉시 패배

## 4. 슬롯 규칙
- Alien 1개는 1칸 사용
- Mutation Injector 1개도 1칸 사용
- 빈칸이 없으면 Kidnap 불가
- Kidnap 결과는 화면 기준 왼쪽부터 오름차순으로 첫 빈칸에 자동 배치
- 정확한 순회 순서는 공통 좌표 규칙 문서에서 고정
- Alien은 드래그로 빈칸 이동 가능
- B를 A 위로 드래그하여 Merge하면 결과물은 A 위치에 생성

## 5. 플레이어 탈락
- 개인 필드 몬스터 수가 허용치를 초과하면 해당 플레이어 탈락
- 탈락 즉시:
  - 모든 조작 비활성화
  - 관전 상태 전환
  - 해당 플레이어의 신규 몬스터 스폰 중단
- 이미 존재하는 몬스터는 제거하지 않음
- 남은 플레이어가 기존 몬스터와 이후 보스를 계속 처리
- 플레이어 탈락과 전체 매치 종료는 별도 상태로 관리

### PlayerBattleState
- ACTIVE
- ELIMINATED
- SPECTATING

### MatchState
- RUNNING
- CLEARED
- FAILED

## 6. 왹져 구성
### 총 36종
- Normal: 8종
- Epic: 8종
- Unique: 8종
- Legendary: 8종
- Mythic: 4종

### Normal~Legendary 역할 분배
현재 V1 기준:
- 단일 공격형 5종
- 광역 공격형 2종
- 상태 이상형 1종

> 광역 3종을 유지하려면 단일 공격형을 4종으로 줄여야 하므로, V1은 5/2/1로 고정한다.

## 7. Merge 규칙
- 동일 등급의 동일 종만 Merge 가능
- 다른 종끼리는 Merge 불가
- Merge 결과는 다음 등급 전체 풀에서 랜덤
- 고정 진화 계보 없음
- `evolutionTargetId` 기반 고정 진화는 사용하지 않음
- Legendary Merge 시:
  - 해당 플레이어가 해금한 Mythic 풀만 사용
  - 다른 플레이어의 해금 상태는 영향을 주지 않음

## 8. Kidnap 규칙
- Normal Alien: 99.5%
- Mutation Injector: 0.5%
- Mutation Injector 종류는 동일 확률
- Kidnap 비용은 시도할수록 증가
- 라운드 전환 시 비용 초기화 없음
- 비용 공식은 `BalanceConfig`에서 관리

## 9. Mutation Injector
- 특정 Mutation을 확정하기 위한 매개체
- 별도 최종 형태가 아님
- 같은 Mutation을 랜덤으로 얻거나 Injector로 얻어도 최종 결과는 동일
- Normal~Mythic 모든 Alien에게 사용 가능
- 사용 방법:
  - Injector를 대상 Alien 위로 드래그
  - 대상 Alien이 Pending Mutation DNA를 보유
- Pending Mutation은 Merge 이후에도 계승

## 10. Mutation DNA 계승
- NONE + NONE → NONE
- A + NONE → A
- NONE + A → A
- A + A → A
- A + B → A 또는 B 중 하나 랜덤

서로 다른 DNA가 만나도 Merge를 막지 않는다. 둘 중 하나만 남도록 하여 플레이 실수와 긴장 요소로 사용한다.

## 11. Pending Mutation과 Active Mutation
### PendingMutationType
- Normal~Legendary에서 보유 가능
- DNA만 계승
- 스탯 변화 없음
- 공격 메커니즘 변화 없음
- 외형 변화 없음

### ActiveMutationType
- Mythic에서만 활성화
- 스탯 변경 적용
- 공격 메커니즘 변경 적용
- 외형 변경 적용

## 12. Mythic Mutation
### DNA가 있는 Mythic
- 보유 DNA에 해당하는 Mutation 확정

### DNA가 없는 Mythic
- 개인 인게임 골드 사용
- 8종 중 랜덤 Mutation 획득

### 재변이
- 이미 변이된 Mythic도 재변이 가능
- 재변이 횟수에 따라 비용 증가
- 같은 Mutation이 다시 등장할 수 있음
- Injector로 확정한 Mutation도 재변이 가능

### 미정 사항
- DNA가 있는 Mythic의 최초 활성화 시 골드 비용 여부: TBD
- 구현 전 최종 확정 필요

## 13. Mutation 8종 카테고리
1. 광역형
2. 단일 보스형
3. 공격속도형
4. 도트 피해형
5. 경제형
6. 도박형
7. 상태 이상형
8. 꽝형

각 Mutation은 반드시 다음 요소를 가짐:
- 강한 장점 1개
- 보조 장점 1개
- 명확한 단점 1개
- 공격 메커니즘 변화 1개
- 외형 변화 1개

## 14. 예시 Mutation
### 비만한
- 강한 장점: 공격력 증가
- 보조 장점: 공격 범위 증가
- 단점: 공격속도 감소
- 메커니즘: 단일 공격의 일부를 Splash로 전환 가능
- 외형: 몸 비율 확대, 이펙트 크기 증가

### 비겁한
- 강한 장점: 낮은 체력 적에게 피해 증가
- 보조 장점: 마무리 성능 강화
- 단점: 높은 체력 적에게 피해 감소
- 메커니즘: 대상 HP 비율 기반 피해 보정
- 외형: 위축된 자세, 불안정한 오라

### 약탈자
- 강한 장점: 기본 공격 적중으로 개인 골드 획득
- 보조 장점: 장기전 경제 성장
- 단점: 기본 전투 성능 감소
- 메커니즘: 적중 기반 골드 생성
- 외형: 금속 장식, 코인 이펙트

## 15. 꽝형 Mutation
- Mutation Injector로 직접 등장하지 않음
- 꽝형 2개를 모으면 보상 발생
- 보상 후보:
  - 새로운 랜덤 Mythic
  - 랜덤 Mutation이 적용된 Mythic
- 다음 세부 규칙은 TBD:
  - 두 Mythic 소모 여부
  - 보상 생성 위치
  - 필드가 가득 찬 경우
  - 개인 Mythic 해금 풀 적용 여부
  - 두 보상 간 확률

## 16. 외형 변화
공통 변형 방식:
- 몸 비율
- 색상
- 오라
- 장식
- 이펙트 크기

원칙:
- 이펙트 시각 크기와 실제 판정 범위는 별도 값
- Mythic 4종 × Mutation 8종을 전부 개별 Prefab으로 만들지 않음
- 공통 변형을 기본 적용
- 테스트 후 극히 일부 조합만 예외 보정

## 17. 데이터 관리
기준 원본:
- Excel 또는 CSV

변환 흐름:
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

## 18. 확정되지 않은 사항
- 최종 라운드 수: 80 또는 100
- 일반 몬스터 라운드별 스폰 수
- 개인 필드 탈락 한도
- DNA 보유 Mythic 최초 활성화 비용
- 꽝형 Mutation 보상 상세 규칙
