using UnityEngine;
using EpicTransport;
using Mirror;
using System.Collections;
using UnityEngine.UI;

public class PrivateLobbyManager : MonoBehaviour
{
    [Header("UI 연결")]
    public Button hostCreateButton;

    // 생성된 숏코드를 UI에 전달하기 위한 전역 변수
    public static string currentShortCode = "";

    public void OnStartPrivateHostClicked()
    {
        if (hostCreateButton != null) hostCreateButton.interactable = false;
        StartCoroutine(CleanAndCreateLobbyRoutine());
    }

    private IEnumerator CleanAndCreateLobbyRoutine()
    {
        // 1. 기존 네트워크 정리
        if (NetworkClient.active) NetworkManager.singleton.StopClient();
        if (NetworkServer.active) NetworkManager.singleton.StopHost();

        EOSLobby eosLobby = NetworkManager.singleton.GetComponent<EOSLobby>();
        if (eosLobby != null) eosLobby.LeaveLobby();

        yield return new WaitForSeconds(1.5f); // 세션 청소 대기

        // 2. 6자리 숏코드 발급
        currentShortCode = GenerateShortCode();
        Debug.Log("[로비 시스템] 발급된 숏코드: " + currentShortCode);

        // 3. FakeByte 내장 함수로 안전하게 방 생성
        uint maxPlayers = 4;
        Epic.OnlineServices.Lobby.LobbyPermissionLevel permissionLevel = Epic.OnlineServices.Lobby.LobbyPermissionLevel.Publicadvertised;
        bool presenceEnabled = true;

        eosLobby.CreateLobby(maxPlayers, permissionLevel, presenceEnabled);

        // 🌟 [핵심] FakeByte 내장 함수(GetCurrentLobbyId)를 사용해 로비 ID가 나올 때까지 대기
        while (string.IsNullOrEmpty(eosLobby.GetCurrentLobbyId()))
        {
            yield return null;
        }

        // 🌟 [핵심] FakeByte 내장 함수(UpdateLobbyAttribute)를 사용해 에러 없이 숏코드 간판 등록!
        eosLobby.UpdateLobbyAttribute("SHORTCODE", currentShortCode);

        yield return new WaitForSeconds(0.5f); // 등록 안정화 대기

        // 4. Mirror 호스트 시작
        NetworkManager.singleton.StartHost();
        if (hostCreateButton != null) hostCreateButton.interactable = true;
    }

    // 영문+숫자 6자리 무작위 생성
    private string GenerateShortCode()
    {
        const string chars = "ABCDEFGHIJKLMNOPQRSTUVWXYZ0123456789";
        string code = "";
        for (int i = 0; i < 6; i++) code += chars[Random.Range(0, chars.Length)];
        return code;
    }
}