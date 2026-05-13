using UnityEngine;
using UnityEngine.Tilemaps;

namespace IC.Gameplay
{
    /// <summary>
    /// Coloque no mesmo GameObject do Tilemap de breakables.
    /// Implementa IDamagable para que a bomba possa notificar via interface,
    /// sem conhecer o tilemap diretamente.
    /// </summary>
    [RequireComponent(typeof(Tilemap))]
    public class BreakableLife : MonoBehaviour, IDamagable
    {
        private Tilemap _tilemap;

        private void Awake()
        {
            _tilemap = GetComponent<Tilemap>();
        }

        // Breakables não têm HP contável — qualquer dano os remove diretamente
        public void TakeDamage(int amount)
        {
            Debug.LogWarning("[BreakableLife] TakeDamage sem célula chamado — use o overload com Vector3Int.");
        }

        public void TakeDamage(int amount, Vector3Int cell)
        {
            if (_tilemap == null) return;
            _tilemap.SetTile(cell, null);
        }
    }
}
