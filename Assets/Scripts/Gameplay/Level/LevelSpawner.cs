using System.Collections.Generic;
using UnityEngine;
using IC.Core.Utility;
using IC.Inputs;

namespace IC.Gameplay
{
    [System.Serializable]
    public struct SpawnLayerMapping
    {
        public SpawnLayer layer;
        public GameObject defaultPrefab;
    }

    /// <summary>
    /// Orquestrador de spawn da fase.
    /// Lê todos os SpawnAnchors da cena e instancia o prefab correto para cada um.
    /// 
    /// Quando manualSpawn estiver desmarcado (padrão), comportamento idêntico ao original:
    /// spawna todos os anchors no Start().
    /// 
    /// Quando manualSpawn estiver marcado, não spawna nada automaticamente.
    /// Sistemas externos chamam SpawnAll() ou SpawnLayer() quando necessário.
    /// O LevelSpawner é agnóstico a quem o consome.
    /// </summary>
    public class LevelSpawner : MonoBehaviour
    {
        [Header("Mapeamento Layer → Prefab padrão")]
        [SerializeField] private List<SpawnLayerMapping> layerMappings = new();

        [Header("Refs")]
        [SerializeField] private GridService2D gridService;

        [Header("Comportamento")]
        [Tooltip("Quando marcado, desativa o spawn automático no Start. " +
                 "Sistemas externos controlam quando spawnar via SpawnAll() ou SpawnLayer().")]
        [SerializeField] private bool manualSpawn = false;

        private readonly Dictionary<SpawnLayer, GameObject> _map = new();

        private void Awake()
        {
            foreach (var mapping in layerMappings)
                _map[mapping.layer] = mapping.defaultPrefab;
        }

        private void Start()
        {
            if (!manualSpawn)
                SpawnAll();
        }

        // =========================================================
        //  API pública — agnóstica a quem consome
        // =========================================================

        /// <summary>
        /// Spawna todos os anchors da cena.
        /// </summary>
        public void SpawnAll()
        {
            var anchors = FindObjectsOfType<SpawnAnchor>();
            foreach (var anchor in anchors)
                Spawn(anchor);
        }

        /// <summary>
        /// Spawna apenas os anchors de uma layer específica.
        /// </summary>
        public void SpawnLayer(SpawnLayer layer)
        {
            var anchors = FindObjectsOfType<SpawnAnchor>();
            foreach (var anchor in anchors)
                if (anchor.SpawnLayer == layer)
                    Spawn(anchor);
        }

        // =========================================================
        //  Internos — lógica de spawn original intocada
        // =========================================================

        private void Spawn(SpawnAnchor anchor)
        {
            var prefab = anchor.PrefabOverride;

            if (prefab == null && !_map.TryGetValue(anchor.SpawnLayer, out prefab))
            {
                Debug.LogWarning($"[LevelSpawner] Nenhum prefab mapeado para {anchor.SpawnLayer} " +
                                 $"e sem override em '{anchor.gameObject.name}'. Pulando.");
                return;
            }

            var pos = SnapToGrid(anchor.transform.position);
            var go = Instantiate(prefab, pos, Quaternion.identity);
        }

        private Vector3 SnapToGrid(Vector3 worldPos)
        {
            if (gridService == null) return worldPos;
            var cell = gridService.WorldToCell(worldPos);
            return gridService.CellCenterWorld(cell);
        }
    }
}