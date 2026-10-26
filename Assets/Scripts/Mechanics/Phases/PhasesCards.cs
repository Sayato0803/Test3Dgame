using System.Collections.Generic;
using DG.Tweening;
using UnityEngine;

namespace GridBattle
{
    /// <summary>
    /// Phần lá phép của Phases (cùng component, tách file cho dễ đọc): rút bài, dùng bài, status có thời hạn.
    /// "Turn" của status = số lần resolve battle liên quan (mỗi vòng mỗi người đúng 1 lượt nên khớp "1 turn = 1 vòng"):
    /// status trên attacker (choáng, Boost, Shield, Bomb) và Rift đếm lượt tấn công của chủ nó;
    /// status trên King (Shield, Reflect, Bomb, King giả) đếm lượt đối thủ tấn công.
    /// </summary>
    public partial class Phases
    {
        [Header("Lá phép")]
        [Tooltip("Các loại lá phép có thể rút (mỗi lá rút ngẫu nhiên 1 loại).")]
        [SerializeField] private CardData[] _deck;
        [Tooltip("Xác suất mỗi lá rút ra là lá Knight / Mage (50/50) thay vì lá phép.")]
        [SerializeField, Range(0f, 1f)] private float _attackerCardChance = 0.25f;

        private const int ChestDraw = 2;
        private const int HealAmount = 2;
        private const int StunDuration = 2;
        private const int RiftDuration = 3;
        private const int BombDuration = 3;
        private const int CloneDuration = 3;
        private const int DecoyBlastHit = 2;
        private static readonly Color TrapColor = new(0.7f, 0.75f, 0.85f);

        private class Rift
        {
            public Player owner;
            public GridSystem grid;
            public Vector2Int a, b;
            public int turns;
            public GameObject[] markers;
        }

        /// <summary>Tornado chờ sẵn trên bàn chủ nó, nổ khi đối thủ Confirm battle kế tiếp.</summary>
        private class TornadoTrap
        {
            public Player owner;
            public GridSystem grid;
            public Vector2Int corner;
            public GameObject marker;
        }

        private int _eventCounter;
        private bool _effectMove;
        /// <summary>Đang tính battle: damage của Bomb gộp vào kết quả battle, King bị thổi bay sau khi tính xong damage.</summary>
        private bool _resolving;
        private readonly List<Rift> _rifts = new();
        private readonly List<TornadoTrap> _traps = new();
        private readonly Dictionary<GridUnit, DizzyRing> _stunFx = new();
        private readonly Dictionary<GridUnit, int> _bombDamage = new();
        private readonly List<(GridUnit unit, System.Func<Vector2Int, bool> area, Vector3 from)> _pendingThrows = new();

        /// <summary>Event counter toàn cục: đặt quân, dùng bài đều lấy 1 số tăng dần.</summary>
        private int NextEvent() => ++_eventCounter;

        /// <summary>Cạnh vùng nổ Suicide Bomb (luôn lẻ để có ô tâm): turn 1-2 = 3, turn 3-4 = 5, từ turn 5 = 7.</summary>
        public static int BombSize(int turn) => turn <= 2 ? 3 : turn <= 4 ? 5 : 7;

        // shortcut: bộ bài vô hạn, rút ngẫu nhiên theo loại; khi có giới hạn số lá mỗi loại thì đổi sang list xáo + rút từ đầu.
        private void Draw(Player p, int count)
        {
            var drawn = new List<object>();
            for (int i = 0; i < count; i++)
            {
                if (HandCount(p) >= Player.MaxHand) break; // tay đầy: không rút thêm, lá còn lại bỏ qua
                if (Random.value < _attackerCardChance)
                {
                    bool knight = Random.value < 0.5f;
                    if (knight) p.KnightCards++;
                    else p.MageCards++;
                    drawn.Add(knight ? UnitKind.Knight : UnitKind.Mage);
                }
                else if (_deck != null && _deck.Length > 0)
                {
                    CardData card = _deck[Random.Range(0, _deck.Length)];
                    p.Hand.Add(card);
                    drawn.Add(card);
                }
            }
            if (drawn.Count > 0) CardsDrawn?.Invoke(drawn);
        }

        // ---------------- Dùng bài ----------------

        /// <summary>Chỉ dùng trong phase Attack của chính mình, trước Confirm, khi còn lượt dùng.</summary>
        public bool CanPlayCard(CardData card) => card != null && Current == GamePhase.Attack && !IsHandingOver && ActionsLeft > 0 && !IsAnimating
                                                 && CurrentPlayer != null && CurrentPlayer.Hand.Contains(card)
                                                 // Tay đầy thì không dùng Chest (dùng xong vẫn rút lại được 1 lá -> vượt luật "đủ 8 lá thì không rút nữa").
                                                 && !(card.Type == CardType.Chest && HandCount(CurrentPlayer) >= Player.MaxHand);

        /// <summary>Bàn phải click khi dùng lá. null = cả 2 bàn (lá nhắm vào quân của mình: King ở bàn mình, attacker ở bàn đối thủ).</summary>
        public GridSystem CardBoard(CardData card) => card.Target switch
        {
            CardTarget.OwnUnit => null,
            CardTarget.OwnAttacker or CardTarget.TwoCells => CurrentPlayer.Opponent.Board,
            _ => CurrentPlayer.Board
        };

        /// <summary>Ô 'c' có hợp lệ làm lần chọn tiếp theo không. first = ô đã chọn trước (lá cần 2 lần chọn), null nếu đây là lần đầu.</summary>
        public bool CanTarget(CardData card, GridSystem grid, Vector2Int c, Vector2Int? first)
        {
            if (CurrentPlayer == null || grid == null) return false;
            GridSystem board = CardBoard(card);
            if (board != null ? grid != board : grid != CurrentPlayer.Board && grid != CurrentPlayer.Opponent.Board) return false;
            Cell cell = grid.GetCell(c);
            if (cell == null) return false;
            GridUnit own = OwnUnitAt(grid, cell);
            return card.Target switch
            {
                CardTarget.OwnKing => IsOwnLiveKing(cell.King),
                CardTarget.OwnAttacker => own != null,
                CardTarget.OwnUnit => own != null && (card.Type != CardType.Healing || NeedsHealing(own)),
                CardTarget.TwoCells => first == null || c != first.Value,
                CardTarget.KingThenCell => first == null ? IsOwnLiveKing(cell.King) : cell.King == null,
                _ => true
            };
        }

        /// <summary>Quân của mình ở ô: King còn sống nếu là bàn mình, attacker nếu là bàn đối thủ.</summary>
        private GridUnit OwnUnitAt(GridSystem grid, Cell cell)
        {
            if (grid == CurrentPlayer.Board) return IsOwnLiveKing(cell.King) ? cell.King : null;
            return cell.Attacker != null && cell.Attacker.Owner == CurrentPlayer ? cell.Attacker : null;
        }

        private bool IsOwnLiveKing(GridUnit k) =>
            k != null && k.Owner == CurrentPlayer && k.TryGetComponent(out KingStats s) && !s.IsDead;

        /// <summary>Healing chỉ chọn được quân cần hồi: King chưa đầy máu, attacker đang choáng.</summary>
        private static bool NeedsHealing(GridUnit u) =>
            u.Kind == UnitKind.King ? u.TryGetComponent(out KingStats s) && s.Health < s.MaxHealth : u.StunTurns > 0;

        /// <summary>Các ô lá tác động khi click 'c' (GridPlacer tô màu): Tornado 2x2, còn lại 1 ô.</summary>
        public static List<Vector2Int> CardArea(CardData card, GridSystem grid, Vector2Int c) =>
            card.Target == CardTarget.Area2x2 ? Area2x2(grid, c) : new List<Vector2Int> { c };

        private static List<Vector2Int> Area2x2(GridSystem grid, Vector2Int corner)
        {
            var cells = new List<Vector2Int>();
            for (int dx = 0; dx <= 1; dx++)
                for (int dy = 0; dy <= 1; dy++)
                    if (grid.GetCell(corner + new Vector2Int(dx, dy)) != null) cells.Add(corner + new Vector2Int(dx, dy));
            return cells;
        }

        private static Vector3 AreaCenter(GridSystem grid, Vector2Int corner) =>
            (grid.CellToWorld(corner) + grid.CellToWorld(corner + Vector2Int.one)) * 0.5f;

        /// <summary>Dùng lá với các ô đã chọn (đủ card.PickCount ô). Kiểm tra lại luật; sai thì không làm gì.</summary>
        public bool PlayCard(CardData card, GridSystem grid, IReadOnlyList<Vector2Int> picks)
        {
            if (!CanPlayCard(card) || picks.Count != card.PickCount) return false;
            if (picks.Count > 0 && !CanTarget(card, grid, picks[0], null)) return false;
            if (picks.Count > 1 && !CanTarget(card, grid, picks[1], picks[0])) return false;

            CurrentPlayer.Hand.Remove(card);
            ActionsLeft--;
            // Không ghi toạ độ: Battle Report ai cũng xem được, ghi ô King là lộ vị trí.
            Report(CurrentPlayer, $"#{NextEvent()} played {card.DisplayName}");
            GridUnit target = picks.Count > 0 ? OwnUnitAt(grid, grid.GetCell(picks[0])) : null;
            switch (card.Type)
            {
                case CardType.Chest:
                    Draw(CurrentPlayer, ChestDraw);
                    break;
                case CardType.Healing:
                    if (target.Kind == UnitKind.King)
                    {
                        var hs = target.GetComponent<KingStats>();
                        hs.Health = Mathf.Min(hs.MaxHealth, hs.Health + HealAmount);
                    }
                    else ClearStun(target);
                    CardFx.Heal(target.transform.position);
                    break;
                case CardType.Shield:
                    target.Shield = true;
                    CardFx.Cast(target.transform.position, CardFx.ShieldColor);
                    break;
                case CardType.Reflect:
                    target.GetComponent<KingStats>().Reflect = true;
                    CardFx.Cast(target.transform.position, CardFx.ReflectColor);
                    break;
                case CardType.SuicideBomb:
                    target.BombTurns = BombDuration;
                    CardFx.Cast(target.transform.position, CardFx.BombColor);
                    break;
                case CardType.DmgBoost:
                    target.DamageBoost++;
                    CardFx.Cast(target.transform.position, CardFx.BoostColor);
                    break;
                case CardType.Tornado:
                    _traps.Add(new TornadoTrap
                    {
                        owner = CurrentPlayer, grid = grid, corner = picks[0],
                        marker = CardFx.Portal(AreaCenter(grid, picks[0]), TrapColor, 2.6f * grid.CellSize)
                    });
                    break;
                case CardType.Rift:
                    OpenRift(grid, picks[0], picks[1]);
                    break;
                case CardType.Clone:
                    SpawnDecoy(target, picks[1]);
                    break;
            }
            Sfx.Play(SfxId.Magic);
            RefreshKingVisibility();
            StateChanged?.Invoke();
            return true;
        }

        /// <summary>
        /// Vòng trạng thái lá phép trên quân và bẫy Tornado. Là thông tin riêng nên chỉ hiện với chủ đang tới lượt;
        /// đối thủ chỉ thấy lúc nó kích hoạt. Cổng Rift thì ai cũng thấy (không nằm ở đây).
        /// </summary>
        private void RefreshStatusFx()
        {
            foreach (Player p in new[] { _player1, _player2 })
            {
                bool mine = p == CurrentPlayer && Current != GamePhase.GameOver;
                foreach (GridUnit k in p.Kings)
                {
                    if (k == null || !k.TryGetComponent(out KingStats s)) continue;
                    bool on = mine && !s.IsDead;
                    StatusOf(k).Show(on && k.Shield, on && s.Reflect, on && k.BombTurns > 0, false, on && s.IsDecoy);
                }
                foreach (GridUnit a in p.Attackers)
                    if (a != null) StatusOf(a).Show(mine && a.Shield, false, mine && a.BombTurns > 0, mine && a.DamageBoost > 0, false);
            }
            foreach (TornadoTrap t in _traps)
                if (t.marker != null) t.marker.SetActive(t.owner == CurrentPlayer && Current != GamePhase.GameOver);
        }

        private static StatusFx StatusOf(GridUnit u) => u.TryGetComponent(out StatusFx f) ? f : u.gameObject.AddComponent<StatusFx>();

        // ---------------- Hiệu ứng trong battle ----------------

        /// <summary>
        /// Shield / Reflect của King chặn trọn 1 đòn (kể cả +1 đè bẹp, DMG Boost). Battle gọi theo counter tăng dần nên đó là đòn đầu tiên.
        /// Có cả 2: cùng kích hoạt, cùng tiêu hao. Hiệu ứng của King chạy trước, rồi tới hiệu ứng của attacker
        /// (Reflect choáng attacker -> Shield của attacker chặn choáng, hoặc Bomb của attacker nổ). Trả damage còn lại.
        /// </summary>
        private int BlockHit(GridUnit a, GridUnit king, KingStats s, int damage, Player by)
        {
            bool shield = king.Shield, reflect = s.Reflect;
            if (!shield && !reflect) return damage;
            string what = shield && reflect ? "Shield + Reflect" : shield ? "Shield" : "Reflect";
            Report(by, $"{what} blocked the {a.Kind} at {a.Coord}");
            king.Shield = s.Reflect = false;
            // Hiệu ứng chạy đúng lúc đòn chạm King.
            Vector3 kingFeet = king.transform.position, attackerFeet = a.transform.position;
            DOVirtual.DelayedCall(_attackAnimTime, () =>
            {
                if (shield) CardFx.ShieldBreak(kingFeet);
                if (reflect) CardFx.ReflectBounce(kingFeet, attackerFeet);
            });
            CartoonFx.Comic("bonk", king.transform.position + Vector3.up * 1.2f, _attackAnimTime);
            if (reflect) Stun(a, StunDuration, _attackAnimTime);
            return 0;
        }

        /// <summary>Lộ King (battle, Scout, bị Bomb trúng). King đang có Suicide Bomb thì nổ; fxDelay = lúc hiệu ứng nổ chạy (khớp nhịp đòn đánh).</summary>
        private void RevealKing(GridUnit king, KingStats s, float fxDelay = 0f)
        {
            s.Reveal(king.Coord);
            if (king.BombTurns > 0) Explode(king, fxDelay);
        }

        /// <summary>
        /// Suicide Bomb nổ vùng BombSize x BombSize tâm ô quân giữ bom (quân giữ bom không mất máu):
        /// - bom trên King: attacker đối phương trong vùng bị thổi bay + choáng (Shield của attacker chặn được);
        /// - bom trên attacker (đứng ở bàn đối thủ): King đối phương trong vùng mất 1 máu (cộng vào damage battle) rồi bị thổi bay.
        /// Nổ dây chuyền được: quân bị choáng đang giữ bom sẽ nổ tiếp; mỗi bom nổ 1 lần.
        /// </summary>
        private void Explode(GridUnit holder, float fxDelay)
        {
            holder.BombTurns = 0;
            GridSystem grid = holder.Grid;
            Vector2Int center = holder.Coord;
            int half = BombSize(Turn) / 2;
            System.Func<Vector2Int, bool> inBlast = c => Mathf.Abs(c.x - center.x) <= half && Mathf.Abs(c.y - center.y) <= half;
            Player enemy = holder.Owner.Opponent;
            Vector3 blast = grid.CellToWorld(center);
            int hit = 0;
            if (holder.Kind == UnitKind.King)
            {
                foreach (GridUnit a in enemy.Attackers.ToArray())
                {
                    if (a.Grid != grid || !inBlast(a.Coord) || ConsumeShield(a, fxDelay)) continue;
                    hit++;
                    Throw(a, inBlast, blast, fxDelay);
                    Stun(a, StunDuration, fxDelay); // quân văng khỏi bàn thì Stun bỏ qua
                }
            }
            else
            {
                foreach (GridUnit k in enemy.Kings.ToArray())
                {
                    if (!k.IsOnGrid || k.Grid != grid || !inBlast(k.Coord) || !k.TryGetComponent(out KingStats ks) || ks.IsDead) continue;
                    hit++;
                    BombDamage(k, ks, holder.Owner, fxDelay);
                    _pendingThrows.Add((k, inBlast, blast));
                }
                if (!_resolving) FlushThrows(fxDelay);
            }
            DOVirtual.DelayedCall(fxDelay, () => CardFx.Explosion(blast, (half + 0.6f) * grid.CellSize));
            Report(CurrentPlayer, $"Suicide Bomb exploded at {center} ({2 * half + 1}x{2 * half + 1}), {hit} unit(s) hit");
        }

        /// <summary>Bomb trên attacker trúng King: 1 damage + lộ. Ngoài battle (bom nổ do Scout) thì xử lý chết / thắng ngay tại chỗ.</summary>
        private void BombDamage(GridUnit k, KingStats ks, Player by, float delay)
        {
            ks.TakeDamage(1);
            if (_resolving)
            {
                _bombDamage[k] = _bombDamage.GetValueOrDefault(k) + 1;
            }
            else
            {
                KingHit?.Invoke(k, 1, delay);
                if (ks.IsDead)
                {
                    Report(by, $"King at {k.Coord} was blown up - DEFEATED");
                    Vector3 dir = k.transform.forward;
                    k.Grid.Remove(k);
                    if (k.TryGetComponent(out UnitReactions kr)) DOVirtual.DelayedCall(delay, () => kr.PlayKnockedOff(dir));
                    _busyUntil = Mathf.Max(_busyUntil, Time.time + delay + _deathBusy);
                    CheckWin(by, k, delay);
                    return;
                }
            }
            if (!ks.IsDead) RevealKing(k, ks, delay);
        }

        /// <summary>King bị bom trên attacker thổi bay sau khi đã tính xong damage của cả battle (King chết thì thôi).</summary>
        private void FlushThrows(float delay)
        {
            foreach (var (unit, area, from) in _pendingThrows)
                if (unit != null && unit.IsOnGrid && unit.TryGetComponent(out KingStats ks) && !ks.IsDead) Throw(unit, area, from, delay);
            _pendingThrows.Clear();
        }

        /// <summary>Shield của attacker chặn 1 lần bị choáng / bị nổ.</summary>
        private bool ConsumeShield(GridUnit u, float delay)
        {
            if (!u.Shield) return false;
            u.Shield = false;
            Vector3 feet = u.transform.position;
            DOVirtual.DelayedCall(delay, () => CardFx.ShieldBreak(feet));
            Report(CurrentPlayer, $"Shield protected {u.Owner.DisplayName}'s {u.Kind} at {u.Coord}");
            return true;
        }

        private void Stun(GridUnit a, int turns, float fxDelay = 0f)
        {
            if (!a.IsOnGrid || ConsumeShield(a, fxDelay)) return;
            a.StunTurns = Mathf.Max(a.StunTurns, turns);
            if (!_stunFx.TryGetValue(a, out DizzyRing ring) || ring == null)
            {
                float top = a.TryGetComponent(out UnitReactions r) ? r.TopHeight : 1f;
                _stunFx[a] = CartoonFx.DizzyStars(a.transform, Vector3.up * (top + 0.15f), float.PositiveInfinity);
            }
            if (a.BombTurns > 0) Explode(a, fxDelay);
        }

        private void ClearStun(GridUnit a)
        {
            a.StunTurns = 0;
            if (_stunFx.Remove(a, out DizzyRing ring) && ring != null) Destroy(ring.gameObject);
        }

        /// <summary>
        /// Sau mỗi lần 'attacker' resolve battle: status trên attacker của nó (choáng từ trước battle, Boost, Shield, Bomb)
        /// và Rift của nó hết 1 turn; status trên King của đối thủ (Shield, Reflect, Bomb, King giả) hết 1 turn.
        /// </summary>
        private void TickStatuses(Player attacker, List<GridUnit> stunnedAtStart)
        {
            foreach (GridUnit a in stunnedAtStart)
                if (--a.StunTurns <= 0) ClearStun(a);
            foreach (GridUnit a in attacker.Attackers)
            {
                a.DamageBoost = 0;
                a.Shield = false;
                if (a.BombTurns > 0) a.BombTurns--;
            }

            foreach (GridUnit k in attacker.Opponent.Kings.ToArray()) // King giả hết hạn bị bỏ khỏi list
            {
                if (!k.TryGetComponent(out KingStats s) || s.IsDead) continue;
                k.Shield = s.Reflect = false; // chỉ giữ qua 1 lần đối thủ tấn công
                if (k.BombTurns > 0) k.BombTurns--;
                if (s.IsDecoy && --s.DecoyTurns <= 0) ExpireDecoy(k);
            }
            for (int i = _rifts.Count - 1; i >= 0; i--)
            {
                Rift r = _rifts[i];
                if (r.owner != attacker || --r.turns > 0) continue;
                foreach (GameObject m in r.markers) Destroy(m);
                _rifts.RemoveAt(i);
            }
        }

        // ---------------- Tornado / Rift / Clone ----------------

        /// <summary>Đối thủ của 'trapOwner' vừa Confirm: mọi bẫy Tornado của 'trapOwner' nổ theo thứ tự đặt, trước khi tính đòn đánh.</summary>
        private void TriggerTraps(Player trapOwner)
        {
            foreach (TornadoTrap t in _traps.FindAll(t => t.owner == trapOwner))
            {
                Report(trapOwner, "Tornado trap went off!");
                Tornado(t.grid, t.corner);
                Destroy(t.marker);
                _traps.Remove(t);
            }
        }

        /// <summary>Mọi quân trong vùng 2x2 (kể cả King) bị thổi bay; King ẩn lại, attacker giữ hướng.</summary>
        private void Tornado(GridSystem grid, Vector2Int corner)
        {
            List<Vector2Int> area = Area2x2(grid, corner);
            var units = new List<GridUnit>();
            foreach (Vector2Int c in area)
            {
                Cell cell = grid.GetCell(c);
                if (cell.King != null) units.Add(cell.King);
                if (cell.Attacker != null) units.Add(cell.Attacker);
            }
            Vector3 center = AreaCenter(grid, corner);
            CardFx.Tornado(center, grid.CellSize);
            foreach (GridUnit u in units) Throw(u, area.Contains, center, 0.25f); // bị cuốn lên một nhịp rồi mới văng
        }

        /// <summary>Cổng A <-> B trên bàn đối thủ, RiftDuration turn. A đang có King còn sống (B không có King): King dịch sang B và ẩn lại.</summary>
        private void OpenRift(GridSystem grid, Vector2Int a, Vector2Int b)
        {
            _rifts.Add(new Rift
            {
                owner = CurrentPlayer, grid = grid, a = a, b = b, turns = RiftDuration,
                markers = new[]
                {
                    CardFx.Portal(grid.CellToWorld(a), CardFx.RiftInColor, grid.CellSize),
                    CardFx.Portal(grid.CellToWorld(b), CardFx.RiftOutColor, grid.CellSize)
                }
            });
            GridUnit king = grid.GetCell(a).King;
            if (king != null && king.TryGetComponent(out KingStats s) && !s.IsDead && grid.GetCell(b).King == null)
            {
                CardFx.Teleport(grid.CellToWorld(a), grid.CellToWorld(b)); // cả 2 bên thấy King vừa bị dịch chuyển
                Relocate(king, b);
            }
        }

        /// <summary>Cổng của các Rift đang mở trên bàn này, 2 chiều (A -> B và B -> A). Null nếu không có.</summary>
        public IReadOnlyDictionary<Vector2Int, Vector2Int> PortalsOn(GridSystem grid)
        {
            Dictionary<Vector2Int, Vector2Int> portals = null;
            foreach (Rift r in _rifts)
            {
                if (r.grid != grid) continue;
                portals ??= new Dictionary<Vector2Int, Vector2Int>();
                portals[r.a] = r.b;
                portals[r.b] = r.a;
            }
            return portals;
        }

        /// <summary>King giả ở ô 'at': HP = HP hiện tại của King, hướng như King. Hết HP hoặc sau CloneDuration turn thì mất.</summary>
        private void SpawnDecoy(GridUnit king, Vector2Int at)
        {
            if (_placer == null) return;
            GridSystem grid = king.Grid;
            GridUnit decoy = _placer.SpawnUnit(UnitKind.King, grid.CellToWorld(at));
            decoy.transform.rotation = king.transform.rotation;
            grid.Place(decoy, at); // HandleUnitPlaced: chủ quân = CurrentPlayer, thêm vào Kings + gắn KingStats
            var s = decoy.GetComponent<KingStats>();
            s.Health = king.GetComponent<KingStats>().Health;
            s.IsDecoy = true;
            s.DecoyTurns = CloneDuration;
            CardFx.Clone(king.transform.position, decoy.transform.position);
            if (decoy.TryGetComponent(out UnitReactions r)) r.PlaySpawn();
        }

        /// <summary>King giả bị tấn công lần thứ DecoyBlastHit: nổ tung, attacker vừa đánh nó văng khỏi bàn (bị loại, Shield không chặn).</summary>
        private void DecoyBlast(GridUnit decoy, GridUnit by, float delay)
        {
            Vector3 at = decoy.transform.position;
            float radius = decoy.Grid.CellSize * 1.2f;
            Report(CurrentPlayer, $"Clone at {decoy.Coord} exploded!");
            Eliminate(by, at, delay);
            decoy.Owner.Kings.Remove(decoy);
            decoy.Grid.Remove(decoy);
            DOVirtual.DelayedCall(delay, () =>
            {
                CardFx.Explosion(at, radius);
                if (decoy == null) return;
                if (decoy.TryGetComponent(out UnitReactions r)) r.Vanish();
                else Destroy(decoy.gameObject);
            });
        }

        private void ExpireDecoy(GridUnit decoy)
        {
            decoy.Owner.Kings.Remove(decoy);
            if (decoy.IsOnGrid) decoy.Grid.Remove(decoy);
            if (decoy.TryGetComponent(out UnitReactions r)) r.Vanish();
            else Destroy(decoy.gameObject);
        }

        // ---------------- Dời quân do hiệu ứng ----------------

        private static bool OnEdge(GridSystem grid, Vector2Int c) => c.x == 0 || c.y == 0 || c.x == grid.Size - 1 || c.y == grid.Size - 1;

        /// <summary>
        /// Bị thổi bay (Bomb, Tornado): attacker đang đứng ở viền bàn thì văng khỏi bàn, bị loại khỏi trận;
        /// còn lại rơi xuống ô trống ngẫu nhiên ngoài vùng 'area'. King không bao giờ bị loại.
        /// </summary>
        private void Throw(GridUnit u, System.Func<Vector2Int, bool> area, Vector3 from, float delay)
        {
            if (u.Kind != UnitKind.King && OnEdge(u.Grid, u.Coord))
            {
                Eliminate(u, from, delay);
                return;
            }
            Vector2Int? to = RandomEmptyCell(u.Grid, area);
            if (to.HasValue) Relocate(u, to.Value, delay, true);
        }

        /// <summary>Attacker văng khỏi bàn: rời trận hẳn (muốn quân mới phải rút lá Knight / Mage).</summary>
        private void Eliminate(GridUnit a, Vector3 from, float delay)
        {
            Vector3 dir = a.transform.position - from;
            dir.y = 0f;
            dir = dir.sqrMagnitude > 0.01f ? dir.normalized : a.transform.forward;
            a.Owner.Attackers.Remove(a);
            if (a.IsOnGrid) a.Grid.Remove(a);
            ClearStun(a);
            Report(CurrentPlayer, $"{a.Owner.DisplayName}'s {a.Kind} was knocked off the board");
            if (a.TryGetComponent(out UnitReactions r)) DOVirtual.DelayedCall(delay, () => { if (r != null) r.PlayKnockedOff(dir); });
            Destroy(a.gameObject, delay + 4f);
        }

        /// <summary>Ô không có King lẫn attacker, bỏ các ô 'exclude'. Null nếu bàn kín.</summary>
        private static Vector2Int? RandomEmptyCell(GridSystem grid, System.Func<Vector2Int, bool> exclude)
        {
            var options = new List<Vector2Int>();
            for (int x = 0; x < grid.Size; x++)
                for (int y = 0; y < grid.Size; y++)
                {
                    var c = new Vector2Int(x, y);
                    Cell cell = grid.GetCell(c);
                    if (cell.King == null && cell.Attacker == null && !exclude(c)) options.Add(c);
                }
            return options.Count == 0 ? null : options[Random.Range(0, options.Count)];
        }

        /// <summary>
        /// Dời quân do hiệu ứng. Ô trên lưới đổi ngay (luật dùng được liền); hình chạy sau 'delay' giây cho khớp nhịp hiệu ứng:
        /// fly = bị thổi bay vòng cung rồi tiếp đất (Bomb, Tornado), không thì biến vào khói rồi bật ra ở chỗ mới (Rift).
        /// King bị dời mất trạng thái lộ (HandleUnitPlaced).
        /// </summary>
        private void Relocate(GridUnit u, Vector2Int to, float delay = 0f, bool fly = false)
        {
            Vector3 from = u.transform.position;
            MoveUnit(u, to);
            Vector3 dest = u.transform.position;
            u.transform.position = from;
            u.TryGetComponent(out UnitReactions r);
            if (fly)
            {
                u.transform.DOJump(dest, 2f, 1, 0.55f).SetDelay(delay).SetEase(Ease.Linear).SetLink(u.gameObject)
                    .OnStart(() => Sfx.Play(SfxId.Boing, 0.8f, 0.1f))
                    .OnComplete(() => { if (r != null) r.Land(0f); });
                return;
            }
            DOVirtual.DelayedCall(delay, () =>
            {
                if (u == null) return;
                CartoonFx.Puff(from + Vector3.up * 0.3f, 0.8f, 10);
                u.transform.position = dest;
                CartoonFx.Puff(dest + Vector3.up * 0.3f, 0.8f, 10);
                if (r != null) r.PlaySpawn();
            });
        }

        /// <summary>Dời quân không do người chơi: giữ nguyên event counter của quân.</summary>
        private void MoveUnit(GridUnit u, Vector2Int to)
        {
            _effectMove = true;
            try { u.Grid.Place(u, to); }
            finally { _effectMove = false; }
        }
    }
}
