using System.Collections.Generic;
using UnityEngine;
using Mirror;

public class CoopSeparateZone : NetworkBehaviour
{
    [Header("해제 조건 설정")]
    [Tooltip("해제되기 위해 해제 구역에 모여야 하는 팀(본체)의 수 (2인 1조 2팀일 경우 2)")]
    public int requiredTeams = 2;

    private HashSet<GameObject> playersInZone = new HashSet<GameObject>();

    [ServerCallback]
    private void OnTriggerEnter2D(Collider2D other)
    {
        if (other.CompareTag("Player") && other.gameObject == other.transform.root.gameObject)
        {
            playersInZone.RemoveWhere(p => p == null || !p.activeInHierarchy);
            playersInZone.Add(other.gameObject);

            CheckAndSeparate();
        }
    }

    [ServerCallback]
    private void OnTriggerExit2D(Collider2D other)
    {
        if (other.CompareTag("Player") && other.gameObject == other.transform.root.gameObject)
        {
            playersInZone.Remove(other.gameObject);
        }
    }

    [Server]
    private void CheckAndSeparate()
    {
        // 구역 안에 있는 플레이어들 중 합체 상태인 유효한 팀(bodyTarget) 수집
        HashSet<GameObject> uniqueBodies = new HashSet<GameObject>();

        foreach (var pObj in playersInZone)
        {
            if (pObj == null) continue;
            PlayerCombineHandler combine = pObj.GetComponent<PlayerCombineHandler>();

            if (combine != null && combine.isCombined && combine.bodyTarget != null)
            {
                uniqueBodies.Add(combine.bodyTarget);
            }
        }

        // 필요한 팀 수(기본 2팀) 이상이 구역에 들어왔을 때만 해제 발동
        if (uniqueBodies.Count >= requiredTeams)
        {
            List<PlayerCombineHandler> allPlayers = CoopPlayerManager.GetPlayerComponents<PlayerCombineHandler>();

            foreach (GameObject body in uniqueBodies)
            {
                if (body == null) continue;
                Vector3 basePos = body.transform.position;

                int spreadIndex = 0;
                float[] spreadOffsets = { 0f, -1.2f, 1.2f, -2.4f, 2.4f };

                foreach (var p in allPlayers)
                {
                    if (p != null && p.isCombined && p.bodyTarget == body)
                    {
                        float offsetX = spreadOffsets[spreadIndex % spreadOffsets.Length];
                        Vector3 safeReleasePos = basePos + new Vector3(offsetX, 0.5f, 0f);

                        p.StopCombineMode(safeReleasePos);

                        CoopPlayerIdentity identity = p.GetComponent<CoopPlayerIdentity>();
                        if (identity != null)
                        {
                            identity.ResetCombinedColors();
                        }

                        spreadIndex++;
                    }
                }
            }

            // 해제 완료 후 대기열 초기화
            playersInZone.Clear();
            Debug.Log($"합체 해제 구역 ({requiredTeams}팀 진입 완료) -> 모든 파티원 강제 분리 및 색상 복구 완료!");
        }
    }
}