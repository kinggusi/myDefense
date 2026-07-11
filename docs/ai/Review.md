# AI Review Checklist

# Purpose

이 문서는 AI가 Pull Request를 리뷰할 때 반드시 확인해야 하는 체크리스트입니다.

기능 구현보다

프로젝트 규칙을 우선합니다.

---

# 1. Game Rule

다음을 반드시 확인하십시오.

□ Human이라는 이름을 사용하지 않았는가?

□ Alien / Wakjeo 용어를 사용했는가?

□ Kidnap 용어를 사용했는가?

□ Gacha와 Kidnap를 혼동하지 않았는가?

□ Merge 규칙을 변경하지 않았는가?

□ Mutation 규칙을 변경하지 않았는가?

---

# 2. Merge Rule

Merge는

같은 종끼리만 가능합니다.

확인

□ 다른 종 Merge가 가능한 코드가 없는가?

□ Merge 결과가 다음 등급 Pool에서 랜덤인가?

□ evolutionTargetId 같은 고정 진화가 남아있지 않은가?

□ Merge 결과가 Target 위치에 생성되는가?

---

# 3. Mutation Rule

확인

□ 생체주입제가 Mutation 예약 역할만 하는가?

□ 신화에서만 Mutation이 발동하는가?

□ Mutation 재변이가 가능한가?

□ 재변이 비용이 증가하는가?

□ Prefix 계승 규칙이 맞는가?

NONE + NONE → NONE

A + NONE → A

NONE + A → A

A + A → A

A + B → A 또는 B 랜덤

---

# 4. Economy

확인

□ Kidnap 비용 증가가 적용되는가?

□ 라운드가 바뀌어도 초기화되지 않는가?

□ 개인 인게임 골드인가?

□ Lobby 재화와 혼동되지 않았는가?

---

# 5. Grid

확인

□ Grid가 4 x 6 인가?

□ 24칸인가?

□ Alien과 Mutation Injector가 모두 1칸 사용하는가?

□ 빈칸 없으면 Kidnap 불가인가?

□ 자동 배치가 왼쪽부터 첫 빈칸인가?

□ Drag 이동이 가능한가?

---

# 6. Player State

확인

□ ACTIVE

□ ELIMINATED

□ SPECTATING

분리되어 있는가?

MatchState

□ RUNNING

□ FAILED

□ CLEARED

분리되어 있는가?

---

# 7. Battle

확인

□ Battle 코드가 Damage를 계산하지 않는가?

□ Physics는 충돌만 담당하는가?

□ DamagePayload를 사용하는가?

□ StatCalculator를 호출하는가?

---

# 8. Spring Boot

확인

□ 전투 실시간 로직이 Spring으로 이동하지 않았는가?

□ 영구 데이터만 저장하는가?

□ Transaction Log가 유지되는가?

---

# 9. Fusion

확인

□ 지속 상태는 Networked인가?

□ 이벤트는 RPC인가?

□ State Authority가 검증하는가?

□ Client가 직접 상태를 변경하지 않는가?

---

# 10. Scene

확인

□ .unity YAML 직접 수정이 없는가?

□ Prefab Reference가 깨지지 않았는가?

□ Missing Script가 없는가?

□ Missing Reference가 없는가?

---

# 11. Unity

확인

□ NullReference 가능성이 없는가?

□ Find()를 매 프레임 호출하지 않는가?

□ GetComponent를 반복 호출하지 않는가?

□ Inspector SerializeField가 적절한가?

□ Singleton이 남용되지 않았는가?

---

# 12. Performance

확인

□ Update 남용이 없는가?

□ GC Allocation이 큰 코드가 없는가?

□ LINQ를 매 프레임 사용하지 않는가?

□ foreach Boxing 문제가 없는가?

---

# 13. Naming

확인

□ Human 사용 금지

□ Alien 사용

□ Mutation 사용

□ Kidnap 사용

□ Merge 사용

---

# 14. Folder

확인

User Domain

↓

Economy

Lobby

Merge

Mutation

Alien

Battle Domain

↓

Monster

Boss

Projectile

Physics

Effect

Shared

↓

DTO

Interface

Contract

---

# 15. Git

확인

□ dev 직접 수정하지 않았는가?

□ main 직접 수정하지 않았는가?

□ feature 브랜치인가?

□ Commit Message가 명확한가?

□ 불필요한 파일이 포함되지 않았는가?

□ .meta 누락이 없는가?

---

# 16. AI Review Result

최종 결과는 아래 형식으로 출력하십시오.

## Summary

PASS / WARNING / FAIL

## Good

잘된 점

## Risk

위험 요소

## Suggestion

개선 사항

## Modified Files

수정 파일

## Breaking Change

있음 / 없음

## Compile Risk

낮음

중간

높음

## Merge Recommendation

Approve

Request Changes