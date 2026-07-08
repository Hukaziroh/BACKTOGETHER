using UnityEngine;

public class SpectatorSystem : MonoBehaviour
{
    // 외부 공개용 Property
    // 💡 여기서 null을 반환하도록 하여 카메라 시스템이 '관전 없음'을 즉시 감지하게 함
    public Transform CurrentTarget { get; private set; }

    private int currentSpectateIndex = -1;

    // 관전 대상 변경 로직
    public void SelectTarget(int index)
    {
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

        for (int i = 0; i < 4; i++)
        {
            currentSpectateIndex = (currentSpectateIndex + 1) % 4;

            if (CoopPlayerIdentity.players.TryGetValue(currentSpectateIndex, out var player) && player != null)
            {
                CurrentTarget = player.transform;
                Debug.Log($"[관전] {currentSpectateIndex + 1}P로 변경");
                return;
            }
        }
    }

    // 💡 관전 종료 함수
    public void StopSpectating()
    {
        CurrentTarget = null; // 이것이 null이 되어야 SpectatorCamera가 내 캐릭터로 돌아갑니다.
        currentSpectateIndex = -1;
        Debug.Log("관전 모드 종료 - 카메라가 원래 위치로 복귀합니다.");
    }
}