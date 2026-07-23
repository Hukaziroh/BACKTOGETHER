using System.Collections.Generic;
using UnityEngine;

/// <summary>
/// 모든 플레이어의 스폰, 퇴장, 조회를 중앙에서 통합 관리하는 핵심 매니저입니다.
/// 매번 FindObjectsByType을 호출하던 무거운 연산을 완전히 대체합니다.
/// </summary>
public static class CoopPlayerManager
{
    private static readonly HashSet<GameObject> activePlayers = new HashSet<GameObject>();

    /// <summary>
    /// 플레이어 오브젝트 스폰 시 등록 (서버/클라이언트)
    /// </summary>
    public static void RegisterPlayer(GameObject player)
    {
        if (player != null && !activePlayers.Contains(player))
        {
            activePlayers.Add(player);
        }
    }

    /// <summary>
    /// 플레이어 퇴장/파괴 시 해제
    /// </summary>
    public static void UnregisterPlayer(GameObject player)
    {
        if (player != null)
        {
            activePlayers.Remove(player);
        }
    }

    /// <summary>
    /// 현재 게임에 존재하는 모든 플레이어 GameObject 목록 반환
    /// </summary>
    public static List<GameObject> GetActivePlayers()
    {
        activePlayers.RemoveWhere(p => p == null || !p.activeInHierarchy);
        return new List<GameObject>(activePlayers);
    }

    /// <summary>
    /// 특정 컴포넌트(예: PlayerRespawn, PlayerCombineHandler 등)를 가진 모든 플레이어 목록 반환
    /// </summary>
    public static List<T> GetPlayerComponents<T>() where T : Component
    {
        activePlayers.RemoveWhere(p => p == null || !p.activeInHierarchy);
        List<T> list = new List<T>();
        foreach (var player in activePlayers)
        {
            if (player != null && player.TryGetComponent<T>(out var component))
            {
                list.Add(component);
            }
        }
        return list;
    }

    /// <summary>
    /// 서버 종료 또는 씬 이동 시 전체 목록 초기화
    /// </summary>
    public static void Clear()
    {
        activePlayers.Clear();
    }
}