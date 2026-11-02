# P2-7 튜토리얼 User/System 1차 검증

> 날짜: 2026-10-15
> 브랜치: feature/user-p2-7-tutorial
> 판정: 부분 완료 — 연습 Battle Adapter·완주 E2E·최종 사람 인수 대기

## 구현 범위

- JWT principal-only GET/command API, 계정별 진행·runId·revision 영속화.
- requestId/payload 장부, 계정 잠금, 단계 순서 검증, 신화 1·2 missing-only 원자적 보상.
- 전투 탭 선택 진입, 안내/닫기/나중에 하기/재개/조회 복구, 강화 비소비·교배 위치 안내.
- User/System 주입 경계 `TutorialPracticeVerifier`, `ITutorialPracticeLauncher`. 기본 미연결 상태는 진행을 보존하고 안전하게 거절.
- Battle·Shared 공용 코드·Balance·기존 프로필 변경 없음. 개발 HTTP 완료 우회 없음.

## 자동 검증

| 검증 | 결과 |
|---|---|
| TutorialServiceTest | 15/15 PASS: 인증·principal·순서·unavailable·run binding·request replay·기보유 보존 |
| TutorialPersistenceTest | 3/3 PASS: 임시 파일 H2를 Spring context 종료/재시작 3회, 2스레드 동일 claim, 두 번째 보상 실패 시 첫 insert/상태/장부 rollback |
| TutorialClientTests 최종 집중 | 24/24 PASS: UI 표시·기본 Launcher 차단·busy 닫기·불명 요청 안내·Build 제외, 계정/세대/endpoint 경계, 실제 Controller 닫힌 창 응답 복구/pending 해소·타계정 응답 pending 보존 |
| Unity MCP | E:/study/MyDefenseGame/Client, SampleScene clean/Play false 확인. 컴파일 오류 0. Editor API로 Fixture 생성 후 SampleScene 복원 |
| 전체 서버 회귀 + bootJar | 511/511 PASS, failures/errors/skipped 0. Java 17, `test balanceToolTest bootJar --rerun-tasks` BUILD SUCCESSFUL |
| BalanceTool 전체 | 84/84 PASS, failures/errors/skipped 0 |
| Unity 전체 EditMode | 756건 실행. 기존 `BattleCanonicalBalanceTests.CanonicalBundle_LoadsManifestRegistriesAndMappings` hash 실패 1건 유지. expected `1-2c24f2a29fd63b81`, actual `1-e37af4a5ff9b7204`. 신규 실패 없음. job `7fc584f0c1c64722846b1abe96d8c38e` |

파일 DB 테스트는 격리 임시 DB와 Spring context 재기동 증거이며 실제 운영 서버 프로세스/클라이언트 완주 증거로 확대 해석하지 않는다. 성공 Verifier는 서버 src/test에만 존재한다.

## UI Fixture

- Scene: `Assets/Scenes/Tests/UserSystem/P2_07_Tutorial_Test.unity`
- 생성: `Tools/MyDefense/Tutorial/Rebuild UI Fixture`
- Feature Test Hub: `P2-7-TUTORIAL`
- Editor-only, NO HTTP/NO REWARD. `다음 상태 보기 (테스트)`는 시각 상태만 바꾼다. production Build 제외.
- 실제 SampleScene에서는 인증된 계정에 전투 탭 진입 버튼만 추가한다. 전체 튜토리얼을 자동 강제 시작하지 않는다.

## 실제 화면 조작·독립 리뷰

- 2026-10-15 PM Windows 실제 마우스 클릭, Unity GameView Galaxy 1080×1920에서 확인했다. 프로그래밍 방식의 Button.onClick 호출로 대체하지 않았다.
- 격리 UI Fixture: 안내 시작 → 연습 준비 상태, 테스트용 다음 상태 → 내 유닛 → 강화 비소비 안내 → 신화 보상 안내 → 교배 위치 → 완료 → 자유 플레이(창 닫힘), 별도 닫기·다시 열기를 확인했다. 글자 겹침/잘림이나 동작 오류는 이 화면비에서 발견하지 않았다. **Fixture는 HTTP/보상/진행 저장이 없으며 실제 연습 완료나 지급 증거가 아니다.**
- 실제 SampleScene: 별도 `127.0.0.1:8197` 메모리 H2 서버와 전용 Auth profile/JWT 게스트로 로비를 연결했다. 기존 master 계정/파일 DB에는 요청하지 않았다. 신화 preview 자동 지급도 껐다.
- 실제 UI 클릭으로 진입/GET → 안내 시작/POST → `PRACTICE_PENDING`, revision=1, rewardGranted=false를 확인했다. 미연결 연습 시작은 준비 중 안내로 차단되고 임의 완료·지급이 발생하지 않았다.
- `나중에 하기 / 닫기` 후 숨김 및 pending=false, 단계 유지 확인. Unity Play 종료/재시작 후 같은 격리 게스트로 복원하고 다시 진입하여 서버의 동일 단계/revision=1/rewardGranted=false를 재조회했다. 서버 프로세스는 이 시나리오 중 유지했으므로 **서버 프로세스 재시작 E2E로 확대하지 않는다.**
- 실행 후 Unity Console 오류 0. SampleScene clean/Play false, 임시 API·환경·Auth profile 및 개발 계정 선택을 기존 값으로 복구하고 직접 실행한 8197 서버만 PID/시작 시각/JAR 경로를 검증하여 종료했다. 기존 계정 인증 저장소는 변경하지 않았다.
- 독립 읽기 전용 리뷰: User/System 1차 범위 **Approve**, 필수 코드 수정 없음. 전체 튜토리얼 완료나 Battle 연습/보상 E2E 승인과 구분한다.
- Battle Launcher/서버 Verifier가 없으므로 실제 연습 이후 보상까지 통과시키지 않았다. 전체 완료 판정은 유지하지 않는다.

## 남은 게이트

1. Battle 담당의 무료 교육 세션·연습 성공 증거·보드 재개 방식 합의 및 Adapter 구현.
2. 실제 authenticated 실행에서 연습 → 로비 안내 → 보상 → 교배 안내 완주와 재접속 검증.
3. 다양한 화면비 실제 조작, UI 오류 발생 시 보고/승인 후 수정·재검증.
4. jjangash/kinggusi 최종 사람 인수. 현재 양쪽 미실행.
5. 운영 인증/매칭 유예는 유지하며 공개 서비스 전 별도 게이트 필요.

커밋·푸시는 이번 구현 승인에 포함하지 않는다.
