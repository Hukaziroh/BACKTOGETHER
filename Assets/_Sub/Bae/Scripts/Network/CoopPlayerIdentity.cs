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
    private static readonly Dictionary<int, int> serverConnectionIndexMap = new Dictionary<int, int>();

    public override void OnStartServer()
    {
        base.OnStartServer();
        CoopPlayerManager.RegisterPlayer(gameObject); 
        AssignAvailableIndex();
    }

    public override void OnStartClient()
    {
        base.OnStartClient();
        CoopPlayerManager.RegisterPlayer(gameObject); 

        if (playerIndex != -1)
        {
            players[playerIndex] = this;
            UpdatePlayerVisual(playerIndex);
        }
    }

    public override void OnStopClient()
    {
        base.OnStopClient();
        CoopPlayerManager.UnregisterPlayer(gameObject); 
        if (players.ContainsKey(playerIndex))
        {
            players.Remove(playerIndex);
        }
    }

    public override void OnStopServer()
    {
        base.OnStopServer();
        CoopPlayerManager.UnregisterPlayer(gameObject); 
        if (!NetworkServer.active)
        {
            serverConnectionIndexMap.Clear();
            CoopPlayerManager.Clear();
        }
    }

    [Server]
    private void AssignAvailableIndex()
    {
        int connId = connectionToClient != null ? connectionToClient.connectionId : -1;

        if (connId != -1 && serverConnectionIndexMap.TryGetValue(connId, out int existingIndex))
        {
            playerIndex = existingIndex;
            if (isClient) UpdatePlayerVisual(playerIndex);
            Debug.Log($"[플레이어 유지] 접속 ID({connId}) - 기존 {playerIndex + 1}P 번호 및 색상을 유지합니다.");
            return;
        }

        bool[] isIndexTaken = new bool[4];
        foreach (int takenIndex in serverConnectionIndexMap.Values)
        {
            if (takenIndex >= 0 && takenIndex < 4)
            {
                isIndexTaken[takenIndex] = true;
            }
        }

        for (int i = 0; i < 4; i++)
        {
            if (!isIndexTaken[i])
            {
                playerIndex = i;
                if (connId != -1)
                {
                    serverConnectionIndexMap[connId] = i;
                }
                if (isClient) UpdatePlayerVisual(playerIndex);

                Debug.Log($"[플레이어 최초 생성] 접속 ID({connId}) - {i + 1}P 번호가 새로 부여되었습니다.");
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
    }
}