// CoopPlayerIdentity.cs
using System.Collections.Generic;
using Mirror;
using UnityEngine;

public enum FaceState
{
    Idle, Walk, Jump, Hit
}

[System.Serializable]
public struct PlayerFaceGroup
{
    public string playerLabel;
    public Sprite idleSprite;
    public Sprite walkSprite;
    public Sprite jumpSprite;
    public Sprite hitSprite;
}

public class CoopPlayerIdentity : NetworkBehaviour
{
    [Header("비주얼 설정 (몸통)")]
    public SpriteRenderer playerSpriteRenderer;

    public Color[] playerColors = new Color[]
    {
        new Color(1f, 0.5f, 0.5f),
        new Color(0.5f, 0.5f, 1f), // Red
        new Color(1f, 0.5f, 1f), // pink
        Color.yellow
    };
    [Header("합체 몸통 색")]
    public Color[] combineColors = new Color[2];

    [SyncVar(hook = nameof(OnPlayerIndexChanged))]
    public int playerIndex = -1;

    [Header("표정(눈) 설정")]
    public SpriteRenderer faceSpriteRenderer;
    public PlayerFaceGroup[] playerFaceGroups = new PlayerFaceGroup[4];
    [Header("합체 눈")]
    public PlayerFaceGroup[] combineFaceGroups = new PlayerFaceGroup[2];

    private PlayerController controller;
    private Animator anim;
    private PlayerCombineHandler combineHandler; // 합체 상태 확인용

    public static Dictionary<int, CoopPlayerIdentity> players = new Dictionary<int, CoopPlayerIdentity>();
    private static readonly Dictionary<int, int> serverConnectionIndexMap = new Dictionary<int, int>();

    private MaterialPropertyBlock propBlock;

    void Awake()
    {
        propBlock = new MaterialPropertyBlock();
        controller = GetComponent<PlayerController>();
        anim = GetComponent<Animator>();
        combineHandler = GetComponent<PlayerCombineHandler>();

        if (faceSpriteRenderer == null)
        {
            Transform faceChild = transform.Find("FaceSprite");
            if (faceChild != null) faceSpriteRenderer = faceChild.GetComponent<SpriteRenderer>();
        }
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

    public override void OnStopServer()
    {
        base.OnStopServer();
        int connId = connectionToClient != null ? connectionToClient.connectionId : -1;
        if (connId != -1 && serverConnectionIndexMap.ContainsKey(connId))
        {
            serverConnectionIndexMap.Remove(connId);
            Debug.Log($"[서버] 커넥션 ID {connId} 퇴장. 인덱스 맵에서 삭제합니다.");
        }
        if (players.ContainsKey(playerIndex) && players[playerIndex] == this)
        {
            players.Remove(playerIndex);
        }
    }

    public override void OnStopClient()
    {
        base.OnStopClient();
        if (players.ContainsKey(playerIndex) && players[playerIndex] == this)
        {
            players.Remove(playerIndex);
        }
    }

    void Update()
    {
        if (faceSpriteRenderer == null || playerFaceGroups == null || playerFaceGroups.Length == 0) return;

        if (combineHandler != null && combineHandler.isCombined && combineHandler.bodyTarget != gameObject)
        {
            faceSpriteRenderer.enabled = false;
            return;
        }
        else
        {
            faceSpriteRenderer.enabled = true;
        }

        int validIndex = playerIndex;
        if (validIndex < 0 || validIndex >= playerFaceGroups.Length) validIndex = 0;

        FaceState currentState = FaceState.Idle;
        bool isStunned = (controller != null && controller.knockback != null && controller.knockback.IsStunned);
        bool isGrounded = true;
        float speed = 0f;

        if (anim != null)
        {
            isGrounded = anim.GetBool("isGrounded");
            speed = anim.GetFloat("Speed");
        }

        if (isStunned) currentState = FaceState.Hit;
        else if (!isGrounded) currentState = FaceState.Jump;
        else if (speed > 0.1f) currentState = FaceState.Walk;

        PlayerFaceGroup group;

        if (combineHandler != null &&
            combineHandler.isCombined &&
            combineHandler.combineFaceIndex >= 0)
        {
            group = combineFaceGroups[combineHandler.combineFaceIndex];
        }
        else
        {
            group = playerFaceGroups[validIndex];
        }

        Sprite targetSprite = GetFaceSprite(group, currentState);
        if (targetSprite != null)
        {
            faceSpriteRenderer.sprite = targetSprite;
        }
    }

    private Sprite GetFaceSprite(PlayerFaceGroup group, FaceState state)
    {
        switch (state)
        {
            case FaceState.Idle: return group.idleSprite;
            case FaceState.Walk: return group.walkSprite;
            case FaceState.Jump: return group.jumpSprite;
            case FaceState.Hit: return group.hitSprite;
            default: return group.idleSprite;
        }
    }

    [Server]
    public void ResetCombinedColors()
    {
        RpcResetColor();
    }

    [ClientRpc]
    private void RpcResetColor()
    {
        UpdatePlayerVisual();
    }

    public void ForceUpdateVisual()
    {
        UpdatePlayerVisual();
    }

    public void ResetFaceVisual()
    {
        if (faceSpriteRenderer != null)
        {
            faceSpriteRenderer.enabled = true;

            if (playerIndex >= 0 && playerIndex < playerFaceGroups.Length)
            {
                faceSpriteRenderer.sprite =
                    playerFaceGroups[playerIndex].idleSprite;
            }
        }

        UpdatePlayerVisual();
    }
    public void ResetPlayerColor()
    {
        if (playerSpriteRenderer == null)
            return;

        Color originalColor = (playerIndex >= 0 && playerIndex < playerColors.Length)
            ? playerColors[playerIndex]
            : Color.white;

        playerSpriteRenderer.GetPropertyBlock(propBlock);

        propBlock.SetFloat("_SplitMode", 0);
        propBlock.SetColor("_Color1", originalColor);

        playerSpriteRenderer.color = originalColor;
        playerSpriteRenderer.SetPropertyBlock(propBlock);
    }

    private void AssignAvailableIndex()
    {
        int connId = connectionToClient != null ? connectionToClient.connectionId : -1;

        if (connId != -1 && serverConnectionIndexMap.TryGetValue(connId, out int existingIndex))
        {
            playerIndex = existingIndex;
            if (isClient) UpdatePlayerVisual();
            return;
        }

        bool[] isIndexTaken = new bool[4];
        foreach (int takenIndex in serverConnectionIndexMap.Values)
        {
            if (takenIndex >= 0 && takenIndex < 4) isIndexTaken[takenIndex] = true;
        }

        for (int i = 0; i < 4; i++)
        {
            if (!isIndexTaken[i])
            {
                playerIndex = i;
                if (connId != -1) serverConnectionIndexMap[connId] = i;
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

    private void UpdatePlayerVisual()
    {
        if (playerSpriteRenderer == null) return;

        playerSpriteRenderer.GetPropertyBlock(propBlock);

        Color mainColor;

        bool isCombinedState = (combineHandler != null &&
                                combineHandler.isCombined &&
                                combineHandler.combineColorIndex >= 0 &&
                                combineHandler.bodyTarget != null);

        if (isCombinedState)
        {
            mainColor = combineColors[combineHandler.combineColorIndex];
            propBlock.SetFloat("_SplitMode", 1); 
        }
        else
        {
            mainColor = (playerIndex >= 0 && playerIndex < playerColors.Length)
                ? playerColors[playerIndex]
                : Color.white;
            propBlock.SetFloat("_SplitMode", 0); 
        }

        propBlock.SetColor("_Color1", mainColor);

        playerSpriteRenderer.color = mainColor;
        playerSpriteRenderer.SetPropertyBlock(propBlock);
    }


}