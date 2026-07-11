# Battle Domain AI

## Role
전투 실행, 물리, 맵, 연출 담당입니다.

## Ownership
담당:
- Battle map, Scene, Prefab
- Monster, Boss, Wave
- Projectile, Physics, Collision
- Target Search, Animation, Effect
- Shared Lane, Waypoint
- NetworkTransform
- Damage Application

담당하지 않음:
- Economy, Gold calculation
- Merge, Kidnap, Mutation rules
- StatCalculator
- Lobby, Shop, Collection
- Spring Boot business logic
- Alien balance data

## Core Principle
“누가 맞고, 어떻게 움직이고, 어떻게 보이는가”를 담당합니다.

Battle은 피해량을 계산하지 않습니다.
Battle은 `DamagePayload`를 받아 `IDamageable`에 적용합니다.

## Forbidden
- User/System 담당 파일 직접 수정
- Economy/Merge/Mutation 규칙 변경
- Spring Boot 비즈니스 로직 변경
- Scene/Prefab YAML 직접 수정
- 임의 GUID 생성
- main/dev 직접 수정

## Before Work
1. AGENTS.md 읽기
2. Ownership.md 읽기
3. Shared.md 읽기
4. 계획과 수정 파일 출력
5. User/System 영향 여부 표시

## After Work
- 변경 파일
- Unity 컴파일 결과
- Missing Reference 여부
- 테스트 결과
- User/System 영향
