# Domain Architecture

## Principle
기술 기준이 아니라 도메인 책임 기준으로 나눕니다.

## Client Recommendation
```text
Client/Assets/
├─ Scripts/
│  ├─ User/
│  │  ├─ Lobby/
│  │  ├─ Shop/
│  │  ├─ Economy/
│  │  ├─ Alien/
│  │  ├─ Kidnap/
│  │  ├─ Merge/
│  │  ├─ Mutation/
│  │  └─ Data/
│  ├─ Battle/
│  │  ├─ Monster/
│  │  ├─ Boss/
│  │  ├─ Wave/
│  │  ├─ Projectile/
│  │  ├─ Targeting/
│  │  ├─ Effects/
│  │  └─ Map/
│  └─ Shared/
│     ├─ Contracts/
│     ├─ DTO/
│     ├─ Enums/
│     └─ Network/
└─ Editor/
   ├─ User/
   ├─ Battle/
   └─ Shared/
```

## Server Recommendation
```text
server/.../
├─ user/
├─ economy/
├─ alien/
├─ shop/
├─ gacha/
├─ balance/
├─ battle/
├─ shared/
└─ common/
```

현재 코드를 한 번에 이동하지 않습니다.
새 기능부터 이 구조를 적용하고, 기존 코드는 기능 수정 시 점진적으로 이동합니다.
