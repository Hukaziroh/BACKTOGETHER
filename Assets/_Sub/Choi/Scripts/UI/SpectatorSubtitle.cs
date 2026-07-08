using UnityEngine;
using UnityEngine.UI; // Text 사용 시
using TMPro;          // TextMeshPro 사용 시 (TMPro를 쓴다면 이 줄 주석 해제)

public class SpectatorSubtitle : MonoBehaviour
{
    [SerializeField] private GameObject uiContainer; // 자막을 감싸고 있는 UI 패널이나 오브젝트
    [SerializeField] private TextMeshProUGUI subtitleText;      // 일반 Text일 경우
                                                     // [SerializeField] private TextMeshProUGUI subtitleText; // TMPro 사용 시

    private SpectatorSystem spectatorSystem;

    private void Update()
    {
        if (spectatorSystem == null)
            spectatorSystem = Object.FindAnyObjectByType<SpectatorSystem>();

        if (spectatorSystem == null) return;

        Transform currentTarget = spectatorSystem.CurrentTarget;

        // 1. 관전 대상이 없거나(본인 플레이 중), 본인을 관전 중인 경우
        if (currentTarget == null || IsMe(currentTarget))
        {
            if (uiContainer.activeSelf) uiContainer.SetActive(false);
        }
        // 2. 다른 사람을 관전 중인 경우
        else
        {
            if (!uiContainer.activeSelf) uiContainer.SetActive(true);

            CoopPlayerIdentity identity = currentTarget.GetComponent<CoopPlayerIdentity>();
            if (identity != null)
            {
                subtitleText.text = $"[관전 중] {identity.playerIndex + 1}P 플레이어를 보는 중...";
            }
        }
    }

    // 대상이 나인지 확인하는 함수
    private bool IsMe(Transform target)
    {
        CoopPlayerIdentity identity = target.GetComponent<CoopPlayerIdentity>();
        return identity != null && identity.isLocalPlayer;
    }
}