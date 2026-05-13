using System.Collections;
using System.Collections.Generic;
using IC.Core.Utility;
using IC.Gameplay;
using UnityEngine;
using UnityEngine.Tilemaps;

namespace IC.Gameplay.Bombs
{
    public class Bomb : MonoBehaviour
    {
        [Header("Physics")]
        [SerializeField] private Collider2D bombCollider;

        [Header("Explosion FX (Tilemap)")]
        [SerializeField] private Tilemap explosionFxTilemap;
        [SerializeField] private TileBase centerTile;
        [SerializeField] private TileBase middleTile;
        [SerializeField] private TileBase endTile;

        [Header("Fallback FX (sprites)")]
        [SerializeField] private bool useSpriteFallbackIfNoTiles = true;

        private static Sprite _fallbackSprite;
        private readonly List<Vector3Int> _paintedCells = new();

        public struct Config
        {
            public BombManager ownerManager;
            public Vector3Int originCell;
            public GridService2D grid;
            public BombStats stats;
            public LayerMask solidMask;
            public LayerMask breakableMask;

            // A bomba detecta IDamagable nessa mask e aplica TakeDamage
            public LayerMask damagableMask;

            public Tilemap fxTilemap;
            public TileBase fxCenter;
            public TileBase fxMiddle;
            public TileBase fxEnd;

            public Collider2D playerColliderToIgnore;
        }

        private Config cfg;
        private bool playerStillOverlapping = true;

        private float explodeAt;

        public Vector3Int OriginCell => cfg.originCell;
        public float TimeToExplode => Mathf.Max(0f, explodeAt - Time.time);
        public int Range => cfg.stats.range;
        public float FuseSeconds => cfg.stats.fuseSeconds;
        public float ExplosionFxSeconds => cfg.stats.explosionFxSeconds;

        public void Init(Config config)
        {
            cfg = config;

            explosionFxTilemap = cfg.fxTilemap;
            centerTile = cfg.fxCenter;
            middleTile = cfg.fxMiddle;
            endTile = cfg.fxEnd;

            if (bombCollider != null)
                bombCollider.isTrigger = true;

            if (bombCollider != null && cfg.playerColliderToIgnore != null)
                Physics2D.IgnoreCollision(bombCollider, cfg.playerColliderToIgnore, true);

            StartCoroutine(FuseRoutine());
        }

        private IEnumerator FuseRoutine()
        {
            explodeAt = Time.time + cfg.stats.fuseSeconds;
            yield return new WaitForSeconds(cfg.stats.fuseSeconds);
            Explode();
        }

        private void Update()
        {
            if (!playerStillOverlapping) return;

            var playerCell = cfg.grid.WorldToCell(cfg.ownerManager.transform.position);
            if (playerCell != cfg.originCell)
            {
                playerStillOverlapping = false;

                if (bombCollider != null)
                {
                    if (cfg.playerColliderToIgnore != null)
                        Physics2D.IgnoreCollision(bombCollider, cfg.playerColliderToIgnore, false);

                    bombCollider.isTrigger = false;
                }

                cfg.ownerManager.OnBombFreedCell(cfg.originCell);
            }
        }

        private void Explode()
        {
            // Dano na célula de origem (pode ter entidade em cima)
            ApplyDamageAt(cfg.originCell);
            PaintExplosion(cfg.originCell, centerTile);

            PropagateStopBeforeObstacle(Vector3Int.right);
            PropagateStopBeforeObstacle(Vector3Int.left);
            PropagateStopBeforeObstacle(Vector3Int.up);
            PropagateStopBeforeObstacle(Vector3Int.down);

            StartCoroutine(ClearFxAfter(cfg.stats.explosionFxSeconds));

            cfg.ownerManager.OnBombExploded(cfg.originCell);
            Destroy(gameObject);
        }

        private void PropagateStopBeforeObstacle(Vector3Int dir)
        {
            bool paintedAny = false;
            Vector3Int lastPaintedCell = cfg.originCell;

            for (int i = 1; i <= cfg.stats.range; i++)
            {
                var cell = cfg.originCell + dir * i;
                var center = cfg.grid.CellCenterWorld(cell);

                bool hitSolid = Physics2D.OverlapPoint(center, cfg.solidMask) != null;
                bool hitBreakable = Physics2D.OverlapPoint(center, cfg.breakableMask) != null;

                if (hitSolid)
                    break;

                // MUDANÇA: notifica via IDamagable — breakable ou entidade viva
                // A bomba não sabe o que acontece depois
                ApplyDamageAt(cell);

                if (hitBreakable)
                {
                    // Para a propagação mas ainda pinta o FX na célula do breakable
                    PaintExplosion(cell, middleTile ? middleTile : centerTile);
                    if (paintedAny)
                        UpgradeCellToEnd(lastPaintedCell);
                    UpgradeCellToEnd(cell);
                    break;
                }

                PaintExplosion(cell, middleTile ? middleTile : centerTile);
                paintedAny = true;
                lastPaintedCell = cell;

                if (i == cfg.stats.range)
                    UpgradeCellToEnd(lastPaintedCell);
            }
        }

        /// <summary>
        /// Busca IDamagable na célula e aplica dano.
        /// Para breakables (tilemap), passa a célula no overload correto.
        /// Para entidades (player/enemy), usa o overload simples.
        /// </summary>
        private void ApplyDamageAt(Vector3Int cell)
        {
            var center = cfg.grid.CellCenterWorld(cell);

            // Entidades vivas (player, enemy) — overload simples
            var entityHits = Physics2D.OverlapPointAll(center, cfg.damagableMask);
            foreach (var hit in entityHits)
            {
                var damagable = hit.GetComponent<IDamagable>();
                damagable?.TakeDamage(1);
            }

            // Breakable tilemap — overload com célula
            var breakableHit = Physics2D.OverlapPoint(center, cfg.breakableMask);
            if (breakableHit != null)
            {
                var damagable = breakableHit.GetComponent<IDamagable>();
                damagable?.TakeDamage(1, cell);
            }
        }

        private void UpgradeCellToEnd(Vector3Int cell)
        {
            if (explosionFxTilemap && (endTile || middleTile || centerTile))
            {
                var use = endTile ? endTile : (middleTile ? middleTile : centerTile);
                explosionFxTilemap.SetTile(cell, use);
            }
        }

        private void PaintExplosion(Vector3Int cell, TileBase tileToUse)
        {
            _paintedCells.Add(cell);

            if (explosionFxTilemap && tileToUse)
            {
                explosionFxTilemap.SetTile(cell, tileToUse);
                return;
            }

            if (useSpriteFallbackIfNoTiles)
            {
                if (_fallbackSprite == null)
                {
                    var tex = new Texture2D(1, 1, TextureFormat.RGBA32, false);
                    tex.SetPixel(0, 0, Color.white);
                    tex.Apply();
                    _fallbackSprite = Sprite.Create(tex, new Rect(0, 0, 1, 1), new Vector2(0.5f, 0.5f), 1f);
                }

                var world = cfg.grid.CellCenterWorld(cell);
                var go = new GameObject("ExplosionFallback");
                var sr = go.AddComponent<SpriteRenderer>();
                sr.sprite = _fallbackSprite;
                sr.sortingLayerName = "FX";
                go.transform.position = world;
                go.transform.localScale = Vector3.one;
                Destroy(go, cfg.stats.explosionFxSeconds);
            }
        }

        private IEnumerator ClearFxAfter(float seconds)
        {
            yield return new WaitForSeconds(seconds);
            if (explosionFxTilemap)
            {
                foreach (var c in _paintedCells)
                    explosionFxTilemap.SetTile(c, null);
                _paintedCells.Clear();
            }
        }
    }
}