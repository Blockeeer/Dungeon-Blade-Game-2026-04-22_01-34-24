using UnityEngine;
using UnityEngine.Events;
using DungeonBlade.Combat;

namespace DungeonBlade.Player
{
    /// <summary>
    /// Health, stamina, armor and death logic per GDD 2.3.
    /// Implements IDamageable so enemies and traps can damage the player.
    /// </summary>
    public class PlayerStats : MonoBehaviour, IDamageable
    {
        [Header("Health")]
        public float maxHealth = 100f;
        public float healthRegenPerSec = 2f;
        public float healthRegenDelay = 4f;          // no regen for X seconds after being hit
        public float currentHealth { get; private set; }

        [Header("Stamina")]
        public float maxStamina = 100f;
        public float staminaRegenPerSec = 25f;
        public float staminaRegenDelay = 0.8f;
        public float currentStamina { get; private set; }

        // Stamina costs (called by PlayerController / PlayerCombat)
        public float dashStaminaCost = 20f;
        public float wallRunStaminaPerSec = 10f;
        public float heavyAttackStaminaCost = 25f;

        [Header("Armor")]
        public float flatArmor = 0f;                 // flat reduction
        public float percentArmor = 0f;              // 0..0.85 reduction

        [Header("Respawns")]
        public int respawnsPerRun = 3;
        public int respawnsRemaining { get; private set; }

        [Header("Events")]
        public UnityEvent<float, float> OnHealthChanged;   // (current, max)
        public UnityEvent<float, float> OnStaminaChanged;
        public UnityEvent OnDeath;
        public UnityEvent<float> OnDamaged;                // raw damage amount

        float lastDamageTime = -99f;
        float lastStaminaSpendTime = -99f;
        bool isDead;

        public bool IsDead => isDead;

        void Awake()
        {
            currentHealth = maxHealth;
            currentStamina = maxStamina;
            respawnsRemaining = respawnsPerRun;
        }

        void Start()
        {
            OnHealthChanged?.Invoke(currentHealth, maxHealth);
            OnStaminaChanged?.Invoke(currentStamina, maxStamina);
        }

        void Update()
        {
            if (isDead) return;

            // HP regen
            if (Time.time - lastDamageTime > healthRegenDelay && currentHealth < maxHealth)
            {
                currentHealth = Mathf.Min(maxHealth, currentHealth + healthRegenPerSec * Time.deltaTime);
                OnHealthChanged?.Invoke(currentHealth, maxHealth);
            }

            // Stamina regen
            if (Time.time - lastStaminaSpendTime > staminaRegenDelay && currentStamina < maxStamina)
            {
                currentStamina = Mathf.Min(maxStamina, currentStamina + staminaRegenPerSec * Time.deltaTime);
                OnStaminaChanged?.Invoke(currentStamina, maxStamina);
            }
        }

        public bool TrySpendStamina(float amount)
        {
            if (currentStamina < amount) return false;
            currentStamina -= amount;
            lastStaminaSpendTime = Time.time;
            OnStaminaChanged?.Invoke(currentStamina, maxStamina);
            return true;
        }

        /// <summary>When true, logs every damage event and what happened. Useful during bring-up; turn off for shipping.</summary>
        public bool debugDamageLogging = true;

        public void TakeDamage(DamageInfo info)
        {
            if (isDead)
            {
                if (debugDamageLogging) Debug.Log($"[PlayerStats] TakeDamage({info.amount}) ignored — player is already dead.");
                return;
            }

            // Player controller i-frames (during dash) skip damage
            var pc = GetComponent<PlayerController>();
            if (pc != null && pc.IsInvulnerable)
            {
                if (debugDamageLogging) Debug.Log($"[PlayerStats] TakeDamage({info.amount}) blocked by i-frames (dash).");
                return;
            }

            float dmg = Mathf.Max(0f, info.amount - flatArmor);
            dmg *= (1f - Mathf.Clamp01(percentArmor));
            if (dmg <= 0f)
            {
                if (debugDamageLogging) Debug.Log($"[PlayerStats] TakeDamage({info.amount}) reduced to 0 by armor (flat={flatArmor}, pct={percentArmor}).");
                return;
            }

            currentHealth -= dmg;
            lastDamageTime = Time.time;
            OnHealthChanged?.Invoke(currentHealth, maxHealth);
            OnDamaged?.Invoke(dmg);

            if (debugDamageLogging) Debug.Log($"[PlayerStats] Took {dmg:F1} damage from {info.source}. HP {currentHealth:F1}/{maxHealth:F1}");

            if (info.knockback.sqrMagnitude > 0.01f && pc != null)
                pc.ApplyKnockback(info.knockback);

            if (currentHealth <= 0f) Die();
        }

        void Die()
        {
            isDead = true;
            if (debugDamageLogging) Debug.Log($"[PlayerStats] Player died.");
            OnDeath?.Invoke();
        }

        /// <summary>Called by checkpoint/respawn flow (DungeonManager).</summary>
        public void Respawn(Vector3 position, float hpFraction = 0.5f)
        {
            isDead = false;
            currentHealth = maxHealth * hpFraction;
            currentStamina = maxStamina;
            respawnsRemaining = Mathf.Max(0, respawnsRemaining - 1);

            var pc = GetComponent<PlayerController>();
            if (pc != null) pc.Teleport(position);

            OnHealthChanged?.Invoke(currentHealth, maxHealth);
            OnStaminaChanged?.Invoke(currentStamina, maxStamina);

            if (debugDamageLogging) Debug.Log($"[PlayerStats] Respawned at {position}. HP {currentHealth:F1}/{maxHealth:F1}. Respawns remaining: {respawnsRemaining}");
        }

        /// <summary>Called on checkpoint reach — heals +20 HP and full stamina per GDD 3.3.</summary>
        public void CheckpointRefresh()
        {
            currentHealth = Mathf.Min(maxHealth, currentHealth + 20f);
            currentStamina = maxStamina;
            OnHealthChanged?.Invoke(currentHealth, maxHealth);
            OnStaminaChanged?.Invoke(currentStamina, maxStamina);
        }

        public void ResetForNewRun()
        {
            isDead = false;
            currentHealth = maxHealth;
            currentStamina = maxStamina;
            respawnsRemaining = respawnsPerRun;
            OnHealthChanged?.Invoke(currentHealth, maxHealth);
            OnStaminaChanged?.Invoke(currentStamina, maxStamina);
        }

        /// <summary>Heal by flat amount (consumables, health orbs).</summary>
        public void ApplyHeal(float amount)
        {
            if (isDead || amount <= 0f) return;
            currentHealth = Mathf.Min(maxHealth, currentHealth + amount);
            OnHealthChanged?.Invoke(currentHealth, maxHealth);
        }

        /// <summary>Restore stamina by flat amount (consumables, orbs).</summary>
        public void ApplyStaminaRestore(float amount)
        {
            if (amount <= 0f) return;
            currentStamina = Mathf.Min(maxStamina, currentStamina + amount);
            OnStaminaChanged?.Invoke(currentStamina, maxStamina);
        }
    }
}
