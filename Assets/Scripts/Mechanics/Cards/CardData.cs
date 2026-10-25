using UnityEngine;

namespace GridBattle
{
    public enum CardType { DmgBoost, Reflect, Shield, Chest, Rift, Tornado, Healing, SuicideBomb, Clone }

    /// <summary>Người chơi phải chọn gì trên bàn khi dùng lá.</summary>
    public enum CardTarget
    {
        Self,          // không chọn gì (Chest)
        OwnKing,       // 1 King của mình
        OwnAttacker,   // 1 attacker của mình trên bàn đối thủ
        OwnUnit,       // 1 quân của mình: King trên bàn mình hoặc attacker trên bàn đối thủ
        Area2x2,       // góc vùng 2x2 trên bàn mình
        TwoCells,      // 2 ô khác nhau trên bàn đối thủ (cổng A rồi B)
        KingThenCell   // 1 King của mình, rồi 1 ô không có King
    }

    /// <summary>
    /// 1 loại lá bài. ScriptableObject để chỉnh tên/mô tả/art trong Inspector mà không sửa code;
    /// hiệu ứng nằm trong Phases (theo Type) để luật tách khỏi dữ liệu hiển thị.
    /// </summary>
    [CreateAssetMenu(menuName = "GridBattle/Card", fileName = "Card")]
    public class CardData : ScriptableObject
    {
        public CardType Type;
        public string DisplayName;
        [TextArea] public string Description;
        [Tooltip("Trống = lá tạm: nền màu Tint + tên lá.")]
        public Sprite Art;
        public Color Tint = new(0.35f, 0.3f, 0.5f);

        /// <summary>Loại mục tiêu suy ra từ Type (không cho chỉnh tay để hiệu ứng luôn nhận đúng mục tiêu).</summary>
        public CardTarget Target => Type switch
        {
            CardType.Chest => CardTarget.Self,
            CardType.DmgBoost => CardTarget.OwnAttacker,
            CardType.Tornado => CardTarget.Area2x2,
            CardType.Rift => CardTarget.TwoCells,
            CardType.Clone => CardTarget.KingThenCell,
            CardType.Shield or CardType.Healing or CardType.SuicideBomb => CardTarget.OwnUnit,
            _ => CardTarget.OwnKing
        };

        /// <summary>Số lần click ô để dùng lá.</summary>
        public int PickCount => Target switch
        {
            CardTarget.Self => 0,
            CardTarget.TwoCells or CardTarget.KingThenCell => 2,
            _ => 1
        };
    }
}
