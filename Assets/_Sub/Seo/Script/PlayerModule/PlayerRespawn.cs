using UnityEngine;
using Mirror;
using UnityEngine.InputSystem;
using UnityEngine.SceneManagement;
using System.Collections;
using System.Collections.Generic;

public class PlayerRespawn : NetworkBehaviour
{
    private PlayerController controller;

    [Header("스폰 시스템")]
    public Vector3 currentSpawnPoint;

    [Tooltip("현재 플레이어가 도달한 가장 뒤쪽 체크포인트 번호")]
    public int currentCheckpointIndex = -1; // 서버에서도 접근 가능하도록 public으로 변경

    private float holdTimer = 0f;
    private const float HOLD_TIME_TO_RESPAWN = 2f;

    [Header("팀 리스폰 씬 설정")]
    public List<string> teamRespawnScenes = new List<string> { "chapter4" };

    void Awake()
    {
        controller = GetComponent<PlayerController>();
    }

    public override void OnStartLocalPlayer()
    {
        currentSpawnPoint = transform.position;
        currentCheckpointIndex = -1;
    }

    void Update()
    {
        if (!isLocalPlayer)
            return;

        string sceneName = SceneManager.GetActiveScene().name;

        if (sceneName == "Lobby" || sceneName == "Main")
            return;

        if (Keyboard.current != null)
        {
            if (Keyboard.current.rKey.isPressed)
            {
                holdTimer += Time.deltaTime;

                if (holdTimer >= HOLD_TIME_TO_RESPAWN)
                {
                    Debug.Log(
                        "[시스템] 비상 탈출! " +
                        $"현재 체크포인트 Index = {currentCheckpointIndex}"
                    );

                    // 챕터 4 등 팀 리스폰이 필요한 씬인지 확인
                    if (teamRespawnScenes.Contains(sceneName) || sceneName.Contains("Chapter4") || sceneName.Contains("Stage4"))
                    {
                        // [수정됨] 내 클라이언트가 알고 있는 확실한 스폰 위치를 서버로 보냅니다.
                        CmdTeamRespawn(currentSpawnPoint);
                    }
                    else
                    {
                        CmdLocalRespawn();
                    }

                    holdTimer = 0f;
                }
            }
            else
            {
                holdTimer = 0f;
            }
        }
    }

    [Command]
    private void CmdLocalRespawn()
    {
        TargetForceRespawn(connectionToClient);
    }

    // =========================================================
    // 체크포인트 갱신 (서버 & 클라이언트 동기화)
    // =========================================================

    // [추가됨] 서버 쪽 변수도 갱신하여 꼬임을 방지합니다.
    [Server]
    public void ServerUpdateSpawnPointIfNewer(Vector3 newPos, int newIndex)
    {
        if (newIndex > currentCheckpointIndex)
        {
            currentSpawnPoint = newPos;
            currentCheckpointIndex = newIndex;
        }
    }

    [ClientRpc]
    public void RpcUpdateSpawnPointIfNewer(Vector3 newPos, int newIndex)
    {
        if (newIndex > currentCheckpointIndex)
        {
            currentSpawnPoint = newPos;
            currentCheckpointIndex = newIndex;
        }
    }

    [TargetRpc]
    public void TargetUpdateSpawnPointIfNewer(NetworkConnection target, Vector3 newPos, int newIndex)
    {
        if (newIndex > currentCheckpointIndex)
        {
            currentSpawnPoint = newPos;
            currentCheckpointIndex = newIndex;
        }
    }

    // =========================================================
    // 일반 맵 개인 리스폰 처리
    // =========================================================
    [TargetRpc]
    private void TargetForceRespawn(NetworkConnection target)
    {
        StartCoroutine(DoLocalRespawnRoutine());
    }

    private IEnumerator DoLocalRespawnRoutine()
    {
        if (controller.knockback != null)
        {
            controller.knockback.ResetKnockback();
        }

        if (controller.rb != null)
        {
            controller.rb.linearVelocity = Vector2.zero;
            controller.rb.angularVelocity = 0f;
        }

        transform.position = currentSpawnPoint;

        Physics2D.SyncTransforms();

        for (int i = 0; i < 3; i++)
        {
            if (controller.rb != null)
            {
                controller.rb.linearVelocity = Vector2.zero;
            }

            yield return new WaitForFixedUpdate();
        }
    }

    // =========================================================
    // 🌟 [수정 완료] 챕터4 팀 전체 리스폰
    // =========================================================

    [Command]
    private void CmdTeamRespawn(Vector3 clientSpawnPos)
    {
        List<PlayerRespawn> allPlayers = CoopPlayerManager.GetPlayerComponents<PlayerRespawn>();

        foreach (var player in allPlayers)
        {
            if (player != null)
            {
                player.RpcExecuteTeamRespawn(clientSpawnPos);
            }
        }
    }
    [ClientRpc]
    private void RpcExecuteTeamRespawn(Vector3 centerSpawnPoint)
    {
        StartCoroutine(DoTeamRespawnRoutine(centerSpawnPoint));
    }

    private IEnumerator DoTeamRespawnRoutine(Vector3 centerSpawnPoint)
    {
        if (controller.knockback != null)
        {
            controller.knockback.ResetKnockback();
        }

        if (controller.rb != null)
        {
            controller.rb.simulated = false; 
            controller.rb.linearVelocity = Vector2.zero;
            controller.rb.angularVelocity = 0f;
        }

        int playerIndex = 0;
        CoopPlayerIdentity identity = GetComponent<CoopPlayerIdentity>();
        if (identity != null)
        {
            playerIndex = identity.playerIndex;
        }

        float[] spawnOffsets = { -1.5f, -0.5f, 0.5f, 1.5f };
        float myOffset = spawnOffsets[playerIndex % 4];

        Vector3 finalSpawnPos = centerSpawnPoint + new Vector3(myOffset, 0, 0);
        transform.position = finalSpawnPos;

        Physics2D.SyncTransforms();
        yield return new WaitForSeconds(0.3f);

        if (controller.rb != null)
        {
            controller.rb.linearVelocity = Vector2.zero;
            controller.rb.simulated = true; 
        }
    }
}
