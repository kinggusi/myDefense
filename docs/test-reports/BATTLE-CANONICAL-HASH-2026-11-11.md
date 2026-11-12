# Battle canonical balance 기대 해시 현행화

## 범위와 상태

- 사용자 명시 승인에 따라 User/System 작업 스레드에서 Battle 테스트의 오래된 고정 기대값 **2줄만** 현행화했다.
- 변경 코드: `Client/Assets/Editor/Tests/BattleCanonicalBalanceTests.cs`의 `CanonicalBundle_LoadsManifestRegistriesAndMappings` 내 `BalanceVersion`/`ContentHash` assertion.
- 기대값은 계속 고정 문자열이다. 실제 manifest를 그대로 기대값으로 읽는 자기참조 검사로 바꾸거나 assertion을 완화하지 않았다.
- Battle 런타임·밸런스 수치·generated JSON·서버·Shared 계약·Scene/Prefab/meta는 변경하지 않았다. 선행 UI/미커밋 변경도 보존했다.
- 상태: Unity 전체/Canonical 및 Balance Tool 테스트 PASS. 독립 최종 리뷰가 XML 증거를 직접 확인하여 **PASS·Approve·필수 수정 0**. 이번 수정은 미커밋·미푸시다.

## 원인과 독립 재계산

기존 테스트는 이전 버전 `1-2c24f2a29fd63b81`과 contentHash를 고정하고 있었지만, 실제 canonical manifest와 파일들은 현행 버전으로 일치했다. 구현 담당과 PM이 각각 읽기 전용 PowerShell 검증을 수행했다.

대상:

- `server/src/main/resources/balance/generated`
- `Client/Assets/StreamingAssets/Balance/generated`
- `server/build/resources/main/balance/generated`

각 manifest의 26개 JSON에 대해 원본 바이트 길이와 SHA-256을 대조하고 이름 정렬 순서를 검사했다. manifest 자신을 제외한 파일 목록을 아래 규칙으로 UTF-8 인코딩하여 aggregate를 다시 계산했다.

```text
name + NUL + sha256_lowercase + NUL + size_decimal + LF
contentHash = SHA256(위 문자열들을 파일명 순서대로 연결한 UTF-8 바이트)
balanceVersion = schemaVersion + "-" + contentHash 앞 16자리
```

이 규칙은 Unity `CanonicalBalanceLoader.ComputeContentHash`와 Spring `BalanceManifestSupport.contentHash`에 일치한다. 기존 로더의 각 파일 hash/size 및 aggregate 검증은 수정하지 않았다.

결과:

- 각 디렉터리: JSON 26개 + manifest 1개, 파일 hash/size/aggregate/버전 검증 모두 PASS.
- 서버 source generated ↔ Unity StreamingAssets의 **27개 파일 바이트 동일**.
- `balanceVersion`: `1-e37af4a5ff9b7204`
- `contentHash`: `e37af4a5ff9b72048fc59868a9ed3068781c96829bb2cbf3bafe346aede2ecda`
- manifest 파일 자체 SHA-256: `059e7cbff3d2bd74977d31cd83d08e8a63d258f88ba7d0ecb4cec53958f3c0b2`

## 실행 검증

- Unity 6000.3.4f1 GUI compile 후 전체 EditMode **877/877 PASS**.
- `BattleCanonicalBalanceTests` **26/26 PASS**.
- 증거: [Unity 전체 결과 XML](../../Client/_localbuild/MidnightValidation/battle-hash-fix-full.xml).
- `balanceToolTest --rerun-tasks` **84/84 PASS**.
- Unity MCP는 호출 가능한 도구가 없어 사용하지 않았고 PM이 기존 Editor의 TestRunner를 실행했다. 기존 Editor 저장 변경 상태를 강제로 저장하거나 Scene/Prefab builder를 실행하지 않았다.
- 독립 리뷰: 소스와 전체877/877·Canonical26/26·BalanceTool84/84 XML 직접 확인 **PASS·Approve·필수 수정 0**. 구현 담당은 자기 작업을 독립 승인하지 않았다.
- PM 최종 diff 검토: 테스트 기대값 2줄, runtime/generated/SampleScene diff 없음, `git diff --check` PASS. TestRunner를 닫고 Play=false를 확인했으며 기존 Editor 별표는 저장하지 않고 보존했다.

## 남은 경계

과거 보고서의 Battle hash 1 FAIL은 당시 실행 이력으로 그대로 유지한다. 위 결과는 해당 stale 테스트 실패가 해소됐음을 뜻하며 UI 입력 WARNING·스크롤 미확인·재료 + 승인 제한·실플레이/사람 인수 잔여가 해결됐다는 뜻은 아니다. 게임 밸런스 적절성이나 Production 실플레이 검증을 새로 완료한 것도 아니다.
