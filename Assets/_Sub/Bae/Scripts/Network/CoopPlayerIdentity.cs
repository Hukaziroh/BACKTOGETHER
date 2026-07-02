using UnityEngine;
using Mirror;

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

    [SyncVar(hook = nameof(OnReadyStatusChanged))]
    public bool isReady = false;

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
            UpdatePlayerVisual(playerIndex);
        }
        NotifyReadyManager();
    }

    public override void OnStartLocalPlayer()
    {
        base.OnStartLocalPlayer();

        if (isServer)
        {
            CmdSetReady(true);
        }
    }

    [Server]
    private void AssignAvailableIndex()
    {
        CoopPlayerIdentity[] allPlayers = FindObjectsByType<CoopPlayerIdentity>(FindObjectsSortMode.None);
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

    [Command]
    public void CmdToggleReady()
    {
        isReady = !isReady;
    }
    [Command]
    public void CmdSetReady(bool state)
    {
        isReady = state;
    }

    void OnPlayerIndexChanged(int oldIndex, int newIndex)
    {
        UpdatePlayerVisual(newIndex);
        NotifyReadyManager();
    }

    void OnReadyStatusChanged(bool oldState, bool newState)
    {
        NotifyReadyManager();
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

    private void NotifyReadyManager()
    {
        LobbyReadyManager readyManager = FindFirstObjectByType<LobbyReadyManager>();
        if (readyManager != null) readyManager.UpdateLobbyUI();
    }
}