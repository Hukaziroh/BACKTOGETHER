using UnityEngine;
using UnityEngine.InputSystem; // 🌟 필수: 키보드 입력 처리를 위해 필요
using UnityEngine.SceneManagement;

public class UIManager : MonoBehaviour
{
    public static UIManager Instance;

    [Header("기능 매니저 연결")]
    [SerializeField] private PauseManager pauseManager;
    [SerializeField] private OptionsManager optionsManager;

    [Header("설정")]
    [SerializeField] private string[] excludedScenes = { "Main", "Lobby" };

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
        }
    }

    private void Update()
    {
        // 1. ESC 입력 감지
        if (Keyboard.current != null && Keyboard.current.escapeKey.wasPressedThisFrame)
        {
            HandleEscapeInput();
        }
    }

    private void HandleEscapeInput()
    {
        string currentScene = SceneManager.GetActiveScene().name;

        // 2. 메인/로비 씬에서는 퍼즈 작동 안 함
        foreach (string scene in excludedScenes)
        {
            if (currentScene == scene) return;
        }

        // 3. UI 상태에 따른 관제 로직
        // 옵션창이 켜져 있으면 -> 옵션 닫고 퍼즈창으로 복귀
        if (optionsManager.optionsPanel != null && optionsManager.optionsPanel.activeSelf)
        {
            pauseManager.CloseOptions();
        }
        // 퍼즈 상태면 -> 게임 재개
        else if (pauseManager.isPaused)
        {
            pauseManager.ResumeGame();
        }
        // 기본 상태면 -> 퍼즈 실행
        else
        {
            pauseManager.PauseGame();
        }
    }

    // --- 외부 호출용 API (나중에 HUD 등 추가 시 여기 작성) ---
    public void RequestPause() => pauseManager.PauseGame();
    public void RequestOptions() => optionsManager.Open();
}