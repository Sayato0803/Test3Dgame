using UnityEngine;

namespace GridBattle
{
    /// <summary>Cài đặt người chơi, lưu bằng PlayerPrefs. Tự áp dụng khi game khởi động.</summary>
    public static class GameSettings
    {
        private const string VolumeKey = "settings.volume";
        private const string QualityKey = "settings.quality";
        private const string FullscreenKey = "settings.fullscreen";
        private const string CameraSpeedKey = "settings.cameraSpeed";

        public static float Volume
        {
            get => PlayerPrefs.GetFloat(VolumeKey, 0.8f);
            set { PlayerPrefs.SetFloat(VolumeKey, Mathf.Clamp01(value)); AudioListener.volume = Volume; }
        }

        public static int Quality
        {
            get => Mathf.Clamp(PlayerPrefs.GetInt(QualityKey, QualitySettings.GetQualityLevel()), 0, QualitySettings.names.Length - 1);
            set { PlayerPrefs.SetInt(QualityKey, value); QualitySettings.SetQualityLevel(Quality, true); }
        }

        public static bool Fullscreen
        {
            get => PlayerPrefs.GetInt(FullscreenKey, Screen.fullScreen ? 1 : 0) == 1;
            set { PlayerPrefs.SetInt(FullscreenKey, value ? 1 : 0); Screen.fullScreen = value; }
        }

        /// <summary>Hệ số tốc độ camera bay (0.5 = chậm, 2 = nhanh).</summary>
        public static float CameraSpeed
        {
            get => PlayerPrefs.GetFloat(CameraSpeedKey, 1f);
            set => PlayerPrefs.SetFloat(CameraSpeedKey, Mathf.Clamp(value, 0.5f, 2f));
        }

        public static void Save() => PlayerPrefs.Save();

        [RuntimeInitializeOnLoadMethod(RuntimeInitializeLoadType.BeforeSceneLoad)]
        private static void ApplyOnStartup()
        {
            AudioListener.volume = Volume;
            QualitySettings.SetQualityLevel(Quality, true);
#if !UNITY_EDITOR
            Screen.fullScreen = Fullscreen;
#endif
        }
    }
}
