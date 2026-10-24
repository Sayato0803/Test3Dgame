using System;
using System.Collections.Generic;
using DG.Tweening;
using UnityEngine;
using UnityEngine.EventSystems;
using UnityEngine.UI;

namespace GridBattle
{
    /// <summary>
    /// Màn hình menu chính kiểu hybrid-casual: tiêu đề lớn + nút PLAY / SETTINGS / QUIT bên trái, 3 quân đứng bên phải.
    /// Các quân quay đầu + đảo mắt theo con trỏ; rê chuột vào QUIT thì giơ vũ khí, cau mày, rung người vì tức.
    /// UI dựng bằng code qua UiFactory.
    /// </summary>
    public class MainMenu : MonoBehaviour
    {
        [SerializeField] private string _title = "KING HUNT";
        [SerializeField] private string _subtitle = "Hide your Kings. Play your cards. Hunt the enemy King!";
        [SerializeField] private string _gameScene = "GridTest";

        [Header("Nền 3D")]
        [SerializeField] private Camera _camera;
        [SerializeField] private GameObject[] _showcaseModels;
        [SerializeField] private float _unitScale = 0.7f;
        [SerializeField] private float _orbitRadius = 6f;
        [SerializeField] private float _orbitHeight = 2.6f;

        private SettingsPanel _settings;
        private CanvasGroup _buttons;
        private readonly List<CharacterRig> _rigs = new();
        private bool _angry;

        private void Start()
        {
            Time.timeScale = 1f;
            if (_camera == null) _camera = Camera.main;
            SpawnShowcase();
            BuildUi();
        }

        private void SpawnShowcase()
        {
            if (_showcaseModels == null) return;
            int n = _showcaseModels.Length;
            for (int i = 0; i < n; i++)
            {
                if (_showcaseModels[i] == null) continue;
                GameObject go = Instantiate(_showcaseModels[i]);
                go.transform.localScale *= _unitScale;
                ToonStyle.Apply(go);
                // Đứng thành hàng ở nửa phải màn hình, hơi quay vào giữa.
                float x = 1.3f + i * 1.5f;
                go.transform.position = new Vector3(x, 0f, (i - (n - 1) * 0.5f) * 0.4f);
                go.transform.rotation = Quaternion.Euler(0f, 180f + x * 4f, 0f);
                var r = go.AddComponent<UnitReactions>();
                _rigs.Add(go.AddComponent<CharacterRig>());
                r.HideUntilSpawn();
                // Lần lượt nhảy ra chào sân, sau đó thỉnh thoảng nhảy lên.
                DOVirtual.DelayedCall(0.3f + i * 0.2f, r.PlaySpawn).SetTarget(this);
                float period = 3.5f + i * 0.7f;
                DOVirtual.DelayedCall(1.5f + i * 0.9f, () => Hop(go.transform, r, period)).SetTarget(this);
            }
        }

        private void Hop(Transform t, UnitReactions r, float period)
        {
            if (t == null) return;
            if (!_angry)
            {
                r.Visual.DOKill(true);
                r.Visual.DOLocalJump(Vector3.zero, 0.5f, 1, 0.45f).OnComplete(() => r.Land(0f, quiet: true));
            }
            DOVirtual.DelayedCall(period, () => Hop(t, r, period)).SetTarget(this);
        }

        private void Update()
        {
            if (_camera == null) return;
            // Camera đứng trước hàng quân, chỉ đung đưa nhẹ để cảnh không chết.
            float a = (180f + Mathf.Sin(Time.time * 0.3f) * 4f) * Mathf.Deg2Rad;
            var target = new Vector3(0f, 0.9f, 0f);
            _camera.transform.position = target + new Vector3(Mathf.Sin(a) * _orbitRadius, _orbitHeight, Mathf.Cos(a) * _orbitRadius);
            _camera.transform.LookAt(target);

            // Điểm con trỏ trên mặt phẳng đứng (vuông góc camera) đi qua hàng quân.
            Ray ray = _camera.ScreenPointToRay(Input.mousePosition);
            var plane = new Plane(-_camera.transform.forward, new Vector3(2.8f, 1f, 0f));
            Vector3? look = plane.Raycast(ray, out float d) ? ray.GetPoint(d) : null;
            foreach (CharacterRig rig in _rigs)
                if (rig != null) rig.LookTarget = look;
        }

        private void BuildUi()
        {
            Canvas canvas = UiFactory.CreateCanvas("MenuCanvas", 10);
            canvas.transform.SetParent(transform, false);
            Transform root = canvas.transform;

            Text title = UiFactory.Label("Title", root, _title, 150, TextAnchor.MiddleLeft, UiFactory.Accent);
            title.rectTransform.Place(new Vector2(0f, 1f), new Vector2(110f, -170f), new Vector2(1000f, 190f));
            title.GetComponent<Outline>().effectDistance = new Vector2(5f, -5f);
            title.GetComponent<Shadow>().effectDistance = new Vector2(0f, -10f);
            Text sub = UiFactory.Label("Subtitle", root, _subtitle, 34, TextAnchor.MiddleLeft);
            sub.rectTransform.Place(new Vector2(0f, 1f), new Vector2(118f, -365f), new Vector2(1000f, 60f));

            RectTransform list = UiFactory.Rect("Buttons", root).Place(new Vector2(0f, 0.5f), new Vector2(110f, -130f), new Vector2(460f, 420f));
            UiFactory.Vertical(list, 26f);
            UiFactory.Button("Play", list, "PLAY", () => SceneFader.Load(_gameScene), 48).Skin("btn_start").Height(130f);
            UiFactory.Button("Settings", list, "SETTINGS", OpenSettings, 44).Skin("btn_blank_blue", true).Height(110f);
            Button quit = UiFactory.Button("Quit", list, "QUIT", Quit, 44).Skin("btn_blank_red", true).Height(110f);
            quit.gameObject.AddComponent<HoverRelay>().Changed = SetAngry;
            _buttons = UiFactory.Group(list.gameObject, true);

            _settings = new SettingsPanel(root, () => UiFactory.Show(_buttons, true));

            // Hiệu ứng xuất hiện: tiêu đề rơi xuống nảy, nút bật lên lần lượt, tiêu đề nhún nhẹ mãi.
            RectTransform tr = title.rectTransform;
            tr.anchoredPosition += new Vector2(0f, 300f);
            tr.DOAnchorPosY(-170f, 0.7f).SetEase(Ease.OutBounce);
            tr.DOScale(1.03f, 1.2f).SetDelay(0.8f).SetLoops(-1, LoopType.Yoyo).SetEase(Ease.InOutSine);
            sub.color = new Color(sub.color.r, sub.color.g, sub.color.b, 0f);
            sub.DOFade(1f, 0.6f).SetDelay(0.5f);
            for (int i = 0; i < list.childCount; i++)
            {
                Transform b = list.GetChild(i);
                b.localScale = Vector3.zero;
                b.DOScale(1f, 0.4f).SetDelay(0.6f + i * 0.12f).SetEase(Ease.OutBack);
            }
        }

        /// <summary>Rê chuột vào QUIT: cả 3 quân giơ vũ khí, cau mày, rung người.</summary>
        private void SetAngry(bool angry)
        {
            if (_angry == angry) return;
            _angry = angry;
            foreach (CharacterRig rig in _rigs)
            {
                if (rig == null) continue;
                rig.Angry = angry;
                if (angry && rig.TryGetComponent(out UnitReactions r))
                {
                    r.Visual.DOKill(true);
                    r.Visual.DOPunchScale(new Vector3(0.12f, -0.1f, 0.12f), 0.3f, 8, 0.6f);
                }
            }
            if (angry) Sfx.Play(SfxId.Deny, 0.8f, 0.1f);
        }

        private void OpenSettings()
        {
            UiFactory.Show(_buttons, false);
            _settings.Open();
        }

        private static void Quit()
        {
#if UNITY_EDITOR
            UnityEditor.EditorApplication.isPlaying = false;
#else
            Application.Quit();
#endif
        }
    }

    /// <summary>Báo khi con trỏ vào / rời 1 phần tử UI.</summary>
    public class HoverRelay : MonoBehaviour, IPointerEnterHandler, IPointerExitHandler
    {
        public Action<bool> Changed;
        public void OnPointerEnter(PointerEventData e) => Changed?.Invoke(true);
        public void OnPointerExit(PointerEventData e) => Changed?.Invoke(false);
        private void OnDisable() => Changed?.Invoke(false);
    }
}
