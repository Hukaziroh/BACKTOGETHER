using UnityEngine;
using EpicTransport;
using Mirror;
using System.Collections;

public class PrivateLobbyManager : MonoBehaviour
{
    public void OnStartPrivateHostClicked()
    {
        EOSLobby eosLobby = NetworkManager.singleton.GetComponent<EOSLobby>();

        if (eosLobby == null)
        {
            Debug.LogError("NetworkManager에서 EOSLobby 컴포넌트를 찾을 수 없습니다!");
            return;
        }

        uint maxPlayers = 4;
        Epic.OnlineServices.Lobby.LobbyPermissionLevel permissionLevel = Epic.OnlineServices.Lobby.LobbyPermissionLevel.Inviteonly;
        bool presenceEnabled = true;

        eosLobby.CreateLobby(maxPlayers, permissionLevel, presenceEnabled);
        StartCoroutine(StartHostWithDelay());
    }

    private IEnumerator StartHostWithDelay()
    {
        yield return new WaitForSeconds(1f);

        Debug.Log("에픽 세션 안정화 확인 - Mirror 호스트를 시작합니다.");
        NetworkManager.singleton.StartHost();
    }
}