# P3-1-3 클랜 UI 구현·검증 기록

기준일: 2026-11-02. 담당: User/System. 상태: **UI 구현·자동검증·일부 실제HTTP 검증 완료 / 잔여 검증 및 사람 인수 전 부분 완료**. 커밋·푸시하지 않았다.

## 범위

- 기존 로비 클랜 탭에 런타임 `ClanController`/`ClanView`를 연결한다. 생성, 검색/페이지, 상세, 자유 가입/신청/취소, 공지/멤버/관리, 승인/거절, 임명/해제/위임/추방, 설정/탈퇴/해산을 제공한다.
- 기존 네온 컴포넌트와 한글 폰트를 사용한다. 프로필·다른 탭 디자인·Battle/Shared는 변경하지 않는다. SampleScene 직접 편집 없이 기존 LobbyManager 설치/탭 이벤트 두 지점만 연결한다.
- 숫자는 `/clans/policy`와 실제 Status에서 표시한다. 생성 견적이 바뀌면 서버가 무차감 거절한다. 가입·경제·역할 최종 권한은 서버가 갖는다.
- 기존 AuthSession.SendAccount HTTP/JWT 경로를 사용한다. POST 자동 재시도 없이 사용자 명시 재시도만 수행한다.
- 전송 전에 endpoint+userId별 pending JSON/UUID를 저장한다. 응답 유실·401·불명확 오류·깨진 성공 응답은 보존하고 동일 payload를 다시 보낸다. GET은 pending을 지우지 않는다. 저장소 오류는 fail-closed이며 계정/endpoint/세대가 바뀐 늦은 응답을 무시한다.
- 조회 실패 후 새 변경을 차단한다. busy 중에도 팝업 닫기·탭 이동은 가능하며 늦은 응답이 화면을 다시 열지 않는다.
- 서버 응답은 null을 보존하는 Newtonsoft 기반 `ClanJson`으로 읽는다. 중복 key·후행 데이터·과도한 깊이/크기를 거절하고 멤버/신청 원소·ID·중복·권한 형태를 검사한다. 명령 JSON과 pending 원문은 기존 JsonUtility 형식을 유지한다. 이미 설치된 `com.unity.nuget.newtonsoft-json` **3.2.2**를 직접 의존으로 명시했다(MCP의 최소 요구 3.0.2와 구분, 버전 변경 없음). DTO link.xml로 IL2CPP stripping 경계를 명시한다.

## 격리 검증

1. Unity가 올바른 Client 프로젝트이고 Play 중이 아니며 현재 씬 저장 상태가 깨끗한지 확인한다. 두 클랜 fixture 씬을 닫고 깨끗한 SampleScene을 연 상태에서 빌드한다. 열린 fixture 파일을 덮어쓰지 않도록 빌더가 시작 전에 fail-fast 한다.
2. `Tools > MyDefense > Clan > Rebuild UI Assets`를 실행한다. Unity API로 Resources 클랜 prefab과 아래 두 씬을 생성한다. 직접 YAML/meta를 작성하지 않는다.
3. Feature Test Hub의 `P3-1-3-CLAN`은 HTTP 없이 역할/긴 목록/요청 유실 UI를 검사한다. 가짜 전송 결과이므로 실제 서버 E2E로 주장하지 않는다.
4. 실제 HTTP 확인은 Java17 환경에서 server 디렉터리의 `./gradlew clanUiSmokeServer --no-daemon`을 실행한다. test classpath만 쓰며 127.0.0.1:18083·매번 새 H2 메모리 DB·Lv10/100000골드의 게스트 fixture 3개를 준비한다. `CLAN_UI_SMOKE_READY` 로그 후 시작한다.
5. Feature Test Hub의 `P3-1-3-CLAN-HTTP` 씬만 단독으로 열고 Play한다. Editor-only 하네스가 메모리 credential store와 실제 게스트 인증 API를 사용한다. 기존 AuthSession/NetworkManager가 존재하면 보호 중단한다. 실제 개발 계정·개발 DB·로그인 대화창·PlayerPrefs 인증/재시도 저장소는 사용하지 않는다.
6. 하단 테스트 계정1/2/3 전환으로 생성→가입→승인/거절→역할 변경→위임→탈퇴→해산을 검사한다. Fixture 경제는 폐기 가능하며 실제 계정 경제 변경을 허용하지 않는다.
7. 두 테스트 씬은 production Build Settings에 넣지 않는다. Play 종료 후 fixture 서버도 종료하고 SampleScene 복귀 상태를 확인한다.

생성 경로:

- `Assets/Resources/Prefabs/Lobby/Clan/ClanScreen.prefab`
- `Assets/Scenes/Tests/UserSystem/P3_01_03_Clan_Test.unity`
- `Assets/Scenes/Tests/UserSystem/P3_01_03_Clan_Http_Test.unity`

## 현재 결과

- 서버 집중: ClanServiceTest 32/32 + ClanPersistenceConcurrencyTest 9/9 = **41/41 PASS**, 실패/오류 0. 정책 조회, 견적 변경 무차감·재전송, 이전 null-field fingerprint 호환을 추가했다.
- PM 전체 서버 회귀: **552/552 PASS**, Balance **84/84 PASS**. 격리 smoke 서버18083 준비 로그 확인 완료. 이는 실제 UI HTTP 조작 완료와 구분한다.
- UI 자동 테스트 1차: 30개 중 20 PASS/10 FAIL. JsonUtility가 서버 `clan:null`을 빈 중첩 객체로 처리하는 문제가 미가입 상태 진입을 차단한 원인이었다. null 보존 파서/fixture 직렬화로 수정하고 방어 회귀를 추가한 재실행은 **ClanClientTests 39/39 PASS**(job `2afba8e5`)다.
- prefab 저장/재로드 시 private toolbar와 런타임 람다 이벤트가 보존되지 않는 문제도 코드 점검에서 수정했다. toolbar 직렬화와 Attach 시 명시적 core 버튼 재바인딩 및 prefab roundtrip 회귀를 추가했다.
- 한 줄 UI 후속 집중 1차는 48개 중47 PASS/1 FAIL이었다. 실패 stack은 Unity InputField.SetText의 EditMode 전용 조기 return으로, validator가 문자를 제거한 뒤 caret 길이를 clamp하기 전에 UpdateLabel이 실행된 테스트 환경 예외였다. 실제 붙여넣기 Append 경로를 검증하도록 테스트를 보완했으며 생산 코드에 Editor 전용 우회는 넣지 않았다. 추가 GraphicRaycaster→ScrollRect 이벤트 드래그 회귀를 포함한 최종 집중 **49/49 PASS**(job `a8e5bfb0`), 후속 독립 리뷰 승인이다.
- Unity 컴파일 오류0, prefab+격리2씬 생성 완료. 한 줄 UI 후속 전 전체 EditMode(job `adc7d07e`)는795개 중794 PASS/기존 Battle hash1 FAIL이었다. 최종 전체 재실행(job `159c9bb4`)은 **805개 중804 PASS / 기존 BattleCanonicalBalanceTests hash1 FAIL**이다. 동일 실패의 expected는 `1-2c24f2a29fd63b81`, actual은 `1-e37af4a5ff9b7204`이며 이 작업에서 수정하거나 전체 PASS로 보고하지 않는다.
- 독립 코드 리뷰: **승인, 필수 수정0**. 사용자 최종 인수: 대기.
- 실제 UI 전용 Fixture: 상세 열기/닫기, 멤버 탭, 휠 스크롤로 마지막 Guest-129 도달, 클리핑 정상 확인.
- 실제 HTTP Fixture: 생성·가입 신청·승인 **PASS**. 생성 계정 골드100000→50000, 승인 후 회원2명을 실제 UI/API로 확인했다. 나머지 HTTP 흐름은 별도 검증 중이다.
- 실제 UI 이슈: 여러 줄 공지 저장이 서버 `CLAN_INVALID_REQUEST`로 거절됐다. UI 소개/공지 입력은 multiline이지만 기존 `ClanService.hasUnsafeText`는 ISOControl 전체(줄바꿈 포함)를 거절하는 정책과 불일치했다. 사용자에게 방향을 문의한 뒤 **한 줄만 입력하도록 UI 변경** 승인을 받았다. 서버 정책은 유지하고 두 InputField를 SingleLine으로 변경했으며 줄바꿈/제어·방향 문자/마크업 차단 및 붙여넣기·저장 payload 회귀를 추가했다.
- 실제 한 줄 공지 `함께 즐기는 왹져 방위대!` 저장 PASS. 테스트 계정2 버튼을 직접 눌러 MEMBER로 전환한 뒤 동일 공지와 관리 탭 없음 확인. 서버 재조회도 role MEMBER, 공지 동일, pending false였다. 화면 증거 `Clan_HTTP_Member.png`. 이는 한 줄 전용 UI 수정 전 검증 결과이며 수정 후 회귀는 별도다.
- 한 줄 UI 수정 후 실제 HTTP 재검증 **PASS**: 소개 `함께 지구를 지키는 클랜` 입력 후 Return으로 줄바꿈이 생기지 않음을 확인했다. 공지 `오늘도 함께 즐겨요!` 입력·저장 후 테스트 계정2를 직접 눌러 MEMBER 화면에서 두 문구 반영 및 관리 탭 없음을 확인했다. 서버 재조회도 같은 값/pending false였다. 화면 증거 `Clan_HTTP_SingleLine_Verified.png`. 발견한 여러 줄 입력 불일치는 사용자 승인 방향으로 수정·재검증했으며, 전체 클랜 최종 인수와는 구분한다.
- `sky.drag` 위쪽 드래그2회는 목록 위치 변화가 없었다. 후속 읽기 전용 `InputSystem.onAfterUpdate` 관찰 중 `(850,682)→(850,447)` 자동입력을 실행한 결과 로그는 두 프레임뿐이었다: frame28887 `pressed=True, held=True, released=False, pos=(456.54,1322.75)` → frame28888 `pressed=False, held=False, released=True`, 위치 동일. **누름 시점에 이미 최종 좌표였고 held 상태의 이동 프레임이 전달되지 않았다.** 따라서 해당 자동입력이 Unity 드래그 조건을 충족하지 못함을 확인했고, UI 결함으로 간주한 생산 코드 우회는 넣지 않았다.
- 별도 회귀는 실제 GraphicRaycaster의 멤버 Row hit→ScrollRect IDragHandler 라우팅과 Initialize/Begin/후속 held Drag/End 후 content.y 이동을 통과했다. 이는 UI 이벤트 계약 PASS이며 **실사용 마우스·손가락 드래그 인수는 여전히 미검증**이다. 자동입력 진단 결과를 실제 드래그 PASS로 바꾸지 않는다.
- 종료 정리: Play=false, compiling=false, SampleScene dirty=false로 복귀 확인. 임시 `ClanUiSmokeServer` 프로세스 PID25768의 commandline을 확인한 뒤 해당 프로세스만 종료했다. 실제 개발 DB·계정은 변경하지 않았다. `git diff --check` 오류 없음. 전체 회귀 후 Console에 Battle 음성 검증용 expected error 로그가 남아 있으므로 Console 전체 오류0으로 주장하지 않으며, 테스트 실패는 위 기존 hash1건이다.

## 남은 검증과 제한

- 모바일 비율별 겹침/클리핑/버튼 크기/긴 목록 스크롤/한국어 폰트, 기존 프로필과 다른 탭 회귀.
- 실제 HTTP에서 생성비 차감·가입 신청·승인·한 줄 소개/공지·MEMBER 관리 탭 미노출까지 확인했다. **자유 가입, 신청 취소/거절, 부클랜장 임명/해제, 위임, 추방, 탈퇴, 해산의 실제 버튼→HTTP 전체 흐름 및 동시 변경 후 stale revision·네트워크 유실 재시도 조작은 잔여**다. 이들의 서버/클라이언트 자동 테스트 PASS를 실제 사용자 조작 전체 PASS로 바꾸지 않는다.
- 계정별 pending은 로컬 PlayerPrefs이므로 암호화 비밀 저장소는 아니다. JWT/refresh/비밀번호는 이 저장소에 저장하지 않는다. 변조된 값도 서버 principal·멱등·권한 검증을 통과해야 한다.
- 운영 DB migration/운영 인증·매칭/클랜 N+1 조회 최적화는 기존 후속 범위를 유지한다. 이전 Unity Battle hash 실패의 마감이나 재검증을 이 작업으로 대신하지 않는다.
