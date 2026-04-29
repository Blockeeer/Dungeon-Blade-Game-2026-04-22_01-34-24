using System;
using DungeonBlade.Combat;
using UnityEngine;

namespace DungeonBlade.Player
{
    public class PlayerStats : MonoBehaviour, IDamageable
    {
        public bool IsAlive => !IsDead;

        public void ApplyDamage(in DamageInfo info)
        {
            var movement = GetComponent<PlayerMovement>();
            if (movement != null && movement.IsInvulnerable)
            {
                Debug.Log($"[Player] Dodged {info.Type} attack from {info.Source?.name} (i-frames).");
                return;
            }

            float amount = info.Amount;

            var sword = GetComponentInChildren<Sword>();
            if (sword != null && sword.State == Sword.SwordState.Blocking && info.IsParryable)
            {
                if (sword.IsParryActive)
                {
                    Debug.Log($"[Player] Parried {info.Type} attack from {info.Source?.name}");
                    return;
                }
                amount *= 1f - sword.BlockDamageReduction;
            }

            TakeDamage(amount);
            Debug.Log($"[Player] Took {amount:F0} {info.Type} damage from {info.Source?.name}.  HP: {Health:F0}/{maxHealth:F0}");
        }

        [Header("Health")]
        [SerializeField] float maxHealth = 100f;
        [SerializeField] float healthRegenPerSecond = 2f;
        [SerializeField] float healthRegenDelay = 4f;

        [Header("Stamina")]
        [SerializeField] float maxStamina = 100f;
        [SerializeField] float staminaRegenPerSecond = 25f;
        [SerializeField] float staminaRegenDelay = 0.5f;

        public float Health { get; private set; }
        public float Stamina { get; private set; }
        public float MaxHealth => maxHealth;
        public float MaxStamina => maxStamina;
        public bool IsDead => Health <= 0f;

        public event Action<float, float> OnHealthChanged;
        public event Action<float, float> OnStaminaChanged;
        public event Action OnDeath;

        float _lastDamageTime = -999f;
        float _lastStaminaUseTime = -999f;

        void Awake()
        {
            Health = maxHealth;
            Stamina = maxStamina;
        }

        void Update()
        {
            if (IsDead) return;

            if (Time.time - _lastDamageTime >= healthRegenDelay && Health < maxHealth)
            {
                SetHealth(Mathf.Min(maxHealth, Health + healthRegenPerSecond * Time.deltaTime));
            }

            if (Time.time - _lastStaminaUseTime >= staminaRegenDelay && Stamina < maxStamina)
            {
                SetStamina(Mathf.Min(maxStamina, Stamina + staminaRegenPerSecond * Time.deltaTime));
            }
        }

        public void TakeDamage(float amount)
        {
            if (IsDead || amount <= 0f) return;

            _lastDamageTime = Time.time;
            SetHealth(Mathf.Max(0f, Health - amount));

            if (Health <= 0f)
            {
                OnDeath?.Invoke();
            }
        }

        public bool TryConsumeStamina(float amount)
        {
            if (Stamina < amount) return false;

            _lastStaminaUseTime = Time.time;
            SetStamina(Stamina - amount);
            return true;
        }

        public void Heal(float amount)
        {
            if (IsDead) return;
            SetHealth(Mathf.Min(maxHealth, Health + amount));
        }

        public void RestoreStamina(float amount)
        {
            if (IsDead) return;
            SetStamina(Mathf.Min(maxStamina, Stamina + amount));
        }

        public void AddMaxHealth(float delta, bool healToFull)
        {
            maxHealth = Mathf.Max(1f, maxHealth + delta);
            if (healToFull) SetHealth(maxHealth);
            else SetHealth(Mathf.Min(maxHealth, Health + Mathf.Max(0f, delta)));
        }

        public void AddMaxStamina(float delta, bool refillToFull)
        {
            maxStamina = Mathf.Max(1f, maxStamina + delta);
            if (refillToFull) SetStamina(maxStamina);
            else SetStamina(Mathf.Min(maxStamina, Stamina + Mathf.Max(0f, delta)));
        }

        public void Revive(float health)
        {
            SetHealth(Mathf.Clamp(health, 1f, maxHealth));
            SetStamina(maxStamina);
        }

        void SetHealth(float value)
        {
            Health = value;
            OnHealthChanged?.Invoke(Health, maxHealth);
        }

        void SetStamina(float value)
        {
            Stamina = value;
            OnStaminaChanged?.Invoke(Stamina, maxStamina);
        }
    }
}
