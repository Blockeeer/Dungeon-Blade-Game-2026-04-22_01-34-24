#if UNITY_EDITOR
using UnityEngine;
using UnityEngine.AI;
using UnityEditor;
using UnityEditor.Animations;
using DungeonBlade.Player;
using DungeonBlade.Enemies;
using DungeonBlade.Combat;
using DungeonBlade.Dungeon;
using DungeonBlade.Runtime;

namespace DungeonBlade.EditorTools
{
    /// <summary>
    /// Builds all gameplay prefabs with placeholder capsule meshes so the
    /// game is instantly playable. User replaces the visual children with
    /// their own model + animator — all the script wiring stays intact.
    /// </summary>
    public static class DBPrefabBuilder
    {
        public static void BuildAll()
        {
            BuildPlayerPrefab();
            BuildEnemyPrefab("SkeletonSoldier",  Color.gray,  typeof(SkeletonSoldier),  "skeletonSoldier");
            BuildEnemyPrefab("SkeletonArcher",   new Color(0.7f,0.7f,0.9f), typeof(SkeletonArcher), "skeletonArcher");
            BuildEnemyPrefab("ArmoredKnight",    new Color(0.3f,0.3f,0.4f), typeof(ArmoredKnight),  "armoredKnight");
            BuildBossPrefab();
            BuildBankNPCPrefab();
            BuildRewardChestPrefab();
            BuildArrowProjectilePrefab();
            BuildBoneProjectilePrefab();
        }

        // ────────── Player ──────────
        static void BuildPlayerPrefab()
        {
            // Root
            var root = new GameObject("Player");
            root.tag = "Player";
            var cc = root.AddComponent<CharacterController>();
            cc.height = 1.7f; cc.radius = 0.4f; cc.center = new Vector3(0, 0.85f, 0);

            // Visual body under a "Visual" pivot
            var visual = new GameObject("Visual");
            visual.transform.SetParent(root.transform, false);

            // Body capsule
            var body = GameObject.CreatePrimitive(PrimitiveType.Capsule);
            body.name = "Body";
            UnityEngine.Object.DestroyImmediate(body.GetComponent<Collider>());
            body.transform.SetParent(visual.transform, false);
            body.transform.localPosition = new Vector3(0, 0.85f, 0);
            body.transform.localScale = new Vector3(0.7f, 0.85f, 0.7f);
            SetColor(body, new Color(0.25f, 0.45f, 0.85f));

            // Head (so you can see facing direction)
            var head = GameObject.CreatePrimitive(PrimitiveType.Sphere);
            head.name = "Head"; UnityEngine.Object.DestroyImmediate(head.GetComponent<Collider>());
            head.transform.SetParent(visual.transform, false);
            head.transform.localPosition = new Vector3(0, 1.65f, 0);
            head.transform.localScale = Vector3.one * 0.35f;
            SetColor(head, new Color(0.9f, 0.8f, 0.7f));

            // Face marker (small cube in front) so direction is obvious
            var face = GameObject.CreatePrimitive(PrimitiveType.Cube);
            face.name = "FaceMarker"; UnityEngine.Object.DestroyImmediate(face.GetComponent<Collider>());
            face.transform.SetParent(visual.transform, false);
            face.transform.localPosition = new Vector3(0, 1.68f, 0.18f);
            face.transform.localScale = new Vector3(0.25f, 0.08f, 0.05f);
            SetColor(face, new Color(0.4f, 0.9f, 1f));

            // Legs
            var leftLeg  = MakeLimb(visual.transform, "LeftLeg",  new Vector3(-0.15f, 0.4f, 0), 0.12f, 0.4f, new Color(0.2f,0.2f,0.3f));
            var rightLeg = MakeLimb(visual.transform, "RightLeg", new Vector3( 0.15f, 0.4f, 0), 0.12f, 0.4f, new Color(0.2f,0.2f,0.3f));
            var leftArm  = MakeLimb(visual.transform, "LeftArm",  new Vector3(-0.38f, 1.1f, 0), 0.1f,  0.3f, new Color(0.25f,0.45f,0.85f));
            var rightArm = MakeLimb(visual.transform, "RightArm", new Vector3( 0.38f, 1.1f, 0), 0.1f,  0.3f, new Color(0.25f,0.45f,0.85f));

            // Sword (visible child)
            var swordPivot = new GameObject("SwordPivot");
            swordPivot.transform.SetParent(rightArm.transform, false);
            swordPivot.transform.localPosition = new Vector3(0, -0.2f, 0.3f);
            var blade = GameObject.CreatePrimitive(PrimitiveType.Cube);
            blade.name = "SwordBlade"; UnityEngine.Object.DestroyImmediate(blade.GetComponent<Collider>());
            blade.transform.SetParent(swordPivot.transform, false);
            blade.transform.localPosition = new Vector3(0, 0.45f, 0);
            blade.transform.localScale = new Vector3(0.08f, 0.9f, 0.02f);
            SetColor(blade, new Color(0.85f, 0.9f, 1f), emissive: new Color(0.4f, 0.6f, 1f));

            // Gun
            var gunPivot = new GameObject("GunPivot");
            gunPivot.transform.SetParent(rightArm.transform, false);
            gunPivot.transform.localPosition = new Vector3(0, -0.2f, 0.3f);
            var gunBody = GameObject.CreatePrimitive(PrimitiveType.Cube);
            gunBody.name = "GunBody"; UnityEngine.Object.DestroyImmediate(gunBody.GetComponent<Collider>());
            gunBody.transform.SetParent(gunPivot.transform, false);
            gunBody.transform.localPosition = new Vector3(0, 0, 0.12f);
            gunBody.transform.localScale = new Vector3(0.08f, 0.12f, 0.38f);
            SetColor(gunBody, new Color(0.15f, 0.15f, 0.2f));
            gunPivot.SetActive(false);

            // Camera pivot
            var camPivot = new GameObject("CameraPivot");
            camPivot.transform.SetParent(root.transform, false);
            camPivot.transform.localPosition = new Vector3(0, 1.5f, 0);

            var camGO = new GameObject("Main Camera");
            camGO.tag = "MainCamera";
            camGO.transform.SetParent(camPivot.transform, false);
            camGO.transform.localPosition = new Vector3(0.4f, 0.4f, -3.5f);
            var cam = camGO.AddComponent<Camera>();
            cam.fieldOfView = 75;
            camGO.AddComponent<AudioListener>();

            // Scripts
            var pc      = root.AddComponent<PlayerController>();
            pc.cameraPivot = camPivot.transform;
            var ps      = root.AddComponent<PlayerStats>();
            var combo   = root.AddComponent<ComboCounter>();
            var combat  = root.AddComponent<PlayerCombat>();
            combat.cameraTransform = camGO.transform;
            combat.comboCounter = combo;
            var skills  = root.AddComponent<PlayerSkills>();

            // Animator (for future)
            var animator = root.AddComponent<Animator>();
            var ac = AssetDatabase.LoadAssetAtPath<AnimatorController>(DBEditorMenu.AnimatorsPath + "/Player.controller");
            if (ac != null) animator.runtimeAnimatorController = ac;
            combat.animator = animator;

            // Placeholder FX (so capsule actually animates)
            var fx = root.AddComponent<PlaceholderCharacterFX>();
            fx.body = body.transform;
            fx.leftLeg = leftLeg.transform; fx.rightLeg = rightLeg.transform;
            fx.leftArm = leftArm.transform; fx.rightArm = rightArm.transform;
            fx.swordPivot = swordPivot.transform;
            fx.gunPivot = gunPivot.transform;
            fx.tintable = new Renderer[] {
                body.GetComponent<Renderer>(), head.GetComponent<Renderer>(),
                leftLeg.GetComponent<Renderer>(), rightLeg.GetComponent<Renderer>(),
                leftArm.GetComponent<Renderer>(), rightArm.GetComponent<Renderer>()
            };

            // Character instantiator — swaps in the selected CharacterData's model at runtime
            var instantiator = root.AddComponent<DungeonBlade.Characters.CharacterInstantiator>();
            instantiator.visualParent = visual.transform;
            instantiator.animator = animator;
            instantiator.swordObject = swordPivot.transform;
            instantiator.gunObject = gunPivot.transform;
            instantiator.leavePlaceholderIfNoSelection = true;

            SavePrefab(root, DBEditorMenu.PrefabsPath + "/Player.prefab");
        }

        // ────────── Enemy (generic) ──────────
        static void BuildEnemyPrefab(string name, Color tint, System.Type enemyType, string animOverride)
        {
            var root = new GameObject(name);
            root.layer = 0;

            var cc = root.AddComponent<CapsuleCollider>();
            cc.height = 1.7f; cc.radius = 0.4f; cc.center = new Vector3(0, 0.85f, 0);

            var agent = root.AddComponent<NavMeshAgent>();
            agent.height = 1.7f; agent.radius = 0.4f; agent.speed = 3.5f;
            agent.stoppingDistance = 1.8f;

            var visual = new GameObject("Visual");
            visual.transform.SetParent(root.transform, false);

            var body = GameObject.CreatePrimitive(PrimitiveType.Capsule);
            body.name = "Body"; UnityEngine.Object.DestroyImmediate(body.GetComponent<Collider>());
            body.transform.SetParent(visual.transform, false);
            body.transform.localPosition = new Vector3(0, 0.85f, 0);
            body.transform.localScale = new Vector3(0.7f, 0.85f, 0.7f);
            SetColor(body, tint);

            var head = GameObject.CreatePrimitive(PrimitiveType.Sphere);
            head.name = "Head"; UnityEngine.Object.DestroyImmediate(head.GetComponent<Collider>());
            head.transform.SetParent(visual.transform, false);
            head.transform.localPosition = new Vector3(0, 1.65f, 0);
            head.transform.localScale = Vector3.one * 0.35f;
            SetColor(head, tint * 0.8f);

            var eye = GameObject.CreatePrimitive(PrimitiveType.Cube);
            eye.name = "Eyes"; UnityEngine.Object.DestroyImmediate(eye.GetComponent<Collider>());
            eye.transform.SetParent(visual.transform, false);
            eye.transform.localPosition = new Vector3(0, 1.68f, 0.18f);
            eye.transform.localScale = new Vector3(0.3f, 0.08f, 0.05f);
            SetColor(eye, new Color(1f, 0.2f, 0.2f), emissive: new Color(1f, 0.1f, 0.1f));

            var eyePoint = new GameObject("EyePoint");
            eyePoint.transform.SetParent(root.transform, false);
            eyePoint.transform.localPosition = new Vector3(0, 1.6f, 0.2f);

            // Health bar
            var hbCanvas = new GameObject("HealthBar");
            hbCanvas.transform.SetParent(root.transform, false);
            hbCanvas.transform.localPosition = new Vector3(0, 2.3f, 0);
            var c = hbCanvas.AddComponent<Canvas>(); c.renderMode = RenderMode.WorldSpace;
            hbCanvas.AddComponent<UnityEngine.UI.CanvasScaler>();
            var rt = hbCanvas.GetComponent<RectTransform>();
            rt.sizeDelta = new Vector2(1.2f, 0.12f);
            rt.localScale = Vector3.one * 0.01f;
            var bg = new GameObject("BG"); bg.transform.SetParent(hbCanvas.transform, false);
            var bgImg = bg.AddComponent<UnityEngine.UI.Image>(); bgImg.color = new Color(0,0,0,0.7f);
            var bgRT = bg.GetComponent<RectTransform>(); bgRT.anchorMin = Vector2.zero; bgRT.anchorMax = Vector2.one; bgRT.sizeDelta = Vector2.zero;
            var fill = new GameObject("Fill"); fill.transform.SetParent(hbCanvas.transform, false);
            var fillImg = fill.AddComponent<UnityEngine.UI.Image>(); fillImg.color = new Color(0.9f, 0.2f, 0.3f);
            fillImg.type = UnityEngine.UI.Image.Type.Filled;
            fillImg.fillMethod = UnityEngine.UI.Image.FillMethod.Horizontal;
            var fillRT = fill.GetComponent<RectTransform>(); fillRT.anchorMin = Vector2.zero; fillRT.anchorMax = Vector2.one; fillRT.sizeDelta = Vector2.zero;

            // Add enemy script (polymorphic)
            var enemy = (EnemyBase)root.AddComponent(enemyType);
            enemy.eyePoint = eyePoint.transform;

            // Animator
            var animator = root.AddComponent<Animator>();
            var acPath = (animOverride == "boss") ? "/Boss_UndeadWarlord.controller" : "/Enemy.controller";
            var ac = AssetDatabase.LoadAssetAtPath<AnimatorController>(DBEditorMenu.AnimatorsPath + acPath);
            if (ac != null) animator.runtimeAnimatorController = ac;
            enemy.animator = animator;

            // Try to load loot table by name
            string lootName = name switch {
                "SkeletonSoldier" => "Loot_SkeletonSoldier",
                "SkeletonArcher"  => "Loot_SkeletonArcher",
                "ArmoredKnight"   => "Loot_ArmoredKnight",
                _ => null,
            };
            if (lootName != null)
            {
                var lt = AssetDatabase.LoadAssetAtPath<DungeonBlade.Loot.LootTable>(DBEditorMenu.LootPath + "/" + lootName + ".asset");
                if (lt != null) enemy.lootTable = lt;
            }

            SavePrefab(root, DBEditorMenu.PrefabsPath + "/" + name + ".prefab");
        }

        // ────────── Boss ──────────
        static void BuildBossPrefab()
        {
            var root = new GameObject("UndeadWarlord");
            var cc = root.AddComponent<CapsuleCollider>();
            cc.height = 2.6f; cc.radius = 0.6f; cc.center = new Vector3(0, 1.3f, 0);
            var agent = root.AddComponent<NavMeshAgent>();
            agent.height = 2.6f; agent.radius = 0.6f; agent.speed = 3.2f; agent.stoppingDistance = 2.5f;

            var visual = new GameObject("Visual");
            visual.transform.SetParent(root.transform, false);
            var body = GameObject.CreatePrimitive(PrimitiveType.Capsule);
            body.name = "Body"; UnityEngine.Object.DestroyImmediate(body.GetComponent<Collider>());
            body.transform.SetParent(visual.transform, false);
            body.transform.localPosition = new Vector3(0, 1.3f, 0);
            body.transform.localScale = new Vector3(1.1f, 1.3f, 1.1f);
            SetColor(body, new Color(0.35f, 0.12f, 0.18f));

            var head = GameObject.CreatePrimitive(PrimitiveType.Sphere);
            head.name = "Head"; UnityEngine.Object.DestroyImmediate(head.GetComponent<Collider>());
            head.transform.SetParent(visual.transform, false);
            head.transform.localPosition = new Vector3(0, 2.5f, 0);
            head.transform.localScale = Vector3.one * 0.5f;
            SetColor(head, new Color(0.35f, 0.12f, 0.18f));

            var eye = GameObject.CreatePrimitive(PrimitiveType.Cube);
            eye.name = "Eyes"; UnityEngine.Object.DestroyImmediate(eye.GetComponent<Collider>());
            eye.transform.SetParent(visual.transform, false);
            eye.transform.localPosition = new Vector3(0, 2.55f, 0.25f);
            eye.transform.localScale = new Vector3(0.45f, 0.09f, 0.06f);
            SetColor(eye, new Color(1f, 0.2f, 0.1f), emissive: new Color(1f, 0.1f, 0.05f));

            var eyePoint = new GameObject("EyePoint");
            eyePoint.transform.SetParent(root.transform, false);
            eyePoint.transform.localPosition = new Vector3(0, 2.4f, 0.3f);

            var projSpawn = new GameObject("ProjectileSpawn");
            projSpawn.transform.SetParent(root.transform, false);
            projSpawn.transform.localPosition = new Vector3(0, 1.8f, 0.8f);

            var chestSpawn = new GameObject("ChestSpawnPoint");
            chestSpawn.transform.SetParent(root.transform, false);
            chestSpawn.transform.localPosition = new Vector3(0, 0.5f, 0);

            var boss = root.AddComponent<UndeadWarlord>();
            boss.eyePoint = eyePoint.transform;
            boss.projectileSpawnPoint = projSpawn.transform;
            boss.chestSpawnPoint = chestSpawn.transform;
            boss.summonOffsets = new[] { new Vector3(4, 0, 4), new Vector3(-4, 0, 4) };

            var animator = root.AddComponent<Animator>();
            var ac = AssetDatabase.LoadAssetAtPath<AnimatorController>(DBEditorMenu.AnimatorsPath + "/Boss_UndeadWarlord.controller");
            if (ac != null) animator.runtimeAnimatorController = ac;
            boss.animator = animator;

            // Prefab links we'll connect up after they're saved
            var skeletonPrefab = AssetDatabase.LoadAssetAtPath<GameObject>(DBEditorMenu.PrefabsPath + "/SkeletonSoldier.prefab");
            if (skeletonPrefab != null) boss.skeletonSoldierPrefab = skeletonPrefab;
            var bonePrefab = AssetDatabase.LoadAssetAtPath<GameObject>(DBEditorMenu.PrefabsPath + "/BoneProjectile.prefab");
            if (bonePrefab != null) boss.boneProjectilePrefab = bonePrefab;
            var chestPrefab = AssetDatabase.LoadAssetAtPath<GameObject>(DBEditorMenu.PrefabsPath + "/RewardChest.prefab");
            if (chestPrefab != null) boss.rewardChestPrefab = chestPrefab;

            SavePrefab(root, DBEditorMenu.PrefabsPath + "/UndeadWarlord.prefab");
        }

        // ────────── Bank NPC ──────────
        static void BuildBankNPCPrefab()
        {
            var root = new GameObject("BankNPC");
            var body = GameObject.CreatePrimitive(PrimitiveType.Capsule);
            body.transform.SetParent(root.transform, false);
            body.transform.localPosition = new Vector3(0, 1f, 0);
            SetColor(body, new Color(0.9f, 0.85f, 0.3f), emissive: new Color(0.3f, 0.25f, 0.05f));

            // Permanent floating sign so the player can find the NPC across the room.
            var sign = new GameObject("Sign");
            sign.transform.SetParent(root.transform, false);
            sign.transform.localPosition = new Vector3(0, 2.7f, 0);
            var signText = sign.AddComponent<TextMesh>();
            signText.text = "BANK";
            signText.fontSize = 80;
            signText.alignment = TextAlignment.Center;
            signText.anchor = TextAnchor.MiddleCenter;
            signText.characterSize = 0.06f;
            signText.color = new Color(1f, 0.9f, 0.4f);
            sign.AddComponent<DungeonBlade.Runtime.Billboard>();

            // "Press E" prompt — hidden by default, BankNPC.cs toggles when in range.
            var prompt = new GameObject("Prompt");
            prompt.transform.SetParent(root.transform, false);
            prompt.transform.localPosition = new Vector3(0, 2.2f, 0);
            var promptText = prompt.AddComponent<TextMesh>();
            promptText.text = "Press E to interact";
            promptText.fontSize = 50;
            promptText.alignment = TextAlignment.Center;
            promptText.anchor = TextAnchor.MiddleCenter;
            promptText.characterSize = 0.05f;
            promptText.color = new Color(0.85f, 0.95f, 1f);
            prompt.AddComponent<DungeonBlade.Runtime.Billboard>();
            prompt.SetActive(false);

            var npc = root.AddComponent<DungeonBlade.UI.BankNPC>();
            npc.promptUI = prompt;
            // bankSystem / bankUI references are set by scene builder
            SavePrefab(root, DBEditorMenu.PrefabsPath + "/BankNPC.prefab");
        }

        // ────────── Reward Chest ──────────
        static void BuildRewardChestPrefab()
        {
            var root = new GameObject("RewardChest");
            var body = GameObject.CreatePrimitive(PrimitiveType.Cube);
            body.transform.SetParent(root.transform, false);
            body.transform.localScale = new Vector3(1.2f, 0.8f, 0.8f);
            body.transform.localPosition = new Vector3(0, 0.4f, 0);
            SetColor(body, new Color(0.8f, 0.55f, 0.15f), emissive: new Color(0.3f, 0.15f, 0f));

            var lid = GameObject.CreatePrimitive(PrimitiveType.Cube);
            lid.transform.SetParent(root.transform, false);
            lid.transform.localScale = new Vector3(1.25f, 0.25f, 0.85f);
            lid.transform.localPosition = new Vector3(0, 0.9f, 0);
            SetColor(lid, new Color(0.6f, 0.35f, 0.1f));

            var chest = root.AddComponent<RewardChest>();
            var lt = AssetDatabase.LoadAssetAtPath<DungeonBlade.Loot.LootTable>(DBEditorMenu.LootPath + "/Loot_Boss_ForsakenKeep.asset");
            if (lt != null) chest.lootTable = lt;

            SavePrefab(root, DBEditorMenu.PrefabsPath + "/RewardChest.prefab");
        }

        // ────────── Projectiles ──────────
        static void BuildArrowProjectilePrefab()
        {
            var root = GameObject.CreatePrimitive(PrimitiveType.Cube);
            root.name = "ArrowProjectile";
            UnityEngine.Object.DestroyImmediate(root.GetComponent<Collider>());
            root.transform.localScale = new Vector3(0.05f, 0.05f, 0.4f);
            SetColor(root, new Color(0.7f, 0.5f, 0.2f));
            root.AddComponent<Projectile>();
            SavePrefab(root, DBEditorMenu.PrefabsPath + "/ArrowProjectile.prefab");
        }

        static void BuildBoneProjectilePrefab()
        {
            var root = GameObject.CreatePrimitive(PrimitiveType.Cube);
            root.name = "BoneProjectile";
            UnityEngine.Object.DestroyImmediate(root.GetComponent<Collider>());
            root.transform.localScale = new Vector3(0.12f, 0.12f, 0.45f);
            SetColor(root, new Color(0.95f, 0.92f, 0.8f), emissive: new Color(0.3f, 0.08f, 0.08f));
            root.AddComponent<Projectile>();
            SavePrefab(root, DBEditorMenu.PrefabsPath + "/BoneProjectile.prefab");
        }

        // ───── helpers ─────
        static GameObject MakeLimb(Transform parent, string name, Vector3 pos, float radius, float height, Color color)
        {
            var go = GameObject.CreatePrimitive(PrimitiveType.Capsule);
            go.name = name;
            UnityEngine.Object.DestroyImmediate(go.GetComponent<Collider>());
            go.transform.SetParent(parent, false);
            go.transform.localPosition = pos;
            go.transform.localScale = new Vector3(radius * 2f, height, radius * 2f);
            SetColor(go, color);
            return go;
        }

        static void SetColor(GameObject go, Color c, Color? emissive = null)
        {
            var r = go.GetComponent<Renderer>();
            if (r == null) return;
            r.sharedMaterial = DBMaterialHelper.Create(c, emissive);
        }

        static void SavePrefab(GameObject instance, string path)
        {
            var existing = AssetDatabase.LoadAssetAtPath<GameObject>(path);
            if (existing != null) AssetDatabase.DeleteAsset(path);
            PrefabUtility.SaveAsPrefabAsset(instance, path);
            UnityEngine.Object.DestroyImmediate(instance);
        }
    }
}
#endif
