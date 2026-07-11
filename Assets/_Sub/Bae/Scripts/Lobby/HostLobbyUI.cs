using System.Collections;
using UnityEngine;
using TMPro;

public class HostLobbyUI : MonoBehaviour
{
    public TextMeshProUGUI roomCodeText;

    void Start()
    {
        StartCoroutine(ShowShortCodeRoutine());
    }

    private IEnumerator ShowShortCodeRoutine()
    {
        while (string.IsNullOrEmpty(PrivateLobbyManager.currentShortCode))
        {
            yield return null;
        }

        if (roomCodeText != null)
        {
            roomCodeText.text = "Room Code: " + PrivateLobbyManager.currentShortCode;
        }
    }

    public void OnCopyButtonClicked()
    {
        string currentCode = PrivateLobbyManager.currentShortCode;

        if (!string.IsNullOrEmpty(currentCode))
        {
            GUIUtility.systemCopyBuffer = currentCode;
            Debug.Log("방 코드가 복사되었습니다: " + currentCode);
        }
    }
}