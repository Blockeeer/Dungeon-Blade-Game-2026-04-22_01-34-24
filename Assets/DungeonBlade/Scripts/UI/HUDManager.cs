using UnityEngine;
using UnityEngine.UI;
using TMPro;

namespace DungeonBlade.UI
{
    /// <summary>
    /// In-dungeon HUD per GDD 11.1.
    /// Drop on a Canvas and assign references.
    /// Listens to events from PlayerStats, PlayerCombat, ComboCounter, PlayerProgression.
    /// </summary>
    public class HUDManager : MonoBehaviour
    {
        [Header("Refs")]
        public DungeonBlade.Player.PlayerStats playerStats;
        public DungeonBlade.Player.PlayerCombat playerCombat;
        public DungeonBlade.Player.PlayerSkills playerSkills;
        public DungeonBlade.Combat.ComboCounter comboCounter;
        public DungeonBlade.Progression.PlayerProgression progression;

        [Header("Health / Stamina")]
        public Image hpFill;
        public TMP_Text hpText;
        public Image staminaFill;

        [Header("Weapon / Ammo")]
        public TMP_Text weaponText;
        public TMP_Text ammoText;

        [Header("Combo")]
        public TMP_Text comboText;
        public CanvasGroup comboGroup;
        public float comboFadeDelay = 3f;
        float comboLastVisible;

        [Header("Skills")]
        public Image[] skillCooldownOverlays;
        public TMP_Text[] skillCooldownText;

        [Header("Hotbar")]
        public Image[] hotbarIcons;
        public TMP_Text[] hotbarCounts;

        [Header("Level / XP")]
        public TMP_Text levelText;
        public Image xpFill;

        [Header("Gold")]
        public TMP_Text goldText;

        [Header("Low HP")]
        public CanvasGroup lowHpVignette;
        public float lowHpThresholdPct = 0.25f;

        void Start()
        {
            if (playerStats != null)
            {
                playerStats.OnHealthChanged.AddListener(OnHealthChanged);
                playerStats.OnStaminaChanged.AddListener(OnStaminaChanged);
            }
            if (playerCombat != null)
            {
                playerCombat.OnAmmoChanged.AddListener(OnAmmoChanged);
                playerCombat.OnWeaponSwitched.AddListener(OnWeaponChanged);
                OnWeaponChanged(playerCombat.CurrentWeapon);
                OnAmmoChanged(playerCombat.CurrentMagAmmo, playerCombat.CurrentReserveAmmo);
            }
            if (comboCounter != null)
            {
                comboCounter.OnComboChanged.AddListener(OnComboChanged);
            }
            if (progression != null)
            {
                progression.OnLevelChanged.AddListener(OnLevelChanged);
                progression.OnXpChanged.AddListener(OnXpChanged);
                OnLevelChanged(progression.level, progression.skillPoints);
                OnXpChanged(progression.currentXp);
            }
            var inv = DungeonBlade.Core.GameServices.Inventory;
            if (inv != null)
            {
                inv.OnGoldChanged.AddListener(OnGoldChanged);
                inv.OnInventoryChanged.AddListener(RefreshHotbar);
                OnGoldChanged(inv.gold);
                RefreshHotbar();
            }
        }

        void Update()
        {
            // Skill cooldowns
            if (playerSkills != null && skillCooldownOverlays != null)
            {
                for (int i = 0; i < skillCooldownOverlays.Length && i < playerSkills.skills.Length; i++)
                {
                    var s = playerSkills.skills[i];
                    float remaining = Mathf.Max(0f, s.readyAt - Time.time);
                    float frac = s.cooldown > 0.01f ? remaining / s.cooldown : 0f;
                    if (skillCooldownOverlays[i] != null) skillCooldownOverlays[i].fillAmount = frac;
                    if (skillCooldownText != null && i < skillCooldownText.Length && skillCooldownText[i] != null)
                        skillCooldownText[i].text = remaining > 0.05f ? Mathf.CeilToInt(remaining).ToString() : "";
                }
            }

            // Combo fade
            if (comboGroup != null)
            {
                float elapsed = Time.time - comboLastVisible;
                if (elapsed < comboFadeDelay) comboGroup.alpha = 1f;
                else comboGroup.alpha = Mathf.MoveTowards(comboGroup.alpha, 0f, Time.deltaTime * 2f);
            }

            // Low HP vignette pulse
            if (playerStats != null && lowHpVignette != null)
            {
                bool low = (playerStats.currentHealth / playerStats.maxHealth) < lowHpThresholdPct;
                float target = low ? 0.5f + 0.5f * Mathf.Sin(Time.time * 4f) : 0f;
                lowHpVignette.alpha = Mathf.MoveTowards(lowHpVignette.alpha, target, Time.deltaTime * 3f);
            }
        }

        void OnHealthChanged(float current, float max)
        {
            if (hpFill != null) hpFill.fillAmount = max > 0 ? current / max : 0;
            if (hpText != null) hpText.text = $"{Mathf.CeilToInt(current)} / {Mathf.CeilToInt(max)}";
        }
        void OnStaminaChanged(float current, float max)
        {
            if (staminaFill != null) staminaFill.fillAmount = max > 0 ? current / max : 0;
        }
        void OnAmmoChanged(int mag, int reserve)
        {
            if (ammoText != null) ammoText.text = $"{mag} / {reserve}";
        }
        void OnWeaponChanged(DungeonBlade.Player.PlayerCombat.WeaponMode m)
        {
            if (weaponText != null) weaponText.text = m == DungeonBlade.Player.PlayerCombat.WeaponMode.Sword ? "SWORD" : "GUN";
            if (ammoText != null) ammoText.gameObject.SetActive(m == DungeonBlade.Player.PlayerCombat.WeaponMode.Gun);
        }
        void OnComboChanged(int combo)
        {
            if (comboText != null) comboText.text = combo > 0 ? $"x{combo}" : "";
            if (combo > 0) comboLastVisible = Time.time;
        }
        void OnLevelChanged(int lvl, int sp)
        {
            if (levelText != null) levelText.text = $"LV {lvl}";
        }
        void OnXpChanged(int xp)
        {
            if (xpFill != null && progression != null)
            {
                int need = progression.XpToNext;
                xpFill.fillAmount = need > 0 ? (float)xp / need : 1f;
            }
        }
        void OnGoldChanged(int g)
        {
            if (goldText != null) goldText.text = g.ToString("N0");
        }

        void RefreshHotbar()
        {
            var inv = DungeonBlade.Core.GameServices.Inventory;
            if (inv == null || hotbarIcons == null) return;
            for (int i = 0; i < hotbarIcons.Length && i < inv.hotbar.Length; i++)
            {
                var slot = inv.hotbar[i];
                if (hotbarIcons[i] != null)
                {
                    hotbarIcons[i].sprite = slot.IsEmpty ? null : slot.item.icon;
                    hotbarIcons[i].enabled = !slot.IsEmpty;
                }
                if (hotbarCounts != null && i < hotbarCounts.Length && hotbarCounts[i] != null)
                    hotbarCounts[i].text = slot.IsEmpty ? "" : slot.quantity.ToString();
            }
        }
    }
}
