# 로컬 파일 DB · 개발 계정 검증 — 2026-09-18

## 범위와 판정

User/System 로컬 저장 기반 및 Editor 개발 로그인 구현. 운영 DB, production matchmaking, 운영 관리자 기능의 완료 판정이 아니다.

- `local`: H2 파일 DB + `ddl-auto=update`, `sql.init.mode=never`.
- 테스트는 별도 메모리 DB 또는 임시 폴더 파일 DB 사용.
- 개발 계정 `jjangash`, `kingusi` 생성. 요청한 비밀번호는 외부 입력으로만 전달했고 BCrypt로 저장했다. 문서/소스/Git에 원문을 넣지 않았다.
- 관리자 권한이나 재화 추가 지급은 하지 않았다. 기존 계정 재생성/비밀번호 초기화/진행 리셋도 하지 않는다.
- 기존 실행 중 메모리 서버는 작업 시작 시 없었다. 종료된 메모리 DB 자료를 이 변경으로 복구한 것은 아니다.

## 자동 검증

- Spring 전체 **450/450 PASS**, 실패/오류 0.
- BalanceTool **82/82 PASS**. 최신 `bootJar` 빌드 성공.
- Unity 집중 인증 **39/39**, 전체 EditMode **694/694 PASS**. Job `2dcfd2e9cbbd45e98f749af3a4effa09`.
- 파일 DB 두 번 열기: 같은 userId, Gold/Diamond/DNA/GrowthCell, 유닛 레벨·조각, 교배 종료시각, 인증 정보 유지.
- 개발 계정 profile/옵션/loopback 제한, 기존 계정 인수 거부, 재bootstrap 비밀번호·진행 불변 확인.
- 게스트 동시 생성: 양쪽 최초 조회 없음 → 승자 commit → 패배 쓰기를 고정한 회귀 테스트. insert-only 중복 거절 후 같은 승자 계정 반환, 임시 User rollback 확인.
- Local Credential 중복 insert도 기존 소유자/hash 불변 및 임시 User rollback 확인.
- 시작 스크립트 PowerShell 문법 검사 PASS.
- 상속된 Spring 환경변수의 일반/점 표기와 JVM 옵션 주입을 차단한다. `create-drop` 하위 JPA 속성·외부 config·profile include 주입 4종이 프로세스 시작 전에 거부됨을 확인했다.
- 서버/실행 스크립트 및 Unity 개발 로그인 증분은 서로 다른 독립 읽기 전용 리뷰에서 PASS, 필수 수정 0. Unity 생성 YAML/meta의 빈 값 뒤 공백 경고는 직접 수정하지 않았다.

## 실제 프로세스 검증

1. 저장소 `.local/data/mydefense`에 local 서버 시작, bootstrap으로 두 계정 생성.
2. API 로그인 및 보호 `/api/auth/me` 성공: `jjangash` userId=2, `kingusi` userId=3, 둘 다 isGuest=false.
3. 직접 시작한 서버 프로세스를 종료하고 최신 jar로 재시작. bootstrap과 비밀번호 환경값 없이 시작.
4. 재시작 전 Access Token으로 두 `/me` 조회 성공. 저장된 Refresh Token으로 갱신 성공, 동일 userId 유지.
5. 검증용 임시 세션은 DPAPI로만 저장했고 검증 후 해당 임시 파일을 제거했다. DB와 JWT 키는 유지했다.

실제 프로세스 종료는 강제 종료 후 재기동 방식이며 정상 종료 백업 절차의 검증을 대체하지 않는다. 재화·유닛·교배 상태 보존은 격리된 파일 DB 통합 테스트에서 확인했다. 실제 상점 구매 후 재시작 검증은 상점 후속 작업으로 남는다.

## UI 및 후속 제한

- `Tools > MyDefense > Auth > Development Login`: Play 전에 계정 선택, Play 중 직접 비밀번호 입력.
- Editor 명시적 local/dev + loopback만 허용. 개발 계정별 DPAPI 저장소로 기존 게스트와 분리.
- 비밀번호 입력 창의 실제 화면 조작은 미실행. computer-use 지침의 인증 창 자동 조작 금지에 따라 사용자 확인으로 남겼다. API 검증을 화면 클릭 PASS로 기록하지 않는다.
- Standalone EXE 개발 계정 전환 UI, Google/Apple 실제 SDK, 모바일 보안 저장소, 운영 DB/migration, 영속 매칭은 남는다.
- DB/비밀 키/로그는 Git 제외. Photon 설정 및 다른 개인 ProjectSettings 변경은 이번 커밋에서 제외한다.

실행 방법: [로컬 보존 가이드](../LOCAL_PERSISTENCE_GUIDE.md), [인증 UI 가이드](../AUTH_STARTUP_UI_GUIDE.md).
