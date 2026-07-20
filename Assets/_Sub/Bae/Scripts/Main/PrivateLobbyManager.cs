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

    public static string currentShortCode = "";

    private void Start()
    {
        if (hostCreateButton != null) hostCreateButton.interactable = false;
        if (joinButton != null) joinButton.interactable = false;

        StartCoroutine(WaitForEpicLoginRoutine());
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
            catch { /* yield가 포함되지 않은 안전한 try-catch */ }

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
        // 1. 기존 연결 끊기
        if (NetworkClient.active) NetworkManager.singleton.StopClient();
        if (NetworkServer.active) NetworkManager.singleton.StopHost();

        // 2. 에픽 로비 확실히 파기 후 대기
        EOSLobby eosLobby = NetworkManager.singleton.GetComponent<EOSLobby>();
        if (eosLobby != null && eosLobby.ConnectedToLobby)
        {
            eosLobby.LeaveLobby();
            while (eosLobby.ConnectedToLobby) yield return null;
        }

        yield return new WaitForSeconds(0.5f);

        currentShortCode = GenerateShortCode();
        Debug.Log("[로비 시스템] 발급된 숏코드: " + currentShortCode);

        uint maxPlayers = 4;
        Epic.OnlineServices.Lobby.LobbyPermissionLevel permissionLevel = Epic.OnlineServices.Lobby.LobbyPermissionLevel.Publicadvertised;
        bool presenceEnabled = true;

        // 3. 새 로비 생성
        eosLobby.CreateLobby(maxPlayers, permissionLevel, presenceEnabled);

        float timeout = 5f;
        while (string.IsNullOrEmpty(eosLobby.GetCurrentLobbyId()) && timeout > 0f)
        {
            timeout -= Time.deltaTime;
            yield return null;
        }

        // 4. 생성 완료 후 숏코드 등록 및 서버 시작
        if (timeout > 0f)
        {
            try
            {
                eosLobby.UpdateLobbyAttribute("SHORTCODE", currentShortCode);
            }
            catch (System.Exception e)
            {
                Debug.LogWarning("[로비 시스템] 어트리뷰트 등록 실패: " + e.Message);
            }

            yield return new WaitForSeconds(0.3f);
            NetworkManager.singleton.StartHost();
        }
        else
        {
            Debug.LogError("[로비 시스템] 에픽 서버 로비 개설 시간 초과(Timeout)");
        }

        // 🌟 [핵심] 코루틴 마지막에 도달하면 무조건 버튼을 살려줌 (try-catch 필요 없음)
        if (hostCreateButton != null) hostCreateButton.interactable = true;
        if (joinButton != null) joinButton.interactable = true;
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