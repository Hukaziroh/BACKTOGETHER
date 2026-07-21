using UnityEngine;
using Mirror;
using UnityEngine.InputSystem;
using UnityEngine.SceneManagement;
using System.Collections; // 코루틴 사용을 위해 필수 추가

public class PlayerRespawn : NetworkBehaviour
{
    private PlayerController controller;

    [Header("스폰 시스템")]
    public Vector3 currentSpawnPoint;
    private float holdTimer = 0f;
    private const float HOLD_TIME_TO_RESPAWN = 2f;

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
                if (holdTimer > 0f)
                {
                    holdTimer = 0f;
                }
            }
        }
    }

    void FixedUpdate()
    {
        if (!isLocalPlayer) return;
        CheckRespawn();
    }

    public void SetSpawnPoint(Vector3 newPoint)
    {
        if (!isLocalPlayer) return;
        currentSpawnPoint = newPoint;
    }

    public void Respawn()
    {
        if (!isLocalPlayer) return;

        if (SceneManager.GetActiveScene().name == "RopeTest")
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
        PlayerRespawn[] allPlayers = FindObjectsByType<PlayerRespawn>(FindObjectsSortMode.None);

        foreach (var player in allPlayers)
        {
            player.TargetForceRespawn(player.connectionToClient);
        }
    }

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

        for (int i = 0; i < 10; i++)
        {
            transform.position = currentSpawnPoint;
            controller.rb.linearVelocity = Vector2.zero;
            yield return new WaitForFixedUpdate();
        }
    }


    private void CheckRespawn()
    {
        string sceneName = SceneManager.GetActiveScene().name;
        if (sceneName == "Lobby" || sceneName == "Main")
        {
            return;
        }
        if (transform.position.y < -50f)
        {
            Respawn();
        }
    }
}