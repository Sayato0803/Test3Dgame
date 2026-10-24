using System.Collections.Generic;
using DG.Tweening;
using UnityEngine;
using UnityEngine.UI;

namespace GridBattle
{
    /// <summary>
    /// HUD trận đấu bằng uGUI (thay OnGUI cũ của Phases): thanh trạng thái, bảng số King, thanh máu trên đầu King,
    /// nút Xác nhận, báo cáo battle, màn che đổi người, banner phase, số damage bay lên, hiệu ứng manga khi đòn trúng,
    /// slow-mo đòn kết liễu, màn hình thắng, pause menu (Esc) + Cài đặt.
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
        private Image _speedLines;
        private Image _koTint;
        private Image _koStamp;
        private Text _score;
        private RectTransform _hpRoot;
        private readonly Dictionary<GridUnit, (RectTransform bar, RectTransform fill, Image fillImg, Text label)> _hpBars = new();
        private float _slowMoUntil;   // thời gian thật: đang slow-mo kết liễu (hitstop trả về 0.25 thay vì 1)

        private GamePhase _lastPhase = (GamePhase)(-1);
        private Player _lastPlayer;
        private bool _draggingLastFrame;

        public bool IsPaused { get; private set; }

        private void Awake()
        {
            Canvas canvas = UiFactory.CreateCanvas("HudCanvas", 10);
            canvas.transform.SetParent(transform, false);
            _root = (RectTransform)canvas.transform;

            BuildSpeedLines();
            _hpRoot = UiFactory.Rect("KingHpBars", _root).Stretch();
            BuildTopBar();
            BuildScoreboard();
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
            _phases.KingHit += Impact;
            _phases.FinalBlow += FinalBlowFx;
        }

        private void OnDisable()
        {
            if (_phases == null) return;
            _phases.StateChanged -= Refresh;
            _phases.KingHit -= ShowDamage;
            _phases.KingHit -= Impact;
            _phases.FinalBlow -= FinalBlowFx;
        }

        private void Start() => Refresh();

        private void OnDestroy() => Time.timeScale = 1f;

        // ---------------- Dựng UI ----------------

        /// <summary>Speed lines kiểu manga: nằm dưới mọi UI khác, chỉ bật lúc đòn trúng.</summary>
        private void BuildSpeedLines()
        {
            _speedLines = UiFactory.Panel("SpeedLines", _root, Color.white, false);
            _speedLines.sprite = Resources.Load<Sprite>("GUI/fx/speed_lines");
            _speedLines.rectTransform.Place(new Vector2(0.5f, 0.5f), Vector2.zero, new Vector2(2800f, 2800f));
            _speedLines.gameObject.SetActive(false);

            // Đòn kết liễu: phủ đỏ toàn màn hình + chữ K.O. lớn.
            _koTint = UiFactory.Panel("KoTint", _root, new Color(1f, 0.15f, 0.1f, 0f), false);
            _koTint.rectTransform.Stretch();
            _koStamp = UiFactory.Panel("KoStamp", _root, Color.white, false);
            _koStamp.sprite = Resources.Load<Sprite>("GUI/sfx_text/sfx_ko");
            _koStamp.preserveAspect = true;
            _koStamp.rectTransform.Place(new Vector2(0.5f, 0.5f), new Vector2(0f, 60f), new Vector2(900f, 560f));
            _koTint.gameObject.SetActive(false);
            _koStamp.gameObject.SetActive(false);
        }

        private void BuildTopBar()
        {
            Image bar = UiFactory.RoundPanel("TopBar", _root, UiFactory.PanelColor, false);
            bar.rectTransform.Place(new Vector2(0.5f, 1f), new Vector2(0f, -16f), new Vector2(1100f, 110f));
            _title = UiFactory.Label("Title", bar.transform, "", 40, TextAnchor.UpperCenter, UiFactory.Accent, FontStyle.Bold);
            _title.rectTransform.Stretch(10f);
            _hint = UiFactory.Label("Hint", bar.transform, "", 26, TextAnchor.LowerCenter);
            _hint.rectTransform.Stretch(12f);
        }

        /// <summary>Góc trên trái: số King còn sống của mỗi bên.</summary>
        private void BuildScoreboard()
        {
            Image panel = UiFactory.RoundPanel("Scoreboard", _root, UiFactory.PanelColor, false);
            panel.rectTransform.Place(new Vector2(0f, 1f), new Vector2(30f, -20f), new Vector2(300f, 110f));
            _score = UiFactory.Label("Score", panel.transform, "", 30, TextAnchor.MiddleCenter);
            _score.rectTransform.Stretch(12f);
        }

        private void BuildSubmit()
        {
            _submit = UiFactory.Button("Submit", _root, "CONFIRM", () => _phases.Submit(), 34).Skin("btn_blank_green", true);
            ((RectTransform)_submit.transform).Place(new Vector2(1f, 0f), new Vector2(-40f, 40f), new Vector2(300f, 90f));
            // Nút pause trên màn hình: bản mobile/web không có phím Esc.
            var pause = UiFactory.Button("PauseButton", _root, "II", TogglePause, 40).Skin("icon_pause");
            ((RectTransform)pause.transform).Place(new Vector2(1f, 1f), new Vector2(-30f, -30f), new Vector2(100f, 100f));
        }

        private void BuildReport()
        {
            Image panel = UiFactory.RoundPanel("BattleReport", _root, UiFactory.PanelColor, false);
            panel.rectTransform.Place(new Vector2(0f, 0.5f), new Vector2(30f, 60f), new Vector2(560f, 300f));
            _report = UiFactory.Group(panel.gameObject, false);
            UiFactory.Label("Title", panel.transform, "BATTLE REPORT", 30, TextAnchor.UpperCenter, UiFactory.Accent, FontStyle.Bold)
                .rectTransform.Stretch(16f);
            _reportText = UiFactory.Label("Lines", panel.transform, "", 25, TextAnchor.UpperLeft);
            _reportText.rectTransform.Stretch(24f);
            _reportText.rectTransform.offsetMax = new Vector2(-24f, -64f);
        }

        private void BuildBanner()
        {
            _banner = UiFactory.Rect("PhaseBanner", _root).Place(new Vector2(0.5f, 0.5f), new Vector2(0f, 120f), new Vector2(1920f, 150f));
            Image strip = UiFactory.RoundPanel("Strip", _banner, new Color(0.1f, 0.12f, 0.3f, 0.85f), false);
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
            UiFactory.Label("Sub", dim.transform, "Pass the device to the next player. No peeking!", 30)
                .rectTransform.Place(new Vector2(0.5f, 0.5f), new Vector2(0f, 30f), new Vector2(1400f, 60f));
            // Bấm bất kỳ đâu trên màn che để bắt đầu lượt.
            var tap = dim.gameObject.AddComponent<Button>();
            tap.transition = Selectable.Transition.None;
            tap.onClick.AddListener(() => { Sfx.Play(SfxId.UiClick); _phases.EndHandover(); });
            Text prompt = UiFactory.Label("TapToStart", dim.transform, "TAP ANYWHERE TO START", 34, TextAnchor.MiddleCenter, UiFactory.Accent);
            prompt.rectTransform.Place(new Vector2(0.5f, 0.5f), new Vector2(0f, -110f), new Vector2(1200f, 60f));
            prompt.DOFade(0.25f, 0.8f).SetLoops(-1, LoopType.Yoyo).SetUpdate(true);
        }

        private void BuildGameOver()
        {
            // Không che bàn cờ: để thấy quân của người thắng nhảy múa. Ảnh WIN + tên ở trên, nút ở dưới.
            RectTransform root = UiFactory.Rect("GameOver", _root).Stretch();
            _gameOver = UiFactory.Group(root.gameObject, false);
            Image badge = UiFactory.Panel("WinBadge", root, Color.white, false);
            badge.sprite = Resources.Load<Sprite>("GUI/sfx_text/sfx_win");
            badge.preserveAspect = true;
            badge.enabled = badge.sprite != null;
            badge.rectTransform.Place(new Vector2(0.5f, 1f), new Vector2(0f, -140f), new Vector2(420f, 280f));
            badge.rectTransform.DOScale(1.06f, 0.5f).SetLoops(-1, LoopType.Yoyo).SetEase(Ease.InOutSine);
            _gameOverTitle = UiFactory.Label("Title", root, "", 76, TextAnchor.MiddleCenter, UiFactory.Accent, FontStyle.Bold);
            _gameOverTitle.rectTransform.Place(new Vector2(0.5f, 1f), new Vector2(0f, -420f), new Vector2(1200f, 120f));
            var again = UiFactory.Button("Replay", root, "REPLAY", SceneFader.Reload, 40).Skin("btn_blank_green", true);
            ((RectTransform)again.transform).Place(new Vector2(0.5f, 0f), new Vector2(-230f, 50f), new Vector2(420f, 110f));
            var menu = UiFactory.Button("Menu", root, "MENU", () => SceneFader.Load(_mainMenuScene), 36).Skin("btn_blank_blue", true);
            ((RectTransform)menu.transform).Place(new Vector2(0.5f, 0f), new Vector2(230f, 50f), new Vector2(420f, 110f));
        }

        private void BuildPause()
        {
            Image dim = UiFactory.Panel("Pause", _root, new Color(0f, 0f, 0f, 0.6f));
            dim.rectTransform.Stretch();
            _pause = UiFactory.Group(dim.gameObject, false);
            Image win = UiFactory.Window("Window", dim.transform, new Vector2(600f, 560f));
            RectTransform list = UiFactory.Rect("List", win.transform).Stretch(50f);
            UiFactory.Vertical(list, 22f);
            UiFactory.Label("Title", list, "PAUSED", 56, TextAnchor.MiddleCenter, UiFactory.Accent, FontStyle.Bold).Height(90f);
            UiFactory.Button("Resume", list, "Resume", () => SetPaused(false)).Skin("btn_resume").Height(86f);
            UiFactory.Button("Settings", list, "SETTINGS", OpenSettings).Skin("btn_blank_blue", true).Height(86f);
            UiFactory.Button("Menu", list, "MAIN MENU", () => SceneFader.Load(_mainMenuScene)).Skin("btn_blank_orange", true).Height(86f);

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

        private void LateUpdate()
        {
            _draggingLastFrame = _placer != null && _placer.IsDragging;
            UpdateHpBars();
        }

        /// <summary>
        /// Thanh máu trên đầu King mà người đang chơi được thấy: King của mình và King đối thủ đang bị lộ.
        /// Ẩn khi đang che màn hình đổi người, King chết thì bỏ thanh.
        /// </summary>
        private void UpdateHpBars()
        {
            if (_phases == null || _placer == null || _phases.CurrentPlayer == null) return;
            Camera cam = _placer.ActiveCam;
            foreach (Player p in new[] { _phases.Player1, _phases.Player2 })
            {
                foreach (GridUnit k in p.Kings)
                {
                    if (k == null || !k.TryGetComponent(out KingStats s)) continue;
                    bool show = !_phases.IsHandingOver && !s.IsDead && (p == _phases.CurrentPlayer || s.IsRevealed);
                    if (!_hpBars.TryGetValue(k, out var hb))
                    {
                        if (!show) continue;
                        hb = CreateHpBar();
                        _hpBars[k] = hb;
                    }
                    float height = k.TryGetComponent(out UnitReactions r) ? r.TopHeight + 0.35f : 1.6f;
                    Vector3 screen = cam.WorldToScreenPoint(k.transform.position + Vector3.up * height);
                    show &= screen.z > 0f;
                    hb.bar.gameObject.SetActive(show);
                    if (!show) continue;
                    hb.bar.position = screen;
                    float ratio = s.MaxHealth > 0 ? (float)s.Health / s.MaxHealth : 0f;
                    hb.fill.anchorMax = new Vector2(ratio, 1f);
                    hb.fillImg.color = Color.Lerp(new Color(0.95f, 0.25f, 0.2f), new Color(0.35f, 0.9f, 0.3f), ratio);
                    hb.label.text = $"{s.Health}/{s.MaxHealth}";
                }
            }
        }

        private (RectTransform, RectTransform, Image, Text) CreateHpBar()
        {
            Image bg = UiFactory.RoundPanel("KingHp", _hpRoot, new Color(0.12f, 0.1f, 0.2f, 1f), false);
            bg.pixelsPerUnitMultiplier = 4f;
            bg.rectTransform.Place(new Vector2(0.5f, 0.5f), Vector2.zero, new Vector2(130f, 30f));
            RectTransform area = UiFactory.Rect("Area", bg.transform).Stretch(5f);
            Image fill = UiFactory.Panel("Fill", area, Color.green, false);
            fill.rectTransform.anchorMin = Vector2.zero;
            fill.rectTransform.anchorMax = Vector2.one;
            fill.rectTransform.offsetMin = fill.rectTransform.offsetMax = Vector2.zero;
            Text label = UiFactory.Label("Hp", bg.transform, "", 20, TextAnchor.MiddleCenter);
            label.rectTransform.Stretch();
            return (bg.rectTransform, fill.rectTransform, fill, label);
        }

        private void Refresh()
        {
            if (_phases == null || _phases.CurrentPlayer == null) return;
            Player p = _phases.CurrentPlayer;
            GamePhase phase = _phases.Current;
            bool over = phase == GamePhase.GameOver;

            _title.text = over ? "GAME OVER" : $"{p.DisplayName} · {_phases.PhaseName.ToUpper()}" +
                (phase == GamePhase.Deploy ? $" · KINGS {p.Kings.Count}/{_phases.KingsPerPlayer}" : "");
            _hint.text = _phases.HintText;
            _submit.SetText(_phases.SubmitLabel);
            _submit.gameObject.SetActive(!over);

            bool showReport = phase == GamePhase.End || over;
            if (showReport) _reportText.text = string.Join("\n", _phases.BattleReport);
            _score.text = $"{ScoreLine(_phases.Player1)}\n{ScoreLine(_phases.Player2)}";
            if (showReport != _report.gameObject.activeSelf || (showReport && _report.alpha < 1f)) UiFactory.Show(_report, showReport);

            if (_phases.IsHandingOver != _handover.gameObject.activeSelf || (_phases.IsHandingOver && _handover.alpha < 1f))
            {
                _handoverTitle.text = $"{p.DisplayName}'S TURN";
                UiFactory.Show(_handover, _phases.IsHandingOver, 0.3f);
            }

            if (over && !_gameOver.gameObject.activeSelf)
            {
                _gameOverTitle.text = $"{_phases.Winner.DisplayName} WINS!";
                // Đợi hiệu ứng King bị đánh văng bay mất hút rồi mới hiện.
                DOVirtual.DelayedCall(3f, () =>
                {
                    UiFactory.Show(_gameOver, true, 0.4f);
                    Sfx.Play(SfxId.Fanfare, 1f, 0f);
                    // Camera toàn cảnh, mọi quân còn sống của người thắng nhảy múa (lệch nhịp cho vui mắt).
                    if (_placer != null) _placer.SwitchCamera(_placer.OverviewCameraIndex);
                    Player w = _phases.Winner;
                    int i = 0;
                    foreach (GridUnit u in w.Kings)
                        if (u != null && u.TryGetComponent(out UnitReactions r)) r.PlayDance(0.12f * i++);
                    foreach (GridUnit u in w.Attackers)
                        if (u != null && u.TryGetComponent(out UnitReactions r)) r.PlayDance(0.12f * i++);
                }).SetTarget(this);
                _gameOver.gameObject.SetActive(true);
                _gameOver.alpha = 0f;
            }

            // Banner khi đổi phase (sau khi bỏ màn che).
            if (!_phases.IsHandingOver && !over && (phase != _lastPhase || p != _lastPlayer))
            {
                _lastPhase = phase;
                _lastPlayer = p;
                ShowBanner($"{p.DisplayName} — {_phases.PhaseName.ToUpper()}" +
                           (phase == GamePhase.Deploy ? $"\n<size=40>KINGS ON FIELD: {p.Kings.Count}/{_phases.KingsPerPlayer}</size>" : ""));
            }
        }

        /// <summary>"P1  KINGS 2/2": số King còn sống / số King tối đa.</summary>
        private string ScoreLine(Player p)
        {
            int alive = 0;
            foreach (GridUnit k in p.Kings)
                if (k != null && k.TryGetComponent(out KingStats s) && !s.IsDead) alive++;
            return $"{p.DisplayName}  KINGS {alive}/{_phases.KingsPerPlayer}";
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

        /// <summary>Đòn trúng kiểu manga (MHA/JoJo): speed lines dồn vào King, hitstop đứng hình, camera giật zoom.</summary>
        private void Impact(GridUnit king, int damage, float delay)
        {
            DOVirtual.DelayedCall(delay, () =>
            {
                if (king == null || _placer == null || IsPaused) return;
                Vector3 screen = _placer.ActiveCam.WorldToScreenPoint(king.transform.position + Vector3.up * 0.6f);
                if (screen.z > 0f && _speedLines.sprite != null)
                {
                    RectTransform rt = _speedLines.rectTransform;
                    _speedLines.DOKill();
                    _speedLines.gameObject.SetActive(true);
                    _speedLines.color = Color.white;
                    rt.position = screen;
                    rt.localScale = Vector3.one * 1.15f;
                    Sequence s = DOTween.Sequence().SetTarget(_speedLines).SetUpdate(true);
                    // Nháy đổi góc vài lần cho cảm giác nét vẽ tay rung lên.
                    for (int i = 0; i < 4; i++)
                        s.AppendCallback(() => rt.localRotation = Quaternion.Euler(0f, 0f, Random.Range(0f, 360f))).AppendInterval(0.045f);
                    s.Append(_speedLines.DOFade(0f, 0.2f));
                    s.Join(rt.DOScale(1.4f, 0.2f));
                    s.OnComplete(() => _speedLines.gameObject.SetActive(false));
                }
                if (CameraRig.Instance != null) CameraRig.Instance.ZoomPunch();
                // Hitstop: gần như đóng băng 0.08s thật rồi chạy tiếp.
                Time.timeScale = 0.05f;
                DOVirtual.DelayedCall(0.08f, () => { if (!IsPaused) Time.timeScale = Time.unscaledTime < _slowMoUntil ? 0.25f : 1f; }, true);
            }).SetTarget(this);
        }

        /// <summary>Đòn kết liễu: slow-mo, camera lao vào King, màn hình phủ đỏ kiểu manga + chữ K.O. lớn đập xuống.</summary>
        private void FinalBlowFx(GridUnit king, float delay)
        {
            DOVirtual.DelayedCall(Mathf.Max(0f, delay - 0.12f), () =>
            {
                if (king == null || IsPaused) return;
                const float duration = 1.4f; // giây thật
                _banner.GetComponent<CanvasGroup>().DOKill();
                _banner.gameObject.SetActive(false); // banner "RESULTS" không đè lên chữ K.O.
                _slowMoUntil = Time.unscaledTime + duration;
                Time.timeScale = 0.25f;
                if (CameraRig.Instance != null) CameraRig.Instance.Focus(king.transform.position + Vector3.up * 0.6f);

                _koTint.gameObject.SetActive(true);
                _koTint.color = new Color(1f, 0.15f, 0.1f, 0f);
                _koTint.DOKill();
                DOTween.Sequence().SetTarget(_koTint).SetUpdate(true)
                    .Append(_koTint.DOFade(0.45f, 0.08f))
                    .AppendInterval(duration - 0.4f)
                    .Append(_koTint.DOFade(0f, 0.3f))
                    .OnComplete(() => _koTint.gameObject.SetActive(false));

                RectTransform ko = _koStamp.rectTransform;
                _koStamp.gameObject.SetActive(true);
                _koStamp.color = Color.white;
                ko.localScale = Vector3.one * 3f;
                ko.localRotation = Quaternion.Euler(0f, 0f, Random.Range(-10f, 10f));
                DOTween.Sequence().SetTarget(_koStamp).SetUpdate(true)
                    .Append(ko.DOScale(1f, 0.18f).SetEase(Ease.InQuad))
                    .AppendCallback(() =>
                    {
                        Sfx.Play(SfxId.Punch, 1f, 0f);
                        if (CameraRig.Instance != null) CameraRig.Instance.Shake(0.35f, 0.3f);
                    })
                    .Append(ko.DOPunchScale(Vector3.one * 0.15f, 0.3f, 8, 0.6f))
                    .AppendInterval(duration - 0.7f)
                    .Append(_koStamp.DOFade(0f, 0.25f))
                    .OnComplete(() => _koStamp.gameObject.SetActive(false));

                DOVirtual.DelayedCall(duration, () => { if (!IsPaused) Time.timeScale = 1f; }, true);
            }).SetTarget(this);
        }

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
