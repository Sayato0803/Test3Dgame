using System.Collections.Generic;
using CardScripts;
using UnityEngine;
using UnityEngine.UI;

namespace GridBattle
{
    /// <summary>
    /// Tay bài trong trận: thay cho nút OnGUI cũ. Số lá mỗi loại = số quân người chơi hiện tại còn được tạo
    /// (Phases.RemainingSpawns), nên Deploy có 2 lá King, Attack có 2 Knight + 2 Mage.
    /// Bấm lá -> GridPlacer.TrySpawnFromCard (quân bay ra theo chuột). Huỷ kéo -> lá được chia lại.
    /// </summary>
    public class CardHand : MonoBehaviour
    {
        [SerializeField] private Phases _phases;
        [SerializeField] private GridPlacer _placer;

        [Header("Hình lá bài")]
        [SerializeField] private Sprite _kingSprite;
        [SerializeField] private Sprite _knightSprite;
        [SerializeField] private Sprite _mageSprite;
        [SerializeField] private Sprite _shineSprite;

        [Header("Mô tả trên ô giấy cuối thẻ")]
        [SerializeField, TextArea] private string _kingText = "Giấu trên bàn mình\n10 máu";
        [SerializeField, TextArea] private string _knightText = "Tầm đánh 3 ô\nphía trước";
        [SerializeField, TextArea] private string _mageText = "Tầm đánh 4 ô\nphía trước";

        [Header("Bố cục (pixel tham chiếu 1920x1080)")]
        [SerializeField] private Vector2 _cardSize = new(170f, 255f);
        [SerializeField] private float _spacing = 185f;
        [SerializeField] private float _restY = 120f;
        [SerializeField] private float _arcDrop = 10f;

        private static readonly UnitKind[] Order = { UnitKind.King, UnitKind.Knight, UnitKind.Mage };

        private RectTransform _hand;
        private readonly List<CardView> _cards = new();

        private void Awake()
        {
            Canvas canvas = UiFactory.CreateCanvas("CardHandCanvas", 5);
            canvas.transform.SetParent(transform, false);
            _hand = UiFactory.Rect("Hand", canvas.transform)
                .Place(new Vector2(0.5f, 0f), Vector2.zero, new Vector2(1400f, 400f));
        }

        private void OnEnable()
        {
            if (_phases != null) _phases.StateChanged += Refresh;
            if (_placer != null)
            {
                _placer.OnDragStarted += OnDrag;
                _placer.OnDragEnded += OnDragEnd;
            }
        }

        private void OnDisable()
        {
            if (_phases != null) _phases.StateChanged -= Refresh;
            if (_placer != null)
            {
                _placer.OnDragStarted -= OnDrag;
                _placer.OnDragEnded -= OnDragEnd;
            }
        }

        private void Start() => Refresh();

        private void OnDrag(GridUnit _) => Refresh();
        private void OnDragEnd(GridUnit _, bool __) => Refresh();

        /// <summary>So số lá mong muốn với số lá đang có: thiếu thì chia thêm, thừa thì thu về; rồi xếp lại.</summary>
        private void Refresh()
        {
            _cards.RemoveAll(c => c == null || c.IsConsumed);

            var want = new List<UnitKind>();
            foreach (UnitKind kind in Order)
            {
                int count = _phases != null ? _phases.RemainingSpawns(kind) : 1;
                if (_placer != null && _placer.DraggingNewKind == kind) count--; // lá đang được dùng
                for (int i = 0; i < count; i++) want.Add(kind);
            }

            // Giữ lá đã có nếu còn cần, thu về lá thừa.
            var keep = new List<CardView>();
            var remaining = new List<UnitKind>(want);
            foreach (CardView c in _cards)
            {
                var kind = (UnitKind)c.Tag;
                if (remaining.Remove(kind)) keep.Add(c);
                else c.Discard();
            }

            var fresh = new HashSet<CardView>();
            foreach (UnitKind kind in remaining)
            {
                CardView card = CreateCard(kind);
                card.Tag = kind;
                keep.Add(card);
                fresh.Add(card);
            }

            keep.Sort((a, b) => ((UnitKind)a.Tag).CompareTo((UnitKind)b.Tag));
            _cards.Clear();
            _cards.AddRange(keep);

            // Xếp hình quạt: lá giữa cao nhất. Lá mới chia bay từ đáy lên, lá cũ trượt sang chỗ mới.
            int n = _cards.Count, dealt = 0;
            for (int i = 0; i < n; i++)
            {
                float offset = i - (n - 1) * 0.5f;
                var pos = new Vector2(offset * _spacing, _restY - offset * offset * _arcDrop);
                CardView c = _cards[i];
                c.transform.SetSiblingIndex(i);
                if (fresh.Contains(c))
                {
                    c.SetRestPosition(pos);
                    c.DealIn(0.08f * dealt++);
                }
                else c.MoveRestPosition(pos, 0.25f);
            }
        }

        private bool OnCardClicked(CardView card)
        {
            if (_placer == null) return false;
            Vector2 screen = RectTransformUtility.WorldToScreenPoint(null, card.transform.position);
            // Bỏ lá khỏi tay trước: TrySpawnFromCard phát OnDragStarted -> Refresh, lá này không được tính nữa.
            _cards.Remove(card);
            bool ok = _placer.TrySpawnFromCard((UnitKind)card.Tag, screen);
            if (!ok) _cards.Add(card);
            return ok;
        }

        private CardView CreateCard(UnitKind kind)
        {
            RectTransform rt = UiFactory.Rect($"Card_{kind}", _hand);
            rt.Place(new Vector2(0.5f, 0f), Vector2.zero, _cardSize);
            rt.pivot = new Vector2(0.5f, 0.5f);

            Sprite sprite = kind switch
            {
                UnitKind.King => _kingSprite,
                UnitKind.Knight => _knightSprite,
                _ => _mageSprite
            };

            // Hình thẻ vừa là ảnh vừa là mask: góc bo trong suốt cắt luôn vệt sáng quét qua.
            var art = rt.gameObject.AddComponent<Image>();
            art.sprite = sprite;
            art.color = Color.white;
            rt.gameObject.AddComponent<Mask>().showMaskGraphic = true;

            if (sprite != null)
            {
                string desc = kind switch
                {
                    UnitKind.King => _kingText,
                    UnitKind.Knight => _knightText,
                    _ => _mageText
                };
                // Ô giấy trống ở đáy thẻ (khoảng 6%..27% chiều cao).
                Text t = UiFactory.Label("Desc", rt, desc, 17, TextAnchor.MiddleCenter, new Color(0.36f, 0.22f, 0.12f), FontStyle.Bold);
                t.rectTransform.anchorMin = new Vector2(0.12f, 0.07f);
                t.rectTransform.anchorMax = new Vector2(0.88f, 0.26f);
                t.rectTransform.offsetMin = t.rectTransform.offsetMax = Vector2.zero;
                t.lineSpacing = 0.9f;
                Object.Destroy(t.GetComponent<Shadow>());
            }
            else
            {
                // Chưa gán hình: lá màu + tên quân.
                art.color = kind == UnitKind.King ? new Color(0.85f, 0.7f, 0.25f)
                    : kind == UnitKind.Knight ? new Color(0.75f, 0.3f, 0.3f) : new Color(0.3f, 0.55f, 0.85f);
                UiFactory.Label("Name", rt, kind.ToString(), 30, TextAnchor.MiddleCenter, null, FontStyle.Bold).rectTransform.Stretch();
            }

            Image shine = null;
            if (_shineSprite != null)
            {
                shine = UiFactory.Panel("ShineSweep", rt, new Color(1f, 1f, 1f, 0.55f), false);
                shine.sprite = _shineSprite;
                shine.rectTransform.Place(new Vector2(0.5f, 0.5f), new Vector2(-280f, 0f), new Vector2(_cardSize.x, _cardSize.y * 1.3f));
                shine.rectTransform.localRotation = Quaternion.Euler(0f, 0f, 25f);
            }

            var view = rt.gameObject.AddComponent<CardView>();
            view.Configure(art, shine, null);
            view.ClickHandler = OnCardClicked;
            return view;
        }
    }
}
