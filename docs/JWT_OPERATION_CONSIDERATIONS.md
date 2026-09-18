# JWT 운영 전환 고려사항

## 1. 목적

현재 로컬·개발 환경의 Battle Session 참가자 등록 구조를 유지하면서, 실제 운영에서는 JWT 인증과 서버 권위 Matchmaking 결과로 안전하게 교체하기 위한 기준을 기록한다.

## 2. 현재 구현 상태

- 2026-09-18 로컬 데이터 보존: `local` H2 파일 DB + Hibernate `update`, 개발용 비밀번호 계정(BCrypt), DPAPI 보호 고정 JWT 키 실행 스크립트를 추가했다. 운영 DB migration·영속 matchmaking은 별개로 남는다. 자세한 실행/복구 범위는 [로컬 보존 가이드](LOCAL_PERSISTENCE_GUIDE.md)를 따른다.
- 2026-09-18 게스트 로그인·복구, JWT 발급·검증, Refresh 회전·재사용 폐기, 계정 API Principal 검증 및 Unity Windows 클라이언트를 구현했다. 실제 Google/Apple 검증기·모바일 보안 저장소·production 매칭은 미완료다(11절).
- 로컬·개발 환경에서는 Fusion Host가 Battle 참가자 명단을 Spring Boot의 개발 전용 Session Roster 등록 API로 전달한다.
- 개발 전용 API는 `local` 또는 `dev` Spring Profile에서만 생성되며 loopback 요청만 허용한다.
- Profile이 없거나 `prod`/`production`인 경우 개발 전용 API와 자동 사용자 준비 기능은 활성화되지 않는다. `local,prod` 등 혼합 Profile에서도 운영 차단이 우선한다.
- Settlement는 등록된 Session Roster, Map, Balance Version, Content Hash, Player Slot을 검증한 뒤 처리한다.
- 서버가 roster 등록 경로에 따라 `SessionSource`를 `PRODUCTION`, `LOCAL_DEVELOPMENT`, `VALIDATION_FIXTURE` 중 하나로 부여하고 Settlement에 영속한다. 클라이언트는 이 값을 지정할 수 없다.
- Quest 영구 진행은 `PRODUCTION` Settlement만 반영하며 local/dev와 검증 Fixture는 항상 제외한다.
- 동일한 Settlement 요청은 멱등 처리되어 보상이 중복 지급되지 않는다.
- Unity 교체 지점은 `IBattleSessionRosterRegistration` 계약이다. 관련 코드는 `FUTURE_AUTH_REPLACEMENT` 문자열로 검색할 수 있다.

현재 Battle roster 구조는 개발 편의를 위한 신뢰 경계이며 운영 매칭 인증 수단이 아니다. 계정 JWT가 있어도 운영에서 클라이언트가 임의의 `playerId`, 참가자 또는 Slot을 권위 있게 등록하게 해서는 안 된다.

## 3. 운영 JWT 적용 시 교체할 부분

### Spring Boot

1. 로그인·Access Token·Refresh Token 발급 API를 구현한다.
2. Spring Security Filter에서 `Authorization: Bearer <token>`을 검증하고 JWT Subject를 서버의 User ID에 연결한다.
3. 운영용 Matchmaking/Session Authority가 두 참가자, Slot, Map, Fusion Session을 확정한다.
4. 운영용 `JwtMatchmakingSessionRosterAdapter`가 위 Authority 결과를 Settlement 검증에 제공한다.
   이 Adapter만 roster를 `SessionSource.PRODUCTION`으로 등록할 수 있어야 한다.
5. 요청 DTO의 username이나 playerId를 신뢰하지 않고 인증 Principal 및 서버 Session 기록과 대조한다.
6. 개발 전용 Session Roster 등록 Controller와 `dev-*` 사용자 자동 생성은 운영 Profile에서 계속 fail-closed 상태를 유지한다.

### Unity

1. 로그인 결과의 Access Token을 메모리에 보관하고 모든 보호 API에 Bearer Header를 붙인다.
2. Refresh Token이 필요하면 PlayerPrefs나 평문 파일이 아닌 OS 보안 저장소를 사용한다.
3. `IBattleSessionRosterRegistration`의 운영 구현인 `JwtMatchmakingRosterRegistrar`를 추가한다.
4. 현재 Factory가 개발 구현 대신 운영 구현을 선택하도록 환경별 Composition Root에서 주입한다.
5. 재접속 시 Token 갱신과 Matchmaking Session 복구를 먼저 수행한 후 Battle 상태를 복원한다.

## 4. 토큰과 비밀정보 운영 기준

- JWT 서명 키, DB 비밀번호, 운영 API Secret은 Git에 커밋하지 않는다.
- 배포 환경의 Secret Manager 또는 CI/CD Secret에서 주입한다.
- Access Token은 짧은 만료 시간을 사용한다.
- Refresh Token은 회전·폐기·탈취 대응 이력을 서버에서 관리한다.
- 서명 키에는 `kid`를 사용해 무중단 회전을 지원한다.
- 운영 통신은 HTTPS만 허용한다.
- 로그, 예외, 분석 이벤트에 전체 Token이나 Secret을 출력하지 않는다.
- Photon App ID처럼 클라이언트에 포함되는 공개 식별자와 JWT 서명 키 같은 비밀정보를 구분한다.

## 5. 인증·네트워크 오류 정책

- `401 Unauthorized`: Token 누락, 만료, 서명 오류.
- `403 Forbidden`: 인증은 성공했지만 해당 Session/Player 권한이 없음.
- 자동 재시도는 Token 갱신 성공 후 안전한 조회 또는 멱등 요청에만 적용한다.
- Settlement 재전송은 동일 `requestId`, `battleSessionId`, `summaryHash`, Payload를 유지한다.
- JWT 갱신 때문에 Settlement 멱등 키를 새로 만들지 않는다.
- Photon User ID와 JWT Subject를 서버가 검증 가능한 방식으로 연결한다.

## 6. 운영 전환 순서

1. Spring Security와 로그인·Token 갱신 기반 구현
2. Unity 공통 HTTP 계층에 Bearer Header 및 401 갱신 처리 추가
3. 서버 권위 Matchmaking/Session Roster 저장 구현
4. `JwtMatchmakingRosterRegistrar`와 `JwtMatchmakingSessionRosterAdapter` 연결
5. 로컬·개발·운영 Profile별 E2E 검증
6. 운영 환경에서 개발 전용 Endpoint가 존재하지 않는지 재확인

## 7. 필수 테스트

- 정상 Access Token으로 보호 API 성공
- 누락·위조·만료 Token 거부
- 다른 사용자의 Session 또는 Slot 위조 거부
- JWT Subject와 Photon User ID 불일치 거부
- Refresh Token 회전 및 재사용 차단
- Profile 미지정 및 `prod`에서 개발 Roster API가 노출되지 않음
- 동일 Settlement 재전송 시 `alreadyProcessed=true`이고 재화 잔액 불변
- Token 갱신 후에도 동일 Settlement 멱등성이 유지됨
- 로그와 Error Response에 Token, 서명 키, 내부 DB 정보가 노출되지 않음

## 8. 현재 개발 구현을 제거하지 않는 이유

로컬 2인 Fusion 검증에는 실제 인증 서버와 Matchmaking이 아직 없으므로 개발 전용 Adapter가 필요하다. 운영 구현은 동일 Interface 뒤에 추가하고 환경별로 선택한다. 이렇게 하면 Battle 및 Settlement 호출부를 다시 작성하지 않고 인증 경계만 교체할 수 있다.

## 9. 2026-09-18 구현 준비 점검

- 개발용 Quest/Daily Content/Session Roster Controller와 local roster Adapter는 `(local | dev) & !prod & !production` 조건으로 제한했다. 익명 Attack Snapshot도 명시적 local/dev와 허용 옵션이 모두 있어야 하며 운영 Profile 혼합 시 거부한다. 11개 Profile 조합과 옵션 true/false 회귀를 포함해 서버 403/403, BalanceTool 82/82 PASS 및 독립 리뷰 PASS.
- 이 준비 점검 당시 로그인·JWT·매칭은 미구현이었다. 후속 계정 인증 구현은 11절을 따른다. 위 개발 경계 보완만으로 production 인증이 완료된 것은 아니다.
- 로그인 진입은 게스트 시작 + Google/Apple 연동, 전투는 기존 유저 Host 유지로 사용자 확정했다. 전용 서버 전환은 추후 운영 테스트 후 판단한다(10절). 서비스 제공자 계정·Client ID·배포 주소·Secret은 실제 연동 단계에서 별도 설정이 필요하다.
- 현재 사용자 조회/상점/강화/교배 등 username 기반 API를 보호할 때는 JWT Header만 추가하지 말고 인증 Principal의 사용자에 귀속시켜야 한다. User 엔티티를 그대로 반환하는 경로에서 password 등 내부 필드가 노출되지 않도록 공개 DTO도 분리해야 한다.
- 현재 Fusion 입장 identity는 문자열 식별자이므로 JWT Subject와 검증 가능한 Match 입장 Ticket/Photon identity 연결이 필요하다. 클라이언트가 전달한 두 사용자 목록만으로 PRODUCTION roster를 등록하지 않는다.
- 현재 메모리 roster 저장은 운영 재시작·다중 인스턴스 복구를 보장하지 않는다. 운영 Match/roster와 입장 Heart 예약·반환을 영속 상태 및 멱등 처리로 연결해야 한다.
- 기본 H2/create-drop 설정과 테스트 데이터 초기화는 운영 DB 설정 및 migration과 분리해야 한다. 비밀정보가 들어 있는 실제 운영 설정은 Git에 추가하지 않는다.
- **JWT는 참가자 신원 검증이지 전투 결과 진실성 증명이 아니다.** 유저 Host 방식이면 인증된 Host의 결과 조작 위험이 남는다. 이 한계를 없애려면 신뢰 가능한 전투 실행/검증 구조가 추가로 필요하며, 전용 서버 전환은 별도 Battle 범위 협의가 필요하다.
- 운영 Adapter가 준비되지 않은 상태의 fail-closed 동작은 유지한다. 실제 제공자 로그인 및 production 2클라이언트 검증을 로컬 Fixture 성공으로 대체하지 않는다.

## 10. Host 유지 및 전용 서버 재평가 정책 — 2026-09-18 사용자 확정

- 당장은 기존 Photon Fusion 유저 Host 방식으로 개발·검증한다. 전용 서버 임대/도입을 진행하거나 출시 필수 조건으로 확정하지 않는다.
- 운영 테스트에서 다음을 측정하고 사용자에게 근거와 비용을 제시한 뒤 전환 여부를 다시 결정한다.
  1. 실제 Android/iOS의 Wi-Fi/모바일망 연결 성공률·지연·끊김.
  2. Host 앱 백그라운드 전환·종료·연결 끊김 시 상대방의 복구/종료/입장 재화 반환 및 정산 중복 방지.
  3. 인증된 Host가 전투 결과를 조작할 수 있는 잔여 위험과 계정 경제에 미치는 영향.
  4. 전용 서버 시험 빌드의 최대 부하 Wave 기준 방당 CPU/메모리/대역폭 및 서버당 수용 방 수.
  5. 실제 접속 패턴을 반영한 전투 서버 + Photon + API/DB/백업/로그 비용. 대화의 가정 예시는 확정 견적이나 측정 결과가 아니다.
- 로그인/JWT, 영속 Matchmaking roster, 사용자·Slot·mapId 검증, 서버 영구 재화와 정산 검증은 전용 서버 결정과 독립적으로 진행한다.
- 인증된 유저 Host와 운영자가 관리하는 전용 서버를 같은 수준의 전투 신뢰 주체로 간주하지 않는다. JWT·hash 검증만으로 Host 조작이 차단된다고 표현하지 않는다.
- 계정 진입 정책(게스트 우선/로그인 필수), 외부 로그인 Provider, 게임 계정 ID, 매칭 참가자, 전투 실행 주체의 구현 경계를 분리한다. 향후 진입 정책/전투 호스팅 변경 시 계정·재화 데이터와 Settlement 계약의 불필요한 재작성을 피한다.
- 전용 서버로 바꾸기로 결정하면 User/System·Shared·Battle의 후속 범위와 운영 예산을 별도 승인받는다. Host 유지 결정은 production 인증/매칭 구현 완료 또는 운영 배포 승인이 아니다.

## 11. 계정 인증 1차 구현 및 운영 잔여 작업 — 2026-09-18

### 현재 구현

- 서버 `/api/auth/guest`, `/refresh`, `/logout`, `/me`, `/providers`, `/challenges`, `/external`, `/links` 경계를 추가했다. 마지막 네 경로도 `/api/auth` 아래다.
- 게스트 복구 비밀·opaque Refresh Token은 서버에 hash로 저장한다. JWT는 issuer/audience/서명/만료/세션 등과 계정 Principal을 검증한다. Refresh 회전 중 재사용을 발견하면 family를 폐기한다.
- 상점·강화·교배 등 계정 API는 요청 username만 신뢰하지 않고 Principal 사용자와 대조한다. 내부 User password는 공개 응답에서 제외했다.
- Unity는 보호 요청에만 Bearer를 붙이고 갱신을 단일 실행으로 합친다. 401 후 자동 재전송은 GET에 한정하며 경제 POST를 무작정 반복하지 않는다.
- Access Token은 메모리, 게스트 복구 키·Refresh Token은 Windows DPAPI에 보관한다. 개발 profile/API 주소별 분리와 동시 파일 잠금을 적용했다.
- Google/Apple challenge·nonce·LINK 충돌 경계와 교체 인터페이스만 준비했다. 실제 네이티브 SDK/서명 검증기가 없으면 unavailable/503이며 성공을 가장하지 않는다.
- 서명은 현재 단일 HS256 키와 고정 kid `account-v1`을 사용한다. 운영 Secret은 외부 주입이며 자동 임시 키는 명시적 개발 환경에 한정한다. kid 존재만으로 무중단 다중 키 회전이 구현됐다고 해석하지 않는다.

### 운영 전 반드시 해야 할 것

1. Google/Apple 개발자 설정 및 audience/Bundle ID 확정, 네이티브 SDK와 공식 공개키 기반 토큰 검증기 연결. 토큰 payload decode만으로 인증하지 않는다.
2. iOS Keychain/Android Keystore 저장소 구현 및 실제 기기 복구/업데이트/백그라운드 검증. 현재 모바일은 평문 저장 우회 없이 차단한다.
3. 기본 H2/create-drop에서 운영 영속 DB와 migration으로 전환하고 auth session/guest/external identity 유일성·트랜잭션·백업·복구를 검증한다. 개발 DB 재시작은 계정 보존을 보장하지 않는다.
4. HTTPS/API 배포 설정, Secret 관리, 신규·이전 키를 병행하는 서명 키 회전, 로그인/갱신 rate limit, 만료 session 정리·보안 감사를 추가한다.
5. 서버 권위 영속 matchmaking roster, 참가자/Slot/mapId/Photon session 고정, 입장 Ticket/Photon identity 검증과 Heart 멱등 예약·반환을 연결한다. 클라이언트 JWT는 이 구현의 대체물이 아니다.
6. 운영 Adapter 미구성 fail-closed 유지, 보호 API 및 dev endpoint 차단 회귀, 실제 Host/Client production 경계 E2E를 수행한다.
7. 계정 삭제·탈퇴, 로그아웃/전환 UI, 제공자 연결 해제/계정 복구 정책과 스토어 요구사항을 검토한다. 서버 logout API만으로 이 사용자 흐름이 완료되는 것은 아니다.

현재 유저 Host 방식 유지. 로그인은 참가자 위조 방어이며 Host의 거짓 전투 결과를 증명·차단하는 전용 서버가 아니다.

검증은 Spring 437/437, BalanceTool 82/82, Unity 664/664 및 Windows local 게스트 복구·동시 갱신까지다. 상세 제한과 증거는 [인증 검증 기록](test-reports/AUTH-STARTUP-2026-09-18.md), UI/구현 교체 지점은 [교체 가이드](AUTH_STARTUP_UI_GUIDE.md)를 따른다.
