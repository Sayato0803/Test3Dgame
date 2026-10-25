using UnityEngine;

namespace GridBattle
{
    /// <summary>
    /// Thống kê của 1 King: máu, đã lộ chưa. Phases tự gắn khi King được đặt lần đầu.
    /// Ẩn bằng cách tắt Renderer (cả object con của model FBX), GameObject vẫn hoạt động để tính damage.
    /// </summary>
    public class KingStats : MonoBehaviour
    {
        public int MaxHealth = 10;
        public int Health = 10;

        /// <summary>Bị đánh trúng -> lộ trong lượt đó. Hết lượt (hoặc chủ quân nhấc King lên) -> ẩn lại, dù King đứng yên.</summary>
        public bool IsRevealed;

        /// <summary>Ô King đứng lúc bị lộ.</summary>
        public Vector2Int RevealedAt;

        public void Reveal(Vector2Int at)
        {
            IsRevealed = true;
            RevealedAt = at;
        }

        public bool IsDead => Health <= 0;

        /// <summary>Reflect: phản đòn đầu tiên. Shield / Bomb nằm trên GridUnit vì attacker cũng mang được.</summary>
        public bool Reflect;
        /// <summary>King giả (Clone): nhận damage như King nhưng không tính vào điều kiện thua.</summary>
        public bool IsDecoy;
        public int DecoyTurns;

        private Renderer[] _renderers;

        private void Awake()
        {
            Health = MaxHealth;
            _renderers = GetComponentsInChildren<Renderer>(true);
        }

        public void TakeDamage(int amount)
        {
            Health = Mathf.Max(0, Health - amount);
        }

        public void SetVisible(bool visible)
        {
            foreach (Renderer r in _renderers)
                if (r != null) r.enabled = visible;
        }
    }
}
