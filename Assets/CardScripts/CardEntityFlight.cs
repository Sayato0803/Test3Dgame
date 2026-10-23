using UnityEngine;
using DG.Tweening;

namespace CardScripts
{
    /// <summary>
    /// Điều khiển hiệu ứng quỹ đạo bay và xuất hiện của mọi vật thể/nhân vật (Knight, Mage, King, Mirror)
    /// khi được triệu hồi từ lá bài:
    /// - Bắt đầu từ vị trí lá bài trên màn hình với tỉ lệ thu nhỏ (Scale = 0)
    /// - Bay vút theo quỹ đạo vòng cung Parabol (DOJump) vào vị trí chỉ định trên sàn đấu 3D
    /// - Phóng to mượt mà về kích thước gốc và tự động xoay hướng về phía người chơi/Camera
    /// - Khi tiếp đất: Thực hiện hiệu ứng nảy đàn hồi (Squash & Stretch) tạo cảm giác trọng lượng sống động
    /// - Kích hoạt Animator (nếu có) để nhân vật thực hiện hành động chào sân
    /// </summary>
    public class CardEntityFlight : MonoBehaviour
    {
        [Header("Flight Animation Settings")]
        [SerializeField] private float flightDuration = 0.75f;
        [SerializeField] private float jumpPower = 2.2f;

        private Vector3 originalScale;

        private void Awake()
        {
            originalScale = transform.localScale;
        }

        /// <summary>
        /// Kích hoạt phóng thực thể bay từ lá bài vào sàn đấu 3D
        /// </summary>
        public void Launch(Vector3 targetWorldPosition, Quaternion targetRotation)
        {
            if (originalScale == Vector3.zero)
            {
                originalScale = Vector3.one;
            }

            // 1. Xuất hiện thu nhỏ và phóng to dần khi bay
            transform.localScale = Vector3.zero;
            transform.DOScale(originalScale, 0.28f).SetEase(Ease.OutBack);

            // 2. Bay vòng cung Parabol vào sàn đấu
            transform.DOJump(targetWorldPosition, jumpPower, 1, flightDuration)
                .SetEase(Ease.OutQuad)
                .OnComplete(OnLanded);

            // 3. Xoay người mượt mà hướng về phía Camera/người chơi
            transform.DORotate(targetRotation.eulerAngles, flightDuration, RotateMode.Fast)
                .SetEase(Ease.OutQuad);
        }

        private void OnLanded()
        {
            // Hiệu ứng nảy đàn hồi khi tiếp đất (Squash & Stretch)
            transform.DOPunchScale(new Vector3(0.18f, -0.15f, 0.18f) * originalScale.y, 0.35f, 7, 1);

            // Nếu thực thể có Animator, kích hoạt trạng thái hoạt ảnh
            Animator anim = GetComponentInChildren<Animator>();
            if (anim != null)
            {
                anim.enabled = true;
            }
        }
    }
}
