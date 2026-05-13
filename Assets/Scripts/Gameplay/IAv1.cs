using System.Collections.Generic;
using UnityEngine;
using UnityEngine.Tilemaps;
using IC.Core.Utility;
using IC.Inputs;
using IC.Gameplay.Bombs;

/*
namespace IC.AI
{
    public class AStarChaser : MonoBehaviour, IInputSource
    {
        [Header("Refs")]
        [SerializeField] private GridService2D grid;
        [SerializeField] private Tilemap solidMap;
        [SerializeField] private Tilemap breakableMap;
        [SerializeField] private Transform player;

        [Header("Masks")]
        [SerializeField] private LayerMask solidMask;
        [SerializeField] private LayerMask breakableMask;
        [SerializeField] private LayerMask bombMask;

        [Header("Config")]
        [SerializeField] private float thinkInterval = 0.12f;
        [SerializeField] private float stepDuration = 1f / 7f;
        [SerializeField] private float bombCooldown = 2.5f;
        [SerializeField] private float fxDuration = 0.35f;

        [Header("Debug")]
        [SerializeField] private bool debugMode;
        [SerializeField] private Tilemap debugTilemap;
        [SerializeField] private TileBase debugTile;

        private float nextThink;
        private float nextBombTime;
        private bool shouldPlant;
        private Vector2 currentDir;
        private List<Vector3Int> path;
        private int pathIndex;
        private Dictionary<Vector3Int, DangerWin> lastDangerMap;
        private Vector3Int lastCell;
        private Mode currentMode;
        private readonly List<Vector3Int> debugPaint = new();

        private enum Mode { Idle, Chase, SeekBreakables }

        public Vector2 ReadMove() => currentDir;

        public bool ConsumePlaceBombPressed()
        {
            bool plant = shouldPlant;
            shouldPlant = false;
            return plant;
        }

        private void Update()
        {
            if (!grid || !player) return;
            if (Time.time < nextThink) return;
            nextThink = Time.time + thinkInterval;

            var danger = BuildDangerIndex();
            lastDangerMap = danger;
            var me = grid.WorldToCell(transform.position);
            lastCell = me;

            if (IsInDanger(me, danger))
            {
                PlanShelter(me, danger);
                shouldPlant = false;
                return;
            }

            if (path == null || path.Count <= 1 || Random.value < 0.05f)
            {
                currentMode = (Random.value > 0.5f) ? Mode.Chase : Mode.SeekBreakables;
            }

            switch (currentMode)
            {
                case Mode.Chase:
                    PlanChasePlayer(me, danger);
                    break;
                case Mode.SeekBreakables:
                    PlanDestroyBreakables(me, danger);
                    break;
            }

            FollowPath(me, danger);
        }

        private void PlanShelter(Vector3Int me, Dictionary<Vector3Int, DangerWin> danger)
        {
            for (int r = 1; r <= 10; r++)
            {
                foreach (var target in Ring(me, r))
                {
                    if (!IsPassable(target)) continue;
                    var p = FindPath(me, target, danger);
                    if (p != null && PathIsSafe(p, me, danger))
                    {
                        SetPath(p);
                        return;
                    }
                }
            }
            SetPath(new List<Vector3Int> { me });
        }

        private bool PathIsSafe(List<Vector3Int> path, Vector3Int start, Dictionary<Vector3Int, DangerWin> danger)
        {
            for (int i = 1; i < path.Count; i++)
            {
                float eta = i * stepDuration;
                if (danger.TryGetValue(path[i], out var w) && eta >= w.open && eta <= w.close)
                    return false;
            }
            return true;
        }

        private void PlanChasePlayer(Vector3Int me, Dictionary<Vector3Int, DangerWin> danger)
        {
            var target = grid.WorldToCell(player.position);
            if (Manhattan(me, target) == 1 && Time.time >= nextBombTime)
            {
                shouldPlant = true;
                nextBombTime = Time.time + bombCooldown;
                PlanShelter(me, danger);
                currentMode = (Random.value > 0.5f) ? Mode.Chase : Mode.SeekBreakables;
                return;
            }
            var p = FindPath(me, target, danger, avoidBreakables: true);
            if (p != null && p.Count > 1)
            {
                SetPath(p);
            }
            else if (breakableMap.HasTile(target) && Time.time >= nextBombTime)
            {
                shouldPlant = true;
                nextBombTime = Time.time + bombCooldown;
                PlanShelter(me, danger);
                currentMode = (Random.value > 0.5f) ? Mode.Chase : Mode.SeekBreakables;
            }
        }

        private void PlanDestroyBreakables(Vector3Int me, Dictionary<Vector3Int, DangerWin> danger)
        {
            for (int r = 1; r <= 8; r++)
            {
                foreach (var tile in Ring(me, r))
                {
                    if (breakableMap && breakableMap.HasTile(tile))
                    {
                        var p = FindPath(me, tile, danger);
                        if (p != null && p.Count > 1)
                        {
                            SetPath(p);
                            if (Manhattan(me, tile) == 1 && Time.time >= nextBombTime)
                            {
                                shouldPlant = true;
                                nextBombTime = Time.time + bombCooldown;
                                PlanShelter(me, danger);
                                currentMode = (Random.value > 0.5f) ? Mode.Chase : Mode.SeekBreakables;
                            }
                            return;
                        }
                    }
                }
            }
        }

        private void FollowPath(Vector3Int me, Dictionary<Vector3Int, DangerWin> danger)
        {
            if (path == null || path.Count < 2 || pathIndex >= path.Count - 1)
            {
                currentDir = Vector2.zero;
                return;
            }

            if (me == path[pathIndex]) pathIndex++;
            if (pathIndex >= path.Count - 1)
            {
                currentDir = Vector2.zero;
                return;
            }

            var next = path[pathIndex + 1];
            if (!IsPassable(next) || IsInDanger(next, danger))
            {
                nextThink = Time.time;
                currentDir = Vector2.zero;
                return;
            }

            currentDir = ToCardinal(next - me);
        }

        private List<Vector3Int> FindPath(Vector3Int start, Vector3Int goal, Dictionary<Vector3Int, DangerWin> danger, bool avoidBreakables = false)
        {
            var open = new List<Vector3Int> { start };
            var came = new Dictionary<Vector3Int, Vector3Int> { [start] = start };
            var g = new Dictionary<Vector3Int, float> { [start] = 0f };

            while (open.Count > 0)
            {
                Vector3Int cur = open[0];
                float bestF = g[cur] + Manhattan(cur, goal);
                foreach (var c in open)
                {
                    float f = g[c] + Manhattan(c, goal);
                    if (f < bestF) { bestF = f; cur = c; }
                }

                if (cur == goal) return Reconstruct(cur, came);
                open.Remove(cur);

                foreach (var n in CardinalNeighbors(cur))
                {
                    if (!IsPassable(n)) continue;
                    if (avoidBreakables && breakableMap && breakableMap.HasTile(n)) continue;

                    float tentative = g[cur] + 1f;
                    if (!g.ContainsKey(n) || tentative < g[n])
                    {
                        g[n] = tentative;
                        came[n] = cur;
                        if (!open.Contains(n)) open.Add(n);
                    }
                }
            }
            return null;
        }

        private List<Vector3Int> Reconstruct(Vector3Int cur, Dictionary<Vector3Int, Vector3Int> came)
        {
            var path = new List<Vector3Int>();
            while (came.ContainsKey(cur) && came[cur] != cur)
            {
                path.Add(cur);
                cur = came[cur];
            }
            path.Add(cur);
            path.Reverse();
            return path;
        }

        private void SetPath(List<Vector3Int> newPath)
        {
            path = newPath;
            pathIndex = 0;

            if (debugMode && debugTilemap && debugTile)
            {
                foreach (var c in debugPaint) debugTilemap.SetTile(c, null);
                debugPaint.Clear();
                foreach (var c in path)
                {
                    debugTilemap.SetTile(c, debugTile);
                    debugPaint.Add(c);
                }
            }
        }

        private bool IsPassable(Vector3Int c)
        {
            if (solidMap && solidMap.HasTile(c)) return false;
            if (Physics2D.OverlapPoint(grid.CellCenterWorld(c), solidMask | bombMask) != null) return false;
            return true;
        }

        private bool IsInDanger(Vector3Int c, Dictionary<Vector3Int, DangerWin> danger)
            => danger.TryGetValue(c, out var w) && w.open <= stepDuration && w.close >= 0f;

        private Dictionary<Vector3Int, DangerWin> BuildDangerIndex()
        {
            var map = new Dictionary<Vector3Int, DangerWin>();
            foreach (var bomb in FindObjectsOfType<Bomb>())
            {
                float open = bomb.TimeToExplode;
                float close = open + fxDuration;

                MarkDanger(map, bomb.OriginCell, open, close);
                foreach (var d in CardinalNeighbors(Vector3Int.zero))
                {
                    for (int i = 1; i <= bomb.Range; i++)
                    {
                        var cell = bomb.OriginCell + d * i;
                        var pos = grid.CellCenterWorld(cell);
                        if (Physics2D.OverlapPoint(pos, solidMask) != null) break;
                        MarkDanger(map, cell, open, close);
                        if (Physics2D.OverlapPoint(pos, breakableMask) != null) break;
                    }
                }
            }
            return map;
        }

        private void MarkDanger(Dictionary<Vector3Int, DangerWin> map, Vector3Int c, float open, float close)
        {
            if (map.TryGetValue(c, out var w))
                map[c] = new DangerWin { open = Mathf.Min(w.open, open), close = Mathf.Max(w.close, close) };
            else
                map[c] = new DangerWin { open = open, close = close };
        }

        private static IEnumerable<Vector3Int> Ring(Vector3Int c, int r)
        {
            for (int dx = -r; dx <= r; dx++)
            {
                yield return new Vector3Int(c.x + dx, c.y + r, 0);
                yield return new Vector3Int(c.x + dx, c.y - r, 0);
            }
            for (int dy = -r + 1; dy <= r - 1; dy++)
            {
                yield return new Vector3Int(c.x + r, c.y + dy, 0);
                yield return new Vector3Int(c.x - r, c.y + dy, 0);
            }
        }

        private static IEnumerable<Vector3Int> CardinalNeighbors(Vector3Int c)
        {
            yield return c + Vector3Int.right;
            yield return c + Vector3Int.left;
            yield return c + Vector3Int.up;
            yield return c + Vector3Int.down;
        }

        private static int Manhattan(Vector3Int a, Vector3Int b)
            => Mathf.Abs(a.x - b.x) + Mathf.Abs(a.y - b.y);

        private static Vector2 ToCardinal(Vector3Int d)
            => Mathf.Abs(d.x) > Mathf.Abs(d.y) ? new Vector2(Mathf.Sign(d.x), 0) : new Vector2(0, Mathf.Sign(d.y));

        private void OnDrawGizmos()
        {
            if (!debugMode || grid == null) return;

            Gizmos.color = Color.red;
            if (lastDangerMap != null)
            {
                foreach (var kv in lastDangerMap)
                {
                    var center = grid.CellCenterWorld(kv.Key);
                    Gizmos.DrawCube(center, Vector3.one * 0.3f);
                }
            }

            Gizmos.color = Color.cyan;
            if (path != null)
            {
                foreach (var step in path)
                {
                    var center = grid.CellCenterWorld(step);
                    Gizmos.DrawWireCube(center, Vector3.one * 0.4f);
                }
            }

            Gizmos.color = Color.green;
            Gizmos.DrawWireSphere(grid.CellCenterWorld(lastCell), 0.3f);

            GUIStyle style = new GUIStyle();
            style.normal.textColor = Color.white;
#if UNITY_EDITOR
            UnityEditor.Handles.Label(grid.CellCenterWorld(lastCell) + Vector3.up * 0.5f, $"Mode: {currentMode}", style);
#endif
        }

        private struct DangerWin { public float open, close; }
    }
}
*/