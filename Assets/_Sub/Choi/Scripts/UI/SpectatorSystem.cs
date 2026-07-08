using UnityEngine;
using System.Collections.Generic;

public class SpectatorSystem : MonoBehaviour
{
    // 외부에서 현재 누구를 보고 있는지 알 수 있도록 Property로 공개
    public Transform CurrentTarget { get; private set; }
    private int currentSpectateIndex = -1;

    // 관전 대상 변경 로직 (UIManager에서 호출)
    public void SelectTarget(int index)
    {
        if (CoopPlayerIdentity.players.TryGetValue(index, out CoopPlayerIdentity playerIdentity))
        {
            CurrentTarget = playerIdentity.transform;
            Debug.Log($"[관전] {index + 1}P를 관전합니다.");
        }
        else
        {
            Debug.LogWarning($"[관전] {index + 1}P를 찾을 수 없습니다.");
        }
    }

    // 순차 변경 로직 (UIManager에서 호출)
    public void CycleTarget()
    {
        if (CoopPlayerIdentity.players.Count == 0) return;

        for (int i = 0; i < 4; i++)
        {
            currentSpectateIndex = (currentSpectateIndex + 1) % 4;
            if (CoopPlayerIdentity.players.ContainsKey(currentSpectateIndex))
            {
                CurrentTarget = CoopPlayerIdentity.players[currentSpectateIndex].transform;
                return;
            }
        }
    }
    public void StopSpectating()
    {
        CurrentTarget = null;
        Debug.Log("관전 모드 종료");
    }
}