using UnityEngine;

namespace IC.Core.Utility
{
    /// Serviço central de grid (1 tile = 1 unidade). Coloque no GameObject 'Grid'.
    [RequireComponent(typeof(Grid))]
    public class GridService2D : MonoBehaviour
    {
        [SerializeField] private Grid grid;

        public Vector3Int WorldToCell(Vector3 world) => grid.WorldToCell(world);

        public Vector3 CellCenterWorld(Vector3Int cell)
        {
            // Centro geométrico da célula
            var origin = grid.CellToWorld(cell);
            return origin + (Vector3)grid.cellSize * 0.5f;
        }

        /// Snap exato qualquer transform ao centro da célula correspondente
        public void SnapTransformCenter(Transform t)
        {
            var c = WorldToCell(t.position);
            t.position = CellCenterWorld(c);
        }

        private void Reset()
        {
            if (!grid) grid = GetComponent<Grid>();
        }
    }
}
