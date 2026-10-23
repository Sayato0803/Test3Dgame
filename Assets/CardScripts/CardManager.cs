using System.Collections.Generic;
using UnityEngine;

namespace CardScripts
{
    /// <summary>
    /// Quản lý tổng thể hệ thống thẻ bài trong Scene:
    /// - Quản lý danh sách các lá bài trên tay
    /// - Khi người chơi bấm vào từng lá bài:
    ///   + Lá Knight -> Triệu hồi Knight
    ///   + Lá Mage   -> Triệu hồi Mage
    ///   + Lá King   -> Triệu hồi King
    ///   + Lá Mirror -> Triệu hồi Entity Mirror
    /// - Tính toán quỹ đạo 3D phóng từ vị trí lá bài trên màn hình vào sàn đấu
    /// </summary>
    public class CardManager : MonoBehaviour
    {
        public static CardManager Instance { get; private set; }

        [Header("Spawn Settings")]
        [SerializeField] private Camera mainCamera;
        [SerializeField] private Transform landingCenter;

        [Header("Default Fallback Prefab")]
        [SerializeField] private GameObject cubePrefab;

        [Header("Cards in Hand")]
        [SerializeField] private List<CardView> activeCards = new List<CardView>();

        private void Awake()
        {
            if (Instance == null)
            {
                Instance = this;
            }
            else
            {
                Destroy(gameObject);
                return;
            }

            if (mainCamera == null)
            {
                mainCamera = Camera.main;
            }
        }

        /// <summary>
        /// Được gọi khi một lá bài được người chơi bấm vào.
        /// Triệu hồi thực thể tương ứng (Knight, Mage, King, Mirror).
        /// </summary>
        public void OnCardClicked(CardView card, GameObject customPrefab = null)
        {
            GameObject prefabToSpawn = customPrefab != null ? customPrefab : cubePrefab;
            if (prefabToSpawn == null)
            {
                Debug.LogWarning("[CardManager] Chưa gán Prefab cho lá bài này!");
                return;
            }

            if (mainCamera == null)
            {
                mainCamera = Camera.main;
                if (mainCamera == null) return;
            }

            // 1. Lấy tọa độ màn hình của lá bài vừa bấm
            Vector3 cardScreenPos = card.transform.position;

            // 2. Chuyển đổi tọa độ màn hình thành vị trí 3D ở ngay phía trước Camera
            Vector3 spawnWorldPos = mainCamera.ScreenToWorldPoint(new Vector3(cardScreenPos.x, cardScreenPos.y, 2.2f));

            // 3. Khởi tạo thực thể tại vị trí vừa tính
            GameObject spawnedEntity = Instantiate(prefabToSpawn, spawnWorldPos, Quaternion.identity);

            // 4. Tính toán điểm đáp trên sàn đấu 3D:
            // Phân bổ vị trí đáp trải đều trên sàn đấu theo vị trí ngang của lá bài
            float screenRatioX = Mathf.Clamp01(cardScreenPos.x / (float)Screen.width);
            float targetX = Mathf.Lerp(-3.6f, 3.6f, screenRatioX);

            Vector3 targetLandingPos;
            if (landingCenter != null)
            {
                targetLandingPos = new Vector3(
                    targetX,
                    landingCenter.position.y + 0.5f,
                    landingCenter.position.z + Random.Range(-0.4f, 0.4f)
                );
            }
            else
            {
                targetLandingPos = new Vector3(
                    targetX,
                    0.5f,
                    2.0f + Random.Range(-0.4f, 0.4f)
                );
            }

            // Xoay mặt hướng về phía Camera/người chơi
            Quaternion targetRotation = Quaternion.Euler(0f, 180f, 0f);

            // 5. Gán hoặc kích hoạt CardEntityFlight để thực hiện bay Parabol mượt mà
            CardEntityFlight flight = spawnedEntity.GetComponent<CardEntityFlight>();
            if (flight == null)
            {
                flight = spawnedEntity.AddComponent<CardEntityFlight>();
            }
            flight.Launch(targetLandingPos, targetRotation);

            // Xóa lá bài khỏi danh sách đang hoạt động
            activeCards.Remove(card);
        }
    }
}
