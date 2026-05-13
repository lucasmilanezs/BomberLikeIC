using System;
using UnityEngine;

namespace IC.Gameplay
{
    public interface IDamagable
    {
        // Overload simples: para entidades com HP (player, inimigo)
        void TakeDamage(int amount);

        // Overload com célula: para tilemaps (breakables)
        void TakeDamage(int amount, Vector3Int cell);
    }

    public class EntityLife : MonoBehaviour, IDamagable
    {
        [Header("Life")]
        [SerializeField] private int maxHealth = 3;

        [Header("Damage Control")]
        [SerializeField] private bool useInvulnerabilityWindow = true;
        [SerializeField] private float invulnerabilitySeconds = 0.2f;

        private float lastDamageTime = -999f;
        private bool isDead;

        public int MaxHealth => maxHealth;
        public int CurrentHealth { get; private set; }
        public bool IsDead => isDead;

        public event Action<int, int> OnHealthChanged;
        public event Action OnDamaged;
        public event Action OnHealed;
        public event Action OnDeath;

        private void Awake()
        {
            CurrentHealth = maxHealth;
        }

        public void TakeDamage(int amount)
        {
            if (isDead) return;
            if (amount <= 0) return;

            if (useInvulnerabilityWindow && Time.time < lastDamageTime + invulnerabilitySeconds)
                return;

            lastDamageTime = Time.time;
            CurrentHealth = Mathf.Max(0, CurrentHealth - amount);

            OnDamaged?.Invoke();
            OnHealthChanged?.Invoke(CurrentHealth, maxHealth);

            if (CurrentHealth == 0)
                Die();
        }

        // Entidades com HP não usam o overload de célula — não faz sentido para elas
        public void TakeDamage(int amount, Vector3Int cell) => TakeDamage(amount);

        public void Heal(int amount)
        {
            if (isDead) return;
            if (amount <= 0) return;

            int previous = CurrentHealth;
            CurrentHealth = Mathf.Min(maxHealth, CurrentHealth + amount);

            if (CurrentHealth != previous)
            {
                OnHealed?.Invoke();
                OnHealthChanged?.Invoke(CurrentHealth, maxHealth);
            }
        }

        public void ResetLife()
        {
            isDead = false;
            CurrentHealth = maxHealth;
            OnHealthChanged?.Invoke(CurrentHealth, maxHealth);
        }

        private void Die()
        {
            if (isDead) return;

            isDead = true;
            OnDeath?.Invoke();
        }
    }
}