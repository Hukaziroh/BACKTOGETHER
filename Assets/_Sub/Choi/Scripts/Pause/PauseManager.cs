using UnityEngine;
using UnityEngine.SceneManagement;
using Mirror;

public class PauseManager : MonoBehaviour
{
    public static PauseManager instance;

    [Header("UI 패널 할당")]
    public GameObject pausePanel;

    [Header("설정")]
    public string mainMenuSceneName = "Main";
    public bool isPaused = false; // UIManager가 상태를 읽을 수 있게 public

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

    // --- 퍼즈/재개 로직 ---
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

    // --- 옵션창 전환 (자신은 숨기고 옵션 매니저를 호출) ---
    public void OpenOptions()
    {
        if (pausePanel != null) pausePanel.SetActive(false);
        OptionsManager.instance.Open();
    }

    // 옵션에서 뒤로가기 눌렀을 때 호출
    public void CloseOptions()
    {
        OptionsManager.instance.Close();
        if (pausePanel != null) pausePanel.SetActive(true);
    }

    // --- 시스템 로직 (그대로 유지) ---
    public void ReturnToMainMenu()
    {
        ResumeGame();

        HostDisconnectHandler disconnectHandler = FindFirstObjectByType<HostDisconnectHandler>();
        if (disconnectHandler != null)
        {
            disconnectHandler.enabled = false;
        }

        if (NetworkServer.active && NetworkClient.isConnected)
            NetworkManager.singleton.StopHost();
        else if (NetworkClient.isConnected)
            NetworkManager.singleton.StopClient();
        else
            SceneManager.LoadScene(mainMenuSceneName);
    }
}