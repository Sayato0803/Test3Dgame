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

        /// <summary>Bị đánh trúng -> bên tấn công thấy. Chủ quân đổi chỗ -> ẩn lại.</summary>
        public bool IsRevealed;

        public bool IsDead => Health <= 0;

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
