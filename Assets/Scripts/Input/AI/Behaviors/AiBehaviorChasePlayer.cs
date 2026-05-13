using System.Collections.Generic;
using System.Linq;
using UnityEngine;
using IC.Input.AI;

namespace IC.Input.AI.Behaviors
{
    /// <summary>
    /// Comportamento "Chaser":
    /// - Ativo se existe rota até o player com 0..maxBreakablesAllowed breakables.
    /// - Se não há breakables: vai até uma célula adjacente ao player e planta bomba.
    /// - Se há breakables (<= limite): entra em "ClearPath", para na célula antes do
    ///   primeiro breakable e planta bomba até abrir caminho.
    /// </summary>
    public class AiBehaviorChasePlayer : MonoBehaviour, IAiBehaviorModule
    {
        [Header("General")]
        [SerializeField] private bool enabledModule = true;
        [SerializeField] private int priority = 10;

        [Header("Chase Settings")]
        [Tooltip("Máximo de tiles quebráveis tolerados no path até o player.")]
        [SerializeField] private int maxBreakablesAllowed = 3;

        [Tooltip("Se true, o Chaser não planta bomba se a célula atual já estiver em perigo.")]
        [SerializeField] private bool avoidPlantingInDanger = true;

        public string BehaviorId => "ChasePlayer";
        public bool Enabled => enabledModule && isActiveAndEnabled;

        public BehaviorDecision Evaluate(AiCorePathfinder core, AiContext ctx)
        {
            var decision = BehaviorDecision.Invalid;

            var pathReadOnly = ctx.pathToPlayer;
            if (pathReadOnly == null || pathReadOnly.Count < 2)
                return decision;

            if (ctx.breakablesOnPathToPlayer < 0 || ctx.breakablesOnPathToPlayer > maxBreakablesAllowed)
                return decision;

            var path = new List<Vector3Int>(pathReadOnly);

            int manhattan = Mathf.Abs(ctx.selfCell.x - ctx.playerCell.x) +
                            Mathf.Abs(ctx.selfCell.y - ctx.playerCell.y);
            bool isAdjacentToPlayer = manhattan == 1;

            // -----------------------------------------------------
            // Caso 1: caminho livre (nenhum breakable)
            // -----------------------------------------------------
            if (ctx.breakablesOnPathToPlayer == 0)
            {
                if (isAdjacentToPlayer)
                {
                    if (avoidPlantingInDanger && IsCellInDanger(ctx, ctx.selfCell))
                        return decision;

                    decision.isValid = true;
                    decision.priority = priority;
                    decision.path = new List<Vector3Int> { ctx.selfCell };
                    decision.wantsBombNow = true;
                    return decision;
                }

                if (path.Count < 2)
                    return decision;

                int lastIndexBeforePlayer = path.Count - 2;
                if (lastIndexBeforePlayer < 0)
                    return decision;

                var pathToAdj = new List<Vector3Int>();
                for (int i = 0; i <= lastIndexBeforePlayer; i++)
                    pathToAdj.Add(path[i]);

                decision.isValid = true;
                decision.priority = priority;
                decision.path = pathToAdj;
                decision.wantsBombNow = false;
                return decision;
            }

            // -----------------------------------------------------
            // Caso 2: ClearPath
            // -----------------------------------------------------
            int firstBreakIdx = -1;
            for (int i = 1; i < path.Count; i++)
            {
                if (core.IsCellBreakable(path[i]))
                {
                    firstBreakIdx = i;
                    break;
                }
            }

            if (firstBreakIdx == -1)
                return decision;

            int bombCellIndex = Mathf.Max(0, firstBreakIdx - 1);
            var bombCell = path[bombCellIndex];

            if (ctx.selfCell == bombCell)
            {
                if (avoidPlantingInDanger && IsCellInDanger(ctx, ctx.selfCell))
                    return decision;

                decision.isValid = true;
                decision.priority = priority;
                decision.path = new List<Vector3Int> { ctx.selfCell };
                decision.wantsBombNow = true;
                return decision;
            }

            var pathToBombSpot = new List<Vector3Int>();
            for (int i = 0; i <= bombCellIndex; i++)
                pathToBombSpot.Add(path[i]);

            decision.isValid = true;
            decision.priority = priority;
            decision.path = pathToBombSpot;
            decision.wantsBombNow = false;
            return decision;
        }

        private bool IsCellInDanger(AiContext ctx, Vector3Int cell)
            => ctx.dangerCells != null && ctx.dangerCells.Contains(cell);
    }
}