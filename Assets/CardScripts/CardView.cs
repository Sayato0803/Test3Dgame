using UnityEngine;
using UnityEngine.UI;
using UnityEngine.EventSystems;
using DG.Tweening;

namespace CardScripts
{
    /// <summary>
    /// Quản lý giao diện và tương tác của lá bài phong cách Balatro cao cấp:
    /// - Kích thước lớn, hiển thị hình ảnh minh họa (Card Sprite) sắc nét
    /// - Khi rê chuột qua (Hover):
    ///   + Nhấc lá bài lên cao và phóng to mượt mà
    ///   + Nghiêng nhẹ 3D (Tilt) tạo chiều sâu sống động
    ///   + Quét vệt sáng lấp lánh (Shine Shimmer Sweep) ngang qua mặt bài như thẻ bài Hologram
    ///   + Phun các hạt sao lấp lánh (Sparkle Stars) rơi nhẹ xung quanh
    /// - Khi bấm vào (Click):
    ///   + Rung nhẹ rồi thụt thẳng xuống dưới biến mất (tiêu hao lá bài)
    ///   + Báo cho CardManager sinh khối 3D bay ra màn hình
    /// </summary>
    [RequireComponent(typeof(CanvasGroup))]
    public class CardView : MonoBehaviour, IPointerEnterHandler, IPointerExitHandler, IPointerClickHandler
    {
        [Header("Hover Animation Settings")]
        [SerializeField] private float hoverLiftHeight = 65f;
        [SerializeField] private float hoverScale = 1.14f;
        [SerializeField] private float hoverDuration = 0.22f;
        [SerializeField] private float tiltAngle = 3.5f;

        [Header("Visual References")]
        [SerializeField] private Image cardArtImage;
        [SerializeField] private Image shineImage;
        [SerializeField] private ParticleSystem sparkleParticle;

        [Header("Spawn Entity Reference")]
        [Tooltip("Prefab của nhân vật/vật thể tương ứng sẽ được sinh ra khi bấm vào lá bài này")]
        [SerializeField] private GameObject spawnPrefab;

        private RectTransform rectTransform;
        private CanvasGroup canvasGroup;
        private Vector2 restAnchoredPosition;
        private Vector3 restScale;
        private Quaternion restRotation;
        private bool isConsumed = false;
        private Sequence shineSequence;

        private void Awake()
        {
            rectTransform = GetComponent<RectTransform>();
            canvasGroup = GetComponent<CanvasGroup>();
            restAnchoredPosition = rectTransform.anchoredPosition;
            restScale = rectTransform.localScale;
            restRotation = rectTransform.localRotation;

            if (sparkleParticle != null)
            {
                sparkleParticle.Stop(true, ParticleSystemStopBehavior.StopEmittingAndClear);
            }

            if (shineImage != null)
            {
                shineImage.gameObject.SetActive(false);
            }
        }

        public void SetRestPosition(Vector2 pos)
        {
            restAnchoredPosition = pos;
            if (rectTransform != null)
            {
                rectTransform.anchoredPosition = pos;
            }
        }

        public void SetCardSprite(Sprite sprite)
        {
            if (cardArtImage != null)
            {
                cardArtImage.sprite = sprite;
            }
        }

        public void SetSpawnPrefab(GameObject prefab)
        {
            spawnPrefab = prefab;
        }

        public GameObject SpawnPrefab => spawnPrefab;

        public void OnPointerEnter(PointerEventData eventData)
        {
            if (isConsumed) return;

            rectTransform.DOKill();

            // 1. Nhấc lá bài lên cao và phóng to
            rectTransform.DOAnchorPosY(restAnchoredPosition.y + hoverLiftHeight, hoverDuration)
                .SetEase(Ease.OutBack);
            rectTransform.DOScale(restScale * hoverScale, hoverDuration)
                .SetEase(Ease.OutBack);

            // 2. Nghiêng nhẹ lá bài ngẫu nhiên tạo độ sống động
            float tilt = (restAnchoredPosition.x < 0) ? tiltAngle : -tiltAngle;
            rectTransform.DOLocalRotate(new Vector3(0, 0, tilt), hoverDuration)
                .SetEase(Ease.OutQuad);

            // 3. Hiệu ứng quét vệt sáng lấp lánh (Shine Shimmer Sweep)
            TriggerShineEffect();

            // 4. Kích hoạt hạt sao lấp lánh rơi nhẹ
            if (sparkleParticle != null)
            {
                sparkleParticle.Play();
            }
        }

        public void OnPointerExit(PointerEventData eventData)
        {
            if (isConsumed) return;

            rectTransform.DOKill();

            // Trở về vị trí, kích thước và góc quay ban đầu
            rectTransform.DOAnchorPosY(restAnchoredPosition.y, hoverDuration)
                .SetEase(Ease.OutQuad);
            rectTransform.DOScale(restScale, hoverDuration)
                .SetEase(Ease.OutQuad);
            rectTransform.DOLocalRotate(restRotation.eulerAngles, hoverDuration)
                .SetEase(Ease.OutQuad);

            // Dừng vệt sáng và hạt lấp lánh
            if (shineSequence != null) shineSequence.Kill();
            if (shineImage != null) shineImage.gameObject.SetActive(false);
            if (sparkleParticle != null)
            {
                sparkleParticle.Stop(true, ParticleSystemStopBehavior.StopEmitting);
            }
        }

        public void OnPointerClick(PointerEventData eventData)
        {
            if (isConsumed) return;
            isConsumed = true;

            // Khóa tương tác
            if (canvasGroup != null) canvasGroup.blocksRaycasts = false;
            if (shineSequence != null) shineSequence.Kill();
            if (shineImage != null) shineImage.gameObject.SetActive(false);
            if (sparkleParticle != null) sparkleParticle.Stop(true, ParticleSystemStopBehavior.StopEmitting);

            rectTransform.DOKill();

            // Gửi tín hiệu tạo thực thể 3D tương ứng
            if (CardManager.Instance != null)
            {
                CardManager.Instance.OnCardClicked(this, spawnPrefab);
            }

            // Hiệu ứng tiêu hao lá bài: Nảy bung lên một nhịp rồi thụt nhanh xuống đáy màn hình và mờ dần
            Sequence consumeSequence = DOTween.Sequence();
            consumeSequence.Append(rectTransform.DOPunchScale(Vector3.one * 0.15f, 0.12f, 12, 1));
            consumeSequence.Append(rectTransform.DOAnchorPosY(restAnchoredPosition.y - 280f, 0.38f).SetEase(Ease.InBack));
            consumeSequence.Join(rectTransform.DOLocalRotate(new Vector3(0, 0, Random.Range(-15f, 15f)), 0.38f));
            consumeSequence.Join(canvasGroup.DOFade(0f, 0.38f));
            consumeSequence.OnComplete(() =>
            {
                gameObject.SetActive(false);
            });
        }

        private void TriggerShineEffect()
        {
            if (shineImage == null) return;

            RectTransform shineRt = shineImage.rectTransform;
            shineImage.gameObject.SetActive(true);

            if (shineSequence != null) shineSequence.Kill();

            shineSequence = DOTween.Sequence();
            // Đặt vệt sáng ở bên trái lá bài
            shineRt.anchoredPosition = new Vector2(-280f, 0f);
            shineImage.color = new Color(1f, 1f, 1f, 0.6f);

            // Quét chéo nhanh sang bên phải
            shineSequence.Append(shineRt.DOAnchorPosX(280f, 0.55f).SetEase(Ease.OutSine));
            shineSequence.Join(shineImage.DOFade(0.1f, 0.55f));
            shineSequence.OnComplete(() =>
            {
                if (shineImage != null) shineImage.gameObject.SetActive(false);
            });
        }
    }
}
