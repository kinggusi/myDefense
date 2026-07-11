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
- 다음 등급 전체 풀 랜덤
- 결과는 드롭 대상 위치에 생성

## Mutation
- PendingMutationType: 모든 등급에서 보유 가능
- ActiveMutationType: Mythic에서만 활성화
- A + B DNA는 둘 중 하나 랜덤 계승

## Damage
```text
StatCalculator
→ DamagePayload
→ Battle hit/collision
→ IDamageable.ApplyDamage(payload)
```

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
