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
    [SyncVar]
    public Vector3 currentSpawnPoint;

    [SyncVar]
    [Tooltip("현재 플레이어가 도달한 가장 뒤쪽 체크포인트 번호")]
    public int currentCheckpointIndex = -1;

    [SyncVar]
    private bool isRespawning = false;

    private float holdTimer = 0f;
    private const float HOLD_TIME_TO_RESPAWN = 2f;

    [Header("팀 리스폰 씬 설정")]
    public List<string> teamRespawnScenes = new List<string> { "chapter4" };

    void Awake()
    {
        controller = GetComponent<PlayerController>();
    }

    public override void OnStartServer()
    {
        currentSpawnPoint = transform.position;
        currentCheckpointIndex = -1;
    }

    void Update()
    {
        if (!isLocalPlayer || isRespawning)
            return;

        // 🌟 New Input System의 restartAction(패드 RT 홀드 등) 상태 감지
        bool isHoldingRestart = controller.input != null &&
                                controller.input.restartAction != null &&
                                controller.input.restartAction.IsPressed();

        if (isHoldingRestart)
        {
            holdTimer += Time.deltaTime;

            if (holdTimer >= HOLD_TIME_TO_RESPAWN)
            {
                holdTimer = 0f;
                CmdRequestRespawn();
            }
        }
        else
        {
            holdTimer = 0f;
        }
    }

    [Command]
    public void CmdRequestRespawn()
    {
        if (isRespawning) return;

        string sceneName = SceneManager.GetActiveScene().name;

        if (teamRespawnScenes.Contains(sceneName))
        {
            StartCoroutine(RespawnAllPlayersRoutine());
        }
        else
        {
            StartCoroutine(RespawnSinglePlayerRoutine());
        }
    }

    private IEnumerator RespawnSinglePlayerRoutine()
    {
        isRespawning = true;

        transform.position = currentSpawnPoint;
        Physics2D.SyncTransforms();

        yield return new WaitForSeconds(0.2f);

        isRespawning = false;
    }

    private IEnumerator RespawnAllPlayersRoutine()
    {
        PlayerRespawn[] allPlayers = FindObjectsByType<PlayerRespawn>();

        foreach (var p in allPlayers)
        {
            if (p != null)
            {
                p.isRespawning = true;
            }
        }

        yield return new WaitForSeconds(0.2f);

        Vector3 centerSpawnPoint = currentSpawnPoint;

        foreach (var p in allPlayers)
        {
            if (p != null)
            {
                int playerIndex = 0;
                CoopPlayerIdentity identity = p.GetComponent<CoopPlayerIdentity>();
                if (identity != null)
                {
                    playerIndex = identity.playerIndex;
                }
                float[] spawnOffsets = { -1.5f, -0.5f, 0.5f, 1.5f };
                float myOffset = spawnOffsets[playerIndex % 4];

                Vector3 finalSpawnPos = centerSpawnPoint + new Vector3(myOffset, 0, 0);

                p.transform.position = finalSpawnPos;
                Physics2D.SyncTransforms();
            }
        }

        yield return new WaitForSeconds(0.2f);

        foreach (var p in allPlayers)
        {
            if (p != null)
            {
                p.isRespawning = false;
            }
        }
    }

    [Server]
    public void UpdateCheckpoint(Vector3 newPos, int newIndex)
    {
        if (newIndex > currentCheckpointIndex)
        {
            currentCheckpointIndex = newIndex;
            currentSpawnPoint = newPos;
        }
    }
}