# P2-3-2 Battle Settlement 장부 검증

검증일: 2026-08-30
기준: `origin/dev` `161350b` (Shared v2 PR #105)

최신 동기화 재검증: 2026-08-31, `origin/dev` `f2ff276` (P2-1-1 및 PlanetContent PR #102 포함)

## 구현 범위

- canonical `spawnGroupId`를 `WaveSpawnSpecData`와 Wave runtime까지 보존
- Fusion State Authority의 성공 Spawn을 Runtime ID, Wave, group, row, ordinal, Lane, owner slot 장부로 기록
- FAILED의 `finalWave + 1` 실제 Spawn 전체를 `waveSpawnFacts`로 투영
- 같은 Wave에서 실제 Kill된 Spawn만 `partialWaveKills`로 투영
- killer/support 사용자 ID를 trusted Battle Summary의 Player slot 1/2로 변환
- `runtimeMonsterId`를 `ulong` 숫자 순서로 정렬한 뒤 invariant decimal string으로 전송
- 개인/팀 Kill, Support, Boss, KillGold 합계와 Player Gold 장부식 검증
- VICTORY에서는 두 미완료 Wave 배열을 빈 배열로 유지

Development UI, Scene, Prefab, Shared DTO, Spring, User/System 코드는 변경하지 않았다.

## 자동 검증

- Unity ScriptAssemblies 컴파일: error CS 0, Bee final ExitCode 0
- Battle 집중 EditMode: 45/45 PASS
- Unity 전체 EditMode: 462/462 PASS, failed 0, skipped 0
- Windows Standalone Build(`Battle.unity` only): PASS
- Unity/Spring 동일 v2 hash fixture: PASS
  - `d48e3596480b89baa9b17e71acb8e9a833cfc1eb42fe8d46aa8653250e0bb2a6`
- FAILED Spawn 전체/Kill 부분집합 투영: PASS
- unsigned Runtime ID 경계 정렬(`2`, `2^63`, `ulong.MaxValue`): PASS
- partial Kill의 Spawn 증거 누락 거부: PASS
- 기존 PlayerSlot 0 호환 생성자 회귀: 전체 EditMode에서 확인 후 양수 배정 슬롯만 중복 검사하도록 보정, 재실행 PASS

### `origin/dev` `f2ff276` 병합 회귀

- 무커밋 merge: 충돌 0, unmerged path 0
- Unity MCP: 현재 구현 Thread에서 호출 가능한 도구 없음; Scene/Prefab 변경 없음
- Unity ScriptAssemblies 컴파일: error CS 0, batchmode 정상 종료
- Settlement + PlanetContent/StateAuthority 집중 EditMode: 160/160 PASS
- Unity 전체 EditMode: 481/481 PASS, failed 0, skipped 0
- Battle Scene 검사: dirty false, Missing Script 0, Broken Prefab 0
- Windows Standalone Development Build: `Battle.unity` 단독 PASS, build error 0
- PlanetContent authoritative `mapId` 불변/fail-closed와 P2-3 Spawn/Kill audit·Settlement projection 병존 확인

추가 증거 파일:

- `C:\myDefense\_localbuild\P2Validation\p2-3-pr-sync-compile.log`
- `C:\myDefense\_localbuild\P2Validation\p2-3-pr-sync-targeted.xml`
- `C:\myDefense\_localbuild\P2Validation\p2-3-pr-sync-full.xml`
- `C:\myDefense\_localbuild\P2Validation\p2-3-pr-sync-build.log`
- `C:\myDefense\_localbuild\P2Validation\P2-3-PRSync-Build\Client.exe`

증거 파일:

- `C:\myDefense\_localbuild\P2Validation\p2-3-ledger-compile-r6.log`
- `C:\myDefense\_localbuild\P2Validation\p2-3-ledger-targeted-r2.xml`
- `C:\myDefense\_localbuild\P2Validation\p2-3-ledger-full-editmode-r3.xml`
- `C:\myDefense\_localbuild\P2Validation\p2-3-ledger-build.log`

## Host/Client Smoke

- Session: `P2QUEST-FAILED-20260830-01` (새 non-P1VAL Session)
- Windows Standalone Host + Client의 동일 Fusion Session 입장: PASS
- trusted local roster 등록: PASS
- 양쪽 Battle balance hash 일치 및 W001 시작: PASS
- Client lane presentation remap: PASS
- 자동 실행 환경은 보드가 비어 있어 W001 생존 Monster를 처치하지 못하고 다음 Wave로 진행하지 않았다.
- 따라서 실제 FAILED terminal Summary POST, Spring 응답, `summaryHash` 대조는 이번 자동 Smoke에서 발생하지 않았다.

### 부분 Wave FAILED Development Fixture

최신 후속 브랜치에는 실제 미완료 Wave 장부를 안전하게 종료 Summary로 만들기 위한 Development Build 전용 버튼을 추가했다.

1. Spring을 `local` profile로 실행한다.
2. 새 non-P1VAL Session에 Host=`dev-host`, Client=`dev-client`로 접속한다.
3. `Show P1 Fixture`에서 Host/Client 보드에 Unit을 생성한다.
4. 현재 Wave에서 Monster가 최소 1마리 처치되고 Spawn audit 중 최소 1개가 아직 Kill audit으로 해소되지 않은 시점에 Host에서 `Force FAILED for partial Settlement (Host only)`를 누른다.
5. Host 로그의 `[P2QuestFixture]`, Development 전용 `[BattleSettlement] request-json=...`, 응답과 Spring 저장 결과를 대조한다.
6. `request-json=` 뒤의 동일 JSON을 `POST /api/battle/settlements`로 다시 보내 `alreadyProcessed=true`를 확인한다.

버튼은 State Authority, `RUNNING`, `currentWave == highestClearedWave + 1`, 실제 Spawn audit, 실제 Kill audit, 아직 Kill audit으로 해소되지 않은 Spawn을 모두 요구한다. P1VAL Session과 완료 Wave, Kill 0, 모든 Spawn 처치 완료, 불일치 장부는 거부한다. 전체 진입점과 요청 JSON 로그는 `UNITY_EDITOR || DEVELOPMENT_BUILD`에서만 컴파일된다.

자동 검증 결과:

- Fixture 최종 규칙: 29/29 PASS
- Settlement/State Authority 포함 집중 EditMode: 117/117 PASS
- Unity 전체 EditMode: 493/493 PASS
- `Battle.unity` 단독 Windows Development Build: SUCCESS, compiler/build error 0
- 최종 로컬 검증 Build는 팀 Photon App ID를 Unity Editor API로 빌드 순간에만 주입했으며, 소스 `PhotonAppSettings.asset`은 즉시 원복되어 Git 변경 0
- 독립 리뷰: PASS, 코드 차단 결함 0

실제 HTTP Smoke 결과:

- Session: `P23-PARTIAL-20260901-225458` (새 non-P1VAL Session)
- Host=`dev-host`, Client=`dev-client`, trusted roster 등록: PASS
- 실제 terminal Summary: `DEFEAT`, `finalWave=2`, 미완료 `spawnWave=3`
- `waveSpawnFacts=4`, `partialWaveKills=2`, 모든 Spawn fact가 `finalWave + 1`: PASS
- 개인 Kill 합계 52 == 팀 Monster Kill 합계 52: PASS
- 최초 POST: `ACCEPTED`, `alreadyProcessed=false`
- Unity 전송 `summaryHash`와 독립 canonical SHA-256 재계산값: `f4e19a86a9f517aba58f02b791f0d383736de7b71b23297b7416b40760d4c176`, 일치 PASS
- 캡처한 동일 JSON 재전송: `ACCEPTED`, `alreadyProcessed=true`, 보상 배열 0
- H2 저장: Settlement 1건, Player Settlement 2건, Reward Claim 2건. 재전송 후 중복 Settlement 없음
- Host/Client 기능 신규 Exception·NullReference·Settlement Error: 0

증거 파일:

- `C:\myDefense\_localbuild\P2Validation\P2-3-PartialSmoke\Logs\P23-PARTIAL-20260901-225458-host.log`
- `C:\myDefense\_localbuild\P2Validation\P2-3-PartialSmoke\Logs\P23-PARTIAL-20260901-225458-client.log`
- `C:\myDefense\_localbuild\P2Validation\P2-3-PartialSmoke\Logs\spring-local-8082.out.log`
- `C:\myDefense\_localbuild\P2Validation\P2-3-PartialSmoke\Logs\P23-PARTIAL-20260901-225458-retry-db-evidence.txt`
- `C:\myDefense\_localbuild\P2Validation\p2-3-smoke-fixture-full-final.xml`
- `C:\myDefense\_localbuild\P2Validation\p2-3-partial-smoke-build-team-photon.log`

## 경계 및 잔여 검증

- `ABORTED`는 현재 Battle `MatchState`가 생성할 수 없는 transport 결과다. Shared DTO/서버 검증에서는 두 배열을 비우는 계약을 유지하고, Battle runtime은 CLEARED/FAILED만 생성한다.
- Fusion Host/Client 연결, trusted roster, 실제 FAILED 미완료 Wave Settlement HTTP 및 동일 payload 재전송은 통과했다.
- Quest 영구 진행과 정확히 한 번 멱등 처리는 User/System의 `QuestSettlementProcessor` 연결 범위다.
- Spawn 사실과 개인 Kill 귀속의 최종 신뢰 경계는 기존대로 Fusion State Authority다.
