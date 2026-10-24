using System;
using DG.Tweening;
using UnityEngine;
using UnityEngine.EventSystems;
using UnityEngine.UI;

namespace GridBattle
{
    /// <summary>
    /// Dựng uGUI bằng code với 1 phong cách chung kiểu hybrid-casual (panel bo tròn viền dày, nút bóng, chữ đậm có viền),
    /// để không phải sửa tay từng scene. Font: Lilita One (Resources/GUI/fonts, giấy phép OFL), thiếu thì dùng LegacyRuntime.
    /// </summary>
    public static class UiFactory
    {
        public static readonly Color PanelColor = new(0.16f, 0.22f, 0.45f, 0.96f);
        public static readonly Color TextOutline = new(0.07f, 0.06f, 0.16f, 1f);
        public static readonly Color ButtonColor = new(0.16f, 0.18f, 0.26f, 1f);
        public static readonly Color ButtonHover = new(0.26f, 0.3f, 0.42f, 1f);
        public static readonly Color Accent = new(1f, 0.83f, 0.22f, 1f);
        public static readonly Color TextColor = new(0.94f, 0.94f, 0.97f, 1f);

        private static Font _font;
        public static Font Font => _font != null ? _font
            : _font = Resources.Load<Font>("GUI/fonts/LilitaOne-Regular") ?? Resources.GetBuiltinResource<Font>("LegacyRuntime.ttf");

        public static void EnsureEventSystem()
        {
            if (EventSystem.current != null || UnityEngine.Object.FindAnyObjectByType<EventSystem>() != null) return;
            var go = new GameObject("EventSystem", typeof(EventSystem));
            Type module = Type.GetType("UnityEngine.InputSystem.UI.InputSystemUIInputModule, Unity.InputSystem");
            if (module != null) go.AddComponent(module);
            else go.AddComponent<StandaloneInputModule>();
        }

        public static Canvas CreateCanvas(string name, int sortingOrder)
        {
            EnsureEventSystem();
            var go = new GameObject(name, typeof(RectTransform));
            var canvas = go.AddComponent<Canvas>();
            canvas.renderMode = RenderMode.ScreenSpaceOverlay;
            canvas.sortingOrder = sortingOrder;
            var scaler = go.AddComponent<CanvasScaler>();
            scaler.uiScaleMode = CanvasScaler.ScaleMode.ScaleWithScreenSize;
            scaler.referenceResolution = new Vector2(1920, 1080);
            scaler.matchWidthOrHeight = 0.5f;
            go.AddComponent<GraphicRaycaster>();
            return canvas;
        }

        public static RectTransform Rect(string name, Transform parent)
        {
            var go = new GameObject(name, typeof(RectTransform));
            go.transform.SetParent(parent, false);
            return (RectTransform)go.transform;
        }

        public static RectTransform Stretch(this RectTransform rt, float inset = 0f)
        {
            rt.anchorMin = Vector2.zero;
            rt.anchorMax = Vector2.one;
            rt.offsetMin = new Vector2(inset, inset);
            rt.offsetMax = new Vector2(-inset, -inset);
            return rt;
        }

        /// <summary>Đặt anchor + pivot cùng 1 điểm (0..1), vị trí và kích thước tính bằng pixel tham chiếu 1920x1080.</summary>
        public static RectTransform Place(this RectTransform rt, Vector2 anchor, Vector2 pos, Vector2 size)
        {
            rt.anchorMin = rt.anchorMax = rt.pivot = anchor;
            rt.anchoredPosition = pos;
            rt.sizeDelta = size;
            return rt;
        }

        public static Image Panel(string name, Transform parent, Color color, bool blockRaycasts = true)
        {
            RectTransform rt = Rect(name, parent);
            var img = rt.gameObject.AddComponent<Image>();
            img.color = color;
            img.raycastTarget = blockRaycasts;
            return img;
        }

        /// <summary>Panel bo tròn viền dày (ảnh 9-slice trắng, tô bằng 'color'). Thiếu ảnh thì là panel vuông màu.</summary>
        public static Image RoundPanel(string name, Transform parent, Color color, bool blockRaycasts = true)
        {
            Image img = Panel(name, parent, color, blockRaycasts);
            img.sprite = Resources.Load<Sprite>("GUI/ui_panels/panel");
            img.type = Image.Type.Sliced;
            img.pixelsPerUnitMultiplier = 1.4f;
            return img;
        }

        /// <summary>Hộp thoại: panel bo tròn màu chủ đạo, đặt giữa màn hình.</summary>
        public static Image Window(string name, Transform parent, Vector2 size)
        {
            Image img = RoundPanel(name, parent, new Color(PanelColor.r, PanelColor.g, PanelColor.b, 1f));
            img.rectTransform.Place(new Vector2(0.5f, 0.5f), Vector2.zero, size);
            return img;
        }

        public static Text Label(string name, Transform parent, string text, int size,
            TextAnchor align = TextAnchor.MiddleCenter, Color? color = null, FontStyle style = FontStyle.Normal)
        {
            RectTransform rt = Rect(name, parent);
            var t = rt.gameObject.AddComponent<Text>();
            t.font = Font;
            t.text = text;
            t.fontSize = size;
            // Lilita One vốn đã đậm: in đậm giả của Unity làm chữ nhoè -> chỉ dùng FontStyle với font dự phòng.
            t.fontStyle = Font.name.StartsWith("Lilita") ? FontStyle.Normal : style;
            t.alignment = align;
            t.color = color ?? TextColor;
            t.raycastTarget = false;
            t.horizontalOverflow = HorizontalWrapMode.Wrap;
            t.verticalOverflow = VerticalWrapMode.Overflow;
            // Chữ kiểu hybrid-casual: viền tối dày + bóng đổ xuống dưới.
            var outline = rt.gameObject.AddComponent<Outline>();
            outline.effectColor = TextOutline;
            outline.effectDistance = new Vector2(2f, -2f);
            var shadow = rt.gameObject.AddComponent<Shadow>();
            shadow.effectColor = new Color(TextOutline.r, TextOutline.g, TextOutline.b, 0.8f);
            shadow.effectDistance = new Vector2(0f, -4f);
            return t;
        }

        public static Button Button(string name, Transform parent, string text, Action onClick, int fontSize = 32)
        {
            Image bg = Panel(name, parent, ButtonHover);
            var outline = bg.gameObject.AddComponent<Outline>();
            outline.effectColor = new Color(Accent.r, Accent.g, Accent.b, 0.35f);
            outline.effectDistance = new Vector2(2f, -2f);

            var btn = bg.gameObject.AddComponent<Button>();
            btn.targetGraphic = bg;
            ColorBlock colors = btn.colors;
            // Ảnh nền mang màu hover; trạng thái thường tối hơn, disabled tối + mờ hẳn.
            colors.normalColor = new Color(ButtonColor.r / ButtonHover.r, ButtonColor.g / ButtonHover.g, ButtonColor.b / ButtonHover.b);
            colors.highlightedColor = Color.white;
            colors.selectedColor = colors.normalColor;
            colors.pressedColor = new Color(0.75f, 0.75f, 0.75f);
            colors.disabledColor = new Color(0.45f, 0.45f, 0.45f, 0.45f);
            colors.colorMultiplier = 1f;
            btn.colors = colors;
            btn.onClick.AddListener(() => Sfx.Play(SfxId.UiClick));
            if (onClick != null) btn.onClick.AddListener(() => onClick());

            Label("Text", bg.transform, text, fontSize, TextAnchor.MiddleCenter, null, FontStyle.Bold).rectTransform.Stretch();
            bg.gameObject.AddComponent<ButtonPop>();
            return btn;
        }

        public static void SetText(this Button b, string text) => b.GetComponentInChildren<Text>().text = text;

        /// <summary>
        /// Thay nền nút bằng ảnh Resources/GUI/ui_buttons/&lt;sprite&gt;. Ảnh đã có chữ/icon: ẩn label.
        /// keepText (ảnh nền trống btn_blank_*): ảnh 9-slice co giãn theo nút, giữ label + viền chữ đậm. Thiếu ảnh thì giữ nút chữ.
        /// </summary>
        public static Button Skin(this Button b, string sprite, bool keepText = false)
        {
            Sprite s = Resources.Load<Sprite>("GUI/ui_buttons/" + sprite);
            if (s == null) return b;
            var img = (Image)b.targetGraphic;
            img.sprite = s;
            img.color = Color.white;
            b.GetComponent<Outline>().enabled = false;
            Text label = b.GetComponentInChildren<Text>();
            if (keepText)
            {
                img.type = Image.Type.Sliced;
                img.pixelsPerUnitMultiplier = 2.5f; // viền 9-slice 90px -> ~36 đơn vị UI, vừa cả nút cao 64
                label.color = Color.white;
                label.rectTransform.offsetMin = new Vector2(0f, 8f); // bóng đáy nút dày hơn đỉnh
            }
            else
            {
                img.preserveAspect = true;
                label.enabled = false;
            }
            ColorBlock colors = b.colors;
            colors.normalColor = colors.selectedColor = colors.highlightedColor = Color.white;
            b.colors = colors;
            return b;
        }

        public static Slider Slider(string name, Transform parent, float min, float max, float value, Action<float> onChange)
        {
            RectTransform root = Rect(name, parent);
            var slider = root.gameObject.AddComponent<Slider>();

            Image bg = Panel("Background", root, new Color(0.2f, 0.22f, 0.3f, 1f));
            bg.rectTransform.anchorMin = new Vector2(0f, 0.35f);
            bg.rectTransform.anchorMax = new Vector2(1f, 0.65f);
            bg.rectTransform.offsetMin = bg.rectTransform.offsetMax = Vector2.zero;

            RectTransform fillArea = Rect("Fill Area", root);
            fillArea.anchorMin = new Vector2(0f, 0.35f);
            fillArea.anchorMax = new Vector2(1f, 0.65f);
            fillArea.offsetMin = new Vector2(5f, 0f);
            fillArea.offsetMax = new Vector2(-15f, 0f);
            Image fill = Panel("Fill", fillArea, Accent, false);
            fill.rectTransform.sizeDelta = new Vector2(10f, 0f);

            RectTransform handleArea = Rect("Handle Area", root).Stretch();
            handleArea.offsetMin = new Vector2(10f, 0f);
            handleArea.offsetMax = new Vector2(-10f, 0f);
            Image handle = Panel("Handle", handleArea, Color.white);
            handle.rectTransform.sizeDelta = new Vector2(24f, 0f);

            slider.fillRect = fill.rectTransform;
            slider.handleRect = handle.rectTransform;
            slider.targetGraphic = handle;
            slider.minValue = min;
            slider.maxValue = max;
            slider.value = value;
            if (onChange != null) slider.onValueChanged.AddListener(v => onChange(v));
            return slider;
        }

        /// <summary>Xếp con theo chiều dọc.</summary>
        public static VerticalLayoutGroup Vertical(RectTransform rt, float spacing, int padding = 0)
        {
            var v = rt.gameObject.AddComponent<VerticalLayoutGroup>();
            v.spacing = spacing;
            v.padding = new RectOffset(padding, padding, padding, padding);
            v.childAlignment = TextAnchor.MiddleCenter;
            v.childControlWidth = true;
            v.childControlHeight = true;
            v.childForceExpandWidth = true;
            v.childForceExpandHeight = false;
            return v;
        }

        public static T Height<T>(this T c, float h) where T : Component
        {
            if (!c.TryGetComponent(out LayoutElement le)) le = c.gameObject.AddComponent<LayoutElement>();
            le.preferredHeight = h;
            le.minHeight = h;
            return c;
        }

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

        public static CanvasGroup Group(GameObject go, bool visible)
        {
            if (!go.TryGetComponent(out CanvasGroup g)) g = go.AddComponent<CanvasGroup>();
            g.alpha = visible ? 1f : 0f;
            g.interactable = g.blocksRaycasts = visible;
            go.SetActive(visible);
            return g;
        }
    }

    /// <summary>Nút nảy nhẹ khi rê chuột (unscaled time để vẫn chạy trong pause menu).</summary>
    public class ButtonPop : MonoBehaviour, IPointerEnterHandler, IPointerExitHandler
    {
        public void OnPointerEnter(PointerEventData e)
        {
            if (TryGetComponent(out Selectable s) && s.interactable) Sfx.Play(SfxId.UiHover);
            transform.DOKill();
            transform.DOScale(1.06f, 0.12f).SetEase(Ease.OutBack).SetUpdate(true);
        }

        public void OnPointerExit(PointerEventData e)
        {
            transform.DOKill();
            transform.DOScale(1f, 0.12f).SetUpdate(true);
        }

        private void OnDisable() => transform.localScale = Vector3.one;
    }
}
