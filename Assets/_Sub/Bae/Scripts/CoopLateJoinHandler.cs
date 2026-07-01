using UnityEngine;
using Mirror;
using UnityEngine.SceneManagement;

public class CoopLateJoinHandler : NetworkBehaviour
{
    [Header("로비 씬 이름")]
    [Tooltip("로비에서는 팀원에게 텔레포트할 필요가 없으므로 씬 이름을 지정해줍니다.")]
    public string lobbySceneName = "Lobby";

    public override void OnStartServer()
    {
        base.OnStartServer();
        if (SceneManager.GetActiveScene().name != lobbySceneName)
        {
            TeleportToTeam();
        }
    }

    [Server]
    private void TeleportToTeam()
    { 
        CoopLateJoinHandler[] allPlayers = FindObjectsByType<CoopLateJoinHandler>(FindObjectsSortMode.None);

        foreach (var player in allPlayers)
        {
            if (player.gameObject != this.gameObject)
            {
                transform.position = player.transform.position;
                Debug.Log("중도 참여 감지! 기존 팀원의 위치로 자동 합류했습니다.");
                break; 
            }
        }
    }
}