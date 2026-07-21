using UnityEngine;
using System.Collections;
using UnityEngine.SceneManagement;

public class ScreenFader : MonoBehaviour
{
    public static ScreenFader Instance { get; private set; }

    [Header("UI 설정")]
    [Tooltip("검은색 Panel에 추가된 CanvasGroup 컴포넌트 연결")]
    public CanvasGroup fadeCanvasGroup;

    [Header("씬 이름 설정")]
    [Tooltip("메인 메뉴 씬 이름")]
    public string mainSceneName = "Main";
    [Tooltip("로비 씬 이름")]
    public string lobbySceneName = "Lobby";

    private string previousSceneName = "";
    private bool isFirstScene = true;

    private void Awake()
    {
        if (Instance == null)
        {
            Instance = this;
            DontDestroyOnLoad(gameObject);
        }
        else
        {
            Destroy(gameObject);
            return;
        }
    }

    private void OnEnable()
    {
        SceneManager.sceneLoaded += OnSceneLoaded;
    }

    private void OnDisable()
    {
        SceneManager.sceneLoaded -= OnSceneLoaded;
    }

    private void OnSceneLoaded(Scene scene, LoadSceneMode mode)
    {
        string currentSceneName = scene.name;

        // 페이드 인을 생략해야 하는 조건들:
        // 1. 게임을 처음 시작할 때
        // 2. 메인 -> 로비로 넘어갈 때
        // 3. 어떤 챕터든 상관없이 메인으로 돌아갈 때 (currentSceneName == mainSceneName)
        if (isFirstScene)
        {
            isFirstScene = false;
            SetAlpha(0f, false);
        }
        else if (previousSceneName == mainSceneName && currentSceneName == lobbySceneName)
        {
            SetAlpha(0f, false);
        }
        else if (currentSceneName == mainSceneName)
        {
            SetAlpha(0f, false);
        }
        else
        {
            // 그 외의 씬 전환 (챕터 이동 등)에서는 페이드 인 실행
            FadeIn(1.5f);
        }

        previousSceneName = currentSceneName;
    }

    private void SetAlpha(float alpha, bool active)
    {
        if (fadeCanvasGroup != null)
        {
            fadeCanvasGroup.alpha = alpha;
            fadeCanvasGroup.gameObject.SetActive(active);
        }
    }

    public void FadeOut(float duration, System.Action onComplete = null)
    {
        StartCoroutine(FadeOutRoutine(duration, onComplete));
    }

    public void FadeIn(float duration)
    {
        StartCoroutine(FadeInRoutine(duration));
    }

    private IEnumerator FadeOutRoutine(float duration, System.Action onComplete)
    {
        if (fadeCanvasGroup != null)
        {
            fadeCanvasGroup.gameObject.SetActive(true);
            fadeCanvasGroup.alpha = 0f;

            float timer = 0f;
            while (timer < duration)
            {
                timer += Time.deltaTime;
                fadeCanvasGroup.alpha = Mathf.Clamp01(timer / duration);
                yield return null;
            }
            fadeCanvasGroup.alpha = 1f;
        }
        onComplete?.Invoke();
    }

    private IEnumerator FadeInRoutine(float duration)
    {
        if (fadeCanvasGroup != null)
        {
            fadeCanvasGroup.gameObject.SetActive(true);
            fadeCanvasGroup.alpha = 1f;

            float timer = 0f;
            while (timer < duration)
            {
                timer += Time.deltaTime;
                fadeCanvasGroup.alpha = Mathf.Clamp01(1f - (timer / duration));
                yield return null;
            }
            fadeCanvasGroup.alpha = 0f;
            fadeCanvasGroup.gameObject.SetActive(false);
        }
    }
}