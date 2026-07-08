using UnityEngine;
using Mirror;

public class CoopSeparateZone : NetworkBehaviour
{
    [ServerCallback]
    private void OnTriggerEnter2D(Collider2D other)
    {
        if (other.CompareTag("Player"))
        {
            PlayerCombineHandler hitCombine = other.GetComponent<PlayerCombineHandler>();
            if (hitCombine != null && hitCombine.isCombined)
            {
                GameObject body = hitCombine.bodyTarget;
                if (body == null) return;

                Vector3 releasePos = body.transform.position;
                PlayerCombineHandler[] allPlayers = FindObjectsByType<PlayerCombineHandler>(FindObjectsSortMode.None);
                foreach (var p in allPlayers)
                {
                    if (p.isCombined && p.bodyTarget == body)
                    {
                        p.StopCombineMode(releasePos);
                    }
                }

                Debug.Log("합체 해제 구역 통과 -> 연결된 모든 파티원 강제 분리 및 소환 완료!");
            }
        }
    }
}