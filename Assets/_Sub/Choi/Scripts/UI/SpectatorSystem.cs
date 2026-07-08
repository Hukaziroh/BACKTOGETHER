using UnityEngine;

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

    // 순차 변경 로직 (본인 건너뛰기)
    public void CycleTarget()
    {
        if (CoopPlayerIdentity.players == null || CoopPlayerIdentity.players.Count <= 1)
        {
            Debug.Log("[관전] 관전할 다른 플레이어가 없습니다.");
            StopSpectating();
            return;
        }

        // 최대 4번(인덱스 개수) 순회
        for (int i = 0; i < 4; i++)
        {
            // 다음 인덱스로 이동
            currentSpectateIndex = (currentSpectateIndex + 1) % 4;

            // 해당 인덱스에 플레이어가 있는지 확인
            if (CoopPlayerIdentity.players.TryGetValue(currentSpectateIndex, out var player) && player != null)
            {
                // 본인이 아닐 때만 타겟 설정
                if (!player.isLocalPlayer)
                {
                    CurrentTarget = player.transform;
                    Debug.Log($"[관전] {currentSpectateIndex + 1}P 관전 중");
                    return;
                }
            }
        }

        // 루프를 다 돌았는데도 타겟을 못 찾았다면 (예: 나 혼자뿐인 경우)
        StopSpectating();
    }

    public void StopSpectating()
    {
        CurrentTarget = null;
        currentSpectateIndex = -1;
        Debug.Log("관전 모드 종료");
    }
}