using UnityEngine;
using Mirror;

public class CoopCheckpoint : NetworkBehaviour
{
    [Header("스폰될 위치 (빈 오브젝트)")]
    public Transform spawnLocation;

    private void OnTriggerEnter2D(Collider2D other)
    {
        if (other.CompareTag("Player"))
        {
            PlayerController player = other.GetComponent<PlayerController>();
            if (player != null && player.isLocalPlayer)
            {
                player.SetSpawnPoint(spawnLocation.position);
            }
        }
    }
}