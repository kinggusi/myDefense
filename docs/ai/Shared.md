# Shared Contract

## Naming

Human

사용 금지

Alien

Wakjeo

Kidnap

Merge

Mutation

사용

---

## Merge Rule

같은 종

+

같은 종

↓

다음 등급

전체 Pool

랜덤

다른 종

↓

Merge 불가

---

## Mutation Rule

모든 Alien은

Mutation Injector를

가질 수 있습니다.

실제 Mutation은

신화에서만 발동합니다.

주입제로 얻은 Mutation과

랜덤 Mutation은

최종 결과가 같습니다.

---

## Grid

4 x 6

24 Slots

Alien

Mutation Injector

모두

1칸 사용

빈칸 없으면

Kidnap 불가

자동 배치

↓

왼쪽부터

첫 빈칸

---

## Player State

ACTIVE

ELIMINATED

SPECTATING

MatchState

RUNNING

FAILED

CLEARED

---

## Damage Contract

StatCalculator

↓

DamagePayload 생성

Battle

↓

충돌 판정

↓

DamagePayload 전달

↓

Monster HP 감소

---

## Data

기준 데이터

CSV

↓

JSON

↓

Unity ScriptableObject

↓

Spring Boot

---

## Folder Responsibility

User

↓

Economy

Lobby

Merge

Mutation

Alien

Spring

Battle

↓

Monster

Boss

Projectile

Physics

Collision

Shared

↓

DTO

DamagePayload

Interface

Convention

Naming

---

## Coding Rule

작업 전

변경 계획 출력

수정 파일 목록 출력

작업 후

변경 요약 출력

컴파일 오류 출력

---

## Forbidden

직접

main

dev

수정 금지

git reset --hard

금지

.unity YAML

직접 수정 금지

씬은

Editor Tool을 우선 사용

---

## Project Identity

장르

2인 협동 머지 디펜스

기술

Unity 6

Photon Fusion

Spring Boot

핵심

Kidnap

Merge

Mutation

협동 플레이

생체변이