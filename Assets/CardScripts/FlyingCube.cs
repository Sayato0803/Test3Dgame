using UnityEngine;
using DG.Tweening;

namespace CardScripts
{
    /// <summary>
    /// Điều khiển hiệu ứng của khối Cube khi được tạo ra từ lá bài:
    /// - Xuất phát từ vị trí lá bài trên màn hình
    /// - Bay vút vào không gian 3D với quỹ đạo vòng cung và hiệu ứng xoay
    /// - Khi chạm đất, kích hoạt Rigidbody để vật thể tương tác vật lý tự nhiên
    /// </summary>
    public class FlyingCube : MonoBehaviour
    {
        [Header("Flight Settings")]
        [SerializeField] private float flightDuration = 0.85f;
        [SerializeField] private float jumpPower = 2.5f;

        private Rigidbody rb;
        private Collider col;

        private void Awake()
        {
            rb = GetComponent<Rigidbody>();
            col = GetComponent<Collider>();

            if (rb != null)
            {
                rb.isKinematic = true; // Tắt vật lý trong lúc bay
            }
        }

        /// <summary>
        /// Khởi động hành trình bay từ vị trí lá bài vào tọa độ 3D đích
        /// </summary>
        public void Launch(Vector3 targetWorldPosition)
        {
            // Hiệu ứng phóng to nhẹ khi vừa xuất hiện
            transform.localScale = Vector3.zero;
            transform.DOScale(Vector3.one, 0.2f).SetEase(Ease.OutBack);

            // Bay vòng cung vào không gian 3D (DOJump)
            transform.DOJump(targetWorldPosition, jumpPower, 1, flightDuration)
                .SetEase(Ease.OutQuad)
                .OnComplete(OnLanded);

            // Xoay tròn tự do trong khi bay tạo cảm giác động lực học
            transform.DORotate(new Vector3(360f, 540f, 180f), flightDuration, RotateMode.FastBeyond360)
                .SetEase(Ease.OutQuad);
        }

        private void OnLanded()
        {
            // Khi tiếp đất: Kích hoạt Rigidbody để nảy và va chạm vật lý
            if (rb != null)
            {
                rb.isKinematic = false;
                rb.AddForce(new Vector3(Random.Range(-0.5f, 0.5f), 1f, Random.Range(0.5f, 1.5f)), ForceMode.Impulse);
            }

            // Hiệu ứng nảy/ép nhẹ của vật thể khi chạm đất (Squash & Stretch)
            transform.DOPunchScale(new Vector3(0.3f, -0.2f, 0.3f), 0.3f, 8, 1);
        }
    }
}
