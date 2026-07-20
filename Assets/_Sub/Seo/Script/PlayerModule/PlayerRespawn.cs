using UnityEngine;
using Mirror;
using UnityEngine.InputSystem;
using UnityEngine.SceneManagement; 

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

        if (SceneManager.GetActiveScene().name == "chapter5")
        {           
            CmdTeamRespawn();
        }
        else
        {
            DoLocalRespawn();
        }
    }

    [Command]
    private void CmdTeamRespawn()
    {
        RpcTeamRespawn();
    }

    [ClientRpc]
    private void RpcTeamRespawn()
    {
        if (isLocalPlayer)
        {
            DoLocalRespawn();
        }
    }
    private void DoLocalRespawn()
    {
        transform.position = currentSpawnPoint;
        controller.rb.linearVelocity = Vector2.zero; 

        if (controller.knockback != null)
        {
            controller.knockback.ResetKnockback(); 
        }
    }

    private void CheckRespawn()
    {
        if (transform.position.y < -50f )
        {
            Respawn();
        }
    }
}