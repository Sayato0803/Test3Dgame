using UnityEngine;

namespace GridBattle
{
    public enum UnitKind { King, Knight, Mage }

    /// <summary>
    /// Dữ liệu của 1 ô trong mảng Cell[,]. Là class thường (không phải MonoBehaviour)
    /// vì ô chỉ là dữ liệu, phần hiển thị do quad trong GridSystem lo.
    /// King và Attacker tách 2 slot: Mage/Knight được đứng lên ô có King ẩn.
    /// </summary>
    public class Cell
    {
        public Vector2Int Coord { get; }
        public GridUnit King { get; set; }
        public GridUnit Attacker { get; set; }

        public Cell(Vector2Int coord)
        {
            Coord = coord;
        }
    }
}
