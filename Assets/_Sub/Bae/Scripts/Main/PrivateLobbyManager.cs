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
    [SerializeField] private GameObject hostPanel;              // [HOST] 클릭 시 뜨는 설정 패널

    [Header("메인 & 실행 버튼 연결")]
    [SerializeField] private Button mainHostButton;             // 메인 화면의 [HOST] 버튼
    [SerializeField] private Button makeRoomButton;              // 패널 안의 [MAKE ROOM] 버튼

    [Header("UI 연결 - 방 설정 (패널 내부)")]
    [SerializeField] private TMP_InputField roomNameInputField;   // 방 이름 입력창 (ROOM TITLE)

    [Header("챕터 선택 UI (< Chapter 1 >)")]
    [SerializeField] private TextMeshProUGUI chapterDisplayText; // 선택된 챕터 글자
    [SerializeField] private Button prevChapterButton;            // 챕터 왼쪽 화살표 <
    [SerializeField] private Button nextChapterButton;            // 챕터 오른쪽 화살표 >

    [Header("방 타입 선택 UI (< Public / Private >)")]
    [SerializeField] private TextMeshProUGUI roomTypeDisplayText; // 선택된 타입 글자 ("Public" 또는 "Private")
    [SerializeField] private Button prevRoomTypeButton;           // 타입 왼쪽 화살표 <
    [SerializeField] private Button nextRoomTypeButton;           // 타입 오른쪽 화살표 >

    [Header("로딩 UI 연결 (비워두어도 자동 탐색됩니다)")]
    [SerializeField] private GameObject loadingPanel;

    [Header("에러 팝업 UI 연결")]
    [SerializeField] private GameObject errorPopupPanel;
    [SerializeField] private TextMeshProUGUI errorMessageText;

    [Header("씬 설정")]
    [SerializeField] private string lobbySceneName = "Lobby";
    [SerializeField] private string mainSceneName = "Main";

    private EOSLobby eosLobby;
    private bool isSubscribed = false; // 중복 구독 방지 플래그

    public static string currentShortCode = "";

    private int selectedChapterIndex = 1;
    private int maxChapterCount = 6;
    private bool isPublicRoom = true; // 기본값 퍼블릭

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

    private void OnEnable()
    {
        SubscribeEvents();
    }

    private void Start()
    {
        SubscribeEvents();

        if (hostPanel != null) hostPanel.SetActive(false);
        if (errorPopupPanel != null) errorPopupPanel.SetActive(false);

        UpdateChapterUI();
        UpdateRoomTypeUI();
    }

    private void OnDisable()
    {
        UnsubscribeEvents();
    }

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
        if (chapterDisplayText != null)
        {
            chapterDisplayText.text = $"Chapter {selectedChapterIndex}";
        }
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
        if (roomTypeDisplayText != null)
        {
            roomTypeDisplayText.text = isPublicRoom ? "Public" : "Private";
        }
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
            ? roomNameInputField.text
            : "Pico Room";

        currentShortCode = GenerateShortCode();
        isCreatingLobby = true;

        SetAllButtonsInteractable(false);

        currentPanel = GetLoadingPanel();
        if (currentPanel != null) currentPanel.SetActive(true);

        lobby.CreateLobby(4, isPublicRoom ? LobbyPermissionLevel.Publicadvertised : LobbyPermissionLevel.Inviteonly, false, null);

        StartCoroutine(CreateLobbyAndSetAttributesRoutine(roomTitle));
    }

    private void OnCreateLobbySucceeded(List<Attribute> attributes)
    {
        isCreatingLobby = false;
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

        List<AttributeData> attrDataList = new List<AttributeData>
        {
            new AttributeData { Key = "ROOM_NAME", Value = roomTitle },
            new AttributeData { Key = "CHAPTER", Value = selectedChapterIndex.ToString() },
            new AttributeData { Key = "SHORTCODE", Value = currentShortCode },
            new AttributeData { Key = "IS_PUBLIC", Value = isPublicRoom ? "1" : "0" }
        };

        var lobby = GetEOSLobby();
        if (lobby != null)
        {
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

        WalkingLoadingPanel[] allPanels = Resources.FindObjectsOfTypeAll<WalkingLoadingPanel>();
        foreach (var p in allPanels)
        {
            if (p != null && p.gameObject.scene.IsValid())
            {
                loadingPanel = p.gameObject;
                break;
            }
        }
        return loadingPanel;
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