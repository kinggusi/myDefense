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
3. `docs/98_IMPLEMENTATION_TASKS.md`
4. `docs/00_GAME_DESIGN.md`
5. `docs/01_TECH_ARCHITECTURE.md`
6. `docs/02_PROJECT_ROADMAP.md`
7. `docs/ai/Ownership.md`
8. Relevant role document: `docs/ai/User.md` or `docs/ai/Battle.md`
9. `docs/ai/Shared.md`
10. For Unity implementation or validation: `docs/04_TEST_STRATEGY.md`
11. For review: `docs/ai/Review.md`

## Task Orchestration
- `docs/98_IMPLEMENTATION_TASKS.md` is the single source of truth for implementation task priority, ownership, dependencies, and status.
- When the user says `작업하자`, follow the PM orchestration workflow in section `0. PM 자동 오케스트레이션` of that document.
- Resolve the working role in this order: explicit user instruction, current branch prefix (`feature/user-*` or `feature/battle-*`), then the relevant role document.
- If the role is still ambiguous, ask once whether this thread owns User/System or Battle work.
- Do not skip a higher-priority eligible task unless it is blocked or the user explicitly selects another task.
- Separate planning, implementation, and review. Do not edit files during the planning-report phase.
- When thread orchestration tools are available, the PM thread should create or steer the implementation thread and review its reports directly so the user does not need to copy reports between threads.
- The PM thread creates the implementation and independent-review subagents. Users do not create or relay between those subagents.
- The implementation subagent must not approve its own work. An independent read-only reviewer inspects the diff, tests, and risks before PM acceptance.
- Keep write-heavy implementation sequential. Parallel agents are allowed for independent read-only review, test analysis, or investigation.
- Unity UI or interaction tasks require the human validation gates defined in `docs/04_TEST_STRATEGY.md`. AI validation alone cannot mark those tasks complete.

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
- Before Unity Scene or Prefab work, verify that the intended Unity Editor and project are connected and record whether Unity MCP is callable in the current implementation thread.
- Keep Task-specific validation Scenes isolated and register them in the Feature Test Hub; do not combine every feature into one stateful test Scene.
- Test-only Scenes and fixtures must not be included in production Build Settings.

## Git Safety
- Never edit `main` or `dev` directly.
- Use `feature/user-*` or `feature/battle-*`.
- Do not run `git reset --hard` or `git clean -fdx`.
- Before editing, report the plan, expected changed files, and domain impact.
- After editing, report changed files, compile result, tests, and risks.
- Do not commit or push without explicit approval.

## Antigravityrules
 - 모든 대답은 '한국어로' 헤주세요
