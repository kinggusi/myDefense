# Battle 행성 디테일 HUD 합성 Monster 표현 검증

실제 `Battle.unity`에 행성 디테일, HUD, 합성 표시·효과, 포탈과 Monster 외형을 연결했다. 자동 검사·독립 리뷰와 동일 PC의 실제 두 Standalone 검증은 수행했다. kinggusi는 합성 효과와 이동 체감을 확인했다. 두 담당자의 최종 디자인 인수, 실제 2PC·장시간·난이도 게이트는 검증 대기로 유지한다.

## 작업본과 범위

- Unity 프로젝트: `C:\myDefense\_localbuild\worktrees\battle-development-showroom\Client`
- 브랜치: `feature/battle-development-showroom`
- HEAD: `54e3ee736028e34efcefc708065c8b721c99bbe9`
- 기존 showroom/HUD/이동 개선 WIP를 보존했다. 이번 요청의 새 작업만 아래에 구분한다.
- Unity MCP callable 도구는 0개였다. 지정한 Unity 6000.3.4f1 프로젝트를 Editor API로 실행해 importer, Catalog, Material, Profile 및 환경 Prefab 참조를 생성했다. YAML/GUID/meta 직접 작성은 하지 않았다.
- 실제 Kidnap·이동·Swap·Merge 승인 경로, 전투 수치, 타깃 잠금, 피해·Kill·Spawn 장부와 지속 Networked 레이아웃을 유지했다.

## 적용 내용

| 영역 | 반영 내용 |
| --- | --- |
| 행성 | 9개 `SFDetailBattlefields` Terrain을 추가했다. NEPTUNE은 푸른 대기 곡면·폭풍 소용돌이·부유 SF 관측소, URANUS는 청록 대기/기울어진 고리, SATURN은 고리/잔해, JUPITER는 폭풍대, MARS는 협곡, EARTH는 생태/궤도, VENUS는 고압 구름, MERCURY는 명암 경계, SUN은 코로나를 표현한다. |
| 보드 | 기존 공통 4×6 매트 SF 타일을 유지하고 외곽 시설·재질·배관·조명과 행성 대기에 디테일을 배치했다. Board/Lane/Waypoint의 논리 길이·배치는 유지한다. |
| HUD | 필드 구분 접두사를 제거하고 실제 roster ID와 Monster 수를 각각 중앙 정렬했다. 상태/Boss 타이머를 별도 행에 예약했다. 한글은 NotoSansKR-Medium, 숫자는 기존 Asap-Bold를 사용한다. |
| Kidnap | UFO 그림이 있는 새 3:1 래스터 버튼, 실시간 가격 및 상태 표시를 적용했다. 원래 버튼 이벤트와 비용 검증을 그대로 사용한다. Noto OFL을 두 빌드에 동봉했다. |
| Merge 짝 | 실제 동일 종·등급 기준으로 밝은 초록 이중선, 검정 대비선과 별도 `합성` 라벨을 표시한다. 선택은 시안으로 구분하며 본체를 흔들거나 깜빡이지 않는다. |
| Merge 효과 | 실제 권위 승인 후 transient sequence별 1회 재생한다. 거부·재조정·스냅샷/late join에는 재생하지 않는다. Legendary 재료 합성은 재생하고 최종 Mythic 선택에서 중복 재생하지 않는다. |
| Monster | canonical `NORMAL_MONSTER`, `ELITE_MONSTER`, `WAVE_BOSS` 각각에 3방향×4 보행 프레임을 매핑했다. 좌향은 우향을 반전한다. 장갑형 침입 생물의 실루엣·색·크기를 위협 단계별로 구분한다. |
| 포탈 | 최종 성공 Spawn 통지에만 반응한다. 해당 peer의 필드 remap·NetworkObject 준비·Session/Runtime ID를 확인하고 실제 Lane 시작점에서 표시한다. late join 시 기존 Spawn 효과를 재생하지 않는다. |
| 피격·사망 | 실제 HP 감소 때만 짧은 표현을 재생한다. 복제 IsDead를 따라 fade/HP 숨김을 처리하고 기존 소멸 시점을 따른다. 피격 RGB 밝기를 낮춰 외형 디테일을 유지한다. |
| 투사체 | 논리 비행·명중 시점은 유지하고 보이는 그림/명중 효과의 끝점만 최신 remap 위치의 Monster 몸체 소켓에 맞춘다. NetworkId와 Session/Runtime ID가 다른 지연 통지는 생략한다. |

Higgsfield의 현재 모델과 견적을 확인한 뒤 실제 생성을 요청했으나 `Requires basic plan or higher`로 거절됐다. 생성 Job은 만들어지지 않았다. 사용자 승인된 기본 이미지 생성으로 새 PNG 15장을 제작했다. 이전 아트 85장의 SHA는 모두 보존했고 새 15장은 생성 원본과 일치한다. 기존 Terrain/매트 아트도 남겨 두었다.

## 자동 검증과 리뷰

| 검사 | 최종 결과 |
| --- | --- |
| 전체 EditMode R8 | 951개 중 950 PASS, 기존 Balance 해시 fixture 1 FAIL, Skip 0 |
| 표현·보드·행성·쇼룸 관련 집중 집계 | 143/143 PASS |
| Monster 표현 | 18/18 PASS |
| HUD | 21/21 PASS |
| 보드 Interaction | 17/17 PASS |
| Development Battle-only 빌드 | 성공, C#/Shader 오류 0 |
| Production compile check Battle-only 빌드 | 성공, C#/Shader 오류 0 |
| Noto 라이선스 동봉 | 양쪽 `ThirdPartyNotices/NotoSansKR-OFL.txt`, 4,301 bytes |
| Profile 오프라인 Unity 렌더 | 9/9 생성 성공; NEPTUNE/SATURN/MARS 대표 렌더의 배치·재질을 추가 시각 검사 |
| 독립 읽기 전용 리뷰 | 최종 R8 승인, 필수 수정 0 |

기존 실패는 `BattleCanonicalBalanceTests.CanonicalBundle_LoadsManifestRegistriesAndMappings`다. 테스트는 `1-2c24f2a29fd63b81`을 기대하고 현재 generated manifest는 `1-e37af4a5ff9b7204`다. 해당 테스트, Balance JSON/Excel 및 계약을 이번 작업에서 변경하지 않았다. 전체 PASS로 판정하지 않는다.

실행 중 발견한 HUD Graphic/Image 중복 부착과 Monster MPB 생성 시점 오류는 실제 Unity 초기화 결함으로 수정했다. 실제 화면에서 발견한 Noto 라벨 높이 부족도 수정했다. 최종 회귀는 설정값뿐 아니라 실제 Badge mesh 색, 한글 Text mesh 정점, 3개 화면비의 상태/Boss 문구 생성과 행간 분리를 검사한다. 초기 실패 로그와 XML은 보존한다.

## 실제 Host Client 검증

동일 PC에서 같은 빌드를 두 개 실행했다. Editor Host 및 실제 2PC 결과로 확대 해석하지 않는다. 서버는 loopback 18082의 별도 H2 메모리 DB이며 P1VAL Settlement coordinator/POST는 비활성이다.

| 세션 | 관찰 결과 |
| --- | --- |
| R5 `P1VAL-NEPTUNE-W009-13b042e466ce` | 두 역할의 새 HUD·실제 납치·유닛 복제, 두 Lane 포탈·Monster 이동/피격을 관찰했다. 보존 시점의 이동/Swap/Merge 83건 및 Merge 22건이 양쪽 일치했고 오류 0이었다. kinggusi 회신: `효과 보임 · 이동도 부드러움`. |
| R7 `P1VAL-NEPTUNE-W010-43becdbe6239` | Boss 외형·공용 Lane·공격·소멸을 관찰했다. 합성선은 밝아졌으나 한글 라벨/상태행이 렌더되지 않는 문제를 잡아 R8에서 해결했다. |
| R8 `P1VAL-NEPTUNE-W010-9ef561e22a85` | ID·Count 중앙 정렬, 협동 상태행, UFO 버튼/가격, 밝은 짝 2개와 실제 `합성` 글자를 확인했다. 양쪽에서 Boss·포탈·보행·피격·잔여 시간과 종료 후 소멸을 확인했다. Host 로그에 `Boss defeated! Next round criteria cleared.`가 기록됐고 양쪽 exception/HTTP500/Shader 오류 0이었다. |

R8 캡처: `final-r8-merge-hints.png`, `final-r8-boss-host.png`, `final-r8-boss-client.png`. 두 Boss 캡처는 순차 촬영이므로 동일 Tick의 HP/좌표 계측 자료는 아니다. HP·타깃 계약은 자동 회귀로 보존 확인했으며 정밀 동시 측정과 장시간 2PC는 별도다.

9개 Profile 렌더는 오프라인 재질·환경 확인 자료다. 실제 네트워크 검증은 위의 두 Standalone 세션으로 구분한다. Elite의 실제 다수 전투·장시간 성능, 모든 행성의 난이도/사람 비주얼 인수는 이번 결과로 완료하지 않는다.

## 재실행과 확인 순서

현재 격리 서버가 실행 중일 때 이전 두 검증 창을 종료한 뒤 실행한다.

```powershell
& "C:\myDefense\_localbuild\P2Validation\BattlePresentationPolish_20261124\Start-BattlePair.ps1" -Map NEPTUNE -Wave 9
```

1. 실제 왹져 납치로 보드를 채운 뒤 유닛을 선택한다. 동일 ID·등급 짝에만 밝은 이중선과 `합성`이 나와야 한다.
2. 빈칸 이동, 잘못된 짝 Swap, 올바른 짝 Merge를 확인한다. 성공 효과는 1회이고 상대 창의 결과가 같아야 한다.
3. Host에서 `Show P1 Fixture` → `Start P1 Wave 009`를 누른다. Wave000은 수동 시작 전의 armed 상태다.
4. 각자의 좌하단/상대 좌상단 포탈, Monster 보행·모서리·공격·사망 소멸과 HP를 비교한다.
5. Boss는 기존 창을 종료하고 `-Wave 10`으로 새 세션을 만든 뒤 Host에서 시작한다. Boss 타이머가 ID/Count를 가리지 않아야 한다.
6. 행성은 `-Map SATURN`, `MARS` 등으로 바꿔 새 세션에서 비교한다. 9행성 모두 최종 아트 인수는 별도 회신한다.

## 변경 파일

- Presentation: `BattleHudPresentation`, `BattleHudLayout`, `BattleHudGraphic`, `BattleHudStyleCatalog`, `BattleBoardInteractionPresentation`, `BattleMonsterArtCatalog`, `BattleMonsterArtView`, `BattleMonsterPresentationCoordinator`, `BattleMonsterArt.shader`, `MonsterHealthView`, `BattlePaintedProjectileView`, `FusionBattleUiController`.
- Battle 연결: `BattleWaveStateAuthority.Presentation.cs`, 기존 `BattleWaveStateAuthority`, `BattleWaveExecutor`, `BattleMonsterNetworkState`, `BattleProjectileNetworkState`, `BattleProjectileSpawner`의 표현 경계.
- Editor: `BattlePresentationRefinementAssetGenerator`, `PlanetContentAssetGenerator`, 기존 Feature Test Catalog/Hub 검증 안내.
- 테스트: `BattleMonsterPresentationTests`, `BattleHudPresentationTests`, `BattleBoardInteractionPresentationTests`, `PlanetBattlefieldTests`, `PlanetContentTests`.
- 아트: `Art/Battle/Planets/SFDetailBattlefields` PNG 9장, Monster atlas 3장, portal/Merge/CTA PNG 3장, Noto font/OFL. Resources Catalog와 행성 Material/Profile/환경 Prefab 참조 및 Unity 생성 meta.
- 문서: 이 보고서와 `98_IMPLEMENTATION_TASKS.md`. 기존 누적 변경 전체를 이번 요청의 신규 변경으로 주장하지 않는다.

## 증거와 남은 작업

증거 루트: `C:\myDefense\_localbuild\P2Validation\BattlePresentationPolish_20261124`

`generation-manifest.json`, `generated-art-validation.json`, `asset-copy-validation.json`, `test-validation-r8.json`, `editmode-all-r8.xml`, `build-validation-r8.json`, `runtime-validation-r8.json`, `human-merge-validation.json`, `planet-renders-r8` 및 원본 로그를 보관한다.

| 순서 | 다음 작업 | 조건/현재 상태 |
| --- | --- | --- |
| 1 | 새 행성·HUD·Monster·합성 표시 최종 사람 인수 | kinggusi 효과/이동 PASS 확보. 두 담당자의 전체 비주얼 인수 대기. |
| 2 | 종료 후 Kidnap 권위 차단 | 기존 사용자 승인 범위다. 현재 `ProcessKidnap`의 비용·횟수·보드 변경 전 RUNNING 검사 보완과 CLEARED/FAILED 반복 요청 검증이 남아 있다. Merge/Mutation/공명은 별도 점검으로 유지한다. |
| 3 | Elite 포함 다수 Monster·Boss 반복 전투/성능 | 새 표현의 장시간 프레임·메모리·포탈/효과 잔류와 동기화 측정. 실제 2PC는 별도 일정 유지. |
| 4 | Daily 정상 구성 난이도·실제 DoT/Splash·trusted Result | SSOT상 S1~S5 Fixture 완주/소비 Snapshot 검사는 이미 수행됐다. 정상 구성 난이도와 실제 피해, 종료 후 조작, trusted Adapter E2E를 구분해 이어간다. |
| 5 | 정식 캐릭터 ID·고유 스킬 연결 | 현재 네 후보는 Development 외형 alias다. User/System canonical ID·스킬 채택안 계약 필요. |
| 6 | Production Adapter/정산·Quest·9행성 난이도 | 운영 인증·trusted adapter와 기존 유예/출시 게이트 유지. 이번 디자인 검사로 완료하지 않음. |

최종 Git 가드: 브랜치/HEAD 유지, staged 0, 보호 경로 변경 0, Shared·서버·Balance 변경 0, 기존 PNG 85장 변경 0, 신규 PNG 15장 원본 불일치 0/누락 meta 0, 전체 신규 Asset 누락 meta 0, `git diff --check` 오류 0이다. Unity 자동 생성 설정·sample meta만 시작 때의 깨끗한 상태로 복원했다. `final-git-guard.json`에 보관했다. 커밋·푸시는 하지 않았다.
