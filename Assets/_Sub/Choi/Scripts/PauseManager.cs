using UnityEngine;
using UnityEngine.SceneManagement;
using UnityEngine.InputSystem;

public class PauseManager : MonoBehaviour
{
    public GameObject pausePanel; // 유니티 인스펙터에서 전체 퍼즈 패널 연결

    private bool isPaused = false;

    void Update()
    {
        // 옛날 방식: Input.GetKeyDown(KeyCode.Escape) 대신 아래 코드 사용
        if (Keyboard.current != null && Keyboard.current.escapeKey.wasPressedThisFrame)
        {
            if (isPaused) ResumeGame();
            else PauseGame();
        }
    }

    // 1. CONTINUE 버튼에 연결할 함수
    public void ResumeGame()
    {
        pausePanel.SetActive(false);
        Time.timeScale = 1f;
        isPaused = false;
    }

    // 2. OPTIONS 버튼에 연결할 함수
    public void OpenOptions()
    {
        Debug.Log("옵션창 열기");
    }

    // 3. EXIT 버튼에 연결할 함수
    public void QuitGame()
    {
        Debug.Log("게임 종료");
#if UNITY_EDITOR
        UnityEditor.EditorApplication.isPlaying = false;
#else
        Application.Quit();
#endif
    }

    // 일시정지 함수
    public void PauseGame()
    {
        pausePanel.SetActive(true);
        Time.timeScale = 0f;
        isPaused = true;
    }
}