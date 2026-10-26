using System.Collections.Generic;
using CardScripts;
using UnityEngine;
using UnityEngine.UI;

namespace GridBattle
{
    /// <summary>
    /// Tay bài trong trận: thay cho nút OnGUI cũ. Số lá quân mỗi loại = Phases.RemainingSpawns
    /// (Deploy: 2 lá King; Knight / Mage: số lá đã rút được từ bộ bài; Scout: 1 lá nếu lượt này còn dùng được).
    /// Sau các lá quân là lá phép trên tay người chơi (Player.Hand); bấm lá phép -> GridPlacer.BeginCard.
    /// Chuột phải vào lá -> xem ảnh thẻ phóng to + mô tả chức năng bên dưới.
    /// Bấm lá -> GridPlacer.TrySpawnFromCard (quân bay ra theo chuột). Huỷ kéo -> lá được chia lại.
    /// </summary>
    public class CardHand : MonoBehaviour
    {
        [SerializeField] private Phases _phases;
        [SerializeField] private GridPlacer _placer;

        [Header("UI trong scene")]
        [SerializeField] private RectTransform _hand;
        [SerializeField] private CardView _cardPrefab;
        [SerializeField] private CanvasGroup _preview;
        [SerializeField] private Image _previewArt;
        [SerializeField] private Text _previewDesc;
        [SerializeField] private float _previewFade = 0.2f;
        [Tooltip("Số lá đang cầm / tối đa (vd 3/8). Đỏ khi tay đầy.")]
        [SerializeField] private Text _countLabel;
        [Tooltip("Nút Draw đầu lượt (OnClick -> Phases.DrawCard); chỉ hiện khi Phases.MustDraw.")]
        [SerializeField] private Button _drawButton;

        [Header("Hình lá bài")]
        [SerializeField] private Sprite _kingSprite;
        [SerializeField] private Sprite _knightSprite;
        [SerializeField] private Sprite _mageSprite;
        [SerializeField] private Sprite _scoutSprite;

        [Header("Mô tả chức năng (chuột phải vào thẻ để xem)")]
        [SerializeField, TextArea] private string _kingText = "Hide it on your board.\n10 HP";
        [SerializeField, TextArea] private string _knightText = "Strikes 3 cells\nin front of it";
        [SerializeField, TextArea] private string _mageText = "Strikes 4 cells\nin front of it";
        [SerializeField, TextArea] private string _scoutText = "Once per match:\nreveal enemy Kings in a 2x2 area";

        [Header("Bố cục (pixel tham chiếu 1920x1080)")]
        [Tooltip("Khoảng cách tâm 2 lá; nhỏ hơn bề rộng lá (170) để lá đè nhau một chút.")]
        [SerializeField] private float _spacing = 150f;
        [SerializeField] private float _restY = 120f;
        [Tooltip("Lá ngoài cùng thấp hơn lá giữa bao nhiêu pixel khi tay xoè hết cỡ.")]
        [SerializeField] private float _arcHeight = 20f;
        [Tooltip("Góc nghiêng (độ) của lá ngoài cùng khi tay xoè hết cỡ.")]
        [SerializeField] private float _fanAngle = 4f;
        [Tooltip("Khoảng cách tối đa giữa lá đầu và lá cuối; nhiều lá thì khít lại.")]
        [SerializeField] private float _maxHandWidth = 1500f;

        private static readonly UnitKind[] Order = { UnitKind.King, UnitKind.Knight, UnitKind.Mage, UnitKind.Scout };

        private readonly List<CardView> _cards = new();

        private void OnEnable()
        {
            if (_phases != null)
            {
                _phases.StateChanged += Refresh;
                _phases.CardsDrawn += OnCardsDrawn;
            }
            if (_placer != null)
            {
                _placer.OnDragStarted += OnDrag;
                _placer.OnDragEnded += OnDragEnd;
            }
        }

        private void OnDisable()
        {
            if (_phases != null)
            {
                _phases.StateChanged -= Refresh;
                _phases.CardsDrawn -= OnCardsDrawn;
            }
            if (_placer != null)
            {
                _placer.OnDragStarted -= OnDrag;
                _placer.OnDragEnded -= OnDragEnd;
            }
        }

        private void Start() => Refresh();

        /// <summary>Nút Sort: gom lá phép cùng loại đứng cạnh nhau (lá quân vốn đã xếp theo Order). Lá cũ trượt sang chỗ mới.</summary>
        public void SortHand()
        {
            if (_phases == null || _phases.CurrentPlayer == null) return;
            // Thứ tự Hand không ảnh hưởng luật (chỉ Add/Remove/Contains). OrderBy ổn định: lá cùng loại giữ thứ tự bốc.
            var sorted = new List<CardData>(System.Linq.Enumerable.OrderBy(_phases.CurrentPlayer.Hand, c => c.Type));
            _phases.CurrentPlayer.Hand.Clear();
            _phases.CurrentPlayer.Hand.AddRange(sorted);
            Refresh();
        }

        // Lá vừa rút: lần Refresh tới chạy hoạt ảnh bốc cho chúng; lá khác (đổi người, huỷ kéo, mở đầu) hiện thẳng ở tay.
        private readonly List<object> _justDrawn = new();
        private void OnCardsDrawn(IReadOnlyList<object> tags) => _justDrawn.AddRange(tags);

        private void OnDrag(GridUnit _) => Refresh();
        private void OnDragEnd(GridUnit _, bool __) => Refresh();

        /// <summary>
        /// So lá mong muốn với lá đang có: thiếu thì chia thêm, thừa thì thu về; rồi xếp lại.
        /// Tag của lá: UnitKind (lá quân) hoặc CardData (lá phép trên tay người chơi hiện tại).
        /// </summary>
        private void Refresh()
        {
            _cards.RemoveAll(c => c == null || c.IsConsumed);

            var want = new List<object>();
            foreach (UnitKind kind in Order)
            {
                int count = _phases != null ? _phases.RemainingSpawns(kind) : 1;
                if (_placer != null && _placer.DraggingNewKind == kind) count--; // lá đang được dùng
                if (_placer != null && _placer.IsScouting && kind == UnitKind.Scout) count--;
                for (int i = 0; i < count; i++) want.Add(kind);
            }
            if (_phases != null && !_phases.IsHandingOver && _phases.CurrentPlayer != null)
            {
                CardData targeting = _placer != null ? _placer.TargetingCard : null; // lá đang chọn mục tiêu: chưa tính
                foreach (CardData card in _phases.CurrentPlayer.Hand)
                {
                    if (card == targeting) { targeting = null; continue; }
                    want.Add(card);
                }
            }

            // Giữ lá đã có nếu còn cần (theo thứ tự 'want'), thu về lá thừa.
            var pool = new List<CardView>(_cards);
            var fresh = new HashSet<CardView>();
            _cards.Clear();
            foreach (object tag in want)
            {
                CardView c = pool.Find(v => v.Tag.Equals(tag));
                if (c != null) pool.Remove(c);
                else
                {
                    c = CreateCard(tag);
                    fresh.Add(c);
                }
                _cards.Add(c);
            }
            foreach (CardView c in pool) c.Discard();

            if (_drawButton != null)
                _drawButton.gameObject.SetActive(_phases != null && _phases.MustDraw && !_phases.IsHandingOver);

            if (_countLabel != null)
            {
                // Deploy chỉ có lá King (không tính giới hạn) -> ẩn bộ đếm.
                Player p = _phases != null && !_phases.IsHandingOver && _phases.Current != GamePhase.Deploy ? _phases.CurrentPlayer : null;
                _countLabel.gameObject.SetActive(p != null);
                if (p != null)
                {
                    int count = _phases.HandCount(p);
                    _countLabel.text = $"{count}/{Player.MaxHand}";
                    _countLabel.color = count >= Player.MaxHand ? new Color(1f, 0.35f, 0.3f) : Color.white;
                }
            }

            // Xếp quạt kiểu Balatro: lá giữa cao nhất, lá hai bên thấp dần và nghiêng ra ngoài. Lá mới chia bay từ đáy lên, lá cũ trượt sang chỗ mới.
            // Cung và góc tính theo độ rộng tay (đã giới hạn bởi _maxHandWidth), không theo số lá -> nhiều lá chỉ khít lại, không tụt khỏi màn hình.
            int n = _cards.Count, dealt = 0;
            float spacing = n > 1 ? Mathf.Min(_spacing, _maxHandWidth / (n - 1)) : _spacing;
            float half = (n - 1) * 0.5f;
            float spread = half * spacing / (_maxHandWidth * 0.5f); // 0 = 1 lá, 1 = tay xoè hết cỡ
            for (int i = 0; i < n; i++)
            {
                float offset = i - half;
                float t = half > 0f ? offset / half : 0f; // -1 (trái) .. 1 (phải)
                var pos = new Vector2(offset * spacing, _restY - t * t * _arcHeight * spread);
                float angle = -t * _fanAngle * spread;
                CardView c = _cards[i];
                c.transform.SetSiblingIndex(i);
                if (fresh.Contains(c))
                {
                    c.SetRestPosition(pos, angle);
                    if (_justDrawn.Remove(c.Tag)) c.DealIn(0.6f * dealt++); // nhiều lá (Chest) thì lần lượt từng lá lên giữa màn hình
                    else c.ShowInstant();
                }
                else c.MoveRestPosition(pos, angle, 0.25f);
            }
            _justDrawn.Clear();
        }

        private bool OnCardClicked(CardView card)
        {
            if (_placer == null) return false;
            Vector2 screen = RectTransformUtility.WorldToScreenPoint(null, card.transform.position);
            // Bỏ lá khỏi tay trước: TrySpawnFromCard phát OnDragStarted -> Refresh, lá này không được tính nữa.
            _cards.Remove(card);
            bool ok = card.Tag switch
            {
                CardData spell => _placer.BeginCard(spell),
                UnitKind.Scout => _placer.BeginScout(),
                _ => _placer.TrySpawnFromCard((UnitKind)card.Tag, screen)
            };
            if (!ok) _cards.Add(card);
            return ok;
        }

        private CardView CreateCard(object tag)
        {
            CardView view = Instantiate(_cardPrefab, _hand);
            view.Tag = tag;
            view.ClickHandler = OnCardClicked;
            view.RightClickHandler = c => ShowPreview(c.Tag);
            if (tag is CardData spell)
            {
                view.name = $"Card_{spell.Type}";
                if (spell.Art != null) view.SetCardSprite(spell.Art);
                else view.SetPlaceholder(spell.DisplayName, spell.Tint);
                return view;
            }
            var kind = (UnitKind)tag;
            view.name = $"Card_{kind}";
            view.SetCardSprite(SpriteOf(kind));
            return view;
        }

        private Sprite SpriteOf(UnitKind kind) => kind switch
        {
            UnitKind.King => _kingSprite,
            UnitKind.Knight => _knightSprite,
            UnitKind.Scout => _scoutSprite,
            _ => _mageSprite
        };

        /// <summary>Ảnh thẻ phóng to giữa màn hình + mô tả chức năng bên dưới. Bấm bất kỳ đâu để đóng (OnClick -> HidePreview).</summary>
        private void ShowPreview(object tag)
        {
            if (tag is CardData spell)
            {
                _previewArt.sprite = spell.Art;
                _previewArt.color = spell.Art != null ? Color.white : spell.Tint;
                _previewDesc.text = $"{spell.DisplayName.ToUpper()}\n{spell.Description}";
                Sfx.Play(SfxId.CardFlip);
                UiFactory.Show(_preview, true, _previewFade);
                return;
            }
            var kind = (UnitKind)tag;
            _previewArt.sprite = SpriteOf(kind);
            _previewArt.color = Color.white;
            string desc = kind switch
            {
                UnitKind.King => _kingText,
                UnitKind.Knight => _knightText,
                UnitKind.Scout => _scoutText,
                _ => _mageText
            };
            _previewDesc.text = $"{kind.ToString().ToUpper()}\n{desc}";
            Sfx.Play(SfxId.CardFlip);
            UiFactory.Show(_preview, true, _previewFade);
        }

        public void HidePreview() => UiFactory.Show(_preview, false, _previewFade);
    }
}
