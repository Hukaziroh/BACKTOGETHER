using UnityEngine;
using System.Collections.Generic;
using Mirror;

public class SafeBoxTrigger : MonoBehaviour
{
    // Authoritative Server: 서버에서만 안전 구역 내 플레이어 목록을 관리합니다.
    public readonly HashSet<GameObject> playersInBox = new HashSet<GameObject>();

    private void OnTriggerEnter2D(Collider2D other)
    {
        // 클라이언트 판정 변조를 방지하기 위해 서버에서만 리스트 등록
        if (!NetworkServer.active) return;

        if (other.CompareTag("Player"))
        {
            playersInBox.Add(other.gameObject);
        }
    }

    private void OnTriggerExit2D(Collider2D other)
    {
        if (!NetworkServer.active) return;

        if (other.CompareTag("Player"))
        {
            playersInBox.Remove(other.gameObject);
        }
    }

    private void Update()
    {
        if (!NetworkServer.active) return;

        // 게임 도중 나가거나 파괴된 플레이어가 있다면 리스트에서 깔끔하게 제거
        playersInBox.RemoveWhere(p => p == null || !p.activeInHierarchy);
    }
}