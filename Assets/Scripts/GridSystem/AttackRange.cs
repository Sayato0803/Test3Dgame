using System.Collections.Generic;
using UnityEngine;

namespace GridBattle
{
    /// <summary>
    /// Nguồn duy nhất để tính ô trong tầm đánh. Preview (GridPlacer) và Battle (Phases) cùng gọi,
    /// nên ô được tô khi kéo chính là ô bị tính damage.
    /// </summary>
    public static class AttackRange
    {
        /// <summary>Hướng mặt -> bước 1 ô. World x -> cột x, world z -> hàng y.</summary>
        public static Vector2Int FacingStep(Transform t)
        {
            Vector3 f = t.forward;
            return new Vector2Int(Mathf.RoundToInt(f.x), Mathf.RoundToInt(f.z));
        }

        /// <summary>'range' ô theo hướng facing, tính cả ô gốc (i = 0). Ô ngoài bàn bỏ qua, xét tiếp.</summary>
        public static List<Vector2Int> GetCells(GridSystem grid, Vector2Int origin, Transform facing, int range)
        {
            var cells = new List<Vector2Int>();
            Vector2Int step = FacingStep(facing);
            for (int i = 0; i < range; i++)
            {
                Vector2Int c = origin + step * i;
                if (grid.GetCell(c) != null) cells.Add(c);
            }
            return cells;
        }
    }
}
