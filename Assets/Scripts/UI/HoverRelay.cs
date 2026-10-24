using System;
using UnityEngine;
using UnityEngine.EventSystems;

namespace GridBattle
{
    /// <summary>Báo khi con trỏ vào / rời 1 phần tử UI.</summary>
    public class HoverRelay : MonoBehaviour, IPointerEnterHandler, IPointerExitHandler
    {
        public Action<bool> Changed;
        public void OnPointerEnter(PointerEventData e) => Changed?.Invoke(true);
        public void OnPointerExit(PointerEventData e) => Changed?.Invoke(false);
        private void OnDisable() => Changed?.Invoke(false);
    }
}
