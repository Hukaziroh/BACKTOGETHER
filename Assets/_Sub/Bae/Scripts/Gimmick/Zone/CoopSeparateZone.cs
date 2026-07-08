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

                Vector3 basePos = body.transform.position;

                PlayerCombineHandler[] allPlayers = FindObjectsByType<PlayerCombineHandler>(FindObjectsSortMode.None);

                int spreadIndex = 0;
                float[] spreadOffsets = { 0f, -1.2f, 1.2f, -2.4f, 2.4f };

                foreach (var p in allPlayers)
                {
                    if (p.isCombined && p.bodyTarget == body)
                    {
                        float offsetX = spreadOffsets[spreadIndex % spreadOffsets.Length];
                        Vector3 safeReleasePos = basePos + new Vector3(offsetX, 0.5f, 0f);

                        p.StopCombineMode(safeReleasePos);
                        spreadIndex++;
                    }
                }

                Debug.Log("합체 해제 구역 통과 -> 연결된 모든 파티원 강제 분리 및 안전 소환 완료!");
            }
        }
    }
}