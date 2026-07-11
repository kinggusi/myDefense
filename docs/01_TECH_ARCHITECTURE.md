# 기술 아키텍처 V1.1

## 1. 전체 구조
```text
Unity Client
├─ Lobby / Shop / Collection
├─ Battle Presentation
├─ Photon Fusion Realtime State
└─ Local Data Assets

Spring Boot
├─ Authentication
├─ User / Wallet
├─ Mythic Unlock
├─ Gacha
├─ Balance Data
├─ Battle Result
└─ Transaction Log
```

## 2. 실시간과 영구 데이터 분리
### Photon Fusion
- 개인 인게임 골드
- 필드 슬롯
- Alien 위치와 소유권
- Kidnap 요청 및 결과
- Merge 요청 및 결과
- Mutation 요청 및 결과
- Wave 진행
- Boss TickTimer
- 플레이어 상태

### Spring Boot
- 로그인
- 계정
- 영구 재화
- Mythic 해금
- Lobby Gacha
- 밸런스 버전
- 전투 결과
- 거래/변경 로그

## 3. Fusion 규칙
- 지속 상태: `[Networked]`
- 일회성 명령: RPC
- 모든 핵심 요청은 State Authority가 검증
- Client는 상태를 직접 확정하지 않음

## 4. 도메인 책임
### User/System
- Alien
- Economy
- Lobby
- Shop
- Kidnap
- Merge
- Mutation
- StatCalculator
- Data Pipeline
- Spring API

### Battle
- Monster
- Boss
- Wave
- Projectile
- Physics
- Collision
- Effect
- Shared Lane
- Target Search

### Shared Contract
- `DamagePayload`
- `IDamageable`
- `ITargetProvider`
- `HitEvent`
- 공통 DTO
- 공통 Enum

## 5. Damage Flow
```text
Alien + Skill + Mutation Data
        ↓
StatCalculator
        ↓
DamagePayload
        ↓
Battle hit/collision
        ↓
IDamageable.ApplyDamage(payload)
```

Battle 코드에서 피해 공식을 중복 계산하지 않는다.

## 6. Unity 데이터 구조
권장 ScriptableObject:
- AlienDefinition
- SkillDefinition
- MutationDefinition
- MutationInjectorDefinition
- BalanceConfig

런타임 상태:
- AlienRuntimeState
- PendingMutationType
- ActiveMutationType
- MutationRerollCount
- OwnerPlayerRef
- GridPosition

## 7. 데이터 파이프라인
```text
Excel/CSV
    ↓
Validation
    ↓
Common JSON
   ├─→ Unity Editor Tool
   │     └─ ScriptableObject 생성/갱신
   └─→ Spring Loader
         └─ DB 초기화 또는 밸런스 캐시
```

## 8. Unity Editor Tool 원칙
AI는 `.unity`, `.prefab`, `.meta`를 직접 작성하지 않는다.

AI가 작성하는 것:
- SceneBuilder.cs
- PrefabBuilder.cs
- DataImporter.cs
- Validator.cs

사람이 실행하는 것:
- Unity 메뉴 클릭
- 생성 결과 시각 확인
- Inspector 미세 조정
- Missing Reference 확인

## 9. 현재 코드에서 유지할 부분
- UnitDrag 기본 UX
- 서버 응답 기반 Spawn 흐름
- GameSession의 ID/그리드/이동/삭제 구조
- Lobby API 조회 흐름
- AutoUIBuilder 방향
- DTO 기반 통신

## 10. 현재 코드에서 변경할 부분
- 4x7 → 4x6
- 28칸 → 24칸
- 고정 진화 제거
- 다른 종 Merge 금지
- Kidnap 확률 변경
- 순차 빈칸 배치
- 고정 비용 제거
- Prefix 단일 필드 분리
- PlayerState와 MatchState 분리
- HTTP 실시간 전투 로직을 단계적으로 Fusion으로 이전
