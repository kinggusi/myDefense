# User/System 기존 코드 보완 및 운영 인증 준비

## 범위와 Git 상태

- 사용자 요청: 기존 코드 보완과 운영 인증·매칭, 자동검증·독립 리뷰·실제 화면 조작까지 진행. 최종 통합 인수는 사용자가 수행.
- 작업 브랜치: `feature/user-p1-5-7-auth-matchmaking`.
- 기존 승인 업로드 HEAD `b08305e`에서 시작해 최신 `origin/dev` `5322150`을 no-commit merge했다. Task 문서 충돌은 최신 Battle 진행 상태와 User/System Quest 상태를 모두 보존해 해소했다.
- merge 및 이번 변경은 아직 커밋/푸시하지 않았다. 개인 Photon/Unity 설정과 샘플 meta 삭제는 보존하며 이번 범위에서 제외한다.
- Battle Runtime 구현은 변경하지 않았다. incoming dev의 Battle 변경은 동료 구현 그대로 통합한다.

## 구현

1. QuestSettlementProcessor: 한 Settlement의 처리 시각을 한 번만 읽어 모든 참가자/조건의 일일·주간 주기를 동일하게 계산.
2. QuestController, DailyContentController, LocalDailyContentResultController, LocalBattleSessionRosterController, LocalFusionSessionRosterAdapter: local/dev에 prod/production이 섞이면 등록하지 않음.
3. BattleEntryAttackSnapshotService: 익명 Snapshot은 local/dev + 명시적 옵션에서만 허용, 운영 혼합 시 차단. 테스트 hook도 환경 제한을 우회하지 않음.
4. SampleScene: Heart/Coin/Diamond 투명 배경 Image의 오래된 missing Sprite 참조 3개만 Unity Editor API로 null 처리. 프로필/Transform/색상/버튼 이벤트는 diff상 변경 없음.
5. 서버 자정/멱등/프로필 회귀 및 Unity Preview Scene 기반 참조 회귀 추가.

## 자동검증 및 독립 리뷰

- 서버 집중: 31/31 PASS.
- 서버 전체: 403/403 PASS, 실패/오류/스킵 0.
- BalanceTool: 82/82 PASS.
- 최신 dev 통합 직후 Unity 기준선: 654/654 PASS.
- 수정 후 Unity EditMode: 655/655 PASS, 실패/스킵 0. Job `4971ba37db644cf59bd8eaeb833a26b7`.
- Java 컴파일, Unity C# 컴파일 오류 없음, 변경 범위 diff --check PASS.
- 서버 독립 리뷰 PASS, UI 독립 리뷰 PASS. 리뷰는 읽기 전용이며 구현자가 자기 승인하지 않음.
- Unity MCP: `Client@0689e9f51f1b7464`, Unity 6000.3.4f1, `E:/study/MyDefenseGame/Client` 연결 확인.
- 새 테스트 Scene/Prefab 생성 없음. 기존 EditMode 회귀만 추가하며 production Build에 Fixture를 추가하지 않음.

## 실제 화면 조작 확인

- 별도 18080 포트와 `codexUiValidation` 메모리 DB의 local 서버를 실행해 기존 DB와 분리했다.
- Unity의 일시적 API 환경 변수만 테스트 서버로 연결; 추적 파일에 endpoint를 저장하지 않음.
- 실제 마우스로 Unity Play 클릭 → sh1 로비 로딩 → Quest 진입 → 일일 목록 표시 → 주간 탭 전환 확인.
- 이 시점의 새 Play Console error/warning 0. 자동테스트 음성/오류경로 로그와 구분하기 위해 Play 전 Console을 정리했다.
- 다른 창의 사용자 입력이 감지되어 추가 클릭을 중단했다. 업적/닫기/내 유닛 스크롤/교배 진입 재검증은 아직 PASS로 기록하지 않는다.
- 사용자 재개 승인 후에도 다른 창 입력/가림이 반복돼 검증을 완료한 것으로 처리하지 않았다. Unity Play를 종료하고 일시적 API 환경 변수는 이전 값으로 복원했다.
- 후속 로그인 검증에서 사용자 조작 허용 아래 실제 마우스로 업적 전환/닫기/내 유닛 스크롤/교배 진입을 완료했다. 서버 오류 재발 없음. 새 게스트 이름의 기존 프로필 한글 glyph 누락은 보고 후 사용자 승인으로 실제 계정 번호 `Guest-{userId}` 표시로 수정했다. 위 중단 기록은 당시 이력이며 현재 실행 증거는 `AUTH-STARTUP-2026-09-18.md`를 따른다.
- 최종 사용자 디자인 인수, 실제 모바일 기기 검증, 전체 통합테스트는 별도 대기.

## P1-5-7 남은 선행 결정과 운영 작업

- 사용자 정책 확정(2026-09-18): 게스트 시작 + Google/Apple 계정 연동, 현재 유저 Host 방식 유지. 전용 서버 전환은 운영 테스트의 안정성·조작 위험·실측 비용을 보고 추후 판단한다. 로그인 진입 정책과 인증 구현을 분리해 향후 다른 진입 방식으로 전환 가능하게 한다.
- 후속으로 JWT 발급/갱신/검증·보호 API Principal 바인딩·Windows Unity 인증 HTTP/UI를 구현했다. 영속 Matchmaking roster, Match Ticket/Photon identity, 실제 Google/Apple 및 모바일 저장소, production Adapter는 아직 미구현. 상세는 `AUTH-STARTUP-2026-09-18.md`를 따른다.
- Provider Client ID/외부 서비스 설정/운영 주소·Secret이 없는 상태에서 실제 production 로그인/2클라이언트 E2E를 PASS로 표시하지 않는다.
- JWT만으로 유저 Host의 전투 결과 조작이 방지되지 않는 신뢰 한계를 정책에 명시해야 한다.
- 자세한 교체 지점과 운영 설정은 `docs/JWT_OPERATION_CONSIDERATIONS.md` 9절에 기록했다.
- 앞선 전용 서버 출시 권장안은 사용자 확정 정책이 아니다. 최종 결정은 현재 Host 유지 및 운영 테스트 후 재평가이며, 전용 서버 계약/전환 작업은 진행하지 않는다. 정책 기록만으로 구현·화면 검증·production E2E 상태를 완료로 올리지 않는다.
