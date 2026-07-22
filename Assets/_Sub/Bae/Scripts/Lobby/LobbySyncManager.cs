using UnityEngine;
using TMPro;
using Mirror;
using Epic.OnlineServices.Lobby;
using Epic.OnlineServices;

public class LobbySyncManager : NetworkBehaviour
{
    public static LobbySyncManager instance;

    [Header("왼쪽 UI: 플레이어 인원수")]
    public TextMeshProUGUI playerCountText;

    [SyncVar(hook = nameof(OnPlayerCountUpdated))]
    public int playerCount = 0;

    [Header("오른쪽 UI: 룸코드 (6자리 숏코드)")]
    public TextMeshProUGUI roomCodeText;

    private EOSLobby eosLobby;

    void Awake()
    {
        if (instance == null) instance = this;
        else Destroy(gameObject);
    }

    void Start()
    {
        eosLobby = FindAnyObjectByType<EOSLobby>();
    }

    void Update()
    {
        if (eosLobby == null)
        {
            eosLobby = FindAnyObjectByType<EOSLobby>();
        }

        // 1. 서버(호스트)에서 실시간 접속 인원 동기화
        if (isServer)
        {
            int currentCount = NetworkServer.connections.Count;
            if (playerCount != currentCount)
            {
                playerCount = currentCount;
            }
        }

        // 2. 오른쪽에 룸코드(숏코드) 표시
        if (roomCodeText != null)
        {
            string displayCode = "";

            // 호스트인 경우 PrivateLobbyManager의 숏코드 우선 확인
            if (!string.IsNullOrEmpty(PrivateLobbyManager.currentShortCode))
            {
                displayCode = PrivateLobbyManager.currentShortCode;
            }
            // 클라이언트이거나 호스트인데 변수가 비어있는 경우 EOSLobby 어트리뷰트에서 가져오기
            else if (eosLobby != null && eosLobby.ConnectedToLobby && eosLobby.ConnectedLobbyDetails != null)
            {
                try
                {
                    Attribute shortCodeAttribute = new Attribute();
                    Result result = eosLobby.ConnectedLobbyDetails.CopyAttributeByKey(
                        new LobbyDetailsCopyAttributeByKeyOptions { AttrKey = "SHORTCODE" },
                        out shortCodeAttribute
                    );

                    if (result == Result.Success)
                    {
                        displayCode = shortCodeAttribute.Data.Value.AsUtf8;
                    }
                }
                catch
                {
                    // 예외 처리 무시
                }
            }

            roomCodeText.text = $"Room Code: {(string.IsNullOrEmpty(displayCode) ? "-" : displayCode)}";
        }
    }

    public override void OnStartClient()
    {
        base.OnStartClient();
        UpdatePlayerCountUI(playerCount);
    }

    [Server]
    public void RefreshPlayerCount()
    {
        playerCount = NetworkServer.connections.Count;
    }

    void OnPlayerCountUpdated(int oldValue, int newValue)
    {
        UpdatePlayerCountUI(newValue);
    }

    private void UpdatePlayerCountUI(int count)
    {
        if (playerCountText != null)
        {
            playerCountText.text = $"Player: {count} / 4";
        }
    }
}