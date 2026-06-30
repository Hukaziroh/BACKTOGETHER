using UnityEngine;
using EpicTransport;
using Mirror;

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
        NetworkManager.singleton.StartHost();
    }
}