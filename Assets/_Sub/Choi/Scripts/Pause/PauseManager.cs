using UnityEngine;
using UnityEngine.SceneManagement;
using Mirror;
using System.Collections;
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

        // 1. 내가 스스로 나가는 것이므로 튕김 UI 방지 (기존 코드 유지)
        HostDisconnectHandler disconnectHandler = FindFirstObjectByType<HostDisconnectHandler>();
        if (disconnectHandler != null)
        {
            disconnectHandler.SetIntentionalExit();
        }

        // 2. 🌟 즉시 씬을 바꾸지 않고, 안전하게 대기한 뒤 나가는 코루틴 실행
        StartCoroutine(LeaveGameGracefullyRoutine());
    }

    private IEnumerator LeaveGameGracefullyRoutine()
    {
        // 네트워크 연결 종료 명령 전달
        if (NetworkServer.active && NetworkClient.isConnected)
            NetworkManager.singleton.StopHost();
        else if (NetworkClient.isConnected)
            NetworkManager.singleton.StopClient();

        // 4. ⭐ 핵심: Mirror 서버와 클라이언트 루프가 완전히 꺼질 때까지 한 프레임씩 대기
        // active 상태가 둘 다 false가 될 때까지 다음 프레임으로 양보합니다.
        while (NetworkServer.active || NetworkClient.active)
        {
            yield return null;
        }

        // 5. 에픽 트랜스포트와 Mirror가 완벽히 정리를 끝낸 후 안전하게 씬 이동
        SceneManager.LoadScene(mainMenuSceneName);
    }
}