using System.Collections.Generic;
using Mirror;
using UnityEngine;

// 🌟 1. 표정 상태 Enum 정의
public enum FaceState
{
    Idle,   // 평소 가만히 있을 때
    Walk,   // 걸을 때
    Jump,   // 점프할 때
    Hit     // 가시 피격 / 기절했을 때
}

// 🌟 2. 각 플레이어별 표정 스프라이트 그룹 (인스펙터에서 펼쳐서 직접 끼워넣는 구조체)
[System.Serializable]
public struct PlayerFaceGroup
{
    public string playerLabel; // 인스펙터 구분용 (예: "1P (Red)")
    public Sprite idleSprite;  // Idle 표정 스프라이트
    public Sprite walkSprite;  // Walk 표정 스프라이트
    public Sprite jumpSprite;  // Jump 표정 스프라이트
    public Sprite hitSprite;   // Hit 표정 스프라이트
}

public class CoopPlayerIdentity : NetworkBehaviour
{
    [Header("비주얼 설정 (몸통)")]
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

    // ==========================================
    // 🌟 표정(눈) 설정 (Enum & 인스펙터 슬롯)
    // ==========================================
    [Header("표정(눈) 설정")]
    public SpriteRenderer faceSpriteRenderer;

    [Tooltip("P0(1P) ~ P3(4P) 플레이어별 표정 세트 (인스펙터에서 펼쳐서 스프라이트를 끼워넣으세요)")]
    public PlayerFaceGroup[] playerFaceGroups = new PlayerFaceGroup[4];

    private PlayerController controller;
    private Animator anim;

    public static Dictionary<int, CoopPlayerIdentity> players = new Dictionary<int, CoopPlayerIdentity>();
    private static readonly Dictionary<int, int> serverConnectionIndexMap = new Dictionary<int, int>();

    private MaterialPropertyBlock propBlock;

    void Awake()
    {
        propBlock = new MaterialPropertyBlock();

        controller = GetComponent<PlayerController>();
        anim = GetComponent<Animator>();

        // 자식 오브젝트 중 FaceSprite 찾기
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

    public override void OnStopClient()
    {
        base.OnStopClient();
        CoopPlayerManager.UnregisterPlayer(gameObject);
        if (players.ContainsKey(playerIndex))
        {
            players.Remove(playerIndex);
        }
    }

    void Update()
    {
        if (faceSpriteRenderer == null || playerIndex < 0 || playerIndex >= playerFaceGroups.Length) return;

        // 1. 현재 상태 Enum 판별
        FaceState currentState = FaceState.Idle;

        if (controller != null && controller.knockback != null && controller.knockback.IsStunned)
        {
            currentState = FaceState.Hit;
        }
        else if (anim != null)
        {
            bool isGrounded = anim.GetBool("isGrounded");
            float speed = anim.GetFloat("Speed");

            if (!isGrounded)
            {
                currentState = FaceState.Jump;
            }
            else if (speed > 0.1f)
            {
                currentState = FaceState.Walk;
            }
        }

        // 2. 현재 플레이어 번호(P0~P3)에 해당하는 세트에서 Enum 상태에 맞는 Sprite 적용
        PlayerFaceGroup currentGroup = playerFaceGroups[playerIndex];
        Sprite targetSprite = GetFaceSprite(currentGroup, currentState);

        if (targetSprite != null)
        {
            faceSpriteRenderer.sprite = targetSprite;
        }
    }

    // Enum 상태에 따른 스프라이트 추출 함수
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