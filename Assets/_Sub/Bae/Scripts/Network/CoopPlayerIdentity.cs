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

    [SyncVar(hook = nameof(OnReadyStatusChanged))]
    public bool isReady = false;

    // 관전 시스템 및 플레이어 관리를 위한 static 딕셔너리
    public static Dictionary<int, CoopPlayerIdentity> players = new Dictionary<int, CoopPlayerIdentity>();

    // 서버 시작 시 실행
    public override void OnStartServer()
    {
        base.OnStartServer();
        AssignAvailableIndex();
    }

    // 클라이언트 시작 시 실행
    public override void OnStartClient()
    {
        base.OnStartClient();

        // 인덱스가 할당된 상태라면 딕셔너리에 등록
        if (playerIndex != -1)
        {
            players[playerIndex] = this;
            UpdatePlayerVisual(playerIndex);
        }

        NotifyReadyManager();
    }

    // 클라이언트 종료 시 실행
    public override void OnStopClient()
    {
        base.OnStopClient();
        // 접속 종료 시 딕셔너리에서 제거
        if (players.ContainsKey(playerIndex))
        {
            players.Remove(playerIndex);
        }
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
                // 서버에서도 시각적 업데이트를 즉시 반영할 수 있게 호출
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

    // SyncVar Hook: 인덱스 변경 시 호출
    void OnPlayerIndexChanged(int oldIndex, int newIndex)
    {
        // 1. 기존 인덱스가 딕셔너리에 있다면 제거
        if (players.ContainsKey(oldIndex) && players[oldIndex] == this)
        {
            players.Remove(oldIndex);
        }

        // 2. 새로운 인덱스로 딕셔너리 등록
        players[newIndex] = this;

        // 3. 비주얼 및 UI 업데이트
        UpdatePlayerVisual(newIndex);
        NotifyReadyManager();
    }

    // SyncVar Hook: 준비 상태 변경 시 호출
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