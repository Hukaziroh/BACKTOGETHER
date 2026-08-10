using UnityEngine;
using Mirror;
using System.Collections.Generic;

public class EyeClearZone : NetworkBehaviour
{
    [Header("탈출 구역 설정")]
    [Tooltip("기믹을 클리어하기 위해 구역 내에 필요한 최소 플레이어 수 (4명)")]
    public int requiredPlayers = 4;

    [Header("연결할 눈 기믹")]
    [Tooltip("이 구역에 다 모였을 때 꺼지게 만들 눈(Eye) 오브젝트를 넣어주세요.")]
    public EyeGimmick targetEyeGimmick;

    private readonly HashSet<NetworkIdentity> playersInZone = new HashSet<NetworkIdentity>();
    private bool isCleared = false;

    [ServerCallback]
    private void OnTriggerEnter2D(Collider2D collision)
    {
        if (isCleared) return;
        if (collision.CompareTag("Player"))
        {
            NetworkIdentity networkIdentity = collision.GetComponentInParent<NetworkIdentity>();
            if (networkIdentity != null)
            {
                playersInZone.Add(networkIdentity);
                CheckZonePlayers();
            }
        }
    }

    [ServerCallback]
    private void OnTriggerExit2D(Collider2D collision)
    {
        if (isCleared) return;

        if (collision.CompareTag("Player"))
        {
            NetworkIdentity networkIdentity = collision.GetComponentInParent<NetworkIdentity>();
            if (networkIdentity != null)
            {
                playersInZone.Remove(networkIdentity);
            }
        }
    }

    [Server]
    private void CheckZonePlayers()
    {
        playersInZone.RemoveWhere(p => p == null || !p.gameObject.activeInHierarchy);

        if (playersInZone.Count >= requiredPlayers && !isCleared)
        {
            isCleared = true;
            if (targetEyeGimmick != null)
            {
                targetEyeGimmick.ClearGimmick();
            }
        }
    }
}