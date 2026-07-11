# Wak-jeo Defense Agent Rules

## 1. Project
- Genre: 2-player cooperative merge defense
- Client: Unity 6
- Realtime networking: Photon Fusion
- Server: Spring Boot
- Repository:
  - `Client/`: Unity project
  - `server/`: Spring Boot project
  - `docs/`: project documents
  - `docs/ai/`: role and review documents

## 2. Read Before Work
Read these files before implementation:
1. `AGENTS.md`
2. `docs/00_GAME_DESIGN.md`
3. `docs/01_TECH_ARCHITECTURE.md`
4. `docs/02_PROJECT_ROADMAP.md`
5. Relevant role document under `docs/ai/`

## 3. Terminology
- Use `Alien`, `Unit`, or `Wakjeo`.
- Never use `Human`.
- Battle summon: `Kidnap`
- Lobby permanent acquisition: `Gacha`
- Combine identical species: `Merge`
- Biological mutation: `Mutation`
- Mutation item: `Mutation Injector`

## 4. Core Game Rules
- Each player uses a `4 x 6` board.
- Alien and Mutation Injector each occupy one slot.
- Kidnap is blocked when no empty slot exists.
- Kidnap result is placed in the first empty slot by the agreed ascending board order.
- Merge is allowed only for identical Alien species of the same grade.
- Merge result is randomly selected from the entire next-grade pool.
- Legendary-to-Mythic result is selected from the current player's unlocked Mythic pool.
- Fixed evolution lineage and `evolutionTargetId` must not be used for gameplay.
- Mutation DNA may be carried before Mythic.
- Actual Mutation effects activate only on Mythic.

## 5. Responsibility Boundary
### System / Economy / Logic
- Alien data
- Kidnap
- Merge
- Mutation
- Mutation Injector
- Personal in-game gold
- StatCalculator
- Lobby, Shop, Collection
- Spring account and persistent-data APIs
- Data pipeline

### Battle / Physics / Action
- Monster movement
- Boss movement
- Wave execution
- Target detection
- Projectile
- Collision and hit
- Effects and animation
- Shared lane
- Boss timer

## 6. Damage Boundary
- System calculates how much damage through `StatCalculator`.
- Battle decides who is hit and applies `DamagePayload`.
- Battle code must not duplicate damage formulas.
- Damage targets should implement `IDamageable`.

## 7. Networking
- Persistent realtime battle state uses Fusion `[Networked]` properties.
- One-time commands and events use RPC.
- State Authority validates:
  - Kidnap
  - Merge
  - Mutation
  - Gold spending
  - Player state
- Spring Boot manages persistent account data, balance data, unlocks, logs, and battle results.

## 8. Unity Safety
- Never edit `.unity` or `.prefab` YAML directly.
- Never manually create or guess GUIDs or `.meta` contents.
- Scene and Prefab generation must use Unity Editor APIs.
- Prefer:
  - `EditorSceneManager`
  - `AssetDatabase`
  - `PrefabUtility`
- If an asset cannot be found, report a warning instead of inventing a reference.

## 9. Git Safety
- Never edit `main` or `dev` directly.
- Work on `feature/user-*` or `feature/battle-*`.
- Do not run:
  - `git reset --hard`
  - `git clean -fdx`
- Do not commit or push without explicit approval.
- Before editing, report:
  - implementation plan
  - expected changed files
- After editing, report:
  - changed files
  - compile result
  - tests
  - risks
