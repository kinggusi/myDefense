# P3-1 클랜 정책·서버 검증 기록

- 날짜: 2026-11-02
- 담당: User/System
- 브랜치: `feature/user-p3-1-clan`, 기반 `4ad4736`
- 범위: P3-1-1 정책 및 P3-1-2 서버 1차. **P3-1-3 UI는 후속 대기**.
- 현재 판정: **P3-1-1 정책·P3-1-2 서버 1차 완료**. 집중·전체 회귀 및 독립 리뷰 PASS, PM 서버 범위 수락. 전체 클랜 UI·운영 출시 완료가 아니다.

## 구현

- 인증 principal 기반 클랜 검색·조회·생성·자유 가입·신청/취소/승인/거절·탈퇴·추방·부클랜장 임명/해제·클랜장 위임·소개/공지/가입방식 설정·혼자일 때 해산.
- 서버 권위 Lv.10, accountGold 50,000, 정원 30명, 계정당 소속 1개, 탈퇴/추방/해산 후 24시간 재가입 대기.
- 성공 명령 전체 영수증 멱등, 실패 트랜잭션 rollback, actor/target User 오름차순 후 Clan 잠금, 잠금 획득 후 기존 entity 캐시 refresh.
- 해산 soft-delete와 이름 보존, 무효 신청의 안전한 재신청, 재전송 시 최신 권한만 표시.
- 기존 AuthService의 게스트 판정 재사용. 게스트는 Guest-ID, 비게스트는 기존 username. 공지는 멤버만, 신청은 관리자만, 응답은 no-store.
- 상세 정책/구현 기본값/API: [CLAN_POLICY.md](../policies/CLAN_POLICY.md).

## 실행 결과

명령:

```powershell
$env:JAVA_HOME='C:/Program Files/Java/jdk-17'
./gradlew.bat test --tests '*clan.*' --no-daemon
```

| 검증 | 결과 |
|---|---|
| compileJava / compileTestJava | PASS |
| ClanServiceTest | 29/29 PASS |
| ClanPersistenceConcurrencyTest | 9/9 PASS |
| 클랜 집중 합계 | **38/38 PASS**, 실패·오류·스킵 0 |
| git diff --check | PASS |
| 전체 서버 회귀 | **549/549 PASS**, 84 suites, 실패·오류·스킵 0 |
| 전체 BalanceTool 회귀 | **84/84 PASS**, 8 suites, 실패·오류·스킵 0 |
| 독립 리뷰 | **PASS**, 필수 수정 0, PM 서버 범위 수락 |
| Unity/MCP/실제 UI 조작 | 이번 범위 없음, 미실행 |

초기 집중 26/26은 통과했다. 이후 파일 DB 테스트 첫 실행에서 테스트 인스턴스 `@TempDir` 초기화가 `@BeforeAll`보다 늦어 초기화 1건 실패했으며 static TempDir로 수정했다. 37건 전체 재실행을 통과했고 독립 리뷰의 NBSP 정규화 지적을 수정·회귀 추가한 최종 38건도 전체 통과했다. 제품 서버/실개발 DB 오류를 숨기거나 실패 테스트를 제외한 것이 아니다.

최종 전체 회귀는 PM이 코드 동결 후 `./gradlew.bat test balanceToolTest --rerun-tasks`로 실행했다. exit 0 / BUILD SUCCESSFUL이며 최종 XML의 서버 549건에는 클랜 38건이 포함된다. 독립 리뷰의 기존 프로필 표시명·일관된 snapshot·no-store·NBSP 지적을 모두 반영한 최종 코드 기준이다.

## 검증한 경계

- Lv.9/10, 생성비 부족, 멱등 재생성, 동일 requestId에 다른 payload, 정규화 중복 이름과 잔액 보존.
- 한 계정 중복 소속, 승인제 신청/취소/거절, 신청 1곳, 승인 시 레벨 재검사, 자유 가입 시 기존 신청 해제.
- 멤버·부클랜장·클랜장의 권한, 위임 후 단일 클랜장, 비어 있지 않은 클랜 해산/클랜장 직접 탈퇴 차단.
- 23:59:59 차단과 정확히 24시간 허용, 생성·신청을 통한 쿨다운 우회 차단.
- 해산 재전송·환급 없음·이름 예약, 해산 클랜 신청의 논리 무효와 교체.
- 탈퇴/추방/강등 후 과거 요청 재전송에서 공지·신청 정보 노출 없음.
- `%`, `_` 검색 wildcard escape, 길이/페이지/응답 크기 상한, 마크업·제어문자 거절.
- NFKC 정규화 후 공백 제거로 NBSP만 있는 빈 이름 및 NBSP로 감싼 중복 이름 거부·잔액 보존·동일 검색 확인.
- 미인증 local 요청 거부, principal과 다른 userId/username payload로 계정 위조 불가, no-store 응답.
- 게스트/비게스트 표시명 기존 프로필과 일치.

실제 별도 트랜잭션 2스레드 검사:

1. 같은 생성 요청 동시 2번 → 클랜 1개, accountGold 1회 차감.
2. 29명 클랜 마지막 자리에 동시 2명 → 1명 성공, 1명 CLAN_FULL, 30명 유지.
3. 한 계정이 두 클랜 동시 가입 → 1곳만 성공.
4. 같은 정규화 이름 동시 생성 → 1명만 생성/차감, 나머지 409 충돌.
5. 단독 해산과 신규 가입 경합 → 가입한 멤버가 남은 클랜을 해산하지 않음.
6. actor/target 순서가 반대인 위임·역할 명령 → 잠금 역전 없이 종료.
7. 관리자 entity를 미리 읽은 트랜잭션에서 타 트랜잭션이 강등 후 승인 요청 → 새 권한으로 거절.
8. 영수증 저장 강제 실패 → 생성 Clan·소속·재화 모두 rollback, 정상 재시도 가능.
9. TempDir 파일 H2 종료/재시작 → 소속·역할·공지·신청·쿨다운·생성/탈퇴 영수증 보존.

각 경합 성공 응답에서 `memberCount == members.size()`도 검사한다.

## 변경 파일

- 서버 `server/src/main/java/com/denfense/server/clan/`: Clan, ClanAccountState, ClanCommandReceipt, 각 Repository, ClanPolicy, ClanClock, ClanDtos, ClanService, ClanController.
- 테스트 `server/src/test/java/com/denfense/server/clan/`: ClanServiceTest, ClanPersistenceConcurrencyTest.
- 문서: `docs/99_MASTER.md`, `docs/98_IMPLEMENTATION_TASKS.md`, `docs/DATABASE_MIGRATION_POLICY.md`, `docs/policies/CLAN_POLICY.md`, 이 검증 기록.
- 기존 Photon/ProjectSettings/Captures 변경은 손대지 않았다. Unity/Battle/Shared 공용 파일 변경 없음. 커밋·푸시 없음.

## 잔여 위험·후속

- 로비 클랜 탭에는 아직 본기능 UI가 연결되지 않았다. 현재 사용자 화면에서 클랜 생성·가입이 된다고 주장하지 않는다.
- P3-1-3에서 API 연결, 관리자 메뉴/확인 팝업, 오류·재시도 UX, 실제 화면 조작 및 최종 사람 인수를 진행한다.
- 운영 DB driver·Migration 및 다중 서버 lock 검증은 운영 준비 게이트다. 현재 scalar 참조의 운영 FK/check 적용 계획은 DATABASE_MIGRATION_POLICY에 별도 기록했다.
- 이름 재사용/변경, 신청 100건 이후 페이지 UI, 공개 닉네임, 출석/기부/채팅/Boss/상점/시즌은 추가 정책/후속 범위다.
- UI 연결 시 생성비·최소레벨·재가입 대기 공개 정책 조회 DTO를 추가해 서버 설정과 UI의 하드코딩 불일치를 방지한다.
- Detail의 계정별 AuthService 표시명 조회는 bounded N+1이며 Clan lock 보유 시간을 늘릴 수 있다. 운영 부하 측정 및 일괄 조회 최적화는 비차단 후속이다.
- 기존 Unity Battle canonical hash 테스트 1건은 이번 서버 작업과 무관하며 변경·재검증하지 않았다.
