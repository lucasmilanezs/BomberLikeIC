using System.Collections.Generic;
using UnityEngine;
using UnityEngine.Tilemaps;

using IC.Core;
using IC.Core.Utility;
using IC.Gameplay.Bombs;
using IC.Gameplay.Movement;
using IC.Inputs;

#if UNITY_EDITOR
using UnityEditor;
#endif

namespace IC.Input.AI
{
    [RequireComponent(typeof(PlayerGridMover))]
    [RequireComponent(typeof(BombManager))]
    public class AiCorePathfinder : MonoBehaviour, IInputSource
    {
        // ----------------- Refs gerais -----------------
        [Header("Grid & Level Refs")]
        [SerializeField] private GridService2D gridService;
        [SerializeField] private Tilemap solidTilemap;
        [SerializeField] private Tilemap breakableTilemap;
        [SerializeField] private Transform player;

        [Header("Masks")]
        [SerializeField] private LayerMask solidMask;
        [SerializeField] private LayerMask breakableMask;
        [SerializeField] private LayerMask bombMask;

        [Header("Pathfinding")]
        [Tooltip("Custo extra para atravessar tiles quebráveis no A* (permite vs evita).")]
        [SerializeField] private float breakablePenalty = 5f;
        [Tooltip("Limite de nós explorados no A* para evitar loops/estouros.")]
        [SerializeField] private int maxNodesExplored = 2000;

        [Header("Fallback Behavior")]
        [Tooltip("Se nenhum comportamento for válido, ativa wander em vez de Idle.")]
        [SerializeField] private bool enableWanderFallback = true;

        [Header("Performance")]
        [SerializeField] private float thinkInterval = 0.15f;

        [Header("Debug")]
        [SerializeField] private bool debugMode = true;
        [SerializeField] private bool drawDanger = true;
        [SerializeField] private bool drawPathToPlayer = true;
        [SerializeField] private bool drawActivePath = true;

        [Header("Behavior Modules")]
        [Tooltip("Lista de scripts que implementam IAiBehaviorModule. A ordem aqui não define prioridade; a prioridade é numérica.")]
        [SerializeField] private List<MonoBehaviour> behaviorModules = new();

        // ----------------- Estado interno -----------------
        private readonly List<IAiBehaviorModule> _behaviors = new();
        private readonly HashSet<Vector3Int> _dangerCells = new();
        private readonly List<Vector3Int> _pathToPlayer = new();

        private PlayerGridMover _mover;
        private BombManager _bombManager;

        private List<Vector3Int> _activePath;
        private int _activePathIndex;
        private string _currentBehaviorId = "Idle";

        private int _breakablesOnPathToPlayer;

        private bool _bombRequest;

        private float _nextThinkTime;

        private static readonly Vector3Int[] DIRS =
        {
            Vector3Int.right,
            Vector3Int.left,
            Vector3Int.up,
            Vector3Int.down
        };

        // ----------------- Propriedades públicas -----------------

        public IReadOnlyList<Vector3Int> PathToPlayer => _pathToPlayer;
        public int BreakablesOnPathToPlayer => _breakablesOnPathToPlayer;
        public IReadOnlyCollection<Vector3Int> DangerMap => _dangerCells;
        public string CurrentBehaviorId => _currentBehaviorId;

        // =========================================================
        //  Ciclo de vida
        // =========================================================

        private void Awake()
        {
            _mover = GetComponent<PlayerGridMover>();
            _bombManager = GetComponent<BombManager>();

            foreach (var mb in behaviorModules)
            {
                if (mb is IAiBehaviorModule mod)
                    _behaviors.Add(mod);
            }
        }

        private void Start()
        {
            if (SceneContext.Instance != null)
            {
                if (!gridService) gridService = SceneContext.Instance.Grid;
                if (!solidTilemap) solidTilemap = SceneContext.Instance.SolidTilemap;
                if (!breakableTilemap) breakableTilemap = SceneContext.Instance.BreakableTilemap;
                if (solidMask == 0) solidMask = SceneContext.Instance.SolidMask;
                if (breakableMask == 0) breakableMask = SceneContext.Instance.BreakableMask;
                if (bombMask == 0) bombMask = SceneContext.Instance.BombMask;
            }
            if (!player)
                player = GameObject.FindGameObjectWithTag("Player")?.transform;
            if (!gridService)
                gridService = FindObjectOfType<GridService2D>();

            if (!player)
                player = GameObject.FindGameObjectWithTag("Player")?.transform;
        }

        private void Update()
        {
            if (thinkInterval > 0f && Time.time < _nextThinkTime) return;
            if (thinkInterval > 0f) _nextThinkTime = Time.time + thinkInterval;

            if (!player)
                player = GameObject.FindGameObjectWithTag("Player")?.transform;

            if (!gridService || !player)
            {
                _currentBehaviorId = "Idle (Missing refs)";
                _activePath = null;
                return;
            }

            var selfCell = gridService.WorldToCell(transform.position);
            var playerCell = gridService.WorldToCell(player.position);

            RebuildDangerMap();

            _pathToPlayer.Clear();
            _breakablesOnPathToPlayer = 0;

            var pathToPlayer = FindPath(selfCell, playerCell, avoidDanger: true, out _breakablesOnPathToPlayer);
            if (pathToPlayer != null)
                _pathToPlayer.AddRange(pathToPlayer);

            var ctx = new AiContext
            {
                selfCell = selfCell,
                playerCell = playerCell,
                pathToPlayer = _pathToPlayer,
                breakablesOnPathToPlayer = _breakablesOnPathToPlayer,
                dangerCells = _dangerCells,
                grid = gridService,
                activePath = _activePath,
                activePathIndex = _activePathIndex
            };

            SelectBehaviorAndPath(ctx);
        }

        // =========================================================
        //  Seleção de comportamento
        // =========================================================

        private void SelectBehaviorAndPath(AiContext ctx)
        {
            BehaviorDecision best = BehaviorDecision.Invalid;
            string bestId = "Idle";

            foreach (var mod in _behaviors)
            {
                if (mod == null || !mod.Enabled)
                    continue;

                var decision = mod.Evaluate(this, ctx);
                if (!decision.isValid || decision.path == null || decision.path.Count == 0)
                    continue;

                bool isBetter =
                    decision.priority > best.priority ||
                    (decision.priority == best.priority &&
                     best.path != null &&
                     decision.path.Count < best.path.Count);

                if (isBetter)
                {
                    best = decision;
                    bestId = mod.BehaviorId;
                }
            }

            if (best.isValid && best.path != null && best.path.Count > 0)
            {
                var newPath = best.path;

                bool destinationChanged = _activePath == null ||
                                          _activePath.Count == 0 ||
                                          newPath[newPath.Count - 1] != _activePath[_activePath.Count - 1] ||
                                          (_activePathIndex < newPath.Count &&
                                           _activePathIndex < _activePath.Count &&
                                           newPath[_activePathIndex] != _activePath[_activePathIndex]);

                _activePath = new List<Vector3Int>(newPath);

                if (destinationChanged)
                    _activePathIndex = 0;

                _currentBehaviorId = bestId;

                if (best.wantsBombNow)
                    _bombRequest = true;

                return;
            }

            if (enableWanderFallback)
            {
                if (BuildWanderPath(ctx))
                {
                    _currentBehaviorId = "Wander";
                    return;
                }
            }

            _activePath = null;
            _currentBehaviorId = "Idle";
        }

        private bool BuildWanderPath(AiContext ctx)
        {
            var neighbors = new List<Vector3Int>();

            foreach (var d in DIRS)
            {
                var c = ctx.selfCell + d;
                if (IsSolid(c)) continue;
                if (_dangerCells.Contains(c)) continue;
                neighbors.Add(c);
            }

            if (neighbors.Count == 0)
            {
                _activePath = null;
                return false;
            }

            var next = neighbors[Random.Range(0, neighbors.Count)];
            _activePath = new List<Vector3Int> { ctx.selfCell, next };
            _activePathIndex = 0;
            return true;
        }

        // =========================================================
        //  Pathfinding A*
        // =========================================================

        public List<Vector3Int> FindPathTo(Vector3Int target, bool avoidDanger, out int breakablesOnPath)
        {
            var start = gridService.WorldToCell(transform.position);
            return FindPath(start, target, avoidDanger, out breakablesOnPath);
        }

        private List<Vector3Int> FindPath(Vector3Int start, Vector3Int goal, bool avoidDanger, out int breakablesOnPath)
        {
            breakablesOnPath = 0;

            if (start == goal)
                return new List<Vector3Int> { start };

            var open = new PriorityQueue<Vector3Int>();
            var cameFrom = new Dictionary<Vector3Int, Vector3Int>();
            var gScore = new Dictionary<Vector3Int, float> { [start] = 0f };

            open.Enqueue(start, Heuristic(start, goal));
            int explored = 0;

            while (open.Count > 0)
            {
                if (++explored > maxNodesExplored) break;

                var current = open.Dequeue();
                if (current == goal)
                    return ReconstructPath(cameFrom, current, out breakablesOnPath);

                foreach (var dir in DIRS)
                {
                    var neighbor = current + dir;

                    if (IsSolid(neighbor)) continue;

                    bool isBreakable = IsBreakable(neighbor);
                    float cost = 1f + (isBreakable ? breakablePenalty : 0f);

                    if (avoidDanger && _dangerCells.Contains(neighbor))
                        cost += 1000f;

                    float tentativeG = gScore[current] + cost;

                    if (!gScore.TryGetValue(neighbor, out float existingG) || tentativeG < existingG)
                    {
                        gScore[neighbor] = tentativeG;
                        float f = tentativeG + Heuristic(neighbor, goal);
                        open.Enqueue(neighbor, f);
                        cameFrom[neighbor] = current;
                    }
                }
            }

            return null;
        }

        public bool IsCellSolid(Vector3Int cell) => IsSolid(cell);
        public bool IsCellBreakable(Vector3Int cell) => IsBreakable(cell);

        private float Heuristic(Vector3Int a, Vector3Int b)
            => Mathf.Abs(a.x - b.x) + Mathf.Abs(a.y - b.y);

        private List<Vector3Int> ReconstructPath(Dictionary<Vector3Int, Vector3Int> cameFrom, Vector3Int current, out int breakablesOnPath)
        {
            var path = new List<Vector3Int> { current };
            breakablesOnPath = 0;

            while (cameFrom.TryGetValue(current, out var prev))
            {
                current = prev;
                path.Add(current);
            }

            path.Reverse();

            for (int i = 1; i < path.Count; i++)
                if (IsBreakable(path[i])) breakablesOnPath++;

            return path;
        }

        private bool IsSolid(Vector3Int cell)
        {
            if (solidTilemap && solidTilemap.HasTile(cell)) return true;
            var center = gridService.CellCenterWorld(cell);
            return Physics2D.OverlapPoint(center, solidMask) != null;
        }

        private bool IsBreakable(Vector3Int cell)
        {
            if (breakableTilemap && breakableTilemap.HasTile(cell)) return true;
            var center = gridService.CellCenterWorld(cell);
            return Physics2D.OverlapPoint(center, breakableMask) != null;
        }

        // =========================================================
        //  Danger Map
        // =========================================================

        private void RebuildDangerMap()
        {
            _dangerCells.Clear();

            var hits = Physics2D.OverlapCircleAll(transform.position, 50f, bombMask);
            if (hits != null && hits.Length > 0)
            {
                foreach (var h in hits)
                {
                    var bomb = h.GetComponent<Bomb>();
                    if (bomb == null) continue;
                    MarkDangerFromBomb(bomb);
                }
            }
            else
            {
                var allBombs = FindObjectsOfType<Bomb>();
                foreach (var bomb in allBombs)
                    MarkDangerFromBomb(bomb);
            }
        }

        private void MarkDangerFromBomb(Bomb bomb)
        {
            var origin = bomb.OriginCell;
            int range = bomb.Range;

            _dangerCells.Add(origin);

            foreach (var dir in DIRS)
            {
                for (int i = 1; i <= range; i++)
                {
                    var cell = origin + dir * i;

                    if (IsSolid(cell)) break;

                    _dangerCells.Add(cell);

                    if (IsBreakable(cell)) break;
                }
            }
        }

        // =========================================================
        //  IInputSource
        // =========================================================

        public Vector2 ReadMove()
        {
            if (_activePath == null || _activePath.Count == 0 || gridService == null)
                return Vector2.zero;

            var myCell = gridService.WorldToCell(transform.position);

            while (_activePathIndex < _activePath.Count && _activePath[_activePathIndex] == myCell)
                _activePathIndex++;

            if (_activePathIndex >= _activePath.Count)
                return Vector2.zero;

            var targetCell = _activePath[_activePathIndex];
            var delta = targetCell - myCell;

            if (delta == Vector3Int.zero) return Vector2.zero;

            if (Mathf.Abs(delta.x) > Mathf.Abs(delta.y))
                delta.y = 0;
            else
                delta.x = 0;

            return new Vector2(Mathf.Clamp(delta.x, -1, 1), Mathf.Clamp(delta.y, -1, 1));
        }

        public bool ConsumePlaceBombPressed()
        {
            if (!_bombRequest) return false;
            _bombRequest = false;
            return true;
        }

        public void RequestBombNow()
        {
            _bombRequest = true;
        }

        // =========================================================
        //  Gizmos
        // =========================================================

        private void OnDrawGizmosSelected()
        {
            if (!debugMode || gridService == null) return;

            var selfCell = gridService.WorldToCell(transform.position);
            var selfCenter = gridService.CellCenterWorld(selfCell);

            Gizmos.color = Color.yellow;
            Gizmos.DrawWireCube(selfCenter, Vector3.one * 0.4f);

            if (drawDanger)
            {
                Gizmos.color = new Color(1f, 0f, 0f, 0.3f);
                foreach (var c in _dangerCells)
                    Gizmos.DrawCube(gridService.CellCenterWorld(c), Vector3.one * 0.35f);
            }

            if (drawPathToPlayer && _pathToPlayer != null && _pathToPlayer.Count > 1)
            {
                for (int i = 0; i < _pathToPlayer.Count; i++)
                {
                    var cell = _pathToPlayer[i];
                    var center = gridService.CellCenterWorld(cell);
                    Gizmos.color = IsBreakable(cell) ? Color.red : Color.cyan;
                    Gizmos.DrawWireCube(center, Vector3.one * 0.3f);
                    if (i > 0)
                        Gizmos.DrawLine(gridService.CellCenterWorld(_pathToPlayer[i - 1]), center);
                }
            }

            if (drawActivePath && _activePath != null && _activePath.Count > 1)
            {
                Gizmos.color = Color.green;
                for (int i = 0; i < _activePath.Count; i++)
                {
                    var center = gridService.CellCenterWorld(_activePath[i]);
                    Gizmos.DrawWireCube(center, Vector3.one * 0.27f);
                    if (i > 0)
                        Gizmos.DrawLine(gridService.CellCenterWorld(_activePath[i - 1]), center);
                }
            }

#if UNITY_EDITOR
            Handles.color = Color.white;
            Handles.Label(selfCenter + Vector3.up * 0.6f, $"AI: {CurrentBehaviorId}");
#endif
        }

        // =========================================================
        //  PriorityQueue A*
        // =========================================================

        private class PriorityQueue<T>
        {
            private readonly List<(T item, float priority)> _items = new();

            public int Count => _items.Count;

            public void Enqueue(T item, float priority)
                => _items.Add((item, priority));

            public T Dequeue()
            {
                int bestIndex = 0;
                float bestPriority = _items[0].priority;

                for (int i = 1; i < _items.Count; i++)
                {
                    if (_items[i].priority < bestPriority)
                    {
                        bestPriority = _items[i].priority;
                        bestIndex = i;
                    }
                }

                var item = _items[bestIndex].item;
                _items.RemoveAt(bestIndex);
                return item;
            }
        }
    }
}