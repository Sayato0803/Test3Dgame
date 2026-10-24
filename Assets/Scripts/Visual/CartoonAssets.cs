using UnityEngine;

namespace GridBattle
{
    /// <summary>
    /// Tham chiếu asset dùng lúc chạy cho phong cách cartoon (nằm trong Resources để luôn được đưa vào bản build):
    /// toon shader, material hạt, sprite ngôi sao.
    /// </summary>
    [CreateAssetMenu(menuName = "King Hunt/Cartoon Assets", fileName = "CartoonAssets")]
    public class CartoonAssets : ScriptableObject
    {
        public Shader ToonShader;
        [Tooltip("Material hạt mềm (khói/bụi), shader URP Particles/Unlit, alpha blend.")]
        public Material PuffMaterial;
        [Tooltip("Material hạt ngôi sao.")]
        public Material StarMaterial;
        public Sprite StarSprite;
        public Color OutlineColor = new(0.09f, 0.06f, 0.12f, 1f);
        public float OutlineWidth = 0.0035f;

        private static CartoonAssets _instance;
        private static bool _loaded;

        public static CartoonAssets Instance
        {
            get
            {
                if (!_loaded || _instance == null)
                {
                    _instance = Resources.Load<CartoonAssets>("CartoonAssets");
                    _loaded = true;
                }
                return _instance;
            }
        }
    }
}
