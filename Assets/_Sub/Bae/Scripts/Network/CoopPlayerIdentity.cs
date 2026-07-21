using System.Collections.Generic;
using Mirror;
using UnityEngine;

public class CoopPlayerIdentity : NetworkBehaviour
{
    [Header("비주얼 설정")]
    public SpriteRenderer playerSpriteRenderer;

    public Color[] playerColors = new Color[]
    {
        Color.white,
        new Color(1f, 0.5f, 0.5f),
        new Color(0.5f, 0.5f, 1f),
        new Color(0.5f, 1f, 0.5f)
    };

    [SyncVar(hook = nameof(OnPlayerIndexChanged))]
    public int playerIndex = -1;

    public static Dictionary<int, CoopPlayerIdentity> players = new Dictionary<int, CoopPlayerIdentity>();

    public override void OnStartServer()
    {
        base.OnStartServer();
        AssignAvailableIndex();
    }

    public override void OnStartClient()
    {
        base.OnStartClient();

        if (playerIndex != -1)
        {
            players[playerIndex] = this;
            UpdatePlayerVisual(playerIndex);
        }
    }

    public override void OnStopClient()
    {
        base.OnStopClient();
        if (players.ContainsKey(playerIndex))
        {
            players.Remove(playerIndex);
        }
    }

    [Server]
    private void AssignAvailableIndex()
    {
        CoopPlayerIdentity[] allPlayers = FindObjectsByType<CoopPlayerIdentity>();
        bool[] isIndexTaken = new bool[4];

        foreach (var p in allPlayers)
        {
            if (p != this && p.playerIndex >= 0 && p.playerIndex < 4)
            {
                isIndexTaken[p.playerIndex] = true;
            }
        }

        for (int i = 0; i < 4; i++)
        {
            if (!isIndexTaken[i])
            {
                playerIndex = i;
                if (isClient) UpdatePlayerVisual(playerIndex);

                Debug.Log($"[플레이어 생성] {i + 1}P 번호가 부여되었습니다.");
                break;
            }
        }
    }

    void OnPlayerIndexChanged(int oldIndex, int newIndex)
    {
        if (players.ContainsKey(oldIndex) && players[oldIndex] == this)
        {
            players.Remove(oldIndex);
        }
        players[newIndex] = this;
        UpdatePlayerVisual(newIndex);
    }

    private void UpdatePlayerVisual(int index)
    {
        if (playerSpriteRenderer == null) return;

        if (index >= 0 && index < playerColors.Length)
        {
            playerSpriteRenderer.color = playerColors[index];
        }
        else
        {
            Debug.LogWarning($"{index + 1}P에 지정된 색상이 없습니다!");
        }
    }
}
