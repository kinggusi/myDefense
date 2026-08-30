# P2-3-2 Battle Settlement 장부 검증

검증일: 2026-08-30
기준: `origin/dev` `161350b` (Shared v2 PR #105)

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

증거 파일:

- `C:\myDefense\_localbuild\P2Validation\p2-3-smoke-host.log`
- `C:\myDefense\_localbuild\P2Validation\p2-3-smoke-client.log`

## 경계 및 잔여 검증

- `ABORTED`는 현재 Battle `MatchState`가 생성할 수 없는 transport 결과다. Shared DTO/서버 검증에서는 두 배열을 비우는 계약을 유지하고, Battle runtime은 CLEARED/FAILED만 생성한다.
- Fusion Host/Client 연결과 trusted roster는 확인했다. 실제 FAILED 미완료 Wave Settlement HTTP Smoke는 Development Build에서 양쪽 보드에 Unit을 배치해 terminal 상태를 만든 뒤 별도 수행해야 한다.
- Quest 영구 진행과 정확히 한 번 멱등 처리는 User/System의 `QuestSettlementProcessor` 연결 범위다.
- Spawn 사실과 개인 Kill 귀속의 최종 신뢰 경계는 기존대로 Fusion State Authority다.
