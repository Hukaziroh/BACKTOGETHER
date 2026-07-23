using UnityEngine;
using EpicTransport;
using Mirror;
using System.Collections;
using UnityEngine.UI;

public class PrivateLobbyManager : MonoBehaviour
{
    [Header("UI 연결")]
    public Button hostCreateButton;
    public Button joinButton;

    [Header("로딩 UI 연결 (비워두어도 자동 탐색됩니다)")]
    [SerializeField] private GameObject loadingPanel;

    public static string currentShortCode = "";

    private void Start()
    {
        if (hostCreateButton != null) hostCreateButton.interactable = false;
        if (joinButton != null) joinButton.interactable = false;

        // 시작 시 로딩 패널 자동 확보 및 비활성화
        GameObject panel = GetLoadingPanel();
        if (panel != null) panel.SetActive(false);

        StartCoroutine(WaitForEpicLoginRoutine());
    }

    /// <span style="color:gray;">/// 로딩 패널이 연결되지 않았거나 씬 전환으로 참조가 끊겼을 때 자동으로 찾아주는 함수</span>
    private GameObject GetLoadingPanel()
    {
        if (loadingPanel == null)
        {
            // 씬 내에 숨겨져(Inactive) 있는 WalkingLoadingPanel 컴포넌트까지 전부 탐색
            WalkingLoadingPanel panelScript = FindObjectOfType<WalkingLoadingPanel>(true);
            if (panelScript != null)
            {
                loadingPanel = panelScript.gameObject;
            }
        }
        return loadingPanel;
    }

    private IEnumerator WaitForEpicLoginRoutine()
    {
        Debug.Log("[로비 시스템] 에픽 서버 로그인 상태를 확인 중입니다...");
        float timeout = 5f;
        bool isLoggedIn = false;

        while (!isLoggedIn && timeout > 0f)
        {
            try
            {
                if (!string.IsNullOrEmpty(EpicTransport.EOSSDKComponent.LocalUserProductIdString))
                {
                    isLoggedIn = true;
                }
            }
            catch { }

            timeout -= Time.deltaTime;
            yield return null;
        }

        Debug.Log("[로비 시스템] 에픽 온라인 서비스 로그인 성공! 버튼을 활성화합니다.");
        if (hostCreateButton != null) hostCreateButton.interactable = true;
        if (joinButton != null) joinButton.interactable = true;
    }

    public void OnStartPrivateHostClicked()
    {
        if (hostCreateButton != null) hostCreateButton.interactable = false;
        if (joinButton != null) joinButton.interactable = false;
        StartCoroutine(CleanAndCreateLobbyRoutine());
    }

    private IEnumerator CleanAndCreateLobbyRoutine()
    {
        // =========================================================
        // 0. 로딩 패널 켜기 및 초기화 (0.05)
        // =========================================================
        GameObject currentPanel = GetLoadingPanel();
        if (currentPanel != null)
        {
            currentPanel.SetActive(true);
            var panelScript = currentPanel.GetComponent<WalkingLoadingPanel>();
            if (panelScript != null) panelScript.SetProgress(0.05f);
        }

        Debug.Log($"[HOST FLOW] ① Host 버튼 클릭 | ServerActive={NetworkServer.active} | ClientActive={NetworkClient.active}");

        // =========================================================
        // 1. 기존 Mirror 네트워크 종료
        // =========================================================

        Debug.Log("[HOST FLOW] ② 기존 네트워크 종료 시작");

        if (NetworkServer.active)
        {
            Debug.Log("[HOST FLOW] 기존 Host 종료");
            NetworkManager.singleton.StopHost();
        }
        else if (NetworkClient.active)
        {
            Debug.Log("[HOST FLOW] 기존 Client 종료");
            NetworkManager.singleton.StopClient();
        }

        currentPanel = GetLoadingPanel();
        if (currentPanel != null)
        {
            var panelScript = currentPanel.GetComponent<WalkingLoadingPanel>();
            if (panelScript != null) panelScript.SetProgress(0.15f);
        }

        Debug.Log($"[HOST FLOW] ③ StopHost/StopClient 완료 | ServerActive={NetworkServer.active} | ClientActive={NetworkClient.active}");


        // =========================================================
        // 2. EOS Lobby 상태 확인
        // =========================================================

        EOSLobby eosLobby = NetworkManager.singleton.GetComponent<EOSLobby>();

        if (eosLobby == null)
        {
            Debug.LogError("[HOST FLOW] EOSLobby 컴포넌트를 찾을 수 없음");

            if (hostCreateButton != null)
                hostCreateButton.interactable = true;

            if (joinButton != null)
                joinButton.interactable = true;

            currentPanel = GetLoadingPanel();
            if (currentPanel != null) currentPanel.SetActive(false);

            yield break;
        }

        currentPanel = GetLoadingPanel();
        if (currentPanel != null)
        {
            var panelScript = currentPanel.GetComponent<WalkingLoadingPanel>();
            if (panelScript != null) panelScript.SetProgress(0.25f);
        }

        Debug.Log(
            $"[HOST FLOW] ④ EOS Lobby 상태 | " +
            $"Connected={eosLobby.ConnectedToLobby} | " +
            $"LobbyID={eosLobby.GetCurrentLobbyId()}"
        );


        // =========================================================
        // 3. 기존 EOS Lobby 나가기
        // =========================================================

        if (eosLobby.ConnectedToLobby)
        {
            Debug.Log("[HOST FLOW] 기존 EOS Lobby Leave 요청");

            eosLobby.LeaveLobby();

            float leaveTimeout = 5f;

            while (eosLobby.ConnectedToLobby && leaveTimeout > 0f)
            {
                leaveTimeout -= Time.unscaledDeltaTime;

                float leaveProg = Mathf.Lerp(0.25f, 0.45f, 1f - (leaveTimeout / 5f));
                currentPanel = GetLoadingPanel();
                if (currentPanel != null)
                {
                    var panelScript = currentPanel.GetComponent<WalkingLoadingPanel>();
                    if (panelScript != null) panelScript.SetProgress(leaveProg);
                }

                Debug.Log(
                    $"[HOST FLOW] EOS Lobby Leave 대기 중... " +
                    $"Connected={eosLobby.ConnectedToLobby} | " +
                    $"남은 시간={leaveTimeout:F1}"
                );

                yield return null;
            }

            Debug.Log(
                $"[HOST FLOW] EOS Lobby Leave 결과 | " +
                $"Connected={eosLobby.ConnectedToLobby} | " +
                $"LobbyID={eosLobby.GetCurrentLobbyId()}"
            );
        }
        else
        {
            Debug.Log("[HOST FLOW] 기존 EOS Lobby 없음");
        }


        // =========================================================
        // 4. 잠시 대기
        // =========================================================

        Debug.Log("[HOST FLOW] EOS / Mirror 상태 안정화 대기");

        yield return new WaitForSecondsRealtime(0.5f);


        // =========================================================
        // 5. 새 Lobby 생성
        // =========================================================

        currentShortCode = GenerateShortCode();

        Debug.Log(
            $"[HOST FLOW] ⑤ 새 Lobby 생성 요청 | " +
            $"ShortCode={currentShortCode}"
        );

        uint maxPlayers = 4;

        Epic.OnlineServices.Lobby.LobbyPermissionLevel permissionLevel =
            Epic.OnlineServices.Lobby.LobbyPermissionLevel.Publicadvertised;

        bool presenceEnabled = true;

        eosLobby.CreateLobby(
            maxPlayers,
            permissionLevel,
            presenceEnabled
        );


        // =========================================================
        // 6. Lobby 생성 완료 대기
        // =========================================================

        float timeout = 5f;

        while (
            string.IsNullOrEmpty(eosLobby.GetCurrentLobbyId()) &&
            timeout > 0f
        )
        {
            timeout -= Time.unscaledDeltaTime;

            float createProg = Mathf.Lerp(0.50f, 0.75f, 1f - (timeout / 5f));
            currentPanel = GetLoadingPanel();
            if (currentPanel != null)
            {
                var panelScript = currentPanel.GetComponent<WalkingLoadingPanel>();
                if (panelScript != null) panelScript.SetProgress(createProg);
            }

            Debug.Log(
                $"[HOST FLOW] Lobby 생성 대기 중... " +
                $"LobbyID={eosLobby.GetCurrentLobbyId()} | " +
                $"남은 시간={timeout:F1}"
            );

            yield return null;
        }


        if (timeout <= 0f)
        {
            Debug.LogError("[HOST FLOW] ⑥ Lobby 생성 Timeout");

            if (hostCreateButton != null)
                hostCreateButton.interactable = true;

            if (joinButton != null)
                joinButton.interactable = true;

            currentPanel = GetLoadingPanel();
            if (currentPanel != null) currentPanel.SetActive(false);

            yield break;
        }

        currentPanel = GetLoadingPanel();
        if (currentPanel != null)
        {
            var panelScript = currentPanel.GetComponent<WalkingLoadingPanel>();
            if (panelScript != null) panelScript.SetProgress(0.85f);
        }

        Debug.Log(
            $"[HOST FLOW] ⑥ Lobby 생성 완료 | " +
            $"LobbyID={eosLobby.GetCurrentLobbyId()}"
        );


        // =========================================================
        // 7. Lobby Attribute 등록
        // =========================================================

        Debug.Log(
            $"[HOST FLOW] SHORTCODE 등록 시작 | " +
            $"ShortCode={currentShortCode}"
        );

        eosLobby.UpdateLobbyAttribute(
            "SHORTCODE",
            currentShortCode
        );


        yield return new WaitForSecondsRealtime(0.3f);


        // =========================================================
        // 8. EosTransport 상태 초기화
        // =========================================================

        EosTransport transport =
            NetworkManager.singleton.transport as EosTransport;

        if (transport != null)
        {
            Debug.Log("[HOST FLOW] EosTransport 초기화");

            transport.ResetIgnoreMessagesAtStartUpTimer();
        }
        else
        {
            Debug.LogWarning("[HOST FLOW] EosTransport를 찾을 수 없음");
        }

        currentPanel = GetLoadingPanel();
        if (currentPanel != null)
        {
            var panelScript = currentPanel.GetComponent<WalkingLoadingPanel>();
            if (panelScript != null) panelScript.SetProgress(0.95f);
        }


        // =========================================================
        // 9. StartHost
        // =========================================================

        Debug.Log(
            $"[HOST FLOW] ⑦ StartHost 직전 | " +
            $"ServerActive={NetworkServer.active} | " +
            $"ClientActive={NetworkClient.active} | " +
            $"LobbyID={eosLobby.GetCurrentLobbyId()}"
        );

        NetworkManager.singleton.StartHost();

        currentPanel = GetLoadingPanel();
        if (currentPanel != null)
        {
            var panelScript = currentPanel.GetComponent<WalkingLoadingPanel>();
            if (panelScript != null) panelScript.SetProgress(1.0f);
        }
        yield return new WaitForSecondsRealtime(0.2f);

        Debug.Log(
            $"[HOST FLOW] ⑧ StartHost 직후 | " +
            $"ServerActive={NetworkServer.active} | " +
            $"ClientActive={NetworkClient.active}"
        );


        // =========================================================
        // 10. 버튼 복구 및 로딩 패널 끄기
        // =========================================================

        if (hostCreateButton != null)
            hostCreateButton.interactable = true;

        if (joinButton != null)
            joinButton.interactable = true;

        currentPanel = GetLoadingPanel();
        if (currentPanel != null)
        {
            currentPanel.SetActive(false);
        }

        Debug.Log("[HOST FLOW] Host 생성 루틴 종료");
    }

    private string GenerateShortCode()
    {
        string allowedChars = "0123456789";
        string code = "";

        for (int i = 0; i < 6; i++)
        {
            code += allowedChars[Random.Range(0, allowedChars.Length)];
        }
        return code;
    }
}