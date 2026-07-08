using UnityEngine;

public class SpectatorSystem : MonoBehaviour
{
    // 관전 타겟을 저장하는 내부 변수
    private Transform _currentTarget;

    // 외부 공개용 Property (안전장치 포함)
    public Transform CurrentTarget
    {
        get
        {
            // 만약 타겟이 파괴되었다면(Unity에서 null이면) 관전을 자동으로 종료
            if (_currentTarget == null) return null;
            return _currentTarget;
        }
        private set => _currentTarget = value;
    }

    private int currentSpectateIndex = -1;

    // 관전 대상 변경 로직
    public void SelectTarget(int index)
    {
        // CoopPlayerIdentity.players 딕셔너리 안전 접근
        if (CoopPlayerIdentity.players != null &&
            CoopPlayerIdentity.players.TryGetValue(index, out CoopPlayerIdentity playerIdentity) &&
            playerIdentity != null)
        {
            CurrentTarget = playerIdentity.transform;
            currentSpectateIndex = index;
            Debug.Log($"[관전] {index + 1}P를 관전합니다.");
        }
        else
        {
            Debug.LogWarning($"[관전] {index + 1}P를 찾을 수 없거나 이미 나갔습니다.");
        }
    }

    // 순차 변경 로직
    public void CycleTarget()
    {
        if (CoopPlayerIdentity.players == null || CoopPlayerIdentity.players.Count == 0) return;

        // 최대 4번(인덱스 범위) 루프를 돌며 유효한 플레이어 찾기
        for (int i = 0; i < 4; i++)
        {
            currentSpectateIndex = (currentSpectateIndex + 1) % 4;

            // 키가 존재하고, 해당 플레이어 오브젝트가 파괴되지 않았는지 확인
            if (CoopPlayerIdentity.players.TryGetValue(currentSpectateIndex, out var player) && player != null)
            {
                CurrentTarget = player.transform;
                Debug.Log($"[관전] {currentSpectateIndex + 1}P로 변경");
                return;
            }
        }
    }

    public void StopSpectating()
    {
        CurrentTarget = null;
        currentSpectateIndex = -1;
        Debug.Log("관전 모드 종료");
    }
}