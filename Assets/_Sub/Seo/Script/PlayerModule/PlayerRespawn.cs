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
    private float holdTimer = 0f;
    private const float HOLD_TIME_TO_RESPAWN = 2f;

    [Header("팀 리스폰 씬 설정")]
    public List<string> teamRespawnScenes = new List<string> { "Chapter4" };

    void Awake()
    {
        controller = GetComponent<PlayerController>();
    }

    public override void OnStartLocalPlayer()
    {
        currentSpawnPoint = transform.position;
    }

    void Update()
    {
        if (!isLocalPlayer) return;

        string sceneName = SceneManager.GetActiveScene().name;
        if (sceneName == "Lobby" || sceneName == "Main") return;

        if (Keyboard.current != null)
        {
            if (Keyboard.current.rKey.isPressed)
            {
                holdTimer += Time.deltaTime;
                if (holdTimer >= HOLD_TIME_TO_RESPAWN)
                {
                    Debug.Log("[시스템] 비상 탈출! 마지막 체크포인트로 강제 이동합니다.");
                    Respawn();
                    holdTimer = 0f;
                }
            }
            else
            {
                if (holdTimer > 0f) holdTimer = 0f;
            }
        }
    }

    [ClientRpc]
    public void RpcUpdateSpawnPoint(Vector3 newPoint)
    {
        currentSpawnPoint = newPoint;
    }

    [TargetRpc]
    public void TargetUpdateSpawnPoint(NetworkConnection target, Vector3 newPoint)
    {
        currentSpawnPoint = newPoint;
    }

    public void Respawn()
    {
        if (!isLocalPlayer) return;

        string currentScene = SceneManager.GetActiveScene().name;
        if (teamRespawnScenes.Contains(currentScene)) CmdTeamRespawn();
        else StartCoroutine(DoLocalRespawnRoutine());
    }

    [Command]
    private void CmdTeamRespawn()
    {
        PlayerRespawn[] allPlayers = FindObjectsByType<PlayerRespawn>(FindObjectsInactive.Exclude);
        foreach (var player in allPlayers)
        {
            if (player != null && player.connectionToClient != null)
            {
                player.TargetForceRespawn(player.connectionToClient);
            }
        }
    }

    [TargetRpc]
    private void TargetForceRespawn(NetworkConnection target)
    {
        StartCoroutine(DoLocalRespawnRoutine());
    }

    private IEnumerator DoLocalRespawnRoutine()
    {
        if (controller.knockback != null) controller.knockback.ResetKnockback();

        if (controller.rb != null)
        {
            controller.rb.linearVelocity = Vector2.zero;
            controller.rb.angularVelocity = 0f;
        }

        // 최적화: 과도한 fixedUpdate 대기를 줄이고 확실히 물리 엔진을 리셋
        transform.position = currentSpawnPoint;
        Physics2D.SyncTransforms(); // 즉시 콜라이더 위치 강제 동기화

        for (int i = 0; i < 3; i++) // 10프레임 대기는 지연이 너무 김, 3프레임으로 안정화
        {
            if (controller.rb != null) controller.rb.linearVelocity = Vector2.zero;
            yield return new WaitForFixedUpdate();
        }
    }
}