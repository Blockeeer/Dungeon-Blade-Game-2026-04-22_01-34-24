using UnityEngine;

namespace DungeonBlade.Combat
{
    public class TrainingDummy : MonoBehaviour, IDamageable
    {
        [SerializeField] float maxHealth = 200f;
        [SerializeField] float respawnDelay = 2f;
        [SerializeField] Color flashColor = Color.red;
        [SerializeField] float flashDuration = 0.08f;

        public float Health { get; private set; }
        public bool IsAlive => Health > 0f;

        Renderer[] _renderers;
        Color[] _baseColors;
        float _flashEndTime;
        float _respawnTime = -1f;

        void Awake()
        {
            Health = maxHealth;
            _renderers = GetComponentsInChildren<Renderer>();
            _baseColors = new Color[_renderers.Length];
            for (int i = 0; i < _renderers.Length; i++)
            {
                if (_renderers[i].material.HasProperty("_BaseColor"))
                    _baseColors[i] = _renderers[i].material.GetColor("_BaseColor");
                else if (_renderers[i].material.HasProperty("_Color"))
                    _baseColors[i] = _renderers[i].material.GetColor("_Color");
            }
        }

        void Update()
        {
            if (_flashEndTime > 0f && Time.time >= _flashEndTime)
            {
                _flashEndTime = -1f;
                ApplyColor(false);
            }

            if (_respawnTime > 0f && Time.time >= _respawnTime)
            {
                _respawnTime = -1f;
                Health = maxHealth;
                gameObject.SetActive(true);
            }
        }

        public void ApplyDamage(in DamageInfo info)
        {
            if (!IsAlive) return;

            Health -= info.Amount;
            Debug.Log($"[Dummy] -{info.Amount:F0} ({info.Type})  HP: {Health:F0}/{maxHealth:F0}");

            ApplyColor(true);
            _flashEndTime = Time.time + flashDuration;

            if (Health <= 0f)
            {
                gameObject.SetActive(false);
                _respawnTime = Time.time + respawnDelay;
            }
        }

        void ApplyColor(bool flash)
        {
            for (int i = 0; i < _renderers.Length; i++)
            {
                var mat = _renderers[i].material;
                Color c = flash ? flashColor : _baseColors[i];
                if (mat.HasProperty("_BaseColor")) mat.SetColor("_BaseColor", c);
                else if (mat.HasProperty("_Color")) mat.SetColor("_Color", c);
            }
        }
    }
}
