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
        string currentSceneName = SceneManager.GetActiveScene().name;
        bool isLobby = currentSceneName == lobbySceneName || currentSceneName == "NLobby";

        if (!isLobby)
        {
            TeleportToTeam();
        }
    }

    [Server]
    private void TeleportToTeam()
    {
        var allPlayers = CoopPlayerManager.GetPlayerComponents<CoopLateJoinHandler>();
        foreach (var player in allPlayers)
        {
            if (player != null && player.gameObject != this.gameObject)
            {
                Vector3 targetPosition = player.transform.position;
                
                PlayerController playerController = GetComponent<PlayerController>();
                if (playerController != null)
                {
                    playerController.currentSpawnPoint = targetPosition;
                }

                TargetTeleportToPlayer(connectionToClient, targetPosition);

                PlayerCombineHandler teamCombine = player.GetComponent<PlayerCombineHandler>();
                if (teamCombine != null && teamCombine.isCombined)
                {
                    PlayerCombineHandler myCombine = GetComponent<PlayerCombineHandler>();
                    if (myCombine != null)
                    {
                        myCombine.JoinExistingCombine(teamCombine.bodyTarget);
                    }
                }

                // Debug.Log($"[서버] 중도 참여자 감지! 팀원({player.name})의 위치로 텔레포트 명령을 전송합니다.");
                break;
            }
        }
    }

    [TargetRpc]
    private void TargetTeleportToPlayer(NetworkConnection target, Vector3 targetPos)
    {
        transform.position = targetPos;
        Rigidbody2D rb = GetComponent<Rigidbody2D>();
        if (rb != null)
        {
            rb.position = targetPos;
            rb.linearVelocity = Vector2.zero;
        }
        
        // Debug.Log($"[클라이언트] 팀원 위치({targetPos})로 스폰 위치가 안전하게 동기화되었습니다.");
    }
}
