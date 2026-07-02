using UnityEngine;
using UnityEngine.UI;
using TMPro;
using Mirror;

public class LobbyReadyManager : MonoBehaviour
{
    [Header("UI 컴포넌트 연결")]
    [Tooltip("방장 전용: '게임 시작(Play)' 버튼을 연결하세요.")]
    public Button startGameButton;

    [Tooltip("클라이언트 전용: '준비하기(Ready)' 버튼을 연결하세요.")]
    public Button readyButton;

    [Tooltip("'준비 완료: 2 / 4' 같은 텍스트를 띄울 TMP를 연결하세요.")]
    public TextMeshProUGUI readySummaryText;

    void Update()
    {
        UpdateLobbyUI();
    }

    public void UpdateLobbyUI()
    {
        CoopPlayerIdentity[] players = FindObjectsByType<CoopPlayerIdentity>(FindObjectsSortMode.None);

        int readyCount = 0;
        int totalPlayers = players.Length;

        foreach (var p in players)
        {
            if (p.isServer || p.isReady)
            {
                readyCount++;
            }
        }

        if (readySummaryText != null)
        {
            readySummaryText.text = $"Ready: {readyCount} / {totalPlayers}";
        }
        UpdateStartButtonState();
    }

    public void UpdateStartButtonState()
    {
        CoopPlayerIdentity[] players = FindObjectsByType<CoopPlayerIdentity>(FindObjectsSortMode.None);
        bool allReady = true;
        int totalPlayers = players.Length;

        foreach (var p in players)
        {
            if (!p.isServer && !p.isReady)
            {
                allReady = false;
                break;
            }
        }

        if (NetworkServer.active) 
        {
            if (startGameButton != null) startGameButton.gameObject.SetActive(true);
            if (readyButton != null) readyButton.gameObject.SetActive(false);

            // if (totalPlayers <= 1) allReady = false;

            if (startGameButton != null) startGameButton.interactable = allReady;
        }
        else 
        {
            if (startGameButton != null) startGameButton.gameObject.SetActive(false);
            if (readyButton != null) readyButton.gameObject.SetActive(true);

            if (NetworkClient.localPlayer != null)
            {
                CoopPlayerIdentity myIdentity = NetworkClient.localPlayer.GetComponent<CoopPlayerIdentity>();
                if (myIdentity != null && readyButton != null)
                {
                    TextMeshProUGUI btnText = readyButton.GetComponentInChildren<TextMeshProUGUI>();
                    if (btnText != null)
                    {
                        btnText.text = myIdentity.isReady ? "준비 취소" : "준비 완료";
                    }
                }
            }
        }
    }

    public void OnReadyButtonClicked()
    {
        if (NetworkClient.localPlayer != null)
        {
            CoopPlayerIdentity myIdentity = NetworkClient.localPlayer.GetComponent<CoopPlayerIdentity>();
            if (myIdentity != null)
            {
                myIdentity.CmdToggleReady();
            }
        }
    }
}