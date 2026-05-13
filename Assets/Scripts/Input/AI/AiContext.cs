using System.Collections.Generic;
using UnityEngine;
using IC.Core.Utility;

namespace IC.Input.AI
{
    /// <summary>
    /// Snapshot de informações que o núcleo oferece aos módulos de comportamento.
    /// </summary>
    public struct AiContext
    {
        public Vector3Int selfCell;
        public Vector3Int playerCell;
        public IReadOnlyList<Vector3Int> pathToPlayer;
        public int breakablesOnPathToPlayer;
        public IReadOnlyCollection<Vector3Int> dangerCells;
        public GridService2D grid;
        public List<Vector3Int> activePath;     
        public int activePathIndex;             
    }

    /// <summary>
    /// Resultado de avaliação de um módulo de comportamento.
    /// </summary>
    public struct BehaviorDecision
    {
        public bool isValid;
        public int priority;
        public List<Vector3Int> path;
        public bool wantsBombNow;

        public static BehaviorDecision Invalid =>
            new BehaviorDecision
            {
                isValid = false,
                priority = int.MinValue,
                path = null,
                wantsBombNow = false
            };
    }
}