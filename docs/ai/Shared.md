# Shared Contract

## Purpose
User/System과 Battle이 함께 사용하는 계약만 둡니다.

## Shared Types
- DamagePayload
- IDamageable
- ITargetProvider
- HitEvent
- PlayerBattleState
- MatchState
- GridPosition
- 공통 DTO, Enum, Network contract

## Grid
- 4 x 6
- 24 slots
- Alien과 Mutation Injector 모두 1칸
- Kidnap은 첫 빈칸 순차 배치

## Merge
- 동일 등급 동일 종만 가능
- Normal~Legendary 결과는 다음 등급 전체 풀 랜덤
- Legendary Merge는 현재 플레이어가 해금한 Mythic 풀에서 서로 다른 후보 3종 제시
- Legendary Merge 후보는 최대 3회 리롤 후 1종 선택
- 결과는 드롭 대상 위치에 생성

## Mutation
- PendingMutationType: 모든 등급에서 보유 가능
- ActiveMutationType: Mythic에서만 활성화
- A + B DNA는 둘 중 하나를 각각 50% 확률로 계승
- DNA를 계승한 Mythic은 생성 즉시 해당 Mutation을 무료로 자동 활성화
- DNA가 없는 Mythic은 개인 인게임 골드를 지불해 랜덤 Mutation 활성화
- 최초 랜덤 Mutation 비용은 300 인게임 골드
- 재변이 비용은 `600 → 1,200 → 2,400 → 4,800`, 이후 4,800 고정
- 재변이 시 현재 Mutation을 후보에서 제외하여 반드시 다른 Mutation 획득
- Mutation된 Mythic에 Injector를 사용하면 기존 Mutation을 Injector Mutation으로 즉시 무료 교체

## Damage
```text
StatCalculator
→ DamagePayload
→ Battle hit/collision
→ IDamageable.ApplyDamage(payload)
```

## Cooperative In-Game Gold
- 몬스터 처치 위치나 마지막 공격자와 무관하게 양쪽 플레이어에게 동일한 처치 골드 100%를 각각 지급
- Kill/Support Kill은 골드 분배가 아닌 통계와 Settlement 기록에만 사용
- 탈락 관전 및 일시적인 연결 종료 중에도 매치가 진행되는 동안 골드 장부 유지
- 재접속 시 누적 인게임 골드와 전투 상태 복구
- 명시적 나가기 또는 매치 종료 시점까지 미복귀한 경우에만 최종 이탈 판정

## Data
```text
CSV/Excel
→ Common JSON
├→ Unity ScriptableObject
└→ Spring Boot
```

## Change Rule
Shared 계약 변경 시:
1. 양쪽 담당자에게 알림
2. Breaking Change 여부 표시
3. 사용처 검색
4. 양쪽 컴파일 확인
5. PR에 마이그레이션 방법 작성
