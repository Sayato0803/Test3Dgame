using DG.Tweening;
using UnityEngine;
using UnityEngine.UI;

namespace GridBattle
{
    /// <summary>
    /// Tiện ích UI dùng lúc chạy. Bản thân UI nằm sẵn trong scene / prefab (chỉnh trong Editor),
    /// script chỉ bật/tắt panel và gán chữ.
    /// </summary>
    public static class UiFactory
    {
        public static void SetText(this Button b, string text) => b.GetComponentInChildren<Text>().text = text;

        /// <summary>Hiện/ẩn 1 nhóm UI có fade + phóng nhẹ. Chạy cả khi Time.timeScale = 0 (pause).</summary>
        public static void Show(CanvasGroup g, bool show, float duration = 0.2f)
        {
            g.DOKill();
            g.transform.DOKill();
            g.interactable = show;
            g.blocksRaycasts = show;
            if (show)
            {
                g.gameObject.SetActive(true);
                g.transform.localScale = Vector3.one * 0.96f;
                g.transform.DOScale(1f, duration).SetEase(Ease.OutBack).SetUpdate(true);
                g.DOFade(1f, duration).SetUpdate(true);
            }
            else
            {
                g.DOFade(0f, duration).SetUpdate(true).OnComplete(() => g.gameObject.SetActive(false));
            }
        }
    }
}
