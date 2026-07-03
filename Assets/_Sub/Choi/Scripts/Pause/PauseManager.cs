using UnityEngine;
using UnityEngine.InputSystem;
using UnityEngine.SceneManagement;
using Mirror; // 🌟 NetworkManager와 isServer 사용을 위해 필수 추가

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

    // --- 패널 제어 함수들 ---

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

    // 🌟 QuitGame을 지우고 메인메뉴 복귀 함수로 변경
    public void ReturnToMainMenu()
    {
        // 퍼즈 상태 해제 및 패널 비활성화
        ResumeGame();

        // 현재 클라이언트가 서버(호스트) 역할도 같이 하고 있는지 확인
        if (NetworkServer.active && NetworkClient.isConnected)
        {
            // 방장(호스트)인 경우: 서버 및 클라이언트를 모두 종료
            NetworkManager.singleton.StopHost();
        }
        else if (NetworkClient.isConnected)
        {
            // 일반 게스트 플레이어인 경우: 클라이언트 연결만 종료
            NetworkManager.singleton.StopClient();
        }
        else
        {
            // 만약 네트워크가 아예 연결 안 된 싱글 테스트 상태라면 그냥 씬 이동
            SceneManager.LoadScene(mainMenuSceneName);
        }
    }
}