using System;
using UnityEngine;

namespace GridBattle
{
    public class GridSystem : MonoBehaviour
    {
        [SerializeField] private Material _tileMaterial;
        [SerializeField] private int _size = 10;
        [SerializeField] private float _cellSize = 1f;
        [Tooltip("Layer của collider bàn. Tạo layer này trong Tags & Layers; không có thì dùng Default.")]
        [SerializeField] private string _gridLayerName = "Grid";
        [SerializeField] private Color _colorB = Color.white;

        private Cell[,] _cells;
        private Renderer[,] _tiles;

        /// <summary>Phát khi 1 quân được đặt lên ô (kể cả khi kéo từ ô khác sang).</summary>
        public event Action<Cell, GameObject> OnUnitPlaced;
        /// <summary>Phát khi 1 quân rời ô (bị kéo đi hoặc bị gỡ).</summary>
        public event Action<Cell, GameObject> OnUnitRemoved;

        public int Size => _size;
        public float CellSize => _cellSize;
        
        private Vector3 Corner => transform.position - new Vector3(_size * _cellSize * 0.5f, 0f, _size * _cellSize * 0.5f);

        private void Awake()
        {
            _cells = new Cell[_size, _size];
            _tiles = new Renderer[_size, _size];
            for (int x = 0; x < _size; x++)
                for (int y = 0; y < _size; y++)
                    _cells[x, y] = new Cell(new Vector2Int(x, y));

            CreateCollider();
            CreateTiles();
        }

        // ---------------- Toạ độ ----------------

        private bool InBounds(Vector2Int c) => c.x >= 0 && c.y >= 0 && c.x < _size && c.y < _size;
        public Cell GetCell(Vector2Int c) => InBounds(c) ? _cells[c.x, c.y] : null;



        /// <summary>Điểm world (vd hit.point) -> ô. Trả false nếu nằm ngoài bàn.</summary>
        public bool TryWorldToCell(Vector3 world, out Vector2Int coord)
        {
            Vector3 local = world - Corner;
            coord = new Vector2Int(
                Mathf.FloorToInt(local.x / _cellSize),
                Mathf.FloorToInt(local.z / _cellSize)); // trục z của world = hàng y của mảng
            return InBounds(coord);
        }

        /// <summary>Ô -> tâm ô trong world (+0.5 để từ mép ô vào giữa ô).</summary>
        public Vector3 CellToWorld(Vector2Int c) =>
            Corner + new Vector3((c.x + 0.5f) * _cellSize, 0f, (c.y + 0.5f) * _cellSize);

        // ---------------- Luật đặt quân ----------------

        /// <summary>
        /// King không chồng lên King khác; Mage/Knight chỉ bị chặn bởi Mage/Knight khác.
        /// Ô đang chứa chính quân này coi như hợp lệ (thả lại chỗ cũ).
        /// </summary>
        public bool CanPlace(GridUnit unit, Vector2Int c)
        {
            Cell cell = GetCell(c);
            if (cell == null) return false;

            GridUnit occupant = unit.Kind == UnitKind.King ? cell.King : cell.Attacker;
            return occupant == null || occupant == unit;
        }

        public bool Place(GridUnit unit, Vector2Int c)
        {
            if (!CanPlace(unit, c)) return false;

            if (unit.IsOnGrid) unit.Grid.Remove(unit); // rời ô cũ (có thể ở bàn khác)

            Cell cell = _cells[c.x, c.y];
            if (unit.Kind == UnitKind.King) cell.King = unit;
            else cell.Attacker = unit;

            unit.Grid = this;
            unit.Coord = c;
            unit.transform.position = CellToWorld(c);
            OnUnitPlaced?.Invoke(cell, unit.gameObject);
            return true;
        }

        private void Remove(GridUnit unit)
        {
            if (unit.Grid != this) return;

            Cell cell = _cells[unit.Coord.x, unit.Coord.y];
            if (cell.King == unit) cell.King = null;
            if (cell.Attacker == unit) cell.Attacker = null;

            unit.Grid = null;
            OnUnitRemoved?.Invoke(cell, unit.gameObject);
        }

        // ---------------- Hiển thị (a1: mỗi ô 1 quad) ----------------

        public void SetTileColor(Vector2Int c, Color color)
        {
            if (InBounds(c)) ApplyColor(_tiles[c.x, c.y], color);
        }

        public void ResetTileColor(Vector2Int c)
        {
            if (InBounds(c)) ApplyColor(_tiles[c.x, c.y], BaseColor(c));
        }

        private Color BaseColor(Vector2Int c) => _colorB;
        private void CreateCollider()
        {
            var go = new GameObject("GridCollider");
            go.transform.SetParent(transform, false);

            int layer = LayerMask.NameToLayer(_gridLayerName);
            if (layer < 0)
                Debug.LogWarning($"[GridSystem] Chưa có layer '{_gridLayerName}', collider bàn dùng layer Default.", this);
            else
                go.layer = layer;

            var box = go.AddComponent<BoxCollider>();
            box.size = new Vector3(_size * _cellSize, 0.02f, _size * _cellSize);
        }

        private void CreateTiles()
        {
            var root = new GameObject("Tiles").transform;
            root.SetParent(transform, false);

            for (int x = 0; x < _size; x++)
            {
                for (int y = 0; y < _size; y++)
                {
                    var c = new Vector2Int(x, y);
                    GameObject tile = GameObject.CreatePrimitive(PrimitiveType.Quad);
                    tile.name = $"Tile_{x}_{y}";
                    Destroy(tile.GetComponent<Collider>());
                    tile.transform.SetParent(root, true);
                    tile.transform.position = CellToWorld(c) + Vector3.up * 0.011f;
                    tile.transform.rotation = Quaternion.Euler(90f, 0f, 0f); 
                    tile.transform.localScale = Vector3.one * (_cellSize * 0.95f); 
                    
                    if (_tileMaterial != null) tile.GetComponent<Renderer>().sharedMaterial = _tileMaterial;
                    _tiles[x, y] = tile.GetComponent<Renderer>();
                    ApplyColor(_tiles[x, y], BaseColor(c));
                }
            }
        }

        private static readonly int BaseColorId = Shader.PropertyToID("_BaseColor");
        private static readonly int ColorId = Shader.PropertyToID("_Color");

        /// <summary>URP Lit dùng _BaseColor, Built-in dùng _Color; set cả 2.</summary>
        private static void ApplyColor(Renderer renderer, Color color)
        {
            Material m = renderer.material;
            if (m.HasProperty(BaseColorId)) m.SetColor(BaseColorId, color);
            if (m.HasProperty(ColorId)) m.SetColor(ColorId, color);
        }
    }
}
