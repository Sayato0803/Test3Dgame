using System.Collections.Generic;
using UnityEngine;

namespace GridBattle
{
    /// <summary>
    /// Viền toon (inverted hull) đẩy đỉnh theo pháp tuyến. Mesh low-poly có pháp tuyến tách ở cạnh cứng
    /// (mỗi mặt 1 bản sao đỉnh) nên viền bị hở/"bể" ở các cạnh. Hàm này tạo bản sao mesh có pháp tuyến làm mượt
    /// (cộng dồn pháp tuyến các đỉnh trùng vị trí) lưu vào kênh tangent - shader Toon đọc khi _OutlineSmoothNormals = 1.
    /// Tangent được skin theo xương nên dùng được cho SkinnedMeshRenderer.
    /// </summary>
    public static class OutlineNormals
    {
        private static readonly Dictionary<Mesh, Mesh> Cache = new();

        /// <summary>Trả về mesh đã bake (cache theo mesh gốc). Mesh không đọc được (Read/Write tắt) thì trả null.</summary>
        public static Mesh GetBaked(Mesh src)
        {
            if (src == null) return null;
            if (src.name.EndsWith("(SmoothOutline)")) return src;
            if (Cache.TryGetValue(src, out Mesh m) && m != null) return m;
            if (!src.isReadable) return null;
            m = Bake(src);
            Cache[src] = m;
            return m;
        }

        public static Mesh Bake(Mesh src)
        {
            Mesh m = Object.Instantiate(src);
            m.name = src.name + " (SmoothOutline)";
            Vector3[] v = m.vertices;
            Vector3[] n = m.normals;
            if (n == null || n.Length != v.Length) { m.RecalculateNormals(); n = m.normals; }

            // Gom đỉnh theo vị trí (làm tròn 0.0001) -> cộng pháp tuyến -> chuẩn hoá.
            var sums = new Dictionary<Vector3Int, Vector3>(v.Length);
            var keys = new Vector3Int[v.Length];
            for (int i = 0; i < v.Length; i++)
            {
                var k = new Vector3Int(Mathf.RoundToInt(v[i].x * 10000f), Mathf.RoundToInt(v[i].y * 10000f), Mathf.RoundToInt(v[i].z * 10000f));
                keys[i] = k;
                sums[k] = sums.TryGetValue(k, out Vector3 s) ? s + n[i] : n[i];
            }

            var t = new Vector4[v.Length];
            for (int i = 0; i < v.Length; i++)
            {
                Vector3 s = sums[keys[i]];
                Vector3 dir = s.sqrMagnitude > 1e-8f ? s.normalized : n[i];
                t[i] = new Vector4(dir.x, dir.y, dir.z, 1f);
            }
            m.tangents = t;
            return m;
        }

        /// <summary>
        /// Thay mesh của mọi MeshFilter / SkinnedMeshRenderer dưới root bằng bản đã bake.
        /// Trả false nếu có mesh không bake được (Read/Write tắt) -> không nên bật _OutlineSmoothNormals.
        /// </summary>
        public static bool ApplyTo(GameObject root)
        {
            bool all = true;
            foreach (MeshFilter mf in root.GetComponentsInChildren<MeshFilter>(true))
            {
                Mesh baked = GetBaked(mf.sharedMesh);
                if (baked != null) mf.sharedMesh = baked;
                else all = false;
            }
            foreach (SkinnedMeshRenderer smr in root.GetComponentsInChildren<SkinnedMeshRenderer>(true))
            {
                Mesh baked = GetBaked(smr.sharedMesh);
                if (baked != null) smr.sharedMesh = baked;
                else all = false;
            }
            return all;
        }
    }
}
