using UnityEngine;
using UnityEngine.Tilemaps;
using IC.Core.Utility;

namespace IC.Core
{
    /// <summary>
    /// Ponto central de referências da cena de fase.
    /// Configure no GO "SceneContext" do Level Template — os prefabs
    /// instanciados pelo LevelSpawner lêem daqui as dependências que
    /// não podem ser serializadas no prefab (tilemaps, masks, grid).
    /// </summary>
    public class SceneContext : MonoBehaviour
    {
        public static SceneContext Instance { get; private set; }

        [Header("Grid")]
        public GridService2D Grid;

        [Header("Tilemaps")]
        public Tilemap SolidTilemap;
        public Tilemap BreakableTilemap;
        public Tilemap ExplosionFxTilemap;

        [Header("Layer Masks")]
        public LayerMask SolidMask;
        public LayerMask BreakableMask;
        public LayerMask BombMask;
        public LayerMask DamagableMask;
        public LayerMask ObstacleMask; // Solid + Breakable combinados (usado pelo PlayerGridMover)

        private void Awake()
        {
            Instance = this;
        }

        private void OnDestroy()
        {
            if (Instance == this)
                Instance = null;
        }
    }
}
