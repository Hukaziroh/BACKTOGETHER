using UnityEngine;
using TMPro;
using Mirror;

public class LobbySyncManager : NetworkBehaviour
{
    public static LobbySyncManager instance;

    [Header("왼쪽 UI: 플레이어 인원수")]
    public TextMeshProUGUI playerCountText;

    [SyncVar(hook = nameof(OnPlayerCountUpdated))]
    public int playerCount = 0;

    [Header("오른쪽 UI: 룸코드")]
    public TextMeshProUGUI roomCodeText;

    [SyncVar(hook = nameof(OnRoomCodeUpdated))]
    public string roomCode = "";

    void Awake()
    {
        if (instance == null) instance = this;
        else Destroy(gameObject);
    }

    void Update()
    {
        if (isServer)
        {
            int currentCount = NetworkServer.connections.Count;
            if (playerCount != currentCount)
            {
                playerCount = currentCount;
            }

            if (string.IsNullOrEmpty(roomCode) && !string.IsNullOrEmpty(PrivateLobbyManager.currentShortCode))
            {
                roomCode = PrivateLobbyManager.currentShortCode;
            }
        }
    }

    public override void OnStartClient()
    {
        base.OnStartClient();
        UpdatePlayerCountUI(playerCount);
        UpdateRoomCodeUI(roomCode);
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

    void OnRoomCodeUpdated(string oldValue, string newValue)
    {
        UpdateRoomCodeUI(newValue);
    }

    private void UpdatePlayerCountUI(int count)
    {
        if (playerCountText != null)
        {
            playerCountText.text = $"Player: {count} / 4";
        }
    }

    public void RefreshRoomCodeUIState()
    {
        UpdateRoomCodeUI(roomCode);
    }

    private void UpdateRoomCodeUI(string code)
    {
        if (roomCodeText == null) return;

        bool isMainScene = UnityEngine.SceneManagement.SceneManager.GetActiveScene().name == "Main";
        string formattedCode = (isMainScene || string.IsNullOrEmpty(code)) ? "Empty" : code;

        // 스트리머 모드 적용 여부 확인 (OptionsManager가 존재하고 IsCodeVisible이 false이면 마스킹 처리)
        bool isCodeVisible = true;
        if (OptionsManager.instance != null)
        {
            isCodeVisible = OptionsManager.instance.IsCodeVisible;
        }

        if (!isMainScene && !string.IsNullOrEmpty(code) && !isCodeVisible)
        {
            formattedCode = "******";
        }

        // 줄바꿈 문자(\n, \r)를 유지하거나 기존 텍스트 형태에 맞춰 prefix 추출
        string prefix = "Code:\n";
        if (!string.IsNullOrEmpty(roomCodeText.text))
        {
            int splitIndex = roomCodeText.text.IndexOf('\n');
            if (splitIndex != -1 && roomCodeText.text.Length >= splitIndex + 1)
            {
                prefix = roomCodeText.text.Substring(0, splitIndex + 1);
            }
            else
            {
                int colonIndex = roomCodeText.text.IndexOf(':');
                if (colonIndex != -1)
                {
                    prefix = roomCodeText.text.Substring(0, colonIndex + 1);
                }
            }
        }

        // 최종 텍스트 적용 (줄바꿈 포함)
        roomCodeText.text = prefix + formattedCode;
    }
}