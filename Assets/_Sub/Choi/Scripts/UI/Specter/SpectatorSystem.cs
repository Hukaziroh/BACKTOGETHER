using UnityEngine;
using System.Collections.Generic;

public class SpectatorSystem : MonoBehaviour
{
    private Transform _currentTarget;
    public Transform CurrentTarget
    {
        get { return (_currentTarget == null) ? null : _currentTarget; }
        private set => _currentTarget = value;
    }

    private int currentSpectateIndex = -1;

    // 관전 대상 선택 (특정 번호)
    public void SelectTarget(int index)
    {
        // 대상이 존재하고, 내가 아닌 경우에만 설정
        if (CoopPlayerIdentity.players.TryGetValue(index, out CoopPlayerIdentity playerIdentity) && playerIdentity != null)
        {
            if (playerIdentity.isLocalPlayer)
            {
                Debug.Log("[관전] 본인은 관전할 수 없습니다.");
                return;
            }

            CurrentTarget = playerIdentity.transform;
            currentSpectateIndex = index;
            Debug.Log($"[관전] {index + 1}P를 관전합니다.");
        }
    }

    // ★ Tab 키를 눌렀을 때 다음 플레이어로 순환 관전하는 메서드
    public void CycleNextTarget()
    {
        if (CoopPlayerIdentity.players == null || CoopPlayerIdentity.players.Count == 0)
        {
            Debug.Log("[관전] 관전할 수 있는 플레이어가 없습니다.");
            return;
        }

        // 본인을 제외한 유효한 플레이어 인덱스 목록 수집
        List<int> validIndices = new List<int>();
        foreach (var kvp in CoopPlayerIdentity.players)
        {
            if (kvp.Value != null && !kvp.Value.isLocalPlayer)
            {
                validIndices.Add(kvp.Key);
            }
        }

        if (validIndices.Count == 0)
        {
            Debug.Log("[관전] 관전 가능한 다른 플레이어가 없습니다.");
            StopSpectating();
            return;
        }

        // 현재 관전 중인 인덱스의 위치를 찾고 다음 인덱스로 순환 (마지막이면 처음으로 돌아감)
        int currentIndexInList = validIndices.IndexOf(currentSpectateIndex);
        int nextIndexInList = (currentIndexInList + 1) % validIndices.Count;

        SelectTarget(validIndices[nextIndexInList]);
    }

    public void StopSpectating()
    {
        CurrentTarget = null;
        currentSpectateIndex = -1;
        Debug.Log("관전 모드 종료");
    }
}