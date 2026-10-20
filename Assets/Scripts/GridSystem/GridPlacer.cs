using System.Collections.Generic;
using UnityEngine;

namespace GridBattle
{

    public class GridPlacer : MonoBehaviour
    {
        [SerializeField] private Camera[] _camera = new Camera[5];
        [SerializeField] private int _startCameraIndex;
        [Tooltip("Để trống = tự tìm mọi GridSystem trong scene.")]
        [SerializeField] private GridSystem[] _grids;
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
        private int _cameraIndex;
        private Camera ActiveCam => _camera[_cameraIndex];

        private GridUnit _dragging;
        private bool _draggingNew;
        private GridSystem _hoverGrid;
        private readonly List<Vector2Int> _hoverCoords = new();
        private readonly List<Rect> _uiRects = new();

        private void Awake()
        {
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

        private void SwitchCamera(int index)
        {
            if (index < 0 || index >= _camera.Length || _camera[index] == null) return;

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
            if (Input.GetKeyDown(KeyCode.Alpha1)) SwitchCamera(0);
            if (Input.GetKeyDown(KeyCode.Alpha2)) SwitchCamera(1);
            if (Input.GetKeyDown(KeyCode.Alpha3)) SwitchCamera(2);
            if (Input.GetKeyDown(KeyCode.Alpha4)) SwitchCamera(3);
            if (Input.GetKeyDown(KeyCode.Alpha5)) SwitchCamera(4);

            float scroll = Input.mouseScrollDelta.y;
            if (scroll != 0)
            {
                if(_dragging == null)
                {
                    Camera cam = ActiveCam;
                    cam.fieldOfView = Mathf.Clamp(cam.fieldOfView - scroll *5f, 20f, 70f);
                }
                else
                {
                    _dragging.transform.Rotate(0,Mathf.Sign(scroll)*90f,0,Space.World);
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
            if (overCell)
            {
                int range = RangeOf(_dragging.Kind);
                if (valid && range > 0) ShowAttackPreview(grid, coord, FacingStep(_dragging.transform), range);
                else SetHover(grid, coord, valid ? _validColor : _invalidColor);
            }

            // Quân mới: click để thả (lần nhả chuột đầu tiên là cú bấm nút). Quân cũ: nhả chuột để thả.
            bool drop = _draggingNew ? Input.GetMouseButtonDown(0) : Input.GetMouseButtonUp(0);
            if (!drop) return;

            if (valid && _cameraIndex != _overViewCam) grid.Place(_dragging, coord);
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

        private static Vector2Int FacingStep(Transform t)
        {
            Vector3 f = t.forward;
            return new Vector2Int(Mathf.RoundToInt(f.x), Mathf.RoundToInt(f.z));
        }

        private void ShowAttackPreview(GridSystem grid, Vector2Int origin, Vector2Int step, int range)
        {
            for (int i = 0; i < range; i++)
            {
                Vector2Int c = origin + step * i;
                if (grid.GetCell(c) == null) continue;
                SetHover(grid, c, _previewAttackColor);
            }
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
            m.y = Screen.height - m.y; 
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
                if(_cameraIndex == 3 || _cameraIndex == 4)
                {
                    go.transform.rotation = Quaternion.Euler(0,180,0);
                }
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
            Ray ray = ActiveCam.ScreenPointToRay(Input.mousePosition);
            var ground = new Plane(Vector3.up, Vector3.zero);
            return ground.Raycast(ray, out float d) ? ray.GetPoint(d) : Vector3.zero;
        }
    }
}
