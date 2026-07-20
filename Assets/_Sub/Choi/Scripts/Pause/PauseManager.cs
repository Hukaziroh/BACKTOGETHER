using UnityEngine;
using UnityEngine.SceneManagement;
using UnityEngine.UI;
using TMPro;
using Mirror;
using System.Collections;

public class PauseManager : MonoBehaviour
{
    public static PauseManager instance;

    [Header("UI 패널 할당")]
    public GameObject pausePanel;

    [Header("방 코드 UI (모든 플레이어 공용)")]
    public GameObject roomCodeUIContainer;     // 방 코드와 눈동자 버튼을 담고 있는 부모 오브젝트
    public TextMeshProUGUI pauseRoomCodeText;  // 방 코드가 표시될 텍스트
    public Button toggleVisibilityButton;      // 눈동자 버튼

    [Header("설정")]
    public string mainMenuSceneName = "Main";
    public bool isPaused = false;

    private bool isCodeVisible = false; // 기본값은 별표(*)로 숨겨진 상태

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

    void Start()
    {
        // 눈동자 버튼 클릭 이벤트 연결
        if (toggleVisibilityButton != null)
        {
            toggleVisibilityButton.onClick.AddListener(ToggleRoomCodeVisibility);
        }
    }

    // --- 퍼즈/재개 로직 ---
    public void PauseGame()
    {
        if (pausePanel != null) pausePanel.SetActive(true);
        isPaused = true;

        // ★ 퍼즈창이 열릴 때 호스트/클라이언트 상관없이 방 코드 표시 갱신
        UpdateRoomCodeDisplay();

        if (GlobalSceneInputManager.Instance != null && pausePanel != null)
        {
            GlobalSceneInputManager.Instance.SetFocusScope(pausePanel);
        }
    }

    public void ResumeGame()
    {
        if (pausePanel != null) pausePanel.SetActive(false);
        isPaused = false;

        if (GlobalSceneInputManager.Instance != null)
        {
            GlobalSceneInputManager.Instance.ClearFocusScope();
        }
    }

    // --- 방 코드 UI 업데이트 및 가리기 로직 ---
    private void UpdateRoomCodeDisplay()
    {
        // 호스트 전용 제한을 제거하고, 클라이언트도 UI가 항상 켜지도록 설정
        if (roomCodeUIContainer != null)
        {
            roomCodeUIContainer.SetActive(true);
        }

        // 숏코드 가져오기
        string currentCode = PrivateLobbyManager.currentShortCode;

        if (pauseRoomCodeText != null)
        {
            if (string.IsNullOrEmpty(currentCode))
            {
                pauseRoomCodeText.text = "CODE: ------";
            }
            else if (isCodeVisible)
            {
                // 눈을 떴을 때: 실제 코드 표시 (예: CODE: 123456)
                pauseRoomCodeText.text = "CODE: " + currentCode;
            }
            else
            {
                // 눈을 감았을 때: 별표로 마스킹 (예: CODE: ******)
                pauseRoomCodeText.text = "CODE: ******";
            }
        }
    }

    // 눈동자 버튼을 누를 때 호출되는 함수
    public void ToggleRoomCodeVisibility()
    {
        isCodeVisible = !isCodeVisible; // 상태 반전 (숨김 <-> 보임)
        UpdateRoomCodeDisplay();        // 텍스트 갱신
    }

    // --- 옵션창 전환 ---
    public void OpenOptions()
    {
        if (pausePanel != null) pausePanel.SetActive(false);
        OptionsManager.instance.Open();

        if (GlobalSceneInputManager.Instance != null && OptionsManager.instance != null)
        {
            GlobalSceneInputManager.Instance.SetFocusScope(OptionsManager.instance.gameObject);
        }
    }

    public void CloseOptions()
    {
        OptionsManager.instance.Close();
        if (pausePanel != null) pausePanel.SetActive(true);

        if (GlobalSceneInputManager.Instance != null && pausePanel != null)
        {
            GlobalSceneInputManager.Instance.SetFocusScope(pausePanel);
        }
    }

    // --- 시스템 로직 ---
    public void ReturnToMainMenu()
    {
        ResumeGame();

        HostDisconnectHandler disconnectHandler = FindAnyObjectByType<HostDisconnectHandler>();
        if (disconnectHandler != null)
        {
            disconnectHandler.SetIntentionalExit();
        }

        StartCoroutine(LeaveGameGracefullyRoutine());
    }

    private IEnumerator LeaveGameGracefullyRoutine()
    {
        if (NetworkServer.active)
        {
            NetworkManager.singleton.StopHost();
        }
        else if (NetworkClient.isConnected)
        {
            NetworkManager.singleton.StopClient();
        }

        while (NetworkServer.active || NetworkClient.isConnected)
        {
            yield return null;
        }

        SceneManager.LoadScene(mainMenuSceneName);
    }
}