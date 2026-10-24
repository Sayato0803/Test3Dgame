#if UNITY_EDITOR
using UnityEditor;

namespace GridBattle.EditorTools
{
    /// <summary>Ảnh trong Resources/GUI tự import thành Sprite để Resources.Load&lt;Sprite&gt; lúc chạy dùng được.</summary>
    public class GuiSpriteImporter : AssetPostprocessor
    {
        private void OnPreprocessTexture()
        {
            if (!assetPath.StartsWith("Assets/Resources/GUI/")) return;
            var t = (TextureImporter)assetImporter;
            t.textureType = TextureImporterType.Sprite;
            t.spriteImportMode = SpriteImportMode.Single;
            t.mipmapEnabled = false;
            // Nút nền trống: 9-slice để co giãn theo mọi kích thước nút mà góc bo không méo.
            if (assetPath.Contains("/btn_blank_")) t.spriteBorder = new UnityEngine.Vector4(90, 90, 90, 90);
            if (assetPath.Contains("/ui_panels/")) t.spriteBorder = new UnityEngine.Vector4(70, 70, 70, 70);
        }
    }
}
#endif
