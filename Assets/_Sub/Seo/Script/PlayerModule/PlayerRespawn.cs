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
    public int currentCheckpointIndex = -1;

    private float holdTimer = 0f;
    private const float HOLD_TIME_TO_RESPAWN = 2f;

    [Header("팀 리스폰 씬 설정")]
    public List<string> teamRespawnScenes = new List<string> { "chapter4" };

    private bool isRespawning = false;

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
        if (!isLocalPlayer || isRespawning)
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
                    holdTimer = 0f;

                    if (teamRespawnScenes.Contains(sceneName) || sceneName.Contains("chapter4"))
                    {
                        CmdStartTeamRespawnSequence(currentSpawnPoint);
                    }
                    else
                    {
                        CmdLocalRespawn();
                    }
                }
            }
            else
            {
                holdTimer = 0f;
            }
        }
    }

    // =========================================================
    // 일반 맵 개인 리스폰 처리
    // =========================================================
    [Command]
    private void CmdLocalRespawn()
    {
        TargetForceRespawn(connectionToClient);
    }

    [TargetRpc]
    private void TargetForceRespawn(NetworkConnection target)
    {
        StartCoroutine(DoLocalRespawnRoutine());
    }

    private IEnumerator DoLocalRespawnRoutine()
    {
        isRespawning = true;
        if (controller.knockback != null) controller.knockback.ResetKnockback();
        if (controller.rb != null)
        {
            controller.rb.linearVelocity = Vector2.zero;
            controller.rb.angularVelocity = 0f;
        }

        transform.position = currentSpawnPoint;
        Physics2D.SyncTransforms();

        yield return new WaitForFixedUpdate();
        isRespawning = false;
    }

    // =========================================================
    // 🌟 챕터 4 팀 전체 리스폰 (3-Phase 동기화 구조)
    // =========================================================

    [Command]
    private void CmdStartTeamRespawnSequence(Vector3 clientSpawnPos)
    {
        StartCoroutine(ServerRespawnSequence(clientSpawnPos));
    }

    private IEnumerator ServerRespawnSequence(Vector3 centerSpawnPoint)
    {
        List<PlayerRespawn> allPlayers = CoopPlayerManager.GetPlayerComponents<PlayerRespawn>();
        Debug.Log("====== TEAM RESPAWN ======");
        Debug.Log("Player Count : " + allPlayers.Count);

        foreach (var p in allPlayers)
        {
            if (p != null) p.RpcPrepareRespawn();
        }
        yield return new WaitForSeconds(0.2f);

        foreach (var p in allPlayers)
        {
            if (p != null) p.RpcExecuteTeleport(centerSpawnPoint);
        }
        yield return new WaitForSeconds(0.2f);
        foreach (var p in allPlayers)
        {
            if (p != null) p.RpcFinishRespawn();
        }
    }

    [ClientRpc]
    private void RpcPrepareRespawn()
    {
        isRespawning = true;
        if (controller.knockback != null) controller.knockback.ResetKnockback();
        if (controller.rb != null)
        {
            controller.rb.simulated = false; 
            controller.rb.linearVelocity = Vector2.zero;
            controller.rb.angularVelocity = 0f;
        }
    }

    [ClientRpc]
    private void RpcExecuteTeleport(Vector3 centerSpawnPoint)
    {
        Debug.Log($"RpcExecuteTeleport : {name}");

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
    }

    [ClientRpc]
    private void RpcFinishRespawn()
    {
        if (controller.rb != null)
        {
            controller.rb.simulated = true; 
        }
        isRespawning = false; 
    }

    // =========================================================
    // 체크포인트 갱신 (서버 & 클라이언트 동기화)
    // =========================================================

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
}