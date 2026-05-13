using System.Collections.Generic;
using System.Linq;
using UnityEngine;
using IC.Input.AI;

namespace IC.Input.AI.Behaviors
{
    /// <summary>
    /// Comportamento de fuga:
    /// - Ativa APENAS quando a célula atual está em dangerCells.
    /// - Procura o tile seguro mais próximo evitando sólidos e breakables.
    /// - Apenas move; nunca planta bomba.
    /// - Quando o inimigo sai do perigo, libera o controle imediatamente.
    ///   O WaitSafe assume se ainda estiver encurralado.
    /// </summary>
    public class AiBehaviorFindShelter : MonoBehaviour, IAiBehaviorModule
    {
        [Header("General")]
        [SerializeField] private bool enabledModule = true;

        [Tooltip("Prioridade do comportamento de fuga. Deve ser maior que WaitSafe (50) e ChasePlayer (10).")]
        [SerializeField] private int priority = 100;

        [Header("Search Settings")]
        [Tooltip("Raio máximo, em tiles, para procurar abrigo ao redor.")]
        [SerializeField] private int maxSearchRadius = 8;

        public string BehaviorId => "FindShelter";
        public bool Enabled => enabledModule && isActiveAndEnabled;

        public BehaviorDecision Evaluate(AiCorePathfinder core, AiContext ctx)
        {
            var decision = BehaviorDecision.Invalid;

            // Só ativa quando está em perigo
            if (ctx.dangerCells == null || !ctx.dangerCells.Contains(ctx.selfCell))
                return decision;

            var start = ctx.selfCell;
            var cameFrom = new Dictionary<Vector3Int, Vector3Int>();
            var visited = new HashSet<Vector3Int> { start };
            var queue = new Queue<Vector3Int>();
            queue.Enqueue(start);

            Vector3Int? shelter = null;

            while (queue.Count > 0)
            {
                var current = queue.Dequeue();

                int dist = Mathf.Abs(current.x - start.x) +
                           Mathf.Abs(current.y - start.y);

                if (dist > maxSearchRadius) continue;

                // Célula segura encontrada
                if (!ctx.dangerCells.Contains(current) && current != start)
                {
                    shelter = current;
                    break;
                }

                foreach (var dir in new[]
                {
                    Vector3Int.right,
                    Vector3Int.left,
                    Vector3Int.up,
                    Vector3Int.down
                })
                {
                    var neighbor = current + dir;
                    if (visited.Contains(neighbor)) continue;
                    if (core.IsCellSolid(neighbor) || core.IsCellBreakable(neighbor)) continue;
                    visited.Add(neighbor);
                    cameFrom[neighbor] = current;
                    queue.Enqueue(neighbor);
                }
            }

            if (!shelter.HasValue)
                return decision;

            var path = ReconstructPath(cameFrom, start, shelter.Value);
            if (path == null || path.Count == 0)
                return decision;

            decision.isValid = true;
            decision.priority = priority;
            decision.path = path;
            decision.wantsBombNow = false;

            return decision;
        }

        private List<Vector3Int> ReconstructPath(
            Dictionary<Vector3Int, Vector3Int> cameFrom,
            Vector3Int start,
            Vector3Int goal)
        {
            var path = new List<Vector3Int> { goal };
            var current = goal;

            while (!current.Equals(start))
            {
                if (!cameFrom.TryGetValue(current, out var prev))
                    return null;

                current = prev;
                path.Add(current);
            }

            path.Reverse();
            return path;
        }
    }
}