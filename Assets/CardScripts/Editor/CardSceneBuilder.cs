#if UNITY_EDITOR
using System;
using System.Collections.Generic;
using System.IO;
using UnityEditor;
using UnityEditor.SceneManagement;
using UnityEngine;
using UnityEngine.EventSystems;
using UnityEngine.UI;

namespace CardScripts.Editor
{
    /// <summary>
    /// Script Editor tự động kiến tạo Scene 'CardScene' và lưu trực tiếp vào thư mục Assets/Scenes/
    /// (Không tạo menu trên thanh công cụ theo đúng yêu cầu người dùng):
    /// - Tự động thiết lập Sprite cho 4 lá bài (Knight, Mage, King, Mirror) và hiệu ứng hạt sao
    /// - Kích thước lá bài lớn hơn (230x345 px) chuẩn phong cách Balatro
    /// - Hiệu ứng quét vệt sáng (Shine Shimmer Sweep) khi lướt chuột
    /// - Hệ thống hạt sao lấp lánh (Sparkling Stars) rơi nhẹ xung quanh
    /// - Khi bấm vào mỗi lá bài: Triệu hồi chính xác thực thể tương ứng:
    ///   + Lá Knight -> Knight (KnightEntity.prefab từ Knight.fbx)
    ///   + Lá Mage   -> Mage (MageEntity.prefab từ Mage.fbx)
    ///   + Lá King   -> King (KingEntity.prefab từ King.fbx)
    ///   + Lá Mirror -> Entity Mirror (Mirror.prefab của người dùng)
    /// </summary>
    [InitializeOnLoad]
    public static class CardSceneBuilder
    {
        private const string ScenePath = "Assets/Scenes/CardScene.unity";
        private const string ModelsFolder = "Assets/AIModels";
        private const string SpritesFolder = "Assets/AIModels/CardSprites";
        private const string CubePrefabPath = "Assets/AIModels/DemoCube.prefab";

        private const string KnightPrefabPath = "Assets/AIModels/KnightEntity.prefab";
        private const string MagePrefabPath = "Assets/AIModels/MageEntity.prefab";
        private const string KingPrefabPath = "Assets/AIModels/KingEntity.prefab";
        private const string MirrorPrefabPath = "Assets/Entity/Mirror.prefab";

        static CardSceneBuilder()
        {
            EditorApplication.delayCall += () =>
            {
                // Tự động kiểm tra và cập nhật Scene nếu cần
                BuildScene();
            };
        }

        public static void BuildScene()
        {
            EnsureFolders();
            ConfigureAllSprites();

            // 1. Tạo hoặc tải các Prefab thực thể tương ứng cho từng lá bài
            GameObject cubePrefab = CreateOrUpdateCubePrefab();
            GameObject knightPrefab = GetOrCreateEntityPrefab("Assets/Knight.fbx", KnightPrefabPath, new Vector3(0, 1.05f, 0), 2.1f, 0.5f);
            GameObject magePrefab = GetOrCreateEntityPrefab("Assets/Mage.fbx", MagePrefabPath, new Vector3(0, 1.15f, 0), 2.3f, 0.5f);
            GameObject kingPrefab = GetOrCreateEntityPrefab("Assets/King.fbx", KingPrefabPath, new Vector3(0, 1.0f, 0), 2.0f, 0.5f);
            GameObject mirrorPrefab = AssetDatabase.LoadAssetAtPath<GameObject>(MirrorPrefabPath);

            // 2. Dựng và lưu Scene
            BuildAndSaveScene(cubePrefab, knightPrefab, magePrefab, kingPrefab, mirrorPrefab);
        }

        private static void EnsureFolders()
        {
            if (!AssetDatabase.IsValidFolder("Assets/Scenes"))
            {
                AssetDatabase.CreateFolder("Assets", "Scenes");
            }
            if (!AssetDatabase.IsValidFolder(ModelsFolder))
            {
                AssetDatabase.CreateFolder("Assets", "AIModels");
            }
            if (!AssetDatabase.IsValidFolder(SpritesFolder))
            {
                AssetDatabase.CreateFolder(ModelsFolder, "CardSprites");
            }
        }

        private static void ConfigureAllSprites()
        {
            string[] spriteFiles = new string[]
            {
                $"{SpritesFolder}/Card_Knight.png",
                $"{SpritesFolder}/Card_Mage.png",
                $"{SpritesFolder}/Card_King.png",
                $"{SpritesFolder}/Card_Mirror.png",
                $"{SpritesFolder}/Sparkle_Star.png",
                $"{SpritesFolder}/Card_Shine.png"
            };

            foreach (var path in spriteFiles)
            {
                TextureImporter ti = AssetImporter.GetAtPath(path) as TextureImporter;
                if (ti != null)
                {
                    bool modified = false;
                    if (ti.textureType != TextureImporterType.Sprite)
                    {
                        ti.textureType = TextureImporterType.Sprite;
                        ti.spriteImportMode = SpriteImportMode.Single;
                        modified = true;
                    }
                    if (!ti.alphaIsTransparency)
                    {
                        ti.alphaIsTransparency = true;
                        modified = true;
                    }
                    if (modified)
                    {
                        ti.SaveAndReimport();
                    }
                }
            }
        }

        private static Sprite LoadSprite(string path)
        {
            return AssetDatabase.LoadAssetAtPath<Sprite>(path);
        }

        private static GameObject GetOrCreateEntityPrefab(string fbxPath, string prefabPath, Vector3 colCenter, float colHeight, float colRadius)
        {
            GameObject fbx = AssetDatabase.LoadAssetAtPath<GameObject>(fbxPath);
            if (fbx == null)
            {
                Debug.LogWarning($"[CardSceneBuilder] Không tìm thấy file model {fbxPath}");
                return null;
            }

            GameObject instance = PrefabUtility.InstantiatePrefab(fbx) as GameObject;
            if (instance == null)
            {
                instance = GameObject.Instantiate(fbx);
            }

            instance.name = Path.GetFileNameWithoutExtension(prefabPath);

            // Gắn CapsuleCollider để thực thể tương tác với không gian 3D
            CapsuleCollider col = instance.GetComponent<CapsuleCollider>();
            if (col == null) col = instance.AddComponent<CapsuleCollider>();
            col.center = colCenter;
            col.height = colHeight;
            col.radius = colRadius;

            // Gắn CardEntityFlight xử lý hiệu ứng bay vòng cung Parabol và tiếp đất nảy đàn hồi
            if (instance.GetComponent<CardEntityFlight>() == null)
            {
                instance.AddComponent<CardEntityFlight>();
            }

            GameObject prefab = PrefabUtility.SaveAsPrefabAsset(instance, prefabPath);
            GameObject.DestroyImmediate(instance);
            AssetDatabase.SaveAssets();

            return prefab;
        }

        private static GameObject CreateOrUpdateCubePrefab()
        {
            GameObject tempCube = GameObject.CreatePrimitive(PrimitiveType.Cube);
            tempCube.name = "DemoCube";
            tempCube.transform.localScale = Vector3.one * 0.95f;

            Rigidbody rb = tempCube.GetComponent<Rigidbody>();
            if (rb == null) rb = tempCube.AddComponent<Rigidbody>();
            rb.mass = 2.5f;
            rb.collisionDetectionMode = CollisionDetectionMode.Continuous;

            if (tempCube.GetComponent<FlyingCube>() == null)
            {
                tempCube.AddComponent<FlyingCube>();
            }
            if (tempCube.GetComponent<CardEntityFlight>() == null)
            {
                tempCube.AddComponent<CardEntityFlight>();
            }

            Material cubeMat = new Material(Shader.Find("Universal Render Pipeline/Lit") ?? Shader.Find("Standard"));
            cubeMat.color = new Color(0.2f, 0.8f, 1f, 1f); // Xanh ngọc phát sáng
            if (cubeMat.HasProperty("_EmissionColor"))
            {
                cubeMat.EnableKeyword("_EMISSION");
                cubeMat.SetColor("_EmissionColor", new Color(0.08f, 0.35f, 0.5f));
            }
            string matPath = $"{ModelsFolder}/CubeMaterial.mat";
            AssetDatabase.CreateAsset(cubeMat, matPath);
            tempCube.GetComponent<Renderer>().sharedMaterial = cubeMat;

            GameObject prefab = PrefabUtility.SaveAsPrefabAsset(tempCube, CubePrefabPath);
            GameObject.DestroyImmediate(tempCube);
            AssetDatabase.SaveAssets();

            return prefab;
        }

        private static void BuildAndSaveScene(
            GameObject cubePrefab,
            GameObject knightPrefab,
            GameObject magePrefab,
            GameObject kingPrefab,
            GameObject mirrorPrefab)
        {
            var scene = EditorSceneManager.NewScene(NewSceneSetup.EmptyScene, NewSceneMode.Single);

            // 1. Camera
            GameObject camObj = new GameObject("Main Camera");
            camObj.tag = "MainCamera";
            Camera cam = camObj.AddComponent<Camera>();
            camObj.transform.position = new Vector3(0f, 3.5f, -7.5f);
            camObj.transform.rotation = Quaternion.Euler(20f, 0f, 0f);
            cam.clearFlags = CameraClearFlags.SolidColor;
            cam.backgroundColor = new Color(0.07f, 0.08f, 0.12f, 1f);
            camObj.AddComponent<AudioListener>();

            // 2. Directional Light
            GameObject lightObj = new GameObject("Directional Light");
            Light light = lightObj.AddComponent<Light>();
            light.type = LightType.Directional;
            light.intensity = 1.35f;
            light.color = new Color(1f, 0.97f, 0.92f);
            lightObj.transform.rotation = Quaternion.Euler(45f, -30f, 0f);

            // 3. Ground Arena
            GameObject ground = GameObject.CreatePrimitive(PrimitiveType.Cube);
            ground.name = "ArenaGround";
            ground.transform.position = new Vector3(0f, -0.5f, 2f);
            ground.transform.localScale = new Vector3(16f, 1f, 14f);
            Material groundMat = new Material(Shader.Find("Universal Render Pipeline/Lit") ?? Shader.Find("Standard"));
            groundMat.color = new Color(0.11f, 0.13f, 0.17f);
            string groundMatPath = $"{ModelsFolder}/ArenaGroundMat.mat";
            AssetDatabase.CreateAsset(groundMat, groundMatPath);
            ground.GetComponent<Renderer>().sharedMaterial = groundMat;

            // 4. EventSystem
            GameObject eventSystemObj = new GameObject("EventSystem");
            eventSystemObj.AddComponent<EventSystem>();
            Type inputSystemModuleType = Type.GetType("UnityEngine.InputSystem.UI.InputSystemUIInputModule, Unity.InputSystem");
            if (inputSystemModuleType != null)
            {
                eventSystemObj.AddComponent(inputSystemModuleType);
            }
            else
            {
                eventSystemObj.AddComponent<StandaloneInputModule>();
            }

            // 5. Canvas UI
            GameObject canvasObj = new GameObject("CardCanvas");
            Canvas canvas = canvasObj.AddComponent<Canvas>();
            canvas.renderMode = RenderMode.ScreenSpaceOverlay;
            CanvasScaler scaler = canvasObj.AddComponent<CanvasScaler>();
            scaler.uiScaleMode = CanvasScaler.ScaleMode.ScaleWithScreenSize;
            scaler.referenceResolution = new Vector2(1920, 1080);
            scaler.matchWidthOrHeight = 0.5f;
            canvasObj.AddComponent<GraphicRaycaster>();

            // 6. Card Hand Container (Cạnh dưới màn hình)
            GameObject handContainer = new GameObject("CardHandContainer");
            handContainer.transform.SetParent(canvasObj.transform, false);
            RectTransform handRect = handContainer.AddComponent<RectTransform>();
            handRect.anchorMin = new Vector2(0.5f, 0f);
            handRect.anchorMax = new Vector2(0.5f, 0f);
            handRect.pivot = new Vector2(0.5f, 0f);
            handRect.anchoredPosition = new Vector2(0f, 40f);
            handRect.sizeDelta = new Vector2(1200f, 380f);

            // 7. Tạo 4 lá bài to và đẹp: Knight, Mage, King, Mirror
            List<CardView> cardList = new List<CardView>();
            float[] xOffsets = new float[] { -390f, -130f, 130f, 390f };
            string[] spriteNames = new string[]
            {
                "Card_Knight.png",
                "Card_Mage.png",
                "Card_King.png",
                "Card_Mirror.png"
            };
            GameObject[] entityPrefabs = new GameObject[]
            {
                knightPrefab,
                magePrefab,
                kingPrefab,
                mirrorPrefab
            };
            Color[] accentGlows = new Color[]
            {
                new Color(1f, 0.35f, 0.35f),  // Đỏ Hiệp sĩ
                new Color(0.35f, 0.8f, 1f),    // Xanh Cyan Pháp sư
                new Color(0.95f, 0.85f, 0.3f), // Vàng Hoàng Gia
                new Color(0.72f, 0.55f, 1f)    // Tím Ma thuật Chiếc Gương
            };

            Sprite starSprite = LoadSprite($"{SpritesFolder}/Sparkle_Star.png");
            Sprite shineSprite = LoadSprite($"{SpritesFolder}/Card_Shine.png");

            for (int i = 0; i < 4; i++)
            {
                Sprite cardSprite = LoadSprite($"{SpritesFolder}/{spriteNames[i]}");
                GameObject cardObj = CreateLargeCardUI(
                    handContainer.transform, 
                    i + 1, 
                    xOffsets[i], 
                    cardSprite, 
                    shineSprite, 
                    starSprite, 
                    accentGlows[i],
                    entityPrefabs[i]
                );
                CardView cardView = cardObj.GetComponent<CardView>();
                cardList.Add(cardView);
            }

            // 8. CardManager
            GameObject managerObj = new GameObject("CardManager");
            CardManager cardManager = managerObj.AddComponent<CardManager>();
            SerializedObject so = new SerializedObject(cardManager);
            so.FindProperty("cubePrefab").objectReferenceValue = cubePrefab;
            so.FindProperty("mainCamera").objectReferenceValue = cam;
            so.FindProperty("landingCenter").objectReferenceValue = ground.transform;

            SerializedProperty cardsProp = so.FindProperty("activeCards");
            cardsProp.ClearArray();
            for (int i = 0; i < cardList.Count; i++)
            {
                cardsProp.InsertArrayElementAtIndex(i);
                cardsProp.GetArrayElementAtIndex(i).objectReferenceValue = cardList[i];
            }
            so.ApplyModifiedProperties();

            EditorSceneManager.SaveScene(scene, ScenePath);
            AssetDatabase.Refresh();
        }

        private static GameObject CreateLargeCardUI(
            Transform parent, 
            int index, 
            float xPos, 
            Sprite cardSprite, 
            Sprite shineSprite, 
            Sprite starSprite, 
            Color glowColor,
            GameObject entityPrefab)
        {
            GameObject cardObj = new GameObject($"Card_{index}");
            cardObj.transform.SetParent(parent, false);

            // Kích thước to hơn đáng kể (230 x 345)
            RectTransform rt = cardObj.AddComponent<RectTransform>();
            rt.anchorMin = new Vector2(0.5f, 0.5f);
            rt.anchorMax = new Vector2(0.5f, 0.5f);
            rt.pivot = new Vector2(0.5f, 0.5f);
            rt.sizeDelta = new Vector2(230f, 345f);
            rt.anchoredPosition = new Vector2(xPos, 130f);

            // Nền ngoài (Border viền sáng phát quang nhẹ)
            Image bgBorder = cardObj.AddComponent<Image>();
            bgBorder.color = Color.white;

            // Mặt thẻ bài hiển thị Sprite minh họa
            GameObject artObj = new GameObject("CardArt");
            artObj.transform.SetParent(cardObj.transform, false);
            RectTransform artRt = artObj.AddComponent<RectTransform>();
            artRt.anchorMin = Vector2.zero;
            artRt.anchorMax = Vector2.one;
            artRt.offsetMin = Vector2.zero;
            artRt.offsetMax = Vector2.zero;
            Image artImg = artObj.AddComponent<Image>();
            artImg.sprite = cardSprite;
            artImg.preserveAspect = false;

            // Thêm Mask để vệt sáng quét chỉ hiển thị bên trong khung thẻ bài
            Mask cardMask = cardObj.AddComponent<Mask>();
            cardMask.showMaskGraphic = true;

            // Vệt sáng lấp lánh (Shine Shimmer Sweep)
            GameObject shineObj = new GameObject("ShineSweep");
            shineObj.transform.SetParent(cardObj.transform, false);
            RectTransform shineRt = shineObj.AddComponent<RectTransform>();
            shineRt.anchorMin = new Vector2(0.5f, 0.5f);
            shineRt.anchorMax = new Vector2(0.5f, 0.5f);
            shineRt.pivot = new Vector2(0.5f, 0.5f);
            shineRt.sizeDelta = new Vector2(240f, 450f);
            shineRt.localRotation = Quaternion.Euler(0f, 0f, 25f);
            shineRt.anchoredPosition = new Vector2(-280f, 0f);
            Image shineImg = shineObj.AddComponent<Image>();
            shineImg.sprite = shineSprite;
            shineImg.color = new Color(1f, 1f, 1f, 0.55f);
            shineImg.raycastTarget = false;
            shineObj.SetActive(false);

            // Component quản lý lá bài
            CardView cardView = cardObj.AddComponent<CardView>();
            cardView.SetRestPosition(new Vector2(xPos, 130f));
            cardView.SetSpawnPrefab(entityPrefab);

            // Hiệu ứng hạt sao lấp lánh (Sparkle Stars)
            ParticleSystem sparkle = CreateStarParticleSystem(cardObj.transform, starSprite, glowColor);

            SerializedObject cardSo = new SerializedObject(cardView);
            cardSo.FindProperty("cardArtImage").objectReferenceValue = artImg;
            cardSo.FindProperty("shineImage").objectReferenceValue = shineImg;
            cardSo.FindProperty("sparkleParticle").objectReferenceValue = sparkle;
            cardSo.FindProperty("spawnPrefab").objectReferenceValue = entityPrefab;
            cardSo.ApplyModifiedProperties();

            return cardObj;
        }

        private static ParticleSystem CreateStarParticleSystem(Transform parent, Sprite starSprite, Color glowColor)
        {
            GameObject psObj = new GameObject("SparkleStars");
            psObj.transform.SetParent(parent, false);

            RectTransform psRt = psObj.AddComponent<RectTransform>();
            psRt.anchorMin = Vector2.zero;
            psRt.anchorMax = Vector2.one;
            psRt.offsetMin = Vector2.zero;
            psRt.offsetMax = Vector2.zero;

            ParticleSystem ps = psObj.AddComponent<ParticleSystem>();

            var main = ps.main;
            main.playOnAwake = false;
            main.loop = true;
            main.startLifetime = 1.3f;
            main.startSpeed = new ParticleSystem.MinMaxCurve(10f, 35f);
            main.startSize = new ParticleSystem.MinMaxCurve(22f, 40f);
            main.startColor = new ParticleSystem.MinMaxGradient(Color.white, glowColor);
            main.gravityModifier = -0.05f; // Hạt sao bay lơ lửng và bay nhẹ lên/rơi lấp lánh
            main.simulationSpace = ParticleSystemSimulationSpace.Local;

            var emission = ps.emission;
            emission.rateOverTime = 28f;

            var shape = ps.shape;
            shape.shapeType = ParticleSystemShapeType.Rectangle;
            shape.scale = new Vector3(230f, 345f, 1f);

            var sizeOverLifetime = ps.sizeOverLifetime;
            sizeOverLifetime.enabled = true;
            AnimationCurve curve = new AnimationCurve();
            curve.AddKey(0f, 0.2f);
            curve.AddKey(0.4f, 1.2f); // Phóng to chớp sáng ở giữa đời sống
            curve.AddKey(1f, 0f);
            sizeOverLifetime.size = new ParticleSystem.MinMaxCurve(1f, curve);

            var colorOverLifetime = ps.colorOverLifetime;
            colorOverLifetime.enabled = true;
            Gradient grad = new Gradient();
            grad.SetKeys(
                new GradientColorKey[] { new GradientColorKey(Color.white, 0f), new GradientColorKey(glowColor, 1f) },
                new GradientAlphaKey[] { new GradientAlphaKey(0f, 0f), new GradientAlphaKey(1f, 0.3f), new GradientAlphaKey(0f, 1f) }
            );
            colorOverLifetime.color = grad;

            // Xoay tròn hạt sao khi bay
            var rot = ps.rotationOverLifetime;
            rot.enabled = true;
            rot.z = new ParticleSystem.MinMaxCurve(-90f, 90f);

            var psRenderer = psObj.GetComponent<ParticleSystemRenderer>();
            Material particleMat = new Material(Shader.Find("Universal Render Pipeline/Particles/Unlit") ?? Shader.Find("Particles/Standard Unlit") ?? Shader.Find("Sprites/Default"));
            if (starSprite != null)
            {
                particleMat.mainTexture = starSprite.texture;
            }
            psRenderer.material = particleMat;

            return ps;
        }
    }
}
#endif
