using DG.Tweening;
using UnityEngine;
using UnityEngine.UI;

namespace GridBattle
{
    /// <summary>
    /// HUD trận đấu bằng uGUI (thay OnGUI cũ của Phases): thanh trạng thái, nút Xác nhận, báo cáo battle,
    /// màn che đổi người, banner phase, số damage bay lên, màn hình thắng, pause menu (Esc) + Cài đặt.
    /// </summary>
    public class GameHud : MonoBehaviour
    {
        [SerializeField] private Phases _phases;
        [SerializeField] private GridPlacer _placer;
        [SerializeField] private string _mainMenuScene = "MainMenu";

        private RectTransform _root;
        private Text _title;
        private Text _hint;
        private Button _submit;
        private CanvasGroup _report;
        private Text _reportText;
        private CanvasGroup _handover;
        private Text _handoverTitle;
        private CanvasGroup _gameOver;
        private Text _gameOverTitle;
        private CanvasGroup _pause;
        private SettingsPanel _settings;
        private RectTransform _banner;
        private Text _bannerText;

        private GamePhase _lastPhase = (GamePhase)(-1);
        private Player _lastPlayer;
        private bool _draggingLastFrame;

        public bool IsPaused { get; private set; }

        private void Awake()
        {
            Canvas canvas = UiFactory.CreateCanvas("HudCanvas", 10);
            canvas.transform.SetParent(transform, false);
            _root = (RectTransform)canvas.transform;

            BuildTopBar();
            BuildSubmit();
            BuildReport();
            BuildBanner();
            BuildHandover();
            BuildGameOver();
            BuildPause();
        }

        private void OnEnable()
        {
            if (_phases == null) return;
            _phases.StateChanged += Refresh;
            _phases.KingHit += ShowDamage;
            _phases.KingSquashed += ShowSquashWord;
        }

        private void OnDisable()
        {
            if (_phases == null) return;
            _phases.StateChanged -= Refresh;
            _phases.KingHit -= ShowDamage;
            _phases.KingSquashed -= ShowSquashWord;
        }

        private void Start() => Refresh();

        private void OnDestroy() => Time.timeScale = 1f;

        // ---------------- Dựng UI ----------------

        private void BuildTopBar()
        {
            Image bar = UiFactory.Panel("TopBar", _root, new Color(0.05f, 0.06f, 0.1f, 0.75f), false);
            bar.rectTransform.Place(new Vector2(0.5f, 1f), new Vector2(0f, -16f), new Vector2(1100f, 110f));
            _title = UiFactory.Label("Title", bar.transform, "", 40, TextAnchor.UpperCenter, UiFactory.Accent, FontStyle.Bold);
            _title.rectTransform.Stretch(10f);
            _hint = UiFactory.Label("Hint", bar.transform, "", 26, TextAnchor.LowerCenter);
            _hint.rectTransform.Stretch(12f);
        }

        private void BuildSubmit()
        {
            _submit = UiFactory.Button("Submit", _root, "Xác nhận", () => _phases.Submit(), 34);
            ((RectTransform)_submit.transform).Place(new Vector2(1f, 0f), new Vector2(-40f, 40f), new Vector2(300f, 90f));
        }

        private void BuildReport()
        {
            Image panel = UiFactory.Panel("BattleReport", _root, UiFactory.PanelColor, false);
            panel.rectTransform.Place(new Vector2(0f, 0.5f), new Vector2(30f, 60f), new Vector2(560f, 300f));
            _report = UiFactory.Group(panel.gameObject, false);
            UiFactory.Label("Title", panel.transform, "KẾT QUẢ TẤN CÔNG", 30, TextAnchor.UpperCenter, UiFactory.Accent, FontStyle.Bold)
                .rectTransform.Stretch(16f);
            _reportText = UiFactory.Label("Lines", panel.transform, "", 25, TextAnchor.UpperLeft);
            _reportText.rectTransform.Stretch(24f);
            _reportText.rectTransform.offsetMax = new Vector2(-24f, -64f);
        }

        private void BuildBanner()
        {
            _banner = UiFactory.Rect("PhaseBanner", _root).Place(new Vector2(0.5f, 0.5f), new Vector2(0f, 120f), new Vector2(1920f, 150f));
            Image strip = UiFactory.Panel("Strip", _banner, new Color(0f, 0f, 0f, 0.55f), false);
            strip.rectTransform.Stretch();
            _bannerText = UiFactory.Label("Text", _banner, "", 72, TextAnchor.MiddleCenter, UiFactory.Accent, FontStyle.Bold);
            _bannerText.rectTransform.Stretch();
            UiFactory.Group(_banner.gameObject, false).blocksRaycasts = false;
        }

        private void BuildHandover()
        {
            Image dim = UiFactory.Panel("Handover", _root, new Color(0.03f, 0.03f, 0.06f, 1f));
            dim.rectTransform.Stretch();
            _handover = UiFactory.Group(dim.gameObject, false);
            _handoverTitle = UiFactory.Label("Title", dim.transform, "", 80, TextAnchor.MiddleCenter, UiFactory.Accent, FontStyle.Bold);
            _handoverTitle.rectTransform.Place(new Vector2(0.5f, 0.5f), new Vector2(0f, 120f), new Vector2(1600f, 140f));
            UiFactory.Label("Sub", dim.transform, "Đưa máy cho người chơi tiếp theo. Người kia đừng nhìn nhé!", 30)
                .rectTransform.Place(new Vector2(0.5f, 0.5f), new Vector2(0f, 30f), new Vector2(1400f, 60f));
            var start = UiFactory.Button("Start", dim.transform, "Bắt đầu", () => _phases.EndHandover(), 38);
            ((RectTransform)start.transform).Place(new Vector2(0.5f, 0.5f), new Vector2(0f, -100f), new Vector2(380f, 100f));
        }

        private void BuildGameOver()
        {
            Image dim = UiFactory.Panel("GameOver", _root, new Color(0f, 0f, 0f, 0.6f));
            dim.rectTransform.Stretch();
            _gameOver = UiFactory.Group(dim.gameObject, false);
            Image win = UiFactory.Window("Window", dim.transform, new Vector2(760f, 460f));
            _gameOverTitle = UiFactory.Label("Title", win.transform, "", 76, TextAnchor.MiddleCenter, UiFactory.Accent, FontStyle.Bold);
            _gameOverTitle.rectTransform.Place(new Vector2(0.5f, 1f), new Vector2(0f, -40f), new Vector2(700f, 140f));
            var again = UiFactory.Button("Again", win.transform, "Chơi lại", SceneFader.Reload, 34);
            ((RectTransform)again.transform).Place(new Vector2(0.5f, 0f), new Vector2(0f, 150f), new Vector2(420f, 90f));
            var menu = UiFactory.Button("Menu", win.transform, "Menu chính", () => SceneFader.Load(_mainMenuScene), 34);
            ((RectTransform)menu.transform).Place(new Vector2(0.5f, 0f), new Vector2(0f, 40f), new Vector2(420f, 90f));
        }

        private void BuildPause()
        {
            Image dim = UiFactory.Panel("Pause", _root, new Color(0f, 0f, 0f, 0.6f));
            dim.rectTransform.Stretch();
            _pause = UiFactory.Group(dim.gameObject, false);
            Image win = UiFactory.Window("Window", dim.transform, new Vector2(600f, 560f));
            RectTransform list = UiFactory.Rect("List", win.transform).Stretch(50f);
            UiFactory.Vertical(list, 22f);
            UiFactory.Label("Title", list, "TẠM DỪNG", 56, TextAnchor.MiddleCenter, UiFactory.Accent, FontStyle.Bold).Height(90f);
            UiFactory.Button("Resume", list, "Tiếp tục", () => SetPaused(false)).Height(86f);
            UiFactory.Button("Settings", list, "Cài đặt", OpenSettings).Height(86f);
            UiFactory.Button("Menu", list, "Về menu chính", () => SceneFader.Load(_mainMenuScene)).Height(86f);

            _settings = new SettingsPanel(_root, () => UiFactory.Show(_pause, true));
        }

        // ---------------- Cập nhật ----------------

        private void Update()
        {
            // Esc khi đang kéo quân là để huỷ kéo (GridPlacer) -> không mở pause ở frame đó.
            bool dragging = _placer != null && _placer.IsDragging;
            if (Input.GetKeyDown(KeyCode.Escape) && !dragging && !_draggingLastFrame) TogglePause();

            if (_phases != null && _phases.CurrentPlayer != null)
            {
                _submit.interactable = _phases.CanSubmit();
                _hint.text = _phases.HintText;
            }
        }

        private void LateUpdate() => _draggingLastFrame = _placer != null && _placer.IsDragging;

        private void Refresh()
        {
            if (_phases == null || _phases.CurrentPlayer == null) return;
            Player p = _phases.CurrentPlayer;
            GamePhase phase = _phases.Current;
            bool over = phase == GamePhase.GameOver;

            _title.text = over ? "Trận đấu kết thúc" : $"{p.DisplayName} · {_phases.PhaseName}";
            _hint.text = _phases.HintText;
            _submit.SetText(_phases.SubmitLabel);
            _submit.gameObject.SetActive(!over);

            bool showReport = phase == GamePhase.End || over;
            if (showReport) _reportText.text = string.Join("\n", _phases.BattleReport);
            if (showReport != _report.gameObject.activeSelf || (showReport && _report.alpha < 1f)) UiFactory.Show(_report, showReport);

            if (_phases.IsHandingOver != _handover.gameObject.activeSelf || (_phases.IsHandingOver && _handover.alpha < 1f))
            {
                _handoverTitle.text = $"Lượt của {p.DisplayName}";
                UiFactory.Show(_handover, _phases.IsHandingOver, 0.3f);
            }

            if (over && !_gameOver.gameObject.activeSelf)
            {
                _gameOverTitle.text = $"{_phases.Winner.DisplayName} THẮNG!";
                // Đợi hiệu ứng King bị đánh văng bay mất hút rồi mới hiện.
                DOVirtual.DelayedCall(3f, () =>
                {
                    UiFactory.Show(_gameOver, true, 0.4f);
                    Sfx.Play(SfxId.Fanfare, 1f, 0f);
                }).SetTarget(this);
                _gameOver.gameObject.SetActive(true);
                _gameOver.alpha = 0f;
            }

            // Banner khi đổi phase (sau khi bỏ màn che).
            if (!_phases.IsHandingOver && !over && (phase != _lastPhase || p != _lastPlayer))
            {
                _lastPhase = phase;
                _lastPlayer = p;
                ShowBanner($"{p.DisplayName} — {_phases.PhaseName.ToUpper()}");
            }
        }

        private void ShowBanner(string text)
        {
            Sfx.Play(SfxId.Chime, 0.8f, 0f);
            _bannerText.text = text;
            var g = _banner.GetComponent<CanvasGroup>();
            g.DOKill();
            _bannerText.rectTransform.DOKill();
            _banner.gameObject.SetActive(true);
            g.alpha = 0f;
            _bannerText.rectTransform.anchoredPosition = new Vector2(-300f, 0f);

            Sequence s = DOTween.Sequence().SetTarget(g);
            s.Append(g.DOFade(1f, 0.25f));
            s.Join(_bannerText.rectTransform.DOAnchorPosX(0f, 0.35f).SetEase(Ease.OutCubic));
            s.AppendInterval(0.9f);
            s.Append(g.DOFade(0f, 0.3f));
            s.Join(_bannerText.rectTransform.DOAnchorPosX(300f, 0.3f).SetEase(Ease.InCubic));
            s.OnComplete(() => _banner.gameObject.SetActive(false));
        }

        /// <summary>Số damage bay lên từ đầu King, xuất hiện đúng lúc hiệu ứng trúng đòn.</summary>
        private void ShowDamage(GridUnit king, int damage, float delay) =>
            PopText(king, $"-{damage}", 64, new Color(1f, 0.3f, 0.25f), delay, 1.6f, 0f);

        /// <summary>Chữ kiểu truyện tranh khi King bị đè bẹp.</summary>
        private void ShowSquashWord(GridUnit king, float delay) =>
            PopText(king, "BẸP!", 96, new Color(1f, 0.86f, 0.2f), delay, 0.6f, Random.Range(-14f, 14f));

        private void PopText(GridUnit king, string text, int size, Color color, float delay, float height, float tilt)
        {
            Text t = UiFactory.Label("PopText", _root, text, size, TextAnchor.MiddleCenter, color, FontStyle.Bold);
            t.rectTransform.sizeDelta = new Vector2(400f, 160f);
            var outline = t.gameObject.AddComponent<Outline>();
            outline.effectColor = new Color(0.1f, 0.05f, 0.1f, 1f);
            outline.effectDistance = new Vector2(4f, -4f);
            t.gameObject.SetActive(false);

            DOVirtual.DelayedCall(delay, () =>
            {
                if (king == null || t == null || _placer == null) return;
                Camera cam = _placer.ActiveCam;
                Vector3 screen = cam.WorldToScreenPoint(king.transform.position + Vector3.up * height);
                if (screen.z < 0f) { Destroy(t.gameObject); return; }

                t.gameObject.SetActive(true);
                RectTransform rt = t.rectTransform;
                rt.position = screen;
                rt.localScale = Vector3.one * 0.3f;
                rt.localRotation = Quaternion.Euler(0f, 0f, tilt);
                Sequence s = DOTween.Sequence().SetTarget(rt);
                s.Append(rt.DOScale(1.25f, 0.16f).SetEase(Ease.OutBack));
                s.Append(rt.DOScale(1f, 0.1f));
                s.Join(rt.DOMoveY(screen.y + 120f, 1f).SetEase(Ease.OutCubic));
                s.Insert(0.7f, t.DOFade(0f, 0.5f));
                s.OnComplete(() => Destroy(t.gameObject));
            }).SetTarget(this);
        }

        // ---------------- Pause ----------------

        private void TogglePause()
        {
            if (_settings.Group.gameObject.activeSelf) { _settings.Close(); return; }
            if (_phases != null && _phases.Current == GamePhase.GameOver) return;
            SetPaused(!IsPaused);
        }

        private void SetPaused(bool paused)
        {
            IsPaused = paused;
            Time.timeScale = paused ? 0f : 1f;
            UiFactory.Show(_pause, paused);
        }

        private void OpenSettings()
        {
            UiFactory.Show(_pause, false);
            _settings.Open();
        }
    }
}
