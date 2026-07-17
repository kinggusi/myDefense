# 왹져 디펜스 마스터 정책서

> 문서 버전: 1.0  
> 기준일: 2026-07-18  
> 대상: 기획, User/System, Battle, Shared, Unity, Spring Boot

## 0. 문서 목적과 상태 표기

이 문서는 전투, 계정 성장, 콘텐츠, 경제, 보상, 네트워크 및 도메인 경계를 한 곳에서 확인하기 위한 제품 정책 기준서다.

제품 정책이 다른 기획 문서와 충돌하면 이 문서를 우선 확인한다. 단, 저장소 안전 규칙과 작업 절차는 루트 `AGENTS.md`를 최우선으로 따른다.

정책 상태는 다음과 같이 구분한다.

- **확정**: 구현 기준으로 사용한다.
- **현행**: 현재 코드나 밸런스 데이터에 존재하지만 최종 밸런스가 아닐 수 있다.
- **권장안**: PM 1차 제안이며 승인 후 확정한다.
- **미정**: 구현 전에 추가 결정이 필요하다.

---

## 1. 게임 개요

### 1.1 기본 정보 — 확정

- 장르: 2인 협동 Merge Defense
- 클라이언트: Unity 6
- 실시간 네트워크: Photon Fusion
- 영구 데이터 서버: Spring Boot
- 일반 협동 매치: 총 80 Wave
- 핵심 재미: Kidnap, 동일 종 Merge, Mythic 선택, Mutation 계승, 2인 협동 생존

### 1.2 핵심 플레이 흐름 — 확정

```text
로비
→ 영구 Alien 획득 및 강화
→ 하트 소비 후 2인 매치 입장
→ Kidnap으로 Alien/Mutation Injector 획득
→ 동일 종·동일 등급 Merge
→ Legendary Merge에서 Mythic 후보 선택
→ Mutation 활성화 및 인게임 강화
→ 80 Wave 클리어 또는 팀 전멸
→ Settlement
→ 계정 보상 지급
```

---

## 2. 공식 용어

| 개념 | 공식 명칭 | 금지·주의 표현 |
|---|---|---|
| 전투 중 소환 | Kidnap / 납치 | Gacha와 혼용 금지 |
| 로비 영구 획득 | Gacha / 뽑기 | Kidnap과 혼용 금지 |
| 동일 개체 합성 | Merge | 진화 계보로 표현 금지 |
| 생체변이 | Mutation | Prefix 단일 필드와 혼용 금지 |
| 생체변이 아이템 | Mutation Injector | 별도 최종 Alien이 아님 |
| 유닛 | Alien / Unit / 왹져 | Human 사용 금지 |
| 전투 임시 골드 | inGameGold | accountGold와 혼용 금지 |
| 계정 영구 골드 | accountGold | inGameGold와 혼용 금지 |
| 매치 정산 | Settlement | 단순 클라이언트 결과 화면과 구분 |

---

## 3. Alien 구성과 획득

### 3.1 전체 구성 — 확정

총 48종이다.

| 등급 | 수량 |
|---|---:|
| Normal | 7 |
| Epic | 7 |
| Unique | 7 |
| Legendary | 7 |
| Mythic | 20 |
| 합계 | 48 |

Normal~Legendary의 7종별 역할 배분은 별도 밸런스 확정이 필요하다.

### 3.2 Mythic 영구 획득 풀 — 확정

- 상점 Gacha에서는 일반 Mythic 18종이 등장한다.
- 로비 Breeding에서는 Mythic 20종이 등장한다.
- Mythic 20종 중 2종은 Breeding 전용이다.
- Breeding 전용 2종은 Gacha에서 등장하지 않는다.
- Breeding 전용 2종은 낮은 확률과 전용 연출을 사용한다.
- 미해금 Mythic 조각을 획득하면 해금 정책에 따라 해금한다.
- 해금 후 남은 초과 조각은 해당 Mythic 조각으로 보관한다.

### 3.3 고정 진화 계보 금지 — 확정

- `evolutionTargetId`가 Merge 결과를 결정해서는 안 된다.
- Normal~Legendary Merge 결과는 다음 등급 전체 풀에서 결정한다.
- `evolutionTargetId`가 데이터에 남아 있더라도 게임 규칙에는 사용하지 않는다.

---

## 4. 전투 필드와 슬롯

### 4.1 개인 필드 — 확정

- 플레이어마다 `4 x 6`, 총 24칸의 개인 보드를 가진다.
- Alien과 Mutation Injector는 각각 한 칸을 차지한다.
- 두 개인 필드 사이에는 Boss용 공용 일자 Lane이 존재한다.
- 일반 Monster는 각 플레이어 개인 영역을 이동한다.
- 각 플레이어 개인 영역이라기보다는 1차 공용보스 라인에 일반 몬스터가 이동할 수 있다. 
- Boss는 공용 Lane을 이동한다.

### 4.2 배치와 이동 — 확정

- 빈칸이 없으면 Kidnap할 수 없다.
- Kidnap 결과는 공통 오름차순 Grid 순서의 첫 빈칸에 배치한다.
- Alien과 Mutation Injector는 빈칸으로 이동할 수 있다.
- B를 A로 드래그해 Merge하면 결과는 A 위치에 생성한다.
- Mutation Injector는 Alien 위로 드래그해 사용한다.

### 4.3 개인 필드 탈락 — 확정

- 개인 필드의 살아 있는 Monster가 100마리에 도달하는 즉시 해당 플레이어가 탈락한다.
- 경고 기준은 80마리, 위험 기준은 90마리다.
- 탈락 플레이어는 조작을 중단하고 관전한다.
- 해당 필드에는 신규 Monster를 Spawn하지 않는다.
- 이미 존재하는 Monster는 제거하지 않는다.
- 남은 플레이어가 기존 Monster까지 처리할 수 있다.

### 4.4 전투 상태 — 확정

`PlayerBattleState`:

- `ACTIVE`
- `ELIMINATED`
- `SPECTATING`

`MatchState`:

- `RUNNING`
- `CLEARED`
- `FAILED`

연결 상태는 전투 상태와 별도로 관리해야 한다. 일시적인 연결 종료를 `ELIMINATED`나 최종 이탈로 처리하지 않는다.

---

## 5. Kidnap

### 5.1 결과 확률 — 확정

- Normal Alien: 99.5%
- Mutation Injector: 0.5%
- Injector 종류는 동일 확률
- Mutation 꽝형은 Injector로 등장하지 않는다.

### 5.2 비용 — 현행 1차값

```text
Kidnap 비용 = 50 + 누적 성공 횟수 × 10
```

- 시작 비용은 50 inGameGold다.
- Wave가 바뀌어도 누적 횟수와 비용은 초기화하지 않는다.
- 일반 협동 모드의 사용 횟수 제한은 없다.
- 비용은 `BalanceConfig` 또는 공통 Balance JSON에서 관리한다.
- 무한 모드의 장기 비용 공식은 일반 협동 모드와 분리할 수 있다.

---

## 6. Merge와 Legendary Mythic 선택

### 6.1 기본 Merge — 확정

- Normal~Legendary 구간에서만 Merge한다.
- 동일 Alien 종이면서 동일 등급인 두 Alien만 Merge할 수 있다.
- 서로 다른 종 또는 서로 다른 등급은 Merge할 수 없다.
- Normal~Unique 결과는 다음 등급 전체 풀에서 랜덤 선택한다.
- Pending Mutation DNA는 Merge 결과로 계승한다.

### 6.2 DNA 계승 — 확정

| 재료 A | 재료 B | 결과 DNA |
|---|---|---|
| NONE | NONE | NONE |
| A | NONE | A |
| NONE | A | A |
| A | A | A |
| A | B | A 또는 B, 각각 50% |

서로 다른 DNA를 보유하더라도 동일 종·동일 등급 조건을 만족하면 Merge할 수 있다.

### 6.3 Legendary Merge — 확정

- Legendary + Legendary Merge에서는 현재 플레이어가 영구 해금한 Mythic 풀을 사용한다.
- 서로 다른 Mythic 후보 3종을 제시한다.
- 플레이어는 후보 중 한 종을 선택한다.
- 최대 3회 후보 리롤이 가능하다.
- 한 후보 화면 안에서 같은 Mythic을 중복 제시하지 않는다.
- 최종 선택한 Mythic은 Merge 대상 위치에 생성한다.
- 계승 DNA가 있으면 선택된 Mythic에 해당 Mutation이 무료로 즉시 활성화된다.
- Merge 자체에는 별도의 골드 비용이 없다.

### 6.4 Legendary 선택 추가 정책 — 미정

- 첫 후보 3종
  → 무료 리롤
  → 두 번째 후보 3종
  → 전투 골드 100 소모 리롤
  → 세 번째 후보 3종
  → 이후 리롤 불가
- 이전 리롤에서 본 Mythic을 전체 후보 풀이 소진되기 전에 다시 보여줄지
- 선택 제한시간과 시간 초과 시 자동 선택 규칙
- 선택 중 재접속했을 때 재료 잠금 및 후보 복원 방식
- Breeding 전용 Mythic을 해금한 뒤 전투 후보에 포함할지
---

## 7. Mutation

### 7.1 Mutation Injector — 확정

- Kidnap에서 낮은 확률로 필드에 등장한다.
- Alien처럼 한 슬롯을 차지한다.
- Normal~Mythic Alien에게 사용할 수 있다.
- Normal~Legendary에 사용하면 `PendingMutationType` DNA를 보유한다.
- Pending DNA는 스탯, 외형, 공격 메커니즘을 변경하지 않는다.
- Pending DNA는 Merge를 통해 계승된다.

### 7.2 DNA 보유 Mythic — 확정

- Legendary Merge로 Mythic이 생성되는 즉시 계승 DNA를 `ActiveMutationType`으로 전환한다.
- 활성화는 무료다.
- 추가 버튼이나 골드 비용이 없다.

### 7.3 DNA 없는 순수 Mythic — 확정

- Mythic 생성 후 Mutation 버튼을 활성화한다.
- 버튼을 누르면 300 inGameGold를 소비한다.
- 꽝형을 포함한 Mutation 풀에서 랜덤으로 하나를 활성화한다.

### 7.4 재변이 — 확정

- 이미 Mutation된 Mythic도 재변이할 수 있다.
- 비용은 `600 → 1,200 → 2,400 → 4,800` 순서로 증가한다.
- 이후 재변이 비용은 4,800으로 고정한다.
- 현재 적용 중인 Mutation은 후보에서 제외한다.
- 재변이하면 반드시 다른 Mutation을 획득한다.
- Injector로 확정한 Mutation도 재변이할 수 있다.

### 7.5 Mythic에 Injector 사용 — 확정

- 이미 Mutation된 Mythic에도 Injector를 사용할 수 있다.
- 기존 Mutation을 Injector의 Mutation으로 즉시 교체한다.
- 교체 골드 비용은 없다.

### 7.6 Mutation 8종 — 확정

1. 광역형
2. 단일 Boss형
3. 공격속도형
4. 지속 피해형
5. 경제형
6. 도박형
7. 상태 이상형
8. 꽝형

각 Mutation은 다음 요소를 가진다.

- 강한 장점 1개
- 보조 장점 1개
- 명확한 단점 1개
- 외형 변화 1개

### 7.7 꽝형 조합 보상 — 확정

- Injector에서는 등장하지 않는다.
- DNA 없는 Mythic의 최초 랜덤 Mutation과 재변이에서는 등장할 수 있다.
- 꽝형 2개 수집 시 merge가 가능하다.(+레전더리 + 레전더리 merge로 본다. 꽝형끼리만 merge 가능)
- 꽝형 2개 merge시 기존 레전더리 merge와 동일하게 후보 3종 선택

---

## 8. 전투 피해와 Stat 경계

### 8.1 책임 — 확정

```text
Alien + Skill + 영구 성장 + Active Mutation
→ User/System StatCalculator
→ AlienAttackSnapshot / DamagePayload
→ Battle Target Search / Projectile / Hit
→ IDamageable.ApplyDamage(payload)
```

- User/System은 피해량과 스탯을 계산한다.
- Battle은 대상을 선택하고 충돌·Hit을 판정한다.
- Battle은 피해 공식을 중복 구현하지 않는다.
- 시각 이펙트 크기와 실제 판정 범위를 분리한다.

### 8.2 Shared 계약 — 목표

- `DamagePayload`
- `IDamageable`
- `ITargetProvider`
- `HitEvent`
- `AlienAttackSnapshot`

---

## 9. Wave, Monster, Boss

### 9.1 기본 규칙 — 확정

- 일반 협동 매치는 80 Wave다.
- Boss는 10 Wave마다 등장한다.
- Boss 제한시간 초과 시 전체 매치는 즉시 `FAILED`다.
- Wave, Spawn, Monster 수치와 Boss 제한시간은 Balance 데이터로 관리한다.

### 9.2 1차 Wave 스폰 권장안 — 승인 전

개인 필드당 기준이다.

| Wave | 일반 스폰 수 | Elite 비율 |
|---|---:|---:|
| 1~9 | 12~16 | 0% |
| 11~19 | 18~22 | 5% |
| 21~29 | 24~28 | 10% |
| 31~39 | 30~34 | 15% |
| 41~49 | 36~40 | 20% |
| 51~59 | 42~46 | 25% |
| 61~69 | 48~52 | 30% |
| 71~79 | 54~60 | 35% |

- 목표 매치 시간 권장안은 VIP권 유무에 따라 상이하다. 
- 최종 Spawn 수는 실제 전투 시뮬레이션과 2클라이언트 테스트 후 확정한다.
- 배율 모드 설정 가능(VIP 또는 프리미엄 결제 시 최대 3배까지 빨라질 수 있다.)
- 각 플레이 타임 : 일반 = 20분 ~ 25분, VIP권 = 10~15분

### 9.3 난이도 목표 — 권장안

- 권장 스펙에서 다음 Wave 시작 전 현재 Monster의 85~95% 처리
- 일반 Monster 평균 처치시간: 2~4초
- Elite 평균 처치시간: 8~12초
- Boss는 권장 스펙에서 제한시간의 70~85% 사용
- 화력 부족이 누적되면 80/90 경고 구간을 지나 100마리 탈락으로 이어짐

---

## 10. 협동 인게임 골드

### 10.1 지급 원칙 — 확정

- Monster가 어느 개인 필드 또는 공용 Lane에서 처치되든 양쪽 플레이어에게 동일한 처치 골드 100%를 각각 지급한다.
- 마지막 공격자가 누구인지는 골드와 무관하다.
- Kill과 Support Kill은 통계와 Settlement 기록에만 사용한다.
- 탈락 후 관전 중인 플레이어에게도 골드를 계속 지급한다.
- 일시적인 연결 종료 중에도 세션과 골드 장부를 유지하고 골드를 계속 지급한다.
- 매치 종료 전에 재접속하면 누적 골드와 전투 상태를 복구한다.

### 10.2 처치 골드 — 1차 권장안

| 대상 | 플레이어별 지급 골드 |
|---|---:|
| Normal Monster | 8 |
| Elite Monster | 20 |
| Wave 10 Boss | 200 |
| Wave 20 Boss | 300 |
| Wave 30 Boss | 450 |
| Wave 40 Boss | 650 |
| Wave 50 Boss | 900 |
| Wave 60 Boss | 1,200 |
| Wave 70 Boss | 1,600 |
| Wave 80 Boss | 2,500 |

이 수치는 승인과 플레이 테스트 전이며 Balance 데이터에서 조정한다.

### 10.3 인게임 골드 소비 목표 — 권장안

| 소비처 | 목표 비중 |
|---|---:|
| Kidnap | 50~60% |
| 인게임 강화 | 20~25% |
| Mutation 활성화·재변이 | 10~20% |
| 비상 보유 | 5~10% |

---

## 11. 인게임 강화

인게임 강화는 매치 종료 시 초기화되는 성장이다.

### 11.1 도입 방향 — 권장안

개별 Alien 강화 대신 플레이어 전체에 적용하는 두 계통을 사용한다.

- 일반 공명: Normal~Legendary 전체 강화
- 신화 공명: Mythic 전체 강화

| 계통 | 단계별 누적 공격력 | 단계별 비용 권장안 |
|---|---|---|
| 일반 공명 | +5%, +10%, +15%, +20%, +25% | 400, 800, 1,400, 2,200, 3,200 |
| 신화 공명 | +8%, +16%, +24%, +32%, +40% | 800, 1,600, 2,800, 4,400, 6,500 |

- 공격속도는 단계당 약 1%만 추가한다.
- 사거리는 인게임 공통 강화로 올리지 않는다.
- 실제 비용은 Kidnap 및 Mutation 소비율과 함께 시뮬레이션 후 확정한다.

---

## 12. 계정 영구 경제와 재화

### 12.1 재화 체계 — 기획 기준

| 재화 | 용도 |
|---|---|
| accountGold | Alien 강화, Mutation 성장, 계정 연구 |
| gem | Breeding 시간 단축, 스킨, 편의, 하트 충전 |
| heart | 일반 게임 및 일부 콘텐츠 입장 |
| Alien 조각 | 해당 Alien 해금 및 강화 |
| 범용 조각 | 부족한 Alien 조각 대체 |
| 성장 세포 | Alien 고레벨 강화·돌파 |
| 변이 촉매 | Mutation 고레벨 강화·돌파 |

### 12.2 명칭 원칙 — 권장

- 코드 `substituteCoin`의 표시명은 `범용 조각`을 권장한다.
- 코드 `growthCell`의 표시명은 `성장 세포`를 사용한다.
- `DNA Coin`은 Pending Mutation DNA와 혼동되므로 사용하지 않는 것을 권장한다.
- 서버·DTO·Unity에서 `inGameGold`와 `accountGold`를 명시적으로 구분한다.

### 12.3 영구 Alien 강화 — 현행 방향

- 특정 Alien 조각과 accountGold를 사용한다.
- 부족한 전용 조각은 범용 조각으로 일부 대체할 수 있다.
- 고레벨 구간은 성장 세포를 추가로 사용한다.
- 영구 Alien 레벨과 전투 중 Merge 등급은 별개다.

### 12.4 영구 강화 밸런스 — 권장안

- 최대 레벨: 50
- 공격력: 레벨당 약 4.5%
- 공격속도: 레벨당 약 0.5%, 총 +25% 제한
- 사거리: 영구 공통 성장 제외
- 10/20/30/40레벨에서 돌파 보정

```text
공격력 배율
= 1 + 0.045 × (레벨 - 1) + 0.08 × 돌파 횟수
```

| 레벨 | 강화 1회 골드 권장 범위 | 목표 소요 시간 |
|---|---:|---|
| 1~10 | 500~3,000 | 전투 1~3회 |
| 11~20 | 4,000~12,000 | 약 1일 |
| 21~30 | 15,000~35,000 | 1~2일 |
| 31~40 | 40,000~80,000 | 2~4일 |
| 41~50 | 90,000~180,000 | 3~7일 |

최종 수치는 최고 해금 맵의 일일 기대 수입을 먼저 정한 뒤 역산한다.

---

## 13. 로비 Breeding

### 13.1 현재 서버 정책 — 현행

- Mythic 부모 2종을 선택한다.
- 부모 Alien은 소모하지 않는다.
- 부모 Alien은 전투와 다른 비-Breeding 콘텐츠에서 계속 사용할 수 있다.
- 현재 서버는 같은 부모 Alien을 둘 이상의 Breeding 슬롯에 동시에 등록하지 못하게 한다.
- Breeding 슬롯만 점유한다.
- 결과 수령 전에는 결과 Mythic을 숨긴다.
- 중복 결과는 해당 Mythic 조각 50개를 지급한다.
- 처리 시간은 현재 Balance 기준 24시간이다.
- 슬롯은 총 3개다.
- 1번 슬롯은 기본 제공한다.
- 2번 슬롯은 계정 레벨 15 또는 gem 500으로 해금한다.
- 3번 슬롯은 gem 1,000으로 해금한다.
- 시작·수령 요청은 멱등 처리해야 한다.
- 동일 Mythic 두 마리 조합 불가

### 13.2 추가 정책 — 미정

- 전용 Mythic 2종의 확률 : 각각 0.7%
- Breeding 포인트 천장 도입 여부 : X
- 남은 시간별 gem 단축 비용 : n분 * 10
- 동시에 다른 슬롯에서 같은 부모를 재사용할 수 있는지 : X

---

## 14. 행성 스테이지·맵

### 14.1 콘텐츠 방향 — 기획 중

진행 순서는 다음과 같다.

```text
해왕성 → 천왕성 → 토성 → 목성 → 화성 → 지구 → 금성 → 수성 → 태양
```

- 태양은 종결 콘텐츠 맵이다.
- 단순 HP 상승뿐 아니라 행성별 연출에 차별점을 둔다.
- 이전 행성 클리어를 다음 행성 해금 조건으로 사용한다.

### 14.2 행성별 1차 권장값 — 승인 전

| 맵 | 권장 전투력 지수 | 클리어 accountGold |
|---|---:|---:|
| 해왕성 | 100 | 1,000 |
| 천왕성 | 150 | 1,400 |
| 토성 | 225 | 2,000 |
| 목성 | 340 | 2,800 |
| 화성 | 510 | 4,000 |
| 지구 | 770 | 5,600 |
| 금성 | 1,150 | 7,800 |
| 수성 | 1,730 | 11,000 |
| 태양 | 2,600 | 16,000 |

Monster HP는 고정 감각값이 아니라 권장 팀 DPS와 목표 처치시간으로 계산한다.

```text
일반 HP = 권장 팀 DPS × 목표 생존시간 ÷ 동시 공격 대상 수
Boss HP = 권장 팀 DPS × 목표 전투시간 × 기믹 보정
```

---

## 15. 일일 콘텐츠

### 15.1 콘텐츠 구성 — 기획안

| 콘텐츠 | 주요 보상 | 전투 특징 |
|---|---|---|
| 배양 구역 | 성장 세포 | 다수의 약한 적 |
| 변이 연구소 | 변이 촉매 | 상태 이상·Boss |

- 각 콘텐츠는 5개 Stage로 구성한다.
- 각 콘텐츠는 하루 3회 무료 입장을 권장한다.
- 기본 보상은 확정 지급하고 소량의 추가 랜덤 보상을 허용한다.
- 최초 클리어 보상을 별도 지급한다.
- 클리어한 Stage만 소탕할 수 있다.
- 입장 시 횟수를 차감하고 서버·매칭 장애 시 반환한다.

### 15.2 1차 재료 보상 권장안

| Stage | 성장 세포 | 변이 촉매 |
|---:|---:|---:|
| 1 | 5 | 3 |
| 2 | 8 | 5 |
| 3 | 12 | 8 |
| 4 | 17 | 12 |
| 5 | 24 | 17 |

정확한 요구량과 공급량은 고레벨 강화 소요 일수를 기준으로 함께 조정한다.

---

## 16. 무한 Wave 랭킹 콘텐츠

### 16.1 목적 — 기획 방향

- 일반 80 Wave와 분리된 순위 경쟁 모드다.
- 개인과 2인 협동 랭킹을 분리한다.
- 시즌 단위로 초기화한다.
- 구간별 도달 보상을 지급한다.

### 16.2 집계 우선순위 — 권장안

1. 최고 도달 Wave
2. Boss 처치 수
3. 해당 Wave 도달 시간

### 16.3 운영 원칙 — 권장안

- 동일 Balance Version끼리만 집계한다.
- 하루 무료 입장권 1장을 권장한다.
- 상위 몇 명만 전투력 보상을 독점하지 않도록 구간 보상을 중심으로 한다.
- Settlement에 비정상 골드, 킬, Wave 진행 검증을 포함한다.
- 무한 모드 전용 Kidnap 비용과 Monster 성장 공식을 사용한다.
- wave당 1분의 제한시간을 가지고 제한시간이 지나기 전에 소탕 시 바로 다음 wave 진행
- 개인전이다.

---

## 17. 길드 콘텐츠

### 17.1 단계별 권장 범위

1. 길드 생성·가입·출석·기부
2. 주간 누적 피해형 길드 Boss
3. 길드 상점
4. 길드 시즌 랭킹
5. 길드 대항전

초기 길드 Boss는 다수 동시접속 실시간 전투보다 구성원이 개별 입장해 피해를 누적하는 방식을 권장한다.

### 17.2 원칙

- 길드 미가입자가 핵심 성장에서 완전히 배제되지 않게 한다.
- 길드 전용 필수 전투력 재화는 피한다.
- 길드 보상은 성장 보조, 꾸미기, 칭호 중심으로 설계한다.

---

## 18. 퀘스트와 무료 재화 공급

### 18.1 공급 비중 — 권장안

| 공급처 | 전체 무료 재화 비중 |
|---|---:|
| 일반 전투 | 55% |
| 일일 콘텐츠 | 20% |
| 일일·주간 퀘스트 | 20% |
| 이벤트·기타 | 5% |

### 18.2 퀘스트 보상 원칙 — 권장안

- 일일 퀘스트 전체 보상: 최고 해금 맵 1회 클리어 골드의 60~80%
- 주간 퀘스트 전체 보상: 최고 해금 맵 4~5회 클리어 가치
- gem은 일일 반복보다 주간·업적·이벤트 중심으로 지급한다.
- 퀘스트가 직접 전투보다 주요 수급처가 되지 않게 한다.

---

## 19. 상점과 판매 가능 상품

### 19.1 판매 후보 — 권장안

- Mythic Gacha
- Alien 조각 로테이션
- 스킨
- 보드 테마
- 프로필·칭호·이모티콘
- Breeding 시간 단축
- 하트 충전
- 성장 세포·변이 촉매 주간 제한 상품
- 시즌 패스
- 닉네임 변경권

### 19.2 원칙

- 장비 시스템은 현재 범위에서 제외한다.
- gem으로 전투력을 무제한 구매하는 구조를 피한다.
- 성장 재료 상품은 일일·주간 구매 제한을 둔다.
- 꾸미기와 편의 상품 비중을 높인다.

---

## 20. 재접속·이탈·관전

### 20.1 연결 종료 — 확정

- 일시적인 연결 종료만으로 `PLAYER_ABANDONED` 처리하지 않는다.
- 매치가 진행되는 동안 플레이어 세션, 보드, inGameGold 장부를 유지한다.
- 연결 종료 중에도 Monster 처치 골드를 계속 지급한다.
- 매치 종료 전에 재접속하면 모든 전투 상태를 복구한다.

### 20.2 최종 이탈 판정 — 확정

- 플레이어가 명시적으로 나가기를 선택한 경우
- 매치 종료 시점까지 복귀하지 않은 경우

위 조건에서만 최종 이탈로 판정한다.

### 20.3 재접속 세부값 — 미정

- UI에서 재접속 유예 시간을 표시할지 : X
- 오랜 연결 종료 후에도 매치 종료 전 복귀를 허용할지 : X
- 명시적 나가기 후 재입장을 완전히 금지할지 : 금지

기술적 연결 유지·재시도 타임아웃의 1차 권장값은 120초지만, 보상 자격 판정은 매치 종료 시점에 수행한다.

---

## 21. 매치 결과와 Settlement

### 21.1 Settlement 정의 — 확정

Settlement는 종료된 매치의 결과와 참가자 상태를 서버에서 확정하고, 영구 보상 계산 및 중복 지급 방지 기록으로 넘기는 정산 단계다.

Settlement는 최소 다음을 기록한다.

- requestId
- battleSessionId
- balanceVersion과 contentHash
- 최종 결과
- 최종 Wave
- 시작·종료 시간
- 참가자와 슬롯
- 탈락 여부와 탈락 Wave
- Kill, Support Kill, Boss Kill
- 초기·획득·소비·최종 inGameGold
- Monster 종류별 처치 수와 골드
- 요약 무결성 값

### 21.2 결과 분류 — 권장 정리

- `CLEARED`: 80 Wave 정상 클리어
- `FAILED`: 정상 전멸 또는 Boss 시간 초과
- `PLAYER_ABANDONED`: 명시적 이탈 또는 매치 종료까지 미복귀
- `SERVER_ABORTED`: 서버·세션 장애로 정상 정산 불가

현재 서버 Enum의 `VICTORY`, `DEFEAT`, `ABORTED`와 최종 명칭을 맞추는 작업이 필요하다.

### 21.3 협동 보상 자격 — 확정

- 한 플레이어가 탈락해도 관전을 유지하고 팀이 80 Wave를 클리어하면 두 플레이어에게 동일한 클리어 보상을 지급한다.
- 탈락한 플레이어가 명시적으로 나가면 해당 플레이어는 영구 보상을 받지 않는다.
- 두 플레이어가 매치 종료까지 남아 있다가 실패하면 최종 도달 Wave 기준 보상을 동일하게 받는다.
- 연결이 종료됐더라도 매치 종료 전에 복귀하면 정상 참가자로 처리한다.
- 매치 종료 시점까지 미복귀한 플레이어의 경우 연결불가의 상태를 120초 이상 유지하였을때만 영구 보상 지급 여부는 현재 정책상 미지급이다.
- `SERVER_ABORTED`는 하트를 반환하고 일반 클리어 보상은 지급하지 않는 방향을 권장한다.

### 21.4 실패 보상률 — 권장안

```text
도달 비율 = finalWave / 80
실패 보상률 = min(80%, 도달 비율 ^ 1.5)
```

- 10 Wave 미만은 보상하지 않는 것을 권장한다.
- 정상 클리어는 100%다.
- 정확한 최소 Wave와 곡선은 승인 전이다.

### 21.5 현재 임시 서버 보상 — 현행

현재 `game-reward.json`은 다음 임시값을 가진다.

- 기본 accountGold: 100
- Wave당 accountGold: 10
- 최대 accountGold: 1,000

행성별 보상과 실패 보상 정책이 확정되면 이 임시값을 대체해야 한다.

---

## 22. Photon Fusion 권한 정책

### 22.1 기본 원칙 — 확정

- 지속 전투 상태는 Fusion `[Networked]` 속성으로 관리한다.
- 일회성 요청은 RPC로 보낸다.
- State Authority가 최종 검증한다.
- 클라이언트는 결과를 직접 확정하지 않는다.

### 22.2 State Authority 검증 대상

- Kidnap 가능 여부와 비용
- 첫 빈칸 배치
- Merge 재료·종·등급
- Legendary 후보 생성·리롤·선택
- Mutation 활성화·재변이와 골드
- 인게임 강화
- 플레이어별 inGameGold
- Wave와 Boss Timer
- PlayerBattleState와 MatchState
- Kill·Support Kill·Boss Kill 장부
- Settlement 전송

### 22.3 Spring Boot 책임

- Authentication
- User와 영구 Wallet
- Alien 해금·조각·강화
- Gacha와 Shop
- Breeding
- Balance Version
- Battle Result와 Settlement
- Transaction Log

---

## 23. 데이터 파이프라인

### 23.1 목표 구조 — 확정

```text
Excel/CSV
→ Validation
→ Common JSON
├→ Unity Importer → ScriptableObject
└→ Spring Loader → DB/Cache
```

핵심 데이터:

- Alien
- Skill
- Mutation
- Mutation Injector
- Monster
- Wave
- Economy
- Field Limit
- Merge Rule
- Mythic Choice
- Gacha
- Breeding
- Reward
- Balance Manifest

### 23.2 운영 원칙

- Unity와 Spring이 같은 Balance Version과 contentHash를 사용한다.
- 매치 시작 시 Balance Version을 고정한다.
- Settlement에서 매치 시작 버전과 서버 버전을 검증한다.
- 하드코딩된 Wave·비용·보상 수치는 단계적으로 Balance 데이터로 이동한다.

---

## 24. 도메인 소유권

### 24.1 User/System

- Lobby, Shop, Collection
- Economy, Alien, Skill
- Kidnap, Merge, Mutation, Mutation Injector
- StatCalculator와 데이터 파이프라인
- Spring Boot 전체
- Fusion 경제·시스템 로직
- Settlement 서버 처리와 영구 보상

### 24.2 Battle

- Battle Map, Scene, Prefab
- Monster, Boss, Wave
- Projectile, Physics, Collision
- Target Search
- Animation, Effect
- Shared Lane, Waypoint
- NetworkTransform
- Hit 적용
- Fusion 전투 실행과 Settlement 전투 장부 생성

### 24.3 Shared

- DTO, Enum, Interface
- DamagePayload, IDamageable, ITargetProvider, HitEvent
- PlayerBattleState, MatchState
- GridPosition
- Network Contract
- Settlement 요청 계약

Shared 계약 변경 시 양쪽 담당자에게 알리고 양쪽 컴파일을 확인한다.

---

## 25. 현재 구현 상태 요약

| 영역 | 상태 | 비고 |
|---|---|---|
| 4x6 보드·24칸 | 구현 | 서버와 Unity 기반 존재 |
| Kidnap 99.5/0.5 | 구현 | 비용 선형 증가 구현 |
| 동일 종·동일 등급 Merge | 구현 | 다음 등급 랜덤 구현 |
| DNA 계승 | 구현 | A/B 50% 포함 |
| Legendary 후보 3종·3회 리롤 | 미구현 | 현재는 즉시 단일 랜덤 Mythic 생성 |
| Pending/Active Mutation 모델 | 부분 구현 | 자동 활성화·랜덤 활성화·재변이 필요 |
| Gacha 18종 | 서버·Unity 기반 | 멱등 구매와 UI 존재 |
| Breeding 20종 | 서버 기반 | Unity UI 연결 필요 |
| Alien 영구 강화 | 서버·Unity 기반 | 최종 비용·성장 곡선 재조정 필요 |
| DamagePayload/IDamageable | 부분 구현 | ITargetProvider/HitEvent 미구현 |
| Wave/Boss/Lane | 로컬 기반 | Balance 연결과 Fusion 전환 필요 |
| Photon Fusion 게임 상태 | 미구현에 가까움 | SDK는 있으나 자체 Networked/RPC 부족 |
| Player 탈락·관전 | 미구현 | 상태·UI·스폰 중단 필요 |
| Settlement 서버 기반 | 구현 중 | 저장 계약 존재, Battle 전송·보상 연결 필요 |
| 행성 스테이지 | 기획 | 맵·기믹·보상 확정 필요 |
| 일일 콘텐츠 | 기획 | Stage·보상·입장 저장 필요 |
| 무한 랭킹 | 기획 | 시즌·검증·보상 필요 |
| 길드 | 기획 | MVP 이후 권장 |

---

## 26. 구현 우선순위

### P0 — 2인 매치 성립

1. Fusion 2인 Battle Session
2. PlayerBattleState와 MatchState
3. Networked Wave, Monster, Boss, TickTimer
4. State Authority 기반 Kidnap·Merge·Gold
5. Legendary 후보 선택·리롤 Network Contract
6. 재접속과 세션 복구
7. Battle Settlement 장부와 서버 전송

### P1 — 핵심 성장과 전투 완성

1. Mutation 활성화·재변이
2. Mutation StatCalculator
3. Damage Shared Contract 완성
4. 영구 강화 밸런스
5. 인게임 강화
6. 행성별 Monster·Boss 밸런스
7. Settlement 보상 지급과 Transaction Log

### P2 — 콘텐츠 확장

1. 행성 스테이지
2. 일일 콘텐츠
3. 무한 Wave 랭킹
4. Quest와 Achievement
5. Shop 편의·꾸미기 상품
6. Breeding Unity UI

### P3 — 소셜·장기 운영

1. Guild
2. Guild Boss
3. 시즌 랭킹
4. 시즌 패스
5. 이벤트 콘텐츠

---

## 27. 최종 확정이 필요한 정책

1. 일반 매치 목표 시간
2. Wave별 최종 Spawn 수와 간격
3. Legendary 리롤 비용과 후보 재등장 규칙
4. 신규 사용자의 기본 Mythic 해금 수
5. 꽝형 2개 보상 상세
6. 행성별 권장 전투력과 보상
7. 영구 강화 비용·조각·성장 세포 요구량
8. 실패 보상 최소 Wave와 보상 곡선
9. Quest별 구체 보상
10. 일일 콘텐츠 입장·보상·초기화 시각
11. 무한 모드 시즌 길이와 구간 보상
12. Guild 콘텐츠 출시 단계
13. Breeding 전용 Mythic 확률과 천장
14. 연결 종료·명시적 이탈·매치 종료 시 보상 경계
15. 상점 성장 상품의 구매 제한
