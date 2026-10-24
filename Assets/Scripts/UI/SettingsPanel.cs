using System;
using UnityEngine;
using UnityEngine.UI;

namespace GridBattle
{
    /// <summary>Hộp thoại Cài đặt dùng chung cho Main Menu và Pause Menu.</summary>
    public class SettingsPanel
    {
        public CanvasGroup Group { get; }

        private readonly Action _onClose;
        private Button _qualityButton;
        private Button _fullscreenButton;

        public SettingsPanel(Transform parent, Action onClose)
        {
            _onClose = onClose;

            Image dim = UiFactory.Panel("SettingsPanel", parent, new Color(0f, 0f, 0f, 0.55f));
            dim.rectTransform.Stretch();
            Group = UiFactory.Group(dim.gameObject, false);

            Image win = UiFactory.Window("Window", dim.transform, new Vector2(720f, 640f));
            RectTransform list = UiFactory.Rect("List", win.transform).Stretch(40f);
            UiFactory.Vertical(list, 18f);

            UiFactory.Label("Title", list, "SETTINGS", 52, TextAnchor.MiddleCenter, UiFactory.Accent, FontStyle.Bold).Height(80f);

            Row(list, "Volume");
            UiFactory.Slider("Volume", list, 0f, 1f, GameSettings.Volume, v => GameSettings.Volume = v).Height(36f);

            Row(list, "Camera speed");
            UiFactory.Slider("CameraSpeed", list, 0.5f, 2f, GameSettings.CameraSpeed, v => GameSettings.CameraSpeed = v).Height(36f);

            _qualityButton = UiFactory.Button("Quality", list, "", CycleQuality, 28).Skin("btn_blank_blue", true).Height(64f);
            _fullscreenButton = UiFactory.Button("Fullscreen", list, "", ToggleFullscreen, 28).Skin("btn_blank_blue", true).Height(64f);
            UiFactory.Button("Close", list, "CLOSE", Close, 32).Skin("btn_blank_orange", true).Height(72f);
            RefreshLabels();
        }

        private static void Row(Transform parent, string text) =>
            UiFactory.Label(text, parent, text, 28, TextAnchor.LowerLeft).Height(40f);

        public void Open()
        {
            RefreshLabels();
            UiFactory.Show(Group, true);
        }

        public void Close()
        {
            GameSettings.Save();
            UiFactory.Show(Group, false);
            _onClose?.Invoke();
        }

        private void CycleQuality()
        {
            GameSettings.Quality = (GameSettings.Quality + 1) % QualitySettings.names.Length;
            RefreshLabels();
        }

        private void ToggleFullscreen()
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
