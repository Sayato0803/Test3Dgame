using System.Collections.Generic;
using DG.Tweening;
using UnityEngine;
using UnityEngine.UI;

namespace GridBattle
{
    /// <summary>
    /// HUD trận đấu. UI nằm sẵn trong scene (HudCanvas) / prefab, script chỉ bật/tắt panel và gán chữ:
    /// thanh trạng thái, bảng số King, thanh máu trên đầu King, nút Xác nhận, báo cáo battle, màn che đổi người, banner phase,
    /// số damage bay lên, hiệu ứng manga khi đòn trúng, slow-mo đòn kết liễu, màn hình thắng, pause menu (Esc) + Cài đặt.
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
        [SerializeField] private CanvasGroup _banner;
        [SerializeField] private Text _bannerText;
        [SerializeField] private CanvasGroup _handover;
        [SerializeField] private Text _handoverTitle;
        [SerializeField] private Text _tapPrompt;
        [SerializeField] private CanvasGroup _gameOver;
        [SerializeField] private Text _gameOverTitle;
        [SerializeField] private RectTransform _winBadge;
        [SerializeField] private CanvasGroup _pause;
        [SerializeField] private SettingsPanel _settings;
        [SerializeField] private Image _speedLines;
        [SerializeField] private Image _koTint;
        [SerializeField] private Image _koStamp;
        [SerializeField] private RectTransform _hpRoot;

        [Header("Prefab")]
        [SerializeField] private Slider _hpBarPrefab;
        [SerializeField] private Text _damageTextPrefab;

        [Header("Thời gian (giây)")]
        [SerializeField] private float _fadeDuration = 0.2f;
        [SerializeField] private float _handoverFade = 0.3f;
        [SerializeField] private float _gameOverFade = 0.4f;
        [Tooltip("Đợi hiệu ứng King bị đánh văng bay mất hút rồi mới hiện màn thắng.")]
        [SerializeField] private float _gameOverDelay = 3f;
        [SerializeField] private float _bannerIn = 0.35f;
        [SerializeField] private float _bannerHold = 0.9f;
        [SerializeField] private float _bannerOut = 0.3f;
        [Tooltip("Banner trượt vào từ trái và ra bên phải quãng này (pixel tham chiếu).")]
        [SerializeField] private float _bannerSlide = 300f;
        [SerializeField] private float _hitstopDuration = 0.08f;
        [SerializeField] private float _hitstopScale = 0.05f;
        [SerializeField] private float _slowMoDuration = 1.4f;
        [SerializeField] private float _slowMoScale = 0.25f;

        [Header("Màu / vị trí")]
        [SerializeField] private Color _hpLow = new(0.95f, 0.25f, 0.2f);
        [SerializeField] private Color _hpHigh = new(0.35f, 0.9f, 0.3f);
        [SerializeField] private float _koTintAlpha = 0.45f;
        [Tooltip("Độ cao số damage so với chân King (world).")]
        [SerializeField] private float _damageHeight = 1.6f;

        private readonly Dictionary<GridUnit, (Slider bar, Image fill, Text label)> _hpBars = new();
        private float _slowMoUntil;   // thời gian thật: đang slow-mo kết liễu (hitstop trả về _slowMoScale thay vì 1)

        private GamePhase _lastPhase = (GamePhase)(-1);
        private Player _lastPlayer;
        private bool _draggingLastFrame;

        public bool IsPaused { get; private set; }

        private void Awake()
        {
            _settings.Closed += () => UiFactory.Show(_pause, true, _fadeDuration);
            _tapPrompt.DOFade(0.25f, 0.8f).SetLoops(-1, LoopType.Yoyo).SetUpdate(true);
            _winBadge.DOScale(1.06f, 0.5f).SetLoops(-1, LoopType.Yoyo).SetEase(Ease.InOutSine);
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
                        Slider bar = Instantiate(_hpBarPrefab, _hpRoot);
                        hb = (bar, bar.fillRect.GetComponent<Image>(), bar.GetComponentInChildren<Text>());
                        _hpBars[k] = hb;
                    }
                    float height = k.TryGetComponent(out UnitReactions r) ? r.TopHeight + 0.35f : 1.6f;
                    Vector3 screen = cam.WorldToScreenPoint(k.transform.position + Vector3.up * height);
                    show &= screen.z > 0f;
                    hb.bar.gameObject.SetActive(show);
                    if (!show) continue;
                    hb.bar.transform.position = screen;
                    float ratio = s.MaxHealth > 0 ? (float)s.Health / s.MaxHealth : 0f;
                    hb.bar.value = ratio;
                    hb.fill.color = Color.Lerp(_hpLow, _hpHigh, ratio);
                    hb.label.text = $"{s.Health}/{s.MaxHealth}";
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

            bool showReport = phase == GamePhase.End || over;
            if (showReport) _reportText.text = string.Join("\n", _phases.BattleReport);
            _score.text = $"{ScoreLine(_phases.Player1)}\n{ScoreLine(_phases.Player2)}";
            if (showReport != _report.gameObject.activeSelf || (showReport && _report.alpha < 1f)) UiFactory.Show(_report, showReport, _fadeDuration);

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
            CanvasGroup g = _banner;
            g.DOKill();
            _bannerText.rectTransform.DOKill();
            g.gameObject.SetActive(true);
            g.alpha = 0f;
            _bannerText.rectTransform.anchoredPosition = new Vector2(-_bannerSlide, 0f);

            Sequence s = DOTween.Sequence().SetTarget(g);
            s.Append(g.DOFade(1f, _bannerIn * 0.7f));
            s.Join(_bannerText.rectTransform.DOAnchorPosX(0f, _bannerIn).SetEase(Ease.OutCubic));
            s.AppendInterval(_bannerHold);
            s.Append(g.DOFade(0f, _bannerOut));
            s.Join(_bannerText.rectTransform.DOAnchorPosX(_bannerSlide, _bannerOut).SetEase(Ease.InCubic));
            s.OnComplete(() => g.gameObject.SetActive(false));
        }

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
                _banner.DOKill();
                _banner.gameObject.SetActive(false); // banner "RESULTS" không đè lên chữ K.O.
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

        public void TogglePause()
        {
            if (_settings.IsOpen) { _settings.Close(); return; }
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
