# AI 협업 워크플로우

## 역할
### 사용자
- 기획, 게임 규칙, 데이터, Spring, User/System Unity 로직

### 동료
- 전투 맵, Scene/Prefab, Monster/Boss/Wave, Physics/Projectile/Effect

### Gemini Pro
- 저장소 탐색
- 반복 구현
- Unity MCP 조작
- Editor Tool
- ScriptableObject 생성기
- 코드 수정과 컴파일 오류 분석

### GPT
- 아키텍처와 기획 검토
- Fusion 권한 검토
- PR 리뷰
- 밸런스 설계

## 공통 흐름
```text
기획 확정
→ feature 브랜치 생성
→ 문서 읽기
→ 분석만 요청
→ 작업 계획 검토
→ 구현
→ 컴파일/테스트
→ AI 리뷰
→ 사람 리뷰
→ PR
→ dev 병합
```

## 세션 원칙
- 기능 하나당 새 세션 권장
- 한 번에 큰 기능을 시키지 않음
- PR 하나당 하나의 주제
- 전체 프로젝트 설명 대신 문서 경로 전달

## Unity MCP 원칙
- 가능하면 MCP로 Unity Editor 직접 조작
- MCP가 없으면 Editor Tool 생성
- `.unity`, `.prefab`, `.meta`, GUID 직접 작성 금지
- 작업 후 사람이 시각 검증
