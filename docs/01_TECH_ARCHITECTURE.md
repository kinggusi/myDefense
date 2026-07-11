# 기술 아키텍처 V1.1

## 1. 전체 구조
```text
Unity Client
├─ User/System Domain
├─ Battle Domain
├─ Shared Contracts
├─ Photon Fusion
└─ Local Data Assets

Spring Boot
├─ Authentication
├─ User / Wallet
├─ Alien / Unlock
├─ Gacha / Shop
├─ Balance Data
├─ Battle Result
└─ Transaction Log
```

## 2. 책임 분리
### User/System Domain
게임 규칙과 데이터를 담당한다.
- Lobby, Shop, Collection
- Economy, Alien, Skill
- Kidnap, Merge, Mutation, Mutation Injector
- StatCalculator, Data Pipeline
- Spring Boot API
- Fusion economy/system logic

### Battle Domain
전투 실행과 표현을 담당한다.
- Battle map, Scene, Prefab
- Monster, Boss, Wave
- Projectile, Physics, Collision
- Target Search, Animation, Effect
- Shared Lane, NetworkTransform

### Shared Domain
공동 계약만 둔다.
- DTO, Enum, Interface
- DamagePayload, IDamageable, ITargetProvider, HitEvent
- Network contracts

## 3. Damage Flow
```text
Alien + Skill + Mutation
        ↓
StatCalculator
        ↓
DamagePayload
        ↓
Battle hit/collision
        ↓
IDamageable.ApplyDamage(payload)
```

Battle은 피해 공식을 계산하지 않는다.

## 4. Photon Fusion
Fusion에서 관리:
- 개인 인게임 골드
- 필드 슬롯
- Alien 위치와 소유권
- Kidnap, Merge, Mutation
- Wave, Boss TickTimer
- PlayerBattleState, MatchState

규칙:
- 지속 상태: `[Networked]`
- 일회성 명령: RPC
- State Authority가 최종 검증

## 5. Spring Boot
Spring에서 관리:
- 로그인, 계정, 영구 재화
- Mythic 해금
- Lobby Gacha, Shop
- 밸런스 버전
- 전투 결과
- 거래 및 변경 로그

## 6. 데이터 구조
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
   ├─→ Unity Importer → ScriptableObject 생성/갱신
   └─→ Spring Loader → DB 초기화 또는 캐시 로드
```

## 8. Unity 작업 방식
### MCP 사용 가능
- Antigravity + Gemini가 Unity Editor를 MCP로 조작
- Scene/Prefab 생성, 컴포넌트 연결, Inspector 값 설정
- Unity가 GUID와 `.meta` 관리

### MCP 사용 불가
- AI가 Editor Tool 작성
- 사람이 Unity 메뉴에서 실행

### 금지
- `.unity`/`.prefab` YAML 직접 수정
- GUID 직접 생성
- `.meta` 직접 작성

## 9. 현재 코드 재사용
유지 가능:
- UnitDrag 기본 UX
- 서버 응답 기반 Spawn 흐름
- GameSession의 ID/그리드/이동/삭제 구조
- Lobby API 조회 흐름
- AutoUIBuilder 방향
- DTO 기반 통신

변경 필요:
- 4x7 → 4x6
- 28칸 → 24칸
- 고정 진화 제거
- 다른 종 Merge 금지
- Kidnap 확률 변경
- 순차 빈칸 배치
- 고정 비용 제거
- Prefix 단일 필드 분리
- PlayerState와 MatchState 분리
- 실시간 전투 HTTP 로직을 단계적으로 Fusion으로 이전
