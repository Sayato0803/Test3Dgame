using System;
using System.Collections.Generic;
using DG.Tweening;
using UnityEngine;

namespace GridBattle
{
    /// <summary>GameOver dùng ở bước 2 (khi có người thắng).</summary>
    public enum GamePhase { Deploy, Setup, Attack, End, GameOver }

    /// <summary>
    /// State machine của trận: giữ phase + player hiện tại, quyết định luật (C1) và chuyển phase khi Submit.
    /// Luồng: Deploy P1 -> Deploy P2 -> Attack(người đi trước) -> End -> Setup(đối thủ) -> Attack -> End -> ...
    /// </summary>
    public class Phases : MonoBehaviour
    {
        [SerializeField] private Player _player1;
        [SerializeField] private Player _player2;
        [Tooltip("Người đi trước (tạm chỉnh tay). Để trống = P1.")]
        [SerializeField] private Player _firstPlayer;
        [SerializeField] private GridPlacer _placer;

        [Header("Luật")]
        [SerializeField] private int _kingsPerPlayer = 2;
        [Tooltip("Tối đa mỗi loại; không bắt buộc đặt đủ.")]
        [SerializeField] private int _knightsPerPlayer = 2;
        [SerializeField] private int _magesPerPlayer = 2;

        [Header("Nhịp hoạt ảnh battle (giây)")]
        [Tooltip("Từ lúc Confirm tới lúc đòn đánh thường chạm King.")]
        [SerializeField] private float _attackAnimTime = 0.3f;
        [Tooltip("Mỗi King trúng đòn sau lệch thêm chừng này.")]
        [SerializeField] private float _hitStagger = 0.25f;
        [SerializeField] private float _squashStagger = 0.35f;
        [Tooltip("King chết: khoá thao tác thêm chừng này sau cú trúng (đủ cho clip KnockedOff / bắn lên trời).")]
        [SerializeField] private float _deathBusy = 2.9f;

        public GamePhase Current { get; private set; } = GamePhase.Deploy;
        public Player CurrentPlayer { get; private set; }

        /// <summary>Đang hiện màn che đổi người: chặn mọi thao tác.</summary>
        public bool IsHandingOver { get; private set; }

        /// <summary>Phase / người chơi / số quân / kết quả battle thay đổi -> HUD và tay bài cập nhật.</summary>
        public event Action StateChanged;
        /// <summary>1 King trúng đòn (King, damage, delay giây trước khi hiệu ứng trúng đòn chạy) -> HUD hiện số damage.</summary>
        public event Action<GridUnit, int, float> KingHit;
        /// <summary>Đòn hạ King cuối cùng của đối thủ (King, delay tới lúc trúng) -> HUD chạy slow-mo kết liễu.</summary>
        public event Action<GridUnit, float> FinalBlow;

        /// <summary>Đang chạy hoạt ảnh battle (đè bẹp, King bị đánh văng): tạm khoá thao tác và Submit.</summary>
        public bool IsAnimating => Time.time < _busyUntil;
        private float _busyUntil;

        private readonly List<string> _battleReport = new();
        private Player _winner;
        private int _pendingCamera = -1;

        public IReadOnlyList<string> BattleReport => _battleReport;
        public Player Winner => _winner;
        public Player Player1 => _player1;
        public Player Player2 => _player2;
        public int KingsPerPlayer => _kingsPerPlayer;

        /// <summary>Người chơi đã dùng thẻ Trinh sát (mỗi trận 1 lần).</summary>
        private readonly HashSet<Player> _scoutUsed = new();

        private void Start()
        {
            if (_firstPlayer == null) _firstPlayer = _player1;

            // Observer: quân nào được đặt lần đầu thì ghi vào player đang tới lượt.
            _player1.Board.OnUnitPlaced += HandleUnitPlaced;
            _player2.Board.OnUnitPlaced += HandleUnitPlaced;

            if (RenderSettings.sun != null) RenderSettings.sun.gameObject.AddComponent<SkyCycle>().Bind(this);
            EnterPhase(GamePhase.Deploy, _player1);
        }

        private void OnDestroy()
        {
            if (_player1 != null && _player1.Board != null) _player1.Board.OnUnitPlaced -= HandleUnitPlaced;
            if (_player2 != null && _player2.Board != null) _player2.Board.OnUnitPlaced -= HandleUnitPlaced;
        }

        // ---------------- Chuyển phase ----------------

        private void EnterPhase(GamePhase phase, Player player)
        {
            if (player != CurrentPlayer) IsHandingOver = true; // đổi người -> che màn hình trước
            Current = phase;
            CurrentPlayer = player;
            bool lookAtOpponent = phase == GamePhase.Attack || phase == GamePhase.End;
            int cam = lookAtOpponent ? player.AttackCameraIndex : player.CameraIndex;
            // Đang che màn hình: camera bay sau khi người chơi mới bấm bắt đầu (để họ thấy camera lượn sang góc của mình).
            if (IsHandingOver) _pendingCamera = cam;
            else if (_placer != null) _placer.SwitchCamera(cam);
            RefreshKingVisibility();
            Debug.Log($"[Phase] {player.DisplayName} - {phase}");
            StateChanged?.Invoke();
        }

        /// <summary>Người chơi mới bấm "bắt đầu" trên màn che.</summary>
        public void EndHandover()
        {
            if (!IsHandingOver) return;
            IsHandingOver = false;
            if (_pendingCamera >= 0 && _placer != null) _placer.SwitchCamera(_pendingCamera);
            _pendingCamera = -1;
            StateChanged?.Invoke();
        }

        public void Submit()
        {
            if (!CanSubmit()) return;

            switch (Current)
            {
                case GamePhase.Deploy:
                    if (CurrentPlayer == _player1) EnterPhase(GamePhase.Deploy, _player2);
                    else EnterPhase(GamePhase.Attack, _firstPlayer);
                    break;
                case GamePhase.Setup:
                    EnterPhase(GamePhase.Attack, CurrentPlayer);
                    break;
                case GamePhase.Attack:
                    EnterPhase(GamePhase.End, CurrentPlayer);
                    ResolveBattle();
                    break;
                case GamePhase.End:
                    HideAllKings(); // King chỉ lộ trong lượt bị đánh; sang lượt mới thì ẩn lại dù đứng yên
                    EnterPhase(GamePhase.Setup, CurrentPlayer.Opponent);
                    break;
            }
        }

        /// <summary>
        /// Với mỗi King còn sống của đối thủ: +1 cho mỗi attacker có tầm phủ trúng (chồng tầm cộng dồn),
        /// +1 nếu có attacker đứng đúng ô King - attacker đó ĐÈ BẸP King rồi nảy sang 1 ô trống trong vùng 3x3.
        /// Damage tính theo vị trí người chơi đã đặt (khớp preview), sau đó mới chạy hoạt ảnh + dời quân nảy.
        /// Trúng -> lộ. Đối thủ hết King -> GameOver.
        /// </summary>
        private void ResolveBattle()
        {
            Player attacker = CurrentPlayer;
            Player defender = attacker.Opponent;
            _battleReport.Clear();
            var hitters = new HashSet<GridUnit>();
            var results = new List<(GridUnit king, KingStats stats, int damage, GridUnit squasher, List<GridUnit> rangeHitters)>();

            // 1. Tính damage cho mọi King trước (chưa dời quân nào).
            foreach (GridUnit king in defender.Kings)
            {
                var stats = king.GetComponent<KingStats>();
                if (stats == null || stats.IsDead) continue;

                int damage = 0;
                GridUnit squasher = null;
                var rangeHitters = new List<GridUnit>();
                foreach (GridUnit a in attacker.Attackers)
                {
                    if (a.Grid != defender.Board) continue;
                    if (AttackRange.GetCells(a.Grid, a.Coord, a.transform, a.Range).Contains(king.Coord))
                    {
                        damage++;
                        hitters.Add(a);
                        rangeHitters.Add(a);
                    }
                    if (a.Coord == king.Coord) squasher = a;
                }
                if (squasher != null) damage++;
                if (damage == 0) continue;

                stats.TakeDamage(damage);
                stats.Reveal(king.Coord);
                results.Add((king, stats, damage, squasher, rangeHitters));
                _battleReport.Add($"King at {king.Coord} " + (squasher != null ? "SQUASHED, " : "") +
                                  $"took {damage} dmg, {stats.Health}/{stats.MaxHealth} HP left" + (stats.IsDead ? " - DEFEATED" : ""));
            }

            // 2. Hoạt ảnh. King chết vì đòn từ xa: quân đánh trúng gần King nhất ra đòn kết liễu (clip PowerSlash / ZapCharge),
            // mỗi quân kết liễu tối đa 1 King. Các quân khác vẫn đánh nhưng không hiện chữ manga.
            // Attacker đè bẹp không lao lên đánh (nó rơi từ trên xuống); đè chết thì King bắn lên trời.
            var finishers = new Dictionary<GridUnit, GridUnit>(); // King -> quân kết liễu
            foreach (var r in results)
                if (r.stats.IsDead && r.squasher == null)
                {
                    GridUnit f = Nearest(r.king, r.rangeHitters, finishers.Values);
                    if (f != null) finishers[r.king] = f;
                }
            var squashers = new HashSet<GridUnit>();
            foreach (var r in results) if (r.squasher != null) squashers.Add(r.squasher);
            int order = 0;
            foreach (GridUnit a in attacker.Attackers)
                if (a.Grid == defender.Board && !squashers.Contains(a) && !finishers.ContainsValue(a) && a.TryGetComponent(out UnitReactions ar))
                    ar.PlayAttack(hitters.Contains(a) ? 0f : 0.05f * ++order);

            float busy = results.Count > 0 ? 1f : 0.5f;
            int hitIndex = 0;
            GridUnit lastKill = null;
            float lastKillTime = 0f;
            foreach (var r in results)
            {
                var kr = r.king.GetComponent<UnitReactions>();
                bool dead = r.stats.IsDead;
                finishers.TryGetValue(r.king, out GridUnit finisher);
                float strike = _attackAnimTime + hitIndex * _hitStagger;
                PlayStrikeFx(r.king, r.rangeHitters, strike, finisher, !dead);
                if (r.squasher != null)
                {
                    float impact = Squash(r.squasher, r.king, kr, dead, hitIndex * _squashStagger);
                    busy = Mathf.Max(busy, impact + 1.6f);
                    strike = impact + 0.05f;
                }
                else if (finisher != null && kr != null && finisher.TryGetComponent(out UnitReactions fr))
                {
                    strike = fr.PlayFinisher(kr, hitIndex * _hitStagger);
                }
                else if (kr != null)
                {
                    if (dead)
                    {
                        // Mọi quân trúng đã kết liễu King khác: King vẫn văng theo hướng quân gần nhất, không có đòn riêng.
                        Vector3 d = r.king.transform.position - Nearest(r.king, r.rangeHitters, null).transform.position;
                        DOVirtual.DelayedCall(strike, () => kr.PlayKnockedOff(new Vector3(d.x, 0f, d.z).normalized));
                    }
                    else kr.PlayHit(strike);
                }
                KingHit?.Invoke(r.king, r.damage, strike);
                if (dead)
                {
                    busy = Mathf.Max(busy, strike + _deathBusy);
                    if (lastKill == null || strike >= lastKillTime) { lastKill = r.king; lastKillTime = strike; }
                }
                hitIndex++;
            }
            _busyUntil = Time.time + busy;

            if (_battleReport.Count == 0) _battleReport.Add("No King was hit.");
            foreach (string line in _battleReport) Debug.Log($"[Battle] {attacker.DisplayName}: {line}");

            if (AllKingsDead(defender))
            {
                _winner = attacker;
                Current = GamePhase.GameOver;
                Debug.Log($"[Phase] {attacker.DisplayName} thắng");
                if (lastKill != null) FinalBlow?.Invoke(lastKill, lastKillTime);
            }
            RefreshKingVisibility();
            StateChanged?.Invoke();
        }

        /// <summary>Quân trong 'units' gần King nhất, bỏ qua các quân trong 'exclude'. Null nếu không còn quân nào.</summary>
        private static GridUnit Nearest(GridUnit king, List<GridUnit> units, IEnumerable<GridUnit> exclude)
        {
            var skip = exclude != null ? new HashSet<GridUnit>(exclude) : null;
            GridUnit best = null;
            float bestDist = float.MaxValue;
            foreach (GridUnit u in units)
            {
                if (skip != null && skip.Contains(u)) continue;
                float dist = (u.transform.position - king.transform.position).sqrMagnitude;
                if (dist < bestDist) { bestDist = dist; best = u; }
            }
            return best;
        }

        /// <summary>
        /// Attacker đè bẹp King rồi nảy sang 1 ô trống trong vùng 3x3 (không có attacker khác, không có King còn sống,
        /// để lượt sau không tự đè tiếp). Không còn ô thì đứng lại. Trả thời điểm va chạm (giây) kể từ bây giờ.
        /// </summary>
        private float Squash(GridUnit unit, GridUnit king, UnitReactions kr, bool lethal, float delay)
        {
            GridSystem grid = unit.Grid;
            Vector3 kingFeet = king.transform.position;
            Vector2Int? target = PickBounceCell(grid, king.Coord);
            if (target.HasValue) grid.Place(unit, target.Value); // gốc transform tới ô mới ngay, hoạt ảnh bay từ đầu King sang

            var ar = unit.GetComponent<UnitReactions>();
            float kingHeight = kr != null ? kr.TopHeight : 1f;
            float impact = ar != null ? ar.PlaySquashAndBounce(kingFeet, kingHeight, delay) : delay;
            if (kr != null) kr.PlaySquashed(impact, lethal);
            return impact;
        }

        /// <summary>
        /// Hiệu ứng đòn đánh theo tầm, chạm King đúng lúc 'strike': Mage bắn tia phép từ ngọc trên gậy,
        /// Knight để lại vệt kiếm chém ngang qua King. comic = false: không hiện chữ manga (lượt King chết).
        /// </summary>
        private static void PlayStrikeFx(GridUnit king, List<GridUnit> hitters, float strike, GridUnit finisher, bool comic)
        {
            Vector3 body = king.transform.position + Vector3.up * 0.55f;
            foreach (GridUnit a in hitters)
            {
                if (a == finisher) continue; // quân kết liễu tự chạy hiệu ứng trong clip
                if (a.Kind == UnitKind.Mage)
                {
                    const float travel = 0.28f;
                    Vector3 from = a.transform.position + Vector3.up * 0.9f;
                    CartoonFx.MagicBolt(from, body, Mathf.Max(0f, strike - travel), travel, comic);
                }
                else if (a.Kind == UnitKind.Knight)
                {
                    CartoonFx.Slash(body, king.transform.position - a.transform.position, strike - 0.04f, comic);
                }
            }
        }

        private static Vector2Int? PickBounceCell(GridSystem grid, Vector2Int center)
        {
            var options = new List<Vector2Int>();
            for (int dx = -1; dx <= 1; dx++)
                for (int dy = -1; dy <= 1; dy++)
                {
                    if (dx == 0 && dy == 0) continue;
                    var c = center + new Vector2Int(dx, dy);
                    Cell cell = grid.GetCell(c);
                    if (cell == null || cell.Attacker != null) continue;
                    if (cell.King != null && cell.King.TryGetComponent(out KingStats ks) && !ks.IsDead) continue;
                    options.Add(c);
                }
            if (options.Count == 0) return null;
            return options[UnityEngine.Random.Range(0, options.Count)];
        }

        /// <summary>Hết lượt: mọi King còn sống ẩn lại (đối thủ không còn thấy, chủ quân thấy King xoay người biến mất).</summary>
        private void HideAllKings()
        {
            foreach (Player p in new[] { _player1, _player2 })
                foreach (GridUnit k in p.Kings)
                    if (k.TryGetComponent(out KingStats s) && !s.IsDead) s.IsRevealed = false;
        }

        private static bool AllKingsDead(Player p)
        {
            if (p.Kings.Count == 0) return false;
            foreach (GridUnit k in p.Kings)
            {
                var s = k.GetComponent<KingStats>();
                if (s == null || !s.IsDead) return false;
            }
            return true;
        }

        /// <summary>
        /// Hot-seat: King chỉ hiện với chủ quân. King của đối thủ hiện khi đã lộ hoặc đã chết; hết game thì hiện tất cả.
        /// </summary>
        private void RefreshKingVisibility()
        {
            foreach (Player p in new[] { _player1, _player2 })
            {
                foreach (GridUnit k in p.Kings)
                {
                    var s = k.GetComponent<KingStats>();
                    if (s == null) continue;
                    bool visible = Current == GamePhase.GameOver || p == CurrentPlayer || s.IsRevealed || s.IsDead;
                    s.SetVisible(visible);
                    // King đang ẩn: chui xuống bàn (chủ quân chỉ thấy vương miện + mắt ló lên). Lộ / chết / hết game: đứng lên.
                    if (k.TryGetComponent(out UnitReactions r))
                        r.SetBurrowed(!s.IsRevealed && !s.IsDead && Current != GamePhase.GameOver);
                }
            }
        }

        public bool CanSubmit()
        {
            if (IsHandingOver || CurrentPlayer == null || IsAnimating) return false;
            if (_placer != null && _placer.IsDragging) return false;

            switch (Current)
            {
                case GamePhase.Deploy:
                    return CurrentPlayer.Kings.Count == _kingsPerPlayer;
                case GamePhase.Attack:
                case GamePhase.Setup:
                case GamePhase.End:
                    return true;
                default:
                    return false;
            }
        }

        // ---------------- Luật cho GridPlacer (C1) ----------------

        public bool CanSpawn(UnitKind kind)
        {
            if (IsHandingOver || CurrentPlayer == null || IsAnimating) return false;

            switch (Current)
            {
                case GamePhase.Deploy:
                    return kind == UnitKind.King && CurrentPlayer.Kings.Count < _kingsPerPlayer;
                case GamePhase.Attack:
                    if (kind == UnitKind.Knight) return CurrentPlayer.CountAttackers(UnitKind.Knight) < _knightsPerPlayer;
                    if (kind == UnitKind.Mage) return CurrentPlayer.CountAttackers(UnitKind.Mage) < _magesPerPlayer;
                    return false;
                default:
                    return false;
            }
        }

        public bool CanPickUp(GridUnit unit)
        {
            if (IsHandingOver || IsAnimating || unit.Owner != CurrentPlayer) return false;

            switch (Current)
            {
                case GamePhase.Deploy:
                case GamePhase.Setup:
                    return unit.Kind == UnitKind.King;
                case GamePhase.Attack:
                    return unit.Kind != UnitKind.King;
                default:
                    return false;
            }
        }

        /// <summary>King thả lên bàn mình, attacker thả lên bàn đối thủ.</summary>
        public bool CanDropOn(GridSystem grid)
        {
            if (IsHandingOver || CurrentPlayer == null) return false;

            switch (Current)
            {
                case GamePhase.Deploy:
                case GamePhase.Setup:
                    return grid == CurrentPlayer.Board;
                case GamePhase.Attack:
                    return grid == CurrentPlayer.Opponent.Board;
                default:
                    return false;
            }
        }

        private void HandleUnitPlaced(Cell cell, GameObject go)
        {
            var unit = go.GetComponent<GridUnit>();
            if (unit == null) return;

            if (unit.Owner != null)
            {
                // Quân cũ đổi chỗ. Chủ quân đổi chỗ King -> King ẩn lại với đối thủ.
                // Chủ quân dời King đã lộ sang ô khác -> ẩn lại (chui xuống). Đặt lại đúng ô cũ thì vẫn lộ.
                if (unit.Kind == UnitKind.King)
                {
                    var stats = unit.GetComponent<KingStats>();
                    if (stats != null) stats.IsRevealed = false;
                    RefreshKingVisibility();
                }
                return;
            }

            unit.Owner = CurrentPlayer;
            if (unit.Kind == UnitKind.King)
            {
                CurrentPlayer.Kings.Add(unit);
                go.AddComponent<KingStats>();
                RefreshKingVisibility(); // King mới đặt -> chui xuống ẩn
            }
            else CurrentPlayer.Attackers.Add(unit);
            StateChanged?.Invoke();
        }

        /// <summary>Số lá bài loại này trên tay (= số quân còn được tạo; Scout: 1 lá nếu chưa dùng).</summary>
        public int RemainingSpawns(UnitKind kind)
        {
            if (IsHandingOver || CurrentPlayer == null) return 0;
            return Current switch
            {
                GamePhase.Deploy when kind == UnitKind.King => _kingsPerPlayer - CurrentPlayer.Kings.Count,
                GamePhase.Attack when kind == UnitKind.Knight => _knightsPerPlayer - CurrentPlayer.CountAttackers(UnitKind.Knight),
                GamePhase.Attack when kind == UnitKind.Mage => _magesPerPlayer - CurrentPlayer.CountAttackers(UnitKind.Mage),
                GamePhase.Attack when kind == UnitKind.Scout => _scoutUsed.Contains(CurrentPlayer) ? 0 : 1,
                _ => 0
            };
        }

        // ---------------- Thẻ Trinh sát ----------------

        public bool CanScout() => Current == GamePhase.Attack && !IsHandingOver && !IsAnimating
                                  && CurrentPlayer != null && !_scoutUsed.Contains(CurrentPlayer);

        public bool CanScoutOn(GridSystem grid) => CurrentPlayer != null && grid == CurrentPlayer.Opponent.Board;

        /// <summary>Lộ mọi King còn sống của đối thủ trong vùng 2x2 (ô 'corner' + 3 ô kế bên tăng x / y) tới hết lượt. Mỗi người 1 lần/trận.</summary>
        public void Scout(GridSystem grid, Vector2Int corner)
        {
            if (!CanScout() || !CanScoutOn(grid)) return;
            _scoutUsed.Add(CurrentPlayer);
            int found = 0;
            foreach (GridUnit k in CurrentPlayer.Opponent.Kings)
            {
                if (!k.TryGetComponent(out KingStats s) || s.IsDead) continue;
                Vector2Int d = k.Coord - corner;
                if (d.x < 0 || d.x > 1 || d.y < 0 || d.y > 1) continue;
                s.Reveal(k.Coord);
                found++;
                CartoonFx.Comic("shock", k.transform.position + Vector3.up * 1.2f, 0.3f);
            }
            for (int dx = 0; dx <= 1; dx++)
                for (int dy = 0; dy <= 1; dy++)
                    if (grid.GetCell(corner + new Vector2Int(dx, dy)) != null)
                        CartoonFx.Puff(grid.CellToWorld(corner + new Vector2Int(dx, dy)) + Vector3.up * 0.1f, 0.6f, 5);
            Sfx.Play(found > 0 ? SfxId.Ding : SfxId.Deny, 1f, 0f);
            _battleReport.Clear();
            _battleReport.Add(found > 0 ? $"Scout found {found} King(s)!" : "Scout found nothing.");
            RefreshKingVisibility();
            StateChanged?.Invoke();
        }

        // ---------------- Cho HUD (GameHud) ----------------

        /// <summary>GridPlacer hỏi để không raycast khi đang che màn hình (nút HUD do EventSystem chặn).</summary>
        public bool IsOverUi(Vector2 guiPoint) => IsHandingOver;

        public string SubmitLabel => Current == GamePhase.End ? "END TURN" : "CONFIRM";

        public string PhaseName => Current switch
        {
            GamePhase.Deploy => "Deploy",
            GamePhase.Setup => "Setup",
            GamePhase.Attack => "Attack",
            GamePhase.End => "Results",
            _ => "Game Over"
        };

        public string HintText => Hint();

        private string Hint() => Current switch
        {
            GamePhase.Deploy => $"Play King cards onto your board ({CurrentPlayer.Kings.Count}/{_kingsPerPlayer} Kings placed)",
            GamePhase.Setup => "Drag your Kings to new cells if you like, then Confirm",
            GamePhase.Attack => $"Place up to {_knightsPerPlayer} Knights ({CurrentPlayer.CountAttackers(UnitKind.Knight)}) and " +
                                $"{_magesPerPlayer} Mages ({CurrentPlayer.CountAttackers(UnitKind.Mage)}) on the enemy board, then Confirm",
            GamePhase.End => "Check the results, then End Turn",
            _ => ""
        };
    }
}
