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
    /// UI nằm sẵn trong scene (MenuCanvas); nút gọi các hàm public qua OnClick trong Inspector.
    /// </summary>
    public class MainMenu : MonoBehaviour
    {
        [SerializeField] private string _gameScene = "GridTest";

        [Header("UI trong scene")]
        [SerializeField] private RectTransform _title;
        [SerializeField] private Text _subtitle;
        [SerializeField] private CanvasGroup _buttons;
        [SerializeField] private HoverRelay _quitHover;
        [SerializeField] private SettingsPanel _settings;

        [Header("Hiệu ứng xuất hiện")]
        [SerializeField] private float _titleDrop = 300f;
        [SerializeField] private float _titleDropTime = 0.7f;
        [SerializeField] private float _buttonStagger = 0.12f;

        [Header("Nền 3D")]
        [SerializeField] private Camera _camera;
        [Tooltip("Quân trưng bày đặt sẵn trong scene (object Showcase): kéo thả vị trí / góc / scale trực tiếp, thêm bớt tuỳ ý.")]
        [SerializeField] private Transform[] _showcaseUnits;
        [Tooltip("Điểm camera nhìn vào và quay quanh.")]
        [SerializeField] private Vector3 _lookAt = new(0f, 0.9f, 0f);
        [SerializeField] private float _orbitRadius = 6f;
        [SerializeField] private float _orbitHeight = 2.6f;
        [Tooltip("Hướng đặt camera quanh _lookAt (độ). 180 = đứng phía -Z nhìn sang +Z.")]
        [SerializeField] private float _orbitAngle = 180f;
        [Tooltip("Camera đung đưa qua lại chừng này độ cho cảnh không chết. 0 = đứng yên.")]
        [SerializeField] private float _swayAngle = 4f;
        [SerializeField] private float _swaySpeed = 0.3f;

        [Header("Quân trưng bày")]
        [SerializeField] private float _spawnDelay = 0.3f;
        [Tooltip("Quân sau xuất hiện / nhảy trễ hơn quân trước chừng này.")]
        [SerializeField] private float _stagger = 0.2f;
        [SerializeField] private float _hopEvery = 3.5f;
        [SerializeField] private float _hopHeight = 0.5f;

        private readonly List<CharacterRig> _rigs = new();
        private Vector3 _rowCenter; // mắt các quân dõi theo con trỏ trên mặt phẳng đi qua điểm này
        private bool _angry;

        private void Start()
        {
            Time.timeScale = 1f;
            if (_camera == null) _camera = Camera.main;
            SpawnShowcase();
            _settings.Closed += () => UiFactory.Show(_buttons, true);
            _quitHover.Changed = SetAngry;
            PlayIntro();
        }

        private void SpawnShowcase()
        {
            if (_showcaseUnits == null) return;
            int n = 0;
            for (int i = 0; i < _showcaseUnits.Length; i++)
            {
                Transform t = _showcaseUnits[i];
                if (t == null) continue;
                _rowCenter += t.position;
                n++;
                ToonStyle.Apply(t.gameObject);
                var r = t.gameObject.AddComponent<UnitReactions>();
                _rigs.Add(t.gameObject.AddComponent<CharacterRig>());
                r.HideUntilSpawn();
                // Lần lượt nhảy ra chào sân, sau đó thỉnh thoảng nhảy lên (lệch nhịp nhau).
                DOVirtual.DelayedCall(_spawnDelay + i * _stagger, r.PlaySpawn).SetTarget(this);
                float period = _hopEvery + i * _stagger * 3.5f;
                DOVirtual.DelayedCall(_spawnDelay + 1.2f + i * _stagger * 4.5f, () => Hop(t, r, period)).SetTarget(this);
            }
            _rowCenter = n > 0 ? _rowCenter / n + Vector3.up : _lookAt;
        }

        private void Hop(Transform t, UnitReactions r, float period)
        {
            if (t == null) return;
            if (!_angry)
            {
                r.Visual.DOKill(true);
                r.Visual.DOLocalJump(Vector3.zero, _hopHeight, 1, 0.45f).OnComplete(() => r.Land(0f, quiet: true));
            }
            DOVirtual.DelayedCall(period, () => Hop(t, r, period)).SetTarget(this);
        }

        private void Update()
        {
            if (_camera == null) return;
            // Camera đứng trước hàng quân, chỉ đung đưa nhẹ để cảnh không chết.
            float a = (_orbitAngle + Mathf.Sin(Time.time * _swaySpeed) * _swayAngle) * Mathf.Deg2Rad;
            _camera.transform.position = _lookAt + new Vector3(Mathf.Sin(a) * _orbitRadius, _orbitHeight, Mathf.Cos(a) * _orbitRadius);
            _camera.transform.LookAt(_lookAt);

            // Điểm con trỏ trên mặt phẳng đứng (vuông góc camera) đi qua hàng quân.
            Ray ray = _camera.ScreenPointToRay(Input.mousePosition);
            var plane = new Plane(-_camera.transform.forward, _rowCenter);
            Vector3? look = plane.Raycast(ray, out float d) ? ray.GetPoint(d) : null;
            foreach (CharacterRig rig in _rigs)
                if (rig != null) rig.LookTarget = look;
        }

        /// <summary>Tiêu đề rơi xuống nảy, nút bật lên lần lượt, tiêu đề nhún nhẹ mãi.</summary>
        private void PlayIntro()
        {
            float restY = _title.anchoredPosition.y;
            _title.anchoredPosition += new Vector2(0f, _titleDrop);
            _title.DOAnchorPosY(restY, _titleDropTime).SetEase(Ease.OutBounce);
            _title.DOScale(1.03f, 1.2f).SetDelay(_titleDropTime + 0.1f).SetLoops(-1, LoopType.Yoyo).SetEase(Ease.InOutSine);
            _subtitle.color = new Color(_subtitle.color.r, _subtitle.color.g, _subtitle.color.b, 0f);
            _subtitle.DOFade(1f, 0.6f).SetDelay(0.5f);
            Transform list = _buttons.transform;
            for (int i = 0; i < list.childCount; i++)
            {
                Transform b = list.GetChild(i);
                b.localScale = Vector3.zero;
                b.DOScale(1f, 0.4f).SetDelay(0.6f + i * _buttonStagger).SetEase(Ease.OutBack);
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

        public void Play() => SceneFader.Load(_gameScene);

        public void OpenSettings()
        {
            UiFactory.Show(_buttons, false);
            _settings.Open();
        }

        public void Quit()
        {
#if UNITY_EDITOR
            UnityEditor.EditorApplication.isPlaying = false;
#else
            Application.Quit();
#endif
        }
    }
}
