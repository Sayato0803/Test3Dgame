using System.Collections.Generic;
using UnityEngine;

namespace GridBattle
{
    /// <summary>
    /// Đổi material của 1 model sang toon shader lúc chạy (model FBX có material nhúng, không sửa trực tiếp được).
    /// Giữ màu / texture gốc; material chuyển đổi được cache theo material gốc để các quân dùng chung.
    /// </summary>
    public static class ToonStyle
    {
        private static readonly Dictionary<Material, Material> Cache = new();
        private static readonly int BaseColorId = Shader.PropertyToID("_BaseColor");
        private static readonly int BaseMapId = Shader.PropertyToID("_BaseMap");
        private static readonly int ColorId = Shader.PropertyToID("_Color");
        private static readonly int MainTexId = Shader.PropertyToID("_MainTex");

        public static void Apply(GameObject root)
        {
            CartoonAssets assets = CartoonAssets.Instance;
            if (assets == null || assets.ToonShader == null) return;
            bool smooth = OutlineNormals.ApplyTo(root);
            if (!smooth) Debug.LogWarning($"[ToonStyle] {root.name}: có mesh chưa bật Read/Write, viền dùng pháp tuyến thường (có thể bị hở ở cạnh).", root);

            foreach (Renderer r in root.GetComponentsInChildren<Renderer>(true))
            {
                if (r is ParticleSystemRenderer || r is SpriteRenderer) continue;
                // Mesh quả cầu lửa thừa trong model Mage (co còn ~1cm trước mặt -> viền toon thành chấm đen). Phép dùng sprite riêng.
                if (r.name.EndsWith("_Fireball")) { r.enabled = false; continue; }
                Material[] mats = r.sharedMaterials;
                for (int i = 0; i < mats.Length; i++)
                    if (mats[i] != null) mats[i] = Convert(mats[i], assets, smooth);
                r.sharedMaterials = mats;
            }
        }

        private static Material Convert(Material src, CartoonAssets assets, bool smoothOutline)
        {
            if (src.shader == assets.ToonShader) return src;
            if (Cache.TryGetValue(src, out Material m) && m != null) return m;

            m = new Material(assets.ToonShader) { name = src.name + " (Toon)" };
            Color c = src.HasProperty(BaseColorId) ? src.GetColor(BaseColorId)
                : src.HasProperty(ColorId) ? src.GetColor(ColorId) : Color.white;
            m.SetColor(BaseColorId, c);
            Texture tex = src.HasProperty(BaseMapId) ? src.GetTexture(BaseMapId)
                : src.HasProperty(MainTexId) ? src.GetTexture(MainTexId) : null;
            if (tex != null) m.SetTexture(BaseMapId, tex);
            m.SetColor("_OutlineColor", assets.OutlineColor);

            // Mắt/đồng tử rất nhỏ: viền mảnh hơn để không thành 1 chấm đen.
            string n = src.name.ToLowerInvariant();
            bool tiny = n.Contains("material.00") || n.Contains("eye") || n.Contains("pupil");
            m.SetFloat("_OutlineWidth", tiny ? 0f : assets.OutlineWidth);
            m.SetFloat("_OutlineSmoothNormals", smoothOutline ? 1f : 0f);
            // Màu tối (đồng tử) không cần highlight/rim: đồng tử giờ cong ôm mặt mắt, highlight toon sẽ thành mảng xám viền cứng.
            if (c.maxColorComponent < 0.1f) { m.SetFloat("_RimStrength", 0f); m.SetFloat("_HighlightStrength", 0f); }

            Cache[src] = m;
            return m;
        }
    }
}
