using System;
using UnityEngine;
using UnityEngine.UI;

namespace GridBattle
{
    /// <summary>Hộp thoại Cài đặt (prefab dùng chung cho Main Menu và Pause Menu). Nút / slider gọi các hàm public qua OnClick trong Inspector.</summary>
    [RequireComponent(typeof(CanvasGroup))]
    public class SettingsPanel : MonoBehaviour
    {
        [SerializeField] private Slider _volume;
        [SerializeField] private Slider _cameraSpeed;
        [SerializeField] private Button _qualityButton;
        [SerializeField] private Button _fullscreenButton;
        [SerializeField] private float _fadeDuration = 0.2f;

        /// <summary>Bảng vừa đóng -> màn hình gọi nó hiện lại.</summary>
        public event Action Closed;

        public bool IsOpen => gameObject.activeSelf;

        public void Open()
        {
            _volume.SetValueWithoutNotify(GameSettings.Volume);
            _cameraSpeed.SetValueWithoutNotify(GameSettings.CameraSpeed);
            RefreshLabels();
            UiFactory.Show(GetComponent<CanvasGroup>(), true, _fadeDuration);
        }

        public void Close()
        {
            GameSettings.Save();
            UiFactory.Show(GetComponent<CanvasGroup>(), false, _fadeDuration);
            Closed?.Invoke();
        }

        public void SetVolume(float v) => GameSettings.Volume = v;
        public void SetCameraSpeed(float v) => GameSettings.CameraSpeed = v;

        public void CycleQuality()
        {
            GameSettings.Quality = (GameSettings.Quality + 1) % QualitySettings.names.Length;
            RefreshLabels();
        }

        public void ToggleFullscreen()
        {
            GameSettings.Fullscreen = !GameSettings.Fullscreen;
            RefreshLabels();
        }

        private void RefreshLabels()
        {
            _qualityButton.SetText($"Graphics: {QualitySettings.names[GameSettings.Quality]}  >");
            _fullscreenButton.SetText($"Fullscreen: {(GameSettings.Fullscreen ? "On" : "Off")}");
        }
    }
}
