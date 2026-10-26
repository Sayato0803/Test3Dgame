using System;
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

        [Header("Visual References")]
        [SerializeField] private Image cardArtImage;
        [SerializeField] private Image shineImage;
        [SerializeField] private ParticleSystem sparkleParticle;

        [Header("Bốc bài")]
        [Tooltip("Animator clip bốc bài cũ (không chạy nữa, chỉ tắt đi để không ghi đè tween).")]
        [SerializeField] private Animator drawAnimator;
        [Tooltip("Sprite ngôi sao cho vầng sáng + sao bắn khi lá lật mặt. Trống = không có sao.")]
        [SerializeField] private Sprite burstSprite;

        [Header("Spawn Entity Reference")]
        [Tooltip("Prefab của nhân vật/vật thể tương ứng sẽ được sinh ra khi bấm vào lá bài này")]
        [SerializeField] private GameObject spawnPrefab;

        private RectTransform rectTransform;
        private CanvasGroup canvasGroup;
        private Vector2 restAnchoredPosition;
        private Vector3 restScale;
        private Quaternion restRotation;
        private bool isConsumed = false;
        private int hoverSiblingIndex = -1;
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

        public void SetRestPosition(Vector2 pos, float angle = 0f)
        {
            restAnchoredPosition = pos;
            restRotation = Quaternion.Euler(0f, 0f, angle);
            if (rectTransform != null)
            {
                rectTransform.anchoredPosition = pos;
                rectTransform.localRotation = restRotation;
            }
        }

        public void SetCardSprite(Sprite sprite)
        {
            if (cardArtImage != null)
            {
                cardArtImage.sprite = sprite;
            }
        }

        /// <summary>Lá chưa có art: nền màu 'tint' + tên lá in giữa thẻ.</summary>
        public void SetPlaceholder(string title, Color tint)
        {
            if (cardArtImage == null) return;
            cardArtImage.sprite = null;
            cardArtImage.color = tint;
            var label = new GameObject("Label", typeof(RectTransform)).AddComponent<Text>();
            label.transform.SetParent(cardArtImage.transform, false);
            label.rectTransform.anchorMin = Vector2.zero;
            label.rectTransform.anchorMax = Vector2.one;
            label.rectTransform.offsetMin = new Vector2(10f, 10f);
            label.rectTransform.offsetMax = new Vector2(-10f, -10f);
            label.font = Resources.Load<Font>("GUI/fonts/LilitaOne-Regular");
            label.text = title.ToUpper();
            label.alignment = TextAnchor.UpperLeft; // góc trên trái: phần còn thấy khi các lá trên tay xếp chồng
            label.resizeTextForBestFit = true;
            label.resizeTextMinSize = 10;
            label.resizeTextMaxSize = 26;
            label.color = Color.white;
            label.raycastTarget = false;
            label.gameObject.AddComponent<Outline>().effectDistance = new Vector2(2f, -2f);
        }

        public void SetSpawnPrefab(GameObject prefab)
        {
            spawnPrefab = prefab;
        }

        public GameObject SpawnPrefab => spawnPrefab;

        /// <summary>
        /// Có handler thì lá bài gọi handler thay vì CardManager (dùng trong trận đấu).
        /// Handler trả false = không được dùng lá này lúc này -> lá rung lắc, không bị tiêu hao.
        /// </summary>
        public Func<CardView, bool> ClickHandler { get; set; }

        /// <summary>Chuột phải vào lá (xem chi tiết). Không tiêu hao lá.</summary>
        public Action<CardView> RightClickHandler { get; set; }

        /// <summary>Dữ liệu tuỳ ý của người tạo lá bài (vd loại quân).</summary>
        public object Tag { get; set; }

        public bool IsConsumed => isConsumed;

        /// <summary>
        /// Bốc bài (tween trên con "Body", lá gốc đứng yên ở chỗ nghỉ trên tay):
        /// 1. lá úp bay vòng cung từ chồng bài góc dưới phải lên giữa màn hình, vừa bay vừa xoay 1 vòng và phóng to;
        /// 2. lật mặt: co ngang -> đổi mặt -> bung ra, loé trắng + vầng sáng sau lưng + sao bắn tung toé;
        /// 3. lơ lửng một nhịp cho người chơi kịp nhìn; 4. lấy đà rồi sà về chỗ trên tay, chạm tay thì nảy nhẹ.
        /// </summary>
        public void DealIn(float delay)
        {
            rectTransform.DOKill();
            rectTransform.anchoredPosition = restAnchoredPosition;
            rectTransform.localRotation = restRotation;
            if (drawAnimator != null) drawAnimator.enabled = false; // clip cũ không dùng nữa: tween bên dưới thay thế
            var body = transform.Find("Body") as RectTransform;
            if (body == null) return;
            GameObject back = body.Find("Back") != null ? body.Find("Back").gameObject : null;

            // Trong lúc bay: vẽ đè mọi lá khác, không nhận chuột (Body bay ngang màn hình).
            dealSequence?.Kill();
            if (dealCanvas == null)
            {
                dealCanvas = gameObject.AddComponent<Canvas>();
                dealCanvas.overrideSorting = true;
                dealCanvas.sortingOrder = 50;
            }
            canvasGroup.alpha = 0f;
            canvasGroup.blocksRaycasts = false;

            Vector2 deck = ScreenToLocal(new Vector2(Screen.width * 0.93f, Screen.height * 0.22f));
            Vector2 center = ScreenToLocal(new Vector2(Screen.width * 0.5f, Screen.height * 0.56f));
            float upright = -restRotation.eulerAngles.z; // bù góc xoè của lá gốc -> giữa màn hình lá đứng thẳng
            const float big = 1.7f;
            body.anchoredPosition = deck;
            body.localScale = Vector3.one * 0.45f;
            body.localEulerAngles = new Vector3(0f, 0f, upright - 40f);
            if (back != null) back.SetActive(true);

            Sequence s = dealSequence = DOTween.Sequence().SetDelay(delay).SetLink(gameObject);
            s.AppendCallback(() =>
            {
                canvasGroup.alpha = 1f;
                GridBattle.Sfx.Play(GridBattle.SfxId.CardDeal, 0.8f);
            });
            // 1. Bay lên giữa màn hình
            s.Append(body.DOJumpAnchorPos(center, 160f, 1, 0.45f).SetEase(Ease.OutQuad));
            s.Join(body.DOScale(big, 0.45f).SetEase(Ease.OutBack));
            s.Join(body.DOLocalRotate(new Vector3(0f, 0f, upright + 360f), 0.45f, RotateMode.FastBeyond360).SetEase(Ease.OutCubic));
            // 2. Lật mặt
            s.Append(body.DOScaleX(0f, 0.11f).SetEase(Ease.InQuad));
            s.AppendCallback(() =>
            {
                if (back != null) back.SetActive(false);
                GridBattle.Sfx.Play(GridBattle.SfxId.CardFlip, 0.9f);
                GridBattle.Sfx.Play(GridBattle.SfxId.Magic, 0.5f);
                FlashWhite(body);
                Burst(body.anchoredPosition);
                TriggerShineEffect();
            });
            s.Append(body.DOScaleX(big, 0.24f).SetEase(Ease.OutBack, 2.5f));
            // 3. Lơ lửng
            s.Append(body.DOAnchorPosY(center.y + 14f, 0.32f).SetEase(Ease.InOutSine));
            // 4. Sà về tay
            s.Append(body.DOAnchorPos(Vector2.zero, 0.34f).SetEase(Ease.InBack, 1.1f));
            s.Join(body.DOScale(1f, 0.34f).SetEase(Ease.InQuad));
            s.Join(body.DOLocalRotate(Vector3.zero, 0.34f).SetEase(Ease.InQuad));
            s.AppendCallback(() => GridBattle.Sfx.Play(GridBattle.SfxId.Pop, 0.6f));
            s.Append(rectTransform.DOPunchScale(restScale * 0.12f, 0.28f, 7, 0.8f));
            s.OnKill(() => EndDeal(body, back)); // cả khi xong lẫn khi bị huỷ giữa chừng
        }

        private Sequence dealSequence;
        private Canvas dealCanvas;

        private void EndDeal(RectTransform body, GameObject back)
        {
            if (this == null) return;
            body.anchoredPosition = Vector2.zero;
            body.localScale = Vector3.one;
            body.localRotation = Quaternion.identity;
            if (back != null) back.SetActive(false);
            if (dealCanvas != null) Destroy(dealCanvas);
            dealCanvas = null;
            canvasGroup.alpha = 1f;
            if (!isConsumed) canvasGroup.blocksRaycasts = true;
        }

        private Vector2 ScreenToLocal(Vector2 screen)
        {
            RectTransformUtility.ScreenPointToLocalPointInRectangle(rectTransform, screen, null, out Vector2 local);
            return local;
        }

        /// <summary>Lớp trắng phủ mặt bài (trong Mask của Body) rồi tắt dần.</summary>
        private static void FlashWhite(RectTransform body)
        {
            var flash = new GameObject("Flash", typeof(RectTransform)).AddComponent<Image>();
            flash.transform.SetParent(body, false);
            flash.rectTransform.anchorMin = Vector2.zero;
            flash.rectTransform.anchorMax = Vector2.one;
            flash.rectTransform.sizeDelta = Vector2.zero;
            flash.raycastTarget = false;
            flash.color = new Color(1f, 1f, 1f, 0.9f);
            flash.DOFade(0f, 0.4f).SetEase(Ease.OutQuad).SetLink(flash.gameObject).OnComplete(() => Destroy(flash.gameObject));
        }

        /// <summary>Vầng sáng xoay sau lưng lá + sao bắn tung toé quanh 'at' (toạ độ trong lá gốc, ngoài Mask của Body).</summary>
        private void Burst(Vector2 at)
        {
            if (burstSprite == null) return;
            Image Spawn(string name, float size, Color color, bool behind)
            {
                var img = new GameObject(name, typeof(RectTransform)).AddComponent<Image>();
                img.transform.SetParent(transform, false);
                if (behind) img.transform.SetAsFirstSibling();
                img.sprite = burstSprite;
                img.raycastTarget = false;
                img.color = color;
                img.rectTransform.sizeDelta = Vector2.one * size;
                img.rectTransform.anchoredPosition = at;
                return img;
            }

            Image glow = Spawn("Glow", 260f, new Color(1f, 0.9f, 0.5f, 0.85f), true);
            glow.transform.localScale = Vector3.zero;
            DOTween.Sequence().SetLink(glow.gameObject)
                .Append(glow.transform.DOScale(2.6f, 0.45f).SetEase(Ease.OutCubic))
                .Join(glow.transform.DOLocalRotate(new Vector3(0f, 0f, 120f), 0.7f, RotateMode.FastBeyond360))
                .Insert(0.2f, glow.DOFade(0f, 0.5f))
                .OnComplete(() => Destroy(glow.gameObject));

            const int stars = 12;
            for (int i = 0; i < stars; i++)
            {
                float a = (i + UnityEngine.Random.Range(-0.3f, 0.3f)) * Mathf.PI * 2f / stars;
                var dir = new Vector2(Mathf.Cos(a), Mathf.Sin(a));
                Color c = Color.Lerp(new Color(1f, 0.95f, 0.6f), new Color(1f, 0.6f, 0.9f), UnityEngine.Random.value);
                Image star = Spawn("Star", UnityEngine.Random.Range(34f, 60f), c, false);
                star.transform.localScale = Vector3.zero;
                float dist = UnityEngine.Random.Range(230f, 380f), time = UnityEngine.Random.Range(0.5f, 0.75f);
                DOTween.Sequence().SetLink(star.gameObject)
                    .Append(star.rectTransform.DOAnchorPos(at + dir * dist, time).SetEase(Ease.OutCubic))
                    .Join(star.transform.DOScale(1.2f, time * 0.3f).SetEase(Ease.OutBack))
                    .Join(star.transform.DOLocalRotate(new Vector3(0f, 0f, UnityEngine.Random.Range(-360f, 360f)), time, RotateMode.FastBeyond360))
                    .Insert(time * 0.45f, star.transform.DOScale(0f, time * 0.55f).SetEase(Ease.InQuad))
                    .Insert(time * 0.45f, star.DOFade(0f, time * 0.55f))
                    .OnComplete(() => Destroy(star.gameObject));
            }
        }

        /// <summary>Lá đã có sẵn trên tay (không phải vừa rút): hiện thẳng ở chỗ nghỉ, không chạy clip bốc bài.</summary>
        public void ShowInstant()
        {
            dealSequence?.Kill();
            rectTransform.DOKill();
            rectTransform.anchoredPosition = restAnchoredPosition;
            rectTransform.localRotation = restRotation;
            canvasGroup.alpha = 1f;
            if (drawAnimator != null) drawAnimator.enabled = false; // như sau DrawFx("end"): để hover tween không bị clip ghi đè
        }

        /// <summary>Animation Event của clip Draw.</summary>
        public void DrawFx(string id)
        {
            switch (id)
            {
                case "flip": GridBattle.Sfx.Play(GridBattle.SfxId.CardFlip, 0.9f); break;
                case "shine": TriggerShineEffect(); break;
                // Hết clip: tắt Animator để hover / vệt sáng (tween) không bị clip ghi đè.
                case "end": drawAnimator.enabled = false; break;
            }
        }

        /// <summary>Dời vị trí + góc nghỉ mượt (khi số lá trên tay thay đổi).</summary>
        public void MoveRestPosition(Vector2 pos, float angle, float duration)
        {
            restAnchoredPosition = pos;
            restRotation = Quaternion.Euler(0f, 0f, angle);
            if (isConsumed) return;
            rectTransform.DOAnchorPos(pos, duration).SetEase(Ease.OutQuad);
            rectTransform.DOLocalRotateQuaternion(restRotation, duration).SetEase(Ease.OutQuad);
        }

        /// <summary>Thu lá bài về đáy màn hình rồi tự huỷ.</summary>
        public void Discard()
        {
            isConsumed = true;
            dealSequence?.Kill();
            canvasGroup.blocksRaycasts = false;
            rectTransform.DOKill();
            canvasGroup.DOKill();
            rectTransform.DOAnchorPosY(restAnchoredPosition.y - 320f, 0.3f).SetEase(Ease.InBack);
            canvasGroup.DOFade(0f, 0.3f).OnComplete(() => Destroy(gameObject));
        }

        private void Deny()
        {
            GridBattle.Sfx.Play(GridBattle.SfxId.Deny);
            rectTransform.DOKill(true);
            rectTransform.DOShakeAnchorPos(0.3f, new Vector2(18f, 0f), 20, 0f);
            // Nháy đỏ 2 lần rồi về màu gốc. DOKill(true) hoàn tất lần nháy trước -> màu đã về gốc trước khi lấy lại.
            if (cardArtImage == null) return;
            cardArtImage.DOKill(true);
            Color baseColor = cardArtImage.color;
            cardArtImage.DOColor(new Color(1f, 0.3f, 0.3f, baseColor.a), 0.08f).SetLoops(4, LoopType.Yoyo)
                .OnComplete(() => cardArtImage.color = baseColor);
        }

        public void OnPointerEnter(PointerEventData eventData)
        {
            if (isConsumed) return;

            GridBattle.Sfx.Play(GridBattle.SfxId.CardHover);
            rectTransform.DOKill();

            // 1. Nhấc lá bài lên cao, phóng to và đưa lên trên các lá bên cạnh.
            // Tween cả X: DOKill có thể vừa cắt ngang MoveRestPosition, chỉ tween Y thì lá kẹt lệch chỗ.
            hoverSiblingIndex = transform.GetSiblingIndex();
            transform.SetAsLastSibling();
            rectTransform.DOAnchorPos(restAnchoredPosition + Vector2.up * hoverLiftHeight, hoverDuration)
                .SetEase(Ease.OutBack);
            rectTransform.DOScale(restScale * hoverScale, hoverDuration)
                .SetEase(Ease.OutBack);

            // 2. Dựng thẳng lá đang xoè nghiêng (kiểu Balatro) cho dễ đọc
            rectTransform.DOLocalRotate(Vector3.zero, hoverDuration)
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
            if (hoverSiblingIndex >= 0) transform.SetSiblingIndex(hoverSiblingIndex);
            hoverSiblingIndex = -1;
            rectTransform.DOAnchorPos(restAnchoredPosition, hoverDuration)
                .SetEase(Ease.OutQuad);
            rectTransform.DOScale(restScale, hoverDuration)
                .SetEase(Ease.OutQuad);
            rectTransform.DOLocalRotateQuaternion(restRotation, hoverDuration)
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
            if (eventData.button == PointerEventData.InputButton.Right)
            {
                RightClickHandler?.Invoke(this);
                return;
            }
            if (ClickHandler != null && !ClickHandler(this))
            {
                Deny();
                return;
            }
            isConsumed = true;
            GridBattle.Sfx.Play(GridBattle.SfxId.CardFlip);

            // Khóa tương tác
            if (canvasGroup != null) canvasGroup.blocksRaycasts = false;
            if (shineSequence != null) shineSequence.Kill();
            if (shineImage != null) shineImage.gameObject.SetActive(false);
            if (sparkleParticle != null) sparkleParticle.Stop(true, ParticleSystemStopBehavior.StopEmitting);

            rectTransform.DOKill();
            canvasGroup.DOKill();

            // Gửi tín hiệu tạo thực thể 3D tương ứng
            if (ClickHandler == null && CardManager.Instance != null)
            {
                CardManager.Instance.OnCardClicked(this, spawnPrefab);
            }

            // Hiệu ứng tiêu hao lá bài: Nảy bung lên một nhịp rồi thụt nhanh xuống đáy màn hình và mờ dần
            Sequence consumeSequence = DOTween.Sequence();
            consumeSequence.Append(rectTransform.DOPunchScale(Vector3.one * 0.15f, 0.12f, 12, 1));
            consumeSequence.Append(rectTransform.DOAnchorPosY(restAnchoredPosition.y - 280f, 0.38f).SetEase(Ease.InBack));
            consumeSequence.Join(rectTransform.DOLocalRotate(new Vector3(0, 0, UnityEngine.Random.Range(-15f, 15f)), 0.38f));
            consumeSequence.Join(canvasGroup.DOFade(0f, 0.38f));
            consumeSequence.OnComplete(() =>
            {
                // Lá bài trong trận được tạo bằng code -> huỷ hẳn; lá trong CardScene giữ hành vi cũ.
                if (ClickHandler != null) Destroy(gameObject);
                else gameObject.SetActive(false);
            });
        }

        private void OnDestroy()
        {
            // Lá bị huỷ giữa chừng (vd đang chia bài): dừng mọi tween còn trỏ tới nó.
            if (rectTransform != null) rectTransform.DOKill();
            if (canvasGroup != null) canvasGroup.DOKill();
            if (shineSequence != null) shineSequence.Kill();
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
