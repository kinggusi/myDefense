# 프로젝트 로드맵 V1.1

## Phase 0. 문서 및 안전장치
- AGENTS.md 루트 배치
- Ownership 문서 확정
- User/Battle 영역 분리
- feature 브랜치 사용
- Unity `.meta` 검사

## Phase 1. User Core Rule
- 4x6 통일
- 24칸 제한
- 같은 종 Merge
- 다음 등급 전체 풀 랜덤
- Kidnap 99.5 / 0.5
- 순차 빈칸 배치
- 누적 Kidnap 비용
- Mutation Injector 모델
- Pending Mutation 계승
- 빈칸 이동

## Phase 2. Data Pipeline
- CSV 스키마
- JSON 변환
- Validation
- ScriptableObject 생성기
- Spring Loader
- 테스트 데이터: Normal 2, Epic 2, Mythic 1, Mutation 2, Injector 2

## Phase 3. Mythic Mutation
- PendingMutationType
- ActiveMutationType
- MutationRerollCount
- 확정/랜덤 Mutation
- 재변이 비용 증가
- 공통 외형 변형
- 공통 스탯 배율과 메커니즘

## Phase 4. Battle Integration
- DamagePayload 계약 확정
- Target Search, Projectile, Hit, Effect
- Shared Lane, Boss Timer

## Phase 5. Photon Fusion
- `[Networked]` battle state
- RPC request flow
- State Authority validation
- Boss TickTimer
- elimination/spectating
- balance version pinning

## Phase 6. Content
- 36종 Alien
- 4종 Mythic
- 8종 Mutation
- Monster, Boss, Wave
- Lobby / Shop / Collection UI

## 권장 첫 PR
브랜치: `feature/user-core-rule-v1`

포함:
- 4x6
- 같은 종 Merge
- 다음 등급 랜덤
- 순차 빈칸 배치
- 누적 Kidnap 비용

제외:
- Photon Fusion 신규 적용
- Mythic Mutation 전체
- Battle Domain
- 대규모 DB 마이그레이션
