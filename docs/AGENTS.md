# Wak-jeo Defense Agent Rules

## Project
- Unity 6 + Photon Fusion + Spring Boot
- Client: Client/
- Server: server/
- Read relevant files under docs before implementation.

## Terminology
- Use Alien, Unit, Mutant.
- Never use Human.
- Battle summon is Kidnap.
- Lobby permanent acquisition is Gacha.
- Merge requires identical Alien species.
- Mutation is 생체변이.

## Responsibility
### System / Economy / Logic
- Alien data
- StatCalculator
- Kidnap
- Merge
- Mutation
- Personal in-game gold
- Inventory and lobby
- Spring account and balance API

### Battle / Physics / Action
- Monster movement
- Wave
- Target detection
- Projectile
- Collision and hit
- Effects
- Shared lane and boss timer

## Damage Boundary
- StatCalculator calculates how much damage.
- Battle determines who is hit and applies DamagePayload.
- Do not duplicate damage formulas in Battle code.

## Networking
- Fusion persistent state uses [Networked].
- One-time commands use RPC.
- State Authority validates Kidnap, Merge, Mutation, Gold, and player state.
- Spring Boot is for persistent account data, balance data, and battle result logs.

## Safety
- Never edit main or dev directly.
- Do not delete files or directories without approval.
- Do not edit .unity YAML manually.
- Prefer Unity Editor scripts for scene and prefab generation.
- Do not run git reset --hard or git clean -fdx.
- Show implementation plan and changed-file list before editing.
- Do not commit or push without explicit approval.
