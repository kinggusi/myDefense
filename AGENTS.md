# Wak-jeo Defense — Agent Rules

## Project
- Genre: 2-player cooperative merge defense
- Client: Unity 6
- Realtime networking: Photon Fusion
- Server: Spring Boot
- Integration branch: `dev`

## Read Before Work
1. `AGENTS.md`
2. `docs/99_MASTER.md`
3. `docs/00_GAME_DESIGN.md`
4. `docs/01_TECH_ARCHITECTURE.md`
5. `docs/02_PROJECT_ROADMAP.md`
6. `docs/ai/Ownership.md`
7. Relevant role document: `docs/ai/User.md` or `docs/ai/Battle.md`
8. `docs/ai/Shared.md`
9. For review: `docs/ai/Review.md`

## Terminology
- Use `Alien`, `Unit`, or `Wakjeo`.
- Never use `Human`.
- Battle summon: `Kidnap`
- Lobby permanent acquisition: `Gacha`
- Merge: combining identical Alien species
- Biological mutation: `Mutation`
- Mutation item: `Mutation Injector`

## Core Rules
- Each player has a `4 x 6` board, 24 slots.
- Alien and Mutation Injector each occupy one slot.
- Kidnap is blocked when no empty slot exists.
- Kidnap result is placed in the first empty slot using the shared ascending grid order.
- Merge is allowed only for identical Alien species of the same grade.
- Normal-to-Legendary Merge result is random from the whole next-grade pool.
- Legendary-to-Mythic Merge presents 3 distinct candidates from the current player's unlocked Mythic pool.
- Legendary-to-Mythic candidates can be rerolled up to 3 times before one is selected.
- Fixed evolution lineage and `evolutionTargetId` must not drive gameplay.
- Pending Mutation DNA can exist before Mythic.
- Active Mutation effects only apply to Mythic.

## Domain Ownership
### User/System Domain
- Lobby, Shop, Collection
- Economy, Alien, Skill
- Kidnap, Merge, Mutation, Mutation Injector
- StatCalculator, Data Pipeline
- Spring Boot APIs
- Fusion system/economy logic

### Battle Domain
- Battle map, scenes, and prefabs
- Monster, Boss, Wave
- Projectile, Physics, Collision
- Target search, Animation, Effect
- Shared lane, Waypoint, NetworkTransform

### Shared Domain
- DTO, Enum, Interface
- DamagePayload, IDamageable, ITargetProvider, HitEvent
- Common networking contracts

## Damage Boundary
- User/System calculates damage with `StatCalculator`.
- Battle decides who is hit and applies `DamagePayload`.
- Battle must not duplicate damage formulas.

## Networking
- Persistent battle state: Fusion `[Networked]` properties.
- One-time commands/events: RPC.
- State Authority validates Kidnap, Merge, Mutation, Gold spending, and player state.
- Spring Boot manages persistent account data, unlocks, balance data, logs, and battle results.

## Unity Safety
- Never directly edit `.unity` or `.prefab` YAML.
- Never create or guess GUIDs or `.meta` file contents.
- Prefer Unity MCP when available.
- Otherwise use `EditorSceneManager`, `AssetDatabase`, and `PrefabUtility`.
- If an asset cannot be found, report a warning instead of inventing a reference.

## Git Safety
- Never edit `main` or `dev` directly.
- Use `feature/user-*` or `feature/battle-*`.
- Do not run `git reset --hard` or `git clean -fdx`.
- Before editing, report the plan, expected changed files, and domain impact.
- After editing, report changed files, compile result, tests, and risks.
- Do not commit or push without explicit approval.

## Antigravityrules
 - 모든 대답은 '한국어로' 헤주세요
