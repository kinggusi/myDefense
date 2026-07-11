# AI Review Checklist

## Domain Boundary
- User 코드가 Battle 파일을 수정했는가?
- Battle 코드가 Economy/Merge/Mutation을 수정했는가?
- Shared 변경이 사전 합의되었는가?
- PR 설명에 Domain Impact가 있는가?

## Game Rule
- 4x6인가?
- 같은 종만 Merge 가능한가?
- 다음 등급 전체 풀 랜덤인가?
- Kidnap 99.5/0.5 규칙을 지키는가?
- Pending과 Active Mutation을 구분하는가?

## Damage
- Battle이 피해 공식을 중복 계산하지 않는가?
- DamagePayload를 사용하는가?
- IDamageable에 적용하는가?

## Fusion
- 지속 상태는 `[Networked]`인가?
- 일회성 요청은 RPC인가?
- State Authority가 검증하는가?
- Client가 상태를 직접 확정하지 않는가?

## Unity
- `.unity`/`.prefab` YAML 직접 수정이 없는가?
- `.meta` 누락이 없는가?
- Missing Script/Reference가 없는가?
- GUID를 직접 작성하지 않았는가?
- Update/Find/GetComponent 반복 호출이 과도하지 않은가?

## Spring
- 영구 데이터와 실시간 전투 상태를 혼동하지 않는가?
- Transaction 경계가 적절한가?
- DTO/Entity를 직접 노출하지 않는가?
- 로그와 예외 처리가 있는가?

## Git
- feature 브랜치인가?
- main/dev 직접 수정이 없는가?
- 불필요한 파일이 없는가?
- 한 PR에 여러 주제가 섞이지 않았는가?

## Output
- Summary: PASS / WARNING / FAIL
- Good
- Risks
- Required Changes
- Breaking Change
- Compile Risk: Low / Medium / High
- Merge Recommendation: Approve / Request Changes
