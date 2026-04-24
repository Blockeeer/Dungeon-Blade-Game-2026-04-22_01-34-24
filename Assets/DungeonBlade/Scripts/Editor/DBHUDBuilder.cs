#if UNITY_EDITOR
using UnityEngine;
using UnityEngine.UI;
using UnityEditor;
using TMPro;
using DungeonBlade.UI;

namespace DungeonBlade.EditorTools
{
    /// <summary>
    /// Creates the in-game HUD, Inventory Canvas, and Bank Canvas at runtime via
    /// SceneBuilder. All layouts use anchor-based positioning so they scale
    /// properly across resolutions.
    /// </summary>
    public static class DBHUDBuilder
    {
        public static GameObject BuildHUD(Transform sceneParent = null)
        {
            var canvas = MakeCanvas("HUD_Canvas", sortOrder: 10);
            if (sceneParent != null) canvas.transform.SetParent(sceneParent, false);
            var cTransform = canvas.GetComponent<RectTransform>();

            // ─── Bottom left: HP + Stamina ───
            var hpBlock = MakePanel(canvas.transform, "HPBlock", new Vector2(0, 0), new Vector2(0, 0), new Vector2(15, 90), new Vector2(260, 70));
            hpBlock.GetComponent<Image>().color = new Color(0, 0, 0, 0.0f);
            var hpLabel = MakeText(hpBlock.transform, "HPLabel", "HEALTH", 12, TextAlignmentOptions.Left);
            PlaceTL(hpLabel, 0, 0, 220, 18);
            var hpText = MakeText(hpBlock.transform, "HPText", "100 / 100", 16, TextAlignmentOptions.Left, bold: true);
            PlaceTL(hpText, 0, 18, 220, 24);
            var hpBar = MakeBar(hpBlock.transform, "HPBar", new Color(1, 0.2f, 0.3f));
            PlaceTL(hpBar.transform.parent.gameObject, 0, 44, 220, 14);

            var staLabel = MakeText(hpBlock.transform, "StaminaLabel", "STAMINA", 11, TextAlignmentOptions.Left);
            PlaceTL(staLabel, 0, 62, 220, 14);
            var staBar = MakeBar(hpBlock.transform, "StaminaBar", new Color(0.2f, 1f, 0.6f));
            PlaceTL(staBar.transform.parent.gameObject, 0, 76, 220, 10);

            // ─── Top right: Level + Gold ───
            var topRight = MakePanel(canvas.transform, "TopRight", new Vector2(1, 1), new Vector2(1, 1), new Vector2(-15, -15), new Vector2(200, 80));
            topRight.GetComponent<Image>().color = new Color(0, 0, 0, 0f);
            var levelText = MakeText(topRight.transform, "LevelText", "LV 1", 22, TextAlignmentOptions.Right, bold: true);
            PlaceTL(levelText, 0, 0, 180, 26);
            var xpBar = MakeBar(topRight.transform, "XPBar", new Color(1f, 0.9f, 0.3f));
            PlaceTL(xpBar.transform.parent.gameObject, 0, 28, 180, 8);
            var goldText = MakeText(topRight.transform, "GoldText", "0", 18, TextAlignmentOptions.Right);
            PlaceTL(goldText, 0, 40, 180, 22);

            // ─── Bottom right: Weapon + Ammo ───
            var weaponBlock = MakePanel(canvas.transform, "WeaponBlock", new Vector2(1, 0), new Vector2(1, 0), new Vector2(-15, 90), new Vector2(220, 70));
            weaponBlock.GetComponent<Image>().color = new Color(0,0,0,0f);
            var weaponText = MakeText(weaponBlock.transform, "WeaponText", "SWORD", 18, TextAlignmentOptions.Right, bold: true);
            PlaceTL(weaponText, 0, 0, 220, 26);
            var ammoText = MakeText(weaponBlock.transform, "AmmoText", "12 / 60", 14, TextAlignmentOptions.Right);
            PlaceTL(ammoText, 0, 28, 220, 20);
            ammoText.gameObject.SetActive(false);

            // ─── Bottom center: Hotbar ───
            var hotbar = MakePanel(canvas.transform, "Hotbar", new Vector2(0.5f, 0), new Vector2(0.5f, 0), new Vector2(0, 20), new Vector2(280, 65));
            hotbar.GetComponent<Image>().color = new Color(0,0,0,0.3f);
            var hotbarIcons = new Image[4];
            var hotbarCounts = new TMP_Text[4];
            for (int i = 0; i < 4; i++)
            {
                var slot = MakePanel(hotbar.transform, $"Slot{i+1}", new Vector2(0,0.5f), new Vector2(0,0.5f), new Vector2(i * 65 + 15, 0), new Vector2(55, 55));
                slot.GetComponent<Image>().color = new Color(0.1f, 0.1f, 0.15f, 0.85f);
                var icon = new GameObject("Icon"); icon.transform.SetParent(slot.transform, false);
                var img = icon.AddComponent<Image>(); img.raycastTarget = false; img.enabled = false;
                var rt = icon.GetComponent<RectTransform>(); rt.anchorMin = Vector2.zero; rt.anchorMax = Vector2.one; rt.sizeDelta = new Vector2(-8, -8); rt.anchoredPosition = Vector2.zero;
                hotbarIcons[i] = img;
                var key = MakeText(slot.transform, "Key", (i + 1).ToString(), 12, TextAlignmentOptions.BottomLeft);
                PlaceTL(key, 4, 34, 20, 18);
                var count = MakeText(slot.transform, "Count", "", 11, TextAlignmentOptions.BottomRight);
                PlaceTL(count, 34, 34, 20, 18);
                hotbarCounts[i] = count;
            }

            // ─── Top center: Combo ───
            var comboObj = MakeText(canvas.transform, "ComboText", "", 52, TextAlignmentOptions.Center, bold: true);
            var comboRT = comboObj.GetComponent<RectTransform>();
            comboRT.anchorMin = comboRT.anchorMax = new Vector2(0.5f, 1f);
            comboRT.pivot = new Vector2(0.5f, 1f);
            comboRT.anchoredPosition = new Vector2(0, -80);
            comboRT.sizeDelta = new Vector2(400, 80);
            comboObj.color = new Color(1f, 0.8f, 0.2f);
            comboObj.fontStyle = FontStyles.Bold;
            var comboGroup = comboObj.gameObject.AddComponent<CanvasGroup>();
            comboGroup.alpha = 0;

            // ─── Skill row (bottom-left above HP) ───
            var skillRow = MakePanel(canvas.transform, "SkillRow", new Vector2(0, 0), new Vector2(0, 0), new Vector2(15, 170), new Vector2(260, 50));
            skillRow.GetComponent<Image>().color = new Color(0,0,0,0f);
            var skillOverlays = new Image[6];
            var skillCDTexts = new TMP_Text[6];
            string[] skillLabels = { "E", "G", "V", "C", "F", "+" };
            for (int i = 0; i < 6; i++)
            {
                var slot = MakePanel(skillRow.transform, $"Skill{i}", new Vector2(0, 0.5f), new Vector2(0, 0.5f), new Vector2(i * 44, 0), new Vector2(40, 40));
                slot.GetComponent<Image>().color = new Color(0.15f, 0.2f, 0.3f, 0.85f);
                var overlay = new GameObject("Overlay"); overlay.transform.SetParent(slot.transform, false);
                var ovImg = overlay.AddComponent<Image>(); ovImg.color = new Color(0, 0, 0, 0.75f);
                ovImg.type = Image.Type.Filled; ovImg.fillMethod = Image.FillMethod.Radial360;
                ovImg.fillOrigin = (int)Image.Origin360.Top; ovImg.fillClockwise = false; ovImg.fillAmount = 0;
                var ort = overlay.GetComponent<RectTransform>();
                ort.anchorMin = Vector2.zero; ort.anchorMax = Vector2.one; ort.sizeDelta = Vector2.zero; ort.anchoredPosition = Vector2.zero;
                skillOverlays[i] = ovImg;
                var key = MakeText(slot.transform, "Key", skillLabels[i], 11, TextAlignmentOptions.TopLeft);
                PlaceTL(key, 3, 2, 18, 15);
                var cd = MakeText(slot.transform, "CD", "", 14, TextAlignmentOptions.Center, bold: true);
                var cdRT = cd.GetComponent<RectTransform>();
                cdRT.anchorMin = Vector2.zero; cdRT.anchorMax = Vector2.one; cdRT.sizeDelta = Vector2.zero; cdRT.anchoredPosition = Vector2.zero;
                skillCDTexts[i] = cd;
            }

            // ─── Low HP Vignette ───
            var vign = new GameObject("LowHPVignette");
            vign.transform.SetParent(canvas.transform, false);
            var vImg = vign.AddComponent<Image>(); vImg.color = new Color(1f, 0.1f, 0.15f, 0.25f);
            vImg.raycastTarget = false;
            var vRT = vign.GetComponent<RectTransform>();
            vRT.anchorMin = Vector2.zero; vRT.anchorMax = Vector2.one; vRT.sizeDelta = Vector2.zero; vRT.anchoredPosition = Vector2.zero;
            var vCG = vign.AddComponent<CanvasGroup>(); vCG.alpha = 0; vCG.blocksRaycasts = false;

            // ─── HUDManager component ───
            var hud = canvas.AddComponent<HUDManager>();
            hud.hpFill = GetChildByName(hpBar.gameObject, "Fill").GetComponent<Image>();
            hud.hpText = hpText;
            hud.staminaFill = GetChildByName(staBar.gameObject, "Fill").GetComponent<Image>();
            hud.weaponText = weaponText;
            hud.ammoText = ammoText;
            hud.comboText = comboObj;
            hud.comboGroup = comboGroup;
            hud.skillCooldownOverlays = skillOverlays;
            hud.skillCooldownText = skillCDTexts;
            hud.hotbarIcons = hotbarIcons;
            hud.hotbarCounts = hotbarCounts;
            hud.levelText = levelText;
            hud.xpFill = GetChildByName(xpBar.gameObject, "Fill").GetComponent<Image>();
            hud.goldText = goldText;
            hud.lowHpVignette = vCG;

            return canvas;
        }

        public static GameObject BuildInventoryCanvas(Transform sceneParent = null)
        {
            var canvas = MakeCanvas("Inventory_Canvas", sortOrder: 20);
            if (sceneParent != null) canvas.transform.SetParent(sceneParent, false);

            var root = MakePanel(canvas.transform, "Root", new Vector2(0.5f, 0.5f), new Vector2(0.5f, 0.5f), Vector2.zero, new Vector2(820, 560));
            root.GetComponent<Image>().color = new Color(0.05f, 0.05f, 0.08f, 0.95f);
            root.SetActive(false);

            var title = MakeText(root.transform, "Title", "INVENTORY", 24, TextAlignmentOptions.Center, bold: true);
            var titleRT = title.GetComponent<RectTransform>();
            titleRT.anchorMin = new Vector2(0, 1); titleRT.anchorMax = new Vector2(1, 1);
            titleRT.pivot = new Vector2(0.5f, 1f); titleRT.anchoredPosition = new Vector2(0, -15); titleRT.sizeDelta = new Vector2(0, 36);

            // Equipment column (left)
            var equipPanel = MakePanel(root.transform, "Equipment", new Vector2(0, 1), new Vector2(0, 1), new Vector2(20, -60), new Vector2(120, 480));
            equipPanel.GetComponent<Image>().color = new Color(0.08f, 0.08f, 0.11f, 1f);
            var equipGrid = equipPanel.AddComponent<GridLayoutGroup>();
            equipGrid.cellSize = new Vector2(55, 55);
            equipGrid.spacing = new Vector2(4, 4);
            equipGrid.padding = new RectOffset(4, 4, 4, 4);
            equipGrid.constraint = GridLayoutGroup.Constraint.FixedColumnCount;
            equipGrid.constraintCount = 2;

            // Main grid (center, 6 cols x 8 rows)
            var gridPanel = MakePanel(root.transform, "MainGrid", new Vector2(0, 1), new Vector2(0, 1), new Vector2(160, -60), new Vector2(420, 480));
            gridPanel.GetComponent<Image>().color = new Color(0.08f, 0.08f, 0.11f, 1f);
            var grid = gridPanel.AddComponent<GridLayoutGroup>();
            grid.cellSize = new Vector2(62, 55);
            grid.spacing = new Vector2(4, 4);
            grid.padding = new RectOffset(6, 6, 6, 6);
            grid.constraint = GridLayoutGroup.Constraint.FixedColumnCount;
            grid.constraintCount = 6;

            // Hotbar bottom
            var hotbarPanel = MakePanel(root.transform, "Hotbar", new Vector2(0, 0), new Vector2(0, 0), new Vector2(160, 20), new Vector2(280, 60));
            hotbarPanel.GetComponent<Image>().color = new Color(0.08f, 0.08f, 0.11f, 1f);
            var hbGrid = hotbarPanel.AddComponent<GridLayoutGroup>();
            hbGrid.cellSize = new Vector2(60, 50);
            hbGrid.spacing = new Vector2(4, 4);
            hbGrid.padding = new RectOffset(6, 6, 6, 6);
            hbGrid.constraint = GridLayoutGroup.Constraint.FixedColumnCount;
            hbGrid.constraintCount = 4;

            // Gold / token text
            var goldText = MakeText(root.transform, "Gold", "Gold: 0", 16, TextAlignmentOptions.Right);
            var goldRT = goldText.GetComponent<RectTransform>();
            goldRT.anchorMin = new Vector2(1, 1); goldRT.anchorMax = new Vector2(1, 1);
            goldRT.pivot = new Vector2(1, 1); goldRT.anchoredPosition = new Vector2(-20, -60); goldRT.sizeDelta = new Vector2(200, 26);
            var tokenText = MakeText(root.transform, "Tokens", "Tokens: 0", 14, TextAlignmentOptions.Right);
            var tkRT = tokenText.GetComponent<RectTransform>();
            tkRT.anchorMin = new Vector2(1, 1); tkRT.anchorMax = new Vector2(1, 1);
            tkRT.pivot = new Vector2(1, 1); tkRT.anchoredPosition = new Vector2(-20, -86); tkRT.sizeDelta = new Vector2(200, 22);

            // Tooltip
            var tooltip = MakePanel(root.transform, "Tooltip", new Vector2(0, 1), new Vector2(0, 1), new Vector2(600, -60), new Vector2(200, 280));
            tooltip.GetComponent<Image>().color = new Color(0.1f, 0.1f, 0.15f, 0.98f);
            tooltip.SetActive(false);
            var ttName = MakeText(tooltip.transform, "Name", "Name", 16, TextAlignmentOptions.Left, bold: true);
            PlaceTL(ttName, 8, 8, 180, 22);
            var ttRarity = MakeText(tooltip.transform, "Rarity", "COMMON", 11, TextAlignmentOptions.Left);
            PlaceTL(ttRarity, 8, 30, 180, 16);
            var ttStats = MakeText(tooltip.transform, "Stats", "", 12, TextAlignmentOptions.TopLeft);
            PlaceTL(ttStats, 8, 50, 180, 150);
            var ttFlavor = MakeText(tooltip.transform, "Flavor", "", 11, TextAlignmentOptions.TopLeft);
            PlaceTL(ttFlavor, 8, 205, 180, 70);
            ttFlavor.color = new Color(0.7f, 0.7f, 0.8f);

            // Context menu
            var ctx = MakePanel(canvas.transform, "ContextMenu", new Vector2(0.5f, 0.5f), new Vector2(0.5f, 0.5f), Vector2.zero, new Vector2(140, 150));
            ctx.GetComponent<Image>().color = new Color(0.12f, 0.12f, 0.16f, 0.98f);
            ctx.SetActive(false);
            var btnE = MakeButton(ctx.transform, "Equip", "Equip");  PlaceTL(btnE, 4, 4, 132, 32);
            var btnU = MakeButton(ctx.transform, "Use", "Use");      PlaceTL(btnU, 4, 40, 132, 32);
            var btnS = MakeButton(ctx.transform, "Sell", "Sell");    PlaceTL(btnS, 4, 76, 132, 32);
            var btnD = MakeButton(ctx.transform, "Discard", "Discard"); PlaceTL(btnD, 4, 112, 132, 32);

            // Slot prefab — build once and save as asset
            var slotPrefab = BuildSlotPrefab();

            // Hook up component
            var inv = canvas.AddComponent<InventoryUI>();
            inv.rootPanel = root;
            inv.slotPrefab = slotPrefab;
            inv.mainGridParent = gridPanel.transform;
            inv.hotbarParent = hotbarPanel.transform;
            inv.equipmentParent = equipPanel.transform;
            inv.tooltipPanel = tooltip;
            inv.tooltipName = ttName;
            inv.tooltipRarity = ttRarity;
            inv.tooltipStats = ttStats;
            inv.tooltipFlavor = ttFlavor;
            inv.contextMenuPanel = ctx;
            inv.btnEquip = btnE.GetComponent<Button>();
            inv.btnUse = btnU.GetComponent<Button>();
            inv.btnSell = btnS.GetComponent<Button>();
            inv.btnDiscard = btnD.GetComponent<Button>();
            inv.goldText = goldText;
            inv.tokenText = tokenText;

            return canvas;
        }

        static GameObject BuildSlotPrefab()
        {
            string path = DBEditorMenu.UIPath + "/InventorySlot.prefab";
            // Force delete any stale prefab so missing-script warnings clear
            var existing = AssetDatabase.LoadAssetAtPath<GameObject>(path);
            if (existing != null) AssetDatabase.DeleteAsset(path);

            var slot = new GameObject("InventorySlot");
            var img = slot.AddComponent<Image>();
            img.color = new Color(0.1f, 0.1f, 0.15f, 0.9f);
            img.raycastTarget = true;
            slot.AddComponent<InventorySlotUI>();

            var icon = new GameObject("Icon"); icon.transform.SetParent(slot.transform, false);
            var iconImg = icon.AddComponent<Image>(); iconImg.raycastTarget = false; iconImg.enabled = false;
            var rt = icon.GetComponent<RectTransform>();
            rt.anchorMin = Vector2.zero; rt.anchorMax = Vector2.one;
            rt.sizeDelta = new Vector2(-8, -8); rt.anchoredPosition = Vector2.zero;

            var count = MakeText(slot.transform, "Count", "", 11, TextAlignmentOptions.BottomRight);
            var cRT = count.GetComponent<RectTransform>();
            cRT.anchorMin = Vector2.zero; cRT.anchorMax = Vector2.one;
            cRT.sizeDelta = new Vector2(-4, -4); cRT.anchoredPosition = Vector2.zero;
            count.raycastTarget = false;

            PrefabUtility.SaveAsPrefabAsset(slot, path);
            UnityEngine.Object.DestroyImmediate(slot);
            return AssetDatabase.LoadAssetAtPath<GameObject>(path);
        }

        public static GameObject BuildCharacterSelectCanvas(Transform sceneParent = null)
        {
            var canvas = MakeCanvas("CharacterSelect_Canvas", sortOrder: 30);
            if (sceneParent != null) canvas.transform.SetParent(sceneParent, false);

            // Full-screen dim
            var root = MakePanel(canvas.transform, "Root",
                new Vector2(0, 0), new Vector2(1, 1), Vector2.zero, Vector2.zero);
            var rootRT = root.GetComponent<RectTransform>();
            rootRT.anchorMin = Vector2.zero; rootRT.anchorMax = Vector2.one;
            rootRT.sizeDelta = Vector2.zero; rootRT.anchoredPosition = Vector2.zero;
            root.GetComponent<Image>().color = new Color(0, 0, 0, 0.88f);

            // Title
            var title = MakeText(root.transform, "Title", "CHOOSE YOUR CHAMPION", 34, TextAlignmentOptions.Center, bold: true);
            var titleRT = title.GetComponent<RectTransform>();
            titleRT.anchorMin = new Vector2(0.5f, 1f); titleRT.anchorMax = new Vector2(0.5f, 1f);
            titleRT.pivot = new Vector2(0.5f, 1f);
            titleRT.anchoredPosition = new Vector2(0, -40);
            titleRT.sizeDelta = new Vector2(700, 50);
            title.color = new Color(1f, 0.92f, 0.6f);

            // Portrait row — horizontal layout
            var rowBG = MakePanel(root.transform, "PortraitRowBG",
                new Vector2(0.5f, 0.5f), new Vector2(0.5f, 0.5f),
                new Vector2(0, 120), new Vector2(960, 180));
            rowBG.GetComponent<Image>().color = new Color(0.08f, 0.08f, 0.12f, 0.7f);

            var row = new GameObject("PortraitRow");
            row.transform.SetParent(rowBG.transform, false);
            var rowLayout = row.AddComponent<HorizontalLayoutGroup>();
            rowLayout.spacing = 12;
            rowLayout.padding = new RectOffset(16, 16, 16, 16);
            rowLayout.childAlignment = TextAnchor.MiddleCenter;
            rowLayout.childControlHeight = true; rowLayout.childControlWidth = false;
            rowLayout.childForceExpandWidth = false; rowLayout.childForceExpandHeight = true;
            var rowRT = row.GetComponent<RectTransform>();
            rowRT.anchorMin = Vector2.zero; rowRT.anchorMax = Vector2.one;
            rowRT.sizeDelta = Vector2.zero; rowRT.anchoredPosition = Vector2.zero;

            // Detail panel (below portrait row)
            var detail = MakePanel(root.transform, "DetailPanel",
                new Vector2(0.5f, 0.5f), new Vector2(0.5f, 0.5f),
                new Vector2(0, -140), new Vector2(700, 280));
            detail.GetComponent<Image>().color = new Color(0.1f, 0.1f, 0.14f, 0.85f);

            var detailPortrait = new GameObject("DetailPortrait");
            detailPortrait.transform.SetParent(detail.transform, false);
            var dpImg = detailPortrait.AddComponent<Image>();
            dpImg.color = new Color(0.3f, 0.35f, 0.45f, 0.8f);
            var dpRT = detailPortrait.GetComponent<RectTransform>();
            dpRT.anchorMin = new Vector2(0, 0.5f); dpRT.anchorMax = new Vector2(0, 0.5f);
            dpRT.pivot = new Vector2(0, 0.5f);
            dpRT.anchoredPosition = new Vector2(20, 0);
            dpRT.sizeDelta = new Vector2(200, 240);

            var detailName = MakeText(detail.transform, "Name", "Character Name", 24, TextAlignmentOptions.Left, bold: true);
            var nameRT = detailName.GetComponent<RectTransform>();
            nameRT.anchorMin = new Vector2(0, 1); nameRT.anchorMax = new Vector2(1, 1);
            nameRT.pivot = new Vector2(0, 1);
            nameRT.anchoredPosition = new Vector2(240, -20);
            nameRT.sizeDelta = new Vector2(440, 32);
            detailName.color = new Color(1f, 0.92f, 0.6f);

            var detailFlavor = MakeText(detail.transform, "Flavor", "Flavor text appears here.", 14, TextAlignmentOptions.TopLeft);
            var flavRT = detailFlavor.GetComponent<RectTransform>();
            flavRT.anchorMin = new Vector2(0, 1); flavRT.anchorMax = new Vector2(1, 1);
            flavRT.pivot = new Vector2(0, 1);
            flavRT.anchoredPosition = new Vector2(240, -60);
            flavRT.sizeDelta = new Vector2(440, 60);
            detailFlavor.color = new Color(0.85f, 0.85f, 0.9f);

            var detailStats = MakeText(detail.transform, "Stats", "Balanced stats", 14, TextAlignmentOptions.TopLeft);
            var statsRT = detailStats.GetComponent<RectTransform>();
            statsRT.anchorMin = new Vector2(0, 1); statsRT.anchorMax = new Vector2(1, 1);
            statsRT.pivot = new Vector2(0, 1);
            statsRT.anchoredPosition = new Vector2(240, -130);
            statsRT.sizeDelta = new Vector2(440, 100);

            // Buttons
            var btnConfirm = MakeButton(detail.transform, "ConfirmButton", "Confirm Selection");
            var cbRT = btnConfirm.GetComponent<RectTransform>();
            cbRT.anchorMin = new Vector2(1, 0); cbRT.anchorMax = new Vector2(1, 0);
            cbRT.pivot = new Vector2(1, 0);
            cbRT.anchoredPosition = new Vector2(-20, 20);
            cbRT.sizeDelta = new Vector2(200, 42);
            btnConfirm.GetComponent<Image>().color = new Color(0.2f, 0.6f, 0.3f, 1f);

            var btnClose = MakeButton(detail.transform, "CloseButton", "Cancel");
            var xbRT = btnClose.GetComponent<RectTransform>();
            xbRT.anchorMin = new Vector2(0, 0); xbRT.anchorMax = new Vector2(0, 0);
            xbRT.pivot = new Vector2(0, 0);
            xbRT.anchoredPosition = new Vector2(20, 20);
            xbRT.sizeDelta = new Vector2(140, 42);
            btnClose.GetComponent<Image>().color = new Color(0.4f, 0.2f, 0.2f, 1f);

            // Portrait button prefab — simple button w/ image + name
            var portraitPrefab = BuildPortraitButtonPrefab();

            // Wire up the CharacterSelectUI component
            var selectUI = canvas.AddComponent<DungeonBlade.Characters.CharacterSelectUI>();
            selectUI.rootPanel = root;
            selectUI.portraitRowParent = row.transform;
            selectUI.portraitButtonPrefab = portraitPrefab;
            selectUI.detailPortrait = dpImg;
            selectUI.detailName = detailName;
            selectUI.detailFlavor = detailFlavor;
            selectUI.detailStats = detailStats;
            selectUI.confirmButton = btnConfirm.GetComponent<Button>();
            selectUI.closeButton = btnClose.GetComponent<Button>();

            return canvas;
        }

        static GameObject BuildPortraitButtonPrefab()
        {
            string path = DBEditorMenu.UIPath + "/CharacterPortraitButton.prefab";
            // Always rebuild to clear any stale missing-script references
            var existing = AssetDatabase.LoadAssetAtPath<GameObject>(path);
            if (existing != null) AssetDatabase.DeleteAsset(path);

            var go = new GameObject("CharacterPortraitButton");
            var img = go.AddComponent<Image>();
            img.color = new Color(0.18f, 0.2f, 0.28f, 1f);
            img.raycastTarget = true;
            var rt = go.GetComponent<RectTransform>();
            rt.sizeDelta = new Vector2(130, 150);

            var nameLbl = MakeText(go.transform, "Name", "Name", 13, TextAlignmentOptions.Center, bold: true);
            var nameRT = nameLbl.GetComponent<RectTransform>();
            nameRT.anchorMin = new Vector2(0, 0); nameRT.anchorMax = new Vector2(1, 0);
            nameRT.pivot = new Vector2(0.5f, 0);
            nameRT.anchoredPosition = new Vector2(0, 4);
            nameRT.sizeDelta = new Vector2(-8, 22);
            nameLbl.raycastTarget = false;

            var pb = go.AddComponent<DungeonBlade.Characters.CharacterPortraitButton>();
            pb.portraitImage = img;
            pb.nameLabel = nameLbl;
            pb.frameImage = img;

            PrefabUtility.SaveAsPrefabAsset(go, path);
            UnityEngine.Object.DestroyImmediate(go);
            return AssetDatabase.LoadAssetAtPath<GameObject>(path);
        }

        // ───── primitives ─────
        static GameObject MakeCanvas(string name, int sortOrder)
        {
            var go = new GameObject(name);
            var c = go.AddComponent<Canvas>();
            c.renderMode = RenderMode.ScreenSpaceOverlay;
            c.sortingOrder = sortOrder;
            var scaler = go.AddComponent<CanvasScaler>();
            scaler.uiScaleMode = CanvasScaler.ScaleMode.ScaleWithScreenSize;
            scaler.referenceResolution = new Vector2(1920, 1080);
            scaler.matchWidthOrHeight = 0.5f;
            go.AddComponent<GraphicRaycaster>();
            return go;
        }

        static GameObject MakePanel(Transform parent, string name, Vector2 anchorMin, Vector2 anchorMax, Vector2 pos, Vector2 size)
        {
            var go = new GameObject(name);
            go.transform.SetParent(parent, false);
            var img = go.AddComponent<Image>();
            img.color = new Color(0, 0, 0, 0.4f);
            var rt = go.GetComponent<RectTransform>();
            rt.anchorMin = anchorMin; rt.anchorMax = anchorMax;
            rt.pivot = new Vector2(anchorMin.x, anchorMin.y);
            rt.anchoredPosition = pos; rt.sizeDelta = size;
            return go;
        }

        static TMP_Text MakeText(Transform parent, string name, string text, int fontSize, TextAlignmentOptions align, bool bold = false)
        {
            var go = new GameObject(name);
            go.transform.SetParent(parent, false);
            var t = go.AddComponent<TextMeshProUGUI>();
            t.text = text; t.fontSize = fontSize; t.alignment = align;
            if (bold) t.fontStyle = FontStyles.Bold;
            t.color = Color.white;
            var rt = go.GetComponent<RectTransform>();
            rt.sizeDelta = new Vector2(200, 30);
            return t;
        }

        static void PlaceTL(GameObject go, float left, float topFromParentTop, float width, float height)
        {
            var rt = go.GetComponent<RectTransform>();
            rt.anchorMin = new Vector2(0, 1); rt.anchorMax = new Vector2(0, 1); rt.pivot = new Vector2(0, 1);
            rt.anchoredPosition = new Vector2(left, -topFromParentTop);
            rt.sizeDelta = new Vector2(width, height);
        }

        static void PlaceTL(TMP_Text t, float left, float topFromParentTop, float width, float height)
            => PlaceTL(t.gameObject, left, topFromParentTop, width, height);

        static GameObject MakeBar(Transform parent, string name, Color fillColor)
        {
            var bar = new GameObject(name);
            bar.transform.SetParent(parent, false);
            var bg = bar.AddComponent<Image>();
            bg.color = new Color(0, 0, 0, 0.5f);
            var rt = bar.GetComponent<RectTransform>();
            rt.sizeDelta = new Vector2(220, 14);

            var fill = new GameObject("Fill");
            fill.transform.SetParent(bar.transform, false);
            var fillImg = fill.AddComponent<Image>();
            fillImg.color = fillColor;
            fillImg.type = Image.Type.Filled;
            fillImg.fillMethod = Image.FillMethod.Horizontal;
            fillImg.fillAmount = 1f;
            var frt = fill.GetComponent<RectTransform>();
            frt.anchorMin = new Vector2(0, 0.1f); frt.anchorMax = new Vector2(1, 0.9f);
            frt.sizeDelta = new Vector2(-4, 0); frt.anchoredPosition = Vector2.zero;

            return bar;
        }

        static GameObject MakeButton(Transform parent, string name, string label)
        {
            var go = new GameObject(name);
            go.transform.SetParent(parent, false);
            var img = go.AddComponent<Image>(); img.color = new Color(0.18f, 0.26f, 0.4f, 1f);
            var btn = go.AddComponent<Button>();
            var cb = btn.colors; cb.highlightedColor = new Color(0.3f, 0.5f, 0.9f); btn.colors = cb;
            var text = MakeText(go.transform, "Label", label, 13, TextAlignmentOptions.Center);
            var trt = text.GetComponent<RectTransform>();
            trt.anchorMin = Vector2.zero; trt.anchorMax = Vector2.one; trt.sizeDelta = Vector2.zero; trt.anchoredPosition = Vector2.zero;
            return go;
        }

        static GameObject GetChildByName(GameObject parent, string name)
        {
            foreach (Transform t in parent.transform)
                if (t.name == name) return t.gameObject;
            return null;
        }
    }
}
#endif
