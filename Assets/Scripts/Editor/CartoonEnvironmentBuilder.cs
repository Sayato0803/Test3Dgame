#if UNITY_EDITOR
using System.Collections.Generic;
using System.IO;
using UnityEditor;
using UnityEditor.SceneManagement;
using UnityEngine;
using UnityEngine.Rendering;
using UnityEngine.Rendering.Universal;

namespace GridBattle.EditorTools
{
    /// <summary>
    /// Dựng môi trường cartoon cho scene đang mở: đảo cỏ nhiều tầng, mặt nước, cây tròn, bụi, đá, hoa, mây trôi,
    /// cờ 2 phe, viền gỗ cho bàn, bầu trời gradient, ánh sáng ấm, post-processing (bloom, màu tươi).
    /// Chạy lại được nhiều lần: xoá object "Environment" cũ rồi dựng mới. Tạo asset dùng chung (material, CartoonAssets) nếu chưa có.
    /// </summary>
    public static class CartoonEnvironmentBuilder
    {
        private const string ArtDir = "Assets/Art";
        private const string MatDir = "Assets/Art/Materials";
        private const string TexDir = "Assets/Art/Textures";
        private const string MeshDir = "Assets/Art/Meshes";

        [MenuItem("Tools/King Hunt/Build Cartoon Environment (Game)")]
        public static void BuildGame() => Build(menu: false);

        [MenuItem("Tools/King Hunt/Build Cartoon Environment (Menu)")]
        public static void BuildMenu() => Build(menu: true);

        public static void Build(bool menu)
        {
            EnsureAssets();
            var rng = new System.Random(menu ? 7 : 42);

            GameObject old = GameObject.Find("Environment");
            if (old != null) Object.DestroyImmediate(old);
            var env = new GameObject("Environment").transform;

            SetupSkyAndLight();
            SetupPostFx(env);
            SetupCameras();

            // Vùng chơi: game = 2 bàn tại z = ±6 (tổng 10 x 22), menu = 1 bàn ở gốc.
            Vector2 half = menu ? new Vector2(6f, 6f) : new Vector2(6f, 12f);
            BuildIsland(env, half);
            StyleBoards(env);
            if (!menu) BuildFlags(env);
            BuildNature(env, half, rng, menu);
            BuildClouds(env, rng);

            foreach (Collider c in env.GetComponentsInChildren<Collider>(true)) Object.DestroyImmediate(c);
            EditorSceneManager.MarkSceneDirty(EditorSceneManager.GetActiveScene());
        }

        // ---------------- Asset dùng chung ----------------

        private static readonly Dictionary<string, Material> Mats = new();

        private static void EnsureAssets()
        {
            Directory.CreateDirectory(MatDir);
            Directory.CreateDirectory(TexDir);
            Directory.CreateDirectory(MeshDir);
            Directory.CreateDirectory("Assets/Resources");
            AssetDatabase.Refresh();

            var cartoon = AssetDatabase.LoadAssetAtPath<CartoonAssets>("Assets/Resources/CartoonAssets.asset");
            if (cartoon == null)
            {
                cartoon = ScriptableObject.CreateInstance<CartoonAssets>();
                AssetDatabase.CreateAsset(cartoon, "Assets/Resources/CartoonAssets.asset");
            }
            cartoon.ToonShader = Shader.Find("KingHunt/Toon");
            cartoon.StarSprite = AssetDatabase.LoadAssetAtPath<Sprite>("Assets/AIModels/CardSprites/Sparkle_Star.png");
            cartoon.PuffMaterial = ParticleMaterial("FX_Puff", SoftCircleTexture());
            cartoon.StarMaterial = ParticleMaterial("FX_Star", cartoon.StarSprite != null ? cartoon.StarSprite.texture : SoftCircleTexture());
            EditorUtility.SetDirty(cartoon);
            AssetDatabase.SaveAssets();
        }

        private static Texture2D SoftCircleTexture()
        {
            string path = TexDir + "/SoftCircle.png";
            var tex = AssetDatabase.LoadAssetAtPath<Texture2D>(path);
            if (tex != null) return tex;

            const int size = 64;
            var t = new Texture2D(size, size, TextureFormat.RGBA32, false);
            for (int y = 0; y < size; y++)
                for (int x = 0; x < size; x++)
                {
                    float d = Vector2.Distance(new Vector2(x + 0.5f, y + 0.5f), new Vector2(size / 2f, size / 2f)) / (size / 2f);
                    float a = Mathf.Clamp01(1f - d);
                    a = Mathf.SmoothStep(0f, 1f, a * 1.6f);
                    t.SetPixel(x, y, new Color(1f, 1f, 1f, a));
                }
            File.WriteAllBytes(path, t.EncodeToPNG());
            Object.DestroyImmediate(t);
            AssetDatabase.ImportAsset(path);
            var ti = (TextureImporter)AssetImporter.GetAtPath(path);
            ti.alphaIsTransparency = true;
            ti.mipmapEnabled = false;
            ti.wrapMode = TextureWrapMode.Clamp;
            ti.SaveAndReimport();
            return AssetDatabase.LoadAssetAtPath<Texture2D>(path);
        }

        private static Material ParticleMaterial(string name, Texture tex)
        {
            string path = $"{MatDir}/{name}.mat";
            var m = AssetDatabase.LoadAssetAtPath<Material>(path);
            if (m == null)
            {
                m = new Material(Shader.Find("Universal Render Pipeline/Particles/Unlit"));
                AssetDatabase.CreateAsset(m, path);
            }
            m.SetTexture("_BaseMap", tex);
            m.SetColor("_BaseColor", Color.white);
            m.SetFloat("_Surface", 1f);
            m.SetFloat("_Blend", 0f);
            m.SetFloat("_SrcBlend", (float)BlendMode.SrcAlpha);
            m.SetFloat("_DstBlend", (float)BlendMode.OneMinusSrcAlpha);
            m.SetFloat("_ZWrite", 0f);
            m.EnableKeyword("_SURFACE_TYPE_TRANSPARENT");
            m.renderQueue = (int)RenderQueue.Transparent;
            EditorUtility.SetDirty(m);
            return m;
        }

        /// <summary>Material toon dùng chung, tạo 1 lần theo tên.</summary>
        private static Material Toon(string name, Color color, float outline = 0.0025f, float shadeStep = 0f)
        {
            if (Mats.TryGetValue(name, out Material cached) && cached != null) return cached;
            string path = $"{MatDir}/{name}.mat";
            var m = AssetDatabase.LoadAssetAtPath<Material>(path);
            if (m == null)
            {
                m = new Material(Shader.Find("KingHunt/Toon"));
                AssetDatabase.CreateAsset(m, path);
            }
            m.shader = Shader.Find("KingHunt/Toon");
            m.SetColor("_BaseColor", color);
            m.SetFloat("_OutlineWidth", outline);
            m.SetFloat("_ShadeStep", shadeStep);
            m.SetColor("_ShadeColor", new Color(0.68f, 0.64f, 0.82f));
            // Highlight cứng trên mặt phẳng lớn (cỏ, bàn) loang thành 1 vệt tròn khổng lồ -> tắt cho môi trường.
            m.SetFloat("_HighlightStrength", 0f);
            m.SetFloat("_RimStrength", 0.15f);
            m.SetFloat("_OutlineSmoothNormals", 1f); // mọi primitive môi trường dùng mesh đã bake (SmoothMesh)
            m.enableInstancing = true;
            EditorUtility.SetDirty(m);
            Mats[name] = m;
            return m;
        }

        // ---------------- Trời, ánh sáng, post FX ----------------

        private static void SetupSkyAndLight()
        {
            string skyPath = $"{MatDir}/Sky_Cartoon.mat";
            var sky = AssetDatabase.LoadAssetAtPath<Material>(skyPath);
            if (sky == null)
            {
                sky = new Material(Shader.Find("KingHunt/SkyGradient"));
                AssetDatabase.CreateAsset(sky, skyPath);
            }
            RenderSettings.skybox = sky;
            RenderSettings.ambientMode = AmbientMode.Trilight;
            RenderSettings.ambientSkyColor = new Color(0.78f, 0.86f, 1f);
            RenderSettings.ambientEquatorColor = new Color(0.86f, 0.84f, 0.78f);
            RenderSettings.ambientGroundColor = new Color(0.45f, 0.52f, 0.38f);
            RenderSettings.fog = true;
            RenderSettings.fogMode = FogMode.Linear;
            RenderSettings.fogColor = new Color(0.84f, 0.93f, 1f);
            RenderSettings.fogStartDistance = 35f;
            RenderSettings.fogEndDistance = 110f;

            Light sun = null;
            foreach (Light l in Object.FindObjectsByType<Light>(FindObjectsInactive.Include, FindObjectsSortMode.None))
                if (l.type == LightType.Directional) { sun = l; break; }
            if (sun == null) sun = new GameObject("Directional Light").AddComponent<Light>();
            sun.type = LightType.Directional;
            sun.color = new Color(1f, 0.95f, 0.86f);
            sun.intensity = 1.15f;
            sun.shadows = LightShadows.Soft;
            sun.shadowStrength = 0.55f;
            sun.transform.rotation = Quaternion.Euler(52f, -35f, 0f);
            RenderSettings.sun = sun;
            sky.SetVector("_SunDir", -sun.transform.forward);
        }

        private static void SetupPostFx(Transform env)
        {
            string path = ArtDir + "/PostFX_Cartoon.asset";
            var profile = AssetDatabase.LoadAssetAtPath<VolumeProfile>(path);
            if (profile == null)
            {
                profile = ScriptableObject.CreateInstance<VolumeProfile>();
                AssetDatabase.CreateAsset(profile, path);

                Bloom bloom = profile.Add<Bloom>(true);
                bloom.threshold.Override(0.95f);
                bloom.intensity.Override(0.45f);
                bloom.scatter.Override(0.6f);

                ColorAdjustments color = profile.Add<ColorAdjustments>(true);
                color.postExposure.Override(0.1f);
                color.contrast.Override(10f);
                color.saturation.Override(22f);

                WhiteBalance wb = profile.Add<WhiteBalance>(true);
                wb.temperature.Override(6f);

                Vignette vignette = profile.Add<Vignette>(true);
                vignette.intensity.Override(0.22f);
                vignette.smoothness.Override(0.45f);

                foreach (VolumeComponent c in profile.components)
                {
                    c.hideFlags = HideFlags.HideInInspector | HideFlags.HideInHierarchy;
                    AssetDatabase.AddObjectToAsset(c, profile);
                }
                AssetDatabase.SaveAssets();
            }

            var go = new GameObject("PostFX");
            go.transform.SetParent(env, false);
            var vol = go.AddComponent<Volume>();
            vol.isGlobal = true;
            vol.sharedProfile = profile;
        }

        private static void SetupCameras()
        {
            foreach (Camera cam in Object.FindObjectsByType<Camera>(FindObjectsInactive.Include, FindObjectsSortMode.None))
            {
                cam.clearFlags = CameraClearFlags.Skybox;
                cam.farClipPlane = Mathf.Max(cam.farClipPlane, 300f);
                var data = cam.GetUniversalAdditionalCameraData();
                data.renderPostProcessing = true;
                data.antialiasing = AntialiasingMode.SubpixelMorphologicalAntiAliasing;
                EditorUtility.SetDirty(cam);
            }
        }

        // ---------------- Đảo, bàn, cờ ----------------

        private static void BuildIsland(Transform env, Vector2 half)
        {
            var island = new GameObject("Island").transform;
            island.SetParent(env, false);

            float rx = half.x + 13f, rz = half.y + 10f;
            // Tầng cỏ trên cùng (mặt ở y = -0.2, ngay dưới mặt bàn).
            Prim(PrimitiveType.Cylinder, island, "GrassTop", new Vector3(0f, -0.45f, 0f), new Vector3(rx * 2f, 0.25f, rz * 2f), Toon("Env_Grass", new Color(0.47f, 0.8f, 0.33f), 0f));
            // Mép cỏ đậm hơn + vách đất bên dưới (nhìn từ cạnh thấy các tầng như bánh).
            Prim(PrimitiveType.Cylinder, island, "GrassLip", new Vector3(0f, -0.75f, 0f), new Vector3(rx * 2f + 0.8f, 0.2f, rz * 2f + 0.8f), Toon("Env_GrassDark", new Color(0.34f, 0.66f, 0.27f), 0f));
            Prim(PrimitiveType.Cylinder, island, "Dirt", new Vector3(0f, -1.6f, 0f), new Vector3(rx * 2f + 0.4f, 0.75f, rz * 2f + 0.4f), Toon("Env_Dirt", new Color(0.62f, 0.4f, 0.24f), 0.002f));
            Prim(PrimitiveType.Cylinder, island, "DirtBottom", new Vector3(0f, -2.6f, 0f), new Vector3(rx * 2f - 3f, 0.5f, rz * 2f - 3f), Toon("Env_DirtDark", new Color(0.48f, 0.3f, 0.18f), 0.002f));

            // Biển.
            GameObject water = Prim(PrimitiveType.Plane, env, "Water", new Vector3(0f, -2.2f, 0f), new Vector3(40f, 1f, 40f), Toon("Env_Water", new Color(0.33f, 0.7f, 0.96f), 0f));
            var mat = water.GetComponent<Renderer>().sharedMaterial;
            mat.SetFloat("_HighlightSize", 0.12f);
            mat.SetFloat("_HighlightStrength", 0.5f);
            water.AddComponent<WaterBob>();
        }

        private static void StyleBoards(Transform env)
        {
            // Bàn giữ màu xám như bản gốc (đá xám), viền và trụ xám đậm hơn, núm bạc.
            Material wood = Toon("Env_BoardGray", new Color(0.6f, 0.62f, 0.66f), 0.003f);
            wood.SetFloat("_OutlineSmoothNormals", 0f); // mesh bàn từ FBX, không bake
            Material woodDark = Toon("Env_BoardGrayDark", new Color(0.42f, 0.44f, 0.49f), 0.003f);
            Material knob = Toon("Env_Silver", new Color(0.85f, 0.87f, 0.92f), 0.003f);

            foreach (GridSystem grid in Object.FindObjectsByType<GridSystem>(FindObjectsInactive.Include, FindObjectsSortMode.None))
            {
                // Mặt bàn: gỗ sáng. Viền gỗ đậm to hơn 1 chút, có 4 chân trụ ở góc.
                foreach (Renderer r in grid.GetComponentsInChildren<Renderer>(true))
                    if (r.name != "GridCollider") r.sharedMaterial = wood;

                Vector3 c = grid.transform.position;
                float s = grid.Size * grid.CellSize;
                Prim(PrimitiveType.Cube, env, "BoardRim", c + new Vector3(0f, -0.2f, 0f), new Vector3(s + 0.7f, 0.25f, s + 0.7f), woodDark);
                for (int i = 0; i < 4; i++)
                {
                    float x = (i % 2 == 0 ? -1 : 1) * (s / 2f + 0.2f);
                    float z = (i < 2 ? -1 : 1) * (s / 2f + 0.2f);
                    Prim(PrimitiveType.Cylinder, env, "BoardPost", c + new Vector3(x, -0.05f, z), new Vector3(0.45f, 0.22f, 0.45f), woodDark);
                    Prim(PrimitiveType.Sphere, env, "BoardKnob", c + new Vector3(x, 0.22f, z), Vector3.one * 0.4f, knob);
                }

                var so = new SerializedObject(grid);
                so.FindProperty("_colorA").colorValue = new Color(1f, 1f, 1f);
                so.FindProperty("_colorB").colorValue = new Color(0.88f, 0.89f, 0.92f);
                so.ApplyModifiedProperties();
            }
        }

        private static void BuildFlags(Transform env)
        {
            Material pole = Toon("Env_Pole", new Color(0.92f, 0.9f, 0.85f), 0.003f);
            foreach (Player p in Object.FindObjectsByType<Player>(FindObjectsInactive.Include, FindObjectsSortMode.None))
            {
                if (p.Board == null) continue;
                bool p1 = p.DisplayName.Contains("1");
                Material cloth = p1 ? Toon("Env_FlagRed", new Color(0.95f, 0.3f, 0.3f), 0.003f) : Toon("Env_FlagBlue", new Color(0.3f, 0.55f, 0.98f), 0.003f);
                Vector3 c = p.Board.transform.position;
                float s = p.Board.Size * p.Board.CellSize / 2f + 0.9f;
                foreach (Vector3 corner in new[] { new Vector3(-s, 0, -s), new Vector3(s, 0, -s), new Vector3(-s, 0, s), new Vector3(s, 0, s) })
                {
                    var flag = new GameObject($"Flag_{p.DisplayName}").transform;
                    flag.SetParent(env, false);
                    flag.position = c + corner;
                    Prim(PrimitiveType.Cylinder, flag, "Pole", new Vector3(0f, 1.1f, 0f), new Vector3(0.09f, 1.3f, 0.09f), pole);
                    Prim(PrimitiveType.Sphere, flag, "Top", new Vector3(0f, 2.45f, 0f), Vector3.one * 0.2f, Toon("Env_Gold", Color.yellow));
                    var clothPivot = new GameObject("ClothPivot").transform;
                    clothPivot.SetParent(flag, false);
                    clothPivot.localPosition = new Vector3(0f, 2.1f, 0f);
                    Prim(PrimitiveType.Cube, clothPivot, "Cloth", new Vector3(0.4f, 0f, 0f), new Vector3(0.75f, 0.45f, 0.04f), cloth);
                    clothPivot.gameObject.AddComponent<FlagWave>();
                }
            }
        }

        // ---------------- Cây cối, đá, hoa, mây ----------------

        private static void BuildNature(Transform env, Vector2 half, System.Random rng, bool menu)
        {
            var nature = new GameObject("Nature").transform;
            nature.SetParent(env, false);
            float rx = half.x + 13f, rz = half.y + 10f;

            Material trunk = Toon("Env_Trunk", new Color(0.5f, 0.32f, 0.18f), 0.003f);
            Material[] leaves =
            {
                Toon("Env_Leaves1", new Color(0.3f, 0.72f, 0.3f), 0.003f),
                Toon("Env_Leaves2", new Color(0.45f, 0.82f, 0.28f), 0.003f),
                Toon("Env_Leaves3", new Color(0.22f, 0.6f, 0.35f), 0.003f),
            };
            Material rock = Toon("Env_Rock", new Color(0.66f, 0.66f, 0.72f), 0.003f, 0.2f);
            Material[] petals =
            {
                Toon("Env_FlowerPink", new Color(1f, 0.55f, 0.75f), 0.002f),
                Toon("Env_FlowerYellow", new Color(1f, 0.88f, 0.3f), 0.002f),
                Toon("Env_FlowerWhite", new Color(0.98f, 0.98f, 0.95f), 0.002f),
            };
            Material stem = Toon("Env_Stem", new Color(0.3f, 0.6f, 0.25f), 0f);

            // Cây: đặt trong vành đảo, ngoài vùng bàn, tránh đường nhìn của các camera.
            int trees = menu ? 14 : 26, placed = 0, guard = 0;
            var used = new List<Vector3>();
            while (placed < trees && guard++ < 2000)
            {
                Vector3 p = RandomOnIsland(rng, rx - 1.5f, rz - 1.5f);
                if (!FreeSpot(p, half, used, 2.6f, menu)) continue;
                used.Add(p);
                BuildTree(nature, p, rng, trunk, leaves[rng.Next(leaves.Length)]);
                placed++;
            }

            // Bụi cây + đá.
            for (int i = 0, n = 0; n < (menu ? 10 : 20) && i < 2000; i++)
            {
                Vector3 p = RandomOnIsland(rng, rx - 0.8f, rz - 0.8f);
                if (!FreeSpot(p, half, used, 1.4f, menu)) continue;
                used.Add(p);
                if (n % 2 == 0) BuildBush(nature, p, rng, leaves[rng.Next(leaves.Length)]);
                else BuildRock(nature, p, rng, rock);
                n++;
            }

            // Hoa nhỏ rải rác (được phép gần bàn hơn, không cản tầm nhìn).
            for (int i = 0, n = 0; n < (menu ? 30 : 60) && i < 4000; i++)
            {
                Vector3 p = RandomOnIsland(rng, rx - 0.5f, rz - 0.5f);
                if (Mathf.Abs(p.x) < half.x + 0.6f && Mathf.Abs(p.z) < half.y + 0.6f) continue;
                var f = new GameObject("Flower").transform;
                f.SetParent(nature, false);
                f.position = p;
                float h = 0.15f + (float)rng.NextDouble() * 0.15f;
                Prim(PrimitiveType.Cylinder, f, "Stem", new Vector3(0f, h * 0.5f, 0f), new Vector3(0.04f, h * 0.5f, 0.04f), stem);
                Prim(PrimitiveType.Sphere, f, "Petals", new Vector3(0f, h, 0f), new Vector3(0.22f, 0.12f, 0.22f), petals[rng.Next(petals.Length)]);
                n++;
            }
        }

        private static Vector3 RandomOnIsland(System.Random rng, float rx, float rz)
        {
            double a = rng.NextDouble() * Mathf.PI * 2.0;
            double r = Mathf.Sqrt((float)rng.NextDouble());
            return new Vector3((float)(System.Math.Cos(a) * r) * rx, -0.2f, (float)(System.Math.Sin(a) * r) * rz);
        }

        /// <summary>Ngoài vùng bàn (+ lề), cách các vật khác, không chắn tầm nhìn camera.</summary>
        private static bool FreeSpot(Vector3 p, Vector2 half, List<Vector3> used, float spacing, bool menu)
        {
            if (Mathf.Abs(p.x) < half.x + 1.5f && Mathf.Abs(p.z) < half.y + 1.5f) return false;
            if (!menu)
            {
                // Cam1/Cam2 nhìn dọc trục z từ z = ±13; Cam3 nhìn từ x = -12. Chừa hành lang trước các camera.
                if (Mathf.Abs(p.x) < 4.5f && Mathf.Abs(p.z) > half.y) return false;
                if (p.x < -half.x && Mathf.Abs(p.z) < 5f) return false;
            }
            else if (p.z < -half.y && Mathf.Abs(p.x) < 5f) return false;
            foreach (Vector3 u in used)
                if ((u - p).sqrMagnitude < spacing * spacing) return false;
            return true;
        }

        private static void BuildTree(Transform parent, Vector3 pos, System.Random rng, Material trunk, Material leaves)
        {
            var tree = new GameObject("Tree").transform;
            tree.SetParent(parent, false);
            tree.position = pos;
            tree.rotation = Quaternion.Euler(0f, (float)rng.NextDouble() * 360f, 0f);
            float s = 0.8f + (float)rng.NextDouble() * 0.6f;
            tree.localScale = Vector3.one * s;

            Prim(PrimitiveType.Cylinder, tree, "Trunk", new Vector3(0f, 0.8f, 0f), new Vector3(0.35f, 0.8f, 0.35f), trunk);
            var crown = new GameObject("Crown").transform;
            crown.SetParent(tree, false);
            crown.localPosition = new Vector3(0f, 1.6f, 0f);
            Prim(PrimitiveType.Sphere, crown, "Leaf", new Vector3(0f, 0.55f, 0f), Vector3.one * 1.7f, leaves);
            Prim(PrimitiveType.Sphere, crown, "Leaf", new Vector3(0.55f, 0.25f, 0.2f), Vector3.one * 1.15f, leaves);
            Prim(PrimitiveType.Sphere, crown, "Leaf", new Vector3(-0.5f, 0.3f, -0.25f), Vector3.one * 1.2f, leaves);
            Prim(PrimitiveType.Sphere, crown, "Leaf", new Vector3(0f, 1.25f, 0f), Vector3.one * 1.0f, leaves);
            crown.gameObject.AddComponent<Sway>();
        }

        private static void BuildBush(Transform parent, Vector3 pos, System.Random rng, Material leaves)
        {
            var bush = new GameObject("Bush").transform;
            bush.SetParent(parent, false);
            bush.position = pos;
            float s = 0.6f + (float)rng.NextDouble() * 0.5f;
            bush.localScale = Vector3.one * s;
            Prim(PrimitiveType.Sphere, bush, "Ball", new Vector3(0f, 0.3f, 0f), new Vector3(1f, 0.8f, 1f), leaves);
            Prim(PrimitiveType.Sphere, bush, "Ball", new Vector3(0.45f, 0.2f, 0.1f), new Vector3(0.7f, 0.6f, 0.7f), leaves);
            Prim(PrimitiveType.Sphere, bush, "Ball", new Vector3(-0.4f, 0.2f, -0.15f), new Vector3(0.65f, 0.55f, 0.65f), leaves);
            bush.gameObject.AddComponent<Sway>().Amplitude = 1.5f;
        }

        private static void BuildRock(Transform parent, Vector3 pos, System.Random rng, Material rock)
        {
            var r = new GameObject("Rock").transform;
            r.SetParent(parent, false);
            r.position = pos;
            r.rotation = Quaternion.Euler(0f, (float)rng.NextDouble() * 360f, 0f);
            float s = 0.5f + (float)rng.NextDouble() * 0.7f;
            Prim(PrimitiveType.Sphere, r, "Stone", new Vector3(0f, 0.15f * s, 0f), new Vector3(1.2f, 0.7f, 0.9f) * s, rock);
            Prim(PrimitiveType.Sphere, r, "Stone", new Vector3(0.45f * s, 0.08f * s, 0.25f * s), new Vector3(0.6f, 0.4f, 0.5f) * s, rock);
        }

        private static void BuildClouds(Transform env, System.Random rng)
        {
            Material cloud = Toon("Env_Cloud", new Color(1f, 1f, 1f), 0f, -0.3f);
            cloud.SetColor("_ShadeColor", new Color(0.82f, 0.88f, 1f));
            var root = new GameObject("Clouds").transform;
            root.SetParent(env, false);
            for (int i = 0; i < 9; i++)
            {
                var c = new GameObject("Cloud").transform;
                c.SetParent(root, false);
                c.position = new Vector3(-40f + i * 10f + (float)rng.NextDouble() * 6f, 13f + (float)rng.NextDouble() * 7f, -30f + (float)rng.NextDouble() * 60f);
                float s = 1.5f + (float)rng.NextDouble() * 1.5f;
                c.localScale = Vector3.one * s;
                int puffs = 3 + rng.Next(3);
                for (int k = 0; k < puffs; k++)
                {
                    float x = (k - (puffs - 1) * 0.5f) * 1.1f;
                    float size = k == puffs / 2 ? 2.2f : 1.4f + (float)rng.NextDouble() * 0.5f;
                    Prim(PrimitiveType.Sphere, c, "Puff", new Vector3(x, size * 0.15f, (float)rng.NextDouble() * 0.6f), new Vector3(size, size * 0.75f, size), cloud);
                }
                var drift = c.gameObject.AddComponent<CloudDrift>();
                drift.Speed = 0.4f + (float)rng.NextDouble() * 0.5f;
            }
        }

        private static GameObject Prim(PrimitiveType type, Transform parent, string name, Vector3 localPos, Vector3 scale, Material mat)
        {
            GameObject go = GameObject.CreatePrimitive(type);
            go.name = name;
            Object.DestroyImmediate(go.GetComponent<Collider>());
            go.transform.SetParent(parent, false);
            go.transform.localPosition = localPos;
            go.transform.localScale = scale;
            go.GetComponent<MeshFilter>().sharedMesh = SmoothMesh(type, go.GetComponent<MeshFilter>().sharedMesh);
            var r = go.GetComponent<Renderer>();
            r.sharedMaterial = mat;
            return go;
        }

        private static readonly Dictionary<PrimitiveType, Mesh> SmoothMeshes = new();

        /// <summary>Bản sao primitive có pháp tuyến mượt trong tangent (viền không hở ở cạnh hộp/trụ), lưu thành asset.</summary>
        private static Mesh SmoothMesh(PrimitiveType type, Mesh builtin)
        {
            if (SmoothMeshes.TryGetValue(type, out Mesh m) && m != null) return m;
            string path = $"{MeshDir}/Smooth_{type}.asset";
            m = AssetDatabase.LoadAssetAtPath<Mesh>(path);
            if (m == null)
            {
                m = OutlineNormals.Bake(builtin);
                AssetDatabase.CreateAsset(m, path);
            }
            SmoothMeshes[type] = m;
            return m;
        }
    }
}
#endif
