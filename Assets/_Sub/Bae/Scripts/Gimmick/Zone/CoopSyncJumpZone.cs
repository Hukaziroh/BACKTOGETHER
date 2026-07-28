using UnityEngine;
using Mirror;
using System.Collections.Generic;

public class CoopSyncJumpZone : NetworkBehaviour
{
    [System.NonSerialized]
    public HashSet<GameObject> playersInZone = new HashSet<GameObject>();

    [ServerCallback]
    private void OnTriggerEnter2D(Collider2D other)
    {
        if (other.CompareTag("Player") && other.gameObject == other.transform.root.gameObject)
        {
            playersInZone.Add(other.gameObject);

            PlayerSyncJump syncJump = other.GetComponent<PlayerSyncJump>();
            if (syncJump != null)
            {
                syncJump.serverZone = this;
                syncJump.isInSyncZone = true;
            }
        }
    }

    [ServerCallback]
    private void OnTriggerExit2D(Collider2D other)
    {
        if (other.CompareTag("Player") && other.gameObject == other.transform.root.gameObject)
        {
            playersInZone.Remove(other.gameObject);

            PlayerSyncJump syncJump = other.GetComponent<PlayerSyncJump>();
            if (syncJump != null)
            {
                if (syncJump.serverZone == this) syncJump.serverZone = null;
                syncJump.isInSyncZone = false;
            }
        }
    }

    [Server]
    public void BroadcastSyncJump(GameObject initiator)
    {
        playersInZone.RemoveWhere(p => p == null || !p.activeInHierarchy);

        foreach (var player in playersInZone)
        {
            if (player != initiator)
            {
                PlayerSyncJump syncJump = player.GetComponent<PlayerSyncJump>();
                if (syncJump != null)
                {
                    syncJump.ServerDoJump();
                }
            }
        }
    }

    [Server]
    public void BroadcastCutJump(GameObject initiator)
    {
        foreach (var player in playersInZone)
        {
            if (player != null && player != initiator)
            {
                var syncJump = player.GetComponent<PlayerSyncJump>();
                if (syncJump != null)
                {
                    syncJump.ServerCutJump();
                }
            }
        }
    }
}