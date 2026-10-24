using System.Collections.Generic;
using DG.Tweening;
using UnityEngine;
using UnityEngine.UI;

namespace GridBattle
{
    /// <summary>
    /// HUD trận đấu. UI nằm sẵn trong scene (HudCanvas) / prefab, script chỉ bật/tắt panel và gán chữ:
    /// thanh trạng thái, bảng số King, thanh máu trên đầu King, nút Xác nhận, báo cáo battle (giữ Tab), màn che đổi người,
    /// số damage bay lên, hiệu ứng manga khi đòn trúng, slow-mo đòn kết liễu, màn hình thắng, pause menu (Esc) + Cài đặt.
    /// King bị hạ: ẩn sạch GUI tới khi hoạt ảnh battle xong để thấy trọn cảnh kết liễu. F1: ẩn / hiện GUI bằng tay.
    /// Nút gọi các hàm public qua OnClick trong Inspector.
    /// </summary>
    public class GameHud : MonoBehaviour
    {
        [SerializeField] private Phases _phases;
        [SerializeField] private GridPlacer _placer;
        [SerializeField] private string _mainMenuScene = "MainMenu";

        [Header("UI trong scene")]
        [SerializeField] private RectTransform _root;
        [SerializeField] private Text _title;
        [SerializeField] private Text _hint;
        [SerializeField] private Button _submit;
        [SerializeField] private Text _score;
        [SerializeField] private CanvasGroup _report;
        [SerializeField] private Text _reportText;
        [SerializeField] private CanvasGroup _handover;
        [SerializeField] private Text _handoverTitle;
        [SerializeField] private Text _tapPrompt;
        [SerializeField] private CanvasGroup _gameOver;
        [SerializeField] private Text _gameOverTitle;
        [SerializeField] private RectTransform _winBadge;
        [SerializeField] private CanvasGroup _pause;
        [SerializeField] private SettingsPanel _settings;
        [SerializeField] private TutorialPanel _tutorial;
        [SerializeField] private Image _speedLines;
        [SerializeField] private Image _koTint;
        [SerializeField] private Image _koStamp;
        [SerializeField] private RectTransform _hpRoot;

        [Tooltip("Các canvas bị tắt khi bấm F1 và trong cảnh kết liễu (HUD + tay bài).")]
        [SerializeField] private Canvas[] _hideOnF1;
        [Tooltip("Tắt ở màn thắng (thanh trên chỉ còn chữ GAME OVER, nút pause không dùng được khi hết trận).")]
        [SerializeField] private GameObject[] _hideOnGameOver;

        [Header("Prefab")]
        [SerializeField] private Slider _hpBarPrefab;
        [SerializeField] private Text _damageTextPrefab;

        [Tooltip("Giữ Tab: Battle Report hiện chừng này dòng mới nhất.")]
        [SerializeField] private int _reportLines = 8;

        [Header("Thời gian (giây)")]
        [SerializeField] private float _fadeDuration = 0.2f;
        [SerializeField] private float _handoverFade = 0.3f;
        [SerializeField] private float _gameOverFade = 0.4f;
        [Tooltip("Đợi hiệu ứng King bị đánh văng bay mất hút rồi mới hiện màn thắng.")]
        [SerializeField] private float _gameOverDelay = 3f;
        [SerializeField] private float _hitstopDuration = 0.08f;
        [SerializeField] private float _hitstopScale = 0.05f;
        [SerializeField] private float _slowMoDuration = 1.4f;
        [SerializeField] private float _slowMoScale = 0.25f;

        [Header("Thanh máu King")]
        [Tooltip("Vệt vàng (con Area/Trail của prefab KingHpBar) đứng yên chừng này giây sau khi máu tụt rồi mới trượt theo.")]
        [SerializeField] private float _hpTrailDelay = 0.45f;
        [Tooltip("Tốc độ vệt vàng trượt về máu thật (tỉ lệ thanh / giây).")]
        [SerializeField] private float _hpTrailSpeed = 0.5f;

        [Header("Màu / vị trí")]
        [SerializeField] private Color _hpLow = new(0.95f, 0.25f, 0.2f);
        [SerializeField] private Color _hpHigh = new(0.35f, 0.9f, 0.3f);
        [SerializeField] private float _koTintAlpha = 0.45f;
        [Tooltip("Độ cao số damage so với chân King (world).")]
        [SerializeField] private float _damageHeight = 1.6f;

        private class HpBar
        {
            public Slider Bar;
            public Image Fill;
            public Text Label;
            public RectTransform Trail;
            public float TrailValue = 1f;
            public float TrailHoldUntil;
        }

        private readonly Dictionary<GridUnit, HpBar> _hpBars = new();
        // Máu đã trừ ngay khi Confirm, nhưng thanh máu chỉ tụt khi đòn chạm King: (lúc chạm, damage).
        private readonly Dictionary<GridUnit, (float at, int damage)> _pendingHits = new();
        private float _slowMoUntil;   // thời gian thật: đang slow-mo kết liễu (hitstop trả về _slowMoScale thay vì 1)

        private GamePhase _lastPhase = (GamePhase)(-1);
        private Player _lastPlayer;
        private bool _draggingLastFrame;
        private bool _hudHidden;     // F1
        private bool _cutscene;      // đang chạy cảnh kết liễu
        private bool _reportShown;   // đang giữ Tab

        public bool IsPaused { get; private set; }

        private void Awake()
        {
            _settings.Closed += () => UiFactory.Show(_pause, true, _fadeDuration);
            _tutorial.Closed += () => UiFactory.Show(_pause, true, _fadeDuration);
            _tapPrompt.DOFade(0.25f, 0.8f).SetLoops(-1, LoopType.Yoyo).SetUpdate(true);
            _winBadge.DOScale(1.06f, 0.5f).SetLoops(-1, LoopType.Yoyo).SetEase(Ease.InOutSine);
        }

        private void OnEnable()
        {
            if (_phases == null) return;
            _phases.StateChanged += Refresh;
            _phases.KingHit += ShowDamage;
            _phases.KingHit += HoldHpUntilHit;
            _phases.KingHit += Impact;
            _phases.KingHit += HideForFinisher;
            _phases.FinalBlow += FinalBlowFx;
        }

        private void OnDisable()
        {
            if (_phases == null) return;
            _phases.StateChanged -= Refresh;
            _phases.KingHit -= ShowDamage;
            _phases.KingHit -= HoldHpUntilHit;
            _phases.KingHit -= Impact;
            _phases.KingHit -= HideForFinisher;
            _phases.FinalBlow -= FinalBlowFx;
        }

        private void Start() => Refresh();

        private void OnDestroy() => Time.timeScale = 1f;

        // ---------------- Nút (OnClick trong Inspector) ----------------

        public void Submit() => _phases.Submit();

        /// <summary>Bấm bất kỳ đâu trên màn che để bắt đầu lượt.</summary>
        public void EndHandover()
        {
            Sfx.Play(SfxId.UiClick);
            _phases.EndHandover();
        }

        public void Resume() => SetPaused(false);
        public void Replay() => SceneFader.Reload();
        public void GoToMainMenu() => SceneFader.Load(_mainMenuScene);

        public void OpenSettings()
        {
            UiFactory.Show(_pause, false, _fadeDuration);
            _settings.Open();
        }

        public void OpenTutorial()
        {
            UiFactory.Show(_pause, false, _fadeDuration);
            _tutorial.Open();
        }

        // ---------------- Cập nhật ----------------

        private void Update()
        {
            // Esc khi đang kéo quân là để huỷ kéo (GridPlacer) -> không mở pause ở frame đó.
            bool dragging = _placer != null && _placer.IsDragging;
            if (Input.GetKeyDown(KeyCode.Escape) && !dragging && !_draggingLastFrame) TogglePause();
            if (Input.GetKeyDown(KeyCode.F1) && !_phases.IsHandingOver) ToggleHud();
            // Enter = bấm nút Confirm / End Turn (cùng điều kiện, cùng tiếng click như chuột).
            if ((Input.GetKeyDown(KeyCode.Return) || Input.GetKeyDown(KeyCode.KeypadEnter)) && !IsPaused
                && _submit.gameObject.activeInHierarchy && _submit.interactable) _submit.onClick.Invoke();
            if (_cutscene && !_phases.IsAnimating)
            {
                _cutscene = false;
                ApplyHudVisibility();
            }

            if (_phases != null && _phases.CurrentPlayer != null)
            {
                _submit.interactable = _phases.CanSubmit();
                _hint.text = _phases.HintText;
            }

            // Giữ Tab: xem lịch sử Battle Report (các dòng mới nhất), nhả ra thì ẩn.
            bool showReport = Input.GetKey(KeyCode.Tab) && _phases != null && !_phases.IsHandingOver && !IsPaused;
            if (showReport != _reportShown)
            {
                _reportShown = showReport;
                if (showReport) _reportText.text = ReportText();
                UiFactory.Show(_report, showReport, _fadeDuration);
            }
        }

        private string ReportText()
        {
            IReadOnlyList<string> lines = _phases.BattleReport;
            if (lines.Count == 0) return "No battles yet.";
            var last = new List<string>();
            for (int i = Mathf.Max(0, lines.Count - _reportLines); i < lines.Count; i++) last.Add(lines[i]);
            return string.Join("\n", last);
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
                    int hp = s.Health;
                    if (_pendingHits.TryGetValue(k, out var hit) && Time.time < hit.at) hp += hit.damage;
                    float ratio = s.MaxHealth > 0 ? (float)hp / s.MaxHealth : 0f;
                    if (!_hpBars.TryGetValue(k, out HpBar hb))
                    {
                        if (!show) continue;
                        Slider bar = Instantiate(_hpBarPrefab, _hpRoot);
                        hb = new HpBar
                        {
                            Bar = bar, Fill = bar.fillRect.GetComponent<Image>(), Label = bar.GetComponentInChildren<Text>(),
                            Trail = (RectTransform)bar.transform.Find("Area/Trail"), TrailValue = ratio
                        };
                        bar.value = ratio;
                        _hpBars[k] = hb;
                    }
                    float height = k.TryGetComponent(out UnitReactions r) ? r.TopHeight + 0.35f : 1.6f;
                    Vector3 screen = cam.WorldToScreenPoint(k.transform.position + Vector3.up * height);
                    show &= screen.z > 0f;
                    hb.Bar.gameObject.SetActive(show);
                    if (!show) continue;
                    hb.Bar.transform.position = screen;
                    if (ratio < hb.Bar.value) hb.TrailHoldUntil = Time.time + _hpTrailDelay; // vừa mất máu: vệt vàng chờ 1 nhịp
                    hb.Bar.value = ratio;
                    hb.Fill.color = Color.Lerp(_hpLow, _hpHigh, ratio);
                    hb.Label.text = $"{hp}/{s.MaxHealth}";
                    if (hb.Trail == null) continue;
                    if (hb.TrailValue < ratio) hb.TrailValue = ratio;
                    else if (Time.time >= hb.TrailHoldUntil) hb.TrailValue = Mathf.MoveTowards(hb.TrailValue, ratio, _hpTrailSpeed * Time.deltaTime);
                    hb.Trail.anchorMax = new Vector2(hb.TrailValue, 1f);
                }
            }
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
            foreach (GameObject go in _hideOnGameOver) if (go != null) go.SetActive(!over);

            _score.text = $"{ScoreLine(_phases.Player1)}\n{ScoreLine(_phases.Player2)}";

            // Màn che đổi người nằm trong HUD: đang ẩn HUD (F1) thì màn che vô hình, không bấm qua được -> kẹt.
            if (_phases.IsHandingOver && _hudHidden)
            {
                _hudHidden = false;
                ApplyHudVisibility();
            }

            if (_phases.IsHandingOver != _handover.gameObject.activeSelf || (_phases.IsHandingOver && _handover.alpha < 1f))
            {
                _handoverTitle.text = $"{p.DisplayName}'S TURN";
                UiFactory.Show(_handover, _phases.IsHandingOver, _handoverFade);
            }

            if (over && !_gameOver.gameObject.activeSelf)
            {
                _gameOverTitle.text = $"{_phases.Winner.DisplayName} WINS!";
                DOVirtual.DelayedCall(_gameOverDelay, () =>
                {
                    UiFactory.Show(_gameOver, true, _gameOverFade);
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

            // Tiếng chuông khi đổi phase (sau khi bỏ màn che).
            if (!_phases.IsHandingOver && !over && (phase != _lastPhase || p != _lastPlayer))
            {
                _lastPhase = phase;
                _lastPlayer = p;
                Sfx.Play(SfxId.Chime, 0.8f, 0f);
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

        private void HoldHpUntilHit(GridUnit king, int damage, float delay) => _pendingHits[king] = (Time.time + delay, damage);

        /// <summary>Số damage bay lên từ đầu King, xuất hiện đúng lúc hiệu ứng trúng đòn.</summary>
        private void ShowDamage(GridUnit king, int damage, float delay)
        {
            Text t = Instantiate(_damageTextPrefab, _root);
            t.text = $"-{damage}";
            t.gameObject.SetActive(false);

            DOVirtual.DelayedCall(delay, () =>
            {
                if (king == null || t == null || _placer == null) return;
                Camera cam = _placer.ActiveCam;
                Vector3 screen = cam.WorldToScreenPoint(king.transform.position + Vector3.up * _damageHeight);
                if (screen.z < 0f) { Destroy(t.gameObject); return; }

                t.gameObject.SetActive(true);
                RectTransform rt = t.rectTransform;
                rt.position = screen;
                rt.localScale = Vector3.one * 0.3f;
                Sequence s = DOTween.Sequence().SetTarget(rt);
                s.Append(rt.DOScale(1.25f, 0.16f).SetEase(Ease.OutBack));
                s.Append(rt.DOScale(1f, 0.1f));
                s.Join(rt.DOMoveY(screen.y + 120f, 1f).SetEase(Ease.OutCubic));
                s.Insert(0.7f, t.DOFade(0f, 0.5f));
                s.OnComplete(() => Destroy(t.gameObject));
            }).SetTarget(this);
        }

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
                // Hitstop: gần như đóng băng một chút thời gian thật rồi chạy tiếp.
                Time.timeScale = _hitstopScale;
                DOVirtual.DelayedCall(_hitstopDuration, () => { if (!IsPaused) Time.timeScale = Time.unscaledTime < _slowMoUntil ? _slowMoScale : 1f; }, true);
            }).SetTarget(this);
        }

        /// <summary>Đòn kết liễu: slow-mo, camera lao vào King, màn hình phủ đỏ kiểu manga + chữ K.O. lớn đập xuống.</summary>
        private void FinalBlowFx(GridUnit king, float delay)
        {
            DOVirtual.DelayedCall(Mathf.Max(0f, delay - 0.12f), () =>
            {
                if (king == null || IsPaused) return;
                float duration = _slowMoDuration; // giây thật
                _slowMoUntil = Time.unscaledTime + duration;
                Time.timeScale = _slowMoScale;
                if (CameraRig.Instance != null) CameraRig.Instance.Focus(king.transform.position + Vector3.up * 0.6f);

                _koTint.gameObject.SetActive(true);
                Color tint = _koTint.color;
                tint.a = 0f;
                _koTint.color = tint;
                _koTint.DOKill();
                DOTween.Sequence().SetTarget(_koTint).SetUpdate(true)
                    .Append(_koTint.DOFade(_koTintAlpha, 0.08f))
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

        // ---------------- Pause ----------------

        public void ToggleHud()
        {
            _hudHidden = !_hudHidden;
            ApplyHudVisibility();
        }

        /// <summary>King bị hạ: ẩn GUI ngay từ lúc quân ra đòn; Update hiện lại khi hoạt ảnh battle xong.</summary>
        private void HideForFinisher(GridUnit king, int damage, float delay)
        {
            if (!king.TryGetComponent(out KingStats s) || !s.IsDead) return;
            _cutscene = true;
            ApplyHudVisibility();
        }

        private void ApplyHudVisibility()
        {
            foreach (Canvas c in _hideOnF1)
                if (c != null) c.enabled = !_hudHidden && !_cutscene;
        }

        public void TogglePause()
        {
            if (_hudHidden || _cutscene)
            {
                // Menu pause nằm trong HUD: pause giữa cảnh kết liễu thì hiện GUI lại luôn.
                _hudHidden = _cutscene = false;
                ApplyHudVisibility();
            }
            if (_settings.IsOpen) { _settings.Close(); return; }
            if (_tutorial.IsOpen) { _tutorial.Close(); return; }
            if (_phases != null && _phases.Current == GamePhase.GameOver) return;
            SetPaused(!IsPaused);
        }

        private void SetPaused(bool paused)
        {
            IsPaused = paused;
            Time.timeScale = paused ? 0f : 1f;
            UiFactory.Show(_pause, paused, _fadeDuration);
        }
    }
}
