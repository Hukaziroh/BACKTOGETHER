using UnityEngine;
using UnityEngine.InputSystem;
using UnityEngine.SceneManagement;

public class PauseManager : MonoBehaviour
{
    // 싱글톤 인스턴스
    public static PauseManager instance;

    public GameObject pausePanel;
    public GameObject optionsPanel;

    [Header("설정")]
    public string mainMenuSceneName = "Main";
    private bool isPaused = false;

    void Awake()
    {
        if (instance == null)
        {
            instance = this;
            DontDestroyOnLoad(gameObject);
            // 씬이 로드될 때마다 실행할 함수 등록
            SceneManager.sceneLoaded += OnSceneLoaded;
        }
        else
        {
            Destroy(gameObject);
        }
    }

    // 씬 전환 시 자동으로 호출되는 함수
    private void OnSceneLoaded(Scene scene, LoadSceneMode mode)
    {
        // 활성화/비활성화 상태와 상관없이 씬 내의 모든 오브젝트를 검색
        GameObject[] allObjects = Resources.FindObjectsOfTypeAll<GameObject>();

        foreach (GameObject obj in allObjects)
        {
            // 씬 안에 있는 오브젝트인지 확인 (DontDestroyOnLoad 제외)
            if (obj.scene.IsValid() && obj.scene.name == scene.name)
            {
                if (obj.name == "PausePanel")
                {
                    pausePanel = obj;
                    pausePanel.SetActive(false); // 찾았으면 일단 끄기
                }
                if (obj.name == "Option_Panel") // 이미지상 이름이 이거였으므로 수정
                {
                    optionsPanel = obj;
                    optionsPanel.SetActive(false);
                }
            }
        }
    }

    void Update()
    {
        // 1. 현재 씬이 메인 메뉴라면 입력 무시
        if (Keyboard.current != null && Keyboard.current.escapeKey.wasPressedThisFrame)
        {
            Debug.Log("ESC 키가 눌렸습니다!");
        }

        if (SceneManager.GetActiveScene().name == mainMenuSceneName) return;

        // 2. 키 입력 감지 (New Input System)
        if (Keyboard.current != null && Keyboard.current.escapeKey.wasPressedThisFrame)
        {
            // 옵션창이 켜져 있으면 닫고 퍼즈창으로 복귀
            if (optionsPanel != null && optionsPanel.activeSelf)
            {
                CloseToPauseOptions(); // CloseToPauseOptions로 수정하여 로직 통일
            }
            // 퍼즈 상태면 재개, 아니면 퍼즈 호출
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

    public void PauseGame()
    {
        if (pausePanel != null) pausePanel.SetActive(true);
        Time.timeScale = 0f;
        isPaused = true;
    }

    public void ResumeGame()
    {
        GameObject currentPause = FindPanel("PausePanel");
        if (currentPause != null)
        {
            currentPause.SetActive(false); // 퍼즈창 끄기
            pausePanel = currentPause;     // 변수 갱신
        }
        Time.timeScale = 1f;
        isPaused = false;
    }

    private GameObject FindPanel(string name)
    {
        GameObject[] allObjects = Resources.FindObjectsOfTypeAll<GameObject>();
        foreach (GameObject obj in allObjects)
        {
            if (obj.name == name) return obj;
        }
        return null;
    }

    public void OpenOptions()
    {
        GameObject currentPause = FindPanel("PausePanel");
        GameObject currentOption = FindPanel("Option_Panel");

        if (currentPause != null)
        {
            currentPause.SetActive(false);
            pausePanel = currentPause;
        }

        if (currentOption != null)
        {
            currentOption.SetActive(true); // 옵션창 켬
            optionsPanel = currentOption;
        }
    }

    public void CloseToPauseOptions()
    {
        GameObject currentPause = FindPanel("PausePanel");
        GameObject currentOption = FindPanel("Option_Panel");

        if (currentOption != null)
        {
            currentOption.SetActive(false); // 옵션창 끔
            optionsPanel = currentOption;
        }

        if (currentPause != null)
        {
            currentPause.SetActive(true); // 퍼즈창 켬
            pausePanel = currentPause;
        }
    }


    public void CloseOptions()
    {
        if (optionsPanel != null) optionsPanel.SetActive(false);
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