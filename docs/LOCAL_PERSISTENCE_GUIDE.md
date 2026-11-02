# 로컬 데이터 보존과 개발 계정

## 저장 범위

`local` 프로필은 H2 파일 DB와 `ddl-auto=update`를 사용한다. 서버 종료 후에도 계정, 재화, 보유 유닛/강화, 교배, 퀘스트/정산 장부, 인증 Refresh Session 등 **DB에 저장되는 상태**가 남는다. Fusion 전투 상태나 현재 메모리 기반 Battle roster까지 자동으로 영속화하는 변경은 아니다.

- 기본 경로: 서버 실행 디렉터리의 `.local/data/mydefense.mv.db`
- 명시 경로: `MYDEFENSE_LOCAL_DB_PATH`에 확장자를 제외한 절대 경로 지정
- `Server/`를 작업 디렉터리로 고정하거나 명시 경로를 지정한다. 서로 다른 경로로 실행하면 별개 DB가 생성될 수 있다.
- 기존 메모리 DB는 URL 변경만으로 자동 이전되지 않는다. 실행 중인 메모리 DB를 보존해야 한다면 종료 전에 별도 export/검증 계획이 필요하다.
- 개발용 초기화는 기존 계정의 재화나 진행을 리셋하지 않는다. 신규 일반 계정 지급 규칙은 변경하지 않는다.

## 개발용 아이디·비밀번호 로그인

Windows에서는 저장소 루트에서 `./scripts/Start-LocalServer.ps1`로 실행한다. 이 스크립트는 작업 경로와 DB를 저장소 루트의 `.local/data/mydefense`로 고정하고 JWT 키를 현재 Windows 사용자 DPAPI로 보호한다. 최초 계정 생성 시에만 `-BootstrapAccounts`를 추가하고 숨김 비밀번호 입력을 사용한다. Java가 PATH에 없으면 `-JavaPath 'C:/Program Files/Java/jdk-17/bin/java.exe'`를 지정한다. 먼저 서버 `bootJar` 빌드가 필요하며 이미 사용 중인 포트의 프로세스는 자동 종료하지 않는다.

상속된 `SPRING_*`/`spring.*` 환경 설정이나 JVM 옵션 주입 변수는 데이터 초기화 설정을 우회할 수 있어 실행 전에 거부한다. 오류가 나면 해당 환경값의 용도를 확인하고 명시적으로 제거한 뒤 실행한다. 스크립트가 임의로 부모 환경 설정을 지우지는 않는다.

운영 계정 정책은 게스트 시작 + Google/Apple 연동을 유지한다. 이번 경로는 PC 로컬 검증용이며 관리자 기능이나 무제한 재화를 부여하지 않는다.

- API: `POST /api/dev/auth/login`, 요청 필드 `username`, `password`
- 기존 JWT/Refresh/로그아웃 경로를 재사용한다. 개발 계정은 `isGuest=false`이며 사용자명을 표시한다.
- `(local 또는 dev) AND NOT prod AND NOT production`에서만 컴포넌트가 생성된다.
- 기본 비활성화. `mydefense.auth.local-accounts.enabled=true`를 명시해야 한다.
- 요청은 loopback 주소만 허용한다. 원격 기기 로그인을 위해 이 경계를 완화하지 않는다.
- 자격 정보는 별도 테이블에 BCrypt hash로만 저장한다. 원문 비밀번호는 코드·문서·Git·실행 로그에 기록하지 않는다.

최초 생성 설정:

| 설정 | 역할 |
|---|---|
| `mydefense.auth.local-accounts.enabled` | 로컬 로그인 활성화 |
| `mydefense.auth.local-accounts.bootstrap-enabled` | 시작 시 신규 개발계정 생성; 기본 꺼짐 |
| `mydefense.auth.local-accounts.usernames` | 쉼표로 나눈 사용자명 |
| `mydefense.auth.local-accounts.password` | 최초 생성용 비밀번호; 외부 비밀설정/환경에서만 전달 |

Spring 환경 변수 표기에서 점은 밑줄로, 하이픈은 제거한다. 예: `MYDEFENSE_AUTH_LOCALACCOUNTS_ENABLED`, `MYDEFENSE_AUTH_LOCALACCOUNTS_BOOTSTRAPENABLED`.

이미 개발 자격이 등록된 이름은 다시 실행해도 비밀번호·재화·진행을 변경하지 않는다. 자격 없이 같은 이름의 기존 계정만 있으면 자동 인수하지 않고 시작을 중단한다. 비밀번호 변경·기존 계정 이전은 별도 승인된 절차가 필요하다. 최초 생성 이후 bootstrap과 비밀번호 입력값을 제거해도 로그인은 저장된 hash로 동작한다.

## JWT와 재시작

`mydefense.auth.signing-key-base64`를 외부 비밀설정으로 고정하면 기존 Access Token도 만료 전까지 재시작 후 검증된다. 최소 32바이트의 무작위 키를 사용하고 Git에 넣지 않는다. 미설정 local/dev는 매 실행 무작위 키이므로 기존 Access Token은 무효화되지만, 저장된 Refresh Token으로 동일 계정의 새 토큰을 받을 수 있다.

## 테스트와 백업

- `src/test/resources/application-local.yml`은 메모리 DB + `create-drop`으로 격리한다. 일반 테스트가 실제 로컬 파일 DB를 열면 안 된다.
- 재시작 전용 테스트는 임시 폴더의 파일 DB를 두 번 열어 같은 계정 ID, 재화, 유닛 레벨/조각, 교배 종료시간, 인증 정보를 비교한다.
- 실제 구매·강화·교배 후 서버 재시작과 재조회로 저장을 추가 확인한다. UI 표시 확인만으로 영속성을 판정하지 않는다.
- H2 파일은 서버 한 프로세스에서만 연다. 별도 프로세스를 같은 DB 경로로 실행하거나 잠금 파일을 지워 강제로 열지 않는다.
- 백업은 서버를 정상 종료한 뒤 DB 파일을 날짜별로 복사한다. 실행 중인 파일 단순 복사는 일관된 백업을 보장하지 않는다.
- DB/백업/인증키/개발 비밀번호는 커밋하지 않는다.

## 운영 제한

이 변경은 **로컬 저장 기반**이며 운영 DB 완성이 아니다. H2 파일 + Hibernate update를 운영 마이그레이션으로 사용하지 않는다. 운영 Driver, schema migration, 백업/복구, 접근 제어, 배포 및 영속 matchmaking roster는 별도 구현·검증한다. [운영 Migration 정책](DATABASE_MIGRATION_POLICY.md)을 따른다.
