using UnityEngine;
using Mirror;

public class PlayerSyncJump : NetworkBehaviour
{
    [Header("동기화 상태")]
    [SyncVar] public bool isInSyncZone = false;
    [HideInInspector] public CoopSyncJumpZone serverZone;

    private PlayerController playerController;
    private Rigidbody2D rb;
    private PlayerGravityController gravityModule;

    void Awake()
    {
        playerController = GetComponent<PlayerController>();
        rb = GetComponent<Rigidbody2D>();
        gravityModule = GetComponent<PlayerGravityController>();
    }

    void Update()
    {
        if (isLocalPlayer && isInSyncZone)
        {
            if (playerController != null && playerController.jumpAction.WasPressedThisFrame())
            {
                CmdTriggerSyncJump();
            }
        }
    }

    [Command]
    private void CmdTriggerSyncJump()
    {
        if (serverZone != null)
        {
            serverZone.BroadcastSyncJump(gameObject);
        }
    }

    [TargetRpc]
    public void TargetDoJump(NetworkConnection target)
    {
        if (playerController != null)
        {
            // PlayerController의 Jump() 메서드가 private라면 public으로 변경하거나,
            // 아래와 같이 CallCombinedJump()를 활용합니다.
            playerController.CallCombinedJump();
        }
    }
}