using System.Collections.Generic;
using UnityEngine;
using Mirror;

public class CoopCheckpoint : NetworkBehaviour
{
    [Header("스폰될 위치 (빈 오브젝트)")]
    public Transform spawnLocation;

    [Header("체크포인트 순서")]
    [Tooltip("체크포인트의 진행 순서입니다. 앞 체크포인트보다 반드시 큰 숫자를 넣어주세요.")]
    public int checkpointIndex = 0;

    [Header("협동 모드 설정")]
    [Tooltip("체크 시 1명만 밟아도 4명 모두의 부활 위치가 여기로 찍힙니다. (챕터4 로프맵 전용)\n해제 시 밟은 사람 본인만 부활 위치가 바뀝니다.")]
    public bool syncToAllPlayers = false;

    [ServerCallback]
    private void OnTriggerEnter2D(Collider2D other)
    {
        if (!other.CompareTag("Player"))
            return;

        PlayerRespawn respawnScript = other.GetComponent<PlayerRespawn>();

        if (respawnScript == null)
            return;

        // =====================================================
        // 챕터4 : 한 명이 체크포인트를 찍으면 전원 갱신
        // =====================================================
        if (syncToAllPlayers)
        {
            Debug.Log(
                $"[체크포인트] '{other.name}' 진입! " +
                $"체크포인트 Index = {checkpointIndex} | " +
                $"팀 전체 체크포인트 갱신 시도"
            );

            List<PlayerRespawn> allRespawns = CoopPlayerManager.GetPlayerComponents<PlayerRespawn>();

            // 스폰 위치 분산 (1.0f 간격)
            float spacing = 1.0f;
            // 4명이 체크포인트 중앙을 기준으로 좌우로 정렬되도록 시작 오프셋 계산
            float startOffsetX = -((allRespawns.Count - 1) * spacing) / 2f;

            for (int i = 0; i < allRespawns.Count; i++)
            {
                if (allRespawns[i] != null)
                {
                    // X축으로 간격을 두고 좌표를 찍음
                    Vector3 offset = new Vector3(startOffsetX + (i * spacing), 0f, 0f);

                    allRespawns[i].RpcUpdateSpawnPointIfNewer(
                        spawnLocation.position + offset,
                        checkpointIndex
                    );
                }
            }
        }

        // =====================================================
        // 일반 맵 : 체크포인트를 밟은 플레이어만 갱신
        // =====================================================
        else
        {
            Debug.Log(
                $"[체크포인트] '{other.name}' 진입! " +
                $"체크포인트 Index = {checkpointIndex} | " +
                $"개인 체크포인트 갱신 시도"
            );

            respawnScript.TargetUpdateSpawnPointIfNewer(
                respawnScript.connectionToClient,
                spawnLocation.position,
                checkpointIndex
            );
        }
    }
}