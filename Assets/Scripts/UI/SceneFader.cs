using DG.Tweening;
using UnityEngine;
using UnityEngine.SceneManagement;
using UnityEngine.UI;

namespace GridBattle
{
    /// <summary>Chuyển scene có fade đen. Tự tạo 1 lần và sống qua các scene.</summary>
    public class SceneFader : MonoBehaviour
    {
        private static SceneFader _instance;
        private CanvasGroup _group;
        private bool _busy;

        private static SceneFader Instance
        {
            get
            {
                if (_instance != null) return _instance;
                Canvas canvas = UiFactory.CreateCanvas("SceneFader", 1000);
                _instance = canvas.gameObject.AddComponent<SceneFader>();
                DontDestroyOnLoad(canvas.gameObject);
                Image black = UiFactory.Panel("Black", canvas.transform, Color.black);
                black.rectTransform.Stretch();
                _instance._group = canvas.gameObject.AddComponent<CanvasGroup>();
                _instance._group.alpha = 0f;
                _instance._group.blocksRaycasts = false;
                return _instance;
            }
        }

        public static void Load(string sceneName)
        {
            SceneFader f = Instance;
            if (f._busy) return;
            f._busy = true;
            f._group.blocksRaycasts = true;
            f._group.DOKill();
            f._group.DOFade(1f, 0.35f).SetUpdate(true).OnComplete(() =>
            {
                Time.timeScale = 1f;
                DOTween.KillAll();
                SceneManager.LoadScene(sceneName);
                f._group.DOFade(0f, 0.45f).SetUpdate(true).SetDelay(0.05f).OnComplete(() =>
                {
                    f._group.blocksRaycasts = false;
                    f._busy = false;
                });
            });
        }

        public static void Reload() => Load(SceneManager.GetActiveScene().name);
    }
}
