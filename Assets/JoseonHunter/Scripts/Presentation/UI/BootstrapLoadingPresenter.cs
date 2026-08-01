using System.Collections;
using TMPro;
using JoseonHunter.Runtime.Gameplay;
using UnityEngine;
using UnityEngine.SceneManagement;

namespace JoseonHunter.Presentation.UI
{
    [DisallowMultipleComponent]
    public sealed class BootstrapLoadingPresenter : MonoBehaviour
    {
        private const float MinimumVisibleSeconds = .35f;
        private const float FadeDurationSeconds = .28f;
        private const float LoadTimeoutSeconds = 30f;

        private static BootstrapLoadingPresenter instance;

        [SerializeField] private CanvasGroup overlay;
        [SerializeField] private RectTransform progressFill;
        [SerializeField] private RectTransform spiritFlame;
        [SerializeField] private TMP_Text subtitle;
        [SerializeField] private string gameplaySceneName = "Gameplay";
        [SerializeField] private bool beginOnStart = true;

        private bool began;
        private bool fading;

        public bool OpaqueForTests => overlay != null && overlay.alpha >= .999f;
        public float ProgressForTests => progressFill != null ? progressFill.anchorMax.x : 0f;

        public void Configure(
            CanvasGroup canvasGroup,
            RectTransform fill,
            RectTransform flame,
            TMP_Text status)
        {
            overlay = canvasGroup;
            progressFill = fill;
            spiritFlame = flame;
            subtitle = status;
        }

        private void Awake()
        {
            if (instance != null && instance != this)
            {
                Destroy(gameObject);
                return;
            }

            instance = this;
            transform.SetParent(null);
            DontDestroyOnLoad(gameObject);
            if (overlay == null) overlay = GetComponent<CanvasGroup>();
            if (overlay != null)
            {
                overlay.alpha = 1f;
                overlay.interactable = false;
                overlay.blocksRaycasts = true;
            }
            SetProgress(0f);
        }

        private void Start()
        {
            if (beginOnStart) Begin(gameplaySceneName);
        }

        private void Update()
        {
            if (spiritFlame == null || fading) return;
            var pulse = 1f + Mathf.Sin(Time.unscaledTime * 4.8f) * .055f;
            spiritFlame.localScale = Vector3.one * pulse;
        }

        private void OnDestroy()
        {
            if (instance == this) instance = null;
        }

        public void Begin(string sceneName = "Gameplay")
        {
            if (began) return;
            began = true;
            gameplaySceneName = string.IsNullOrWhiteSpace(sceneName) ? "Gameplay" : sceneName;
            StartCoroutine(LoadGameplay());
        }

        private IEnumerator LoadGameplay()
        {
            GameplayReadySignal.Reset();
            var startedAt = Time.realtimeSinceStartup;
            AsyncOperation operation;
            try
            {
                operation = SceneManager.LoadSceneAsync(gameplaySceneName, LoadSceneMode.Single);
            }
            catch (System.Exception)
            {
                ShowFailure();
                yield break;
            }

            if (operation == null)
            {
                ShowFailure();
                yield break;
            }

            while (!operation.isDone)
            {
                SetProgress(Mathf.Clamp01(operation.progress / .9f));
                if (Time.realtimeSinceStartup - startedAt > LoadTimeoutSeconds)
                {
                    ShowFailure();
                    yield break;
                }
                yield return null;
            }

            SetProgress(1f);
            while (!GameplayReadySignal.IsReady)
            {
                if (Time.realtimeSinceStartup - startedAt > LoadTimeoutSeconds)
                {
                    ShowFailure();
                    yield break;
                }
                yield return null;
            }

            while (Time.realtimeSinceStartup - startedAt < MinimumVisibleSeconds)
                yield return null;

            if (Application.isBatchMode)
                yield return null;
            else
                yield return new WaitForEndOfFrame();
            fading = true;
            var alpha = 1f;
            while (alpha > 0f)
            {
                alpha -= Mathf.Max(Time.unscaledDeltaTime, .001f) / FadeDurationSeconds;
                if (overlay != null) overlay.alpha = Mathf.Clamp01(alpha);
                yield return null;
            }

            if (overlay != null) overlay.blocksRaycasts = false;
            Destroy(gameObject);
        }

        private void SetProgress(float value)
        {
            if (progressFill == null) return;
            var anchorMax = progressFill.anchorMax;
            anchorMax.x = Mathf.Clamp01(value);
            progressFill.anchorMax = anchorMax;
        }

        private void ShowFailure()
        {
            StopAllCoroutines();
            SetProgress(0f);
            if (subtitle != null) subtitle.text = "길을 열지 못했습니다. 다시 시작해 주세요.";
            if (overlay != null)
            {
                overlay.alpha = 1f;
                overlay.blocksRaycasts = true;
            }
        }
    }
}
