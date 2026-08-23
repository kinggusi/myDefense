# P0/P1 개발 현황 및 협업 인계

> 작성일: 2026-08-23
> 공유 기준 브랜치: `feature/user-p1-mutation-upgrade-balance`
> 선행 기준: `origin/dev` (`27fea14`) + P0 커밋 3개
> 상태 원칙: 구현과 자동 검증은 끝났더라도 사람 검증이 필요한 항목은 `검증 대기`를 유지한다.

## 1. 현재 진행 상황

### P0

- Fusion 2인 Session, PlayerRef/User ID, MatchState 연결 구현 완료
- 양쪽 4×6 보드, 공용 Boss Lane, Networked Wave/Monster/Boss 구현 완료
- State Authority 기반 Kidnap, 개인 Gold, Move/Swap/Merge, Mutation Injector 구현 완료
- Legendary → Mythic 후보 3종, 리롤, 선택 및 SEALED/ACTIVE DNA 처리 구현 완료
- Network Projectile, authoritative Hit/Damage, Kill/Support Kill 장부 구현 완료
- 탈락·관전·FAILED/CLEARED, 재접속 Snapshot, Settlement 전송 기반 구현 완료
- Feature Test Hub와 P0 통합 테스트 시나리오 구현 완료
- 대부분의 항목은 실제 Host/Standalone Client 사람 검증이 남아 `검증 대기`

### P1

- Mutation 활성화·재변이·Injector 교체 및 8종 전투 효과 구현
- Mutation StatCalculator와 canonical Mutation Balance 연결
- 일반/신화 공명 인게임 강화 구현
- 9개 행성, 80 Wave, 10 Wave 간격 Boss 및 행성 배율 구현
- Settlement 영구 보상, 자격 판정, 멱등 Transaction 구현
- P1 통합 테스트 시나리오 작성
- 정상 Settlement E2E는 신뢰된 Session roster adapter(P1-5-7)가 없어 안전 거부 상태

## 2. 확인된 검증 결과

- Unity EditMode: `360/360` 통과
- Spring Server: `296/296` 통과
- BalanceTool: `70/70` 통과
- Battle Scene: Dirty=false, validate issue 0, Missing Script 0, Broken Prefab 0
- 실제 Fusion Host + Standalone Client 동일 Session 참가 확인
- Player 1/2 개별 Kidnap 비용 차감 및 보드 복제 확인
- 로컬/상대 보드 시점 반전 확인
- canonical Attack Snapshot 48종 로드 및 Monster HP 감소 확인
- Host/Client 기능 신규 예외 0

## 3. 사용자 통합 테스트로 남은 항목

1. Standalone Client의 실제 UI 버튼 입력 → RPC → State Authority 전체 경로
2. 양쪽 드래그 이동, Swap, 동일 종·등급 Merge, 동시 요청
3. Legendary 후보 3종, 무료/유료 리롤, 선택, 시간 초과
4. Mutation 활성화·재변이·Injector, 일반/신화 공명 UI와 양쪽 동기화
5. Boss 1마리, Timer, 처치 후 다음 Wave, 시간 초과 FAILED
6. 개인 필드 100마리 탈락, 관전, 양쪽 탈락 FAILED, Wave 80 CLEARED
7. Client 종료·동일 User ID 재접속 후 Board/Gold/Mutation/Mythic/Wave 복구
8. P1-5-7 완료 후 정상 Settlement 지급·중복 요청 E2E

상세 순서는 다음 문서를 사용한다.

- `docs/P0_INTEGRATION_TEST_SCENARIO.md`
- `docs/P1_INTEGRATION_TEST_SCENARIO.md`

## 4. 협업 분담

### jjangash — User/System

- Fusion 경제·규칙: Kidnap, Gold, Merge, Mutation, 공명
- `BattleWaveStateAuthority`의 경제·사용자 상태 부분
- Spring Boot API, Settlement, 영구 보상, Transaction
- Excel → JSON Balance 파이프라인과 Manifest
- P1-5-7의 Spring trusted roster adapter 구현
- P2 Lobby/Shop/Collection/Breeding UI·API

주요 소유 경로:

```text
server/**
balance/**
Client/Assets/Scripts/Shop/**
Client/Assets/Scripts/Ui/**
Client/Assets/Scripts/Battle/Balance/**
Client/Assets/Scripts/Battle/Wave/BattleWaveStateAuthority*.cs
```

### kinggusi — Battle

- Battle Scene/Prefab, 두 필드와 공용 Lane
- Monster/Boss/Wave 이동과 Spawn
- Projectile, Target Search, Hit, Animation, Effect
- Mutation별 시각 효과와 상태 이상 표현
- 관전 카메라와 Battle HUD
- 행성별 난이도 실제 플레이 테스트
- Host/Client Battle 측 P0/P1 통합 검증

주요 소유 경로:

```text
Client/Assets/Scenes/Battle.unity
Client/Assets/Prefabs/Battle/**
Client/Assets/Scripts/Battle/Combat/**
Client/Assets/Scripts/Battle/Monsters/**
Client/Assets/Scripts/Battle/Presentation/**
Client/Assets/Scripts/Battle/Wave/BattleBossPatternRuntime.cs
```

### 공동 합의가 필요한 Shared

- `Client/Assets/Scripts/Shared/Contracts/**`
- `DamagePayload`, `AlienAttackSnapshot`, Battle 상태 Enum
- Settlement DTO/JSON 필드
- P1-5-7 Session roster 전달 계약
- `BattleWaveStateAuthority`에서 Battle 이벤트와 User/System 상태가 만나는 경계

Shared 파일은 한 브랜치에서만 먼저 수정하고, 상대 브랜치는 해당 커밋을 반영한 뒤 작업한다.

## 5. 권장 Git 흐름

1. 현재 브랜치를 원격에 push해 공동 기준점을 만든다.
2. 동료가 현재 브랜치의 diff와 P0/P1 테스트 결과를 리뷰한다.
3. 승인 후 현재 브랜치를 `dev`에 병합한다.
4. 최신 `origin/dev`에서 역할별 새 브랜치를 만든다.
   - User/System: `feature/user-p1-session-roster-settlement`
   - Battle: `feature/battle-p1-effects-playtest`
5. P1-5-7 Shared 계약이 필요하면 User/System 브랜치에서 계약을 먼저 확정·push하고 Battle 브랜치가 반영한다.
6. 두 브랜치 모두 자동 테스트와 독립 리뷰 후 PR을 요청한다.

## 6. 커밋·공유 제외 항목

다음은 로컬 환경 또는 산출물이므로 공동 기준 브랜치에 포함하지 않는다.

- Photon App ID가 들어간 `PhotonAppSettings.asset`
- `Client/Packages/manifest.json`, `packages-lock.json`
- `Client/ProjectSettings/**`의 로컬 환경 변경
- Fusion Demo/Menu unitypackage meta 삭제
- `_localbuild/**`, `Client/_localbuild/**`
- 로컬 스크린샷과 로그

## 7. 다음 우선순위

1. 현재 P0/P1 기준점 push 및 동료 리뷰
2. 사용자의 P0/P1 통합 테스트 수행과 결과 기록
3. P1-5-7 trusted Session roster adapter 구현
4. Settlement 정상 지급 E2E 완료
5. P1 Battle 시각 효과·난이도 플레이 검증
6. P2 행성 선택/콘텐츠 및 미완성 Lobby·Shop 기능으로 이동
