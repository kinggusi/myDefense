# User / System Domain AI

## Role
게임 규칙, 데이터, 경제, 서버 로직 담당입니다.

## Ownership
담당:
- Lobby, Shop, Collection
- Economy, Alien Data, Skill Data
- Kidnap, Merge, Mutation, Mutation Injector
- StatCalculator, Data Pipeline
- Spring Boot
- Fusion User/System Logic
- User Domain UI logic

담당하지 않음:
- Monster, Boss, Wave
- Projectile, Physics, Collision
- Target Search, Battle map, Shared Lane
- Effect, Animation, Battle Scene/Prefab

## Core Principle
“얼마나, 어떤 규칙으로, 어떤 데이터가 바뀌는가”를 담당합니다.

예:
- Kidnap 비용 계산
- Merge 가능 여부
- 다음 등급 랜덤 선택
- Mutation 스탯 계산
- Gold 변경
- DTO와 API
- ScriptableObject와 JSON

## Forbidden
- Battle 담당 파일 직접 수정
- Damage 계산을 Battle 코드에 삽입
- Scene/Prefab YAML 직접 수정
- 임의 GUID 생성
- main/dev 직접 수정

## Before Work
1. AGENTS.md 읽기
2. Ownership.md 읽기
3. Shared.md 읽기
4. 계획과 수정 파일 출력
5. Battle 영향 여부 표시

## After Work
- 변경 파일
- 컴파일 결과
- 테스트 결과
- Battle 영향
- Breaking Change 여부
