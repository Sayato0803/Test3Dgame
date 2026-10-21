using UnityEngine;

namespace GridBattle
{
    /// <summary>Gắn lên mỗi quân để lưới biết loại quân và quân đang đứng ở ô nào.</summary>
    public class GridUnit : MonoBehaviour
    {
        [SerializeField] private UnitKind _kind;

        public UnitKind Kind
        {
            get => _kind;
            set => _kind = value;
        }

        /// <summary>Lưới đang chứa quân này; null nếu quân chưa được đặt.</summary>
        public GridSystem Grid { get; internal set; }
        public Vector2Int Coord { get; internal set; }
        public bool IsOnGrid => Grid != null;

        /// <summary>Số ô tầm đánh (tính cả ô đang đứng). GridPlacer gán lúc spawn; King = 0.</summary>
        public int Range { get; set; }

        /// <summary>Player sở hữu quân; Phases gán khi quân được đặt lần đầu.</summary>
        public Player Owner { get; internal set; }
    }
}
