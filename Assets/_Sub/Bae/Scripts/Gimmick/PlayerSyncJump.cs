using UnityEngine;
using Mirror;

public class PlayerSyncJump : NetworkBehaviour
{
    [Header("동기화 상태")]
    public bool isInSyncZone = false;

    private CoopSyncJumpZone currentZone;
    private PlayerController playerController;

    void Awake()
    {
        playerController = GetComponent<PlayerController>();
    }

    [TargetRpc]
    public void TargetSetZone(NetworkConnection target, CoopSyncJumpZone zone)
    {
        currentZone = zone;
        isInSyncZone = true;
    }

    [TargetRpc]
    public void TargetClearZone(NetworkConnection target)
    {
        currentZone = null;
        isInSyncZone = false;
    }

    void Update()
    {
        if (!isLocalPlayer || !isInSyncZone || currentZone == null) return;

        if (playerController != null && playerController.jumpAction.WasPressedThisFrame())
        {
            CmdTriggerSyncJump(currentZone, gameObject);
        }
    }

    [Command]
    private void CmdTriggerSyncJump(CoopSyncJumpZone zone, GameObject initiator)
    {
        if (zone != null)
        {
            zone.BroadcastSyncJump(initiator);
        }
    }

    [TargetRpc]
    public void TargetDoJump(NetworkConnection target)
    {
        if (playerController != null)
        {
            playerController.CallCombinedJump();
        }
    }
}