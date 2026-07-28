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
    [SyncVar] // 서버에서 값이 바뀌면 클라이언트에 자동 동기화
    public Vector3 currentSpawnPoint;

    [SyncVar] // 체크포인트 인덱스도 자동 동기화
    [Tooltip("현재 플레이어가 도달한 가장 뒤쪽 체크포인트 번호")]
    public int currentCheckpointIndex = -1;

    [SyncVar] // 리스폰 상태 동기화 (이 값이 true면 클라이언트 조작 자동 차단)
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
        // 🌟 서버 시작 시에만 초기화하도록 변경
        currentSpawnPoint = transform.position;
        currentCheckpointIndex = -1;
    }

    void Update()
    {
        // isRespawning이 SyncVar이므로 서버가 true로 바꾸면 클라이언트 입력은 자동으로 막힘
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
                        CmdStartTeamRespawnSequence();
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
    // 🌟 일반 맵 개인 리스폰 처리 (완벽한 서버 권한)
    // =========================================================
    [Command]
    private void CmdLocalRespawn()
    {
        // 클라이언트에게 Rpc를 쏘지 않고 서버에서 바로 코루틴 실행
        StartCoroutine(ServerDoLocalRespawnRoutine());
    }

    [Server]
    private IEnumerator ServerDoLocalRespawnRoutine()
    {
        isRespawning = true;

        // 물리 튀는 현상(러버밴딩)을 막기 위해 simulated 끄기 금지. 속도만 리셋.
        if (controller.rb != null)
        {
            controller.rb.linearVelocity = Vector2.zero;
            controller.rb.angularVelocity = 0f;
        }
        if (controller.knockback != null) controller.knockback.ResetKnockback();

        // 서버에서 위치 강제 이동 (이후 NetworkTransform이 알아서 클라이언트로 쏴줌)
        transform.position = currentSpawnPoint;
        Physics2D.SyncTransforms();

        yield return new WaitForFixedUpdate();

        isRespawning = false;
    }

    // =========================================================
    // 🌟 챕터 4 팀 전체 리스폰 (서버가 다 계산해서 꽂아줌)
    // =========================================================
    [Command]
    private void CmdStartTeamRespawnSequence()
    {
        StartCoroutine(ServerRespawnSequence(currentSpawnPoint));
    }

    [Server]
    private IEnumerator ServerRespawnSequence(Vector3 centerSpawnPoint)
    {
        List<PlayerRespawn> allPlayers = CoopPlayerManager.GetPlayerComponents<PlayerRespawn>();
        Debug.Log("====== TEAM RESPAWN (SERVER) ======");
        Debug.Log("Player Count : " + allPlayers.Count);

        // 1. 모든 플레이어 정지
        foreach (var p in allPlayers)
        {
            if (p != null)
            {
                p.isRespawning = true; // SyncVar를 통해 클라이언트 자동 입력 차단
                if (p.controller.rb != null)
                {
                    p.controller.rb.linearVelocity = Vector2.zero;
                    p.controller.rb.angularVelocity = 0f;
                }
                if (p.controller.knockback != null) p.controller.knockback.ResetKnockback();
            }
        }

        yield return new WaitForSeconds(0.2f);

        // 2. 서버에서 모든 플레이어 위치 재배치
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

                // 서버 위치 이동 (마찬가지로 NetworkTransform이 동기화)
                p.transform.position = finalSpawnPos;
                Physics2D.SyncTransforms();
            }
        }

        yield return new WaitForSeconds(0.2f);

        // 3. 리스폰 해제
        foreach (var p in allPlayers)
        {
            if (p != null)
            {
                p.isRespawning = false;
            }
        }
    }

    // =========================================================
    // 체크포인트 갱신 (초간단 SyncVar 갱신)
    // =========================================================
    [Server]
    public void UpdateCheckpoint(Vector3 newPos, int newIndex)
    {
        // 서버에서 이 메서드만 부르면, SyncVar에 의해 클라이언트 데이터는 알아서 동기화됨
        if (newIndex > currentCheckpointIndex)
        {
            currentSpawnPoint = newPos;
            currentCheckpointIndex = newIndex;
        }
    }
}