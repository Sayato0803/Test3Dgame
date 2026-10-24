using System;
using UnityEngine;

namespace GridBattle
{
    /// <summary>Bảng hướng dẫn điều khiển (prefab dùng chung cho Main Menu và Pause Menu). Nội dung (icon phím / chuột + chữ) sửa thẳng trong prefab.</summary>
    [RequireComponent(typeof(CanvasGroup))]
    public class TutorialPanel : MonoBehaviour
    {
        [SerializeField] private float _fadeDuration = 0.2f;

        /// <summary>Bảng vừa đóng -> màn hình gọi nó hiện lại.</summary>
        public event Action Closed;

        public bool IsOpen => gameObject.activeSelf;

        public void Open() => UiFactory.Show(GetComponent<CanvasGroup>(), true, _fadeDuration);

        public void Close()
        {
            UiFactory.Show(GetComponent<CanvasGroup>(), false, _fadeDuration);
            Closed?.Invoke();
        }
    }
}
