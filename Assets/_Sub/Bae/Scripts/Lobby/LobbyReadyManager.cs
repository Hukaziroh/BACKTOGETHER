using UnityEngine;
using UnityEngine.UI;
using TMPro;
using Mirror;

public class LobbyReadyManager : MonoBehaviour
{
    [Header("UI 컴포넌트 연결")]
    public Button startGameButton;
    public Button readyButton;
    public TextMeshProUGUI readySummaryText;

    void Start()
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
            if (p.isReady)
            {
                readyCount++;
            }
        }

        if (readySummaryText != null)
        {
            readySummaryText.text = $"Ready: {readyCount} / {totalPlayers}";
        }
        UpdateStartButtonState(players);
    }

    public void UpdateStartButtonState(CoopPlayerIdentity[] players)
    {
        bool allReady = true;
        int totalPlayers = players.Length;

        foreach (var p in players)
        {
            if (!p.isReady) 
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