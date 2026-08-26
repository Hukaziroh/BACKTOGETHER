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

    [Header("오른쪽 UI: 룸코드 (분리된 오브젝트)")]
    // 🌟 라벨(번역된 문구)과 값(코드 데이터)을 별도로 관리합니다.
    public TextMeshProUGUI roomCodeLabelText;
    public TextMeshProUGUI roomCodeValueText;

    [SyncVar(hook = nameof(OnRoomCodeUpdated))]
    public string roomCode = "";

    [SyncVar(hook = nameof(OnRoomNameUpdated))]
    public string roomName = ""; // 방 이름 SyncVar

    [SyncVar(hook = nameof(OnRoomVisibilityUpdated))]
    public bool isPublicRoom = true;

    void Awake()
    {
        if (instance == null) instance = this;
        else Destroy(gameObject);

        ForceRoomCodeTextEllipsis();
    }

    public override void OnStartServer()
    {
        base.OnStartServer();
        // 방 생성시 저장된 이름을 가져와 동기화
        roomName = PrivateLobbyManager.lastCreatedRoomName;
        isPublicRoom = PrivateLobbyManager.lastCreatedRoomIsPublic;
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

    void OnRoomNameUpdated(string oldValue, string newValue)
    {
        UpdateRoomCodeUI(roomCode);
    }

    void OnRoomVisibilityUpdated(bool oldValue, bool newValue)
    {
        UpdateRoomCodeUI(roomCode);
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
        // 🌟 문자열 파싱을 하지 않고 값(Value) 오브젝트만 업데이트합니다.
        if (roomCodeValueText == null) return;
        ForceRoomCodeTextEllipsis();

        bool isMainScene = UnityEngine.SceneManagement.SceneManager.GetActiveScene().name == "Main";

        string formattedText = isMainScene
            ? "Empty"
            : isPublicRoom
                ? roomName
                : code;

        if (!isPublicRoom && string.IsNullOrEmpty(formattedText) && !isMainScene &&
            !string.IsNullOrEmpty(PrivateLobbyManager.currentShortCode))
        {
            formattedText = PrivateLobbyManager.currentShortCode;
        }

        // 스트리머 모드(가리기) 적용 여부 확인
        bool isCodeVisible = true;
        if (OptionsManager.instance != null)
        {
            isCodeVisible = OptionsManager.instance.IsCodeVisible;
        }

        // 방 이름은 가리지 않고, 비공개방 코드에만 스트리머 모드를 적용한다.
        if (!isMainScene && !isPublicRoom && !isCodeVisible)
        {
            formattedText = "******";
        }

        // 🌟 더 이상 접두사(Prefix)를 찾거나 파싱하지 않습니다.
        // 라벨(roomCodeLabelText)은 이미 유니티 내 번역 시스템이 처리한 대로 고정되어 있고,
        // 우리는 오직 코드 값(roomCodeValueText)만 갱신합니다.
        roomCodeValueText.text = formattedText;
    }

    private void ForceRoomCodeTextEllipsis()
    {
        if (roomCodeValueText != null)
        {
            roomCodeValueText.textWrappingMode = TextWrappingModes.NoWrap;
            roomCodeValueText.overflowMode = TextOverflowModes.Ellipsis;
        }

        if (roomCodeLabelText != null)
        {
            roomCodeLabelText.textWrappingMode = TextWrappingModes.NoWrap;
            roomCodeLabelText.overflowMode = TextOverflowModes.Ellipsis;
        }
    }
}
