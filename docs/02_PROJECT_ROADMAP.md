# 프로젝트 로드맵 V1.1

## Phase 0. 문서 및 안전장치
- AGENTS.md 루트 배치
- docs 기준 문서 확정
- User/Battle 역할 분리
- feature 브랜치 사용
- Unity .meta 검사

## Phase 1. 기존 프로토타입 규칙 교정
목표: Fusion 이전에 게임 규칙 자체를 맞춘다.

작업:
- 4x6 통일
- 24칸 제한
- 같은 종 Merge
- 다음 등급 전체 풀 랜덤
- Kidnap 99.5/0.5
- 순차 빈칸 배치
- 누적 Kidnap 비용
- Mutation Injector 그리드 객체
- Pending Mutation 계승
- 빈칸 드래그 이동

## Phase 2. 데이터 파이프라인
- CSV 스키마
- JSON 변환
- Validation
- ScriptableObject 생성기
- Spring Loader
- 테스트 데이터:
  - Normal 2종
  - Epic 2종
  - Mythic 1종
  - Mutation 2종
  - Injector 2종

## Phase 3. Mythic Mutation
- PendingMutationType
- ActiveMutationType
- MutationRerollCount
- 확정 Mutation
- 랜덤 Mutation
- 재변이 비용 증가
- 공통 외형 변형
- 공통 스탯 배율
- 공통 메커니즘

## Phase 4. Battle 분리
### User
- StatCalculator
- Kidnap
- Merge
- Mutation
- Gold
- Data

### Battle
- Monster
- Boss
- Wave
- Targeting
- Projectile
- Hit
- Effect
- Shared Lane

## Phase 5. Photon Fusion
- `[Networked]` battle state
- RPC request flow
- State Authority validation
- Boss TickTimer
- player elimination/spectating
- session balance version pinning

## Phase 6. 콘텐츠 확장
- 36종 Alien
- 4종 Mythic
- 8종 Mutation
- Monster
- Boss
- Wave
- Lobby/Shop/Collection UI

## 첫 번째 PR 권장 범위
브랜치:
`feature/user-core-rule-v1`

포함:
- 4x6
- 같은 종 Merge
- 다음 등급 랜덤
- 순차 빈칸 배치
- 누적 Kidnap 비용

제외:
- Photon Fusion
- Mutation Injector 전체 구현
- Mythic 스킬
- 대규모 DB 마이그레이션
