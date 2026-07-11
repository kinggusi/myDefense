# Ownership

## User/System Owner
담당자: 사용자

### Unity
- Lobby, Shop, Collection
- Economy, Alien, Skill
- Kidnap, Merge, Mutation, Mutation Injector
- StatCalculator, Data Pipeline
- Fusion economy/system logic

### Spring Boot
- Authentication, User, Wallet
- Economy, Alien, Skill
- Shop, Gacha, Mythic Unlock
- Balance, Transaction Log
- Persistent Battle Result

## Battle Owner
담당자: 동료

### Unity
- Battle Map, Scene, Prefab
- Monster, Boss, Wave
- Projectile, Physics, Collision
- Target Search, Effect, Animation
- Shared Lane, Waypoint, NetworkTransform

## Shared
공동 합의 후 수정:
- DTO, Enum, Interface
- DamagePayload, IDamageable, ITargetProvider, HitEvent
- GridPosition, Network contract

## Rules
- 다른 담당 영역 파일은 원칙적으로 수정하지 않음
- 수정이 필요하면 먼저 담당자에게 요청
- Shared 변경은 양쪽 컴파일 확인
- PR 설명에 Domain Impact를 반드시 작성
