using UnityEngine;
using TMPro;
using Mirror;
using UnityEngine.SceneManagement;
using UnityEngine.UI;
using Epic.OnlineServices;
using Epic.OnlineServices.Lobby;
using System.Collections;

public class PauseManager : MonoBehaviour
{
    private static PauseManager _instance;
    public static PauseManager instance
    {
        get
        {
            if (_instance == null || _instance.gameObject.scene != SceneManager.GetActiveScene())
            {
                PauseManager[] managers = FindObjectsByType<PauseManager>(FindObjectsInactive.Exclude);
                foreach (var mgr in managers)
                {
                    if (mgr.gameObject.scene == SceneManager.GetActiveScene())
                    {
                        _instance = mgr;
                        break;
                    }
                }
                if (_instance == null && managers.Length > 0) _instance = managers[0];
            }
            return _instance;
        }
        set => _instance = value;
    }

    private bool isLeaving = false;

    [Header("UI 패널 할당")]
    public GameObject pausePanel;

    [Header("설정")]
    public string mainMenuSceneName = "Main";
    public bool isPaused = false;

    void Awake()
    {
        instance = this;
    }

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

    /// <summary>
    /// [메인 메뉴로 이동] 버튼 클릭 시 실행할 함수
    /// </summary>
    public void OnClick_ReturnToMain()
    {
        if (isLeaving) return;
        StartCoroutine(LeaveGameRoutine());
    }

    private IEnumerator LeaveGameRoutine()
    {
        isLeaving = true;
        Debug.Log("[퍼즈 시스템] ① 메인 메뉴 이동 시작");

        // =========================================================
        // 1. EOS Lobby 퇴장 및 방 파괴 처리
        // =========================================================
        EOSLobby eosLobby = NetworkManager.singleton != null ? NetworkManager.singleton.GetComponent<EOSLobby>() : null;

        if (eosLobby != null)
        {
            if (eosLobby.ConnectedToLobby)
            {
                // ★ 내가 방장(Host)이면 에픽 서버에서 방 파괴, 손님(Client)이면 나만 퇴장
                if (NetworkServer.active)
                {
                    Debug.Log("[퍼즈 시스템] ② 호스트이므로 EOS Lobby Destroy(방 파괴) 요청");
                    eosLobby.DestroyLobby();
                }
                else
                {
                    Debug.Log("[퍼즈 시스템] ② 클라이언트이므로 EOS Lobby Leave(퇴장) 요청");
                    eosLobby.LeaveLobby();
                }

                float leaveTimeout = 5f;
                while (eosLobby.ConnectedToLobby && leaveTimeout > 0f)
                {
                    leaveTimeout -= Time.unscaledDeltaTime;
                    yield return null;
                }

                if (leaveTimeout <= 0f && eosLobby.ConnectedToLobby)
                {
                    Debug.LogWarning("[퍼즈 시스템] ③ EOS Lobby 처리 타임아웃");
                }
                else
                {
                    Debug.Log("[퍼즈 시스템] ③ EOS Lobby 처리 완료");
                }
            }
        }

        yield return new WaitForSecondsRealtime(0.2f);

        // =========================================================
        // 2. Mirror 네트워크 종료
        // =========================================================
        if (NetworkServer.active)
        {
            Debug.Log("[퍼즈 시스템] ④ Mirror StopHost 실행");
            NetworkManager.singleton.StopHost();
        }
        else if (NetworkClient.active)
        {
            Debug.Log("[퍼즈 시스템] ④ Mirror StopClient 실행");
            NetworkManager.singleton.StopClient();
        }

        yield return new WaitForSecondsRealtime(0.3f);

        // =========================================================
        // 3. 메인 씬 이동
        // =========================================================
        Debug.Log("[퍼즈 시스템] ⑤ 메인 씬 로드");
        if (!string.IsNullOrEmpty(mainMenuSceneName))
        {
            SceneManager.LoadScene(mainMenuSceneName);
        }

        isLeaving = false;
    }
}