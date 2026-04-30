using System.Collections;
using UnityEngine;
using UnityEngine.SceneManagement;
using UnityEngine.UI;

namespace DungeonBlade.Core
{
    public class FadeLoader : MonoBehaviour
    {
        public static FadeLoader Instance { get; private set; }

        [SerializeField] CanvasGroup fadeGroup;
        [SerializeField] Image fadeImage;
        [SerializeField] float defaultFadeDuration = 0.4f;

        bool _busy;

        void Awake()
        {
            if (Instance != null && Instance != this) { Destroy(gameObject); return; }
            Instance = this;
            DontDestroyOnLoad(gameObject);
            if (fadeGroup != null) fadeGroup.alpha = 0f;
            if (fadeImage != null) fadeImage.raycastTarget = false;
        }

        public void LoadScene(string sceneName, float fadeDuration = -1f)
        {
            if (_busy) return;
            float dur = fadeDuration > 0f ? fadeDuration : defaultFadeDuration;
            StartCoroutine(LoadRoutine(sceneName, dur));
        }

        IEnumerator LoadRoutine(string sceneName, float dur)
        {
            _busy = true;
            yield return Fade(0f, 1f, dur);

            var op = SceneManager.LoadSceneAsync(sceneName);
            if (op != null)
            {
                op.allowSceneActivation = true;
                while (!op.isDone) yield return null;
            }

            yield return Fade(1f, 0f, dur);
            _busy = false;
        }

        IEnumerator Fade(float from, float to, float duration)
        {
            if (fadeGroup == null) yield break;
            if (fadeImage != null) fadeImage.raycastTarget = to > 0.01f;
            float t = 0f;
            while (t < duration)
            {
                t += Time.unscaledDeltaTime;
                fadeGroup.alpha = Mathf.Lerp(from, to, t / duration);
                yield return null;
            }
            fadeGroup.alpha = to;
            if (fadeImage != null) fadeImage.raycastTarget = to > 0.01f;
        }
    }
}
