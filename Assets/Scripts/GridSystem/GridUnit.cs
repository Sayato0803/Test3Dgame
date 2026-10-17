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
    }
}
