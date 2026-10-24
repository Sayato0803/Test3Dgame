using System;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.EventSystems;

namespace GridBattle
{

    public class GridPlacer : MonoBehaviour
    {
        [Tooltip("Các góc camera (Cam1..Cam5). Có CameraRig thì đây chỉ là điểm neo cho camera chính bay tới.")]
        [SerializeField] private Camera[] _camera = new Camera[5];
        [SerializeField] private CameraRig _rig;
        [SerializeField] private int _startCameraIndex;
        [Tooltip("Để trống = tự tìm mọi GridSystem trong scene.")]
        [SerializeField] private GridSystem[] _grids;
        [Tooltip("Để trống = không giới hạn luật (test tự do như trước).")]
        [SerializeField] private Phases _phases;
        [Tooltip("Chỉ chọn layer Grid để quân/đạn không chặn raycast.")]
        [SerializeField] private LayerMask _gridMask = ~0;

        [SerializeField] private GameObject _kingModel;
        [SerializeField] private GameObject _knightModel;
        [SerializeField] private GameObject _mageModel;
        [SerializeField] private float _unitScale = 1f;

        [Header("Kéo thả")]
        [SerializeField] private float _dragHeight = 0.5f;
        [SerializeField] private Color _validColor = new(0.3f, 0.9f, 0.4f);
        [SerializeField] private Color _invalidColor = new(0.95f, 0.25f, 0.25f);

        [SerializeField] private int _mageRange = 4;
        [SerializeField] private int _knightRange = 3;
        [SerializeField] private Color _previewAttackColor = new(0.55f, 0.8f, 1f, 0.4f);

        private readonly int _overViewCam = 2;
        public int OverviewCameraIndex => _overViewCam;
        private int _cameraIndex;
        public Camera ActiveCam => _rig != null ? _rig.Camera : _camera[_cameraIndex];

        private GridUnit _dragging;
        private bool _draggingNew;
        private float _spawnTime;
        private GridSystem _hoverGrid;
        private readonly List<Vector2Int> _hoverCoords = new();

        public bool IsDragging => _dragging != null;
        /// <summary>Đang kéo 1 quân mới từ thẻ bài (chưa đặt) - CardHand dùng để không chia lại lá đó.</summary>
        public UnitKind? DraggingNewKind => _dragging != null && _draggingNew ? _dragging.Kind : null;
        /// <summary>Camera đang bay: tạm khoá thao tác.</summary>
        public bool IsCameraMoving => _rig != null && _rig.IsMoving;

        /// <summary>Bắt đầu cầm 1 quân (quân mới từ thẻ hoặc quân đã đặt).</summary>
        public event Action<GridUnit> OnDragStarted;
        /// <summary>Kết thúc cầm: true = đã đặt xuống ô, false = huỷ.</summary>
        public event Action<GridUnit, bool> OnDragEnded;

        // Không có Phases thì nhấc được mọi quân (test tự do).
        private bool CanPickUp(GridUnit unit) => _phases == null || _phases.CanPickUp(unit);

        private void Awake()
        {
            if (_rig != null) _rig.SnapTo(_startCameraIndex);
            SwitchCamera(_startCameraIndex);
            if (_grids == null || _grids.Length == 0)
                _grids = FindObjectsByType<GridSystem>(FindObjectsSortMode.None);
        }

        // Ví dụ 1 thành phần khác "nghe" tín hiệu của GridSystem (Observer pattern).
        private void OnEnable()
        {
            foreach (GridSystem g in _grids)
            {
                g.OnUnitPlaced += LogPlaced;
                g.OnUnitRemoved += LogRemoved;
            }
        }

        private void OnDisable()
        {
            foreach (GridSystem g in _grids)
            {
                if (g == null) continue;
                g.OnUnitPlaced -= LogPlaced;
                g.OnUnitRemoved -= LogRemoved;
            }
        }

        public void SwitchCamera(int index)
        {
            if (index < 0 || index >= _camera.Length || _camera[index] == null) return;

            if (_rig != null) _rig.MoveTo(index);
            else
                for (int i = 0; i < _camera.Length; i++)
                    if (_camera[i] != null)
                        _camera[i].gameObject.SetActive(i == index);

            _cameraIndex = index;
            if (index == _overViewCam && _dragging != null){
                CancelDrag();
                ClearHover();
            }
        }

        private void LogPlaced(Cell cell, GameObject unit) => Debug.Log($"[Grid] Đặt {unit.name} vào ô {cell.Coord}");
        private void LogRemoved(Cell cell, GameObject unit) => Debug.Log($"[Grid] {unit.name} rời ô {cell.Coord}");

        private void Update()
        {
            if (Time.timeScale == 0f) return; // pause menu

            if (Input.GetKeyDown(KeyCode.Alpha1)) SwitchCamera(0);
            if (Input.GetKeyDown(KeyCode.Alpha2)) SwitchCamera(1);
            if (Input.GetKeyDown(KeyCode.Alpha3)) SwitchCamera(2);
            if (Input.GetKeyDown(KeyCode.Alpha4)) SwitchCamera(3);
            if (Input.GetKeyDown(KeyCode.Alpha5)) SwitchCamera(4);

            // Đang che màn hình đổi người hoặc camera đang bay: không cho thao tác gì.
            if ((_phases != null && (_phases.IsHandingOver || _phases.IsAnimating)) || IsCameraMoving)
            {
                ClearHover();
                return;
            }

            float scroll = Input.mouseScrollDelta.y;
            if (scroll != 0 && !MouseOverUi())
            {
                if(_dragging == null)
                {
                    Camera cam = ActiveCam;
                    cam.fieldOfView = Mathf.Clamp(cam.fieldOfView - scroll *5f, 20f, 70f);
                }
                else
                {
                    _dragging.transform.Rotate(0,Mathf.Sign(scroll)*90f,0,Space.World);
                    Reactions(_dragging)?.PlayTurn();
                }

            }

            ClearHover();
            bool overCell = TryGetHoveredCell(out GridSystem grid, out Vector2Int coord);

            if (_dragging == null)
            {
                // Nhấn giữ lên ô có quân -> nhấc quân lên. Ưu tiên Mage/Knight vì đứng đè lên King.
                if (overCell && Input.GetMouseButtonDown(0))
                {
                    Cell cell = grid.GetCell(coord);
                    GridUnit picked = null;
                    if (cell.Attacker != null && CanPickUp(cell.Attacker)) picked = cell.Attacker;
                    else if (cell.King != null && CanPickUp(cell.King)) picked = cell.King;
                    if (picked != null) BeginDrag(picked, false);
                }
                return;
            }

            // ----- Đang kéo -----
            if (Input.GetKeyDown(KeyCode.Escape) || Input.GetMouseButtonDown(1))
            {
                CancelDrag();
                return;
            }

            // Quân mới từ thẻ bay chậm hơn lúc đầu (vệt bay từ lá bài ra), sau đó bám chuột sát.
            float follow = _draggingNew ? Mathf.Lerp(5f, 20f, (Time.time - _spawnTime) / 0.5f) : 20f;
            Vector3 target = overCell ? grid.CellToWorld(coord) : MouseOnGround();
            _dragging.transform.position = Vector3.Lerp(
                _dragging.transform.position,
                target + Vector3.up * _dragHeight,
                1f - Mathf.Exp(-follow * Time.deltaTime));

            bool valid = overCell && grid.CanPlace(_dragging, coord)
                         && (_phases == null || _phases.CanDropOn(grid));
            if (overCell)
            {
                if (valid && _dragging.Range > 0) ShowAttackPreview(grid, coord);
                else SetHover(grid, coord, valid ? _validColor : _invalidColor);
            }

            // Quân mới: click để thả (cú click lá bài đã nhả chuột). Quân cũ: nhả chuột để thả.
            bool drop = _draggingNew ? Input.GetMouseButtonDown(0) : Input.GetMouseButtonUp(0);
            if (!drop) return;

            if (valid && _cameraIndex != _overViewCam)
            {
                GridUnit unit = _dragging;
                float before = unit.transform.position.y;
                _dragging = null; // trước Place: listener của OnUnitPlaced thấy IsDragging = false
                grid.Place(unit, coord);
                Reactions(unit)?.Land(before - unit.transform.position.y);
                OnDragEnded?.Invoke(unit, true);
            }
            else CancelDrag();
        }

        private void BeginDrag(GridUnit unit, bool isNew)
        {
            _dragging = unit;
            _draggingNew = isNew;
            _spawnTime = Time.time;
            Reactions(unit)?.BeginHeld();
            OnDragStarted?.Invoke(unit);
        }

        private void CancelDrag()
        {
            GridUnit unit = _dragging;
            if (unit == null) return;
            _dragging = null;

            UnitReactions r = Reactions(unit);
            if (_draggingNew)
            {
                if (r != null) r.Vanish();
                else Destroy(unit.gameObject);
            }
            else
            {
                float before = unit.transform.position.y;
                unit.transform.position = unit.Grid.CellToWorld(unit.Coord); // về chỗ cũ
                r?.Land(before - unit.transform.position.y);
            }
            OnDragEnded?.Invoke(unit, false);
        }

        private static UnitReactions Reactions(GridUnit u) => u != null && u.TryGetComponent(out UnitReactions r) ? r : null;

        // ---------------- Thẻ bài ----------------

        /// <summary>
        /// Lá bài được bấm: tạo quân ngay trước camera tại vị trí lá bài, quân bay ra và bám theo chuột.
        /// Trả false nếu lúc này không được tạo loại quân đó (lá bài sẽ rung từ chối).
        /// </summary>
        public bool TrySpawnFromCard(UnitKind kind, Vector2 screenPos)
        {
            if (_dragging != null || IsCameraMoving || Time.timeScale == 0f) return false;
            if (_phases != null && !_phases.CanSpawn(kind)) return false;
            if (_cameraIndex == _overViewCam) return false;

            Vector3 start = ActiveCam.ScreenToWorldPoint(new Vector3(screenPos.x, screenPos.y, 4f));
            GridUnit unit = SpawnUnit(kind, ModelOf(kind), start);
            Reactions(unit)?.PlaySpawn();
            BeginDrag(unit, true);
            return true;
        }

        private GameObject ModelOf(UnitKind kind) => kind switch
        {
            UnitKind.King => _kingModel,
            UnitKind.Knight => _knightModel,
            _ => _mageModel
        };

        // ---------------- Raycast ----------------

        private bool TryGetHoveredCell(out GridSystem grid, out Vector2Int coord)
        {
            grid = null;
            coord = default;
            if (MouseOverUi()) return false;

            Ray ray = ActiveCam.ScreenPointToRay(Input.mousePosition);
            if (!Physics.Raycast(ray, out RaycastHit hit, 500f, _gridMask)) return false;

            grid = hit.collider.GetComponentInParent<GridSystem>();
            return grid != null && grid.TryWorldToCell(hit.point, out coord);
        }

        /// <summary>Khi chuột ra ngoài bàn, quân vẫn đi theo chuột trên mặt phẳng y = 0.</summary>
        private Vector3 MouseOnGround()
        {
            Ray ray = ActiveCam.ScreenPointToRay(Input.mousePosition);
            var ground = new Plane(Vector3.up, Vector3.zero);
            return ground.Raycast(ray, out float d) ? ray.GetPoint(d) : _dragging.transform.position;
        }

        private void SetHover(GridSystem grid, Vector2Int coord, Color color)
        {
            _hoverGrid = grid;
            _hoverCoords.Add(coord);
            grid.SetTileColor(coord, color);
        }

        private void ClearHover()
        {
            if (_hoverGrid != null)
                foreach (Vector2Int c in _hoverCoords)
                    _hoverGrid.ResetTileColor(c);
            _hoverCoords.Clear();
            _hoverGrid = null;
        }

        //Preview attack range

        private int RangeOf(UnitKind kind) => kind switch
        {
            UnitKind.Mage => _mageRange,
            UnitKind.Knight => _knightRange,
            _ => 0
        };

        // Dùng chung AttackRange với Battle -> ô tô preview = ô tính damage.
        private void ShowAttackPreview(GridSystem grid, Vector2Int origin)
        {
            foreach (Vector2Int c in AttackRange.GetCells(grid, origin, _dragging.transform, _dragging.Range))
                SetHover(grid, c, _previewAttackColor);
        }

        // ---------------- UI ----------------

        /// <summary>Chuột đang trên UI (thẻ bài, HUD) thì không raycast xuống bàn.</summary>
        private bool MouseOverUi()
        {
            if (EventSystem.current != null && EventSystem.current.IsPointerOverGameObject()) return true;
            Vector2 m = Input.mousePosition;
            m.y = Screen.height - m.y;
            return _phases != null && _phases.IsOverUi(m);
        }

        private GridUnit SpawnUnit(UnitKind kind, GameObject model, Vector3 position)
        {
            GameObject go;
            if (model != null)
            {
                go = Instantiate(model);
                if(_cameraIndex == 3 || _cameraIndex == 4)
                {
                    go.transform.rotation = Quaternion.Euler(0,180,0);
                }
            }
            else
            {
                // Primitive làm con của 1 gốc rỗng để UnitReactions có thể bọc phần hiển thị.
                go = new GameObject();
                GameObject shape = GameObject.CreatePrimitive(kind == UnitKind.King ? PrimitiveType.Cylinder
                    : kind == UnitKind.Knight ? PrimitiveType.Cube : PrimitiveType.Capsule);
                Destroy(shape.GetComponent<Collider>());
                shape.transform.SetParent(go.transform, false);
                shape.transform.localPosition = Vector3.up * 0.5f;
            }

            go.name = kind.ToString();
            go.transform.localScale *= _unitScale;
            go.transform.position = position;

            var unit = go.AddComponent<GridUnit>();
            unit.Kind = kind;
            unit.Range = RangeOf(kind);
            ToonStyle.Apply(go);
            go.AddComponent<UnitReactions>();
            return unit;
        }
    }
}
