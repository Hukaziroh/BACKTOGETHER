using System.Collections;
using UnityEngine;
using UnityEngine.UI;
using TMPro;
using Epic.OnlineServices.Lobby;

public class ClientRoomItemUI : MonoBehaviour
{
    [Header("UI 요소 연결")]
    [SerializeField] private TextMeshProUGUI roomNameText;    // 방 제목
    [SerializeField] private TextMeshProUGUI chapterText;     // 챕터 (예: "Ch.3")
    [SerializeField] private TextMeshProUGUI playerCountText; // 인원수 (예: "2/4")
    [SerializeField] private Button joinButton;              // 입장 버튼

    private LobbyDetails targetLobby;
    private System.Action<LobbyDetails> onJoinCallback;

    // ⭐ [추가] 모든 방 리스트 버튼이 공유하는 3초 쿨다운 타임
    private static float globalNextClickTime = 0f;

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
        Attribute attr;
        if (lobby.CopyAttributeByKey(new LobbyDetailsCopyAttributeByKeyOptions { AttrKey = "ROOM_NAME" }, out attr) == Epic.OnlineServices.Result.Success)
        {
            if (attr.Data != null)
            {
                roomName = attr.Data.Value.AsUtf8;
            }
        }

        // 2. 챕터 가져오기
        string chapterStr = "1";
        string chapterDisplayName = "";
        if (lobby.CopyAttributeByKey(new LobbyDetailsCopyAttributeByKeyOptions { AttrKey = "CHAPTER" }, out attr) == Epic.OnlineServices.Result.Success)
        {
            if (attr.Data != null)
            {
                chapterStr = attr.Data.Value.AsUtf8;
            }
        }

        if (lobby.CopyAttributeByKey(new LobbyDetailsCopyAttributeByKeyOptions { AttrKey = "CHAPTER_NAME" }, out attr) == Epic.OnlineServices.Result.Success)
        {
            if (attr.Data != null)
            {
                chapterDisplayName = attr.Data.Value.AsUtf8;
            }
        }

        // 3. 인원수 가져오기
        uint currentMembers, maxMembers;
        bool joinable = EOSLobby.IsLobbyJoinable(lobby, out currentMembers, out maxMembers);

        // 유령방이면 입장 불가 처리
        if (currentMembers == 0)
        {
            joinable = false;
        }

        // UI 세팅
        if (roomNameText != null) roomNameText.text = roomName;
        if (chapterText != null)
        {
            chapterText.text = string.IsNullOrEmpty(chapterDisplayName)
                ? $"Ch.{chapterStr}"
                : chapterDisplayName;
        }
        if (playerCountText != null) playerCountText.text = $"{currentMembers} / {maxMembers}";

        // 꽉 찼거나 0명인 유령방은 버튼 비활성화
        if (joinButton != null)
        {
            joinButton.interactable = joinable;
        }
    }

    private void OnJoinButtonClicked()
    {
        // ⭐ [추가] 현재 시간이 쿨다운이 끝나기 전이라면 무시 (연타 방지)
        if (Time.time < globalNextClickTime)
        {
            Debug.LogWarning("[UI] 3초 쿨다운 중입니다. 다중 클릭을 방지합니다.");
            return;
        }

        // Debug.Log("[UI] 방 입장 버튼 클릭됨!");

        if (targetLobby != null)
        {
            uint currentMembers = targetLobby.GetMemberCount(new LobbyDetailsGetMemberCountOptions());
            if (currentMembers == 0)
            {
                Debug.LogWarning("[UI] 이미 파괴 진행 중인 유령방입니다. 입장을 차단합니다.");
                if (joinButton != null) joinButton.interactable = false;
                return;
            }

            // ⭐ [추가] 클릭 성공 시, 현재 시간 + 3초를 쿨다운 해제 시간으로 설정
            globalNextClickTime = Time.time + 3f;

            // ⭐ [추가] 시각적으로도 버튼을 즉시 회색(비활성화)으로 만들고 3초 뒤에 복구하는 코루틴 실행
            if (joinButton != null) joinButton.interactable = false;
            StartCoroutine(CooldownRoutine());

            onJoinCallback?.Invoke(targetLobby);
        }
        else
        {
            Debug.LogError("[UI] targetLobby가 null입니다!");
        }
    }

    // ⭐ [추가] 3초 뒤에 버튼을 원래 상태로 되돌리는 코루틴
    private IEnumerator CooldownRoutine()
    {
        yield return new WaitForSeconds(3f);

        // 3초 뒤에 이 버튼이 파괴되지 않았고 타겟 로비가 존재한다면 다시 검사
        if (targetLobby != null && joinButton != null)
        {
            uint currentMembers, maxMembers;
            bool joinable = EOSLobby.IsLobbyJoinable(targetLobby, out currentMembers, out maxMembers);

            // 여전히 들어갈 수 있는(꽉 차지 않았고 0명도 아닌) 방이라면 다시 버튼 활성화
            if (joinable && currentMembers > 0)
            {
                joinButton.interactable = true;
            }
        }
    }
}
