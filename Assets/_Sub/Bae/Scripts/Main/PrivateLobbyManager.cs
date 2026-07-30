using UnityEngine;
using EpicTransport;
using Mirror;
using System.Collections;
using System.Collections.Generic;
using Epic.OnlineServices.Lobby;
using UnityEngine.UI;
using TMPro;
using UnityEngine.SceneManagement;

public class PrivateLobbyManager : MonoBehaviour
{
    [Header("UI 패널 연결")]
    [SerializeField] private GameObject hostPanel;

    [Header("메인 & 실행 버튼 연결")]
    [SerializeField] private Button mainHostButton;
    [SerializeField] private Button makeRoomButton;

    [Header("UI 연결 - 방 설정 (패널 내부)")]
    [SerializeField] private TMP_InputField roomNameInputField;

    [Header("챕터 선택 UI (< Chapter 1 >)")]
    [SerializeField] private TextMeshProUGUI chapterDisplayText;
    [SerializeField] private Button prevChapterButton;
    [SerializeField] private Button nextChapterButton;

    [Header("방 타입 선택 UI (< Public / Private >)")]
    [SerializeField] private TextMeshProUGUI roomTypeDisplayText;
    [SerializeField] private Button prevRoomTypeButton;
    [SerializeField] private Button nextRoomTypeButton;

    [Header("로딩 UI 연결 (비워두어도 자동 탐색됩니다)")]
    [SerializeField] private GameObject loadingPanel;

    [Header("에러 팝업 UI 연결")]
    [SerializeField] private GameObject errorPopupPanel;
    [SerializeField] private TextMeshProUGUI errorMessageText;

    [Header("씬 설정")]
    [SerializeField] private string lobbySceneName = "Lobby";
    [SerializeField] private string mainSceneName = "Main";

    private EOSLobby eosLobby;
    private bool isSubscribed = false;

    public static string currentShortCode = "";

    private int selectedChapterIndex = 1;
    private int maxChapterCount = 6;
    private bool isPublicRoom = true; // 기본은 퍼블릭!

    private bool isCreatingLobby = false;
    private bool attributeUpdateDone = false;
    private bool attributeUpdateFailed = false;

    private GameObject currentPanel;

    private EOSLobby GetEOSLobby()
    {
        if (eosLobby == null && NetworkManager.singleton != null)
        {
            eosLobby = NetworkManager.singleton.GetComponent<EOSLobby>();
        }
        return eosLobby;
    }

    private void SubscribeEvents()
    {
        if (isSubscribed) return;

        var lobby = GetEOSLobby();
        if (lobby != null)
        {
            lobby.CreateLobbySucceeded += OnCreateLobbySucceeded;
            lobby.CreateLobbyFailed += OnCreateLobbyFailed;
            lobby.LobbyAttributesUpdateSucceeded += OnLobbyAttributesUpdateSucceeded;
            lobby.LobbyAttributesUpdateFailed += OnLobbyAttributesUpdateFailed;
            isSubscribed = true;
        }
    }

    private void UnsubscribeEvents()
    {
        if (!isSubscribed) return;

        if (eosLobby != null)
        {
            eosLobby.CreateLobbySucceeded -= OnCreateLobbySucceeded;
            eosLobby.CreateLobbyFailed -= OnCreateLobbyFailed;
            eosLobby.LobbyAttributesUpdateSucceeded -= OnLobbyAttributesUpdateSucceeded;
            eosLobby.LobbyAttributesUpdateFailed -= OnLobbyAttributesUpdateFailed;
        }
        isSubscribed = false;
    }

    private void OnEnable() { SubscribeEvents(); }
    private void Start()
    {
        SubscribeEvents();
        if (hostPanel != null) hostPanel.SetActive(false);
        if (errorPopupPanel != null) errorPopupPanel.SetActive(false);
        UpdateChapterUI();
        UpdateRoomTypeUI();
    }
    private void OnDisable() { UnsubscribeEvents(); }

    public void OnClick_MainHost()
    {
        SubscribeEvents();
        if (hostPanel != null) hostPanel.SetActive(true);
    }

    public void OnClick_PrevChapter()
    {
        selectedChapterIndex--;
        if (selectedChapterIndex < 1) selectedChapterIndex = maxChapterCount;
        UpdateChapterUI();
    }

    public void OnClick_NextChapter()
    {
        selectedChapterIndex++;
        if (selectedChapterIndex > maxChapterCount) selectedChapterIndex = 1;
        UpdateChapterUI();
    }

    private void UpdateChapterUI()
    {
        if (chapterDisplayText != null) chapterDisplayText.text = $"Chapter {selectedChapterIndex}";
    }

    public void OnClick_PrevRoomType()
    {
        isPublicRoom = !isPublicRoom;
        UpdateRoomTypeUI();
    }

    public void OnClick_NextRoomType()
    {
        isPublicRoom = !isPublicRoom;
        UpdateRoomTypeUI();
    }

    private void UpdateRoomTypeUI()
    {
        if (roomTypeDisplayText != null) roomTypeDisplayText.text = isPublicRoom ? "Public" : "Private";
    }

    public void OnClick_MakeRoom()
    {
        SubscribeEvents();
        if (isCreatingLobby) return;

        var lobby = GetEOSLobby();
        if (lobby == null)
        {
            ShowErrorPopup("네트워크 시스템이 준비되지 않았습니다.");
            return;
        }

        string roomTitle = (roomNameInputField != null && !string.IsNullOrEmpty(roomNameInputField.text))
            ? roomNameInputField.text : "Pico Room";

        // ★ 분리 로직 핵심: 퍼블릭 방이면 코드를 비우고, 프라이빗일 때만 6자리 코드를 생성!
        if (isPublicRoom)
        {
            currentShortCode = "";
        }
        else
        {
            currentShortCode = GenerateShortCode();
        }

        isCreatingLobby = true;
        SetAllButtonsInteractable(false);

        currentPanel = GetLoadingPanel();
        if (currentPanel != null) currentPanel.SetActive(true);

        // ★ 에픽 서버에 방 생성 요청 (퍼블릭은 리스트에 띄우고, 프라이빗은 초대 전용으로 숨김)
        LobbyPermissionLevel permission = isPublicRoom ? LobbyPermissionLevel.Publicadvertised : LobbyPermissionLevel.Inviteonly;
        lobby.CreateLobby(4, permission, false, null);

        StartCoroutine(CreateLobbyAndSetAttributesRoutine(roomTitle));
    }

    private void OnCreateLobbySucceeded(List<Epic.OnlineServices.Lobby.Attribute> attributes)
    {
        isCreatingLobby = false; // 방 생성이 확인되면, 아래 코루틴에서 다음 단계(속성 부여)로 넘어갑니다.
    }

    private void OnCreateLobbyFailed(string errorMessage)
    {
        isCreatingLobby = false;
        SetAllButtonsInteractable(true);
        if (currentPanel != null) currentPanel.SetActive(false);
        ShowErrorPopup("방 생성 실패: " + errorMessage);
    }

    private void OnLobbyAttributesUpdateSucceeded()
    {
        attributeUpdateDone = true;
        attributeUpdateFailed = false;
    }

    private void OnLobbyAttributesUpdateFailed(string errorMessage)
    {
        attributeUpdateDone = true;
        attributeUpdateFailed = true;
    }

    private IEnumerator CreateLobbyAndSetAttributesRoutine(string roomTitle)
    {
        float timeout = 5f;
        while (isCreatingLobby && timeout > 0f)
        {
            timeout -= Time.unscaledDeltaTime;
            yield return null;
        }

        if (isCreatingLobby)
        {
            SetAllButtonsInteractable(true);
            if (currentPanel != null) currentPanel.SetActive(false);
            ShowErrorPopup("방 생성 시간이 초과되었습니다.");
            yield break;
        }

        attributeUpdateDone = false;
        attributeUpdateFailed = false;

        // ★ 해결!: 에픽 서버가 인식할 수 있는 순수한 List 형태로 속성(AttributeData)들을 조립합니다.
        List<AttributeData> attrDataList = new List<AttributeData>();

        // 공통 속성
        attrDataList.Add(new AttributeData { Key = "ROOM_NAME", Value = roomTitle });
        attrDataList.Add(new AttributeData { Key = "CHAPTER", Value = selectedChapterIndex.ToString() });
        attrDataList.Add(new AttributeData { Key = "IS_PUBLIC", Value = isPublicRoom ? "1" : "0" });

        // 프라이빗 방일 때만 숏코드(비밀번호) 속성을 욱여넣습니다.
        if (!isPublicRoom)
        {
            attrDataList.Add(new AttributeData { Key = "SHORTCODE", Value = currentShortCode });
        }

        var lobby = GetEOSLobby();
        if (lobby != null)
        {
            // 리스트를 배열(.ToArray())로 변환하여 에픽 서버로 발사!
            lobby.UpdateLobbyAttributes(attrDataList.ToArray());
        }

        float attributeTimeout = 5f;
        while (!attributeUpdateDone && attributeTimeout > 0f)
        {
            attributeTimeout -= Time.unscaledDeltaTime;
            yield return null;
        }

        if (attributeUpdateFailed || attributeTimeout <= 0f)
        {
            SetAllButtonsInteractable(true);
            if (currentPanel != null) currentPanel.SetActive(false);
            ShowErrorPopup("방 정보를 설정하지 못했습니다. 다시 시도해주세요.");
            yield break;
        }

        EosTransport transport = NetworkManager.singleton.transport as EosTransport;
        if (transport != null) transport.ResetIgnoreMessagesAtStartUpTimer();

        // 모든 준비가 끝났으니 방장으로서 접속!
        NetworkManager.singleton.StartHost();

        yield return new WaitForSecondsRealtime(0.2f);
        SetAllButtonsInteractable(true);
        if (currentPanel != null) currentPanel.SetActive(false);
    }

    private void SetAllButtonsInteractable(bool interactable)
    {
        if (mainHostButton != null) mainHostButton.interactable = interactable;
        if (makeRoomButton != null) makeRoomButton.interactable = interactable;
        if (prevChapterButton != null) prevChapterButton.interactable = interactable;
        if (nextChapterButton != null) nextChapterButton.interactable = interactable;
        if (prevRoomTypeButton != null) prevRoomTypeButton.interactable = interactable;
        if (nextRoomTypeButton != null) nextRoomTypeButton.interactable = interactable;
    }

    private GameObject GetLoadingPanel()
    {
        if (loadingPanel != null) return loadingPanel;
        if (WalkingLoadingPanel.Instance != null)
        {
            loadingPanel = WalkingLoadingPanel.Instance.gameObject;
            return loadingPanel;
        }
        return null;
    }

    private string GenerateShortCode()
    {
        string allowedChars = "0123456789";
        string code = "";
        for (int i = 0; i < 6; i++) code += allowedChars[Random.Range(0, allowedChars.Length)];
        return code;
    }

    private void ShowErrorPopup(string message)
    {
        if (errorPopupPanel != null)
        {
            if (errorMessageText != null) errorMessageText.text = message;
            errorPopupPanel.SetActive(true);
        }
    }

    public void OnClick_ReturnToMain()
    {
        if (errorPopupPanel != null) errorPopupPanel.SetActive(false);
        if (!string.IsNullOrEmpty(mainSceneName)) SceneManager.LoadScene(mainSceneName);
    }
}