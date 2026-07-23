using System.Collections.Generic;
using UnityEngine;
using Mirror;

public class CoopCheckpoint : NetworkBehaviour
{
    [Header("스폰될 위치 (빈 오브젝트)")]
    public Transform spawnLocation;

    [Header("협동 모드 설정")]
    [Tooltip("체크 시 1명만 밟아도 4명 모두의 부활 위치가 여기로 찍힙니다. (챕터4 로프맵 전용)\n해제 시 밟은 사람 본인만 부활 위치가 바뀝니다.")]
    public bool syncToAllPlayers = false;

    [ServerCallback]
    private void OnTriggerEnter2D(Collider2D other)
    {
        if (other.CompareTag("Player"))
        {
            PlayerRespawn respawnScript = other.GetComponent<PlayerRespawn>();
            if (respawnScript == null) return;

            if (syncToAllPlayers)
            {
                Debug.Log($"[체크포인트] '{other.name}' 진입! 팀 전체 스폰 동기화.");

                List<PlayerRespawn> allRespawns = CoopPlayerManager.GetPlayerComponents<PlayerRespawn>();
                foreach (var respawn in allRespawns)
                {
                    if (respawn != null) respawn.RpcUpdateSpawnPoint(spawnLocation.position);
                }
            }
            else
            {
                Debug.Log($"[체크포인트] '{other.name}' 개인 스폰 저장.");
                respawnScript.TargetUpdateSpawnPoint(respawnScript.connectionToClient, spawnLocation.position);
            }
        }
    }
}