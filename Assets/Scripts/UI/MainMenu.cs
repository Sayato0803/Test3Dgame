using DG.Tweening;
using UnityEngine;
using UnityEngine.UI;

namespace GridBattle
{
    /// <summary>
    /// Màn hình menu chính: nền 3D (các quân đứng trên bệ, camera lượn chậm quanh) + nút Chơi / Cài đặt / Thoát.
    /// UI dựng bằng code qua UiFactory.
    /// </summary>
    public class MainMenu : MonoBehaviour
    {
        [SerializeField] private string _title = "KING HUNT";
        [SerializeField] private string _subtitle = "Giấu King · Rút bài · Săn King đối thủ";
        [SerializeField] private string _gameScene = "GridTest";

        [Header("Nền 3D")]
        [SerializeField] private Camera _camera;
        [SerializeField] private GameObject[] _showcaseModels;
        [SerializeField] private float _unitScale = 0.7f;
        [SerializeField] private float _orbitRadius = 6f;
        [SerializeField] private float _orbitHeight = 2.6f;
        [SerializeField] private float _orbitSpeed = 8f;

        private SettingsPanel _settings;
        private CanvasGroup _buttons;
        private float _angle = 200f;

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
                float x = (i - (n - 1) * 0.5f) * 1.6f;
                go.transform.position = new Vector3(x, 0f, Mathf.Abs(x) * 0.3f);
                go.transform.rotation = Quaternion.Euler(0f, 180f + x * -8f, 0f);
                var r = go.AddComponent<UnitReactions>();
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
            r.Visual.DOKill(true);
            r.Visual.DOLocalJump(Vector3.zero, 0.6f, 1, 0.5f).OnComplete(() => r.Land(0f, quiet: true));
            r.Visual.DOLocalRotate(new Vector3(0f, 360f, 0f), 0.5f, RotateMode.LocalAxisAdd);
            DOVirtual.DelayedCall(period, () => Hop(t, r, period)).SetTarget(this);
        }

        private void Update()
        {
            if (_camera == null) return;
            _angle += _orbitSpeed * Time.deltaTime;
            float a = _angle * Mathf.Deg2Rad;
            var target = new Vector3(0f, 0.9f, 0f);
            _camera.transform.position = target + new Vector3(Mathf.Sin(a) * _orbitRadius, _orbitHeight, Mathf.Cos(a) * _orbitRadius);
            _camera.transform.LookAt(target);
        }

        private void BuildUi()
        {
            Canvas canvas = UiFactory.CreateCanvas("MenuCanvas", 10);
            canvas.transform.SetParent(transform, false);
            Transform root = canvas.transform;

            // Lớp tối bên trái để chữ dễ đọc.
            Image shade = UiFactory.Panel("Shade", root, new Color(0f, 0f, 0f, 0.45f), false);
            shade.rectTransform.anchorMin = Vector2.zero;
            shade.rectTransform.anchorMax = new Vector2(0.42f, 1f);
            shade.rectTransform.offsetMin = shade.rectTransform.offsetMax = Vector2.zero;

            Text title = UiFactory.Label("Title", root, _title, 120, TextAnchor.MiddleLeft, UiFactory.Accent, FontStyle.Bold);
            title.rectTransform.Place(new Vector2(0f, 1f), new Vector2(110f, -170f), new Vector2(900f, 160f));
            Text sub = UiFactory.Label("Subtitle", root, _subtitle, 32, TextAnchor.MiddleLeft);
            sub.rectTransform.Place(new Vector2(0f, 1f), new Vector2(115f, -300f), new Vector2(900f, 60f));

            RectTransform list = UiFactory.Rect("Buttons", root).Place(new Vector2(0f, 0.5f), new Vector2(110f, -110f), new Vector2(460f, 380f));
            UiFactory.Vertical(list, 26f);
            UiFactory.Button("Play", list, "Chơi", () => SceneFader.Load(_gameScene), 40).Height(100f);
            UiFactory.Button("Settings", list, "Cài đặt", OpenSettings, 40).Height(100f);
            UiFactory.Button("Exit", list, "Thoát", Quit, 40).Height(100f);
            _buttons = UiFactory.Group(list.gameObject, true);

            _settings = new SettingsPanel(root, () => UiFactory.Show(_buttons, true));

            // Hiệu ứng xuất hiện.
            title.rectTransform.anchoredPosition += new Vector2(-80f, 0f);
            title.color = new Color(title.color.r, title.color.g, title.color.b, 0f);
            title.rectTransform.DOAnchorPosX(110f, 0.7f).SetEase(Ease.OutCubic);
            title.DOFade(1f, 0.7f);
            sub.color = new Color(sub.color.r, sub.color.g, sub.color.b, 0f);
            sub.DOFade(1f, 0.6f).SetDelay(0.3f);
            _buttons.alpha = 0f;
            _buttons.DOFade(1f, 0.5f).SetDelay(0.5f);
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
}
