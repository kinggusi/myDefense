# P0-1-5 Battle 계약 사용처 검토

## 검토 범위

- Unity Battle Runtime: `BattleSessionContext`, `BattleSummary`, `BattleWaveExecutor`, `BattleKillDeduplicator`
- Shared 계약: `BattleState`, `BattleSettlementSummary`
- Spring DTO: `BattleSessionSnapshotDtos`, `BattleSettlementDtos`, `LegendaryChoiceStateDtos`

## 확인 결과

### BattleSessionSnapshot

- Snapshot은 재접속 복구용 transport 계약이며 Fusion live state를 대체하지 않는다.
- 플레이어 2명, 슬롯 `{1,2}`, 개인 Gold/Kidnap 비용, 현재 Wave/Boss 타이머, 보드 객체를 포함한다.
- Unity validator의 `waveType` 허용값은 `REGULAR`/`BOSS`이며 Battle Balance의 `WaveType`과 일치한다.
- `eliminatedWave`와 `alienSpecId`는 nullable JSON 값으로 유지한다.
- Java fixture의 `NORMAL` 표기는 계약과 불일치하므로 `REGULAR`로 정정한다.

### LegendaryChoiceState

- Legendary Merge 결과 선택 창의 Shared 상태로, 재료 2종, 후보 3종, 리롤 잔여량, 10초 제한(리롤 성공 시 10초 재설정), 선택/자동선택 상태를 포함한다.
- `selectedAlienId`는 선택 전 `null`을 허용하며, 선택 후 후보 중 하나여야 한다.
- Unity 전용 serializer가 enum을 문자열로, nullable ID를 JSON `null`로 출력해 Spring `String phase`/`Long selectedAlienId`와 대응한다.

### BattleSettlementSummary

- `BattleSummary`의 최종 결과를 Spring Settlement DTO로 변환할 때 사용하는 별도 transport 계약이다.
- `BattleSummary` 자체는 현재 runtime 집계 모델이라 playerSlot, initial/final Gold, supportKills, monster totalKillGold/bossKills, startedAt/finishedAt, requestId/summaryHash를 직접 보유하지 않는다.
- 따라서 Settlement 전송 adapter와 Gold/killGold 장부 연결은 P0-10 범위에서 구현해야 하며, 이번 Shared 계약 단계에서 Battle Runtime을 임의 수정하지 않는다.

## P0-1-6 반영

1. Snapshot Java 계약 fixture의 `waveType`을 `REGULAR`로 고정한다.
2. Unity Snapshot validator와 Java record 필드 순서를 유지한다.
3. Legendary choice wire serializer와 계약 테스트를 유지한다.
4. Settlement runtime adapter는 후속 P0-10에서 추가한다.

## 영향 및 위험

- 기존 Battle Runtime API의 enum/이벤트 시그니처는 변경하지 않는다.
- Snapshot/Legendary 계약은 신규 필드 추가이므로 기존 호출자의 breaking change가 없다.
- Settlement 전송 시점에 BattleSummary와 SettlementSummary 사이의 장부 변환이 누락되면 전송이 불가능하므로 P0-10 구현 전에 adapter 설계가 필요하다.
