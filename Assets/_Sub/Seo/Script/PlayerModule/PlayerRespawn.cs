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

    // 💡 [1번 리팩토링 적용]: 매 프레임 문자열 연산을 막는 씬 캐싱 변수
    private bool isLobbyOrMainScene = false;

    void Awake()
    {
        controller = GetComponent<PlayerController>();
    }

    public override void OnStartLocalPlayer()
    {
        base.OnStartLocalPlayer();
        currentSpawnPoint = transform.position;

        // 시작 시점에 씬 이름을 한 번만 확인
        string sceneName = SceneManager.GetActiveScene().name;
        isLobbyOrMainScene = (sceneName == "Lobby" || sceneName == "Main");
    }

    // 💡 [2번 리팩토링 적용]: PlayerController에서 순서대로 호출
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

    // 💡 [3번 핵심 고침]: FindObjectsByType 완전 제거 -> CoopPlayerManager 활용으로 서버 성능 최적화
    [Command]
    private void CmdTeamRespawn()
    {
        List<PlayerRespawn> allPlayers = CoopPlayerManager.GetPlayerComponents<PlayerRespawn>();

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
        if (controller != null && controller.knockback != null) controller.knockback.ResetKnockback();
        if (controller != null && controller.rb != null)
        {
            controller.rb.linearVelocity = Vector2.zero;
            controller.rb.angularVelocity = 0f;
        }

        // Mirror NetworkTransform 위치 동기화 오차 보정 루프
        for (int i = 0; i < 10; i++)
        {
            transform.position = currentSpawnPoint;
            if (controller != null && controller.rb != null) controller.rb.linearVelocity = Vector2.zero;
            yield return new WaitForFixedUpdate();
        }

        if (controller != null && controller.rb != null) controller.rb.linearVelocity = Vector2.zero;
    }

    // 💡 [CoopCheckpoint 연동]: 개인/팀 체크포인트 획득 시 부활 위치 동기화
    [TargetRpc]
    public void TargetUpdateSpawnPoint(NetworkConnection target, Vector3 newSpawnPoint)
    {
        currentSpawnPoint = newSpawnPoint;
    }

    [ClientRpc]
    public void RpcUpdateSpawnPoint(Vector3 newSpawnPoint)
    {
        currentSpawnPoint = newSpawnPoint;
    }
}