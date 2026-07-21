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

    public void StopSpectating()
    {
        CurrentTarget = null;
        currentSpectateIndex = -1;
        Debug.Log("관전 모드 종료");
    }
}