using System.Collections.Generic;
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
        [SerializeField] private int _knightsPerPlayer = 2;
        [SerializeField] private int _magesPerPlayer = 2;

        public GamePhase Current { get; private set; } = GamePhase.Deploy;
        public Player CurrentPlayer { get; private set; }

        /// <summary>Đang hiện màn che đổi người: chặn mọi thao tác.</summary>
        public bool IsHandingOver { get; private set; }

        private Rect _submitRect;
        private readonly List<string> _battleReport = new();
        private Player _winner;

        private void Start()
        {
            if (_firstPlayer == null) _firstPlayer = _player1;

            // Observer: quân nào được đặt lần đầu thì ghi vào player đang tới lượt.
            _player1.Board.OnUnitPlaced += HandleUnitPlaced;
            _player2.Board.OnUnitPlaced += HandleUnitPlaced;

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
            if (_placer != null)
            {
                bool lookAtOpponent = phase == GamePhase.Attack || phase == GamePhase.End;
                _placer.SwitchCamera(lookAtOpponent ? player.AttackCameraIndex : player.CameraIndex);
            }
            RefreshKingVisibility();
            Debug.Log($"[Phase] {player.DisplayName} - {phase}");
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
                    EnterPhase(GamePhase.Setup, CurrentPlayer.Opponent);
                    break;
            }
        }

        /// <summary>
        /// Với mỗi King còn sống của đối thủ: +1 cho mỗi attacker có tầm phủ trúng (chồng tầm cộng dồn),
        /// +1 nếu có attacker đứng đúng ô King. Trúng -> lộ. Đối thủ hết King -> GameOver.
        /// </summary>
        private void ResolveBattle()
        {
            Player attacker = CurrentPlayer;
            Player defender = attacker.Opponent;
            _battleReport.Clear();

            foreach (GridUnit king in defender.Kings)
            {
                var stats = king.GetComponent<KingStats>();
                if (stats == null || stats.IsDead) continue;

                int damage = 0;
                bool standingOnKing = false;
                foreach (GridUnit a in attacker.Attackers)
                {
                    if (a.Grid != defender.Board) continue;
                    if (AttackRange.GetCells(a.Grid, a.Coord, a.transform, a.Range).Contains(king.Coord)) damage++;
                    if (a.Coord == king.Coord) standingOnKing = true;
                }
                if (standingOnKing) damage++;
                if (damage == 0) continue;

                stats.TakeDamage(damage);
                stats.IsRevealed = true;
                _battleReport.Add($"King ở ô {king.Coord} trúng {damage} dmg, còn {stats.Health}/{stats.MaxHealth}" +
                                  (stats.IsDead ? " - CHẾT" : ""));
            }

            if (_battleReport.Count == 0) _battleReport.Add("Không trúng King nào.");
            foreach (string line in _battleReport) Debug.Log($"[Battle] {attacker.DisplayName}: {line}");

            if (AllKingsDead(defender))
            {
                _winner = attacker;
                Current = GamePhase.GameOver;
                Debug.Log($"[Phase] {attacker.DisplayName} thắng");
            }
            RefreshKingVisibility();
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
                }
            }
        }

        public bool CanSubmit()
        {
            if (IsHandingOver || CurrentPlayer == null) return false;
            if (_placer != null && _placer.IsDragging) return false;

            switch (Current)
            {
                case GamePhase.Deploy:
                    return CurrentPlayer.Kings.Count == _kingsPerPlayer;
                case GamePhase.Attack:
                    return CurrentPlayer.CountAttackers(UnitKind.Knight) == _knightsPerPlayer
                        && CurrentPlayer.CountAttackers(UnitKind.Mage) == _magesPerPlayer;
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
            if (IsHandingOver || CurrentPlayer == null) return false;

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
            if (IsHandingOver || unit.Owner != CurrentPlayer) return false;

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
                if (unit.Kind == UnitKind.King)
                {
                    var stats = unit.GetComponent<KingStats>();
                    if (stats != null) stats.IsRevealed = false;
                }
                return;
            }

            unit.Owner = CurrentPlayer;
            if (unit.Kind == UnitKind.King)
            {
                CurrentPlayer.Kings.Add(unit);
                go.AddComponent<KingStats>();
            }
            else CurrentPlayer.Attackers.Add(unit);
        }

        // ---------------- UI tạm (OnGUI) ----------------

        /// <summary>GridPlacer hỏi để không raycast xuyên qua nút Submit.</summary>
        public bool IsOverUi(Vector2 guiPoint) => IsHandingOver || _submitRect.Contains(guiPoint);

        private void OnGUI()
        {
            if (CurrentPlayer == null) return;
            GUI.depth = -10; // vẽ đè lên nút của GridPlacer

            if (IsHandingOver)
            {
                var full = new Rect(0, 0, Screen.width, Screen.height);
                GUI.Box(full, "");
                GUI.Box(full, "");
                var btn = new Rect(Screen.width * 0.5f - 160, Screen.height * 0.5f - 30, 320, 60);
                if (GUI.Button(btn, $"Đưa máy cho {CurrentPlayer.DisplayName} - bấm để bắt đầu"))
                    IsHandingOver = false;
                return;
            }

            if (Current == GamePhase.GameOver)
            {
                var box = new Rect(Screen.width * 0.5f - 160, Screen.height * 0.5f - 40, 320, 80);
                GUI.Box(box, $"\n{_winner.DisplayName} THẮNG!");
                return;
            }

            GUI.Label(new Rect(16, 16, 600, 24), $"{CurrentPlayer.DisplayName} - {Current}: {Hint()}");
            if (Current == GamePhase.End)
                for (int i = 0; i < _battleReport.Count; i++)
                    GUI.Label(new Rect(16, 40 + i * 20, 600, 20), _battleReport[i]);

            _submitRect = new Rect(Screen.width - 196, Screen.height - 56, 180, 40);
            bool old = GUI.enabled;
            GUI.enabled = CanSubmit();
            if (GUI.Button(_submitRect, Current == GamePhase.End ? "Kết thúc lượt" : "Xác nhận")) Submit();
            GUI.enabled = old;
        }

        private string Hint() => Current switch
        {
            GamePhase.Deploy => $"đặt King lên bàn mình ({CurrentPlayer.Kings.Count}/{_kingsPerPlayer})",
            GamePhase.Setup => "có thể đổi chỗ King, rồi Xác nhận",
            GamePhase.Attack => $"đặt/đổi Knight ({CurrentPlayer.CountAttackers(UnitKind.Knight)}/{_knightsPerPlayer}), " +
                                $"Mage ({CurrentPlayer.CountAttackers(UnitKind.Mage)}/{_magesPerPlayer}) trên bàn đối thủ",
            GamePhase.End => "xem kết quả, rồi Kết thúc lượt",
            _ => ""
        };
    }
}
