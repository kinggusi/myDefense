# Antigravity + Gemini Pro + GPT Plus 운영안

## 1. 기본 원칙
- Antigravity/Gemini Pro: 저장소 전체 탐색, 반복 구현, Editor Tool, 대량 변경
- GPT Plus: 기획 결정, 아키텍처 검토, PR 리뷰, 위험한 로직 검증
- 동료의 회사 GPT: 최소 사용, 짧은 검증 전용
- 동일 질문을 여러 모델에 반복하지 않음
- 문서와 코드 맥락을 저장소에 남겨 매번 긴 설명을 하지 않음

## 2. 저장소 문서
루트에 다음 문서를 유지:
- AGENTS.md
- docs/00_GAME_DESIGN_V1.md
- docs/01_PROJECT_ROADMAP.md
- docs/02_AI_WORKFLOW.md
- docs/03_SHARED_CONTRACTS.md

AI는 작업 전 AGENTS.md와 해당 도메인 문서만 읽게 함.

## 3. 워크스페이스 분리
- myDefense-user
  - branch: feature/user-*
- myDefense-battle
  - branch: feature/battle-*
- myDefense-integration
  - branch: feature/integration-*

처음에는 user와 battle 두 개만 운영하고 통합 전용은 충돌이 늘어날 때 추가.

## 4. Antigravity 작업 방식
한 작업은 반드시 작은 단위로 요청:
1. 분석
2. 계획과 변경 파일 보고
3. 구현
4. 컴파일/검증
5. diff 요약
6. 커밋 전 사람 승인

금지:
- dev/main 직접 수정
- .unity YAML 직접 편집
- 대규모 자동 리팩토링
- git reset --hard
- git clean -fdx
- 승인 없는 commit/push

## 5. 토큰 절약 전략

### 동료
동료는 GPT에 전체 저장소를 붙여 넣지 않음.
아래만 전달:
- 작업 이슈
- 관련 파일 2~5개
- 공유 계약 문서
- 컴파일 오류
- diff

질문 예:
“이 DamagePayload 계약과 MonsterHitHandler 변경분만 검토해 주세요. 전체 구조 설명은 docs/03_SHARED_CONTRACTS.md 기준입니다.”

### Gemini Pro
적합한 작업:
- 파일 탐색
- 기존 구현 현황 분석
- 반복 코드 생성
- Editor Tool 생성
- ScriptableObject 생성기
- 테스트 뼈대
- 폴더 정리 제안

### GPT Plus
적합한 작업:
- 머지/변이 확률 규칙 검증
- 네트워크 권한 경계 검증
- PR diff 리뷰
- 아키텍처 결정
- 밸런스 모델 설계
- 동료에게 줄 간결한 작업 명세 작성

## 6. 프롬프트 템플릿

### 분석 전용
AGENTS.md와 지정 문서를 읽고 관련 파일만 분석하십시오.
코드는 수정하지 마십시오.
현재 구현, 기획 충돌, 유지/수정/신규 항목을 표로 보고하십시오.

### 구현
작업 브랜치에서 지정 범위만 구현하십시오.
먼저 변경 계획과 예상 파일을 보고하십시오.
씬 YAML은 직접 수정하지 말고 Editor Tool을 사용하십시오.
작업 후 컴파일 오류, 테스트 결과, 변경 파일을 요약하십시오.

### 리뷰
이 PR의 변경 파일과 공유 계약만 검토하십시오.
기획 위반, 네트워크 권한 위반, Unity .meta 누락, 회귀 위험을 우선 확인하십시오.

## 7. 첫 Antigravity 작업
목표:
현재 HTTP 프로토타입의 규칙을 V1 기획에 맞춤.

범위:
- 4×6 통일
- 순차 빈칸 배치
- 같은 종 머지
- 다음 등급 랜덤
- 납치 비용 누적
- Alien/Injector 결과 DTO 초안

제외:
- Photon Fusion
- 전투 물리
- 신화 스킬
- 전체 데이터 자동 생성
- Spring DB 마이그레이션 대규모 변경
