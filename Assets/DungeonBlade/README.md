# DungeonBlade — Unity Implementation

A fully-wired Unity implementation of the DungeonBlade GDD v1.0. Designed so the **only things you need to do** are:

1. Import your character model.
2. Drop your animation clips onto the pre-built Animator Controllers.

Everything else — scenes, dungeon geometry, prefabs, UI, items, loot, save system — is built for you by a single menu click.

---

## 1. What's in the box

```
DungeonBlade/
├─ Scripts/
│  ├─ Core/           GameServices, GameManager
│  ├─ Player/         PlayerController, Stats, Combat, Skills  (K-style cancels, wall-run, dash i-frames)
│  ├─ Combat/         IDamageable, ComboCounter, Projectile
│  ├─ Enemies/        EnemyBase + SkeletonSoldier/Archer/ArmoredKnight/UndeadWarlord
│  ├─ Items/          ItemData hierarchy + WeaponData/ArmorData/ConsumableData
│  ├─ Inventory/      48-slot inventory + 9 equipment slots + 4-slot hotbar
│  ├─ Bank/           120-slot vault + shop + token exchange
│  ├─ Progression/    XP curve, level 20 cap, +HP/stam/SP per level
│  ├─ Loot/           LootTable with guaranteed/independent/weighted pools
│  ├─ Dungeon/        Checkpoints, SpikeTrap, FallPit, CollapsingFloor, ArrowWall, RewardChest, DungeonManager
│  ├─ Save/           JSON save/load with item registry
│  ├─ UI/             HUDManager, InventoryUI, BankUI, BankNPC
│  ├─ Runtime/        PlaceholderCharacterFX (procedural capsule animation), SceneTransitionTrigger
│  └─ Editor/         DBEditorMenu + 5 builders (content, animators, prefabs, HUD, scenes)
└─ Docs/              (empty — drop design notes here)
```

Roughly **6,500 lines** across 38 files.

---

## 2. Setup (2 minutes)

1. **Copy the entire `DungeonBlade/` folder into `Assets/` in your Unity 2022 LTS project.**
2. **Install TextMeshPro** if prompted (Window → TextMeshPro → Import TMP Essential Resources).
3. **Install the AI Navigation package** (Window → Package Manager → Unity Registry → "AI Navigation").
4. In the Unity menu, click **DungeonBlade → Build Everything**.
5. Open `Assets/DungeonBladeSample/Scenes/Dungeon_ForsakenKeep.unity`.
6. **Bake the NavMesh:** Window → AI → Navigation → Bake tab → Bake.
7. Press **Play**.

You'll control a blue capsule with a glowing sword, fight skeleton capsules, cross a wall-run bridge, face an elite knight, and finally fight a big red boss capsule that has 3 phases. Every GDD mechanic — K-style slash-to-fire cancel, wall-run, dash i-frames, inventory, save/load, boss phases — is live.

---

## 3. Replacing placeholders with your real assets

### Your character model

1. Import your rigged character (`.fbx` or `.blend`) into the project.
2. Open `Assets/DungeonBladeSample/Prefabs/Player.prefab` in Prefab Mode.
3. Select the `Visual` child GameObject. It contains: `Body`, `Head`, `FaceMarker`, `LeftLeg/RightLeg`, `LeftArm/RightArm`, `SwordPivot` (inside RightArm), `GunPivot` (inside RightArm).
4. Delete the placeholder primitive children of `Visual` (keep the pivots for now).
5. Drag your character model in as a child of `Visual`.
6. On the Player root, **remove the `PlaceholderCharacterFX` component** — it's only for the capsules.
7. Confirm the `Animator` component on Player root is still pointing at `Animators/Player.controller`.
8. In the `PlayerCombat` component, reassign `Sword Pivot` and `Gun Pivot` to weapon-attach transforms on your model's hand bones.

Repeat for `SkeletonSoldier.prefab`, `SkeletonArcher.prefab`, `ArmoredKnight.prefab`, and `UndeadWarlord.prefab`. Each has a `Visual` child, a placeholder Animator already linked to the right controller, and an `EyePoint` transform for sightline raycasts.

### Your animation clips

The Animator Controllers (`Assets/DungeonBladeSample/Animators/`) are pre-built with every state and parameter the gameplay code sets:

| Controller | States | Parameters |
|---|---|---|
| **Player.controller** | Idle, Locomotion, Jump, SwordSlash, SwordHeavy, Blocking, Fire, Reload, Dash | `Speed` (float), `WeaponMode`/`ComboStep` (int), `Blocking`/`ADS`/`Airborne`/`Dashing` (bool), triggers: `LightSlash`, `HeavySlash`, `Fire`, `Reload`, `Jump` |
| **Enemy.controller** | Idle, Run, Attack, Stagger, Death | `Speed`, `State`, triggers: `Attack`, `Stagger`, `Death`, `Slam`, `ShieldBash` |
| **Boss_UndeadWarlord.controller** | Idle, Run, Combo, Stomp, Shockwave, Summon, BoneThrow, Death, PhaseTransition | `Speed` (float), `State`/`ComboStep` (int), `Enraged` (bool), triggers: `Combo`, `Stomp`, `Shockwave`, `Summon`, `BoneThrow`, `Death`, `PhaseTransition` |

Open each controller in the Animator window and drag your motion clips onto the corresponding states. **You don't add transitions or parameters** — they're already there. Transitions use triggers (set by code) plus exit-time returns to Idle; if a clip is a different length than the preset exit time, tweak the transition's Exit Time value.

### Mixamo-specific tip

Since you're already working on Mixamo rig fixes on DungeonBlade: Mixamo clips come in with root motion baked in, so set each clip's `Animation Type` to `Humanoid` and your character prefab's Animator `Apply Root Motion` to true. The `PlayerController` uses a `CharacterController` (not Rigidbody) and applies horizontal movement directly, so you'll want to disable root motion on the X/Z axes only or strip it from the clips — otherwise root motion fights the input-driven movement.

---

## 4. Menu reference

All under **DungeonBlade** in the Unity menu bar.

- **Build Everything** — runs every builder in sequence. Use this first, and when you want to regenerate everything.
- **Sub-Builders / Sample Items Only** — regenerates weapon/armor/consumable ScriptableObjects + loot tables.
- **Sub-Builders / Animator Controllers Only** — regenerates the 3 animator controllers. ⚠ Wipes any clips you've assigned.
- **Sub-Builders / Prefabs Only** — regenerates Player/enemy/NPC/chest prefabs.
- **Sub-Builders / Lobby Scene Only** — rebuilds `Lobby.unity`.
- **Sub-Builders / Dungeon Scene Only** — rebuilds `Dungeon_ForsakenKeep.unity`.
- **Open Save Folder** — reveals `Application.persistentDataPath` in Finder/Explorer.
- **Delete Sample Content** — nukes `Assets/DungeonBladeSample/` so you can start clean.

**Do not** run the Animator Controllers sub-builder after assigning clips — it'll wipe them.

---

## 5. Input (default Unity Input Manager axes)

| Action | Key/Button |
|---|---|
| Move | WASD |
| Look | Mouse |
| Jump | Space |
| Dash | Left Shift (directional from movement input) |
| Light Attack | Left Mouse Button |
| Heavy Attack | Hold LMB |
| Fire (gun mode) | LMB |
| ADS (gun mode) | RMB |
| Block/Parry | RMB (sword mode) |
| Reload | R |
| Switch Weapon | Q |
| Skill 1 — Blade Dash | E |
| Skill 2 — Shotgun Burst | G |
| Skill 3 — Smoke Grenade | V |
| Skill 4 — Counter Strike | (auto on parry) |
| Skill 5 — Battle Roll | C |
| Skill 6 — Iron Skin | F |
| Inventory | I or Tab |
| Bank (at NPC) | E when prompt shows |

---

## 6. Architecture notes

- **Service locator:** `DungeonBlade.Core.GameServices.Inventory` / `.Bank` / `.Save` / `.Progression` / `.Dungeon`. Systems resolve each other lazily — easy to test scenes in isolation.
- **Events everywhere:** `PlayerStats.OnDamaged`, `PlayerCombat.OnComboStep`/`OnParrySuccess`/`OnWeaponSwitched`, `PlayerSkills.OnSkillUsed`, `DungeonManager.OnDungeonClear`, `OnBossDefeated`. Wire FMOD/Wwise and VFX on these — you won't modify gameplay scripts.
- **ScriptableObject items:** Designers add gear in the Project window via `Create → DungeonBlade → Weapon` (or Armor/Consumable). Remember to add new items to `SaveSystem.itemRegistry` (on the `_GameBootstrap` object) so they serialize.
- **K-style cancel window:** `PlayerCombat.slashToFireCancelWindow` — tune for desired feel. 0.15s tight, 0.45s forgiving.
- **Dungeon geometry:** the generated `Dungeon_ForsakenKeep.unity` is intentional gray-box. When you're ready for art, replace the `Z1_*`, `Z2_*`, etc. cubes with your actual meshes. All script references persist.

---

## 7. Known limitations

- **NavMesh doesn't bake automatically.** Unity doesn't expose NavMesh baking cleanly to editor scripts. Bake manually each time the dungeon scene changes (Window → AI → Navigation → Bake).
- **No motion clips on animators.** The states are there — drop your clips in.
- **Shader fallback.** Placeholder materials use URP Lit, falling back to Standard. If you're on HDRP, the capsules will appear pink — reassign materials.
- **Minimal Bank UI.** The generated bank canvas is a plain dark panel. Build out the tabbed shop/vault UI when you're ready — `BankSystem` already exposes all the needed API.
- **No item icons.** `ItemData.icon` is a Sprite field — assign sprites per item for the inventory to show anything more than solid squares.

---

## 8. Multi-character selection (added in latest build)

DungeonBlade supports a character select screen where the player picks from any number of models. The system is built around four files in `Scripts/Characters/`:

- `CharacterData` — ScriptableObject holding a character's name, model prefab, portrait, stat multipliers, and weapon-attach bone info.
- `CharacterRoster` — persistent list of all available characters plus the currently-selected one. Persists the selection to PlayerPrefs.
- `CharacterInstantiator` — runtime component on the Player prefab that instantiates the selected model in place of the placeholder capsules, retargets the Animator avatar, and parents the sword/gun to the chosen hand bones.
- `CharacterSelectUI` — the in-lobby UI, auto-opens on first entry to the Lobby scene.
- `CharacterChangeNPC` — blue capsule NPC in the Lobby; walk up to it and press E to reopen character select.

### Setup (one-click)

1. Put your character model FBX files in a folder (e.g. your unzipped `Player Model/` download).
2. In Unity: **DungeonBlade → Characters → Build Roster From Folder...**
3. Pick the folder containing your FBX files.
4. The builder auto-imports each, configures them as Humanoid, and creates a `CharacterData` asset for each one.
5. Run **DungeonBlade → Sub-Builders → Lobby Scene Only** to rebuild the lobby with the updated roster.
6. Press Play in Lobby — the character select screen opens automatically on first launch.

### How it works at runtime

- On first launch, `CharacterSelectUI` detects no selection exists and opens automatically.
- When the player clicks a portrait and hits Confirm, the selection is saved to PlayerPrefs under `DungeonBlade.SelectedCharacterId`.
- When the dungeon scene loads, the Player prefab spawns with its capsule visual, then `CharacterInstantiator.Awake()` reads the roster, instantiates the selected model under the `Visual` transform, clears the placeholder capsules, and retargets the Animator.
- The sword and gun get reparented to `mixamorig:RightHand` (standard Mixamo hand bone name) with configurable offsets.
- Stat multipliers (HP, stamina, speed, damage) are applied to the Player's components on spawn.

### Adding a character manually

Right-click in Project → **Create → DungeonBlade → Character**. Fill in:
- **Character ID** — unique string like `char_sorceress`
- **Display Name** — shown in UI
- **Model Prefab** — drag your rigged character FBX here
- **Portrait** — optional sprite for UI
- **Model Rotation** — usually `(0, 0, 0)` for Mixamo rigs (they face +Z at rest). Use `(0, 180, 0)` if your model faces backward.
- **Sword/Gun Attach Bone Name** — usually `mixamorig:RightHand`. Check your model's hierarchy if the naming is different.
- **Stat multipliers** — leave at 1.0 for balanced, tune for class variety (e.g. 1.3 HP, 0.9 speed for a tank character).

Then drag the new `CharacterData` asset into the `CharacterRoster.characters` list on the `_GameBootstrap` object in both Lobby and Dungeon scenes.

### Bone name reference

Mixamo standard names (you'll see these in the Hierarchy under your imported character):
- `mixamorig:Hips` — root
- `mixamorig:Spine`, `mixamorig:Spine1`, `mixamorig:Spine2`
- `mixamorig:LeftShoulder`, `mixamorig:LeftArm`, `mixamorig:LeftForeArm`, `mixamorig:LeftHand`
- `mixamorig:RightShoulder`, `mixamorig:RightArm`, `mixamorig:RightForeArm`, `mixamorig:RightHand` ← sword/gun attach here

Some Blender exports strip the `mixamorig:` prefix. If your bone is just `RightHand` with no prefix, update the CharacterData's `Sword Attach Bone Name` field accordingly. The code does a recursive name search so any valid transform name works.
