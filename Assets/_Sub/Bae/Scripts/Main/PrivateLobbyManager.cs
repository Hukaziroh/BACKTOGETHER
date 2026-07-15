using UnityEngine;
using EpicTransport;
using Mirror;
using System.Collections;
using UnityEngine.UI;

public class PrivateLobbyManager : MonoBehaviour
{
    [Header("UI 연결")]
    public Button hostCreateButton;
    public Button joinButton; // 🌟 새로 추가됨: 참가 버튼도 같이 잠그기 위해

    // 생성된 숏코드를 UI에 전달하기 위한 전역 변수
    public static string currentShortCode = "";

    // 🌟 새로 추가됨: 시작하자마자 버튼을 잠그고 로그인을 기다림
    private void Start()
    {
        if (hostCreateButton != null) hostCreateButton.interactable = false;
        if (joinButton != null) joinButton.interactable = false;

        StartCoroutine(WaitForEpicLoginRoutine());
    }

    // 🌟 새로 추가됨: 401 권한 에러를 막아주는 핵심 대기 로직
    private IEnumerator WaitForEpicLoginRoutine()
    {
        Debug.Log("[로비 시스템] 에픽 서버 로그인 상태를 확인 중입니다...");

        while (string.IsNullOrEmpty(EOSSDKComponent.LocalUserProductIdString))
        {
            yield return null;
        }

        Debug.Log("[로비 시스템] 에픽 온라인 서비스 로그인 성공! 버튼을 활성화합니다.");
        if (hostCreateButton != null) hostCreateButton.interactable = true;
        if (joinButton != null) joinButton.interactable = true;
    }

    public void OnStartPrivateHostClicked()
    {
        if (hostCreateButton != null) hostCreateButton.interactable = false;
        StartCoroutine(CleanAndCreateLobbyRoutine());
    }

    private IEnumerator CleanAndCreateLobbyRoutine()
    {
        if (NetworkClient.active) NetworkManager.singleton.StopClient();
        if (NetworkServer.active) NetworkManager.singleton.StopHost();

        EOSLobby eosLobby = NetworkManager.singleton.GetComponent<EOSLobby>();
        if (eosLobby != null) eosLobby.LeaveLobby();

        yield return new WaitForSeconds(1.5f); 

        currentShortCode = GenerateShortCode();
        Debug.Log("[로비 시스템] 발급된 숏코드: " + currentShortCode);

        uint maxPlayers = 4;
        Epic.OnlineServices.Lobby.LobbyPermissionLevel permissionLevel = Epic.OnlineServices.Lobby.LobbyPermissionLevel.Publicadvertised;
        bool presenceEnabled = true;

        eosLobby.CreateLobby(maxPlayers, permissionLevel, presenceEnabled);

        while (string.IsNullOrEmpty(eosLobby.GetCurrentLobbyId()))
        {
            yield return null;
        }
        eosLobby.UpdateLobbyAttribute("SHORTCODE", currentShortCode);

        yield return new WaitForSeconds(0.5f); // 등록 안정화 대기

        // 4. Mirror 호스트 시작
        NetworkManager.singleton.StartHost();
        if (hostCreateButton != null) hostCreateButton.interactable = true;
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