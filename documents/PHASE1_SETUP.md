# Dungeon Blade — Phase 1 Setup Guide

This document covers what was scaffolded in the first pass and the manual steps required in the Unity Editor before you can press Play.

## What's in place

- Full `Assets/_Project/...` folder structure per the GDD
- `com.unity.inputsystem` and `com.unity.cinemachine` added to `Packages/manifest.json`
- Core systems: `GameManager`, `SceneLoader`, `SaveSystem` (JSON persistence at `%AppData%/.../DungeonBlade/`), `InputManager`, `PlayerInputActions` (code-defined bindings — no `.inputactions` asset needed)
- Player Milestone 1 — Movement: `PlayerMovement.cs` (walk / sprint / double-jump / dash / slide / wall-run / bunny-hop) and `PlayerStats.cs` (HP + stamina with regen)
- Four scenes copied from `SampleScene`: `0_LandingScene`, `1_MainMenu`, `2_Lobby`, `3_Dungeon1`
- Editor helper that auto-syncs scenes into Build Settings on first import (`DungeonBlade > Build Settings > Sync Scenes` menu also available manually)
- Default `NewBehaviourScript.cs` removed

## First-time Editor setup

1. **Open the project.** Unity will:
   - Resolve the new packages (Input System + Cinemachine). When Unity asks "Enable new Input System? (requires restart)", choose **Yes, and disable old**. The codebase only uses the new system.
   - Import the four new scenes and auto-add them to Build Settings via `BuildSceneRegistrar`.

2. **Set up the Landing scene (`Assets/Scenes/0_LandingScene.unity`):**
   - Open the scene
   - Create an empty GameObject named `[Bootstrap]`
   - Add the `LandingLoader` component to it
   - Save

3. **Set up the Player in `3_Dungeon1.unity`:**
   - Open the scene
   - Create a Capsule (or empty GameObject named `Player`) at a sensible spawn location
   - Add components in this order:
     - `Character Controller` (Unity built-in)
     - `PlayerStats`
     - `PlayerMovement`
   - Create a child empty GameObject named `CameraRig` at local position (0, 1.6, 0)
   - Drag a `Main Camera` under `CameraRig` (or create one) at local position (0, 0, 0)
   - On the `PlayerMovement` component, drag the `CameraRig` transform into the `Camera Rig` field
   - Add a `Plane` at y=0 to serve as a ground plane for testing

4. **Press Play in `0_LandingScene`** — it should auto-load `1_MainMenu`. From there you can manually open `3_Dungeon1` to test movement.

## Movement controls (current bindings)

| Action                        | Key                                        |
| ----------------------------- | ------------------------------------------ |
| Move                          | WASD                                       |
| Look                          | Mouse                                      |
| Jump (and double-jump in air) | Space                                      |
| Sprint                        | Left Ctrl                                  |
| Dash (directional)            | Left Shift                                 |
| Slide                         | C (while running)                          |
| Wall-run                      | Hold direction into a wall while airborne  |
| Bunny-hop                     | Jump within ~0.18s of landing while moving |

Cursor is locked on play. Esc is bound to Pause but not yet wired to UI.

## M2 — Combat scene wiring

After M1 movement is verified, set up combat in `3_Dungeon1.unity`:

1. **Create the WeaponHolder** under `Player`:
   - Right-click `Player` in Hierarchy → Create Empty → name it `WeaponHolder`
   - Make it a child of `CameraRig` so weapons follow the camera (drag `WeaponHolder` onto `CameraRig`)
   - Set Local Position to `(0.4, -0.3, 0.6)` (roughly hip-level forward of the camera)

2. **Create the Sword:**
   - Right-click `WeaponHolder` → 3D Object → Cube → name it `Sword`
   - Scale to `(0.08, 0.08, 0.8)` to look bladelike
   - Local Position `(0, 0, 0.4)`
   - **Remove the BoxCollider** (it would collide with the player)
   - Add Component → `Sword`
   - Right-click `Sword` → Create Empty → name it `HitOrigin`. Local position `(0, 0, 0.4)`. Drag `HitOrigin` into the Sword's **Hit Origin** field.

3. **Create the Gun:**
   - Right-click `WeaponHolder` → 3D Object → Cube → name it `Gun`
   - Scale to `(0.1, 0.15, 0.4)`. Local Position `(0, 0, 0.2)`
   - Remove the BoxCollider
   - Add Component → `Gun`
   - Right-click `Gun` → Create Empty → name it `Muzzle`. Local position `(0, 0, 0.25)`. Drag `Muzzle` into the Gun's **Muzzle** field.
   - Drag the `Main Camera` into the Gun's **Aim Camera** field.

4. **Add ComboSystem to the Player:**
   - Select `Player` → Add Component → `Combo System`

5. **Add PlayerCombat to the Player:**
   - Select `Player` → Add Component → `Player Combat`
   - **Weapons** array: set Size = 2, drag `Sword` into element 0 and `Gun` into element 1
   - Drag the `ComboSystem` component (from Player) into the `Combo System` field

6. **Create a Training Dummy:**
   - Hierarchy root → 3D Object → Cube → name it `TrainingDummy`
   - Position it ~3 units in front of the player spawn
   - Add Component → `Training Dummy`

7. **Save and Play.**

### Combat controls (M2)

| Action                  | Key                                         |
| ----------------------- | ------------------------------------------- |
| Light attack / Fire gun | Left Mouse                                  |
| Heavy sword (hold both) | Right Mouse + hold Left Mouse, then release |
| Block / Aim Down Sights | Right Mouse                                 |
| Reload                  | R                                           |
| Switch weapon           | Q                                           |

Hits are logged to the Console, and dummies flash red. The 3-hit sword combo (12 / 14 / 20 dmg) chains if you click within ~0.35s of the previous hit. Pressing fire while the gun is equipped after a sword hit triggers the **GunZ-style cancel** — it interrupts the sword recovery so you can immediately shoot.

## M3 — Dungeon: code wiring + zone blockout

M3 has two phases: (3a) wiring the new components onto the player, and (3b) blocking out the 5 zones with primitives.

### 3a — Player wiring (one-time)

1. Open `3_Dungeon1.unity`. Select `Player`.
2. **Add Component → `Respawn Manager`**. It auto-uses the existing `PlayerStats` and `CharacterController`. Leave defaults (3 respawns, 50% HP on respawn).
3. Make sure your test ground plane is on the `Default` layer (or set `Ground Layers` on RespawnManager to match). The RespawnManager continuously sphere-casts down to record the last stable ground for fall recovery.
4. **Add a `ZoneManager`** to the scene: Hierarchy root → Create Empty → name `[ZoneManager]` → Add Component → `Zone Manager`.

### 3b — Block out the dungeon (per GDD §3)

For each zone: create geometry from cubes, add a `Zone` trigger, add the zone's hazards. **Use cubes scaled to size — don't worry about textures or polish; this is a blockout.**

The 5 zones flow linearly. Suggested origin layout (all on Y=0):

| Zone            | Center X | Length (Z) | Width (X)             |
| --------------- | -------- | ---------- | --------------------- |
| 1 — Gate Hall   | 0        | 30         | 12                    |
| 2 — Barracks    | 0        | 25         | 10 (with upper floor) |
| 3 — The Bridge  | 0        | 40         | 8 (broken mid)        |
| 4 — Armory      | 0        | 20         | 14                    |
| 5 — Throne Room | 0        | 30         | 30 (circular feel)    |

Place them end-to-end along Z so the player walks forward through each.

**For each zone, do this:**

1. Create an empty GameObject named `Zone_X_Name` at the zone center.
2. Floor: 3D Object → Cube. Scale to `(width, 0.2, length)`. Position at `(0, -0.1, 0)`.
3. Walls: Cubes scaled to `(0.5, 4, length)` placed at `±width/2` for left/right walls. End walls at `±length/2`.
4. Add a `BoxCollider` (set `Is Trigger`) sized to enclose the playable area, then add a `Zone` component. Set `zoneId` like `zone_1` and `zoneName` like `Gate Hall`.

**Hazards per zone:**

- **Zone 1 (Gate Hall)** — tutorial, no hazards. Place 1–2 `TrainingDummy` objects to represent future Skeleton Soldiers.
- **Zone 2 (Barracks)** — add 2 `SpikeTrap` strips: Cube scaled `(2, 0.2, 0.5)`, set BoxCollider as trigger, add `SpikeTrap` component. Optionally add a child cube as the `Spikes Visual` so spikes pop up.
- **Zone 3 (The Bridge)** — leave a 6-unit gap in the middle of the floor and place a `KillVolume` below the gap: scaled cube at Y=-10, BoxCollider trigger, `KillVolume` component. Add a `CollapsingFloor` segment over part of the bridge (a Cube floor segment + a child empty trigger collider above it set as `standDetector`, with the visual being the floor cube itself).
- **Zone 4 (Armory)** — `ArrowWall`: Place a `PressurePlate` (Cube scaled `(2, 0.05, 2)` with trigger collider) on the floor. Place an empty parent `ArrowWallSpawners` on a side wall with 3 child empties pointed across the corridor (their forward direction is the arrow direction). Add `ArrowWall` component, drag plate into `Plate`, drag the spawner empties into `Arrow Spawn Points`.
- **Zone 5 (Throne Room)** — circular blockout: scale a single floor cube to `(30, 0.2, 30)`. Add a few pillar cubes as cover. No hazards; this is the boss arena.

**Checkpoints (GDD §3.3):**

- Place a `Checkpoint` GameObject (empty + BoxCollider trigger ~3×3×3) at the end of Zone 1. Set `checkpointId = "cp_1"`.
- Place another at the end of Zone 3. `checkpointId = "cp_2"`.
- Each checkpoint should have a child empty named `SpawnPoint` placed where the player should re-spawn from. Drag it into the Checkpoint's `Spawn Point` field.

**Test:** walk through each zone — Console will log `[Zone] Entered <name>` on entry and `[Respawn] Checkpoint reached: <id>` at checkpoints. Walk into the SpikeTrap to take damage. Step on the PressurePlate to trigger the ArrowWall volley (visible as red debug rays). Stand on a CollapsingFloor for ~1.2s to see it fall. Fall into the gap on the Bridge to take fall damage and teleport back to the last stable ground.

## M4 — Enemy AI: NavMesh + enemy placement

M4 introduces three enemy archetypes — **Skeleton Soldier** (fast melee chaser), **Skeleton Archer** (ranged kiter), and **Armored Knight** (slow heavy hitter). All share `EnemyBase` (state machine + perception + hit/flash/death) with subclass-specific attacks.

Scripts live under [Assets/\_Project/Enemies/](Assets/_Project/Enemies/):

- [AI/EnemyBase.cs](Assets/_Project/Enemies/AI/EnemyBase.cs) — FSM, aggro + sight-cone LOS, patrol, hit flash, death
- [AI/EnemyStats.cs](Assets/_Project/Enemies/AI/EnemyStats.cs) — per-enemy tunables
- [Skeleton/SkeletonSoldier.cs](Assets/_Project/Enemies/Skeleton/SkeletonSoldier.cs)
- [Archer/SkeletonArcher.cs](Assets/_Project/Enemies/Archer/SkeletonArcher.cs) + [Archer/Arrow.cs](Assets/_Project/Enemies/Archer/Arrow.cs)
- [Knight/ArmoredKnight.cs](Assets/_Project/Enemies/Knight/ArmoredKnight.cs)

### 4a — One-time scene prep

1. **Tag the Player.** Open `3_Dungeon1.unity`, select `Player`, set **Tag = `Player`** (create it via `Add Tag…` if missing). Enemy perception uses `FindGameObjectWithTag("Player")`.

2. **Create an `Enemy` layer.** `Edit > Project Settings > Tags and Layers` → add a user layer named `Enemy`. You'll assign this to enemy GameObjects and filter it on the player's weapon `Hit Mask`.

3. **Configure Physics collisions** (`Edit > Project Settings > Physics`):
   - Make sure the `Enemy` layer collides with `Default` (so they don't fall through floors) and whatever the Player is on.
   - Uncheck `Enemy ↔ Enemy` if you don't want them to shove each other (optional — NavMeshAgent avoidance handles most of this already).

4. **Set the player's hit masks.** On the `Sword` and `Gun` components, set their `Hit Mask` to include `Enemy` (plus `Default` if you want them to hit the training dummy too).

### 4b — Bake the NavMesh

Unity 2022.3 uses the **AI Navigation** package (separate from the legacy built-in system).

1. **Install the package** (one-time): `Window > Package Manager` → switch the dropdown from _Packages: In Project_ to _Packages: Unity Registry_ → search **AI Navigation** → click **Install**.
2. Open `3_Dungeon1.unity`.
3. Create an empty GameObject at the scene root, name it `[NavMesh]`.
4. Add Component → **Nav Mesh Surface**. Leave defaults (Agent Type = _Humanoid_, Collect Objects = _All_, Include Layers = _Everything_, Use Geometry = _Render Meshes_).
5. Click **Bake** on the component. A blue overlay should appear on walkable surfaces. Verify the Bridge gap in Zone 3 creates a **real hole** in the NavMesh.
6. Any time you change zone geometry, click **Bake** again.

_(If you see `Window > AI > Navigation (Obsolete)` — that's the legacy workflow. Ignore it; use NavMeshSurface above.)_

### 4c — Build the three enemy prefabs

The enemies are primitive-based blockouts for now — model/animation polish is M9. For each archetype:

**Shared base setup (do this once, then duplicate):**

1. Hierarchy → 3D Object → **Capsule**. Name it (see below). Move to a NavMesh'd spot.
2. Set the GameObject's **Layer = `Enemy`**.
3. Delete the default `CapsuleCollider` and add a fresh one — Radius `0.5`, Height `2.0`, Center `(0, 1, 0)`. Leave it non-trigger.
4. Add Component → `Nav Mesh Agent` (Unity built-in). Leave **all** defaults — the enemy script overwrites speed, angular speed, and stopping distance from `EnemyStats` at Awake. (You'll see `EnemyStats` as a block on the enemy script itself in step 5 — not on the NavMeshAgent.)
5. **Add the enemy script.** In the Inspector, click **`Add Component`** at the bottom. Type the script name (`Skeleton Soldier`, `Skeleton Archer`, or `Armored Knight` — Unity auto-converts `SkeletonSoldier.cs` to a searchable "Skeleton Soldier" display name) and click it. The component appears with a **Stats** foldout (open it — that's where `EnemyStats` lives: Health / Combat / Perception / Movement / Ranged / Armor blocks) plus Patrol, Perception, and FX foldouts below. If Unity pops a yellow warning like _"Missing component: NavMeshAgent"_, go back to step 4 — the script auto-adds it in most cases but requires it at runtime.

6. **Tint the capsule** so you can tell enemies apart at a glance (and so the hit-flash actually shows a color change).
   - In the Project window, right-click in `Assets/_Project/Materials/` → **Create → Material**. Name it per the enemy (`M_SkeletonSoldier`, `M_SkeletonArcher`, `M_ArmoredKnight`).
   - Click the new material. In the Inspector, click the **Base Map** color swatch (the white box next to "Base Map") and pick a color — see "Per-enemy specifics" below for suggested tints.
   - Drag the material from the Project window onto the Capsule in the Hierarchy (or Scene view). The capsule should change color immediately.
   - **Important:** the `EnemyBase` script snapshots this color in `Awake()`, so whatever you set here is the "normal" color it returns to after each hit-flash. Don't change materials at runtime.

**Per-enemy specifics:**

- **`Skeleton_Soldier`** — Add Component → **`Skeleton Soldier`**.
  - **Stats block**: defaults are tuned (100 HP, 15 dmg, 5 m/s chase, 10m aggro, 1.8m attack range) — leave as-is for your first one, you can tweak later.
  - **Tint:** bone-white (RGB ~`230, 225, 210`) so it reads as "skeleton" at a glance.

- **`Skeleton_Archer`** — Add Component → **`Skeleton Archer`**. Two extra setup steps before it can fire:

  **(a) Create the Arrow prefab (one-time):**
  1. Hierarchy → 3D Object → **Cube**, name it `Arrow`.
  2. Scale to `(0.05, 0.05, 0.4)` — skinny shaft shape.
  3. Add Component → **`Rigidbody`**. Uncheck **Use Gravity** (the `Arrow` script also sets this, but uncheck for cleanliness). Set **Collision Detection = Continuous Dynamic** (fast-moving projectiles need this or they'll tunnel through walls).
  4. On the existing **Box Collider**, check **Is Trigger**.
  5. Add Component → **`Arrow`** (the script from [Assets/\_Project/Enemies/Archer/Arrow.cs](Assets/_Project/Enemies/Archer/Arrow.cs)).
  6. Drag the `Arrow` GameObject from the Hierarchy into the Project window at `Assets/_Project/Enemies/Archer/` — this saves it as a prefab (you'll see the GameObject name turn blue).
  7. Delete the `Arrow` from the Hierarchy. The prefab asset is all you need.

  **(b) Set up the archer GameObject:**
  1. Right-click the `Skeleton_Archer` capsule in the Hierarchy → **Create Empty** (child). Name it `ShootOrigin`. Set its Transform to local position `(0, 1.4, 0.5)` — this is where arrows spawn from (chest-height, slightly in front).
  2. Select `Skeleton_Archer` again. In the `Skeleton Archer` component:
     - **Arrow Prefab**: drag the `Arrow` prefab from the Project window into this field.
     - **Shoot Origin**: drag the child `ShootOrigin` into this field.
     - **Projectile Hit Mask**: click the dropdown → check `Default` (so arrows can hit the player — remember the Player is on `Default`).
     - **Min Range** = `5`, **Max Range** = `14`.
  3. Open the **Stats** foldout and set **Aggro Range = 15**, **Chase Speed = 3** (archers are slower on foot than soldiers).
  4. **Tint:** dark green (RGB ~`60, 110, 70`).

- **`Armored_Knight`** — Add Component → **`Armored Knight`**.
  - Open the **Stats** foldout and override these defaults:
    - **Max Health** = `250`
    - **Attack Damage** = `25`
    - **Chase Speed** = `3`
    - **Aggro Range** = `8`
    - **Attack Range** = `2.4`
  - Leave **Attack Windup / Attack Recovery / Damage Reduction** at their defaults — the knight script auto-bumps them to 0.75s / 0.7s / 0.35 in `Awake()` if left at zero, so don't override unless you want different values.
  - **Tint:** steel grey (RGB ~`120, 125, 135`). The knight will flash **orange** briefly during its wind-up as a telegraph — that's intentional (tells you an attack is coming, giving you time to dodge or parry).

**After each enemy is set up:**

1. Save the scene (`Ctrl+S`).
2. (Optional but recommended) Turn the configured enemy into a **prefab** so you don't have to redo wiring each time you place one: drag the GameObject from Hierarchy into `Assets/_Project/Enemies/Prefabs/`. Future placements = drag the prefab into the scene, done.

### 4d — Zone placement (per GDD §3)

Place enemies on the NavMesh — they'll auto-patrol a small radius around their spawn and aggro when the player enters LOS.

| Zone            | Enemies                                                      |
| --------------- | ------------------------------------------------------------ |
| 1 — Gate Hall   | 2× Skeleton Soldier (replace the two TrainingDummies)        |
| 2 — Barracks    | 2× Skeleton Soldier + 1× Skeleton Archer on the upper floor  |
| 3 — The Bridge  | 1× Skeleton Archer at the far end (they kite across the gap) |
| 4 — Armory      | 1× Armored Knight + 1× Skeleton Soldier                      |
| 5 — Throne Room | Empty — this is the boss arena (M5)                          |

**Keep one `TrainingDummy` in Zone 1** next to the player spawn for combat-tuning tests.

### 4e — Controls & feedback

No new controls. Expect the following Console logs while testing:

- `[Skeleton_Soldier] -14 (Melee)  HP: 86/100` — player hit lands
- `[Skeleton_Archer] died.` — enemy killed, fades out after ~2s
- `[Player] Took 15 Melee damage from Armored_Knight.  HP: 85/100` — enemy hit lands

Gizmos: select an enemy in the scene view to see its **aggro sphere** (yellow wire) and **sight cone** (red lines). Soldier/Knight also show their active-frame hit sphere (magenta/red).

### 4f — Tuning knobs

Everything tunable is on the `EnemyStats` block in the Inspector — no code edits needed. Common starting points:

- Too deadly? Drop `AttackDamage` or bump `AttackCooldown`.
- Too passive? Increase `AggroRange` / `SightAngle`.
- Archer never shoots? Raise `MaxRange` past the arena length, or confirm the arrow prefab's `Arrow` hit mask includes the player's layer.
- Enemies stall mid-path? NavMesh probably wasn't baked after geometry changes — re-bake.

## M5 — Boss: Undead Warlord

M5 adds the Zone 5 boss encounter. The **Undead Warlord** has 1000 HP split across three phases with distinct mechanics. Code lives under [Assets/\_Project/Boss/](Assets/_Project/Boss/):

- [Scripts/BossBase.cs](Assets/_Project/Boss/Scripts/BossBase.cs) — reusable base: dormant-until-activated, 3 phases, HP-gated transitions with damage reduction during transitions
- [Scripts/BossArenaTrigger.cs](Assets/_Project/Boss/Scripts/BossArenaTrigger.cs) — arena entry volume. Seals the door behind the player and activates the boss.
- [Scripts/BossAddSpawner.cs](Assets/_Project/Boss/Scripts/BossAddSpawner.cs) — summons weakened Skeleton_Soldier adds during Phase 2
- [UndeadWarlord/UndeadWarlord.cs](Assets/_Project/Boss/UndeadWarlord/UndeadWarlord.cs) — the boss itself
- [UndeadWarlord/BoneSpear.cs](Assets/_Project/Boss/UndeadWarlord/BoneSpear.cs) — Phase 2 ranged projectile

**Also updated in M5:**

- `PlayerMovement.IsInvulnerable` (true while dashing) — grants 0.18s i-frames during dash
- `PlayerStats.ApplyDamage` now skips damage when i-frames are active. Console logs `[Player] Dodged ... (i-frames).`

### Phase design

| Phase          | HP Range          | Behavior                                                                                                                                                                                     |
| -------------- | ----------------- | -------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------- |
| **1**          | 100% → 66%        | Cleave (30 dmg, 3.2m range) + Heavy Slam (50 dmg AoE, orange telegraph, 1.2s wind-up, 8s cd). Slow, telegraphed.                                                                             |
| **Transition** | at each threshold | 1.2s purple flash, 90% damage reduction — can't cheese with a burst combo.                                                                                                                   |
| **2**          | 66% → 33%         | Keeps cleave + slam. Adds: (a) **Bone Spear** volley — 3 spears @ 18 dmg, 10s cd, ranged pressure. (b) **Summon adds** — 2× weakened Skeleton_Soldier (80 HP), 12s cd, max 4 alive.          |
| **3**          | 33% → 0%          | Enrage: +50% move speed, +30% damage. All Phase 1 attacks still available, PLUS **Spinning Sweep** — 360° AoE, 25 dmg/tick, 1.8s active, 6s cd. No more adds or spears — just pure pressure. |

### 5a — Build the Bone Spear prefab (one-time)

1. Hierarchy → 3D Object → **Cube**, name `BoneSpear`.
2. Scale `(0.12, 0.12, 0.9)` — thicker than the archer's arrow.
3. **Layer = `Default`** (not Enemy — same issue as the arrow).
4. Add Component → **Rigidbody**. Uncheck **Use Gravity**. Collision Detection = **Continuous Dynamic**.
5. Change the BoxCollider to **Is Trigger**.
6. Add Component → **`Bone Spear`** (from [Assets/\_Project/Boss/UndeadWarlord/BoneSpear.cs](Assets/_Project/Boss/UndeadWarlord/BoneSpear.cs)).
7. Drag to `Assets/_Project/Boss/UndeadWarlord/` to save as a prefab. Delete the scene copy.

### 5b — Build the Undead Warlord

1. Hierarchy → 3D Object → **Capsule**, name `UndeadWarlord`. Place in the center of Zone 5 (Throne Room).
2. **Layer = `Enemy`**.
3. **Scale to 1.5× the skeletons** so it reads as a boss — set Transform Scale `(1.5, 1.8, 1.5)`.
4. Delete default CapsuleCollider → add fresh one: Radius `0.5`, Height `2.0`, Center `(0, 1, 0)`. (Scale on the Transform makes it physically bigger without math.)
5. Add Component → **Nav Mesh Agent**. Leave defaults.
6. Add Component → **`Undead Warlord`**.

**In the Undead Warlord Inspector, configure the Stats block first:**

- **Max Health** = `1000`
- **Attack Damage** = `30` (base cleave)
- **Chase Speed** = `5`
- **Move Speed** = `2.5`
- **Aggro Range** = `25` (covers the whole arena)
- **Sight Angle** = `360` (bosses don't have a sight cone — they always see you)
- **Attack Range** = `3.2`
- **Damage Reduction** = `0.2`

**Then the boss-specific fields** (most default values are tuned — only these need manual wiring):

- **Phase 2 Threshold** = `0.66`, **Phase 3 Threshold** = `0.33` — leave defaults unless you want different pacing.
- **Bone Spear Prefab** — drag your `BoneSpear` prefab into this field.
- **Spear Origin** — right-click `UndeadWarlord` → Create Empty → name `SpearOrigin`, local position `(0, 2.0, 0.7)` (chest-high, in front). Drag into this field.
- **Spear Hit Mask** — check **`Default`** only (same as the archer's arrow — hits the player).
- **Add Spawner** — set up next (§5c), then drag the spawner component into this field.
- **Melee Hit Mask** — check **`Default`** only (hits the player, not other enemies).

7. Create material `M_UndeadWarlord` in `Assets/_Project/Materials/`. Tint it **bone/grey-purple** (~`180, 160, 190`). Drag onto the capsule — the script snapshots this as base color.

### 5c — Set up the add spawner

1. In the Zone 5 arena, Hierarchy → Create Empty → name `BossAddSpawner`. Place near the arena center, at floor level.
2. Right-click `BossAddSpawner` → Create Empty (child) → name `SpawnPoint_1`. Position at one of the arena's perimeter corners.
3. Repeat: `SpawnPoint_2`, `SpawnPoint_3`, `SpawnPoint_4` at the remaining three corners. All must be on the NavMesh (blue overlay should cover them).
4. Select `BossAddSpawner`. Add Component → **`Boss Add Spawner`**. Configure:
   - **Add Prefab** — drag your `Skeleton_Soldier` prefab (from `Assets/_Project/Enemies/Prefabs/`) here.
   - **Spawn Points** — set size = 4, drag `SpawnPoint_1..4` into the slots.
   - **Adds Per Wave** = `2`, **Max Alive Adds** = `4`.
   - **Override Max Health** = `80` (weakens the adds), **Override Move Speed** = `0` (skip), **Override Attack Damage** = `0` (skip).
5. Go back to `UndeadWarlord` → drag the `BossAddSpawner` component into the boss's **Add Spawner** field.

### 5d — Set up the arena trigger + seal wall

This step makes two things: a **detection volume** (`BossArenaTrigger` — invisible, fires when player walks through) and a **physical wall** (`ArenaSealWall` — solid cube that appears behind the player to lock them in). They are separate GameObjects with different jobs.

**Important Unity concept before you start:**

- **Transform → Scale** changes how big the **mesh and collider together** appear in the world. Use this for visible cubes.
- **Box Collider → Size** is the dimension of the collider in **local units**, multiplied by Scale. Use this when sizing a trigger that has no mesh (like an empty GameObject with just a collider).
- **Transform → Position** is where the GameObject sits in the world.

---

#### Step 1 — Find the doorway between Zone 4 and Zone 5

Open Zone 5 in the Scene view. You should see the boundary wall between Zone 4 (Armory) and Zone 5 (Throne Room). There should be a **gap** in that wall where the player walks through. **If there's no gap, you need one** — temporarily disable the wall cube, or split it into two cubes with a gap. Players need a doorway to enter the arena.

**Find these numbers and write them down:**

- **Doorway opening width (X)**: how wide is the gap? (Typically 4–8 units)
- **Doorway opening height (Y)**: how tall? (Should match your wall height, usually 4)
- **Doorway center position**: where is the middle of the opening in world coordinates? Click an empty point at the center of the gap in Scene view — Unity shows world coords in the bottom of the Inspector when an object is selected.

For this guide I'll assume **doorway = 6 wide, 4 tall, centered at world `(0, 2, -15)`**. Adjust to match your scene.

---

#### Step 2 — Create the BossArenaTrigger (detection volume)

1. Hierarchy → right-click → **Create Empty**. Name it `BossArenaTrigger`.
2. Inspector → **Transform**:
   - **Position** = the doorway center, e.g. `(0, 2, -15)`
   - **Scale** = `(1, 1, 1)` (leave default — we'll size via the collider)
3. Add Component → **Box Collider**. In the Box Collider:
   - Check **Is Trigger** ✅
   - **Center** = `(0, 0, 0)` (default — collider centers on the GameObject's position)
   - **Size** = `(8, 4, 2)` for our 6-wide doorway:
     - `8` = 2 units wider than the doorway, so a sprinting/dashing player can't squeeze around
     - `4` = matches doorway height
     - `2` = depth — even a fast-moving player passes through 2 units of trigger
   - **Adjust the Size values** if your doorway is different — make X = doorway_width + 2, Y = doorway_height, Z = 2.

You should now see a translucent green wireframe box in the Scene view, covering the doorway.

---

#### Step 3 — Create the ArenaSealWall (physical barrier)

1. Hierarchy → right-click `BossArenaTrigger` → **3D Object → Cube**. Name it `ArenaSealWall`. (It's a child of BossArenaTrigger now.)
2. Inspector → **Transform**:
   - **Position** = `(0, 0, 0)` (this is _local_ position relative to its parent, so it sits at the same world position as the trigger). If the trigger is at the doorway center, leaving local position at zero means the wall is also at the doorway center.
   - **Scale** = `(6, 4, 0.3)` — the wall's visible mesh dimensions:
     - `6` = matches your doorway opening width exactly (smaller than the trigger, so it plugs the gap without poking into wall geometry)
     - `4` = matches doorway height
     - `0.3` = thin — looks like a barrier, not a room
   - **Adjust** to match your doorway: X = doorway_width, Y = doorway_height, Z = 0.3.
3. Inspector → **Box Collider** (auto-attached to the Cube):
   - **Is Trigger** ❌ unchecked — this is a **solid** wall.
   - Leave Center `(0, 0, 0)` and Size `(1, 1, 1)` (default — the Transform Scale handles size).
4. Inspector → **Mesh Renderer** → **Materials** → click the small material to inspect it. Either:
   - **Quick option**: leave the default white material. The wall will be a solid white cube — visible enough.
   - **Better option**: Create a new material in `Assets/_Project/Materials/` named `M_ArenaSealWall`. Tint it **dark grey** (RGB ~`60, 60, 70`) so it visually contrasts with the floor. Drag onto `ArenaSealWall`.

You should now see a grey wall slab plugging the doorway in the Scene view.

---

#### Step 4 — Wire the BossArenaTrigger script

1. Select `BossArenaTrigger` in Hierarchy.
2. Add Component → **`Boss Arena Trigger`**. Configure:
   - **Boss** — drag the `UndeadWarlord` GameObject from Hierarchy into this field.
   - **Arena Seal Wall** — drag the `ArenaSealWall` child from Hierarchy into this field.
   - **Player Tag** — `Player` (already the default).

---

#### Step 5 — Save and verify

1. **Save the scene** (`Ctrl+S`).
2. Press **Play** without walking into the arena yet:
   - The wall should **disappear immediately** on scene load — the script auto-disables it at `Awake`. ✅
   - The trigger volume is invisible (only its collider exists) — you won't see it in Game view, only in Scene view via the gizmo.
3. Walk your player toward the doorway. The moment you cross the trigger, you should see:
   - **Wall reappears** behind you, blocking retreat.
   - Console: `[Boss] Arena sealed — fight begins.` and `[Boss] UndeadWarlord activated.`
4. Kill the boss. On death:
   - **Wall disappears**.
   - Console: `[Boss] Defeated — arena unsealed.`

If the wall doesn't disappear on scene load, you forgot to assign it to the script's `Arena Seal Wall` field. If the trigger doesn't fire, your player might not be tagged `Player` (check Step 4a from M4).

### 5e — Re-bake NavMesh

After adding the boss, add spawner, spawn points, and seal wall — select `[NavMesh]` → **Nav Mesh Surface** → click **Bake** again. Adds need to path, the boss needs to path, and the seal wall needs to register as a barrier.

### 5f — Playtest checklist

Press Play and walk from the dungeon entrance all the way to Zone 5.

1. **Approach Zone 5** — the boss is visible but doesn't react (dormant).
2. **Enter the arena trigger** — Console logs `[Boss] Arena sealed — fight begins.` Seal wall appears behind you. Boss activates.
3. **Phase 1** — boss chases you, cleaves at ~3m. Takes ~10 hits of your sword combo before hitting 66% HP.
4. **Transition to Phase 2** — boss flashes purple for ~1.2s. Damage you deal during this is massively reduced. Console: `[Boss] UndeadWarlord transitioning → Phase2`.
5. **Phase 2** — boss now additionally:
   - Summons 2× skeleton adds every 12s. Console: `[BossAddSpawner] Spawned 2 add(s).`
   - Fires bone spear volleys (3 spears each time). Dodge with dash!
6. **Dash through an attack** — Console logs `[Player] Dodged Melee attack from UndeadWarlord (i-frames).` i-frames work.
7. **Transition to Phase 3** — at 33% HP, purple flash again.
8. **Phase 3** — boss turns red. Stops summoning adds / spears. Uses spinning sweep AoE. Moves faster, hits harder.
9. **Kill boss** — at 0 HP, boss fades. Seal wall disappears. Console: `[Boss] Defeated — arena unsealed.`
10. **Walk back out** — arena entrance is passable again.

### 5g — Tuning knobs

Common adjustments without touching code:

| Symptom                          | Knob                                                                                                                       |
| -------------------------------- | -------------------------------------------------------------------------------------------------------------------------- |
| Fight too short                  | Bump Stats → Max Health. 1000 ≈ 45–60s.                                                                                    |
| Phase 1 too easy                 | Lower `Cleave Cooldown` (2.0 → 1.2) or raise `Cleave Damage` (30 → 40).                                                    |
| Phase 2 feels same as Phase 1    | Lower `Add Spawn Cooldown` (12 → 8) or `Spear Cooldown` (10 → 6).                                                          |
| Phase 3 one-shots me             | Lower `Enrage Damage Mul` (1.3 → 1.15) or `Spin Damage` (25 → 15).                                                         |
| Can't get close enough to attack | Raise `Attack Range` (3.2 → 4.0).                                                                                          |
| Transitions too punishing        | Lower `Transition Damage Reduction` (0.9 → 0.6) — you'll still get some value from burst combos.                           |
| Boss won't face me               | The boss auto-faces during windup. If it's spinning wildly, check NavMeshAgent → Angular Speed (should be 540 from stats). |

## M6 — Inventory: data, UI, equipment, save/load

M6 adds the player inventory: a **6×8 grid (48 slots)**, a **6-slot hotbar** (keys 1–6), **4 equipment slots** (Head / Body / MainHand / OffHand), drag-and-drop, right-click-use for consumables, hover tooltips, and persistence in `profile.json`.

Code lives under [Assets/\_Project/Inventory/](Assets/_Project/Inventory/):

- [Scripts/Item.cs](Assets/_Project/Inventory/Scripts/Item.cs) — base ScriptableObject (id, name, icon, type, stackable, value)
- [Scripts/WeaponItem.cs](Assets/_Project/Inventory/Scripts/WeaponItem.cs) + [ConsumableItem.cs](Assets/_Project/Inventory/Scripts/ConsumableItem.cs) — sample subclasses
- [Scripts/InventoryManager.cs](Assets/_Project/Inventory/Scripts/InventoryManager.cs) — singleton, holds grid + hotbar + equipment, fires events
- [Scripts/InventoryController.cs](Assets/_Project/Inventory/Scripts/InventoryController.cs) — Tab toggle + soft-pause (cursor unlocks, player input blocked, world keeps running)
- [Scripts/EquipmentBinder.cs](Assets/_Project/Inventory/Scripts/EquipmentBinder.cs) — toggles the scene Sword/Gun GameObjects based on what's equipped
- [Scripts/HotbarBinder.cs](Assets/_Project/Inventory/Scripts/HotbarBinder.cs) — keys 1–6 use hotbar slots
- [Scripts/InventoryPersistence.cs](Assets/_Project/Inventory/Scripts/InventoryPersistence.cs) — load on Start, save on Quit
- [Scripts/ItemDatabase.cs](Assets/_Project/Inventory/Scripts/ItemDatabase.cs) — registry of all item assets (used to resolve `itemId → Item` on load)
- [UI/SlotWidget.cs](Assets/_Project/Inventory/UI/SlotWidget.cs) — single slot UI behavior (drag, drop, click, tooltip)
- [UI/ItemTooltip.cs](Assets/_Project/Inventory/UI/ItemTooltip.cs) — floating tooltip
- [UI/InventoryUI.cs](Assets/_Project/Inventory/UI/InventoryUI.cs) — builds the layout, owns drag-ghost

**Also changed:**

- `PlayerInputActions`: `OpenInventory` rebound from `I` to **`Tab`**. Added `Hotbar5` (`5` key) and `Hotbar6` (`6` key).
- `PlayerStats`: added `RestoreStamina(amount)` for stamina-restoring consumables.
- `PlayerMovement` + `PlayerCombat`: skip input when inventory is open (gravity still applies).
- `PlayerProfile`: now serializes inventory contents.

### 6a — Create sample item assets (3 min)

Right-click in `Assets/_Project/Inventory/Items/` (create the folder if missing):

**1. Health Potion (consumable):**

- **Create → DungeonBlade → Item → Consumable Item**
- Name file: `Item_HealthPotion`
- Set fields:
  - **Item Id** = `health_potion`
  - **Display Name** = `Health Potion`
  - **Description** = `Restores 50 HP. A familiar red glow.`
  - **Type** = `Consumable`
  - **Stackable** = ✅, **Max Stack** = `99`
  - **Sell Value** = `5`, **Buy Value** = `25`
  - **Heal Amount** = `50`
  - **Icon** = drag in any small red circle sprite (Unity built-in `Knob` works), or leave blank — tooltip still shows the name.

**2. Stamina Tonic (consumable):**

- **Create → DungeonBlade → Item → Consumable Item**
- Name: `Item_StaminaTonic`
- **Item Id** = `stamina_tonic`, **Display Name** = `Stamina Tonic`, **Description** = `Restores 50 Stamina.`
- **Stamina Amount** = `50`. All other fields = same pattern as Health Potion.

**3. Iron Sword (weapon):**

- **Create → DungeonBlade → Item → Weapon Item**
- Name: `Item_IronSword`
- **Item Id** = `iron_sword`, **Display Name** = `Iron Sword`, **Description** = `A sturdy blade. Reliable.`
- **Type** = `Weapon`, **Equip Slot** = `MainHand`, **Stackable** = ❌
- **Weapon Kind** = `Melee`, **Damage Bonus** = `0`
- **Sell Value** = `15`

**4. Iron Pistol (weapon):**

- **Create → DungeonBlade → Item → Weapon Item**
- Name: `Item_IronPistol`
- **Item Id** = `iron_pistol`, **Equip Slot** = `OffHand`, **Weapon Kind** = `Ranged`, **Stackable** = ❌

**5. Bone Fragment (material):**

- **Create → DungeonBlade → Item → Misc Item**
- Name: `Item_BoneFragment`
- **Item Id** = `bone_fragment`, **Display Name** = `Bone Fragment`, **Description** = `A jagged piece of skeleton bone. Crafting material.`
- **Type** = `Material`, **Stackable** = ✅, **Max Stack** = `99`, **Sell Value** = `2`

### 6b — Create the Item Database

1. Right-click in `Assets/_Project/Inventory/`. **Create → DungeonBlade → Item Database**. Name it `ItemDatabase`.
2. Open it. Set **Items** size = `5`. Drag in all 5 item assets from §6a (order doesn't matter).

### 6c — Inventory manager + persistence (one-time scene wiring)

In `3_Dungeon1.unity`:

1. Hierarchy → Create Empty at scene root → name `[Inventory]`.
2. Add Component → **`Inventory Manager`**. (Optional) Expand **Starting Items**, set Size = `2`. Each row has an `Item` field and a `Quantity` field side-by-side:
   - Element 0: Item = `Item_HealthPotion`, Quantity = `5`
   - Element 1: Item = `Item_BoneFragment`, Quantity = `10`
3. Add Component → **`Inventory Controller`**. (Leave Inventory Panel blank for now — we'll wire it after we build the UI.)
4. Add Component → **`Inventory Persistence`**. Drag your `ItemDatabase` asset into the **Database** field.
5. Add Component → **`Hotbar Binder`**. Drag the `Player` GameObject into **Player Ref**.
6. Add Component → **`Equipment Binder`**.
   - **Player Combat** → drag your `Player` GameObject (it has PlayerCombat).
   - **Main Hand Sword** → drag the existing `Sword` GameObject from `Player → CameraRig → WeaponHolder → Sword`.
   - **Off Hand Gun** → drag the existing `Gun` GameObject from the same parent.

### 6d — Build the Inventory UI

You'll build this as children of your existing `Canvas` (the one with the Crosshair). All sizes are recommendations — adjust to taste.

**Step 1: Build the slot prefab (one-time)**

1. Hierarchy → right-click `Canvas` → **UI → Image**. Name it `SlotWidget`.
2. Set Width/Height = `64`. Color = dark grey (`60, 60, 70, 220`).
3. Right-click `SlotWidget` → **UI → Image** child → name `Icon`. Anchor = stretch (Alt+Shift+Stretch). Margins all `6`. Source Image = leave empty.
4. Right-click `SlotWidget` → **UI → Text - TextMeshPro** child → name `Quantity`. Anchor = bottom-right. Position offset `(-4, 4)`. Font Size = `14`. Alignment = bottom-right. Set initial text = `1`.
5. Add Component → **`Slot Widget`** on the parent. Drag `SlotWidget` (itself) → Background field. Drag `Icon` → Icon Image. Drag `Quantity` → Quantity Text.
6. Drag `SlotWidget` from Hierarchy into `Assets/_Project/Inventory/UI/Prefabs/` to save as a prefab. Delete the scene copy.

**Step 2: Build the inventory panel**

1. Right-click `Canvas` → **UI → Panel**. Name `InventoryPanel`. Color = dark with alpha `~200`. Anchor = full-stretch (covers screen).
2. Inside `InventoryPanel`:
   - **GridParent** — Right-click → Create Empty → name `GridParent`. Anchor = middle-center. Pos X `0`, Pos Y `0`, Width `420`, Height `560`. Add Component → **Grid Layout Group**. Cell Size = X `64`, Y `64`. Spacing = X `4`, Y `4`. Constraint = `Fixed Column Count`, Constraint Count = `6`.
   - **EquipmentParent** — Right-click `InventoryPanel` → Create Empty → name `EquipmentParent`. Anchor = middle-center. Pos X `-280` (off to the left of GridParent), Pos Y `0`, Width `80`, Height `280`. Add Component → **Grid Layout Group**. Cell Size = X `64`, Y `64`. Spacing = X `0`, Y `4`. Constraint = `Fixed Column Count`, Constraint Count = `1`.
   - **HotbarParent** — anchor bottom-center. Pos X `0`, Pos Y `40`, Width `410`, Height `70`. Add Component → **Grid Layout Group**. Cell Size = X `64`, Y `64`. Spacing = X `4`, Y `0`. Constraint = `Fixed Column Count`, Constraint Count = `6`. **Move HotbarParent OUT of InventoryPanel** so it stays visible when the panel is closed — drag it in Hierarchy directly under `Canvas` (not under InventoryPanel).

3. **Drag Ghost** — the floating icon that follows the cursor during drag.
   1. Right-click `InventoryPanel` → **UI → Image** → name `DragGhost`.
   2. Rect Transform: Anchor = top-left (single click, no Alt/Shift). Pos X `0`, Pos Y `0`. Width `64`, Height `64`.
   3. Image component: leave **Source Image** empty (script assigns at runtime). Color = white (or alpha `200` for translucent ghost feel).
   4. **Uncheck `Raycast Target`** on the Image component — critical, or the ghost blocks `OnDrop` events on slots underneath.
   5. **Disable the GameObject** by unchecking the checkbox next to its name at the top of the Inspector. The script enables it only during a drag.

4. **Tooltip** — the floating panel showing item name + type + description on hover.
   1. Right-click `Canvas` (NOT InventoryPanel — tooltip stays alive after closing inventory) → **UI → Panel** → name `Tooltip`.
   2. Rect Transform: Anchor = top-left. Pos X `0`, Pos Y `0`. Width `260`, Height `120`. (The script repositions to the cursor at runtime.)
   3. Image component (default Panel background): Color = dark with alpha `~230` (e.g. `15, 15, 20, 230`).
   4. Add Component → **Vertical Layout Group**. Set Padding all = `8`, Spacing = `4`, Child Alignment = Upper Left, Child Force Expand Width = ✅, Child Force Expand Height = ❌.
   5. Add Component → **Content Size Fitter**. Vertical Fit = `Preferred Size` (so the tooltip auto-grows to fit the description).
   6. Now add the three text children — for **each one**: right-click `Tooltip` → **UI → Text - TextMeshPro**. Configure:
      - **Name** — Font Size `18`, Bold, Color white, initial text `"Item Name"`. Alignment = Left + Top.
      - **Type** — Font Size `12`, Italic, Color light grey (`180, 180, 200`), initial text `"Type • Slot"`. Alignment = Left + Top.
      - **Description** — Font Size `13`, Color soft white (`220, 220, 220`), initial text `"Item description goes here. May be multiple lines."`. Alignment = Left + Top.
      - On all three, **uncheck `Raycast Target`** (tooltips shouldn't intercept clicks).
      - On Description specifically, in the TMP component scroll to **Wrapping** and set **Word Wrapping = ✅**, **Overflow = `Overflow`** so multi-line descriptions wrap inside the tooltip.
   7. Add Component → **`Item Tooltip`** on the `Tooltip` panel itself. Wire fields:
      - **Root** → drag `Tooltip` (itself) — its RectTransform.
      - **Name Text** → drag the `Name` child.
      - **Type Text** → drag the `Type` child.
      - **Description Text** → drag the `Description` child.
   8. **Disable the `Tooltip` GameObject** by unchecking the checkbox next to its name at the top of the Inspector. The script enables it on hover.

   **End-of-step Tooltip Hierarchy:**

   ```
   Canvas
   └── Tooltip (disabled, has Item Tooltip + Vertical Layout Group + Content Size Fitter)
       ├── Name        (TMP, bold, 18pt)
       ├── Type        (TMP, italic, 12pt)
       └── Description (TMP, 13pt, word-wrapping)
   ```

**Step 3: Wire the InventoryUI script**

1. Add Component → **`Inventory UI`** on `InventoryPanel`.
2. Configure:
   - **Slot Prefab** → drag the `SlotWidget` prefab from Project window.
   - **Grid Parent** → `GridParent`.
   - **Hotbar Parent** → `HotbarParent`.
   - **Equipment Parent** → `EquipmentParent`.
   - **Drag Ghost** → the Drag Ghost Image you made.
   - **Tooltip** → the Tooltip panel's `ItemTooltip` component.
   - **Player Ref** → the `Player` GameObject.

3. Go back to `[Inventory]` GameObject → **Inventory Controller** component → drag `InventoryPanel` into the **Inventory Panel** field.

### 6e — First test

1. Save scene (`Ctrl+S`).
2. Press **Play**.
3. **Hotbar** (always visible at bottom) shows 6 empty slots.
4. Press **Tab** — inventory panel opens, cursor unlocks. Health Potions should appear in the grid (from Starting Items in §6c).
5. **Hover** over a slot with an item — tooltip appears with name + description.
6. **Drag** a Health Potion from the grid into a hotbar slot. It should swap/move.
7. **Close** the panel (Tab or Escape). Cursor relocks.
8. **Press `1`** with a Health Potion in hotbar slot 0 → potion is consumed, HP fills (Console: `[Consumable] Health Potion used → +50 HP, +0 stamina`).
9. **Drag the Iron Sword onto the MainHand equipment slot** → Console: `[Equipment] MainHand=yes, OffHand=no`. The scene Sword GameObject becomes active. Drag it back to grid → Sword disappears from the player's hand.
10. **Quit Play**, then re-enter. Your inventory state should persist (saved to `profile.json` on quit, loaded on next Start).

### 6f — Tuning + extending

| Goal                                                   | How                                                                                             |
| ------------------------------------------------------ | ----------------------------------------------------------------------------------------------- |
| Add a new item type                                    | Subclass `Item`, override `OnUse`, add `[CreateAssetMenu]`. Then add the asset to ItemDatabase. |
| Spawn loot from enemies (M8)                           | Call `InventoryManager.Instance.AddItem(item, qty)` on enemy death.                             |
| Disallow equipping a 2H weapon while OffHand is filled | Override `MoveOrSwap` logic in `InventoryManager` — current Phase 1 doesn't enforce it.         |
| Bigger grid                                            | Change `GridWidth` / `GridHeight` constants in `InventoryManager.cs`.                           |

## M7 — Bank + Shop NPCs (Lobby scene)

M7 lives in the **`2_Lobby` scene** — between dungeon runs the player visits two NPCs: a **Banker** (vault storage + gold deposit/withdraw) and a **Shopkeeper** (buy/sell). Both use F-to-interact with a prompt label, and reuse the inventory's drag/drop UI patterns.

Code lives under [Assets/\_Project/Bank/](Assets/_Project/Bank/):

**Data + logic**

- [Scripts/BankManager.cs](Assets/_Project/Bank/Scripts/BankManager.cs) — singleton, 6×8 vault grid + stored gold, events.
- [Scripts/PlayerWallet.cs](Assets/_Project/Bank/Scripts/PlayerWallet.cs) — singleton holding "pocket gold" (separate from bank gold). Defaults to 100 starting gold.
- [Scripts/ShopDefinition.cs](Assets/_Project/Bank/Scripts/ShopDefinition.cs) — ScriptableObject defining a shop's stock list + sell-price multiplier.
- [Scripts/ShopManager.cs](Assets/_Project/Bank/Scripts/ShopManager.cs) — singleton, opens/closes shops, validates buy/sell.
- [Scripts/Interactable.cs](Assets/_Project/Bank/Scripts/Interactable.cs) + [BankNPC.cs](Assets/_Project/Bank/Scripts/BankNPC.cs) + [ShopNPC.cs](Assets/_Project/Bank/Scripts/ShopNPC.cs) — interactable NPC components.
- [Scripts/PlayerInteractor.cs](Assets/_Project/Bank/Scripts/PlayerInteractor.cs) — finds nearest interactable in front of player camera; F triggers it.

**UI**

- [UI/BankController.cs](Assets/_Project/Bank/UI/BankController.cs) + [ShopController.cs](Assets/_Project/Bank/UI/ShopController.cs) — open/close panels, push/pop `MenuState`.
- [UI/BankUI.cs](Assets/_Project/Bank/UI/BankUI.cs) — builds the bank grid, deposit/withdraw buttons, gold readout.
- [UI/BankSlotWidget.cs](Assets/_Project/Bank/UI/BankSlotWidget.cs) — same drag/drop pattern as inventory SlotWidget but hits BankManager.
- [UI/ShopUI.cs](Assets/_Project/Bank/UI/ShopUI.cs) — builds the stock entry list.
- [UI/ShopStockEntry.cs](Assets/_Project/Bank/UI/ShopStockEntry.cs) — single buy row with icon + name + price + Buy button.
- [UI/ShopSellZone.cs](Assets/_Project/Bank/UI/ShopSellZone.cs) — drop zone where you drag inventory items to sell.

**Cross-cutting changes**

- [MenuState.cs](Assets/_Project/Core/GameManager/MenuState.cs) — shared global "any menu open?" gate. Inventory, Bank, and Shop all push/pop this. `PlayerMovement`, `PlayerCombat`, `HotbarBinder`, `PlayerInteractor` all check it instead of calling each controller individually.
- [DragRouter.cs](Assets/_Project/Inventory/UI/DragRouter.cs) — small static state for cross-panel drag. Lets you drag from Inventory → Bank, Bank → Inventory, Inventory → Shop sell zone.
- [InventoryPersistence.cs](Assets/_Project/Inventory/Scripts/InventoryPersistence.cs) — now also saves/loads BankManager state and PlayerWallet gold.
- [BankData.cs](Assets/_Project/Core/SaveSystem/BankData.cs) — switched to use the same `SerializedSlot` format as inventory.

### 7a — Create the ShopDefinition asset (one-time)

1. Right-click in `Assets/_Project/Bank/` → **Create → DungeonBlade → Shop Definition**. Name it `Shop_LobbyShopkeeper`.
2. Configure:
   - **Shop Name** = `Lobby Shopkeeper`
   - **Sell Price Multiplier** = `1.0` (full SellValue when selling)
   - **Stock List** size = `4`. Add:
     | # | Item | Quantity | Price Override |
     |---|---|---|---|
     | 0 | `Item_HealthPotion` | 1 | 0 (uses BuyValue 25) |
     | 1 | `Item_StaminaTonic` | 1 | 0 |
     | 2 | `Item_IronSword` | 1 | 0 |
     | 3 | `Item_IronPistol` | 1 | 0 |

   _Quantity is per-buy (always 1 for now). PriceOverride = 0 means "use the item's default BuyValue."_

### 7b — Lobby scene wiring

Open `Assets/Scenes/2_Lobby.unity`.

#### Step 1: Player setup

The Lobby scene needs the **same Player** (with PlayerStats, PlayerMovement, PlayerCombat, etc.) as the Dungeon. Easiest path: copy your Player from `3_Dungeon1.unity` into `2_Lobby.unity`.

**Sub-step 1a — Copy the Player from Dungeon to Lobby:**

1. Open `3_Dungeon1.unity`. In Hierarchy, select `Player`.
2. **Right-click → Copy** (or `Ctrl+C`).
3. **Open `2_Lobby.unity`** (double-click in Project window).
4. In the Lobby's Hierarchy, **right-click empty space → Paste** (or `Ctrl+V`).
5. The Player + all children (CameraRig, Main Camera, WeaponHolder, Sword, Gun) appears.

**Sub-step 1b — Position the Player:**

1. Select the pasted `Player`. Inspector → **Transform → Position**: set to a sensible spawn spot in the lobby (e.g. `(0, 1, 0)` if your lobby floor is at y=0).
2. The Player should be standing on the floor of the Lobby — drag it visually if it's floating or clipping.

**Sub-step 1c — Delete the default `Main Camera` at scene root:**
The default Lobby scene has a `Main Camera` GameObject at the root. Your Player has its own camera now (under CameraRig), so the root one is redundant and will conflict.

1. In Hierarchy, find the top-level **`Main Camera`** (NOT the one under your Player).
2. Right-click → **Delete**.
3. Verify: in the Hierarchy search bar, type `t:Camera` — only ONE result should remain (the player's camera under CameraRig).

**Sub-step 1d — Verify the Player's camera is tagged `MainCamera`:**

1. Select `Main Camera` under `Player → CameraRig`.
2. Top of Inspector → **Tag** dropdown should say `MainCamera`. If not, set it.

**Sub-step 1e — Verify Player tag and components:**

1. Select `Player`. Inspector top:
   - **Tag** = `Player` ✅
   - **Layer** = `Default` ✅
2. Inspector should show all these components (from M1–M6):
   - Transform
   - Character Controller
   - Player Stats
   - Player Movement
   - Player Combat
   - Combo System
   - Respawn Manager (from M3)
3. If anything is missing, the Player wasn't fully set up in 3_Dungeon1.unity — go back and fix there first, then re-copy.

**Sub-step 1f — Add EventSystem (if missing):**
UI input (mouse clicks, drag-drop) requires an EventSystem in the scene.

1. In Hierarchy, look for a top-level `EventSystem` GameObject.
2. **If missing**: right-click empty space → **UI → Event System**. Unity creates one with sensible defaults.

**Sub-step 1g — Set up the Lobby Canvas:**
The Player's Crosshair was probably a child of a Canvas in the Dungeon. Check if a Canvas was pasted with the Player:

1. If **`Canvas`** appears at scene root with the Crosshair as a child → **keep it**.
2. If **no Canvas** appeared → create one: right-click Hierarchy → **UI → Canvas**. Configure Canvas Scaler:
   - **UI Scale Mode** = `Scale With Screen Size`
   - **Reference Resolution** = `1920 × 1080`
   - **Match** = `0.5`

You'll add the Lobby's UI panels (HotbarParent, InventoryPanel, BankPanel, ShopPanel, Tooltip) under this Canvas in §7e–§7g.

---

#### Step 2: Add scene-wide system GameObjects

The Lobby needs the same `[Inventory]` system root as the Dungeon — plus a new `[Bank]` root for the bank/shop systems.

**Sub-step 2a — Create `[Inventory]`:**

1. Hierarchy → right-click empty space (scene root, NOT inside another GameObject) → **Create Empty**.
2. Name it `[Inventory]` (the brackets are convention to flag "scene-wide manager, not a real game object").
3. Set Transform Position to `(0, 0, 0)` (managers don't need a real position).

**Sub-step 2b — Add 5 components to `[Inventory]`:**

With `[Inventory]` selected, click **Add Component** in Inspector for each:

1. **`Inventory Manager`**
   - **Starting Items** — set Size = `2` and add:
     - Element 0: `Item_HealthPotion`, Quantity = `5`
     - Element 1: `Item_BoneFragment`, Quantity = `10`
   - (Or leave empty if you want to start fresh — items will load from save file anyway)

2. **`Inventory Controller`**
   - **Inventory Panel** — leave empty for now. You'll drag the `InventoryPanel` GameObject into this field after building the Inventory UI in §7e–7g (or recreate the M6 UI in this scene).

3. **`Inventory Persistence`**
   - **Database** — drag your `ItemDatabase` asset (from `Assets/_Project/Inventory/ItemDatabase.asset`) into this field.
   - **Load On Start** = ✅
   - **Save On Quit** = ✅

4. **`Hotbar Binder`**
   - **Player Ref** — drag the `Player` GameObject from Hierarchy into this field.

5. **`Equipment Binder`**
   - **Player Combat** — drag `Player` (it has the PlayerCombat component).
   - **Main Hand Sword** — drag the `Sword` child from `Player → CameraRig → WeaponHolder → Sword`.
   - **Off Hand Gun** — drag the `Gun` child from the same parent.

**Sub-step 2c — Create `[Bank]`:**

1. Hierarchy → right-click scene root → **Create Empty**.
2. Name it `[Bank]`.
3. Position `(0, 0, 0)`.

**Sub-step 2d — Add 5 components to `[Bank]`:**

With `[Bank]` selected, **Add Component** five times:

1. **`Player Wallet`**
   - **Starting Gold** = `100` (gives you cash to test buying immediately).

2. **`Bank Manager`** — no fields to configure.

3. **`Shop Manager`** — no fields to configure.

4. **`Bank Controller`**
   - **Bank Panel** — leave empty. You'll drag the `BankPanel` GameObject in after §7f.

5. **`Shop Controller`**
   - **Shop Panel** — leave empty. You'll drag the `ShopPanel` GameObject in after §7g.

---

**End of Step 2 Hierarchy check:**

```
2_Lobby (scene)
├── Directional Light
├── Player                          ← from Dungeon, with all M1-M6 components
│   └── CameraRig
│       ├── Main Camera             ← only camera, tagged MainCamera
│       └── WeaponHolder
│           ├── Sword
│           └── Gun
├── Canvas                          ← from M6 or newly created
│   ├── Crosshair                   (optional in Lobby)
│   └── (UI panels added in §7e–7g)
├── EventSystem
├── [Inventory]                     ← 5 components
├── [Bank]                          ← 5 components
└── (Lobby geometry — floors, walls, etc.)
```

Save the scene before continuing (`Ctrl+S`).

Hierarchy → Create Empty → name `[Bank]`. Add:

- **Player Wallet** — default Starting Gold = `100`.
- **Bank Manager**
- **Shop Manager**
- **Bank Controller** (drag BankPanel later)
- **Shop Controller** (drag ShopPanel later)

#### Step 3: Add PlayerInteractor to Player

Select `Player` → Add Component → **Player Interactor**. Configure:

- **Interact Range** = `2.5`
- **Interact Mask** = `Default` (or whatever layer the NPCs use)
- **Look Camera** — drag your `Main Camera` (under `Player → CameraRig`)
- **Prompt Label** — leave for now; we'll wire it after building UI.

### 7c — Build the Banker NPC

The Banker is a stationary capsule the player walks up to. The default Capsule mesh is fine for Phase 1 — we'll polish visuals in M9.

**Sub-step 7c-1: Create the GameObject**

1. In Hierarchy → right-click empty space (scene root, not under any other object) → **3D Object → Capsule**.
2. **Rename** the new Capsule to `Banker`.
3. **Position** it: select Banker → Inspector → Transform → **Position** = something visible from the player spawn, e.g. `(3, 1, 5)`. Adjust Y if your Lobby floor isn't at y=0.
4. Make sure the Capsule sits **on top of the floor**, not sunk into it. The default Capsule has Height 2 with Center at Y=0, so a Position Y of `1` puts the bottom at floor level.

**Sub-step 7c-2: Create the Banker material**

1. In Project window, navigate to `Assets/_Project/Materials/` (create the folder if missing).
2. Right-click → **Create → Material**. Name it `M_Banker`.
3. Click `M_Banker` to select it. In Inspector → click the **Base Map** color swatch.
4. Set RGB to `80, 110, 160` (a calm blue-grey suggestive of "banker / official").
5. Drag `M_Banker` from Project window onto the `Banker` capsule in Hierarchy. The capsule turns blue-grey.

**Sub-step 7c-3: Set up the collider**

1. With Banker selected, in Inspector look at the existing **Capsule Collider** (Unity adds one by default with the Capsule mesh).
2. **DELETE** the Capsule Collider — click the 3-dot menu (⋮) on the component → **Remove Component**.
3. Add Component → **Sphere Collider**. Configure:
   - **Is Trigger** = ❌ unchecked (player should physically bump into the NPC, not walk through)
   - **Radius** = `1.0`
   - **Center** = `(0, 1, 0)` (centers the sphere at the capsule's middle, not at its feet)

Why swap to a Sphere Collider: simpler interaction range, more forgiving than a tight capsule for "walk near the NPC to interact."

**Sub-step 7c-4: Add the BankNPC script**

1. With Banker selected → **Add Component** → search `Bank NPC` → click.
2. In the new component, set **Prompt Text** = `Press [F] to access Bank`.

The script extends `Interactable`. When the player's `PlayerInteractor` finds Banker in range and the player presses F, the BankNPC script tells the BankController to open the Bank UI.

**Sub-step 7c-5: Optional — stop the Banker from getting pushed around**
Without a Rigidbody the Capsule is static, but if you ever add one for animations later, set Rigidbody → **Is Kinematic** = ✅ so the player can't shove the NPC across the floor. Skip for Phase 1.

---

### 7d — Build the Shopkeeper NPC

Same exact workflow as the Banker, just different placement and color so you can tell them apart at a glance.

**Sub-step 7d-1: Create the GameObject**

1. Hierarchy → right-click scene root → **3D Object → Capsule**. Rename to `Shopkeeper`.
2. **Position** it at least 3 units away from the Banker, e.g. `(-3, 1, 5)` or `(3, 1, -5)`. Player should be able to walk between them without overlapping interact ranges.

**Sub-step 7d-2: Create the Shopkeeper material**

1. Project window → `Assets/_Project/Materials/` → right-click → **Create → Material** → name `M_Shopkeeper`.
2. Set Base Map color to `160, 90, 60` (warm orange/red — "merchant / trade").
3. Drag onto the Shopkeeper capsule.

**Sub-step 7d-3: Set up the collider**

1. **Remove** the default Capsule Collider.
2. **Add Component → Sphere Collider**. **Is Trigger** unchecked, **Radius** `1.0`, **Center** `(0, 1, 0)`.

**Sub-step 7d-4: Add the ShopNPC script**

1. **Add Component → Shop NPC**.
2. Configure:
   - **Shop** → drag `Shop_LobbyShopkeeper` (from `Assets/_Project/Bank/`) into this field. **This is required — without it the NPC will warn `[ShopNPC] No ShopDefinition assigned.` when you press F.**
   - **Prompt Text** = `Press [F] to Shop`.

---

### 7e — Build the prompt UI label

The prompt is a small text label near the bottom of the screen that appears whenever the player is within range of an Interactable. The script (`PlayerInteractor`) auto-shows/hides it.

**Sub-step 7e-1: Verify Canvas exists**
You should already have a Canvas in the Lobby (from §7b Step 1g). If not:

1. Right-click Hierarchy → **UI → Canvas**.
2. Configure Canvas Scaler:
   - **UI Scale Mode** = `Scale With Screen Size`
   - **Reference Resolution** = `1920 × 1080`
   - **Match** = `0.5`

**Sub-step 7e-2: Create the prompt text**

1. Right-click `Canvas` → **UI → Text - TextMeshPro**. Name it `InteractPrompt`.
2. (If TMP Importer dialog appears, click **Import TMP Essentials** and skip the second button.)

**Sub-step 7e-3: Configure Rect Transform**
With InteractPrompt selected, set Rect Transform values:

1. Click the **anchor preset square** → click **bottom-center** (middle column, bottom row of the 9-cell grid).
2. **Pos X** = `0` (centered horizontally)
3. **Pos Y** = `120` (120 pixels up from screen bottom — sits above the hotbar)
4. **Width** = `320`, **Height** = `40`

**Sub-step 7e-4: Configure the TextMeshPro component**

1. **Text Input** (top of TMP component): type `Press [F] to interact` — placeholder text. The script overwrites it at runtime with each NPC's specific prompt.
2. **Font Size** = `24`
3. **Font Style** → click **Bold** (B button)
4. **Vertex Color** → click swatch → set white (`255, 255, 255, 255`)
5. **Alignment**: click center-horizontal + center-vertical (icons are in a small grid below the font controls).

**Sub-step 7e-5: Optional — add a subtle outline so the text reads on light backgrounds**

1. In TMP component → expand `Material Preset` → enable **Outline**.
2. Color = black, Thickness = `0.15`.

**Sub-step 7e-6: Disable by default**

1. With InteractPrompt selected, **uncheck the checkbox next to its name** at the very top of the Inspector.
2. The label should appear greyed out in Hierarchy. The PlayerInteractor script enables it only when the player is near an NPC.

**Sub-step 7e-7: Wire it to PlayerInteractor**

1. Select `Player` in Hierarchy.
2. In Inspector, find the **Player Interactor** component (added in §7b Step 3).
3. Find the **Prompt Label** field.
4. Drag `InteractPrompt` from Hierarchy → drop onto **Prompt Label** field.

---

**Quick test (you can do this now without finishing 7f and 7g):**

1. Save scene (`Ctrl+S`).
2. Press **Play**.
3. Walk toward the Banker capsule. As you get within ~2.5m, the prompt at the bottom of the screen should change to `Press [F] to access Bank`.
4. Walk toward the Shopkeeper. Prompt should switch to `Press [F] to Shop`.
5. Walk away from both. Prompt should hide.
6. **Pressing F** at this point will throw a warning in Console (`No BankController in scene.` or `No ShopController in scene.`) — that's expected. The prompt detection works; the actual UI panels come in §7f and §7g.

If the prompt never appears:

- Check `PlayerInteractor`'s **Look Camera** field — should be the Player's Main Camera.
- Check `PlayerInteractor`'s **Interact Range** = `2.5` (you might need to walk closer than you think).
- Check both NPCs have their `BankNPC` / `ShopNPC` script with a non-empty Prompt Text.
- Check the InteractPrompt is enabled at scene root (it starts disabled by default — enable it manually first to test).

If only one NPC's prompt fires:

- The other might not have a collider, or its collider is tiny/misplaced.

If pressing F does nothing while the prompt IS visible:

- The Player tag isn't `Player`, OR the `Interact` input action isn't bound to F. Check `PlayerInputActions.cs` — `Interact = ... <Keyboard>/f`.

### 7f — Build the Bank UI

Same workflow as Inventory UI in M6. Reuse the `SlotWidget` prefab from `Assets/_Project/Inventory/UI/Prefabs/SlotWidget.prefab` for visual consistency, BUT we'll use a **separate** widget script for bank slots (`BankSlotWidget`).

**Quickest path:** create a duplicate prefab.

**Sub-step 7f-prep-1: Duplicate the SlotWidget prefab**

1. Open the **Project window**.
2. Navigate to `Assets/_Project/Inventory/UI/Prefabs/`.
3. **Right-click `SlotWidget.prefab`** → **Duplicate** (or `Ctrl+D` while it's selected).
4. A new file appears next to it called `SlotWidget 1.prefab`.
5. **Rename it** to `BankSlotWidget` (single-click the name to edit, type `BankSlotWidget`, press Enter).

**Sub-step 7f-prep-2: Create the destination folder**

1. Navigate to `Assets/_Project/Bank/UI/`.
2. If a `Prefabs` folder doesn't exist, right-click empty space → **Create → Folder** → name it `Prefabs`.
3. Drag `BankSlotWidget.prefab` from `Inventory/UI/Prefabs/` to `Bank/UI/Prefabs/`.

**Sub-step 7f-prep-3: Open the duplicated prefab in edit mode**

1. **Double-click `BankSlotWidget.prefab`** in the Project window.
2. Unity enters **prefab edit mode** — the Hierarchy now shows ONLY the prefab's contents (a small `BankSlotWidget` parent with `Icon` and `Quantity` children). The scene is greyed out behind a blue bar at the top.

**Sub-step 7f-prep-4: Swap the script**

1. In the prefab editor's Hierarchy, click the prefab root **`BankSlotWidget`**.
2. In the Inspector, find the **Slot Widget (Script)** component (it carried over from the duplicate).
3. Click the **3-dot menu (⋮)** on that component → **Remove Component**.
4. With the prefab root still selected, click **Add Component** → search `Bank Slot Widget` → click it.
5. The new component appears with empty fields (Background, Icon Image, Quantity Text — same field names as before).

**Sub-step 7f-prep-5: Re-wire the three references**

1. **Background** — drag the prefab root **`BankSlotWidget`** itself (the parent in the prefab Hierarchy) → drop onto the Background field. Unity auto-grabs its Image component.
2. **Icon Image** — drag the **`Icon`** child → drop onto the Icon Image field.
3. **Quantity Text** — drag the **`Quantity`** child → drop onto the Quantity Text field.

**Sub-step 7f-prep-6: (Optional) Tint the bank slot a different color**
The `BankSlotWidget` script defaults to greenish empty/filled colors (so visually it differs from the inventory grid's grey). You can leave defaults or customize:

- **Empty Color** = `0.15, 0.18, 0.15, 0.85` (default)
- **Filled Color** = `0.25, 0.32, 0.25, 0.95` (default)
- **Hover Color** = `0.4, 0.55, 0.4, 1` (default)

**Sub-step 7f-prep-7: Exit prefab edit mode**

1. Click the **`<`** (back arrow) at the top-left of the Hierarchy, OR
2. Click anywhere outside the prefab edit blue bar.
3. Unity returns you to your Lobby scene.

**Sub-step 7f-prep-8: Verify the prefab saved**

1. In Project window, click `BankSlotWidget.prefab`.
2. Inspector should show its components in read-only preview: Image, BankSlotWidget script (with all 3 fields filled), and the two children (Icon, Quantity) collapsed.
3. If the BankSlotWidget script's fields show `None` instead of references → re-open the prefab and redo Sub-step 7f-prep-5.

You now have two slot prefabs:

- `Assets/_Project/Inventory/UI/Prefabs/SlotWidget.prefab` — used by Inventory grid + hotbar + equipment
- `Assets/_Project/Bank/UI/Prefabs/BankSlotWidget.prefab` — used by the Bank vault grid

Different scripts, same visual layout.

**Now the panel:**

1. Right-click `Canvas` → **UI → Panel**. Name `BankPanel`. Stretch full-screen, color dark with alpha `~200`. **Disable** by default.
   **Sub-step 7f-2-1: Build GridParent (the 6×8 vault grid container)**

The script will spawn 48 BankSlotWidgets as children of this GameObject at runtime.

1. In Hierarchy, **right-click `BankPanel`** → **Create Empty**.
2. Rename the new GameObject to `GridParent`.
3. Inspector → **Rect Transform**:
   - Click the **anchor preset square** → click **middle-center** (center cell of 9-cell grid). No Alt/Shift.
   - **Pos X** = `0`
   - **Pos Y** = `0`
   - **Width** = `420`
   - **Height** = `560`
4. **Add Component → Grid Layout Group**. Configure:
   - **Padding** all = `0`
   - **Cell Size** = X `64`, Y `64`
   - **Spacing** = X `4`, Y `4`
   - **Start Corner** = `Upper Left`
   - **Start Axis** = `Horizontal`
   - **Child Alignment** = `Upper Left`
   - **Constraint** = `Fixed Column Count`
   - **Constraint Count** = `6`

GridParent stays empty in edit mode — slots are spawned at runtime.

---

**Sub-step 7f-2-2: Build GoldPanel (deposit/withdraw UI)**

GoldPanel holds 2 readout texts + 2 input fields + 2 buttons, stacked vertically.

1. **Right-click `BankPanel`** → **Create Empty**. Rename to `GoldPanel`.
2. Inspector → **Rect Transform**:
   - Anchor preset → click **top-center** (top middle cell of 9-cell grid).
   - **Pos X** = `0`
   - **Pos Y** = `-60` (negative Y = below the top edge by 60 pixels)
   - **Width** = `400`, **Height** = `260`
3. **Add Component → Vertical Layout Group**. Configure:
   - **Padding** all = `8`
   - **Spacing** = `4`
   - **Child Alignment** = `Upper Center`
   - **Child Force Expand** Width = ✅, Height = ❌
   - **Child Control Size** Width = ✅, Height = ✅

Now add the 6 children inside GoldPanel:

**(a) PocketGoldText:**

1. Right-click `GoldPanel` → **UI → Text - TextMeshPro**. Rename `PocketGoldText`.
2. Text input: `Pocket: 100g`. Font Size `18`, Bold, Color white, Alignment center.

**(b) VaultGoldText:**

1. Right-click `GoldPanel` → **UI → Text - TextMeshPro**. Rename `VaultGoldText`.
2. Text input: `Vault: 0g`. Font Size `18`, Bold, Color white, Alignment center.

**(c) DepositInput:**

1. Right-click `GoldPanel` → **UI → Input Field - TextMeshPro**. Rename `DepositInput`.
   - (If TMP Importer dialog appears the first time, click Import TMP Essentials.)
2. With DepositInput selected, find the **TMP_InputField** component → **Content Type** = `Integer Number` (so users can only type digits).
3. Inside DepositInput in Hierarchy, find the `Placeholder` child. Click it. In its TMP Text component, change Text Input to `Amount to deposit`.

**(d) DepositButton:**

1. Right-click `GoldPanel` → **UI → Button - TextMeshPro**. Rename `DepositButton`.
2. Inside DepositButton, click the `Text (TMP)` child → change Text Input to `Deposit`.

**(e) WithdrawInput:**

1. Right-click `GoldPanel` → **UI → Input Field - TextMeshPro**. Rename `WithdrawInput`.
2. **Content Type** = `Integer Number`.
3. Placeholder text: `Amount to withdraw`.

**(f) WithdrawButton:**

1. Right-click `GoldPanel` → **UI → Button - TextMeshPro**. Rename `WithdrawButton`.
2. Text child: `Withdraw`.

Hierarchy now:

```
BankPanel
├── GridParent
└── GoldPanel
    ├── PocketGoldText
    ├── VaultGoldText
    ├── DepositInput (with Placeholder + Text children)
    ├── DepositButton (with Text child)
    ├── WithdrawInput
    └── WithdrawButton
```

---

**Sub-step 7f-2-3: Build CloseButton**

A "Close (Esc)" button in the top-right corner of the panel.

1. Right-click `BankPanel` → **UI → Button - TextMeshPro**. Rename `CloseButton`.
2. Inspector → **Rect Transform**:
   - Anchor preset → **top-right** (top row, right cell — single corner, NOT stretch).
   - **Pos X** = `-80`, **Pos Y** = `-30`
   - **Width** = `120`, **Height** = `40`
3. Inside CloseButton, click the `Text (TMP)` child → change Text Input to `Close (Esc)`.

---

**Sub-step 7f-2-4: Build DragGhost**

The floating icon that follows the cursor during drag.

1. Right-click `BankPanel` → **UI → Image**. Rename `DragGhost`.
2. Rect Transform:
   - Anchor → **top-left** (top row, left cell).
   - **Pos X** = `0`, **Pos Y** = `0`. **Width** = `64`, **Height** = `64`.
3. Image component:
   - **Source Image** = leave empty.
   - Color = white, alpha `200`.
   - **Raycast Target** = ❌ unchecked (so the ghost doesn't block drop events on slots beneath it).
4. **Disable** the GameObject — uncheck the box at top of Inspector.

---

**Sub-step 7f-2-5: End-of-step Hierarchy check**

```
BankPanel (disabled)
├── GridParent
├── GoldPanel
│   ├── PocketGoldText
│   ├── VaultGoldText
│   ├── DepositInput
│   ├── DepositButton
│   ├── WithdrawInput
│   └── WithdrawButton
├── CloseButton
└── DragGhost (disabled)
```

---

**Sub-step 7f-3: Add the BankUI script and wire its 7 fields**

1. Click `BankPanel` in Hierarchy.
2. **Add Component → Bank UI**.
3. The component shows 7 fields. Wire each:

| Field                | What to drag                     | From                                                                                                     |
| -------------------- | -------------------------------- | -------------------------------------------------------------------------------------------------------- |
| **Slot Prefab**      | `BankSlotWidget.prefab`          | Project window (`Assets/_Project/Bank/UI/Prefabs/`)                                                      |
| **Grid Parent**      | `GridParent` GameObject          | Hierarchy (child of BankPanel)                                                                           |
| **Drag Ghost**       | `DragGhost` Image                | Hierarchy (child of BankPanel)                                                                           |
| **Tooltip**          | the same `Tooltip` panel from M6 | Hierarchy (child of Canvas, top level) — this drags its `Item Tooltip` component reference automatically |
| **Pocket Gold Text** | `PocketGoldText`                 | Hierarchy (under GoldPanel)                                                                              |
| **Vault Gold Text**  | `VaultGoldText`                  | Hierarchy (under GoldPanel)                                                                              |
| **Deposit Input**    | `DepositInput`                   | Hierarchy (under GoldPanel)                                                                              |
| **Withdraw Input**   | `WithdrawInput`                  | Hierarchy (under GoldPanel)                                                                              |

If the Tooltip from M6 doesn't exist in this scene yet — create one (same setup as §6d step 4 / Tooltip block in M6). The bank reuses it; no need for a separate bank tooltip.

---

**Sub-step 7f-4: Wire the button OnClick events**

For each button, the workflow is the same:

1. Click the button in Hierarchy (e.g. `DepositButton`).
2. Inspector → **Button** component → scroll to **On Click ()** at the bottom.
3. Click the **`+`** button → a new event row appears.
4. Drag `BankPanel` from Hierarchy → drop onto the **runtime object slot** (left side of the row, currently `None (Object)`).
5. Click the **function dropdown** (currently says `No Function`) → navigate to **`BankUI` → `OnDepositPressed ()`** for DepositButton.

Repeat for the other two buttons:

| Button             | Function to select             |
| ------------------ | ------------------------------ |
| **DepositButton**  | `BankUI → OnDepositPressed()`  |
| **WithdrawButton** | `BankUI → OnWithdrawPressed()` |
| **CloseButton**    | `BankUI → OnClosePressed()`    |

Common gotcha: if you scroll to "Dynamic" or "Static" sections and only see weird names — make sure `BankPanel` is the one assigned in the runtime object slot. The function dropdown only shows methods on whatever object you assigned.

---

**Sub-step 7f-5: Wire BankPanel to BankController**

1. Select `[Bank]` GameObject in Hierarchy.
2. Inspector → **Bank Controller** component.
3. Find the **Bank Panel** field (currently `None (Game Object)`).
4. Drag `BankPanel` from Hierarchy → drop onto this field.

Now the BankNPC.OnInteract → BankController.Open() → bankPanel.SetActive(true) chain is complete.

---

**Sub-step 7f-6: Quick test (just the bank, no shop yet)**

1. Save scene (`Ctrl+S`).
2. Press **Play**.
3. Walk up to the Banker. Prompt appears.
4. Press **F**. Bank panel should appear with:
   - 48 empty bank slots in a 6×8 grid (centered)
   - GoldPanel showing `Pocket: 100g`, `Vault: 0g`, two input fields, two buttons
   - Close button in top-right
5. Type `30` in DepositInput → click **Deposit**. Console: `[Bank] Deposited 30g. Vault: 30g.` Pocket → `70g`, Vault → `30g`.
6. Type `10` in WithdrawInput → click **Withdraw**. Console: `[Bank] Withdrew 10g. Vault: 20g.` Pocket → `80g`, Vault → `20g`.
7. Click **Close (Esc)** → panel disappears. Cursor relocks. You can move again.

If something breaks:

- **Bank panel doesn't open on F** → BankController's Bank Panel field is empty (Sub-step 7f-5).
- **Slots don't appear in the grid** → Slot Prefab field on BankUI is empty (Sub-step 7f-3).
- **Buttons do nothing when clicked** → On Click event not wired (Sub-step 7f-4).
- **Pocket/Vault text doesn't update** → those text fields not wired on BankUI.

### 7g — Build the Shop UI

The shop UI has 5 things on screen:

- **Header** (top): shop name, e.g. "Lobby Shopkeeper"
- **PocketGoldText** (top-right): live readout of player's pocket gold
- **StockParent** (middle): the buyable items list — auto-populated at runtime from the ShopDefinition's Stock List
- **SellZone** (bottom): drop zone for selling items from inventory
- **CloseButton** (top-right corner): exits the shop

The script auto-spawns rows under StockParent based on what's in the assigned ShopDefinition. You don't pre-create stock rows in edit mode.

---

**Sub-step 7g-1: Create the ShopPanel**

1. In Hierarchy → **right-click `Canvas`** → **UI → Panel**. Rename to `ShopPanel`.
2. The default Panel comes with a stretched anchor and a semi-transparent grey Image — that's fine.
3. Inspector → **Image** component → click the Color swatch → set RGBA `15, 15, 25, 200` (dark navy backdrop).
4. **Disable the GameObject** — uncheck the box at the top of the Inspector. The `ShopController` will enable it when the player presses F at the Shopkeeper.

---

**Sub-step 7g-2: Add the Header (shop name display)**

1. Right-click `ShopPanel` → **UI → Text - TextMeshPro**. Rename `Header`.
2. Rect Transform:
   - Anchor preset → **top-center** (single click).
   - **Pos X** = `0`, **Pos Y** = `-40`
   - **Width** = `600`, **Height** = `60`
3. TMP component:
   - Text Input = `(Shop Name)` (placeholder; script overwrites at runtime)
   - Font Size = `32`, Bold, Color white
   - Alignment = center / center

---

**Sub-step 7g-3: Add the PocketGoldText readout**

1. Right-click `ShopPanel` → **UI → Text - TextMeshPro**. Rename `PocketGoldText`.
2. Rect Transform:
   - Anchor preset → **top-right** (top row, right cell).
   - **Pos X** = `-180`, **Pos Y** = `-30`
   - **Width** = `200`, **Height** = `40`
3. TMP component:
   - Text Input = `Gold: 100g` (placeholder)
   - Font Size = `20`, Bold, Color = warm yellow (`240, 200, 80`)
   - Alignment = right / center

---

**Sub-step 7g-4: Add StockParent (the buy list container)**

This is the empty container the ShopUI script populates with stock entries at runtime.

1. Right-click `ShopPanel` → **Create Empty** (NOT Image — just an empty RectTransform). Rename `StockParent`.
2. Rect Transform:
   - Anchor preset → **middle-center**.
   - **Pos X** = `0`, **Pos Y** = `30`
   - **Width** = `460`, **Height** = `400`
3. **Add Component → Vertical Layout Group**:
   - Padding all = `8`
   - Spacing = `6`
   - Child Alignment = `Upper Center`
   - Child Force Expand → Width = ✅, Height = ❌
   - Child Control Size → Width = ✅, Height = ✅
4. **Add Component → Content Size Fitter**:
   - Horizontal Fit = `Unconstrained`
   - Vertical Fit = `Preferred Size`

The Content Size Fitter auto-grows StockParent based on how many stock rows the script spawns. If the shop has 4 items, 4 rows. If 10, 10 rows.

---

**Sub-step 7g-5: Add SellZone (drop area for selling)**

1. Right-click `ShopPanel` → **UI → Image**. Rename `SellZone`.
2. Rect Transform:
   - Anchor preset → **bottom-center**.
   - **Pos X** = `0`, **Pos Y** = `100`
   - **Width** = `360`, **Height** = `100`
3. Image component:
   - Source Image = leave default (white square).
   - Color = RGBA `100, 50, 50, 180` (dim red, "danger / sell" mood).
4. **Add Component → Shop Sell Zone** (the script you'll find under DungeonBlade.Bank.UI).
5. Add a child label:
   - Right-click `SellZone` → **UI → Text - TextMeshPro**. Rename `Label`.
   - Anchor = stretch / stretch (Alt+Shift+click bottom-right of 9-cell grid). All margins `0`.
   - TMP Text Input = `Drag items here to sell`.
   - Font Size = `18`, Bold, Color = white.
   - Alignment = center / center.
   - **Uncheck Raycast Target** on the Label's TMP component (so the label doesn't block drop events on the SellZone parent).

---

**Sub-step 7g-6: Add CloseButton**

1. Right-click `ShopPanel` → **UI → Button - TextMeshPro**. Rename `CloseButton`.
2. Rect Transform:
   - Anchor preset → **top-right**.
   - **Pos X** = `-80`, **Pos Y** = `-30`
   - **Width** = `120`, **Height** = `40`
3. Inside CloseButton, click the `Text (TMP)` child → change Text Input to `Close (Esc)`.

---

**End-of-step Hierarchy check:**

```
ShopPanel (disabled)
├── Header
├── PocketGoldText
├── StockParent              (empty in edit mode; populated at runtime)
├── SellZone
│   └── Label                ("Drag items here to sell")
└── CloseButton
    └── Text (TMP)
```

---

**Sub-step 7g-7: Build the ShopStockEntry prefab**

The ShopUI script needs a prefab to instantiate one row per stock item. Build it in the scene first, then save as a prefab.

**(a) Create the row parent:**

1. Right-click `Canvas` → **UI → Image**. Rename `ShopStockEntry`.
2. Width = `380`, Height = `48`. Color = RGBA `40, 40, 50, 220` (dark slot bg).
3. Add Component → **Horizontal Layout Group**:
   - Padding all = `6`
   - Spacing = `8`
   - Child Alignment = `Middle Left`
   - Child Force Expand Width = ❌, Height = ✅
   - Child Control Size Width = ✅, Height = ✅

**(b) Add Icon child:**

1. Right-click `ShopStockEntry` → **UI → Image**. Rename `Icon`.
2. Rect Transform doesn't matter much (Layout Group controls position).
3. **Add Component → Layout Element**: set Preferred Width = `40`, Preferred Height = `40`. (This forces the layout group to give it 40×40.)
4. Source Image = leave empty (script assigns at runtime).

**(c) Add Name child:**

1. Right-click `ShopStockEntry` → **UI → Text - TextMeshPro**. Rename `Name`.
2. **Add Component → Layout Element**: Flexible Width = `1` (takes up the leftover space).
3. TMP Text Input = `(Item Name)`. Font Size = `16`. Color = white. Alignment = middle-left.

**(d) Add Price child:**

1. Right-click `ShopStockEntry` → **UI → Text - TextMeshPro**. Rename `Price`.
2. **Add Component → Layout Element**: Preferred Width = `80`.
3. TMP Text Input = `0g`. Font Size = `14`. Color = warm yellow. Alignment = middle-right.

**(e) Add BuyButton child:**

1. Right-click `ShopStockEntry` → **UI → Button - TextMeshPro**. Rename `BuyButton`.
2. **Add Component → Layout Element**: Preferred Width = `80`, Preferred Height = `36`.
3. Inside BuyButton, click `Text (TMP)` child → change text to `Buy`.

**(f) Add the script:**

1. Click `ShopStockEntry` (the parent row).
2. **Add Component → Shop Stock Entry**.
3. Wire its 4 fields:
   - **Icon Image** → drag the `Icon` child.
   - **Name Text** → drag the `Name` child.
   - **Price Text** → drag the `Price` child.
   - **Buy Button** → drag the `BuyButton` child.

**(g) Save as a prefab:**

1. Drag `ShopStockEntry` from Hierarchy → into `Assets/_Project/Bank/UI/Prefabs/` folder in Project window.
2. The Hierarchy entry turns blue (linked to a prefab).
3. **Delete** the blue `ShopStockEntry` from Hierarchy (it would clutter the Canvas; the script spawns its own at runtime).

---

**Sub-step 7g-8: Add the ShopUI script and wire its 5 fields**

1. Click `ShopPanel` in Hierarchy.
2. **Add Component → Shop UI**.
3. Wire each field:

| Field                | What to drag                     | From                                                |
| -------------------- | -------------------------------- | --------------------------------------------------- |
| **Stock Prefab**     | `ShopStockEntry.prefab`          | Project window (`Assets/_Project/Bank/UI/Prefabs/`) |
| **Stock Parent**     | `StockParent` GameObject         | Hierarchy (child of ShopPanel)                      |
| **Shop Name Text**   | `Header` TMP                     | Hierarchy (child of ShopPanel)                      |
| **Pocket Gold Text** | `PocketGoldText` TMP             | Hierarchy (child of ShopPanel)                      |
| **Tooltip**          | the same `Tooltip` panel from M6 | Hierarchy (top-level under Canvas)                  |

---

**Sub-step 7g-9: Wire CloseButton OnClick**

1. Click `CloseButton` in Hierarchy.
2. Inspector → **Button** component → scroll to **On Click ()**.
3. Click the **`+`** button.
4. Drag `ShopPanel` from Hierarchy → drop onto the runtime object slot.
5. Click the function dropdown → select **ShopUI → OnClosePressed()**.

---

**Sub-step 7g-10: Wire ShopPanel to ShopController**

1. Click `[Bank]` GameObject in Hierarchy.
2. Inspector → **Shop Controller** component → **Shop Panel** field.
3. Drag `ShopPanel` from Hierarchy → drop onto this field.

---

**Sub-step 7g-11: Quick test (the shop should now work)**

1. Save scene (`Ctrl+S`).
2. Press **Play**.
3. Walk to the Shopkeeper. Prompt: `Press [F] to Shop`.
4. Press **F**. Shop panel opens with:
   - Header showing `Lobby Shopkeeper`
   - PocketGoldText showing `Gold: 100g`
   - 4 stock rows: Health Potion / Stamina Tonic / Iron Sword / Iron Pistol — each with icon (or blank if no sprite), name, price, Buy button.
   - SellZone at bottom: dim red rectangle saying "Drag items here to sell".
   - Close (Esc) button at top-right.
5. Click **Buy** on Health Potion. Console: `[Shop] Bought Health Potion for 25g.` Pocket → `75g`. Health Potion stack in inventory grows by 1 (open Tab to verify).
6. **Open inventory (Tab)**, **drag a Bone Fragment** onto the SellZone. Console: `[Shop] Sold 1× Bone Fragment for 2g.` Pocket → `77g`. Bone Fragment stack decreases by 1.
7. Click **Close (Esc)** → panel disappears.

If something breaks:

- **Shop opens but shows no stock rows** → ShopUI's Stock Prefab field is empty, OR Stock Parent is empty, OR Shop_LobbyShopkeeper has empty Stock List.
- **Buy button does nothing** → ShopStockEntry prefab's Buy Button field wasn't wired, OR prefab wasn't saved properly.
- **SellZone doesn't react to drops** → the Image component on SellZone has Raycast Target unchecked, OR the child Label has Raycast Target checked (which blocks).
- **Pocket text shows 0** → PlayerWallet starting gold issue (see §7f-6 troubleshooting)..

### 7h — Test (Lobby scene)

1. Save scene. Press Play in `2_Lobby.unity` (or start from `0_LandingScene`).
2. **Hotbar** is visible at bottom (carries over from M6).
3. **Walk toward the Banker capsule** — the prompt `Press [F] to access Bank` appears.
4. **Press F** — Bank panel opens. You see your inventory grid (left, from M6 Inventory UI if you placed it in this scene too) + bank grid (right). Pocket: 100g, Vault: 0g.
5. **Type "30" in DepositInput → click Deposit.** Pocket → 70, Vault → 30.
6. **Type "10" in WithdrawInput → click Withdraw.** Pocket → 80, Vault → 20.
7. **Drag a Health Potion from inventory → into a bank slot.** The potion moves from inventory to bank.
8. **Drag the same potion back from bank → into an empty inventory slot.** It returns.
9. **Click Close (Esc also works).**
10. **Walk toward the Shopkeeper.** Prompt: `Press [F] to Shop`.
11. **Press F** — Shop panel opens. You see 4 stock entries with prices (Health Potion 25g, Stamina Tonic 25g, Iron Sword …, Iron Pistol …).
12. **Click Buy on Health Potion.** Console: `[Shop] Bought Health Potion for 25g.` Pocket: 80 → 55. Health Potion stack in inventory grows by 1.
13. **Drag a Bone Fragment from inventory → onto the SellZone.** Console: `[Shop] Sold 1× Bone Fragment for 2g.` Pocket: 55 → 57.
14. **Quit Play.** Re-enter — your gold + bank state persist.

### 7i — Tuning + extending

| Goal                           | How                                                                                     |
| ------------------------------ | --------------------------------------------------------------------------------------- |
| Make selling worth less        | Lower `SellPriceMultiplier` on the ShopDefinition (e.g. `0.5` = half value).            |
| Multiple shops                 | Create more `ShopDefinition` assets, drop new `ShopNPC` with each.                      |
| Bank charges a fee             | In `BankManager.DepositGold`, take a percentage off the deposit before adding to vault. |
| Shop locked to specific items  | Different ShopDefinition per NPC — already supported.                                   |
| Persistent gold display in HUD | M9 work — bind a TMP text to `PlayerWallet.OnGoldChanged`.                              |

## M8 — Reward system: loot drops, chests, EXP, leveling

M8 connects everything from M4–M7. Enemies now drop items + gold + EXP, bosses drop guaranteed loot, chests can be placed anywhere, and the player levels up with stat gains.

Code lives under [Assets/\_Project/Rewards/Scripts/](Assets/_Project/Rewards/Scripts/):

- [LootTable.cs](Assets/_Project/Rewards/Scripts/LootTable.cs) — ScriptableObject defining `{Item, dropChance, minQty, maxQty}` rolls + `{minGold, maxGold}` + `experience`.
- [DropSpawner.cs](Assets/_Project/Rewards/Scripts/DropSpawner.cs) — static helper that rolls a LootTable and instantiates pickups around a position. Also exposes `DropPrefabBindings` MonoBehaviour to register the pickup prefabs at scene start.
- [ItemPickup.cs](Assets/_Project/Rewards/Scripts/ItemPickup.cs) — bobbing/spinning world object. Press F to pick up (manual). Extends `Interactable`.
- [GoldPickup.cs](Assets/_Project/Rewards/Scripts/GoldPickup.cs) — auto-magnet pickup. Flies to player within 4m, auto-collects within 0.6m.
- [Chest.cs](Assets/_Project/Rewards/Scripts/Chest.cs) — interactable chest, opens once, spawns loot from its assigned LootTable.
- [ExperienceSystem.cs](Assets/_Project/Rewards/Scripts/ExperienceSystem.cs) — singleton, level cap 10, exponential curve (level N→N+1 = `100 * 2^(N-1)` EXP), grants +10 max HP / +5 max stamina per level.

**Cross-cutting changes:**

- [EnemyBase.cs](Assets/_Project/Enemies/AI/EnemyBase.cs) — added `lootTable` field. On death, calls `DropSpawner.SpawnLoot()`.
- [PlayerStats.cs](Assets/_Project/Player/Scripts/PlayerStats/PlayerStats.cs) — added `AddMaxHealth` / `AddMaxStamina` methods (used by ExperienceSystem on level-up).
- [InventoryPersistence.cs](Assets/_Project/Inventory/Scripts/InventoryPersistence.cs) — now also persists level + EXP via `PlayerProfile.level` / `experience`.

### 8a — Build the pickup prefabs (one-time)

Both pickups are tiny placeholder cubes for Phase 1. Real models / VFX are M9.

**Sub-step 8a-1: Build the ItemPickup prefab**

1. Hierarchy → 3D Object → **Cube**. Name `ItemPickup`.
2. Scale to `(0.4, 0.4, 0.4)` — small enough to be unobtrusive on the floor.
3. Tint via material — create `M_ItemPickup` (light blue, `100, 200, 255`). Drag onto cube.
4. Existing BoxCollider — set **Is Trigger** = ✅.
5. **Layer** — leave `Default` (so it doesn't collide with enemies/walls but still triggers detection).
6. Add Component → **Item Pickup**.
7. Drag the cube into `Assets/_Project/Rewards/Prefabs/` (create the folder if missing) → save as prefab.
8. Delete the scene copy.

**Sub-step 8a-2: Build the GoldPickup prefab**

1. Hierarchy → 3D Object → **Cube**. Name `GoldPickup`.
2. Scale to `(0.25, 0.25, 0.25)` — smaller than item pickups.
3. Create `M_Gold` material — RGB `255, 215, 60` (gold yellow). Drag onto cube.
4. BoxCollider → **Is Trigger** = ✅.
5. Add Component → **Gold Pickup**.
6. Drag to `Assets/_Project/Rewards/Prefabs/` → save as prefab.
7. Delete scene copy.

**Sub-step 8a-3: Register the prefabs in each scene**

`DropSpawner` is static — needs prefab references registered at runtime. Use the `DropPrefabBindings` component for this.

In **each scene that has enemies or chests** (`3_Dungeon1.unity` for now; Lobby doesn't drop loot):

1. Hierarchy → Create Empty at scene root → name `[DropPrefabs]`.
2. Add Component → **Drop Prefab Bindings**.
3. Wire fields:
   - **Item Pickup Prefab** → drag `ItemPickup.prefab` from Project window.
   - **Gold Pickup Prefab** → drag `GoldPickup.prefab`.
4. Save scene.

### 8b — Add the ExperienceSystem

In **`3_Dungeon1.unity`** AND **`2_Lobby.unity`** (both scenes — so the player keeps leveling state in both):

1. Hierarchy → Create Empty at scene root → name `[Experience]`.
2. Add Component → **Experience System**.
3. Wire fields:
   - **Player Stats** → drag the `Player` GameObject (it has the PlayerStats component).
4. Leave defaults (level 1, exp 0, +10 HP, +5 stamina per level).

### 8c — Create LootTable assets

A LootTable is a ScriptableObject (`.asset` file in your Project window) that defines what an enemy drops on death. Each LootTable has **3 separate Inspector sections**, not one list:

```
LootTable Inspector layout:

▼ Item Rolls (the drop list)
   Rolls (Size: N)
     Element 0
       Item: <reference>
       Drop Chance: 0.0–1.0
       Min Quantity: int
       Max Quantity: int
     Element 1
       ...

▼ Gold
   Min Gold: int
   Max Gold: int

▼ Experience
   Experience: int
```

- **Item Rolls** = the list of possible item drops. Each row rolls _independently_ against its DropChance — a 70% common + 5% rare can drop both, neither, or just one.
- **Gold** = a min–max range. Random integer in this range is dropped (set both to 0 to drop no gold).
- **Experience** = single value granted to the player when this LootTable is rolled.

**Sub-step 8c-1: Create the folder and 6 LootTable assets**

1. In Project window → navigate to `Assets/_Project/Rewards/` (if `Rewards` doesn't exist, create the folder first).
2. Right-click empty space → **Create → DungeonBlade → Loot Table**. Name the new file `Loot_SkeletonSoldier`.
3. Repeat 5 more times for: `Loot_SkeletonArcher`, `Loot_ArmoredKnight`, `Loot_UndeadWarlord`, `Loot_BasicChest`, `Loot_BossChest`.

You should now see 6 .asset files in `Assets/_Project/Rewards/`.

**Sub-step 8c-2: Fill each LootTable**

Click each asset → use the Inspector to fill the 3 sections according to the tables below.

---

**Loot_SkeletonSoldier:**

| Section                | Field        | Value               |
| ---------------------- | ------------ | ------------------- |
| Item Rolls             | Rolls Size   | `1`                 |
| Item Rolls → Element 0 | Item         | `Item_BoneFragment` |
| Item Rolls → Element 0 | Drop Chance  | `0.7`               |
| Item Rolls → Element 0 | Min Quantity | `1`                 |
| Item Rolls → Element 0 | Max Quantity | `1`                 |
| Gold                   | Min Gold     | `5`                 |
| Gold                   | Max Gold     | `15`                |
| Experience             | Experience   | `25`                |

---

**Loot_SkeletonArcher:**

| Section                | Field        | Value               |
| ---------------------- | ------------ | ------------------- |
| Item Rolls             | Rolls Size   | `1`                 |
| Item Rolls → Element 0 | Item         | `Item_BoneFragment` |
| Item Rolls → Element 0 | Drop Chance  | `0.7`               |
| Item Rolls → Element 0 | Min Quantity | `1`                 |
| Item Rolls → Element 0 | Max Quantity | `1`                 |
| Gold                   | Min Gold     | `8`                 |
| Gold                   | Max Gold     | `18`                |
| Experience             | Experience   | `30`                |

---

**Loot_ArmoredKnight:**

| Section                | Field        | Value               |
| ---------------------- | ------------ | ------------------- |
| Item Rolls             | Rolls Size   | `2`                 |
| Item Rolls → Element 0 | Item         | `Item_HealthPotion` |
| Item Rolls → Element 0 | Drop Chance  | `0.5`               |
| Item Rolls → Element 0 | Min Quantity | `1`                 |
| Item Rolls → Element 0 | Max Quantity | `1`                 |
| Item Rolls → Element 1 | Item         | `Item_BoneFragment` |
| Item Rolls → Element 1 | Drop Chance  | `1.0`               |
| Item Rolls → Element 1 | Min Quantity | `1`                 |
| Item Rolls → Element 1 | Max Quantity | `2`                 |
| Gold                   | Min Gold     | `20`                |
| Gold                   | Max Gold     | `40`                |
| Experience             | Experience   | `60`                |

---

**Loot_UndeadWarlord** (boss):

| Section                | Field        | Value               |
| ---------------------- | ------------ | ------------------- |
| Item Rolls             | Rolls Size   | `3`                 |
| Item Rolls → Element 0 | Item         | `Item_IronSword`    |
| Item Rolls → Element 0 | Drop Chance  | `0.5`               |
| Item Rolls → Element 0 | Min Quantity | `1`                 |
| Item Rolls → Element 0 | Max Quantity | `1`                 |
| Item Rolls → Element 1 | Item         | `Item_IronPistol`   |
| Item Rolls → Element 1 | Drop Chance  | `0.5`               |
| Item Rolls → Element 1 | Min Quantity | `1`                 |
| Item Rolls → Element 1 | Max Quantity | `1`                 |
| Item Rolls → Element 2 | Item         | `Item_HealthPotion` |
| Item Rolls → Element 2 | Drop Chance  | `1.0`               |
| Item Rolls → Element 2 | Min Quantity | `3`                 |
| Item Rolls → Element 2 | Max Quantity | `3`                 |
| Gold                   | Min Gold     | `200`               |
| Gold                   | Max Gold     | `400`               |
| Experience             | Experience   | `300`               |

_Note:_ both weapon rows are 50% drop chance and roll independently. So a Warlord kill might drop just sword, just pistol, both, or neither — plus 3 potions guaranteed. If you want guaranteed weapon drop, set both Drop Chances to `1.0`.

---

**Loot_BasicChest** (Zone 2 + Zone 4 chests):

| Section                | Field        | Value                        |
| ---------------------- | ------------ | ---------------------------- |
| Item Rolls             | Rolls Size   | `2`                          |
| Item Rolls → Element 0 | Item         | `Item_HealthPotion`          |
| Item Rolls → Element 0 | Drop Chance  | `1.0`                        |
| Item Rolls → Element 0 | Min Quantity | `2`                          |
| Item Rolls → Element 0 | Max Quantity | `4`                          |
| Item Rolls → Element 1 | Item         | `Item_BoneFragment`          |
| Item Rolls → Element 1 | Drop Chance  | `1.0`                        |
| Item Rolls → Element 1 | Min Quantity | `3`                          |
| Item Rolls → Element 1 | Max Quantity | `6`                          |
| Gold                   | Min Gold     | `50`                         |
| Gold                   | Max Gold     | `120`                        |
| Experience             | Experience   | `0` (chests don't grant EXP) |

---

**Loot_BossChest** (Zone 5 post-boss):

| Section                | Field        | Value               |
| ---------------------- | ------------ | ------------------- |
| Item Rolls             | Rolls Size   | `3`                 |
| Item Rolls → Element 0 | Item         | `Item_HealthPotion` |
| Item Rolls → Element 0 | Drop Chance  | `1.0`               |
| Item Rolls → Element 0 | Min Quantity | `5`                 |
| Item Rolls → Element 0 | Max Quantity | `5`                 |
| Item Rolls → Element 1 | Item         | `Item_StaminaTonic` |
| Item Rolls → Element 1 | Drop Chance  | `1.0`               |
| Item Rolls → Element 1 | Min Quantity | `3`                 |
| Item Rolls → Element 1 | Max Quantity | `3`                 |
| Item Rolls → Element 2 | Item         | `Item_BoneFragment` |
| Item Rolls → Element 2 | Drop Chance  | `1.0`               |
| Item Rolls → Element 2 | Min Quantity | `8`                 |
| Item Rolls → Element 2 | Max Quantity | `10`                |
| Gold                   | Min Gold     | `100`               |
| Gold                   | Max Gold     | `200`               |
| Experience             | Experience   | `0`                 |

---

**Sub-step 8c-3: Verify each LootTable saved correctly**

After filling each one, click somewhere else in the Project window then click back on the asset. The Inspector should show the values you entered persist. If anything reverts to default after clicking off, that asset isn't being saved — check that you're not currently in Play mode (assets don't save during Play).

### 8d — Assign loot tables to enemies

For each enemy prefab in `Assets/_Project/Enemies/Prefabs/`:

1. Open the prefab. Click the root.
2. Inspector → look for the **Loot Table** field on the enemy script (Skeleton Soldier / Skeleton Archer / Armored Knight).
3. Drag the matching Loot_X.asset into the field.
4. Save prefab.

For the boss in your dungeon scene:

1. Open `3_Dungeon1.unity`. Click `UndeadWarlord` in Hierarchy.
2. Inspector → **Undead Warlord** component → **Loot Table** field.
3. Drag `Loot_UndeadWarlord.asset` in.
4. Save scene.

### 8e — Place chests in the dungeon

Per the GDD, place 3 chests:

**Chest 1 (Zone 2 — Barracks):** loot pile in a corner.
**Chest 2 (Zone 4 — Armory):** rewarded for surviving the ArrowWall trap.
**Chest 3 (Zone 5 — Throne Room):** spawns post-boss-defeat (or place in advance, opens after the boss is dead — see notes below).

For each chest:

1. Hierarchy → 3D Object → **Cube**, name `Chest_Z2` (and `Chest_Z4`, `Chest_Z5`).
2. Scale `(1.0, 0.7, 0.7)` — chest-shaped.
3. Material: `M_Chest` (warm brown, `120, 80, 50`).
4. Existing Box Collider — leave non-trigger (player should bump into it).
5. Add Component → **Chest**. Configure:
   - **Loot Table** → `Loot_BasicChest` (or `Loot_BossChest` for Zone 5 chest).
   - **Spawn Point** — leave empty (uses chest position + Vector3.up).
   - **Scatter Radius** = `1.0`.
   - **Open Visual** / **Closed Visual** — leave empty for Phase 1 (no visual swap on open; M9 polish).
   - **Prompt Text** = `Press [F] to open chest`.
6. Position chests in their respective zones, on a NavMesh-walkable surface.
7. Parent each chest under its zone (`Zone_2_Barracks`, `Zone_4_Armory`, `Zone_5_ThroneRoom`).

### 8f — Playtest

1. Save all scenes.
2. **Reset save** so you start at level 1 with 100g and an empty inventory: top menu → **DungeonBlade → Save Data → Reset ALL**.
3. Play `3_Dungeon1.unity`.
4. Walk to a Skeleton Soldier. Kill it.
5. Console should show:
   - `[Skeleton_Soldier] died.`
   - `[Pickup]` log when you walk over the Bone Fragment (after pressing F).
   - `[Gold] +Xg (Pocket: 100+Xg)` automatically when you walk close.
   - `[EXP] +25 EXP. (25/100)`
6. Continue killing enemies. After ~4 kills you'll level up:
   - `[Level] LEVEL UP! Now level 2.`
   - PlayerStats updates: max HP 100→110, max stamina 100→105.
7. Open a chest in Zone 2 — multiple item drops scatter around it.
8. Kill the boss. Massive loot dump.
9. Quit Play. Console: `[Save] Saved profile.json + bank.json`.
10. Re-enter Play. Console: `[EXP] Loaded level X (Y EXP).` State persists.

### 8g — Tuning + extending

| Goal                                     | Knob                                                                                                     |
| ---------------------------------------- | -------------------------------------------------------------------------------------------------------- |
| More gold per kill                       | Bump `Min Gold` / `Max Gold` on the LootTable.                                                           |
| Faster leveling                          | Lower `BaseExpPerLevel` constant in `ExperienceSystem.cs` (currently 100).                               |
| Linear curve instead of exponential      | Replace `Mathf.Pow(2, currentLevel - 1)` in `ExperienceRequired` with `currentLevel`.                    |
| Bigger stat gain per level               | Bump `maxHpPerLevel` / `maxStaminaPerLevel` on ExperienceSystem in Inspector.                            |
| Bosses guaranteed-drop a specific weapon | Set Drop Chance = `1.0` on that row, and 0 on other weapon rows.                                         |
| Auto-pickup items (no F key needed)      | In `ItemPickup.cs`, add `OnTriggerEnter` that calls `OnInteract` automatically.                          |
| Drops persist between sessions           | Track placed pickups in a list, serialize their positions/contents to a third save file. (Phase 2 work.) |

## M10 — Main Menu, Settings, Pause, Game Over, scene transitions

M10 connects all 4 scenes (`0_LandingScene`, `1_MainMenu`, `2_Lobby`, `3_Dungeon1`) into a real game flow: Landing → Main Menu → New Game / Continue → Lobby → enter Dungeon → die or finish → return to Lobby → Quit. Plus mid-game pause, settings menu (volumes / sensitivity / fullscreen), and game over.

Code:

- [Core/GameManager/FadeLoader.cs](Assets/_Project/Core/GameManager/FadeLoader.cs) — DontDestroyOnLoad fade overlay, fades to black, loads scene async, fades in.
- [Core/GameManager/SettingsManager.cs](Assets/_Project/Core/GameManager/SettingsManager.cs) — singleton, volumes / sensitivity / fullscreen persisted via PlayerPrefs.
- [UI/Menus/MainMenuController.cs](Assets/_Project/UI/Menus/MainMenuController.cs) — buttons + Continue-only-if-save-exists logic.
- [UI/Menus/SettingsUI.cs](Assets/_Project/UI/Menus/SettingsUI.cs) — binds sliders/toggles to SettingsManager. Reusable in MainMenu and PauseMenu.
- [UI/Menus/PauseController.cs](Assets/_Project/UI/Menus/PauseController.cs) — Esc opens, `Time.timeScale = 0`, Resume/Settings/Lobby/MainMenu/Quit buttons.
- [UI/Menus/GameOverScreen.cs](Assets/_Project/UI/Menus/GameOverScreen.cs) — listens to `PlayerStats.OnDeath`, fades in after delay, Retry/Lobby/MainMenu buttons.
- [Bank/Scripts/ScenePortal.cs](Assets/_Project/Bank/Scripts/ScenePortal.cs) — interactable cube/door, F-to-travel between scenes. Used for Lobby→Dungeon and Dungeon→Lobby.

Cross-cutting: [PlayerMovement.cs](Assets/_Project/Player/Scripts/Movement/PlayerMovement.cs) now reads sensitivity from `SettingsManager.Instance.MouseSensitivity` at runtime instead of the SerializeField, so changing the slider applies live.

### 10a — Build the FadeLoader (one-time, lives forever)

The fade overlay must persist across scene loads, so it goes in `0_LandingScene` (which loads first) and uses `DontDestroyOnLoad`.

1. Open `0_LandingScene.unity`.
2. Right-click Hierarchy → **UI → Canvas**. Name it `FadeCanvas`.
   - **Canvas** component → **Sort Order** = `1000` (renders on top of everything).
   - **Canvas Scaler** → UI Scale Mode = `Scale With Screen Size`, Reference = `1920 × 1080`.
3. Right-click `FadeCanvas` → **Create Empty**. Name `FadeRoot`.
   - Add Component → **Canvas Group**. Alpha = `0`, Interactable = ✅, Blocks Raycasts = ❌.
4. Right-click `FadeRoot` → **UI → Image**. Name `FadeImage`. Anchor stretch (Alt+Shift+stretch). Color = solid black, alpha `255`. Raycast Target ❌.
5. Add Component → **Fade Loader** on `FadeRoot`. Wire fields:
   - **Fade Group** → drag `FadeRoot`'s CanvasGroup
   - **Fade Image** → drag `FadeImage`
   - **Default Fade Duration** = `0.4`
6. Save scene. The FadeLoader survives all scene loads.

### 10b — Add SettingsManager to LandingScene

Same scene, also persistent.

1. Hierarchy → Create Empty → name `[Settings]`.
2. Add Component → **Settings Manager**. No fields to wire — uses PlayerPrefs.
3. Save scene.

### 10c — Build the Main Menu

Open `1_MainMenu.unity`.

**Canvas + buttons:**

1. Right-click → **UI → Canvas** if missing. Configure Canvas Scaler (1920×1080, Match 0.5).
2. Right-click `Canvas` → **UI → Panel**. Name `MainMenuPanel`. Color dark `(15, 15, 25, 230)`.
3. Inside `MainMenuPanel`, add a TMP title:
   - Right-click → **UI → Text - TextMeshPro**. Name `Title`.
   - Anchor top-center. Pos Y = `-180`. Width `800`, Height `100`. Font Size `64`, Bold, color white. Text = `Dungeon Blade`.
4. Right-click `MainMenuPanel` → Create Empty → name `ButtonGroup`. Anchor middle-center. Width `300`, Height `300`. Add **Vertical Layout Group** (padding 8, spacing 12, child force expand width ✅). Add **Content Size Fitter** (Vertical Fit = Preferred Size).
5. Inside `ButtonGroup`, add 4 buttons (right-click → UI → Button - TextMeshPro):
   - `NewGameButton` — Text = `New Game`
   - `ContinueButton` — Text = `Continue`
   - `SettingsButton` — Text = `Settings`
   - `QuitButton` — Text = `Quit`
6. Add a small TMP `VersionLabel` at bottom-right of MainMenuPanel. Anchor bottom-right, Pos `(-10, 10)`, Width `120`, Height `30`, Font Size `12`. Text = `v0.1`.

**Settings panel (overlay):** 7. Right-click `Canvas` → **UI → Panel**. Name `SettingsPanel`. Stretch full-screen, color `(15, 15, 25, 230)`. **Disable** by default. 8. Inside SettingsPanel, build the rows.

**8a — Container:**

- Right-click `SettingsPanel` → Create Empty → name `Content`.
- Rect Transform: Anchor middle-center, Width `700`, Height `560`, Pos `(0, 0)`.
- Add Component → **Vertical Layout Group**. Padding `(20, 20, 20, 20)`, Spacing `12`, Child Alignment `Upper Center`, Child Force Expand: Width ✅, Height ❌, Child Controls Size: Width ✅, Height ❌.
- Add Component → **Content Size Fitter**. Vertical Fit = `Preferred Size`.
- (Optional) Right-click `Content` → UI → Text - TextMeshPro at top → name `SettingsTitle`. Text = `Settings`. Font Size `36`, Alignment center, Bold. Add Component → **Layout Element**, Preferred Height `60`.

**8b — Row template (Master Volume):**

- Right-click `Content` → Create Empty → name `Row_MasterVolume`.
- Rect Transform: Width auto (driven by parent), Height `40`. Add **Layout Element**: Preferred Height `40`.
- Add Component → **Horizontal Layout Group**. Padding `(0,0,0,0)`, Spacing `12`, Child Alignment `Middle Left`, Child Force Expand Width ❌, Height ✅, Child Controls Size Width ❌, Height ✅.
- Inside `Row_MasterVolume`:
  - Right-click → UI → Text - TextMeshPro → name `Label`. Text = `Master Volume`. Width `200`. Add **Layout Element** → Preferred Width `200`. Font Size `20`, Alignment middle-left.
  - Right-click → UI → Slider → name `Slider`. Add **Layout Element** → Preferred Width `360`, Flexible Width `1`. Slider component: Min Value `0`, Max Value `1`, Whole Numbers ❌, Value `0.8`.
  - Right-click → UI → Text - TextMeshPro → name `ValueText`. Text = `80%`. Width `80`. Add **Layout Element** → Preferred Width `80`. Font Size `20`, Alignment middle-right.

**8c — Duplicate the row 3 more times:**

- Select `Row_MasterVolume` → Ctrl+D three times. Rename the copies:
  - `Row_MusicVolume` (Label = `Music Volume`, Slider Min `0`, Max `1`, Value `0.8`, ValueText = `80%`)
  - `Row_SfxVolume` (Label = `SFX Volume`, Slider Min `0`, Max `1`, Value `0.8`, ValueText = `80%`)
  - `Row_Sensitivity` (Label = `Mouse Sensitivity`, Slider **Min `0.01`, Max `1`**, Value `0.2`, ValueText = `0.20`)

**8d — Fullscreen row:**

- Right-click `Content` → Create Empty → name `Row_Fullscreen`. Add **Layout Element** Preferred Height `40`. Add **Horizontal Layout Group** (same settings as 8b row).
- Inside `Row_Fullscreen`:
  - Add TMP `Label`, Text = `Fullscreen`, Preferred Width `200`.
  - Right-click → UI → Toggle → name `Toggle`. Add **Layout Element** Preferred Width `40`. (Toggle's Background + Checkmark stay as-is.)

**8e — Buttons row (bottom):**

- Right-click `Content` → Create Empty → name `Row_Buttons`. Add **Layout Element** Preferred Height `60`. Add **Horizontal Layout Group**: Spacing `20`, Child Alignment `Middle Center`, Child Force Expand Width ✅.
- Inside `Row_Buttons`:
  - **ResetButton:**
    1. Right-click `Row_Buttons` → UI → Button - TextMeshPro → name `ResetButton`.
    2. Select `ResetButton`. Add Component → **Layout Element**. Check `Preferred Width` = `220`, check `Preferred Height` = `48`.
    3. Expand `ResetButton` in the Hierarchy → select its auto-created child `Text (TMP)`. In TextMeshPro - Text (UI), set Text = `Reset to Defaults`. Font Size `20`, Alignment center + middle.
    4. (Optional) Select `ResetButton` again → Image component → Color `(80, 60, 60, 255)` muted red to visually mark it as a destructive action.
  - **CloseButton:**
    1. Right-click `Row_Buttons` → UI → Button - TextMeshPro → name `CloseButton`.
    2. Select `CloseButton`. Add Component → **Layout Element**. Check `Preferred Width` = `160`, check `Preferred Height` = `48`.
    3. Expand `CloseButton` → select its child `Text (TMP)`. Set Text = `Close`. Font Size `20`, Alignment center + middle.
    4. (Optional) Image color `(60, 80, 60, 255)` muted green for a "safe" action.

  **Notes for both buttons:**
  - The Button GameObject created by Unity already has both **Image** and **Button** components. You only need to add **Layout Element** yourself.
  - The label text lives on the auto-created **child** GameObject named `Text (TMP)` — not on the Button itself.
  - Do **not** wire the Button's `On Click ()` event in the Inspector. `SettingsUI.cs` registers handlers in code (`closeButton.onClick.AddListener(OnClose)` / `resetButton.onClick.AddListener(OnReset)`).
  - If the buttons stretch to fill the row instead of using their preferred widths, check that the parent `Row_Buttons` Horizontal Layout Group has **Child Controls Size → Width ❌** (so the Layout Element's Preferred Width drives sizing).

9. Add Component → **Settings UI** on `SettingsPanel`. Wire each field by dragging from the Hierarchy:

   | Inspector field        | Drag from Hierarchy          |
   | ---------------------- | ---------------------------- |
   | Master Slider          | `Row_MasterVolume/Slider`    |
   | Music Slider           | `Row_MusicVolume/Slider`     |
   | Sfx Slider             | `Row_SfxVolume/Slider`       |
   | Sensitivity Slider     | `Row_Sensitivity/Slider`     |
   | Fullscreen Toggle      | `Row_Fullscreen/Toggle`      |
   | Master Value Text      | `Row_MasterVolume/ValueText` |
   | Music Value Text       | `Row_MusicVolume/ValueText`  |
   | Sfx Value Text         | `Row_SfxVolume/ValueText`    |
   | Sensitivity Value Text | `Row_Sensitivity/ValueText`  |
   | Close Button           | `Row_Buttons/CloseButton`    |
   | Reset Button           | `Row_Buttons/ResetButton`    |

   Tip: lock the Inspector (padlock icon top-right) on `SettingsPanel` so it stays selected while you drag children in.

10. Add Component → **Main Menu Controller** on `MainMenuPanel`. Wire each field:

    | Inspector field | Drag from Hierarchy                               |
    | --------------- | ------------------------------------------------- |
    | New Game Button | `ButtonGroup/NewGameButton`                       |
    | Continue Button | `ButtonGroup/ContinueButton`                      |
    | Settings Button | `ButtonGroup/SettingsButton`                      |
    | Quit Button     | `ButtonGroup/QuitButton`                          |
    | Settings Panel  | `SettingsPanel` (the GameObject, not a component) |
    | Version Label   | `VersionLabel` (TMP_Text)                         |

    Note: Settings Panel must be the **disabled** SettingsPanel GameObject — the controller calls `SetActive(true)` when Settings is clicked.

11. Save scene (`Ctrl+S`). Verify final hierarchy:
    ```
    Canvas
    ├── MainMenuPanel
    │   ├── Title (TMP)
    │   ├── ButtonGroup
    │   │   ├── NewGameButton
    │   │   ├── ContinueButton
    │   │   ├── SettingsButton
    │   │   └── QuitButton
    │   └── VersionLabel (TMP)
    └── SettingsPanel  (disabled)
        └── Content
            ├── SettingsTitle (TMP, optional)
            ├── Row_MasterVolume   (Label + Slider + ValueText)
            ├── Row_MusicVolume    (Label + Slider + ValueText)
            ├── Row_SfxVolume      (Label + Slider + ValueText)
            ├── Row_Sensitivity    (Label + Slider + ValueText)
            ├── Row_Fullscreen     (Label + Toggle)
            └── Row_Buttons        (ResetButton + CloseButton)
    ```
    Press Play. You should see the main menu with 4 buttons. Click **Settings** → SettingsPanel appears, sliders show current values. Move a slider → ValueText updates live. Click **Close** → returns to main menu. Click **New Game** → fades to Lobby.

### 10d — Build the Pause Menu (in Dungeon scene; optional in Lobby)

Open `3_Dungeon1.unity`. The pause menu is a Canvas overlay layered on top of the in-game HUD.

**Pre-req:** the Dungeon scene needs a `[Settings]` GameObject too (same as MainMenu).
- Hierarchy → Create Empty → name `[Settings]` → Add Component → **Settings Manager** → Save.
- Without this, opening Settings from the pause menu won't update sliders/values (same SettingsManager.Instance == null bug as before).

**1. Create the PausePanel:**

1. In the Hierarchy, find your existing `Canvas` (the one with HUD/Inventory). Right-click `Canvas` → **UI → Panel** → name `PausePanel`.
2. Rect Transform: anchor preset **stretch all** (Alt+click bottom-right preset). Left/Top/Right/Bottom = `0`.
3. Image component → Color = `(0, 0, 0, 200)` (semi-transparent black so the game shows through dimmed).
4. **Disable** the GameObject (uncheck the checkbox at the top-left of the Inspector). Name turns gray.

**2. Add the title:**

1. Right-click `PausePanel` → **UI → Text - TextMeshPro** → name `Title`.
2. Rect Transform: anchor **top-center**. Pos Y = `-120`. Width `600`, Height `100`.
3. TMP component: Text = `PAUSED`. Font Size `56`, Bold, Alignment center+middle, Color white.

**3. Add the button group:**

1. Right-click `PausePanel` → Create Empty → name `ButtonGroup`.
2. Rect Transform: anchor **middle-center**. Width `320`, Height `400`. Pos `(0, 0)`.
3. Add Component → **Vertical Layout Group**: Padding `(8,8,8,8)`, Spacing `12`, Child Alignment `Upper Center`, Child Force Expand Width ✅, Height ❌, Child Controls Size Width ✅, Height ❌.
4. Add Component → **Content Size Fitter**: Vertical Fit = `Preferred Size`.

**4. Add 5 buttons inside `ButtonGroup`:**

For each: right-click `ButtonGroup` → **UI → Button - TextMeshPro** → rename → set child `Text (TMP)` → add **Layout Element** with Preferred Height `48`.

| GameObject name | Child Text |
|---|---|
| `ResumeButton` | `Resume` |
| `SettingsButton` | `Settings` |
| `ReturnLobbyButton` | `Return to Lobby` |
| `MainMenuButton` | `Main Menu` |
| `QuitButton` | `Quit to Desktop` |

**5. Build the in-Dungeon SettingsPanel:**

The cleanest way: replicate §10c steps 7-9 inside the Dungeon Canvas.

1. Right-click `Canvas` → **UI → Panel** → name `SettingsPanel`. Stretch full, color `(15, 15, 25, 230)`. **Disable** by default.
2. Inside, build the same `Content` container with the 4 sliders + Fullscreen toggle + Reset/Close buttons (see §10c step 8a-8e).
3. Add Component → **Settings UI** on `SettingsPanel`. Wire all 11 fields per the §10c step 9 table.

**Shortcut (optional):** in the MainMenu scene, right-click `SettingsPanel` → **Prefab → Create Original Prefab...** → save it under `Assets/_Project/UI/Prefabs/SettingsPanel.prefab`. Then drag the prefab into the Dungeon scene's Canvas. The SettingsUI component and all its wiring carry over. (Re-disable the panel after dragging.)

**6. Add the PauseController:**

1. Hierarchy → Create Empty (or pick existing `[Inventory]`) → name `[PauseManager]` if creating new.
2. Add Component → **Pause Controller**.
3. Wire each field by drag-and-drop:

   | Inspector field | Drag from Hierarchy |
   |---|---|
   | Pause Panel | `Canvas/PausePanel` (the disabled one) |
   | Settings Panel | `Canvas/SettingsPanel` (the disabled one) |
   | Resume Button | `PausePanel/ButtonGroup/ResumeButton` |
   | Settings Button | `PausePanel/ButtonGroup/SettingsButton` |
   | Return Lobby Button | `PausePanel/ButtonGroup/ReturnLobbyButton` |
   | Main Menu Button | `PausePanel/ButtonGroup/MainMenuButton` |
   | Quit Button | `PausePanel/ButtonGroup/QuitButton` |
   | Persistence | `[Inventory]` GameObject (has Inventory Persistence component) |

   Lock the Inspector on `[PauseManager]` while dragging children in.

**7. Save scene** (`Ctrl+S`). Verify final hierarchy:

```
Canvas
├── InventoryPanel              (existing, disabled)
├── PausePanel                  (disabled)
│   ├── Title (TMP — "PAUSED")
│   └── ButtonGroup
│       ├── ResumeButton
│       ├── SettingsButton
│       ├── ReturnLobbyButton
│       ├── MainMenuButton
│       └── QuitButton
└── SettingsPanel               (disabled)
    └── Content
        ├── Row_MasterVolume
        ├── Row_MusicVolume
        ├── Row_SfxVolume
        ├── Row_Sensitivity
        ├── Row_Fullscreen
        └── Row_Buttons (Reset + Close)

[PauseManager]                  (root, has Pause Controller)
[Settings]                      (root, has Settings Manager)
```

**8. Test in Play mode:**

1. Press Play → enter Dungeon (via portal from Lobby, or set Dungeon as the play scene).
2. Press **Esc** → PausePanel appears, time freezes (`Time.timeScale = 0`), cursor unlocks.
3. Click **Settings** → SettingsPanel overlays the pause menu. Sliders work.
4. Press **Esc** again → SettingsPanel closes (the controller closes child panel first, then pause on next press).
5. Click **Resume** → game unpauses, cursor re-locks.
6. Re-pause → click **Return to Lobby** → save fires, fade to Lobby.
7. Re-pause → click **Quit to Desktop** → save fires, exits Play (or app in build).

**Common issues:**

- **Esc does nothing** → no PauseController in scene, or it's on a disabled GameObject. Confirm `[PauseManager]` is in Hierarchy and enabled.
- **Esc opens pause but Inventory was already open** → expected. PauseController checks `MenuState.IsAnyOpen` and refuses to open over another menu. Close Inventory first.
- **Cursor stays locked when paused** → PauseController.Open sets `Cursor.lockState = None`. If still locked, something else (maybe PlayerMovement) is forcing it back. Check Console for repeated lock calls.
- **Buttons do nothing** → likely a wiring gap. Re-check all 8 fields in PauseController.

Repeat for `2_Lobby.unity` if you want pause-during-lobby (useful for accessing Settings without leaving NPCs). Same steps; lobby's PauseController references the lobby scene's own PausePanel/SettingsPanel.

### 10e — Build the Game Over Screen (Dungeon scene)

Open `3_Dungeon1.unity`. The Game Over screen is a fullscreen overlay that appears when the player dies.

**1. Create the panel:**
1. In the Hierarchy, right-click your `Canvas` → **UI → Panel** → name `GameOverPanel`.
2. Rect Transform: anchor preset **stretch all** (Alt+click bottom-right preset). Left/Top/Right/Bottom = `0`.
3. Image component → Color = `(0, 0, 0, 230)` (near-opaque black).
4. **Disable** the GameObject (uncheck the checkbox at the top-left of the Inspector).

**2. Add the "You Died" header:**
1. Right-click `GameOverPanel` → **UI → Text - TextMeshPro** → name `Title`.
2. Rect Transform: anchor **top-center**. Pos Y = `-180`. Width `800`, Height `140`.
3. TMP component:
   - Text = `You Died`
   - Font Size = `72`
   - Bold ✅
   - Alignment = center + middle
   - Color = red (use the Color field → set RGB `200, 30, 30, 255`)
4. (Optional) Add Outline material or a drop shadow via TMP's "Underlay" effect for impact.

**3. Add the button group:**
1. Right-click `GameOverPanel` → Create Empty → name `ButtonGroup`.
2. Rect Transform: anchor **middle-center**. Width `300`, Height `240`. Pos `(0, 0)`.
3. Add Component → **Vertical Layout Group**: Padding `(8,8,8,8)`, Spacing `12`, Child Alignment `Upper Center`, Child Force Expand Width ✅, Height ❌, Child Controls Size Width ✅, Height ❌.
4. Add Component → **Content Size Fitter**: Vertical Fit = `Preferred Size`.

**4. Add 3 buttons inside `ButtonGroup`:**

For each: right-click `ButtonGroup` → **UI → Button - TextMeshPro** → rename → set child `Text (TMP)` → Add Component → **Layout Element** → Preferred Height `48`.

| GameObject name | Child Text |
|---|---|
| `RetryButton` | `Retry` |
| `LobbyButton` | `Return to Lobby` |
| `MainMenuButton` | `Main Menu` |

**5. Add the Game Over Screen component:**
1. Select `GameOverPanel` in the Hierarchy.
2. Add Component → **Game Over Screen**.
3. Wire each field:

   | Inspector field | Drag from Hierarchy |
   |---|---|
   | Panel | `GameOverPanel` (itself — drag the same GameObject onto its own field) |
   | Player Stats | `Player` (has the PlayerStats component) |
   | Retry Button | `ButtonGroup/RetryButton` |
   | Lobby Button | `ButtonGroup/LobbyButton` |
   | Main Menu Button | `ButtonGroup/MainMenuButton` |
   | Show Delay | `1.0` (a 1-second black-screen pause before buttons appear) |

   Tip: lock the Inspector on `GameOverPanel` so the Inspector stays focused while you drag children in.

**6. Save scene** (Ctrl+S). Verify hierarchy:

```
Canvas
└── GameOverPanel               (disabled)
    ├── Title (TMP — "You Died", red)
    └── ButtonGroup
        ├── RetryButton
        ├── LobbyButton
        └── MainMenuButton
```

**7. Test in Play mode:**
1. Press Play → enter Dungeon → let an enemy kill you (or set HP to 1 and run into a hit).
2. After the player dies, wait ~1 second → GameOverPanel fades in, time freezes, cursor unlocks.
3. Click **Retry** → reloads the Dungeon (player respawns at start, save state restored).
4. Click **Return to Lobby** → fades to Lobby.
5. Click **Main Menu** → fades to MainMenu.

**Common issues:**
- **GameOverPanel never appears** → the Player Stats field isn't wired. GameOverScreen subscribes to `playerStats.OnDeath` in Start. Without the wiring, no event fires.
- **Buttons unresponsive** → `Time.timeScale = 0` is set when the panel shows; that's expected. UI buttons still work because they use unscaled time.
- **Cursor stays locked** → confirm you set `Cursor.lockState = None` in the Show() method (already in code).

### 10f — Build the Lobby ↔ Dungeon portals

ScenePortal extends `Interactable`, so it uses the same `[F]` press flow as NPCs/chests. The player's `PlayerInteractor` (added in M7) detects the closest interactable and triggers it.

**1. Create the portal material (once, reused):**
1. Project window → navigate to `Assets/_Project/Materials/`.
2. Right-click → **Create → Material** → name `M_Portal_Dungeon`.
3. Inspector → Albedo color → set RGB `120, 30, 30, 255` (dark red).
4. (Optional) Set Emission ✅ → color same dark red, intensity `1.0` so the portal glows.

**2. Lobby portal (enters dungeon):**

1. Open `2_Lobby.unity`.
2. Hierarchy → right-click → **3D Object → Cube** → name `Portal_ToDungeon`.
3. Transform:
   - Position: somewhere visible from the player spawn (e.g. against a wall opposite the NPCs).
   - Rotation: face the player approach direction (Y rotation as needed).
   - Scale: `(2, 3, 0.5)` for a door-like silhouette.
4. **Apply material:**
   - Drag `M_Portal_Dungeon` from Project window onto the Cube in Scene view (or onto its Mesh Renderer's Material slot in the Inspector).
5. **Replace the default Box Collider with a Sphere Collider:**
   - In the Inspector, click the gear icon on Box Collider → **Remove Component**.
   - Add Component → **Sphere Collider**.
   - Center `(0, 0, 0)`, Radius `1.5`.
   - **Is Trigger** = ❌ (so the player physically can't pass through; they have to interact).
6. Add Component → **Scene Portal**. Wire fields:

   | Inspector field | Value |
   |---|---|
   | Prompt Text | `Press [F] to enter Forsaken Keep` |
   | Target Scene | `3_Dungeon1` |
   | Persistence | drag `[Inventory]` GameObject from Hierarchy |
   | Save Before Transition | ✅ |

   Note: `Target Scene` must be a string matching the scene file name exactly (case-sensitive). The defaults from `SceneLoader.cs` are `0_LandingScene`, `1_MainMenu`, `2_Lobby`, `3_Dungeon1`.
7. (Optional polish) Add a TMP world-space label "Forsaken Keep" floating above the portal — right-click `Portal_ToDungeon` → **3D Object → Text - TextMeshPro** → set text + scale.
8. Save scene.

**3. Dungeon exit (back to Lobby):**

1. Open `3_Dungeon1.unity`.

2. **Create the lobby material (optional, for visual contrast):**
   - Project window → `Assets/_Project/Materials/`.
   - Right-click → **Create → Material** → name `M_Portal_Lobby`.
   - Inspector → Albedo → RGB `30, 80, 120, 255` (blue).
   - (Optional) Emission ✅, same blue, intensity `1.0`.

3. **Create the portal cube:**
   - Hierarchy → right-click → **3D Object → Cube** → name `Portal_ToLobby`.
   - Transform:
     - **Position:** at the dungeon entrance — where the player spawns. (Tip: select the Player GameObject, copy its position, then offset slightly so the player doesn't spawn inside the portal — e.g. 2 units behind it.)
     - **Rotation:** face it toward where the player will be coming from (Y rotation 180° if the player approaches from behind, etc.).
     - **Scale:** `(2, 3, 0.5)` (door-shaped).

4. **Apply the material:**
   - Drag `M_Portal_Lobby` from Project window onto the cube in the Scene view (or onto the Mesh Renderer's Material slot).

5. **Replace the Box Collider with a Sphere Collider:**
   - Inspector → Box Collider → gear icon → **Remove Component**.
   - Add Component → **Sphere Collider** → Center `(0, 0, 0)`, Radius `1.5`.
   - **Is Trigger** = ❌.

6. **Add Scene Portal component and wire it:**

   | Inspector field | Value |
   |---|---|
   | Prompt Text | `Press [F] to return to Lobby` |
   | Target Scene | `2_Lobby` |
   | Persistence | drag `[Inventory]` GameObject from Hierarchy |
   | Save Before Transition | ✅ |

7. **(Optional polish) Add the floating label:**
   - Right-click `Portal_ToLobby` → **3D Object → Text - TextMeshPro** → name `PortalLabel`.
   - Transform: Position `(0, 2.2, 0)`, Rotation `(0, 0, 0)`, Scale `(0.05, 0.05, 0.05)`.
   - TMP component: Text = `Lobby`, Font Size `36`, Bold, Alignment center+middle, Color white (or match portal blue).
   - Add Component → **Billboard** (the script created earlier in `Assets/_Project/Core/Utilities/Billboard.cs`). Lock Y Axis ✅.

8. **Save scene** (Ctrl+S).

**4. Test the portals:**
1. Play from MainMenu → New Game → Lobby loads.
2. Walk near `Portal_ToDungeon`. The "Press [F] to enter Forsaken Keep" prompt should appear (PlayerInteractor handles the prompt UI).
3. Press F → save fires (check Console for `[Save]` log) → fade to Dungeon.
4. In Dungeon, walk to `Portal_ToLobby` → F → fades back to Lobby.

**Common issues:**
- **No prompt appears when near the portal** → either Sphere Collider radius too small, or the portal lacks the `Interactable` script (Scene Portal extends it, so confirm Scene Portal component is present).
- **F press does nothing** → check that Player has a `PlayerInteractor` component (M7). Without it, no interaction is detected.
- **Scene won't load** → the target scene isn't in Build Settings. See §10g for the fix (or run `DungeonBlade > Build Settings > Sync Scenes`).

### 10g — Wire scene order in Build Settings + LandingLoader

**1. Confirm all scenes are in Build Settings:**

1. **File → Build Settings** (Ctrl+Shift+B).
2. The "Scenes In Build" list should contain (in order):
   - `Scenes/0_LandingScene` (index 0)
   - `Scenes/1_MainMenu` (index 1)
   - `Scenes/2_Lobby` (index 2)
   - `Scenes/3_Dungeon1` (index 3)
3. If any are missing: open the scene in the Editor → in Build Settings, click **Add Open Scenes**. Drag in the list to reorder if needed.
4. **Shortcut:** top menu → **DungeonBlade → Build Settings → Sync Scenes** auto-populates this list.

**2. LandingLoader configuration:**

`LandingLoader` is the script that holds the splash/landing scene briefly, then loads MainMenu. It also auto-spawns persistent systems (`[GameManager]`, `[InputManager]`) the first time the game runs.

1. **Open the scene:** double-click `Assets/Scenes/0_LandingScene.unity` in the Project window.

2. **Find the LandingLoader GameObject:**
   - In the Hierarchy, look for a root GameObject named `[Bootstrap]`, `[Loader]`, `[Landing]`, or similar — whatever you named it during M10a.
   - If you don't see one: select **any** root GameObject in the Hierarchy, then in the Inspector click **Add Component** → search `Landing Loader`. Or create a new empty: right-click Hierarchy → Create Empty → name `[Bootstrap]` → Add Component → **Landing Loader**.

3. **Inspector settings (exact field names from the script):**

   | Inspector field | Value | What it does |
   |---|---|---|
   | Next Scene | `1_MainMenu` | The scene loaded after the splash. String must match the scene file name exactly (case-sensitive). Default is fine. |
   | Min Display Seconds | `0.5` | Minimum time the landing scene stays visible. Default is `0.5`. Bump to `1.5` for a longer splash. |

4. **(Optional) Polish the landing visual:**
   - Add a Canvas with a logo Image (Sprite) and/or a TMP "Dungeon Blade" title.
   - Add a `[Fader]` GameObject with **FadeLoader** component if not already there (so the transition into MainMenu fades).
   - Add a simple background Image (dark color or splash art).

5. **Verify autospawn behavior:**
   - LandingLoader auto-creates `[GameManager]` and `[InputManager]` if they don't exist (via `EnsureSystem<T>()`).
   - This means you don't need to manually place those GameObjects — they spawn at runtime and persist via `DontDestroyOnLoad`.
   - The first scene load through LandingLoader is what sets up the global singletons for the entire session.

6. **Save scene** (Ctrl+S) if you changed anything.

**Test the splash:**
- Press Play in `0_LandingScene` → Unity holds for `Min Display Seconds` (0.5s default) → fades to MainMenu.
- Console should show no errors. If "Scene couldn't be loaded" appears, the `Next Scene` string is wrong or `1_MainMenu` isn't in Build Settings (re-run Sync Scenes from §10g step 1).

**3. FadeLoader present in every scene that uses fading:**

The `FadeLoader` is `DontDestroyOnLoad`, so it persists once spawned. But it must exist in the **first scene loaded** (`0_LandingScene`) for fades to work in subsequent scenes.

1. Open `0_LandingScene.unity`.
2. Confirm there's a `[Fader]` GameObject (or similar) with the FadeLoader component + a Canvas + CanvasGroup + Image.
3. If missing: see §10a for setup.

### 10h — Test the full game loop

End-to-end smoke test. Run this once from a clean save to validate every M10 piece works together.

**1. Pre-test cleanup:**

1. Save all open scenes (Ctrl+S in each).
2. Top menu → **DungeonBlade → Save Data → Reset ALL** (clears profile.json + bank.json so you start fresh).
3. Open `0_LandingScene.unity` so Play starts there.

**2. Run the loop:**

| Step | Expected behavior | If broken, check |
|---|---|---|
| Press Play | Landing screen appears (logo/splash). Fades to MainMenu after ~1.5s. | LandingLoader.TargetScene = `1_MainMenu`; FadeLoader exists in scene |
| MainMenu loads | 4 buttons visible. **Continue** is grayed out (no save). | MainMenuController.Continue Button wired; profile.json doesn't exist yet |
| Click **Settings** | SettingsPanel overlays MainMenu. Sliders/toggle show defaults. | SettingsManager exists in scene; SettingsUI wired |
| Adjust Master slider | ValueText updates live (e.g. `80%` → `60%`). | All 4 ValueText fields wired |
| Click **Close** | Returns to MainMenu. | CloseButton wired; SettingsUI.OnClose runs |
| Click **New Game** | Fades to Lobby. profile.json gets deleted first. | NewGameButton wired; Lobby in Build Settings |
| Lobby loads | Player spawns, NPCs visible (Banker + Shopkeeper). | LobbyManager prefab present |
| Walk to `Portal_ToDungeon` | "Press [F] to enter Forsaken Keep" prompt appears. | Sphere Collider radius; PlayerInteractor on Player |
| Press F | Inventory saves; fades to Dungeon. | ScenePortal wired; Dungeon in Build Settings |
| Dungeon loads | Player spawns at start. Enemies in zones. | DungeonManager spawn point set |
| Press **Esc** | PausePanel appears, time freezes, cursor unlocks. | PauseController in scene; PausePanel wired |
| Click Settings (in Pause) | Dungeon-scene SettingsPanel overlays. Sliders work. | Dungeon `[Settings]` GameObject + SettingsUI wired |
| Press Esc → Click **Resume** | Game unpauses; cursor relocks. | ResumeButton wired |
| Take damage until death | After 1s black, GameOverPanel appears. | GameOverScreen wired; PlayerStats.OnDeath fires |
| Click **Retry** | Dungeon reloads; player respawns at start. | RetryButton wired; SceneLoader handles current scene name |
| Die again → Click **Return to Lobby** | Save fires; fades to Lobby. Last bank/inventory preserved. | LobbyButton wired |
| Re-enter Dungeon → Pause → **Quit to Desktop** | Save fires; Play mode exits. | QuitButton wired |
| Press Play again from `0_LandingScene` | Landing → MainMenu. **Continue** is now enabled (save exists). | profile.json was created; MainMenuController checks `SaveSystem.HasProfile` |

**3. Common test failures:**

- **"Continue" never enables** → SaveSystem isn't writing profile.json. Check Console for `[Save]` log on each scene transition.
- **Settings changes don't persist between scenes** → SettingsManager uses PlayerPrefs (auto-saves). If they reset, check that `OnEnable` reads from PlayerPrefs.
- **Pause Esc doesn't open in Lobby** → PauseController is Dungeon-only by default. Add it to Lobby per §10d's last paragraph if you want lobby-pause.
- **Bank/inventory empty after retry** → `InventoryPersistence.LoadOnAwake` not running. Confirm `[Inventory]` GameObject exists in the Dungeon scene with InventoryPersistence component.

**4. Final commit checkpoint:**

If all 19 rows pass, M10 is complete. Recommended commit message:
```
M10 complete — full menu flow (Main Menu, Pause, Game Over, scene portals)
```

### 10i — Tuning + extending

| Goal                                                         | Knob                                                                                            |
| ------------------------------------------------------------ | ----------------------------------------------------------------------------------------------- |
| Faster fade transitions                                      | Lower `Default Fade Duration` on FadeLoader.                                                    |
| Settings open faster from anywhere                           | Add a Settings shortcut key in `PlayerInputActions.cs`.                                         |
| Multi-slot saves                                             | Phase 2 — extend `SaveSystem` to take a slot ID, expand MainMenu UI.                            |
| Auto-pause when window loses focus                           | In PauseController.Awake, hook `Application.focusChanged += paused => { if (paused) Open(); }`. |
| Settings affect actual audio (not just AudioListener.volume) | Wire `AudioMixer` exposed parameters in M9 polish.                                              |

## Phase 1 complete

All 10 milestones are now in place. From here, **M9 polish** is the remaining work:

- HUD (HP/stamina/EXP bars, gold readout, ammo count, boss HP bar)
- Audio (combat SFX, ambient music, UI clicks, footsteps)
- VFX (hit particles, level-up flash, gold sparkle, telegraph indicators)
- Camera shake on heavy impacts
- Real models replacing placeholder cubes
- Item icons in inventory
- Item rarity tiers

## Save data location

`%AppData%\..\LocalLow\<CompanyName>\<ProductName>\DungeonBlade\`

- `profile.json` — level, EXP, gold, owned items
- `bank.json` — vault contents, stored gold, dungeon tokens

## Troubleshooting

- **Compile errors about `UnityEngine.InputSystem`** — Unity hasn't finished resolving the package yet. Wait for the package import to complete, or open `Window > Package Manager` and confirm Input System is installed.
- **Player falls through floor** — make sure your test ground plane has a Collider (planes do by default; primitives do).
- **Build Settings is empty** — run `DungeonBlade > Build Settings > Sync Scenes` from the menu bar.
