# P2-2 Daily Battle Balance/Shared 선행 계약 검증

검증일: 2026-09-02

## 범위

- `DailyBattleStage` Excel 50행과 생성 JSON
- Spring strict loader, validator, registry
- Unity canonical loader와 Stage registry
- Unity/Spring `DailyBattleSessionContext` schema v1 직렬화 계약
- 기존 서버·BalanceTool·Unity EditMode 회귀

## 결과

| 검증 | 결과 |
|---|---:|
| `compileJava` / `compileBalanceToolJava` | PASS |
| `convertBalance` | PASS, 25개 파일 생성 |
| `syncCanonicalBalanceToUnity` | PASS |
| Spring 전체 테스트 | 363/363 PASS |
| BalanceTool 전체 테스트 | 82/82 PASS |
| Unity 계약 집중 EditMode | 32/32 PASS |
| Unity 전체 EditMode | 506/506 PASS |
| Unity 컴파일 Console | Error 0 |
| `git diff --check` | PASS |

생성 Balance는 schema v1, `balanceVersion=1-b6ca576fc911aecb`, `contentHash=b6ca576fc911aecbdff4817778532fc2547bc734972538b3a36e5b8d54df63b2`다.

## 후속 Battle 게이트

- State Authority가 Daily Session 문맥을 받아 Player 1 Board/Lane만 활성화
- `DailyBattleStage` Wave/Timer/Monster/Boss/상태 이상 실행
- 일반 Settlement와 분리된 Daily Result trusted Adapter 제출
- Production Adapter 부재·문맥 불일치 시 fail-closed
- placeholder 환경 Profile의 사람 비주얼 검증
