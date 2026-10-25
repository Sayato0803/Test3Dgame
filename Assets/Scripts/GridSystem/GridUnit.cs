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

        /// <summary>Event counter lúc người chơi đặt / dời quân lần cuối. Battle xử lý đòn theo số này tăng dần.</summary>
        public int PlacedAt { get; set; }

        /// <summary>Số lượt tấn công của chủ quân còn bị choáng: không gây damage, không nhấc được.</summary>
        public int StunTurns { get; set; }

        /// <summary>DMG Boost: +damage mỗi đòn trúng trong lần resolve battle kế tiếp của chủ quân.</summary>
        public int DamageBoost { get; set; }

        /// <summary>Shield: King chặn trọn 1 đòn; attacker chặn 1 lần bị choáng / bị nổ.</summary>
        public bool Shield { get; set; }

        /// <summary>Suicide Bomb còn mấy turn (0 = không có).</summary>
        public int BombTurns { get; set; }
    }
}
