# myDefense 현재 코드 기준 개발 가닥

## 1. 현재 코드에서 유지할 것
- Unity의 드래그 머지 UX
- 서버 응답 기반 유닛 배치 흐름
- GameSession의 유닛 ID, 그리드 점유, 이동, 삭제 구조
- 로비 API 조회와 보유 왹져 카드 생성 흐름
- AutoUIBuilder 기반 Editor Tool 방향
- DTO 기반 Unity↔Spring 통신 구조

## 2. 기획과 충돌해 우선 수정할 것

### 필수 수정 1: 4×7 → 4×6
대상:
- Unity GridManager
- Unity GameManager
- Spring GameSession grid
- 좌표 유효성 검사
- 필드 최대 개수 28 → 24

### 필수 수정 2: 머지 규칙
현재:
- 같은 종은 evolutionTargetId 고정 진화
- 다른 종은 다음 등급 랜덤

변경:
- 다른 종은 머지 불가
- 같은 종만 머지
- 결과는 다음 등급 전체 풀에서 랜덤
- evolutionTargetId 제거 또는 사용 중단

### 필수 수정 3: 납치
현재:
- 노말/에픽/유니크 직접 등장
- 빈칸 랜덤 배치
- 비용 고정

변경:
- 노말 99.5%
- 생체주입제 0.5%
- 첫 빈칸 순차 배치
- 납치 횟수에 따라 비용 증가
- 생체주입제도 그리드 객체로 관리

### 필수 수정 4: Prefix 모델
현재:
- prefixType 하나만 존재
- source의 Prefix만 계승
- 임시 SLIME 리스크 존재

변경:
- injectedMutationType
- activeMutationType
- mutationRerollCount
- 양쪽 부모 인자 모두 검사
- A+B는 둘 중 하나 랜덤
- 임시 SLIME 규칙 제거

### 필수 수정 5: 플레이어 상태
현재:
- isGameOver 하나로 전체 상태 처리

변경:
- PlayerBattleState: ACTIVE / ELIMINATED / SPECTATING
- MatchState: RUNNING / CLEARED / FAILED
- 탈락 필드 기존 몬스터 유지
- 탈락 필드 신규 스폰 중단

## 3. 권장 개발 순서

### Phase 0. 안전 장치
- dev 직접 수정 금지
- feature/user-* 와 feature/battle-* 분리
- Unity .meta 누락 검사
- AGENTS.md와 docs 먼저 작성

### Phase 1. 기존 HTTP 프로토타입 규칙 교정
- 4×6
- 납치 확률과 비용
- 순차 빈칸 배치
- 같은 종 머지
- 인자 계승
- 생체주입제 그리드 객체
- 빈칸 이동

목표:
Fusion 이전에 게임 규칙 자체가 정상 동작하도록 함.

### Phase 2. 데이터 기반 구조
- AlienDefinition
- SkillDefinition
- MutationDefinition
- BalanceConfig
- CSV/JSON 스키마
- JSON → ScriptableObject Editor Tool
- 중복 ID 및 필수값 검증

목표:
36종 왹져와 8종 변이를 코드 수정 없이 추가 가능하게 함.

### Phase 3. 신화·생체변이
- 신화 변이 요청
- 확정 변이
- 랜덤 변이
- 재변이 비용 증가
- 공통 스탯 배율
- 공통 메커니즘
- 공통 외형 변형
- 극히 일부 조합만 Override

### Phase 4. 전투 책임 분리
사용자/System:
- StatCalculator
- 납치
- 머지
- 생체변이
- 인게임 골드
- 데이터 파이프라인

동료/Battle:
- 몬스터 이동
- 웨이브
- 타깃 탐색
- 투사체
- 충돌
- 피격
- 이펙트
- 공용 구역
- 보스 타이머

공통 계약:
- DamagePayload
- IDamageable
- ITargetProvider
- HitEvent

### Phase 5. Photon Fusion 이전
Fusion에서 관리:
- 개인 인게임 골드
- 필드 슬롯
- 유닛 위치와 소유권
- 납치/머지/변이 요청
- 웨이브
- 보스 TickTimer
- 플레이어 탈락 상태

Spring Boot에서 관리:
- 로그인
- 계정
- 영구 재화
- 신화 해금
- 로비 뽑기
- 밸런스 JSON
- 전투 결과
- 거래 및 변경 로그

## 4. 첫 스프린트 작업 분배

### 사용자
- 4×6 통일
- KidnapResult DTO
- 순차 빈칸 선택
- 누적 납치 비용
- 같은 종 머지 규칙
- Mutation 인자 모델

### 동료
- 공용 1자 구역 프로토타입
- 몬스터 경로
- 타깃 탐색
- DamagePayload 소비
- 보스 이동 및 타이머 샌드박스

### 공동
- 그리드 좌표 규칙
- DamagePayload 계약
- ScriptableObject 데이터 컬럼
- PR 리뷰
