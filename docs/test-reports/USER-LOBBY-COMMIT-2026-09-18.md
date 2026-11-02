# User/System 누적 변경 커밋 검토 (2026-09-18)

## 범위

- 사용자 요청: 커밋/푸시 대상 선별 후 개발 브랜치에 업로드.
- 브랜치: `feature/user-p2-3-quest-system`.
- Quest/업적 API·영속 장부·보상·Excel 및 양쪽 generated JSON/manifest.
- local/dev 테스트용 Mythic 1·2 초기 보유와 프로필 차단 테스트.
- SampleScene 로비 UI, 4열 컬렉션/미해금 구역, Quest/교배 팝업 스타일, 하단 활성 아이콘, 레전더리 7종 원본 일러 및 카탈로그.
- 재현 가능한 local 서버 실행 설정과 버전 고정 Unity MCP 패키지.
- Battle 런타임 구현 변경 없음. BattleCanonicalBalanceTests는 공통 Balance manifest hash 기대값만 갱신.

## 이번 재검증

- 실제 Unity 연결: `E:/study/MyDefenseGame/Client`, Unity `6000.3.4f1`, `Client@0689e9f51f1b7464`.
- Unity 전체 EditMode: **587/587 PASS**, 실패/스킵 0. Job `db977814e80e4137894982b075cc6ea7`.
- 서버 `test balanceToolTest --rerun-tasks`: BUILD SUCCESSFUL, 8개 task 실제 실행.
- 서버: **391/391 PASS**, 실패/오류/스킵 0.
- BalanceTool: **82/82 PASS**, 실패/오류/스킵 0.
- canonical manifest 25개 파일의 LF 정규화 SHA-256/크기 검증 PASS. Unity/Spring Quest JSON 동일.
- 독립 읽기 전용 리뷰 2개: 서버/Balance 및 Unity UI/에셋. 신규 커밋 차단 항목 없음, WARNING.
- 새 에셋/스크립트 meta 누락 0. 새 UI 코드/에셋의 개발 PC 절대 경로 의존성 0.
- 테스트의 의도적인 오류 경로 로그는 존재한다. 이번 검증을 Console 전체 error 0으로 표현하지 않는다.
- Unity 생성 Scene/Prefab/meta의 공백 경고는 수동 YAML/meta 수정으로 정리하지 않았다.

## 포함하지 않은 로컬 변경

- `PhotonAppSettings.asset`: 개인 App ID 교체. 협업 세션 전체 변경으로 확정하지 않았으므로 유지하되 커밋 제외.
- `PackageManagerSettings.asset`: 에디터 알림/내부 instance 상태.
- `UnityConnectSettings.asset`: 개인 Unity Cloud 활성화.
- `ProjectSettings.asset`: 자동 생성된 Input Action preload 변화. 이번 기능 구현에 필요한 변경으로 확인되지 않아 제외.
- Photon 샘플 `.unitypackage.meta` 2개 삭제: 기능과 무관한 로컬 패키지 정리이므로 제외.
- 빌드, 로그, 캐시, 테스트 XML, 화면 캡처와 `.tmp` 산출물은 업로드하지 않음.

## 남은 경고와 인수 조건

1. Quest API는 local/dev 전용이며 운영 JWT 인증 구현이 아니다. `prod`/`production`과 `local`/`dev`를 혼합 활성화하면 현 `@Profile` 조건상 API가 활성화될 수 있으므로 운영 배포 전 명시적 차단 및 테스트가 필요하다. 운영에 개발 프로필을 함께 활성화하지 않는다.
2. QuestSettlementProcessor가 조건마다 현재 시간을 읽으므로 KST 자정을 걸친 단일 정산의 주기 귀속이 갈릴 수 있다. 처리 시각 고정과 자정 경계 회귀검증은 후속 보완 대상이다.
3. Quest 신규 테이블은 운영 배포 전 DATABASE_MIGRATION_POLICY에 따른 migration이 필요하다. 개발 테스트 성공을 운영 배포 승인으로 해석하지 않는다.
4. SampleScene의 Heart/Diamond/Coin 알파 0 배경 Image 3개에 기존 GUI Kit 원본에서 유래한 누락 Sprite 참조가 남아 있다. 새 일러/아이콘 카탈로그 참조는 정상이며 해당 기존 참조 정리는 별도 후속이다.
5. 재료 `+`는 안내 UI이고 판매 정책/API는 미완성이다. 상단 재화 `+`의 구매 기능도 완료 판정하지 않는다.
6. 이번 턴에서는 화면 조작/EXE 재빌드를 반복하지 않았다. 기존 실제 클릭·스크롤·화면비 검증은 LOBBY-NEON-COLLECTION.md를 참고한다. 최종 사용자 디자인 인수와 실제 기기 테스트는 대기한다.

## 전달

- 사용자 승인 범위는 commit/push이며 `dev` 병합·운영 배포는 수행하지 않는다.
- 서로 의존하는 서버/Balance와 Unity UI 변경은 같은 feature 브랜치의 전체 변경으로 리뷰한다.
- 실제 커밋 해시는 Git 이력으로 확인한다.
