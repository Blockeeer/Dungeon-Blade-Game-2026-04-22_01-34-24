#if UNITY_EDITOR
using UnityEngine;
using UnityEditor;
using DungeonBlade.Items;
using DungeonBlade.Loot;
using DungeonBlade.Bank;

namespace DungeonBlade.EditorTools
{
    /// <summary>
    /// Creates a full sample content set:
    ///   • Weapons at every rarity (swords + pistol + shotgun)
    ///   • Armor pieces (head/chest/legs/boots)
    ///   • Consumables (4 potions + grenade)
    ///   • Dungeon Clear Token
    ///   • Loot tables for each regular enemy, elite knight, and boss chest
    /// All prices match GDD 8.4 economy table.
    /// </summary>
    public static class DBContentBuilder
    {
        public static void BuildSampleItems()
        {
            // ───── Weapons ─────
            MakeSword("sword_common_rusty",     "Rusty Saber",        ItemRarity.Common,   meleeDmg: 18f, value: 150);
            MakeSword("sword_uncommon_steel",   "Steel Blade",        ItemRarity.Uncommon, meleeDmg: 26f, value: 600);
            MakeSword("sword_rare_silver",      "Silverlight Blade",  ItemRarity.Rare,     meleeDmg: 34f, value: 1500);
            MakeSword("sword_epic_voidedge",    "Voidedge",           ItemRarity.Epic,     meleeDmg: 44f, value: 3200,
                     special: "Lifesteal 10% of melee damage", specVal: 0.10f);
            MakeSword("sword_legendary_warlord","Warlord's Blade",    ItemRarity.Legendary,meleeDmg: 60f, value: 8000,
                     special: "Every 3rd hit explodes", specVal: 1f);

            MakePistol("gun_common_pistol",   "Flintlock Pistol", ItemRarity.Common,   dmg: 25f, mag: 8,  value: 200);
            MakePistol("gun_uncommon_pistol", "Iron Revolver",    ItemRarity.Uncommon, dmg: 32f, mag: 10, value: 700);
            MakeShotgun("gun_rare_shotgun",   "Blunderbuss",      ItemRarity.Rare,     dmg: 60f, mag: 4,  value: 1800);

            // ───── Armor ─────
            MakeArmor("armor_head_common_hood",    "Tattered Hood",   EquipmentSlot.Head,  ItemRarity.Common,   flat: 1f, hp: 5f, value: 120);
            MakeArmor("armor_chest_common_tunic",  "Leather Tunic",   EquipmentSlot.Chest, ItemRarity.Common,   flat: 3f, hp:10f, value: 200);
            MakeArmor("armor_legs_common_pants",   "Leather Pants",   EquipmentSlot.Legs,  ItemRarity.Common,   flat: 2f, hp: 5f, value: 150);
            MakeArmor("armor_boots_common_boots",  "Worn Boots",      EquipmentSlot.Boots, ItemRarity.Common,   flat: 1f, stam:5f,value: 100);
            MakeArmor("armor_chest_rare_plate",    "Knight's Plate",  EquipmentSlot.Chest, ItemRarity.Rare,     flat: 8f, hp:25f, pct: 0.10f, value: 1800);
            MakeArmor("armor_head_epic_helm",      "Warlord Helm",    EquipmentSlot.Head,  ItemRarity.Epic,     flat: 6f, hp:20f, pct: 0.08f, value: 2600);

            // ───── Consumables ─────
            MakeConsumable("pot_hp_small",    "Small Health Potion",  ItemRarity.Common, ConsumableEffect.RestoreHp,       50f, value: 25);
            MakeConsumable("pot_hp_large",    "Large Health Potion",  ItemRarity.Uncommon, ConsumableEffect.RestoreHp,      100f, value: 75);
            MakeConsumable("pot_stamina",     "Stamina Potion",       ItemRarity.Common, ConsumableEffect.RestoreStamina,  60f, value: 30);
            MakeConsumable("grenade_frag",    "Frag Grenade",         ItemRarity.Uncommon, ConsumableEffect.ThrowGrenade,    80f, value: 50);

            // ───── Token ─────
            var token = ScriptableObject.CreateInstance<ItemData>();
            token.itemId = "token_dungeon_clear";
            token.displayName = "Dungeon Clear Token";
            token.flavorText = "Proof of a dungeon cleared. Spend at the Bank NPC.";
            token.category = ItemCategory.Token;
            token.rarity = ItemRarity.Rare;
            token.stackable = true;
            token.maxStack = 99;
            token.baseValue = 0;
            SaveAsset(token, DBEditorMenu.ItemsPath + "/Token_DungeonClear.asset");
        }

        // ───── Helpers ─────
        static WeaponData MakeSword(string id, string name, ItemRarity rarity, float meleeDmg, int value,
                                    string special = null, float specVal = 0f)
        {
            var w = ScriptableObject.CreateInstance<WeaponData>();
            w.itemId = id; w.displayName = name; w.rarity = rarity;
            w.category = ItemCategory.Weapon;
            w.weaponType = WeaponType.Sword;
            w.meleeDamage = meleeDmg;
            w.attackSpeedMult = 1f;
            w.baseValue = value;
            if (!string.IsNullOrEmpty(special)) { w.hasSpecialProperty = true; w.specialPropertyDescription = special; w.specialPropertyValue = specVal; }
            SaveAsset(w, DBEditorMenu.ItemsPath + "/Weapon_" + id + ".asset");
            return w;
        }

        static WeaponData MakePistol(string id, string name, ItemRarity rarity, float dmg, int mag, int value)
        {
            var w = ScriptableObject.CreateInstance<WeaponData>();
            w.itemId = id; w.displayName = name; w.rarity = rarity;
            w.category = ItemCategory.Weapon;
            w.weaponType = WeaponType.Pistol;
            w.rangedDamage = dmg; w.fireRate = 6f; w.magSize = mag;
            w.reloadTime = 1.4f; w.spreadHip = 2f; w.spreadAds = 0.4f;
            w.baseValue = value;
            SaveAsset(w, DBEditorMenu.ItemsPath + "/Weapon_" + id + ".asset");
            return w;
        }

        static WeaponData MakeShotgun(string id, string name, ItemRarity rarity, float dmg, int mag, int value)
        {
            var w = ScriptableObject.CreateInstance<WeaponData>();
            w.itemId = id; w.displayName = name; w.rarity = rarity;
            w.category = ItemCategory.Weapon;
            w.weaponType = WeaponType.Shotgun;
            w.rangedDamage = dmg; w.fireRate = 1.5f; w.magSize = mag;
            w.reloadTime = 2.2f; w.spreadHip = 6f; w.spreadAds = 3f;
            w.baseValue = value;
            SaveAsset(w, DBEditorMenu.ItemsPath + "/Weapon_" + id + ".asset");
            return w;
        }

        static ArmorData MakeArmor(string id, string name, EquipmentSlot slot, ItemRarity rarity,
                                   float flat = 0, float pct = 0, float hp = 0, float stam = 0, int value = 100)
        {
            var a = ScriptableObject.CreateInstance<ArmorData>();
            a.itemId = id; a.displayName = name; a.rarity = rarity;
            a.category = ItemCategory.Armor;
            a.slot = slot;
            a.flatDefense = flat; a.percentDefense = pct;
            a.bonusMaxHp = hp; a.bonusMaxStamina = stam;
            a.baseValue = value;
            SaveAsset(a, DBEditorMenu.ItemsPath + "/Armor_" + id + ".asset");
            return a;
        }

        static ConsumableData MakeConsumable(string id, string name, ItemRarity rarity,
                                             ConsumableEffect effect, float val, int value)
        {
            var c = ScriptableObject.CreateInstance<ConsumableData>();
            c.itemId = id; c.displayName = name; c.rarity = rarity;
            c.category = ItemCategory.Consumable;
            c.effect = effect; c.value = val;
            c.stackable = true; c.maxStack = 99;
            c.baseValue = value;
            SaveAsset(c, DBEditorMenu.ItemsPath + "/Consumable_" + id + ".asset");
            return c;
        }

        static void SaveAsset(Object obj, string path)
        {
            var existing = AssetDatabase.LoadAssetAtPath<Object>(path);
            if (existing != null) AssetDatabase.DeleteAsset(path);
            AssetDatabase.CreateAsset(obj, path);
        }

        // ───── Loot Tables ─────
        public static void BuildSampleLootTables()
        {
            var pot_hp_small  = Load<ConsumableData>("pot_hp_small");
            var pot_stamina   = Load<ConsumableData>("pot_stamina");
            var sword_uncommon= Load<WeaponData>("sword_uncommon_steel");
            var sword_rare    = Load<WeaponData>("sword_rare_silver");
            var sword_epic    = Load<WeaponData>("sword_epic_voidedge");
            var sword_leg     = Load<WeaponData>("sword_legendary_warlord");
            var chest_rare    = Load<ArmorData>("armor_chest_rare_plate");
            var head_epic     = Load<ArmorData>("armor_head_epic_helm");
            var token         = Load<ItemData>("token_dungeon_clear");

            // Skeleton Soldier loot (common drops)
            var skSoldier = ScriptableObject.CreateInstance<LootTable>();
            skSoldier.independentRolls.Add(new LootEntry { item = pot_hp_small,  chance = 0.15f });
            skSoldier.independentRolls.Add(new LootEntry { item = pot_stamina,   chance = 0.10f });
            SaveAsset(skSoldier, DBEditorMenu.LootPath + "/Loot_SkeletonSoldier.asset");

            // Skeleton Archer
            var skArcher = ScriptableObject.CreateInstance<LootTable>();
            skArcher.independentRolls.Add(new LootEntry { item = pot_hp_small, chance = 0.12f });
            SaveAsset(skArcher, DBEditorMenu.LootPath + "/Loot_SkeletonArcher.asset");

            // Armored Knight elite
            var knight = ScriptableObject.CreateInstance<LootTable>();
            knight.independentRolls.Add(new LootEntry { item = sword_uncommon, chance = 0.50f });
            knight.independentRolls.Add(new LootEntry { item = chest_rare,     chance = 0.35f });
            knight.independentRolls.Add(new LootEntry { item = pot_hp_small,   chance = 1f, minQuantity = 1, maxQuantity = 2 });
            SaveAsset(knight, DBEditorMenu.LootPath + "/Loot_ArmoredKnight.asset");

            // Boss reward chest — matches GDD 6.2
            var boss = ScriptableObject.CreateInstance<LootTable>();
            boss.guaranteedGoldMin = 150; boss.guaranteedGoldMax = 300;
            boss.dungeonTokenAmount = 1;
            // "Guaranteed Uncommon or Rare" — use weighted pool
            boss.rollWeightedPool = true;
            boss.weightedPool.Add(new WeightedLootPool { entry = new LootEntry { item = sword_uncommon }, weight = 5f });
            boss.weightedPool.Add(new WeightedLootPool { entry = new LootEntry { item = sword_rare },     weight = 5f });
            // 70% rare, 30% epic, 5% legendary — independent rolls
            boss.independentRolls.Add(new LootEntry { item = sword_rare, chance = 0.70f });
            boss.independentRolls.Add(new LootEntry { item = sword_epic, chance = 0.30f });
            boss.independentRolls.Add(new LootEntry { item = head_epic,  chance = 0.30f });
            boss.independentRolls.Add(new LootEntry { item = sword_leg,  chance = 0.05f });
            if (token != null) boss.guaranteedDrops.Add(new LootEntry { item = token, minQuantity = 1, maxQuantity = 1 });
            SaveAsset(boss, DBEditorMenu.LootPath + "/Loot_Boss_ForsakenKeep.asset");
        }

        static T Load<T>(string idSuffix) where T : ItemData
        {
            string[] guids = AssetDatabase.FindAssets("t:ItemData", new[] { DBEditorMenu.ItemsPath });
            foreach (var g in guids)
            {
                var path = AssetDatabase.GUIDToAssetPath(g);
                var obj = AssetDatabase.LoadAssetAtPath<ItemData>(path);
                if (obj != null && obj.itemId == idSuffix) return obj as T;
            }
            return null;
        }

        /// <summary>Builds a list of shop entries for the Bank. Called by scene builder.</summary>
        public static System.Collections.Generic.List<ShopEntry> BuildShopEntries()
        {
            var list = new System.Collections.Generic.List<ShopEntry>();
            TryAdd(list, "pot_hp_small");
            TryAdd(list, "pot_hp_large");
            TryAdd(list, "pot_stamina");
            TryAdd(list, "grenade_frag");
            TryAdd(list, "sword_common_rusty");
            TryAdd(list, "gun_common_pistol");
            TryAdd(list, "armor_chest_common_tunic");
            TryAdd(list, "armor_head_common_hood");
            return list;
        }

        public static System.Collections.Generic.List<ShopEntry> BuildTokenShopEntries()
        {
            var list = new System.Collections.Generic.List<ShopEntry>();
            TryAddToken(list, "sword_rare_silver", 2);
            TryAddToken(list, "armor_chest_rare_plate", 3);
            TryAddToken(list, "sword_epic_voidedge", 5);
            return list;
        }

        static void TryAdd(System.Collections.Generic.List<ShopEntry> list, string id)
        {
            var it = Load<ItemData>(id);
            if (it != null) list.Add(new ShopEntry { item = it, stock = -1 });
        }

        static void TryAddToken(System.Collections.Generic.List<ShopEntry> list, string id, int cost)
        {
            var it = Load<ItemData>(id);
            if (it != null) list.Add(new ShopEntry { item = it, stock = -1, priceOverride = cost });
        }
    }
}
#endif
