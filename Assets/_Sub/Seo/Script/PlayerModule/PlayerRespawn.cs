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
    [Tooltip("여기에 적힌 씬에서는 1명만 죽어도 4명이 다 같이 부활합니다. (로프 맵 등)")]
    public List<string> teamRespawnScenes = new List<string> { "Chapter4" };

    // 💡 [리팩토링]: 씬 이름 비교 결과 캐싱 변수
    private bool isLobbyOrMainScene = false;

    void Awake()
    {
        controller = GetComponent<PlayerController>();
    }

    public override void OnStartLocalPlayer()
    {
        base.OnStartLocalPlayer();
        currentSpawnPoint = transform.position;

        // 💡 시작 시점에 한 번만 씬 체크
        string sceneName = SceneManager.GetActiveScene().name;
        isLobbyOrMainScene = (sceneName == "Lobby" || sceneName == "Main");
    }

    // 💡 [리팩토링]: PlayerController에서 순서대로 호출
    public void CustomUpdate()
    {
        if (isLobbyOrMainScene) return;

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
                holdTimer = 0f;
            }
        }
    }

    public void Respawn()
    {
        if (!isLocalPlayer) return;

        string currentScene = SceneManager.GetActiveScene().name;
        if (teamRespawnScenes.Contains(currentScene))
        {
            CmdTeamRespawn();
        }
        else
        {
            StartCoroutine(DoLocalRespawnRoutine());
        }
    }

    [Command]
    private void CmdTeamRespawn()
    {
        // 💡 CoopPlayerManager를 활용하여 FindObjectsByType 연산 제거
        var allPlayers = CoopPlayerManager.GetPlayerComponents<PlayerRespawn>();

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
        for (int i = 0; i < 10; i++)
        {
            transform.position = currentSpawnPoint;
            if (controller.rb != null) controller.rb.linearVelocity = Vector2.zero;
            yield return new WaitForFixedUpdate();
        }

        if (controller.rb != null) controller.rb.linearVelocity = Vector2.zero;
    }
}