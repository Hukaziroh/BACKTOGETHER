using System.Collections.Generic;
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

    private int lastPlayerCount = -1;
    private int lastReadyCount = -1;

    void Start()
    {
        UpdateLobbyUI();
    }

    void Update()
    {
        CoopPlayerIdentity[] players = FindObjectsByType<CoopPlayerIdentity>(FindObjectsSortMode.None);

        int currentReadyCount = 0;
        int validCount = 0;

        foreach (var p in players)
        {
            if (p != null && p.gameObject.activeInHierarchy)
            {
                validCount++;
                if (p.isReady) currentReadyCount++;
            }
        }

        if (validCount != lastPlayerCount || currentReadyCount != lastReadyCount)
        {
            lastPlayerCount = validCount;
            lastReadyCount = currentReadyCount;
            UpdateLobbyUI();
        }
    }

    public void UpdateLobbyUI()
    {
        CoopPlayerIdentity[] allPlayers = FindObjectsByType<CoopPlayerIdentity>(FindObjectsSortMode.None);
        List<CoopPlayerIdentity> validPlayers = new List<CoopPlayerIdentity>();

        int readyCount = 0;

        foreach (var p in allPlayers)
        {
            if (p != null && p.gameObject.activeInHierarchy)
            {
                validPlayers.Add(p);
                if (p.isReady)
                {
                    readyCount++;
                }
            }
        }

        if (readySummaryText != null)
        {
            readySummaryText.text = $"Ready: {readyCount} / {validPlayers.Count}";
        }

        UpdateStartButtonState(validPlayers);
    }

    public void UpdateStartButtonState(List<CoopPlayerIdentity> players)
    {
        bool allReady = true;
        int totalPlayers = players.Count;

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

            // 혼자일 때 시작을 막으려면 아래 주석을 해제하세요.
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