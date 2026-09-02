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
- `BattleSettlementSummary` Unity/Spring 전송 계약

`BattleSummary`는 Unity 런타임 누적 장부이고 `BattleSettlementSummary`는 Spring 전송 DTO다. 전송 결과는 `VICTORY`, `DEFEAT`, `ABORTED`를 사용하며, nullable `eliminatedWave`를 포함한 JSON 필드는 Spring `BattleSettlementDtos`와 1:1로 유지한다.

Settlement 전 참가자 신뢰 경계는 `IBattleSessionRosterRegistration`이다. local/dev에서는 Fusion State Authority가 loopback 개발 API로 roster를 등록하고, 운영에서는 동일 인터페이스 뒤의 구현을 JWT principal + matchmaking 검증 Adapter로 교체한다. 이 교체는 Settlement Summary나 보상 계산 계약을 바꾸지 않는다. 운영 Adapter가 준비되지 않은 환경은 영구 보상을 fail-closed 처리한다.

서버는 roster 등록 경로에서 `SessionSource`를 직접 확정한다. `PRODUCTION`만 Quest 영구 진행 대상이며 `LOCAL_DEVELOPMENT`와 `VALIDATION_FIXTURE`는 정산·회귀 검증은 가능하지만 계정 Quest 진행을 바꾸지 않는다. 이 값은 클라이언트 Settlement payload에서 받지 않는다. 승인된 Settlement의 Quest 사실은 `(settlementId, userId, questConditionId)` 장부로 정확히 한 번 누적한다.

재접속 `BattleSessionSnapshot` schema v3는 authoritative `mapId`를 포함한다. Shared 계약과 Unity/Spring canonical JSON fixture를 함께 변경해야 하며, Battle State Authority는 서버가 확정한 Session mapId를 Snapshot에 투영하고 재접속 시 불일치를 fail-closed해야 한다.

일일 전투는 일반 행성 Settlement와 분리된 `DailyBattleSessionContext` schema v1을 사용한다. 서버가 발급한 `runId`와 `battleSessionId`, `contentType`, Stage, canonical `mapId`, Balance version/hash를 Unity와 Spring에서 동일 순서로 직렬화한다. `sessionSource`는 이 DTO에 포함하지 않고 trusted 서버 경계에서만 부여한다. Battle은 이 문맥과 `DailyBattleStage` canonical Balance를 검증한 뒤 단일 `Battle.unity`의 Player 1 Board/Lane만 활성화하며, State Authority 외의 결과 제출과 문맥 불일치는 fail-closed한다.

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
- PlayerBattleState, PlayerConnectionState, MatchState

세 상태는 독립적으로 관리한다.

- `PlayerBattleState`: `ACTIVE → ELIMINATED → SPECTATING`
- `PlayerConnectionState`: `CONNECTED ↔ DISCONNECTED`
- `MatchState`: `RUNNING → CLEARED` 또는 `RUNNING → FAILED`
- 명시적 이탈과 매치 종료 시점까지 미복귀한 참가자의 보상 자격은 Settlement 계약에서 별도로 관리한다.

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
- PlayerBattleState, PlayerConnectionState와 MatchState 분리
- 실시간 전투 HTTP 로직을 단계적으로 Fusion으로 이전
