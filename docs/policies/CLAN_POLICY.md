# P3-1 클랜 1차 정책·서버 계약

기준: 2026-11-02. 담당: User/System. **정책·서버 1차 완료**: 클랜 집중 38/38, 전체 서버 549/549, Balance 84/84 및 독립 리뷰 PASS(필수 수정 0), PM 서버 범위 수락.
후속 사용자 승인으로 P3-1-3 UI 연결을 구현했다. UI용 정책/가격 견적/계정 레벨 확장 후 서버 집중41/41·전체552/552·Balance84/84·클랜UI49/49 및 독립 리뷰를 통과했다. 일부 실제HTTP 생성/가입/설정/권한을 확인했으나 역할변경 등 나머지 HTTP 조작·실사용 드래그·사람 인수가 남아 부분 완료다. 전체Unity805 중 기존Battle hash1실패를 유지하며 [UI 검증 기록](../test-reports/P3-1-3-CLAN-UI.md)에 자동/실제 검증을 구분한다.

## 1. 사용자 승인 핵심 정책

- 가입 조건: 계정 Lv.10 이상.
- 생성 비용: **accountGold 50,000**. Battle의 inGameGold가 아니다.
- 최대 인원: 클랜장 포함 30명. 계정당 소속 클랜은 1개다.
- 자유 가입 / 승인제 중 클랜장이 선택한다.
- 탈퇴·추방 후 24시간 동안 재가입할 수 없다.
- 역할: `LEADER`, `DEPUTY`, `MEMBER`.
- 클랜장: 설정, 권한 위임, 해산. 다른 멤버가 있으면 위임 후 탈퇴하며 혼자일 때만 해산한다.
- 부클랜장: 가입 승인·거절, 일반 멤버 추방.
- 검색, 소개, 공지, 멤버 목록을 제공한다.
- 2026-11-02 UI 검증 후 사용자 확정: **소개·공지는 한 줄 입력만 지원**한다. 서버의 줄바꿈/제어문자 거절 정책은 유지하고 UI에서 타이핑·붙여넣기의 줄바꿈과 금지 문자를 차단한다.
- **출석·기부·채팅·클랜 Boss·클랜 상점·시즌 경쟁은 1차 범위에서 제외**한다.

## 2. 구현 기본값 — 핵심 정책과 구분

아래는 1차 구현을 닫기 위한 기본값이다. 사용자 직접 확정 핵심 정책으로 소급 기록하지 않는다.

- 생성에도 Lv.10과 재가입 대기를 적용한다. 생성으로 24시간 대기를 우회할 수 없다.
- 혼자 해산한 클랜장에게도 24시간 대기를 적용하고 생성비는 환급하지 않는다.
- 가입 신청은 계정당 활성 클랜 1곳이다. 신청 취소·거절은 탈퇴가 아니므로 대기가 없다.
- 신청은 인원을 예약하지 않는다. 승인 시 레벨·현재 소속·쿨다운·정원을 다시 검사한다.
- 자유 가입 또는 새 클랜 생성 성공 시 기존 신청은 해제된다.
- 부클랜장 임명·해제는 클랜장만 할 수 있으며 별도 인원 제한은 두지 않는다(전체 정원 내).
- 위임받은 멤버는 클랜장, 이전 클랜장은 일반 멤버가 된다. 자신에게 위임·추방·승인하는 요청은 거절한다.
- 클랜장만 부클랜장을 추방할 수 있다. 부클랜장은 다른 부클랜장·클랜장을 추방할 수 없다.
- 이름은 2~20 Unicode 문자이며 문자·숫자·공백·`_`·`-`만 허용한다. **NFKC 정규화 후** 앞뒤 공백 제거, 대소문자 무시 중복 검사를 적용한다. NBSP 등 호환 공백으로 빈 이름이나 중복 이름을 만들 수 없다.
- 클랜 이름 변경은 1차 제외다. 해산은 soft-delete하므로 **해산한 이름도 재사용하지 않는다**. 재사용/운영 정리 정책은 추후 변경할 수 있다.
- 소개 120자, 공지 500자. 제어/방향전환 문자와 `<`, `>` 마크업을 거절한다. 공지는 멤버만 읽고 신청 목록은 클랜장·부클랜장만 읽는다.
- 멤버 표시명은 기존 AuthService/Lobby 프로필 정책과 같다. 게스트는 `Guest-{실제 userId}`, 연동/개발 비게스트 계정은 기존 username을 표시한다. 비밀번호·provider ID는 노출하지 않는다. 추후 공개 닉네임 도입 시 표시 매핑만 교체한다.
- 신청 목록은 userId 오름차순 최초 100건까지 반환한다. 승인·거절 후 다음 신청이 노출된다. UI의 커서 페이지 탐색은 P3-1-3에서 필요한 경우 확장한다.
- 해산 클랜의 기존 신청은 저장상 참조를 유지하되 즉시 논리 무효다. 새 신청으로 대체하거나 해당 신청을 취소할 수 있다. 해산이 다른 계정의 잠금을 역순으로 잡아 전체 신청을 지우지 않는다.

## 3. 정책 수치 관리

이번 클랜은 서버 로비 정책이므로 `ClanPolicy`에 서버 설정 기본값을 모았다. Battle Excel/generated JSON 또는 canonical Balance/hash는 바꾸지 않는다. 비용·레벨·정원은 서버가 결정한다. UI가 보내는 선택적 `expectedCreationGold`는 표시한 견적과 서버 가격의 일치 확인에만 쓰며 서버 비용을 덮어쓰지 않는다.

| Spring 설정 (`mydefense.clan.` 접두사) | 기본값 |
|---|---:|
| `minimum-level` | 10 |
| `creation-gold` | 50000 |
| `maximum-members` | 30 |
| `rejoin-cooldown` | PT24H |
| `name-minimum` / `name-maximum` | 2 / 20 |
| `description-maximum` / `notice-maximum` | 120 / 500 |

추후 운영 Excel 관리에 편입할 때는 로더를 `ClanPolicy` 공급 경계에 연결한다. 서버별 값이 달라지는 롤링 배포는 피하고 모든 인스턴스의 정책을 같은 버전으로 배포한다. 기존 정원보다 작게 설정하면 기존 멤버를 자동 추방하지 않고 신규 가입만 차단한다.

## 4. API·UI 인계

모든 API는 인증 principal이 필요하다. local이라도 username query만으로 사용할 수 없다. actor userId를 요청에서 받지 않는다.
인증 응답은 `Cache-Control: no-store`로 공유 캐시 보관을 금지한다.

| 메서드·경로 | 응답·기능 |
|---|---|
| `GET /api/clans/policy` | 인증된 UI용 최소레벨·생성비·정원·재가입 초·입력 길이 정책 |
| `GET /api/clans?q=&page=0&size=20` | 활성 클랜 Summary 검색. page 0~1000, size 1~20. 이름 검색 `%`, `_`, escape 문자는 리터럴 처리 |
| `GET /api/clans/me` | 실제 accountGold/accountLevel, 역할, 재가입 시각, 활성 신청, 현재 클랜, 서버 UTC 시각 |
| `GET /api/clans/{clanId}` | 소개·멤버 공개, 공지는 멤버만, 신청 목록은 관리자만 |
| `POST /api/clans/commands` | 아래 명령 수행 후 actor 최신 Status 반환 |

생성 예:

```json
{"requestId":"고유 UUID","operation":"CREATE","name":"왹져연합","description":"함께 방어해요","joinMode":"OPEN"}
```

가입 예:

```json
{"requestId":"고유 UUID","operation":"JOIN","clanId":1}
```

승인 예:

```json
{"requestId":"고유 UUID","operation":"APPROVE","clanId":1,"targetUserId":22,"expectedRevision":4}
```

- `CREATE`: name, description(생략 가능), joinMode 필수. clanId/target/revision/notice는 금지. UI는 조회한 `expectedCreationGold`도 보내며 변경 시 409 `CLAN_QUOTE_CHANGED`로 무차감 거절한다. 기존 클라이언트 호환을 위해 생략 가능하다.
- `expectedCreationGold=null`은 canonical command JSON에서 필드 자체를 제외하여 기존 영수증 fingerprint 바이트를 유지한다. null 이전 payload로 생성한 영수증 재전송 회귀를 통과했다. 값이 있으면 fingerprint에 포함한다.
- `JOIN`, `APPLY`, `CANCEL_APPLICATION`, `LEAVE`: clanId. 새 가입·탈퇴는 최신 서버 조건을 검사한다.
- `APPROVE`, `REJECT`, `KICK`, `PROMOTE`, `DEMOTE`, `TRANSFER`: clanId, targetUserId, expectedRevision 필수.
- `UPDATE`: clanId, expectedRevision, description, notice, joinMode 필수. 이름 변경은 지원하지 않는다.
- `DISBAND`: clanId, expectedRevision 필수.
- 해당 명령과 무관한 설정/대상 필드는 거절한다. 자유 가입은 `OPEN`, 승인제는 `APPROVAL`이다.
- 검색/조회는 재화를 소비하지 않는다. UI는 GET 결과의 실제 값만 표시하고 서버 오류를 임의 성공으로 처리하지 않는다.
- 409(`CLAN_STATE_CHANGED` 등) 시 최신 조회 후 사용자가 의도한 작업을 새 requestId로 요청한다. 네트워크 응답 유실이면 **동일 payload·동일 requestId**로 재시도한다.
- 해산, 탈퇴, 추방은 확인 팝업 필요. 실제 클랜 UI에서 사라진 관리자 메뉴를 보여주지 않되 최종 권한은 항상 서버가 재검증한다.

## 5. 영속·멱등·동시성

- 성공한 **모든 명령**은 `(userId, requestId)` 유일 영수증과 명령 전체 payload SHA-256을 저장한다. 동일 키·다른 내용은 409다. 실패한 요청은 영수증·재화·소속을 모두 rollback한다.
- 중복 성공 요청은 다시 실행하지 않고 actor의 **현재** 소속·권한으로 Status를 구성한다. 탈퇴/추방/강등 전 공지·가입 신청 목록을 과거 응답에서 재노출하지 않는다.
- 생성비 차감, Clan, 소속, 영수증은 하나의 트랜잭션이다. 중복 이름 DB 경합 오류는 트랜잭션 밖에서 409로 변환한다.
- 잠금 순서: 영향받는 actor/target `users.id` 오름차순 → `clans.id` 한 행. Clan lock 이후 사용자 lock을 추가하지 않는다.
- account state는 사용자 row lock 후 처음 읽으며 이미 영속 컨텍스트에 있으면 refresh한다. Clan도 lock 후 refresh하여 관리자 요청 대기 중 권한·정원 변경을 재검증한다.
- 계정당 소속 1개는 userId PK + User lock으로, 정원은 Clan lock으로 보장한다. DB 프로세스 여러 개에서도 JVM 메모리 lock에 의존하지 않는다.
- 해산은 클랜 lock 안에서 실제 회원 수와 저장 count가 둘 다 1인지 확인한다. 물리 삭제하지 않으므로 동시에 들어온 신청/가입·재전송 참조가 dangling delete로 바뀌지 않는다.
- 재가입 시각은 서버 UTC Instant. 정확히 24시간이 된 순간부터 허용한다.
- 클랜 소속·역할·신청·대기·성공 영수증은 서버 재시작 후 유지한다. 임시 파일 H2 재시작 테스트로 확인하며 실개발 계정/DB는 테스트에 사용하지 않는다.

## 6. 검증 및 다음 단계

- 집중 테스트: 인증, 레벨, 비용, 중복 이름, 가입/신청, 역할, 위임/해산, 24시간 경계, 입력/검색, 최신권한 재전송.
- 별도 트랜잭션 2스레드: 생성 재전송, 마지막 정원, 한 계정의 두 클랜 경쟁, 중복 이름, 해산/가입, 반대 actor/target 잠금 순서, 미리 읽은 관리자 entity의 강등 후 재검증.
- 실패 rollback, 임시 H2 파일 재시작 후 소속/권한/신청/대기/영수증 유지.
- 독립 리뷰 및 서버 전체 회귀 결과는 별도 검증 기록에 남긴다.
- **P3-1-3: 로비 클랜 탭 실제 UI 연결 구현·검증 진행 중**이다. 기존 프로필·다른 탭 디자인과 Battle/Shared는 변경하지 않으며 실제 화면 검증·사용자 최종 인수 전 완료로 처리하지 않는다.
- 실제 운영 DB driver/migration 및 다중 서버·운영 DB lock 특성 검증은 공개 서비스 전 게이트다. H2 테스트를 운영 승인으로 간주하지 않는다.
- UI는 인증된 `/clans/policy` DTO를 조회하여 생성비·최소레벨·재가입 대기·입력 제한을 표시한다. 현재 서버 설정이 단일 원천이며 정책 조회 실패 시 새 명령을 차단한다.
- Detail은 정원과 신청 100건으로 상한은 있지만 표시명 조회가 계정별 AuthService 조회를 사용해 N+1 쿼리와 Clan lock 보유 시간이 늘어난다. 운영 부하 테스트·일괄 표시명 조회 최적화는 후속으로 남긴다.
