using UnityEngine;
using Mirror;

public class PlayerSyncJump : NetworkBehaviour
{
    [Header("동기화 상태")]
    [SyncVar] public bool isInSyncZone = false;
    [HideInInspector] public CoopSyncJumpZone serverZone;

    private PlayerController playerController;

    void Awake()
    {
        playerController = GetComponent<PlayerController>();
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

    [Server]
    public void ServerDoJump()
    {
        if (playerController != null)
        {
            playerController.CallCombinedJump();
        }
    }

    [Command]
    public void CmdCutSyncJump()
    {
        if (serverZone != null) serverZone.BroadcastCutJump(gameObject);
    }

    [Server]
    public void ServerCutJump()
    {
        if (playerController != null)
        {
            playerController.ApplyShortJump();
        }
    }
}