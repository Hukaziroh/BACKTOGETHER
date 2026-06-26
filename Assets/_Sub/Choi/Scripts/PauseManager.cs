using UnityEngine;

public class PauseManager : MonoBehaviour
{
    public GameObject pausePanel; // 유니티 인스펙터에서 퍼즈창 UI 오브젝트 연결
    private bool isPaused = false;

    void Update()
    {
        // ESC 키를 누르면 퍼즈 토글
        if (Input.GetKeyDown(KeyCode.Escape))
        {
            if (isPaused) ResumeGame();
            else PauseGame();
        }
    }

    public void PauseGame()
    {
        pausePanel.SetActive(true);      // 퍼즈창 보이기
        Time.timeScale = 0f;            // 게임 정지
        isPaused = true;
    }

    public void ResumeGame()
    {
        pausePanel.SetActive(false);     // 퍼즈창 숨기기
        Time.timeScale = 1f;            // 게임 재개
        isPaused = false;
    }
}