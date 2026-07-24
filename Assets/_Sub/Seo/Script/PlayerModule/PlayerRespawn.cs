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
    [SerializeField]
    private int currentCheckpointIndex = -1;

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
                        $"현재 체크포인트 Index = {currentCheckpointIndex} " +
                        "마지막 체크포인트로 강제 이동합니다."
                    );

                    Respawn();

                    holdTimer = 0f;
                }
            }
            else
            {
                if (holdTimer > 0f)
                    holdTimer = 0f;
            }
        }
    }

    // =========================================================
    // 개인 체크포인트 갱신
    // =========================================================

    [TargetRpc]
    public void TargetUpdateSpawnPointIfNewer(
        NetworkConnection target,
        Vector3 newPoint,
        int newCheckpointIndex)
    {
        // 이미 같은 위치 또는 더 뒤쪽 체크포인트를 찍었다면 무시
        if (newCheckpointIndex <= currentCheckpointIndex)
        {
            Debug.Log(
                $"[체크포인트] {gameObject.name} | " +
                $"기존 Index = {currentCheckpointIndex} | " +
                $"새 Index = {newCheckpointIndex} | " +
                $"이미 더 뒤쪽 체크포인트를 저장했으므로 갱신하지 않음"
            );

            return;
        }

        currentCheckpointIndex = newCheckpointIndex;
        currentSpawnPoint = newPoint;

        Debug.Log(
            $"[체크포인트] {gameObject.name} 개인 체크포인트 갱신 완료 | " +
            $"Index = {currentCheckpointIndex} | " +
            $"스폰 위치 = {currentSpawnPoint}"
        );
    }

    // =========================================================
    // 챕터4 팀 전체 체크포인트 갱신
    // =========================================================

    [ClientRpc]
    public void RpcUpdateSpawnPointIfNewer(
        Vector3 newPoint,
        int newCheckpointIndex)
    {
        // 챕터4에서도 이미 더 뒤쪽 체크포인트를 찍었다면
        // 앞쪽 체크포인트로 절대 되돌아가지 않음
        if (newCheckpointIndex <= currentCheckpointIndex)
        {
            Debug.Log(
                $"[챕터4 체크포인트] {gameObject.name} | " +
                $"기존 Index = {currentCheckpointIndex} | " +
                $"새 Index = {newCheckpointIndex} | " +
                $"이미 더 뒤쪽 체크포인트를 저장했으므로 갱신하지 않음"
            );

            return;
        }

        currentCheckpointIndex = newCheckpointIndex;
        currentSpawnPoint = newPoint;

        Debug.Log(
            $"[챕터4 체크포인트] {gameObject.name} 팀 체크포인트 갱신 완료 | " +
            $"Index = {currentCheckpointIndex} | " +
            $"스폰 위치 = {currentSpawnPoint}"
        );
    }

    // =========================================================
    // 기존 함수 호환용
    // =========================================================

    [ClientRpc]
    public void RpcUpdateSpawnPoint(Vector3 newPoint)
    {
        currentSpawnPoint = newPoint;
    }

    [TargetRpc]
    public void TargetUpdateSpawnPoint(
        NetworkConnection target,
        Vector3 newPoint)
    {
        currentSpawnPoint = newPoint;
    }

    // =========================================================
    // 리스폰
    // =========================================================

    public void Respawn()
    {
        if (!isLocalPlayer)
            return;

        string currentScene =
            SceneManager.GetActiveScene().name;

        // 챕터4는 기존대로 모든 플레이어가 함께 리스폰
        if (teamRespawnScenes.Contains(currentScene))
        {
            CmdTeamRespawn();
        }
        else
        {
            // 일반 맵은 본인만 리스폰
            StartCoroutine(DoLocalRespawnRoutine());
        }
    }

    // =========================================================
    // 챕터4 팀 전체 리스폰
    // =========================================================

    [Command]
    private void CmdTeamRespawn()
    {
        PlayerRespawn[] allPlayers =
            FindObjectsByType<PlayerRespawn>(
                FindObjectsInactive.Exclude
            );

        foreach (var player in allPlayers)
        {
            if (player != null &&
                player.connectionToClient != null)
            {
                player.TargetForceRespawn(
                    player.connectionToClient
                );
            }
        }
    }

    [TargetRpc]
    private void TargetForceRespawn(
        NetworkConnection target)
    {
        StartCoroutine(
            DoLocalRespawnRoutine()
        );
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
}