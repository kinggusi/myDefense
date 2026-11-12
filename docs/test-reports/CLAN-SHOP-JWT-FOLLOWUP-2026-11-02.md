# 클랜·상점 잔여 검증 및 JWT 후속 보완

## 2026-11-12 커밋·통합 준비 재검증

- 사용자 승인으로 남은 인증/상점 코드·격리 테스트 도구·Battle 고정 해시 기대값 수정과 관련 문서를 커밋하여 `dev` PR 통합 대상으로 준비했다. Photon App ID, ProjectSettings, 예제 패키지 meta 삭제, 로컬 캡처는 제외한다.
- `gradlew test balanceToolTest --rerun-tasks`: BUILD SUCCESSFUL. 서버 567/567, Balance 84/84, 실패/오류/스킵 0. 기존 deprecated MockBean/SpyBean 등 경고는 남아 있다.
- 최신 Unity 누적 작업 트리 결과 `reference-v7-supply-label.xml`: 928/928 PASS. 별도의 깨끗한 checkout 실행 결과는 아니며 로컬 설정을 병합했다는 뜻도 아니다.
- 실제 기기·최종 사람 인수·운영 Google/Apple/매칭과 아래 미수행 UI 검증은 그대로 잔여다. 아래의 미커밋·미푸시 표기는 각 과거 검증 당시 이력이다.

기준: 2026-11-02, `54e3ee7` 이후 `feature/user-clan-shop-jwt-validation`. User/System 범위. JWT 후속 보완과 아래 검증을 수행했으며, 전체 잔여 검증 완료·최종 사람 인수·운영 출시 승인을 뜻하지 않는다.

## 변경 범위

- 서버가 거부한 Access Token은 로컬 만료 시각이 남아 있어도 다음 명시 재시도에서 갱신한다. 경제 POST는 자동으로 재전송하지 않고 기존 pending payload/requestId를 유지한다.
- 요청/토큰 갱신/게스트 복구/개발 로그인 도중 세션이 교체되면 이전 응답으로 credential을 저장하거나 후속 구매를 보내지 않는다.
- 상점·교배 컨트롤러는 AuthSession 인스턴스, 세대, userId, 정규화 API endpoint에 묶는다. 정상 토큰 회전은 허용하고 계정·endpoint 변경 후 늦은 UI 반영/명령은 차단한다.
- 실제 AuthSession/ShopCatalogController와 서버 구매 API를 사용하는 Editor-only `P2_06_01_Shop_Http_Test` 씬을 Feature Test Hub에 추가한다. 서버 하네스는 test classpath, loopback 18084, 매번 새 메모리 H2만 사용한다. production Build에는 포함하지 않는다.
- 가격·확률·보상·게임 데이터·Battle·Shared·서버 production 경제 소스는 변경하지 않는다. Google/Apple 실제 연동·모바일 보안 저장소·운영 매칭은 기존 유예를 유지한다.

## 실제 클랜 화면 조작

격리 `ClanUiSmokeServer` 18083, 임시 DB/3계정으로 수행했다. 생성/신청의 이번 초기 상태는 API로 준비했으며 화면 조작으로 생성했다고 기록하지 않는다.

- 클랜장 화면에서 Guest-3 가입 승인 확인 → 인원 1→2, 신청 2→1 반영.
- 멤버 관리 → 부클랜장 임명 확인 → Guest-3 역할 변경 반영.
- 테스트 계정2 전환 → 부클랜장 본인 표시, 클랜장 설정 버튼 없음, 가입 관리 접근 가능 확인.
- 부클랜장으로 Guest-4 가입 승인 확인 → 인원 2→3, 신청 없음 반영.
- 테스트 계정3 전환 → MEMBER 본인 표시, 설정/가입 관리/멤버 관리 버튼 미노출 확인.
- 탈퇴 확인창의 24시간 재가입 제한 문구 확인 후 취소 → 인원 3 유지. 최종 탈퇴 클릭은 컴퓨터 조작 스킬의 삭제성 동작 확인 규칙에 따라 사용자 승인 대기로 남겼다.
- 해당 시점 Unity Console 화면 오류 카운터 0. Play 종료 후 commandline으로 식별한 자체 ClanUiSmokeServer PID20808만 종료했다. 실제 개발 계정·파일 DB는 사용하지 않았다.

신청 취소/거절, 부클랜장 해제, 위임, 추방, 최종 탈퇴·해산, stale revision/응답 유실의 실제 버튼 전체 흐름은 이 기록의 위 검사만으로 PASS가 아니다. 기존 자동 테스트와 구분한다. 이전 자동 drag의 held 이동 프레임 미전달 한계와 실제 마우스/손가락 드래그 인수 대기도 유지한다.

## 자동 검증

- 최신 집중 Unity Auth/Shop/Breeding/Clan: 148/148 PASS.
- 서버 집중 Shop/Auth/MythicBreeding: 66/66 PASS. `ShopPersistenceRestartTest`는 파일 H2의 지갑/상품/한도/할인/광고/영수증 보존과 동일 요청 재전송 무차감을 검증한다. 같은 JVM 안의 Spring Context 재시작이며 OS 프로세스 재시작이나 화면 조작으로 표현하지 않는다.
- 전체 서버 557/557, Balance 84/84 PASS. 마지막 테스트 하네스의 모의 광고 opt-in 한 줄 추가 후에는 testClasses 컴파일·실제 하네스 재실행과 아래 광고 UI 검증을 수행했다.
- 최종 전체 Unity 827건 실행, 826 PASS / 기존 Battle hash 1 FAIL. `BattleCanonicalBalanceTests.CanonicalBundle_LoadsManifestRegistriesAndMappings`: expected `1-2c24f2a29fd63b81`, actual `1-e37af4a5ff9b7204`. Battle 소유 테스트·Balance 데이터는 임의 변경하지 않았다.
- 신규 HTTP 씬 Missing Script 0, production Build 제외 확인. 컴파일 오류 없음. 최종 실제 상점 검사 후 Console error/warning 조회 0건.
- 독립 코드 리뷰에서 게스트 복구 fallback의 stale credential 저장과 열린 대상 테스트 씬 덮어쓰기 위험을 발견하여 구현자가 보완했다. 개발 로그인 응답도 같은 세션 보호를 적용했다.
- 최종 독립 read-only 리뷰 PASS / Approve, 필수 수정 0건. PM은 위 JWT 보완·수행 검증 범위를 수락하며 미수행 검증은 아래에 유지한다. C#/Java/Gradle/Markdown `git diff --check` 오류 없음.

## 변경 파일

- 런타임: `AuthSession.cs`, `ShopCatalogController.cs`, `MythicBreedingController.cs`.
- 회귀/격리 검증: `AuthClientTests.cs`, `ShopCatalogTests.cs`, `ShopCatalogAssetBuilder.cs`, `FeatureTestCatalog.cs`, `ShopHttpUiFixture.cs` 및 Unity 생성 meta/HTTP 테스트 씬, `ShopUiSmokeServer.java`, `server/build.gradle`.
- 기록: 본 문서, `98_IMPLEMENTATION_TASKS.md`, `JWT_OPERATION_CONSIDERATIONS.md`, 기존 클랜/상점 검증 문서의 후속 링크.
- 기존 Photon 설정·ProjectSettings·package meta 삭제·Captures 등 관련 없는 작업 트리 변경은 이번 수정에 포함하거나 되돌리지 않는다.

## 실제 상점 화면 조작 및 HTTP 대조

격리 `ShopUiSmokeServer` 18084 / Editor-only HTTP 씬 / 임시 계정으로 수행했다. 실제 개발 계정·파일 DB·8080 서버는 변경하지 않았다. 초기 테스트 재화는 하네스에서만 제공한다.

1. 실제 클릭으로 JWT 거부 재현 → 골드 10,000 구매(500 다이아) 확인 → HTTP 401과 `결과 확인 필요` 표시, 지갑 무변화/pending 보존 확인.
2. `같은 요청 재시도` 클릭 → 토큰 복구 후 동일 requestId 구매 성공. 골드 5,000,000→5,010,000, 다이아 100,000→99,500, 구매 가능 횟수 3→2.
3. 실제 클릭으로 왹져DNA 20(500 다이아), 성장세포 10(300 다이아), 10회 뽑기(할인 2,700 다이아) 구매. 최종 다이아 96,000/DNA20/성장세포10, 주간 할인 3→2 확인. 이번 격리 씬에서 실제 로비 뽑기 연출을 검사한 것은 아니다.
4. 위 골드 구매의 동일 payload/requestId를 HTTP로 다시 제출 → 같은 영수증, 지갑 변화 없음. 이 단계는 화면 클릭이 아닌 API 멱등 대조다.
5. 휠로 목록 아래 상품 확인 → 일일 NORMAL 조각 20을 1,000골드로 실제 클릭 구매 → 골드5,009,000, 구매 완료/버튼 비활성화 확인. 스크롤 상단 클리핑 확인.
6. 새 임시 DB로 모의 광고 검증. 구매 상품 보존용 NORMAL2(id23) 구매 상태는 API로 준비했다. 실제 광고 취소 클릭 → 횟수3 유지/지갑 불변. 실제 모의 광고 완료 클릭 → 횟수3→2, 미구매 EPIC19→17/UNIQUE8→10/LEGEND2→7 변경, 구매한 NORMAL2 id23/구매완료 유지. 골드4,999,000/다이아100,000 불변. 운영 광고 SDK·보상 서버 검증이 아니라 개발 모의 경로다.

증거: `Client/_localbuild/ShopJwtFollowup/Purchases.png`, `AdResult.png`. 테스트 종료 후 Play=false/Compile=false, SampleScene 복귀/Dirty=false. commandline 확인 후 자체 임시 서버 PID47156 및 재시작 PID17068만 종료했다.

## 잔여 및 인수 경계

- 클랜의 위 미수행 HTTP 버튼 흐름, 실제 마우스·손가락 드래그, 로비 통합 결과 연출/복귀, 모바일 기기·사람 최종 인수는 미완료로 유지한다.
- 이번 턴 OS 프로세스 재시작 뒤 구매 상태 복구, 앱 종료 후 pending 재시도, 럭키 상품 실제 화면 구매, 광고 실패 버튼/일일 3회 소진 전체 UI는 재검증하지 않았다. 자동 테스트 또는 과거 기록과 구분한다.
- 신규 HTTP fixture에는 기존 SampleScene의 Gacha 연출 참조를 복제하지 않는다. 서버 Gacha 구매·지갑·할인 갱신과 실제 로비의 결과 연출/닫기/복귀는 별도 검사다.
- 실제 기기·사람 최종 인수, 기존 Battle canonical hash 실패, 운영 광고 SDK·운영 인증/매칭은 별도 잔여다.
- 이번 작업 커밋·푸시는 하지 않는다.

## 후속: 실제 OS 프로세스 재시작 검증

2026-11-02 후속. 위 메모리 DB/Editor 검증과 별개로 실제 로비 EXE와 격리 파일 DB를 사용했다.

- test-classpath 전용 `ShopPersistenceUiServer` 및 테스트를 추가했다. loopback 18085, `server/build/shop-ui-persistence/<UUID>` 파일 H2만 사용하며 fresh/resume를 명시한다. 기존 디렉터리 재사용 fresh, 잘못된 marker/key, 리다이렉트 경로, 중복 실행, 준비 후 생긴 기존 DB 채택을 차단한다. resume는 계정/재화를 재지급하지 않는다.
- 테스트 전용 ShopRandomSource만 럭키 발생을 고정한다. 운영 상품 수치·Gacha 난수·확률은 변경하지 않았다. 모의 광고는 명시 opt-in이다.
- 신규 하네스 10/10, 전체 서버 567/567(87 suites) PASS. Balance 84/84는 이전 결과이며 후속 호출은 UP-TO-DATE로 새 실행이 아니다. 독립 read-only 최종 리뷰 PASS / 필수 수정 0건.
- 원래 SampleScene만 포함한 Development Client EXE를 빌드했다. 재컴파일 후 최종 build `build-9c9d71a123`: error 0 / warning 0. 테스트 씬은 production Build에 추가하지 않았다.
- 격리 인증 profile `shop-e2e-c07726e9`, 테스트 Guest-2로 일반 AuthStartup 및 로비 로드 성공. 실제 계정 credential이나 8080 서버/기존 DB는 변경하지 않았다.

실제 수행 순서:

1. run `c07726e9-806d-4792-aa00-ceb0c93b12f5`의 최초 서버 PID50044 / Client PID39508 사용. 테스트 초기 재화 Gold5,000,000 / Diamond100,000.
2. **API로** LUCKY_DNA(30,000 Gold → DNA20) 구매, 개발 모의 광고 완료 및 리롤 1회 수행. Gold4,970,000 / Diamond100,000 / DNA20 / 성장세포0 / 광고 잔여2, 럭키 구매완료 상태 확인. 이 단계는 UI 구매 PASS가 아니다.
3. commandline/실행 경로로 확인한 위 두 프로세스만 종료. 동일 run을 resume로 실행해 새 서버 PID49280, 동일 profile Client PID32308 생성. 실제 OS 프로세스 종료·재시작이며 같은 JVM Context 재시작과 구분된다.
4. API 재조회: serverTime을 제외한 전체 catalog가 재시작 전과 동일. 동일 payload/requestId `2846c2c2-d072-40a4-884c-0ce6c68893ac` 재전송 시 영수증 전체 동일, 지갑/한도/상품/리롤 상태 불변.
5. 재실행 EXE에서 Guest-2 / Level10 / Gold4,970,000 / Diamond100,000 표시와 로비 복구를 화면 관찰했다. `Client/_localbuild/ShopPersistence20261102/Player-resume.log`에서도 동일 guest 로비 로드 성공.

검증 경계:

- Windows `PickerHost.exe` 창 선택 overlay가 실제 클릭을 차단했다. 대상 활성화·새 관찰 후 1회 재시도도 동일하여 사용자에게 선택창 완료/닫기를 요청하고 입력을 중단했다. 앱 접근 승인 답변만으로 OS 선택창 처리가 완료됐다고 간주하지 않는다.
- 실제 로비 뽑기 연출→닫기→재구매, UI 럭키 구매, 광고 실패/3회 소진, 앱 종료 후 pending 명시 재시도는 여전히 미완료다. 위 API 영수증 재전송은 커밋 후 응답 유실의 UI 재현이 아니다.
- 새 변경은 `ShopPersistenceUiServer.java`, `ShopPersistenceUiServerTest.java`, `server/build.gradle` 및 문서이며 기존 미커밋 JWT/검증 변경은 보존한다. 커밋·푸시는 하지 않았다.
