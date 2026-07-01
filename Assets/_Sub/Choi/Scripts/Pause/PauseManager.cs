using UnityEngine;
using UnityEngine.InputSystem;
using UnityEngine.SceneManagement;

public class PauseManager : MonoBehaviour
{
    public static PauseManager instance;

    [Header("UI 패널 할당")]
    public GameObject pausePanel;
    public GameObject optionsPanel;
    public GameObject ConnectPanel;

    [Header("설정")]
    public string mainMenuSceneName = "Main";
    private bool isPaused = false;

    void Awake()
    {
        if (instance == null)
        {
            instance = this;
            DontDestroyOnLoad(gameObject);
        }
        else
        {
            Destroy(gameObject);
        }
    }

    void Update()
    {
        // 1. ESC 키 입력 감지 테스트
        if (Keyboard.current != null && Keyboard.current.escapeKey.wasPressedThisFrame)
        {
            Debug.Log("ESC 키가 눌렸습니다!");
        }

        // 2. 메인 메뉴 씬이면 입력 무시
        if (SceneManager.GetActiveScene().name == mainMenuSceneName) return;

        // 3. 게임 중 ESC 입력 시 메뉴 전환
        if (Keyboard.current != null && Keyboard.current.escapeKey.wasPressedThisFrame)
        {
            if (optionsPanel != null && optionsPanel.activeSelf)
            {
                CloseOptions();
            }
            else if (isPaused)
            {
                ResumeGame();
            }
            else
            {
                PauseGame();
            }
        }
    }

    // --- 패널 제어 함수들 (이제 Find 없이 바로 변수 사용) ---

    public void PauseGame()
    {
        if (pausePanel != null) pausePanel.SetActive(true);
        isPaused = true;
    }

    public void ResumeGame()
    {
        if (pausePanel != null) pausePanel.SetActive(false);
        isPaused = false;
    }

    public void OpenOptions()
    {
        if (pausePanel != null) pausePanel.SetActive(false);
        if (optionsPanel != null) optionsPanel.SetActive(true);
    }

    public void CloseOptions()
    {
        if (SceneManager.GetActiveScene().name == mainMenuSceneName)
        {
            if (optionsPanel != null) optionsPanel.SetActive(false);
            if (ConnectPanel != null) ConnectPanel.SetActive(true);
        }

        else
        {
            if (optionsPanel != null) optionsPanel.SetActive(false);
            if (pausePanel != null) pausePanel.SetActive(true);
        }

    }

    public void QuitGame()
    {
        Debug.Log("게임 종료");
#if UNITY_EDITOR
        UnityEditor.EditorApplication.isPlaying = false;
#else
        Application.Quit();
#endif
    }
}