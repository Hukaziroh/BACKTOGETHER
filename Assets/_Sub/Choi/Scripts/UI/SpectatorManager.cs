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
        if (CoopPlayerIdentity.players.ContainsKey(index))
        {
            CurrentTarget = CoopPlayerIdentity.players[index].transform;
            currentSpectateIndex = index;
            Debug.Log($"{index + 1}P 관전 시작");
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
}