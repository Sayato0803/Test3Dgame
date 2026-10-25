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

        /// <summary>'range' ô theo hướng facing, tính cả ô gốc. Ô ngoài bàn bỏ qua, xét tiếp. portals: xem Walk.</summary>
        public static List<Vector2Int> GetCells(GridSystem grid, Vector2Int origin, Transform facing, int range,
            IReadOnlyDictionary<Vector2Int, Vector2Int> portals = null) =>
            Walk(origin, FacingStep(facing), range, c => grid.GetCell(c) != null, portals);

        /// <summary>
        /// Đường đánh thuần (không cần GridSystem, để test). Rift: ô cổng không tốn tầm và không bị đánh, đòn ra ở cổng kia
        /// (cổng 2 chiều, giữ hướng, đi tiếp phần tầm còn lại). Mỗi cổng chỉ qua 1 lần mỗi đòn để đường đánh không lặp vô hạn.
        /// </summary>
        public static List<Vector2Int> Walk(Vector2Int origin, Vector2Int step, int range, System.Func<Vector2Int, bool> inBoard,
            IReadOnlyDictionary<Vector2Int, Vector2Int> portals = null)
        {
            var cells = new List<Vector2Int>();
            HashSet<Vector2Int> used = null;
            Vector2Int c = origin;
            int n = 0;
            while (n < range)
            {
                if (portals != null && portals.TryGetValue(c, out Vector2Int exit) && (used ??= new HashSet<Vector2Int>()).Add(c))
                {
                    used.Add(exit); // ra ở cổng kia thì không bị hút ngược lại
                    c = exit;
                    continue;
                }
                if (inBoard(c)) cells.Add(c);
                n++;
                c += step;
            }
            return cells;
        }
    }
}
