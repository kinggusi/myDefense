# 환경별 설정 및 배포 가이드

이 문서는 MyDefenseGame의 local/dev/staging/prod 환경을 분리하는 방법과
Photon Fusion App ID 및 Spring Boot 설정을 안전하게 주입하는 방법을 설명한다.

## 1. 기본 원칙

- Git에는 소스 코드와 설정 예시만 저장한다.
- 실제 Photon App ID, DB 비밀번호, JWT Secret, API 토큰은 Git에 저장하지 않는다.
- 개발 PC에는 local/dev 설정만 두는 것을 권장한다.
- 운영 설정은 CI/CD Secret Manager 또는 운영 서버 환경변수로 관리한다.
- Unity 모바일 빌드에는 API URL과 Photon App ID가 들어갈 수 있지만 DB 비밀번호와 서버 Secret은 절대 들어가지 않는다.

## 2. 환경 구분

```text
local   개인 PC에서 Unity와 Spring을 함께 실행
dev     팀 개발 서버와 Dev Photon App 사용
staging 운영과 유사한 사전 검증 환경
prod    실제 스토어 출시 및 운영 환경
```

권장 자산 분리:

```text
local   API localhost:8080, 로컬 H2 또는 개발 DB
dev     Dev API, Dev DB, Dev Photon App
staging Staging API, Staging DB, Staging Photon App
prod    Production API, Production DB, Production Photon App
```

## 3. Git에 저장하는 것과 저장하지 않는 것

Git에 저장:

- Unity/Spring 소스
- `\.env.example`
- `server/config/application-*.yml.example`
- 환경변수 이름과 실행 방법을 설명하는 문서
- 빌드 및 배포 스크립트

Git에 저장하지 않음:

- `\.env`
- `\.local.*`
- 실제 `application-*.yml`
- Photon 실제 App ID
- DB 주소·계정·비밀번호
- JWT Secret, API 토큰, 서명 키

`.gitignore`에는 로컬 및 실제 환경 파일을 제외하는 규칙이 있다.

## 4. Photon Fusion Dev App ID 발급

개발 단계에서는 Photon Dev App을 별도로 만든다.

1. [Photon Dashboard](https://dashboard.photonengine.com/)에 회원가입 또는 로그인한다.
2. `YOUR > APPS > Development`로 이동한다.
3. `CREATE A NEW APP`을 선택한다.
4. Photon SDK에서 `Fusion`을 선택한다.
5. SDK Version에서 `Fusion 2`를 선택한다.
6. 앱 이름을 입력하고 생성한다.
7. 생성된 App ID를 복사한다.

공식 문서: [Create a Photon Fusion AppId](https://doc.photonengine.com/fusion/current/getting-started/appid-instructions)

App ID는 채팅, Git, 문서에 기록하지 않는다. 개발용 App과 운영용 App은 별도로 만든다.

## 5. 현재 개발 단계에서의 사용 방법

현재 API 주소는 다음 우선순위로 읽는다.

1. 실행 인자 `-apiBaseUrl=https://...`
2. 환경변수 `MYDEFENSE_API_BASE_URL`
3. 기본값 `http://localhost:8080/api`

예시(PowerShell):

```powershell
$env:MYDEFENSE_ENV='dev'
$env:MYDEFENSE_API_BASE_URL='http://localhost:8080/api'
$env:MYDEFENSE_PHOTON_APP_ID='발급받은 Dev App ID'
```

그 다음 Unity Editor에서 Play Mode를 실행한다.

현재 코드에는 `RuntimeEnvironmentConfig`와 API URL 외부화가 적용되어 있다.
Photon App ID의 `CustomPhotonAppSettings` 런타임 주입은 Fusion SDK API 확인 후 별도 연결이 필요하다.
그 전까지는 Unity의 `Tools > Fusion > Realtime Settings`에서 Dev App ID를 로컬로 입력할 수 있지만,
변경된 `PhotonAppSettings.asset`은 절대 stage/commit하지 않는다.

## 6. Spring Boot 개발 실행

기본 실행은 local 설정을 사용한다.

```powershell
cd server
\.gradlew bootRun
```

Dev 서버는 외부 설정과 환경변수를 사용한다.

```powershell
$env:SPRING_PROFILES_ACTIVE='dev'
$env:SPRING_CONFIG_ADDITIONAL_LOCATION='optional:file:./config/'
$env:DB_URL='jdbc:...'
$env:DB_USERNAME='...'
$env:DB_PASSWORD='...'
\.gradlew bootRun
```

`server/config/application-dev.yml.example`을 실제 `application-dev.yml`로 복사할 수 있지만,
실제 파일은 Git에 추가하지 않는다.

## 7. 운영 출시 흐름

운영 설정을 개발자 PC에 모두 보관하고 프로젝트 전체를 압축해서 배포하지 않는다.

### Unity 출시 빌드

```text
1. CI가 Git의 특정 커밋을 checkout
2. Production 환경 선택
3. CI Secret에서 Production API URL과 Photon App ID 주입
4. Unity Android/iOS 빌드 생성
5. App Store/Google Play에 빌드 업로드
6. 주입용 임시 파일과 작업 디렉터리 삭제
```

Unity 빌드에 포함될 수 있는 값:

- Production API URL
- Production Photon App ID

Unity 빌드에 포함하면 안 되는 값:

- DB 비밀번호
- JWT Secret
- 내부 API 토큰
- Photon Secret Key

### Spring Boot 운영 배포

```text
1. 운영 서버 또는 CI가 Git 소스를 checkout
2. SPRING_PROFILES_ACTIVE=prod 설정
3. DB_URL/DB_USERNAME/DB_PASSWORD/JWT_SECRET 주입
4. Spring Boot 실행
5. 운영 DB 연결 확인
```

운영 DB 자격증명과 Secret은 서버 프로세스에만 존재하며 Unity 앱에는 전달하지 않는다.

## 8. Dev에서 Prod로 전환하는 방법

코드를 수정하거나 Unity Scene을 다시 만들지 않고 환경값만 바꾼다.

```text
Dev:
  MYDEFENSE_ENV=dev
  MYDEFENSE_API_BASE_URL=https://dev-api.example.com/api
  MYDEFENSE_PHOTON_APP_ID=DEV_APP_ID

Prod:
  MYDEFENSE_ENV=prod
  MYDEFENSE_API_BASE_URL=https://api.example.com/api
  MYDEFENSE_PHOTON_APP_ID=PROD_APP_ID
```

Dev와 Prod의 Photon App, DB, API는 덮어쓰지 말고 동시에 유지한다.

## 9. 스토어 계정 소유권

실제 출시 계정은 동생 사업자 명의로 준비한다.

- Apple Developer/App Store Connect: 사업자 조직 계정
- Google Play Console: 사업자 조직 계정
- Production Photon App: 사업자 또는 회사 조직 계정 권장
- Production DB: 사업자 또는 회사 조직 계정 권장

개발용 Photon/DB 계정은 개인 또는 팀 계정을 사용할 수 있지만,
공개 출시 전에는 운영 자산을 사업자 소유 계정으로 분리한다.

## 10. 현재 구현 상태

- Unity API URL 환경변수/실행 인자: 적용
- Spring 환경별 예시 설정: 적용
- `.env` 및 실제 local 설정 무시 규칙: 적용
- Photon App ID 런타임 주입: 후속 구현 필요
- 실제 운영 Secret 등록: CI/CD 구성 시 진행
- commit/push: 이 문서 작업에서는 수행하지 않음
