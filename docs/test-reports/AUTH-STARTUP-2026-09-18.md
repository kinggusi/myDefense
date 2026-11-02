# P1-5-7 로그인 기반·시작 UI 검증 — 2026-09-18

## 판정

**부분 완료. Windows local 게스트/JWT 흐름 검증 완료, 실제 Google/Apple·모바일·production 매칭 및 사람 디자인 인수는 미완료.**

사용자 결정은 게스트 시작 + Google/Apple 연동, 유저 Host 유지다. 전용 서버는 운영 테스트 뒤 재평가한다. 이번 변경은 User/System 계정·로비 영역이며 동료 Battle Runtime을 수정하지 않는다. `feature/user-p1-5-7-auth-matchmaking`의 dev `5322150` 미커밋 merge 상태를 보존했고 추가 커밋/푸시는 하지 않았다.

## 구현 파일 묶음

- `server/.../auth/`: guest credential, JWT, 회전 Refresh Session, replay family 폐기, Principal 검사, 외부 로그인 challenge/link/Verifier 경계.
- 서버 계정 Controller와 공개 DTO: 요청 username이 인증된 사용자와 일치해야 접근, User 내부 password 미노출, local/dev legacy 명시 제한.
- `Client/Assets/Scripts/Auth/`: 인증 세션, Windows DPAPI 저장소, 공통 인증 HTTP, 시작 화면·계정 연동 패널·교체 테마.
- `NetworkManager/HttpManager/LobbyManager/AlienDetailPopup`: 인증 계정 기반 조회와 로비 초기화. 기존 프로필 레이아웃은 수정하지 않음.
- `AuthStartupAssetBuilder`, `AuthClientTests`, Feature Test Hub, Resources/Auth Prefab·Theme, 격리 UI Fixture. Unity API로 생성했고 .unity/.prefab/.meta 직접 편집 없음.
- SampleScene diff는 앞선 missing Sprite 참조 3개 정리 외 추가 Scene 변경 없음. Build Settings 변경 없음.

## 자동검증

- Spring 전체 **437/437 PASS**, BalanceTool **82/82 PASS**; Java compile PASS. 인증 테스트 34개 포함.
- 최종 Unity EditMode **664/664 PASS**, 실패/스킵 0. Job `e0e5eab16c4c44c487d9b6c618a59dd7`.
- 게스트 ID 표시 후속: 표시 분기·실제 ID 변경·기존 폰트 지원 6개 회귀 추가, Unity **670/670 PASS**, 실패/스킵 0. Job `3e0a669da27b40ceb6c49e1686f13237`. 독립 읽기 전용 리뷰 PASS, Scene/Prefab 및 프로필 스타일 추가 변경 없음. 이번 작은 변경은 자동/MCP 읽기 검증이며 전체 로그인 화면 조작을 다시 수행한 것은 아니다.
- 테스트 범위: JWT 위조/만료/Principal, refresh 회전·재사용 폐기, 제공자 미구성/연동 경계, profile 혼합 차단, DPAPI roundtrip/손상 보존/동시 파일 잠금, endpoint 정책, 인증 Prefab 참조와 Fixture 빌드 제외.
- 서버 독립 리뷰 PASS. Unity 독립 리뷰 차단 없음. 연동 실패 후 버튼 복원 경고는 수정 후 재검토했다. 글꼴 문제의 후속 조치는 아래 기록을 따르며 사람 인수는 대기다.

## 실행 검증 — 자동 호출과 실제 클릭 구분

- Unity MCP `Client@0689e9f51f1b7464`, Unity 6000.3.4f1, 프로젝트 `E:/study/MyDefenseGame/Client`.
- 별도 18080 포트, `local`, `codexAuthValidation` 메모리 DB와 별도 credential profile을 사용했다. 기존 8080 데이터/계정은 변경하지 않았다.
- **개발 검증 호출**로 신규 guest→JWT→보호 로비 로딩 성공. 실제 로그인 버튼을 손으로 클릭했다고 기록하지 않는다. 외부 Google/Apple 로그인 창을 자동 조작하지 않았다.
- Play 종료/재진입으로 저장된 게스트의 동일 userId=2 복구 확인.
- 만료 시각을 개발 검증용으로 앞당긴 뒤 보호 GET 3개 동시 실행: 성공 3, 실패 0, token 회전 확인. 비밀 값은 출력하지 않았다.
- **실제 마우스 조작**으로 Quest 업적 탭 전환/닫기, 내 유닛 탭/목록 스크롤, 신화 교배 진입 확인. 기존 서버 500/404 재발 없음.
- 새 런타임 error 0. 기존 프로필 TMP 글꼴에 `게스트` 3글자가 없어 glyph warning 3개 발생. 이후 사용자가 실제 게스트 ID 표시를 승인하여 `Guest-{서버 userId}`로 변경했다. 프로필 디자인/글꼴/레이아웃과 내부 username은 유지한다. MCP에서 기존 폰트의 영문·숫자 지원 및 Guest-2의 필요 크기 92.52×29.80, 기존 영역 260.50×40.92를 확인했다. 로그인 전체 흐름을 재실행한 결과와 혼동하지 않는다.
- 시작 UI/로딩 표시를 1080×1920 Game View에서 확인. 다양한 화면비·노치·실제 Android/iOS 시각 검증은 이번 실행에서 미완료이며 PASS로 기록하지 않는다.

## 화면 증거

- 시작 화면: `Client/Captures/AuthValidation/auth-start.png`.
- 로딩 단계: `Client/Captures/AuthValidation/guest-lobby.png` (파일명과 달리 유닛 정보 로딩 중 캡처이며 최종 로비 완료 캡처가 아님).
- 실제 로비 클릭 결과는 도구의 화면 관찰로 확인; 별도 파일로 저장된 최종 로비 캡처는 없음.

## 남은 것 / 신뢰 한계

1. 최종 디자인 인수, 여러 화면비와 실제 기기 검증. 게스트 글꼴 문제는 승인된 계정 ID 표시로 수정했고 자동 회귀 결과는 후속 기록을 따른다.
2. Google/Apple 네이티브 SDK 및 서버 실제 서명 검증기·제공자 설정. 현재 미구성일 때 unavailable/503으로 차단한다.
3. iOS Keychain/Android Keystore. Windows DPAPI 성공을 모바일 지원으로 해석하지 않는다.
4. 영속 운영 DB/migration, JWT 다중 키 회전, rate limit, session 정리, 계정 삭제·전환 UI 및 운영 HTTPS 구성.
5. production matchmaking roster/입장 Ticket/Photon identity 연결, 실제 인증된 Host·Client E2E. 계정 JWT 완료가 전투 production 경계 완료는 아니다.
6. 네트워크 응답 유실·디스크 쓰기 실패·장시간 복귀의 실제 Unity fault-injection 전 범위는 아직 미실행.

H2 메모리 검증 서버 재시작 시 계정 자료는 사라진다. 영구 저장 검증을 대신하지 않는다. 유저 Host 결과 조작 위험도 JWT만으로 제거하지 않는다.

검증 종료 후 Play를 중단하고 SampleScene을 열어 두었으며 임시 API/profile/legacy 환경 변수는 이전 값으로 복구했다. 테스트 실행 프로세스에 종료 신호를 전달했다. 교체 방법은 [시작 UI 가이드](../AUTH_STARTUP_UI_GUIDE.md)를 참고한다.
