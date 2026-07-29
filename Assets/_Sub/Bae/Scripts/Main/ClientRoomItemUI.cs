using UnityEngine;
using UnityEngine.UI;
using TMPro;
using Epic.OnlineServices.Lobby;

public class ClientRoomItemUI : MonoBehaviour
{
    [Header("UI 요소 연결")]
    [SerializeField] private TextMeshProUGUI roomNameText;   // 방 제목
    [SerializeField] private TextMeshProUGUI chapterText;    // 챕터 (예: "Ch.3")
    [SerializeField] private TextMeshProUGUI playerCountText; // 인원수 (예: "2/4")
    [SerializeField] private Button joinButton;             // 입장 버튼

    private LobbyDetails targetLobby;
    private System.Action<LobbyDetails> onJoinCallback;

    private void Awake()
    {
        if (joinButton != null)
            joinButton.onClick.AddListener(OnJoinButtonClicked);
    }

    public void Setup(LobbyDetails lobby, System.Action<LobbyDetails> onJoinClicked)
    {
        targetLobby = lobby;
        onJoinCallback = onJoinClicked;

        // 1. 방 이름 가져오기
        string roomName = "알 수 없는 방";
        Epic.OnlineServices.Lobby.Attribute attr;
        if (lobby.CopyAttributeByKey(new LobbyDetailsCopyAttributeByKeyOptions { AttrKey = "ROOM_NAME" }, out attr) == Epic.OnlineServices.Result.Success && attr.Data.HasValue)
        {
            roomName = attr.Data.Value.Value.AsUtf8;
        }

        // 2. 챕터 가져오기
        string chapterStr = "1";
        if (lobby.CopyAttributeByKey(new LobbyDetailsCopyAttributeByKeyOptions { AttrKey = "CHAPTER" }, out attr) == Epic.OnlineServices.Result.Success && attr.Data.HasValue)
        {
            chapterStr = attr.Data.Value.Value.AsUtf8;
        }

        // 3. 인원수 가져오기
        uint currentMembers = lobby.GetMemberCount(new LobbyDetailsGetMemberCountOptions());
        uint maxMembers = 4;
        var infoResult = lobby.GetLobbyDetailsInfo(new LobbyDetailsGetLobbyDetailsInfoOptions(), out var lobbyInfo);
        if (infoResult == Epic.OnlineServices.Result.Success && lobbyInfo.HasValue)
        {
            maxMembers = lobbyInfo.Value.MaxMembers;
        }

        // UI 세팅
        if (roomNameText != null) roomNameText.text = roomName;
        if (chapterText != null) chapterText.text = $"Ch.{chapterStr}";
        if (playerCountText != null) playerCountText.text = $"{currentMembers} / {maxMembers}";

        // 꽉 찬 방은 입장 불가
        if (joinButton != null)
        {
            joinButton.interactable = (currentMembers < maxMembers);
        }
    }

    private void OnJoinButtonClicked()
    {
        if (targetLobby != null)
        {
            onJoinCallback?.Invoke(targetLobby);
        }
    }
}