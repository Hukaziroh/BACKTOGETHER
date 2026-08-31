using UnityEngine;
using UnityEngine.UI;
using TMPro;

public class SpectatorSubtitle : MonoBehaviour
{
    [SerializeField] private GameObject uiContainer;           // 자막을 감싸고 있는 UI 패널이나 오브젝트
    [SerializeField] private TextMeshProUGUI customText;       // 1. 내가 직접 사용할 텍스트 (다국어 처리나 커스텀 메시지용)
    [SerializeField] private TextMeshProUGUI playerInfoText;   // 2. 플레이어 번호 텍스트 (예: "2P")

    private SpectatorSystem spectatorSystem;
    private float findTimer = 0f;
    private Transform lastTarget = null;
    private CoopPlayerIdentity lastIdentity = null;

    private void LateUpdate()
    {
        MatchPlayerInfoOutline();
    }

    private void Update()
    {
        if (spectatorSystem == null)
        {
            findTimer += Time.deltaTime;
            if (findTimer > 1f)
            {
                spectatorSystem = Object.FindAnyObjectByType<SpectatorSystem>();
                findTimer = 0f;
            }
            if (spectatorSystem == null) return;
        }

        Transform currentTarget = spectatorSystem.CurrentTarget;

        if (currentTarget != lastTarget)
        {
            lastTarget = currentTarget;
            lastIdentity = currentTarget != null ? currentTarget.GetComponent<CoopPlayerIdentity>() : null;
            
            if (lastIdentity != null && !lastIdentity.isLocalPlayer)
            {
                playerInfoText.text = $"{lastIdentity.playerIndex + 1}P";
                
                Color targetColor = Color.white;
                if (lastIdentity.playerIndex >= 0 && lastIdentity.playerIndex < lastIdentity.playerColors.Length)
                {
                    targetColor = lastIdentity.playerColors[lastIdentity.playerIndex];
                }

                playerInfoText.color = targetColor;
                customText.color = targetColor;
            }
        }

        if (lastIdentity == null || lastIdentity.isLocalPlayer)
        {
            if (uiContainer.activeSelf) uiContainer.SetActive(false);
        }
        else
        {
            if (!uiContainer.activeSelf) uiContainer.SetActive(true);
        }
    }



    private void MatchPlayerInfoOutline()
    {
        if (customText == null || playerInfoText == null) return;

        if (playerInfoText.font != customText.font)
        {
            playerInfoText.font = customText.font;
        }

        Material sharedStyleMaterial = customText.fontSharedMaterial;
        if (sharedStyleMaterial != null && playerInfoText.fontSharedMaterial != sharedStyleMaterial)
        {
            playerInfoText.fontSharedMaterial = sharedStyleMaterial;
            playerInfoText.SetMaterialDirty();
            playerInfoText.SetVerticesDirty();
        }
    }
}
