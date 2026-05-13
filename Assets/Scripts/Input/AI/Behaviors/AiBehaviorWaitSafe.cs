using System.Collections.Generic;
using System.Linq;
using UnityEngine;
using IC.Input.AI;

namespace IC.Input.AI.Behaviors
{
    /// <summary>
    /// Comportamento de espera segura:
    /// - Ativa quando a célula atual está SEGURA mas:
    ///   a) Não há nenhuma saída segura disponível (encurralado por sólidos/danger), OU
    ///   b) O caminho de menor resistência até o player passa por uma danger cell.
    /// - Fica parado até o perigo se dissipar.
    /// - Prioridade entre FindShelter (100) e ChasePlayer (10).
    /// - Nunca planta bomba.
    /// </summary>
    public class AiBehaviorWaitSafe : MonoBehaviour, IAiBehaviorModule
    {
        [Header("General")]
        [SerializeField] private bool enabledModule = true;

        [Tooltip("Deve ser menor que FindShelter (100) e maior que ChasePlayer (10).")]
        [SerializeField] private int priority = 50;

        public string BehaviorId => "WaitSafe";
        public bool Enabled => enabledModule && isActiveAndEnabled;

        private static readonly Vector3Int[] DIRS =
        {
            Vector3Int.right,
            Vector3Int.left,
            Vector3Int.up,
            Vector3Int.down
        };

        public BehaviorDecision Evaluate(AiCorePathfinder core, AiContext ctx)
        {
            var decision = BehaviorDecision.Invalid;

            // Já está em perigo — FindShelter cuida disso
            if (ctx.dangerCells != null && ctx.dangerCells.Contains(ctx.selfCell))
                return decision;

            // Verifica se há alguma saída segura nos vizinhos imediatos
            bool hasEscape = false;
            foreach (var dir in DIRS)
            {
                var neighbor = ctx.selfCell + dir;

                if (core.IsCellSolid(neighbor)) continue;
                if (core.IsCellBreakable(neighbor)) continue;
                if (ctx.dangerCells != null && ctx.dangerCells.Contains(neighbor)) continue;

                hasEscape = true;
                break;
            }

            // Verifica se o path até o player passa por alguma danger cell
            bool pathCrossedDanger = false;
            if (ctx.pathToPlayer != null)
            {
                foreach (var cell in ctx.pathToPlayer)
                {
                    if (ctx.dangerCells != null && ctx.dangerCells.Contains(cell))
                    {
                        pathCrossedDanger = true;
                        break;
                    }
                }
            }

            // Encurralado OU caminho comprometido — fica parado
            if (!hasEscape || pathCrossedDanger)
            {
                decision.isValid = true;
                decision.priority = priority;
                decision.path = new List<Vector3Int> { ctx.selfCell };
                decision.wantsBombNow = false;
                return decision;
            }

            // Há saída segura e caminho limpo — deixa outros comportamentos assumir
            return decision;
        }
    }
}