using System.Collections.Generic;
using UnityEngine;
using UnityEngine.UI;

namespace GridBattle
{
    /// <summary>
    /// Viền chữ kín 8 hướng + bóng đổ trong 1 effect (số đỉnh x10).
    /// Outline của Unity chỉ lệch 4 góc chéo (hở cạnh ngang / dọc); chồng nhiều Outline thì số đỉnh nhân lên
    /// vượt giới hạn 65000 của mesh UI và cả khối chữ biến mất.
    /// </summary>
    public class TextOutline8 : BaseMeshEffect
    {
        public Color OutlineColor = Color.black;
        public float Thickness = 2f;
        public Color ShadowColor = new(0f, 0f, 0f, 0.5f);
        public Vector2 ShadowOffset = new(0f, -3f);

        private static readonly List<UIVertex> Source = new();
        private static readonly List<UIVertex> Result = new();

        public override void ModifyMesh(VertexHelper vh)
        {
            if (!IsActive()) return;
            Source.Clear();
            Result.Clear();
            vh.GetUIVertexStream(Source);
            // Vẽ theo thứ tự: bóng -> 8 lớp viền -> chữ gốc (lớp sau đè lớp trước).
            Copy(ShadowOffset, ShadowColor);
            for (int i = 0; i < 8; i++)
            {
                float a = i * Mathf.PI / 4f;
                Copy(new Vector2(Mathf.Cos(a), Mathf.Sin(a)) * Thickness, OutlineColor);
            }
            Result.AddRange(Source);
            vh.Clear();
            vh.AddUIVertexTriangleStream(Result);
        }

        private static void Copy(Vector2 offset, Color color)
        {
            foreach (UIVertex src in Source)
            {
                UIVertex v = src;
                v.position += (Vector3)offset;
                Color32 c = color;
                c.a = (byte)(c.a * src.color.a / 255);
                v.color = c;
                Result.Add(v);
            }
        }
    }
}
