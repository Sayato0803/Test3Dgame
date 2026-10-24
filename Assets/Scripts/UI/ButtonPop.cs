using DG.Tweening;
using UnityEngine;
using UnityEngine.EventSystems;
using UnityEngine.UI;

namespace GridBattle
{
    /// <summary>Nút nảy nhẹ khi rê chuột + tiếng click (unscaled time để vẫn chạy trong pause menu).</summary>
    public class ButtonPop : MonoBehaviour, IPointerEnterHandler, IPointerExitHandler
    {
        [SerializeField] private float _hoverScale = 1.06f;
        [SerializeField] private float _duration = 0.12f;

        private void Awake()
        {
            if (!TryGetComponent(out Button b)) return;
            b.onClick.AddListener(() => Sfx.Play(SfxId.UiClick));
            // Click chuột để lại trạng thái Selected -> nút giữ sprite viền focus sang cả lượt người sau.
            b.onClick.AddListener(() => { if (EventSystem.current != null) EventSystem.current.SetSelectedGameObject(null); });
        }

        public void OnPointerEnter(PointerEventData e)
        {
            if (TryGetComponent(out Selectable s) && s.interactable) Sfx.Play(SfxId.UiHover);
            transform.DOKill();
            transform.DOScale(_hoverScale, _duration).SetEase(Ease.OutBack).SetUpdate(true);
        }

        public void OnPointerExit(PointerEventData e)
        {
            transform.DOKill();
            transform.DOScale(1f, _duration).SetUpdate(true);
        }

        private void OnDisable() => transform.localScale = Vector3.one;
    }
}
