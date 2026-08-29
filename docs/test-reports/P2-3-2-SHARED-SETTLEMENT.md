# P2-3-2 Shared Settlement 검증

검증일: 2026-08-29

## 범위

- Unity/Spring `partialWaveKills` 1:1 DTO 계약
- FAILED 미완료 Wave(`finalWave + 1`) 처치 증거 검증
- canonical JSON과 SHA-256 상호운용
- 탈락 Wave와 활성 Lane별 완료 Wave Kill 기대치

## 결과

- Spring 집중 Settlement/DTO/Hasher/E2E 테스트: PASS
- Spring 전체 테스트: 312/312, failures 0, errors 0, skipped 0
- BalanceTool: 70/70, failures 0, errors 0, skipped 0
- Unity Settlement 계약 테스트: 19/19
- Unity 전체 EditMode: 442/442, failed 0, skipped 0
- Unity 신규 compile error: 0

## 계약 판정

- `runtimeMonsterId`는 C# `ulong` 범위를 보존하는 decimal string이다.
- `partialWaveKills`는 FAILED 미완료 Wave만 허용한다.
- Runtime ID unsigned 정렬과 canonical Spawn 위치 중복을 서버가 검증한다.
- payload 수정 후 이전 `summaryHash` 재사용은 거부된다.
- 미완료 Wave killer/support/Boss 개인 귀속이 `players` 집계에 포함되지 않으면 거부된다.
- Spring은 canonical Spawn 위치와 수량 상한을 검증하지만 Fusion의 실제 Spawn 이력을 독립 보유하지 않으므로, Runtime ID의 실제 생성 사실과 개인 귀속은 trusted State Authority 신뢰 경계다.
- 구버전 Unity Serializer와는 호환되지 않으므로 Unity/Spring 동시 배포가 필요하다.

## 잔여 작업

- Battle 담당이 State Authority Kill 장부에서 `spawnOrder`, `spawnOrdinal`, `playerSlot`을 채워 `partialWaveKills`로 투영한다.
- Quest Settlement Processor가 승인된 Settlement의 개인/팀 집계를 멱등 반영한다.
- 실제 Host/Client FAILED 미완료 Wave Settlement를 2클라이언트로 확인한다.
