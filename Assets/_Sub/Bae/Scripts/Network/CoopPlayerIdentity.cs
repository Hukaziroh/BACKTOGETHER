using UnityEngine;
using Mirror;

public class CoopPlayerIdentity : NetworkBehaviour
{
    [Header("비주얼 설정")]
    public SpriteRenderer playerSpriteRenderer;

    [Tooltip("1P, 2P, 3P, 4P 순서대로 사용할 스프라이트 4개를 넣어주세요.")]
    public Sprite[] playerSprites = new Sprite[4];

    [SyncVar(hook = nameof(OnPlayerIndexChanged))]
    public int playerIndex = -1; 

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
                Debug.Log($"[플레이어 생성] {i + 1}P 번호가 부여되었습니다. (ID: {netId})");
                break;
            }
        }
    }

    void OnPlayerIndexChanged(int oldIndex, int newIndex)
    {
        UpdatePlayerVisual(newIndex);
    }

    private void UpdatePlayerVisual(int index)
    {
        if (playerSpriteRenderer == null) return;

        if (index >= 0 && index < playerSprites.Length)
        {
            if (playerSprites[index] != null)
            {
                playerSpriteRenderer.sprite = playerSprites[index];

                // 팁: 만약 캐릭터 이미지 파일이 하나고 '색상만' 바꾸고 싶다면 아래 코드
                // Color[] pColors = { Color.red, Color.blue, Color.yellow, Color.green };
                // playerSpriteRenderer.color = pColors[index];
            }
            else
            {
                Debug.LogWarning($"{index + 1}P에 지정된 스프라이트(이미지)가 없습니다!");
            }
        }
    }
}