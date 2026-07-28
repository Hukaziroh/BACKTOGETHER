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
        new Color(1f, 0.5f, 0.5f), // Red
        new Color(0.5f, 0.5f, 1f), // Blue
        new Color(0.5f, 1f, 0.5f)  // Green
    };

    [SyncVar(hook = nameof(OnPlayerIndexChanged))]
    public int playerIndex = -1;

    [SyncVar(hook = nameof(OnCombineVisualChanged))] public int splitMode = 1;
    [SyncVar(hook = nameof(OnCombineVisualChanged))] public int color1 = -1;
    [SyncVar(hook = nameof(OnCombineVisualChanged))] public int color2 = -1;
    [SyncVar(hook = nameof(OnCombineVisualChanged))] public int color3 = -1;
    [SyncVar(hook = nameof(OnCombineVisualChanged))] public int color4 = -1;

    public static Dictionary<int, CoopPlayerIdentity> players = new Dictionary<int, CoopPlayerIdentity>();
    private static readonly Dictionary<int, int> serverConnectionIndexMap = new Dictionary<int, int>();

    private MaterialPropertyBlock propBlock; 

    void Awake()
    {
        propBlock = new MaterialPropertyBlock();
    }

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
            UpdatePlayerVisual();
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

    private void AssignAvailableIndex()
    {
        int connId = connectionToClient != null ? connectionToClient.connectionId : -1;

        if (connId != -1 && serverConnectionIndexMap.TryGetValue(connId, out int existingIndex))
        {
            playerIndex = existingIndex;
            color1 = existingIndex; 
            splitMode = 1;
            if (isClient) UpdatePlayerVisual();
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
                color1 = i;
                splitMode = 1;
                if (connId != -1)
                {
                    serverConnectionIndexMap[connId] = i;
                }
                if (isClient) UpdatePlayerVisual();
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
        UpdatePlayerVisual();
    }

    void OnCombineVisualChanged(int oldVal, int newVal)
    {
        UpdatePlayerVisual();
    }

    [Server]
    public void SetCombinedColors(int mode, int c1, int c2, int c3 = -1, int c4 = -1)
    {
        splitMode = mode;
        color1 = c1;
        color2 = c2;
        color3 = c3;
        color4 = c4;
    }

    [Server]
    public void ResetCombinedColors()
    {
        splitMode = 1;
        color1 = playerIndex;
        color2 = -1;
        color3 = -1;
        color4 = -1;
    }

    private void UpdatePlayerVisual()
    {
        if (playerSpriteRenderer == null) return;
        playerSpriteRenderer.GetPropertyBlock(propBlock);

        if (splitMode == 1)
        {
            Color mainColor = (color1 >= 0 && color1 < playerColors.Length) ? playerColors[color1] : Color.white;
            propBlock.SetFloat("_SplitMode", 1);
            propBlock.SetColor("_Color1", mainColor);
            playerSpriteRenderer.color = mainColor;
        }
        else if (splitMode == 2) 
        {
            Color c1 = (color1 >= 0 && color1 < playerColors.Length) ? playerColors[color1] : Color.white;
            Color c2 = (color2 >= 0 && color2 < playerColors.Length) ? playerColors[color2] : Color.white;

            propBlock.SetFloat("_SplitMode", 2);
            propBlock.SetColor("_Color1", c1);
            propBlock.SetColor("_Color2", c2);
            playerSpriteRenderer.color = Color.white; 
        }
        else if (splitMode == 4) 
        {
            Color c1 = (color1 >= 0 && color1 < playerColors.Length) ? playerColors[color1] : Color.white;
            Color c2 = (color2 >= 0 && color2 < playerColors.Length) ? playerColors[color2] : Color.white;
            Color c3 = (color3 >= 0 && color3 < playerColors.Length) ? playerColors[color3] : Color.white;
            Color c4 = (color4 >= 0 && color4 < playerColors.Length) ? playerColors[color4] : Color.white;

            propBlock.SetFloat("_SplitMode", 4);
            propBlock.SetColor("_Color1", c1);
            propBlock.SetColor("_Color2", c2);
            propBlock.SetColor("_Color3", c3);
            propBlock.SetColor("_Color4", c4);
            playerSpriteRenderer.color = Color.white;
        }

        playerSpriteRenderer.SetPropertyBlock(propBlock);
    }
}