# Battle 조작·합성 표시·매트 맵 개선 — 2026-11-24

상태: 구현·집중 자동 검증·독립 코드 리뷰 통과. kinggusi가 실제 이동·Merge가 모두 부드럽다고 확인했다. jjangash 조작 확인과 양쪽 최종 비주얼 인수는 검증 대기. 전체 회귀 PASS로 판정하지 않는다.

## 범위와 원인

- 실제 Battle.unity의 표시·조작 경로를 보완했다. 별도 쇼룸 전투 로직은 만들지 않았다.
- HUD가 매 프레임 mythic-choice/Alien 설정을 조회하던 경로를 immutable SessionContext와 선택 metadata 키 캐시로 변경했다. 화면 값은 최대20Hz로 갱신하며 실제 명령은 기존 State Authority에서 다시 검증한다.
- Client 드롭 시 원래 칸으로 먼저 복귀하던 표시를 render-only 미리보기와 승인 대기로 변경했다. 실제 Unit root, Collider, 공격 원점은 승인 전 원래 권위 위치를 유지한다.
- Board registry와 클릭당 공유 picking을 사용하고, 반복 전역 탐색과 동일 pose의 중복 Sprite/Mesh 생성을 줄였다.
- 승인·거절/timeout·terminal·disconnect·Session reset·소스 소멸/교체 경계를 처리한다. Pending 중 추가 press는 버리며 승인 뒤 지연 실행하지 않는다.
- Puppet ghost의 조종선도 함께 이동하도록 복제본의 world-space 점만 local-space로 변환한다. 원본 line/공격 원점은 유지한다.

## Merge 표시

실제 규칙은 동일 소유자의 같은 Alien 종 ID + 같은 등급이다. Normal/Epic/Unique/Legendary가 대상이며 Mythic은 최고 등급이라 Merge할 수 없다. Mutation Injector와 Mythic 선택 중 잠긴 보드는 짝에서 제외한다.

- 발밑 작은 배지: N/E/U/L/M과 등급 색.
- 선택: 청록 테두리.
- 실제 Merge 짝: 초록 테두리.
- 하단: 실제 Alien ID, 등급, 짝 수, 승인 대기 표시.
- DNA 배지는 실제 화면에서 DN으로 잘리는 것을 발견해 R3에서34px로 넓혔다.
- 개발용 Mixed 외형은 여러 실제 종이 같은 캐릭터로 보일 수 있다. 외형만 같은 다른 종은 짝으로 표시하지 않는다.
- Merge 확률·풀·비용·명령·Shared 계약은 변경하지 않았다.

## 선명도와 맵

- 원본16 + DarkBold16 pose PNG의 SHA-256 변경0. 캐릭터 디자인을 다시 그리지 않았다.
- Unity importer를 통해32장에 mipmap, Trilinear, alpha coverage 보존을 적용했다. 기존 Effect PNG와 타일 원본은 유지한다.
- Built-in Forward Battle 카메라에4x MSAA를 요청하는 가역 runtime scope를 사용한다. 마지막 소유자가 해제될 때 기존 설정을 복원한다. ProjectSettings 자산은 수정하지 않는다.
- 9개 행성별 새 Terrain + 공통 SF Matte Surfaces atlas10장을 추가했다. 타일 중앙의 잔무늬를 줄이고 행성 특징은 외곽 지형·시설에 배치했다.
- 각 Profile은 정확한 mapId Terrain과 명시된 공통 atlas를 참조한다. 누락·혼재는 fail-fast하며 임의 fallback은 없다.
- 기존18장과 GenerateBattlefieldContent를 보존했다. 새 테마는 GenerateMatteBattlefieldContent로 Unity API에서 적용한다.

Higgsfield의 실제 생성 요청은 Requires basic plan or higher로 거절돼 job이 생성되지 않았다. 이번 요청의 대체 허용에 따라 기본 image_gen으로 제작했다. 원본 유닛·사용자 이미지는 이번 Higgsfield 시도에 업로드하지 않았다.

## 자동 검증

| 검사 | 결과 |
| --- | --- |
| Unity6000.3.4f1 컴파일 | C# 오류0 |
| 최종 R3 집중 EditMode |264/264 PASS, skip0 |
| R2 전체 EditMode |928/929 PASS, 기존 Balance fixture1 FAIL, skip0 |
| 신규 Interaction |16/16 PASS |
| Quality scope |1/1 PASS |
| HUD |18/18 PASS |
| Art |23/23 PASS |
| PlanetBattlefield |9/9 PASS |
| 원본 pose PNG 보존 |32/32 SHA-256 동일 |
| 새 PNG |10장, 전부 opaque, SHA-256 중복0/manifest 일치 |
| unit importer |32/32 mipmap·Trilinear·coverage 적용 |
| 독립 읽기 전용 리뷰 |R2/R3 승인, Required Changes0 |

기존 실패는 BattleCanonicalBalanceTests.CanonicalBundle_LoadsManifestRegistriesAndMappings의 고정 expected hash다. HEAD의 테스트는1-2c24f2a29fd63b81을 기대하지만 HEAD의 generated manifest는1-e37af4a5ff9b7204이다. 이번 작업에서 해당 테스트, canonical loader, Balance JSON/Excel은 변경하지 않았다. SSOT의 기존2026-11-02 기록에도 같은 Battle hash 실패1건이 명시돼 있다.

Unity MCP callable 도구0개를 확인했다. 모든 Profile/Prefab/importer 변경은 명시한 worktree Client를 Unity Editor API로 실행해 생성했다. YAML/GUID/meta를 직접 작성하지 않았다.

## 실제 실행 관찰

동일 PC의 Host/Client 검증이며 실제2PC 검증을 대체하지 않는다.

R2 세션: P1VAL-NEPTUNE-W009-a3de4683c8ce

- dev-art-host / dev-art-client 접속, 서버 계산 Attack Snapshot48개씩 로드 성공.
- Client Kidnap9회: Gold100000→99190. 양쪽 로그에서 같은 slot/spec가 복제된 것을 확인했다.
- 실제 화면에서 새 NEPTUNE 맵, N 배지, 청록 선택, Alien #26·NORMAL·Merge 짝2 및 초록 짝2개를 관찰했다.
- 같은 Puppet 외형으로 표시된 서로 다른 종은 Merge 짝0으로 표시됨을 관찰했다.
- 검사한 게임 exception/HTTP500은0.
- 대기 상태 trace는 policyReads1, mutationReads0, cachedPoses4/poseCreations4로 유지됐다. 최근2초 샘플은 mean16.67ms/max16.68ms였다. 전투 중 성능이나 이전 버전 대비 개선율을 의미하지 않는다.
- 자동 native drag는 drag started/실제 move 명령을 재현하지 못했다. 이후 kinggusi가 직접 조작해 “이동·Merge 모두 부드러움”으로 회신했다. Host/Client에 기록된 move/merge/swap31건이 일치했고 양쪽 게임 exception/HTTP500은0이었다. 실제 확인과 자동 입력 도구의 한계를 구분한다.
- P1VAL은 Settlement coordinator/POST가 비활성이다. 실제 계정·Quest·영구 보상에 사용하지 않는다.
- Wave000은 검증 Wave009를 수동으로 시작하기 전의 armed 상태다.

## 실행 및 사람 확인

격리 메모리 서버가 계속 실행 중일 때 다음 launcher를 사용한다. 이전 테스트 창은 먼저 종료한다.

```powershell
& "C:\myDefense\_localbuild\P2Validation\BattleInteractionMatte_20261124\Start-BattlePair.ps1" -Map NEPTUNE -Wave 9
```

1. Client에서 왹져 납치를 여러 번 눌러 같은 종이 있는 보드를 만든다.
2. 유닛을 선택해 하단 ID/등급과 초록 Merge 짝을 확인한다. 같은 외형의 다른 ID는 초록 표시가 없어야 한다.
3. 빈 타일로 천천히/빠르게 이동한다. 잡은 위치와 미리보기가 맞고, 드롭 직후 원래 칸으로 되튀지 않는지 확인한다.
4. 초록 짝으로 Merge한다. 유닛 수가1개 줄고 다음 등급 배지로 바뀌며 양쪽 화면 결과가 같아야 한다.
5. Merge 불가 조합은 기존 Swap 동작을 확인한다. Mythic/Injector를 잘못 합성 표시하지 않아야 한다.
6. 퍼펫티어를 이동해 인형·조종선이 함께 따라오는지 확인한다.
7. 축소/기본 크기에서 검은 외곽선·작은 세부와 배지가 선명한지 확인한다.
8. Host의 Show P1 Fixture에서 Start P1 Wave009를 누른 뒤 공격·Projectile 원점과 양쪽 복제를 확인한다.
9. 맵 취향은 NEPTUNE 외 행성도 -Map으로 바꿔 별도 새 세션에서 확인한다.

jjangash: 조작·비주얼 검증 대기. kinggusi: 실제 이동/합성 체감 PASS, 최종 비주얼 인수는 명시적 회신 전까지 대기. 양쪽 게이트 PASS 전 작업 완료로 올리지 않는다.

## 변경 파일

- Presentation: FusionBattleUiController, FusionKidnapBoardView, FusionKidnapUnitDrag, BattleHudPresentation, BattleDevelopmentArtOverride, BattleUnitArtView, BattleSpriteArt, BattleUnitRelief.shader.
- 신규 Presentation: BattleBoardInteractionPresentation.cs, BattlePresentationQuality.cs, BattleRenderGhostLease.cs.
- Editor: BattleArtAssetGenerator.cs, BattleShowroomSceneBuilder.cs, PlanetContentAssetGenerator.cs, PlanetBattlefieldPreviewExporter.cs.
- Tests: BattleBoardInteractionPresentationTests.cs, BattlePresentationQualityTests.cs, BattleHudPresentationTests.cs, BattleArtPresentationTests.cs, PlanetBattlefieldTests.cs, PlanetContentTests.cs.
- Assets: Planets/MatteBattlefields PNG10장과 Unity 생성 meta, pose importer32개, 행성 Profile/Material의 새 테마 참조.
- 문서: 이 보고서와98_IMPLEMENTATION_TASKS.md.
- 기존 showroom/HUD 누적 WIP는 그대로 보존했다. 위 목록은 이번 요청의 변경 범위를 나타내며 누적 branch 전체를 새 작업으로 주장하지 않는다.

## 증거와 제한

증거 루트: C:\myDefense\_localbuild\P2Validation\BattleInteractionMatte_20261124

generation-manifest.json, generated-art-validation.json, unit-png-preservation.json, unit-importer-validation.json, runtime-observation.json, pair-r2-a3de4683c8ce.json, Unity XML/log를 함께 보관한다.

Production JWT/실제 matchmaking·정산 E2E·실제2PC·전투 장시간 성능·모든 행성 비주얼 승인은 이번 자동 결과로 완료 처리하지 않는다. commit/push는 하지 않는다.

## 최종 확인

- R3 Development: C:\myDefense\_localbuild\P2Validation\BattleInteractionMatteClient_R3_20261124\Client.exe — Battle.unity만 포함, build exit0, C#/Shader 오류0.
- R3 production compile check: C:\myDefense\_localbuild\P2Validation\BattleInteractionMatteProduction_R3_20261124\Client.exe — build exit0, C#/Shader 오류0. 실제 production 서비스 접속 검증은 하지 않았다.
- 사람이 조작한 현재 창은 R2이며, R3는 DNA 배지 폭만24→34px로 바뀌었다. Launcher는 다음 실행부터 R3를 사용한다. 사람 테스트 보드와 창은 유지했다.
- Unity 실행이 자동 재직렬화한 QualitySettings와 codecoverage/UnityConnect, slnx, sample meta를 시작 때의 깨끗한 상태로 복원했다. ProjectSettings/Packages/PhotonAppSettings/sample 삭제 diff0.
- feature/battle-development-showroom, HEAD54e3ee736028e34efcefc708065c8b721c99bbe9 유지, staged0, git diff --check PASS, commit/push0.
- 최종 증거: build-validation-r3.json, human-move-merge-validation.json(양쪽31개 명령 일치), battle-interaction-focused-r3-20261124.xml(264/264), battle-interaction-editmode-all-r2-20261124.xml(928/929).
