# JWT 운영 전환 고려사항

## 1. 목적

현재 로컬·개발 환경의 Battle Session 참가자 등록 구조를 유지하면서, 실제 운영에서는 JWT 인증과 서버 권위 Matchmaking 결과로 안전하게 교체하기 위한 기준을 기록한다.

## 2. 현재 구현 상태

- 실제 회원 인증용 JWT 발급·검증 계층은 아직 없다.
- 로컬·개발 환경에서는 Fusion Host가 Battle 참가자 명단을 Spring Boot의 개발 전용 Session Roster 등록 API로 전달한다.
- 개발 전용 API는 `local` 또는 `dev` Spring Profile에서만 생성되며 loopback 요청만 허용한다.
- Profile이 없거나 `prod`인 경우 개발 전용 API와 자동 사용자 준비 기능은 활성화되지 않는다.
- Settlement는 등록된 Session Roster, Map, Balance Version, Content Hash, Player Slot을 검증한 뒤 처리한다.
- 동일한 Settlement 요청은 멱등 처리되어 보상이 중복 지급되지 않는다.
- Unity 교체 지점은 `IBattleSessionRosterRegistration` 계약이다. 관련 코드는 `FUTURE_AUTH_REPLACEMENT` 문자열로 검색할 수 있다.

현재 구조는 개발 편의를 위한 신뢰 경계이며 운영 인증 수단이 아니다. 운영에서 클라이언트가 임의의 `playerId`, 참가자 또는 Slot을 권위 있게 등록하게 해서는 안 된다.

## 3. 운영 JWT 적용 시 교체할 부분

### Spring Boot

1. 로그인·Access Token·Refresh Token 발급 API를 구현한다.
2. Spring Security Filter에서 `Authorization: Bearer <token>`을 검증하고 JWT Subject를 서버의 User ID에 연결한다.
3. 운영용 Matchmaking/Session Authority가 두 참가자, Slot, Map, Fusion Session을 확정한다.
4. 운영용 `JwtMatchmakingSessionRosterAdapter`가 위 Authority 결과를 Settlement 검증에 제공한다.
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
