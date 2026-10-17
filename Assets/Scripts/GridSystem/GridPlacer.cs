using System.Collections.Generic;
using UnityEngine;

namespace GridBattle
{
    /// <summary>
    /// Input đặt quân kiểu TFT:
    /// - Bấm nút King/Knight/Mage -> quân mới dính vào chuột -> click 1 ô để thả.
    /// - Nhấn giữ quân đang ở trên bàn -> kéo -> nhả chuột để thả.
    /// - Thả vào ô không hợp lệ: quân cũ về chỗ cũ, quân mới bị huỷ. Esc / chuột phải = huỷ.
    /// </summary>
    public class GridPlacer : MonoBehaviour
    {
        [SerializeField] private Camera _camera;
        [Tooltip("Để trống = tự tìm mọi GridSystem trong scene.")]
        [SerializeField] private GridSystem[] _grids;
        [Tooltip("Chỉ chọn layer Grid để quân/đạn không chặn raycast.")]
        [SerializeField] private LayerMask _gridMask = ~0;

        [Header("Model tạm (để trống = dùng khối primitive)")]
        [SerializeField] private GameObject _kingModel;
        [SerializeField] private GameObject _knightModel;
        [SerializeField] private GameObject _mageModel;
        [SerializeField] private float _unitScale = 1f;

        [Header("Kéo thả")]
        [SerializeField] private float _dragHeight = 0.5f;
        [SerializeField] private Color _validColor = new(0.3f, 0.9f, 0.4f);
        [SerializeField] private Color _invalidColor = new(0.95f, 0.25f, 0.25f);

        private GridUnit _dragging;
        private bool _draggingNew;
        private GridSystem _hoverGrid;
        private Vector2Int _hoverCoord;
        private readonly List<Rect> _uiRects = new();

        private void Awake()
        {
            if (_camera == null) _camera = Camera.main;
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

        private void LogPlaced(Cell cell, GameObject unit) => Debug.Log($"[Grid] Đặt {unit.name} vào ô {cell.Coord}");
        private void LogRemoved(Cell cell, GameObject unit) => Debug.Log($"[Grid] {unit.name} rời ô {cell.Coord}");

        private void Update()
        {
            ClearHover();
            bool overCell = TryGetHoveredCell(out GridSystem grid, out Vector2Int coord);

            if (_dragging == null)
            {
                // Nhấn giữ lên ô có quân -> nhấc quân lên. Ưu tiên Mage/Knight vì đứng đè lên King.
                if (overCell && Input.GetMouseButtonDown(0))
                {
                    Cell cell = grid.GetCell(coord);
                    GridUnit picked = cell.Attacker != null ? cell.Attacker : cell.King;
                    if (picked != null)
                    {
                        _dragging = picked;
                        _draggingNew = false;
                    }
                }
                return;
            }

            // ----- Đang kéo -----
            if (Input.GetKeyDown(KeyCode.Escape) || Input.GetMouseButtonDown(1))
            {
                CancelDrag();
                return;
            }

            Vector3 target = overCell ? grid.CellToWorld(coord) : MouseOnGround();
            _dragging.transform.position = Vector3.Lerp(
                _dragging.transform.position,
                target + Vector3.up * _dragHeight,
                1f - Mathf.Exp(-20f * Time.deltaTime));

            bool valid = overCell && grid.CanPlace(_dragging, coord);
            if (overCell) SetHover(grid, coord, valid ? _validColor : _invalidColor);

            // Quân mới: click để thả (lần nhả chuột đầu tiên là cú bấm nút). Quân cũ: nhả chuột để thả.
            bool drop = _draggingNew ? Input.GetMouseButtonDown(0) : Input.GetMouseButtonUp(0);
            if (!drop) return;

            if (valid) grid.Place(_dragging, coord);
            else
            {
                CancelDrag();
                return;
            }
            _dragging = null;
        }

        private void CancelDrag()
        {
            if (_draggingNew) Destroy(_dragging.gameObject);
            else _dragging.transform.position = _dragging.Grid.CellToWorld(_dragging.Coord); // về chỗ cũ
            _dragging = null;
        }

        // ---------------- Raycast ----------------

        private bool TryGetHoveredCell(out GridSystem grid, out Vector2Int coord)
        {
            grid = null;
            coord = default;
            if (MouseOverUi()) return false;

            Ray ray = _camera.ScreenPointToRay(Input.mousePosition);
            if (!Physics.Raycast(ray, out RaycastHit hit, 500f, _gridMask)) return false;

            grid = hit.collider.GetComponentInParent<GridSystem>();
            return grid != null && grid.TryWorldToCell(hit.point, out coord);
        }

        /// <summary>Khi chuột ra ngoài bàn, quân vẫn đi theo chuột trên mặt phẳng y = 0.</summary>
        private Vector3 MouseOnGround()
        {
            Ray ray = _camera.ScreenPointToRay(Input.mousePosition);
            var ground = new Plane(Vector3.up, Vector3.zero);
            return ground.Raycast(ray, out float d) ? ray.GetPoint(d) : _dragging.transform.position;
        }

        private void SetHover(GridSystem grid, Vector2Int coord, Color color)
        {
            _hoverGrid = grid;
            _hoverCoord = coord;
            grid.SetTileColor(coord, color);
        }

        private void ClearHover()
        {
            if (_hoverGrid != null) _hoverGrid.ResetTileColor(_hoverCoord);
            _hoverGrid = null;
        }

        // ---------------- Nút tạm (OnGUI) ----------------

        private void OnGUI()
        {
            _uiRects.Clear();
            DrawSpawnButton(0, "King", UnitKind.King, _kingModel);
            DrawSpawnButton(1, "Knight", UnitKind.Knight, _knightModel);
            DrawSpawnButton(2, "Mage", UnitKind.Mage, _mageModel);
        }

        private void DrawSpawnButton(int index, string label, UnitKind kind, GameObject model)
        {
            var rect = new Rect(16 + index * 110, Screen.height - 56, 100, 40);
            _uiRects.Add(rect);
            if (GUI.Button(rect, label) && _dragging == null)
            {
                _dragging = SpawnUnit(kind, model);
                _draggingNew = true;
            }
        }

        private bool MouseOverUi()
        {
            Vector2 m = Input.mousePosition;
            m.y = Screen.height - m.y; // OnGUI tính y từ trên xuống
            foreach (Rect r in _uiRects)
                if (r.Contains(m)) return true;
            return false;
        }

        private GridUnit SpawnUnit(UnitKind kind, GameObject model)
        {
            GameObject go;
            if (model != null)
            {
                go = Instantiate(model);
            }
            else
            {
                go = GameObject.CreatePrimitive(kind == UnitKind.King ? PrimitiveType.Cylinder
                    : kind == UnitKind.Knight ? PrimitiveType.Cube : PrimitiveType.Capsule);
                Destroy(go.GetComponent<Collider>());
            }

            go.name = kind.ToString();
            go.transform.localScale *= _unitScale;
            go.transform.position = MouseOnGroundOrOrigin();

            var unit = go.AddComponent<GridUnit>();
            unit.Kind = kind;
            return unit;
        }

        private Vector3 MouseOnGroundOrOrigin()
        {
            Ray ray = _camera.ScreenPointToRay(Input.mousePosition);
            var ground = new Plane(Vector3.up, Vector3.zero);
            return ground.Raycast(ray, out float d) ? ray.GetPoint(d) : Vector3.zero;
        }
    }
}
