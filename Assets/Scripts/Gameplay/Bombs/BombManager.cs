using System.Collections.Generic;
using IC.Core.Utility;
using IC.Core;
using IC.Inputs;
using UnityEngine;
using UnityEngine.Tilemaps;

namespace IC.Gameplay.Bombs
{
    public class BombManager : MonoBehaviour
    {
        [Header("Refs")]
        [SerializeField] private GridService2D gridService;
        [SerializeField] private BombStats stats;
        [SerializeField] private Bomb bombPrefab;

        [Header("Level Refs")]
        [SerializeField] private Tilemap breakableTilemap;
        [SerializeField] private LayerMask solidMask;
        [SerializeField] private LayerMask breakableMask;

        // MUDANÇA: mask para entidades vivas (Player + Enemy layers)
        // Configure no Inspector com as layers Player e Enemy marcadas
        [SerializeField] private LayerMask damagableMask;

        [Header("Explosion FX (Tilemap)")]
        [SerializeField] private Tilemap explosionFxTilemap;
        [SerializeField] private TileBase fxCenterTile;
        [SerializeField] private TileBase fxMiddleTile;
        [SerializeField] private TileBase fxEndTile;

        private IInputSource input;
        private readonly HashSet<Vector3Int> occupiedCells = new();
        private int activeCount;
        private Collider2D playerCollider;

        private void Awake()
        {
            input = GetComponent<IInputSource>();
            playerCollider = GetComponent<Collider2D>();
            if (SceneContext.Instance != null)
            {
                if (!gridService) gridService = SceneContext.Instance.Grid;
                if (!breakableTilemap) breakableTilemap = SceneContext.Instance.BreakableTilemap;
                if (!explosionFxTilemap) explosionFxTilemap = SceneContext.Instance.ExplosionFxTilemap;
                if (solidMask == 0) solidMask = SceneContext.Instance.SolidMask;
                if (breakableMask == 0) breakableMask = SceneContext.Instance.BreakableMask;
                if (damagableMask == 0) damagableMask = SceneContext.Instance.DamagableMask;
            }
        }

        private void Update()
        {
            if (input == null) return;

            if (input.ConsumePlaceBombPressed())
                TryPlaceBomb();
        }

        private void TryPlaceBomb()
        {
            if (activeCount >= stats.maxSimultaneous) return;

            var cell = gridService.WorldToCell(transform.position);
            if (occupiedCells.Contains(cell)) return;

            var center = gridService.CellCenterWorld(cell);
            var bomb = Instantiate(bombPrefab, center, Quaternion.identity);

            bomb.Init(new Bomb.Config
            {
                ownerManager = this,
                originCell = cell,
                grid = gridService,
                stats = stats,
                solidMask = solidMask,
                breakableMask = breakableMask,
                damagableMask = damagableMask,  // MUDANÇA: passa a nova mask
                playerColliderToIgnore = playerCollider,

                fxTilemap = explosionFxTilemap,
                fxCenter = fxCenterTile,
                fxMiddle = fxMiddleTile,
                fxEnd = fxEndTile
            });

            occupiedCells.Add(cell);
            activeCount++;
        }

        public void OnBombFreedCell(Vector3Int cell) => occupiedCells.Remove(cell);

        public void OnBombExploded(Vector3Int cell)
        {
            occupiedCells.Remove(cell);
            activeCount = Mathf.Max(0, activeCount - 1);
        }
    }
}