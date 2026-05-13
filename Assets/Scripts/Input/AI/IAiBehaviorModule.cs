using System.Collections;
using System.Collections.Generic;
using UnityEngine;

namespace IC.Input.AI
{
    /// <summary>
    /// Interface para módulos de comportamento plugáveis.
    /// Cada módulo avalia o contexto e, se quiser assumir o controle,
    /// retorna um BehaviorDecision com path e prioridade.
    /// </summary>
    public interface IAiBehaviorModule
    {
        string BehaviorId { get; }
        bool Enabled { get; }

        BehaviorDecision Evaluate(AiCorePathfinder core, AiContext ctx);
    }
}
