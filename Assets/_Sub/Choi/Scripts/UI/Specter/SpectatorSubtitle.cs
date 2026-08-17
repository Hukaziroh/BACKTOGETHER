using UnityEngine;
using UnityEngine.UI;
using TMPro;

public class SpectatorSubtitle : MonoBehaviour
{
    [SerializeField] private GameObject uiContainer;           // 자막을 감싸고 있는 UI 패널이나 오브젝트
    [SerializeField] private TextMeshProUGUI customText;       // 1. 내가 직접 사용할 텍스트 (다국어 처리나 커스텀 메시지용)
    [SerializeField] private TextMeshProUGUI playerInfoText;   // 2. 플레이어 번호 텍스트 (예: "2P")

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
                // 플레이어 번호만 표시 (예: "2P")
                playerInfoText.text = $"{identity.playerIndex + 1}P";

                // 플레이어 인덱스에 해당하는 색상 가져오기
                Color targetColor = Color.white;
                if (identity.playerIndex >= 0 && identity.playerIndex < identity.playerColors.Length)
                {
                    targetColor = identity.playerColors[identity.playerIndex];
                }

                // 두 텍스트 모두에 동일한 플레이어 색상 적용
                playerInfoText.color = targetColor;
                customText.color = targetColor;
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