using DG.Tweening;
using UnityEngine;
using UnityEngine.SceneManagement;

namespace GridBattle
{
    /// <summary>Chuyển scene có fade đen. Prefab Resources/UI/SceneFader, tạo 1 lần và sống qua các scene.</summary>
    public class SceneFader : MonoBehaviour
    {
        [SerializeField] private CanvasGroup _group;
        [SerializeField] private float _fadeOut = 0.35f;
        [SerializeField] private float _fadeIn = 0.45f;

        private static SceneFader _instance;
        private bool _busy;

        private static SceneFader Instance
        {
            get
            {
                if (_instance != null) return _instance;
                _instance = Instantiate(Resources.Load<SceneFader>("UI/SceneFader"));
                DontDestroyOnLoad(_instance.gameObject);
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
            f._group.DOFade(1f, f._fadeOut).SetUpdate(true).OnComplete(() =>
            {
                Time.timeScale = 1f;
                DOTween.KillAll();
                SceneManager.LoadScene(sceneName);
                f._group.DOFade(0f, f._fadeIn).SetUpdate(true).SetDelay(0.05f).OnComplete(() =>
                {
                    f._group.blocksRaycasts = false;
                    f._busy = false;
                });
            });
        }

        public static void Reload() => Load(SceneManager.GetActiveScene().name);
    }
}
