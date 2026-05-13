using UnityEngine;

namespace IC.Gameplay
{
    /// <summary>
    /// Coloque no Enemy GO junto com EntityLife.
    /// Escuta OnDeath e destrói o GameObject.
    /// Hook preparado para score, animação de morte, etc.
    /// </summary>
    [RequireComponent(typeof(EntityLife))]
    public class EnemyDeath : MonoBehaviour
    {
        private EntityLife _life;

        private void Awake()
        {
            _life = GetComponent<EntityLife>();
        }

        private void OnEnable()
        {
            _life.OnDeath += HandleDeath;
        }

        private void OnDisable()
        {
            _life.OnDeath -= HandleDeath;
        }

        private void HandleDeath()
        {
            // TODO: score, animação, pooling, etc.
            Debug.Log($"[EnemyDeath] {gameObject.name} morreu.");
            Destroy(gameObject);
        }
    }
}
