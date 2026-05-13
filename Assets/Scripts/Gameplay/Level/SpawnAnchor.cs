using UnityEngine;

namespace IC.Gameplay
{
    /// <summary>
    /// Coloque em GOs vazios na cena para definir pontos de spawn.
    /// O LevelSpawner lê todos os SpawnAnchors e instancia o prefab correto.
    /// 
    /// Fluxo de resolução do prefab:
    /// 1. Se PrefabOverride estiver preenchido, usa ele.
    /// 2. Caso contrário, usa o prefab padrão mapeado para SpawnLayer no LevelSpawner.
    /// </summary>
    public class SpawnAnchor : MonoBehaviour
    {
        [Tooltip("Layer de spawn — define qual prefab padrão o LevelSpawner vai usar.")]
        [SerializeField] private SpawnLayer spawnLayer;

        [Tooltip("Prefab específico para este anchor. Sobrescreve o mapeamento padrão.")]
        [SerializeField] private GameObject prefabOverride;

        public SpawnLayer SpawnLayer => spawnLayer;
        public GameObject PrefabOverride => prefabOverride;

        private void OnDrawGizmos()
        {
            Gizmos.color = GizmoColor();
            Gizmos.DrawWireSphere(transform.position, 0.35f);
            Gizmos.DrawWireCube(transform.position, Vector3.one * 0.7f);

#if UNITY_EDITOR
            UnityEditor.Handles.Label(
                transform.position + Vector3.up * 0.5f,
                prefabOverride != null
                    ? $"{spawnLayer} [{prefabOverride.name}]"
                    : spawnLayer.ToString()
            );
#endif
        }

        private Color GizmoColor() => spawnLayer switch
        {
            SpawnLayer.Player  => Color.blue,
            SpawnLayer.Enemy   => Color.red,
            SpawnLayer.PowerUp => Color.yellow,
            SpawnLayer.Exit    => Color.green,
            _                  => Color.white
        };
    }
}
