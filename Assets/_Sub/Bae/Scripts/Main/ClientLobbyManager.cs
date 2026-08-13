using System.Collections;
using System.Collections.Generic;
using System.Linq;
using Epic.OnlineServices;
using Epic.OnlineServices.Lobby;
using EpicTransport;
using Mirror;
using TMPro;
using UnityEngine;
using UnityEngine.EventSystems;
using UnityEngine.InputSystem;
using UnityEngine.UI;

public class ClientLobbyManager : MonoBehaviour
{
    [Header("UI 패널 연결")]
    [SerializeField] private GameObject mainPanel;
    [SerializeField] private GameObject clientSelectionPanel;
    [SerializeField] private GameObject clientPublicPanel;
    [SerializeField] private GameObject clientPrivatePanel;
    [SerializeField] private GameObject logo;

    [Header("선택 패널 버튼 연결 (Client Selection)")]
    [SerializeField] private Button mainClientButton;
    [SerializeField] private Button selectPublicModeButton;
    [SerializeField] private Button selectPrivateModeButton;
    [SerializeField] private Button quickJoinSelectionButton;

    [Header("퍼블릭 방 리스트 - 상단 컨트롤")]
    [SerializeField] private TMP_InputField searchInputField;
    [SerializeField] private Button researchButton;
    [SerializeField] private GameObject chapterFilterSelectObject;
    [SerializeField] private TextMeshProUGUI filterChapterText;
    [SerializeField] private Button prevFilterChapterButton;
    [SerializeField] private Button nextFilterChapterButton;

    [Header("비공개 방 관련")]
    [SerializeField] private TMP_InputField privateRoomInputField; // 비공개 방 코드/비밀번호 입력용 인풋필드

    [Header("퍼블릭 방 리스트 - 스크롤 및 로비 아이템")]
    [SerializeField] private Transform roomListContainer;
    [SerializeField] private GameObject roomItemPrefab;
    [SerializeField] private GameObject publicRoomNoRoomText; // 퍼블릭 방 리스트 전용 텍스트

    [Header("퀵 조인 관련")]
    [SerializeField] private GameObject quickJoinNoRoomText; // 퀵 조인 전용 텍스트 (3초 뒤 자동 꺼짐)

    [Header("퍼블릭 방 리스트 - 하단 페이지네이션")]
    [SerializeField] private Button prevPageButton;
    [SerializeField] private Button nextPageButton;
    [SerializeField] private TextMeshProUGUI pageText;

    [Header("로딩 UI 연결 (비워두어도 자동 탐색됩니다)")]
    [SerializeField] private GameObject loadingPanel;

    [Header("일반 에러 팝업 UI 연결 (기본 Fallback용)")]
    [SerializeField] private GameObject errorPopupPanel;
    [SerializeField] private TextMeshProUGUI errorMessageText;
    [SerializeField] private Button errorCloseButton;

    [Header("연결 타임아웃 팝업 UI (10초 초과)")]
    [SerializeField] private GameObject timeoutPopupPanel;
    [SerializeField] private TextMeshProUGUI timeoutMessageText;
    [SerializeField] private Button timeoutCloseButton;

    [Header("호스트 디스커넥트 팝업 UI (방 폭파/연결 끊김)")]
    [SerializeField] private GameObject disconnectPopupPanel;
    [SerializeField] private TextMeshProUGUI disconnectMessageText;
    [SerializeField] private Button disconnectCloseButton;

    [Header("꽉 찬 방 알림 텍스트 (인스펙터에서 할당)")]
    [SerializeField] private TextMeshProUGUI fullRoomMessageText;
    private Coroutine fullRoomMessageCoroutine;

    private EOSLobby eosLobby;
    private bool isSubscribed = false;

    private bool isLocalSearchRequest = false;

    private List<LobbyDetails> allFetchedLobbies = new List<LobbyDetails>();
    private List<LobbyDetails> filteredLobbies = new List<LobbyDetails>();

    private int currentPage = 0;
    private const int itemsPerPage = 8;
    private int selectedFilterChapter = 0; // 0: All, 1~6: Chapter 1~6
    private bool isQuickJoining = false;

    private Coroutine connectionTimeoutCoroutine;
    private Coroutine hideQuickJoinNoRoomCoroutine;


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
            lobby.FindLobbiesSucceeded += OnFindLobbiesSucceeded;
            lobby.FindLobbiesFailed += OnFindLobbiesFailed;
            lobby.JoinLobbySucceeded += OnJoinLobbySucceeded;
            lobby.JoinLobbyFailed += OnJoinLobbyFailed;
            isSubscribed = true;
        }
    }

    private void UnsubscribeEvents()
    {
        if (!isSubscribed) return;

        if (eosLobby != null)
        {
            eosLobby.FindLobbiesSucceeded -= OnFindLobbiesSucceeded;
            eosLobby.FindLobbiesFailed -= OnFindLobbiesFailed;
            eosLobby.JoinLobbySucceeded -= OnJoinLobbySucceeded;
            eosLobby.JoinLobbyFailed -= OnJoinLobbyFailed;
        }
        isSubscribed = false;
    }

    private void OnEnable() { SubscribeEvents(); }

    private void Start()
    {
        SubscribeEvents();
        if (clientSelectionPanel != null) clientSelectionPanel.SetActive(false);
        if (clientPublicPanel != null) clientPublicPanel.SetActive(false);
        if (clientPrivatePanel != null) clientPrivatePanel.SetActive(false);
        if (errorPopupPanel != null) errorPopupPanel.SetActive(false);
        if (timeoutPopupPanel != null) timeoutPopupPanel.SetActive(false);
        if (disconnectPopupPanel != null) disconnectPopupPanel.SetActive(false);
        if (quickJoinNoRoomText != null) quickJoinNoRoomText.SetActive(false);

        GameObject panel = GetLoadingPanel();
        if (panel != null) panel.SetActive(false);

        if (mainPanel != null)
        {
            mainPanel.SetActive(true);
            if (GlobalSceneInputManager.Instance != null)
            {
                GlobalSceneInputManager.Instance.SetFocusScope(mainPanel);
            }
        }

        if (searchInputField != null)
        {
            searchInputField.onEndEdit.AddListener(OnSearchInputEndEdit);
            searchInputField.onValueChanged.AddListener(OnSearchInputChanged); // ★ 검색 입력 즉시 반영 리스너 추가[cite: 1]
            searchInputField.characterLimit = 15; // ★ 퍼블릭 방 검색 최대 글자 수 제한 (원하는 숫자로 변경 가능)[cite: 1]
            searchInputField.interactable = false;
        }

        if (privateRoomInputField != null)
        {
            privateRoomInputField.onEndEdit.AddListener(OnPrivateRoomInputEndEdit);
            privateRoomInputField.characterLimit = 6; // ★ 비공개 방 코드(ShortCode) 최대 글자 수 제한[cite: 1]
            privateRoomInputField.interactable = false;
        }

        UpdateFilterChapterUI();
    }

    private void OnDisable()
    {
        UnsubscribeEvents();

        if (searchInputField != null)
        {
            searchInputField.onEndEdit.RemoveListener(OnSearchInputEndEdit);
            searchInputField.onValueChanged.RemoveListener(OnSearchInputChanged); // ★ 해제 코드 추가[cite: 1]
        }

        if (privateRoomInputField != null)
        {
            privateRoomInputField.onEndEdit.RemoveListener(OnPrivateRoomInputEndEdit);
        }
    }

    private void Update()
    {
        bool leftPressed = false;
        bool rightPressed = false;
        bool upPressed = false;
        bool downPressed = false;
        bool enterPressed = false;
        bool escPressed = false;

        if (Keyboard.current != null)
        {
            leftPressed |= Keyboard.current.leftArrowKey.wasPressedThisFrame ||
                      Keyboard.current.aKey.wasPressedThisFrame;

            rightPressed |= Keyboard.current.rightArrowKey.wasPressedThisFrame ||
                      Keyboard.current.dKey.wasPressedThisFrame;

            enterPressed |= Keyboard.current.enterKey.wasPressedThisFrame ||
                      Keyboard.current.numpadEnterKey.wasPressedThisFrame;

            upPressed |= Keyboard.current.upArrowKey.wasPressedThisFrame ||
               Keyboard.current.wKey.wasPressedThisFrame;

            downPressed |= Keyboard.current.downArrowKey.wasPressedThisFrame ||
                      Keyboard.current.sKey.wasPressedThisFrame;

            escPressed |= Keyboard.current.escapeKey.wasPressedThisFrame;
        }

        if (Gamepad.current != null)
        {
            leftPressed |= Gamepad.current.dpad.left.wasPressedThisFrame ||
                      Gamepad.current.leftStick.left.wasPressedThisFrame;

            rightPressed |= Gamepad.current.dpad.right.wasPressedThisFrame ||
                      Gamepad.current.leftStick.right.wasPressedThisFrame;

            downPressed |= Gamepad.current.dpad.down.wasPressedThisFrame ||
                      Gamepad.current.leftStick.down.wasPressedThisFrame;
        }

        // 엔터 입력 처리 (팝업이 열려 있는 경우 엔터로 닫기 수행)
        if (enterPressed)
        {
            if (timeoutPopupPanel != null && timeoutPopupPanel.activeSelf)
            {
                OnClick_CloseTimeoutPopup();
                return;
            }
            else if (disconnectPopupPanel != null && disconnectPopupPanel.activeSelf)
            {
                OnClick_CloseDisconnectPopup();
                return;
            }
            else if (errorPopupPanel != null && errorPopupPanel.activeSelf)
            {
                OnClick_CloseErrorPopup();
                return;
            }
        }

        // ESC 입력 처리 (열려 있는 패널 계층에 따라 역순으로 닫기)
        if (escPressed)
        {
            if (timeoutPopupPanel != null && timeoutPopupPanel.activeSelf)
            {
                OnClick_CloseTimeoutPopup();
                return;
            }
            else if (disconnectPopupPanel != null && disconnectPopupPanel.activeSelf)
            {
                OnClick_CloseDisconnectPopup();
                return;
            }
            else if (errorPopupPanel != null && errorPopupPanel.activeSelf)
            {
                OnClick_CloseErrorPopup();
                return;
            }
            else if (clientPublicPanel != null && clientPublicPanel.activeSelf)
            {
                OnClick_ClosePublicPanel();
                return;
            }
            else if (clientPrivatePanel != null && clientPrivatePanel.activeSelf)
            {
                OnClick_ClosePrivatePanel();
                return;
            }
            else if (clientSelectionPanel != null && clientSelectionPanel.activeSelf)
            {
                OnClick_CloseSelectionPanel();
                return;
            }
        }

        if (EventSystem.current == null) return;

        GameObject selected = EventSystem.current.currentSelectedGameObject;

        // 퍼블릭 패널 입력필드 처리
        if (clientPublicPanel != null && clientPublicPanel.activeSelf)
        {
            if (selected == null) return;

            // 퍼블릭 방 리스트 하단 페이지네이션 좌우 입력 처리 (패드/키보드 공용)
            bool isPaginationSelected = (prevPageButton != null && selected == prevPageButton.gameObject) ||
                                        (nextPageButton != null && selected == nextPageButton.gameObject) ||
                                        (pageText != null && (selected == pageText.gameObject || selected == pageText.transform.parent.gameObject));

            if (isPaginationSelected)
            {
                if (leftPressed)
                {
                    OnClick_PrevPage();
                }
                else if (rightPressed)
                {
                    OnClick_NextPage();
                }

                return;
            }

            bool isFilterChapterSelected = (chapterFilterSelectObject != null && selected == chapterFilterSelectObject) ||
                                   (filterChapterText != null && (selected == filterChapterText.gameObject || selected == filterChapterText.transform.parent.gameObject)) ||
                                   (prevFilterChapterButton != null && selected == prevFilterChapterButton.gameObject) ||
                                   (nextFilterChapterButton != null && selected == nextFilterChapterButton.gameObject);

            if (isFilterChapterSelected)
            {
                if (leftPressed)
                {
                    OnClick_PrevFilterChapter();
                }
                else if (rightPressed)
                {
                    OnClick_NextFilterChapter();
                }

                return;
            }
            if (searchInputField != null)
            {
                bool isInputFieldSelected = (selected == searchInputField.gameObject) ||
                                    (selected == searchInputField.transform.parent?.gameObject);

                if (isInputFieldSelected && Keyboard.current != null)
                {
                    if (enterPressed)
                    {
                        if (!searchInputField.interactable)
                        {
                            searchInputField.interactable = true;
                            searchInputField.ActivateInputField();
                            searchInputField.Select();
                        }
                    }
                }
            }
        }

        // 비공개 패널 입력필드 및 네비게이션 처리
        if (clientPrivatePanel != null && clientPrivatePanel.activeSelf)
        {
            if (privateRoomInputField != null)
            {
                bool isPrivateInputFieldSelected = (selected == privateRoomInputField.gameObject) ||
                                    (selected == privateRoomInputField.transform.parent?.gameObject);

                if (isPrivateInputFieldSelected && Keyboard.current != null)
                {
                    if (enterPressed)
                    {
                        if (!privateRoomInputField.interactable)
                        {
                            privateRoomInputField.interactable = true;
                            privateRoomInputField.ActivateInputField();
                            privateRoomInputField.Select();
                        }
                    }
                }
            }

            if (upPressed)
            {
                SelectPrivateRoomUp();
            }

            if (downPressed)
            {
                SelectPrivateRoomDown();
            }
        }
    }

    private void OnSearchInputEndEdit(string text)
    {
        if (searchInputField != null)
        {
            searchInputField.interactable = false;

            Selectable nextSelectable = searchInputField.FindSelectableOnDown();
            if (nextSelectable != null && EventSystem.current != null)
            {
                EventSystem.current.SetSelectedGameObject(nextSelectable.gameObject);
            }
        }
    }

    private void OnPrivateRoomInputEndEdit(string text)
    {
        if (privateRoomInputField != null)
        {
            privateRoomInputField.interactable = false;

            Selectable nextSelectable = privateRoomInputField.FindSelectableOnDown();
            if (nextSelectable != null && EventSystem.current != null)
            {
                EventSystem.current.SetSelectedGameObject(nextSelectable.gameObject);
            }
        }
    }

    #region Panel Navigation

    public void OnClick_MainClient()
    {
        SubscribeEvents();

        if (mainPanel != null) mainPanel.SetActive(false);
        if (clientSelectionPanel != null)
        {
            clientSelectionPanel.SetActive(true);
            if (GlobalSceneInputManager.Instance != null)
            {
                GlobalSceneInputManager.Instance.SetFocusScope(clientSelectionPanel);
            }
            if (EventSystem.current != null)
            {
                EventSystem.current.SetSelectedGameObject(null);
                if (selectPublicModeButton != null)
                    EventSystem.current.SetSelectedGameObject(selectPublicModeButton.gameObject);
            }
        }
    }

    public void OnClick_SelectPublicMode()
    {
        SubscribeEvents();

        if (searchInputField != null)
        {
            searchInputField.text = string.Empty;
            searchInputField.interactable = false;
            searchInputField.DeactivateInputField();
        }
        selectedFilterChapter = 0;
        UpdateFilterChapterUI();

        if (clientSelectionPanel != null) clientSelectionPanel.SetActive(false);
        if (clientPublicPanel != null)
        {
            clientPublicPanel.SetActive(true);
            if (GlobalSceneInputManager.Instance != null)
            {
                GlobalSceneInputManager.Instance.SetFocusScope(clientPublicPanel);
            }
        }
        if (logo != null) logo.SetActive(false);

        OnClick_Research();
    }

    public void OnClick_SelectPrivateMode()
    {
        SubscribeEvents();

        if (privateRoomInputField != null)
        {
            privateRoomInputField.text = string.Empty;
            privateRoomInputField.interactable = false;
            privateRoomInputField.DeactivateInputField();
        }

        if (clientSelectionPanel != null) clientSelectionPanel.SetActive(false);
        if (clientPrivatePanel != null)
        {
            clientPrivatePanel.SetActive(true);
            if (GlobalSceneInputManager.Instance != null)
            {
                GlobalSceneInputManager.Instance.SetFocusScope(clientPrivatePanel);
            }
        }
    }

    public void OnClick_ReturnToConnectPanel()
    {
        if (clientPublicPanel != null) clientPublicPanel.SetActive(false);
        if (clientPrivatePanel != null) clientPrivatePanel.SetActive(false);
        if (clientSelectionPanel != null) clientSelectionPanel.SetActive(false);

        if (logo != null) logo.SetActive(true);
        if (errorPopupPanel != null) errorPopupPanel.SetActive(false);
        if (timeoutPopupPanel != null) timeoutPopupPanel.SetActive(false);
        if (disconnectPopupPanel != null) disconnectPopupPanel.SetActive(false);

        if (mainPanel != null)
        {
            mainPanel.SetActive(true);
            if (GlobalSceneInputManager.Instance != null)
            {
                GlobalSceneInputManager.Instance.SetFocusScope(mainPanel);
            }
            if (EventSystem.current != null)
            {
                EventSystem.current.SetSelectedGameObject(null);
                if (mainClientButton != null)
                {
                    EventSystem.current.SetSelectedGameObject(mainClientButton.gameObject);
                }
            }
        }
    }

    public void OnClick_CloseSelectionPanel()
    {
        OnClick_ReturnToConnectPanel();
    }

    public void OnClick_ClosePublicPanel()
    {
        if (clientPublicPanel != null) clientPublicPanel.SetActive(false);
        if (logo != null) logo.SetActive(true);
        if (clientSelectionPanel != null)
        {
            clientSelectionPanel.SetActive(true);
            if (GlobalSceneInputManager.Instance != null)
            {
                GlobalSceneInputManager.Instance.SetFocusScope(clientSelectionPanel);
            }
            if (EventSystem.current != null)
            {
                EventSystem.current.SetSelectedGameObject(null);
                if (selectPublicModeButton != null)
                {
                    EventSystem.current.SetSelectedGameObject(selectPublicModeButton.gameObject);
                }
            }
        }
    }

    public void OnClick_ClosePrivatePanel()
    {
        if (clientPrivatePanel != null) clientPrivatePanel.SetActive(false);
        if (clientSelectionPanel != null)
        {
            clientSelectionPanel.SetActive(true);
            if (GlobalSceneInputManager.Instance != null)
            {
                GlobalSceneInputManager.Instance.SetFocusScope(clientSelectionPanel);
            }
            if (EventSystem.current != null)
            {
                EventSystem.current.SetSelectedGameObject(null);
                if (selectPrivateModeButton != null)
                {
                    EventSystem.current.SetSelectedGameObject(selectPrivateModeButton.gameObject);
                }
            }
        }
    }

    #endregion

    #region Public Room List Logic

    public void OnClick_Research()
    {
        SubscribeEvents();

        var lobby = GetEOSLobby();
        if (lobby == null)
        {
            ShowError("네트워크 시스템이 준비되지 않았습니다.");
            return;
        }

        SetInteractableAll(false);

        LobbySearchSetParameterOptions[] searchOptions = new LobbySearchSetParameterOptions[]
        {
            new LobbySearchSetParameterOptions
            {
                ComparisonOp = ComparisonOp.Equal,
                Parameter = new AttributeData { Key = "IS_PUBLIC", Value = "1" }
            }
        };

        isLocalSearchRequest = true;
        lobby.FindLobbies(50, searchOptions);
    }

    private void OnFindLobbiesSucceeded(List<LobbyDetails> lobbies)
    {
        if (!isLocalSearchRequest) return;
        isLocalSearchRequest = false;

        SetInteractableAll(true);
        allFetchedLobbies = lobbies ?? new List<LobbyDetails>();

        if (isQuickJoining)
        {
            isQuickJoining = false;

            foreach (var lobby in allFetchedLobbies)
            {
                if (EOSLobby.IsLobbyJoinable(lobby, out uint currentMembers, out uint maxMembers))
                {
                    Debug.Log($"[QuickJoin] 빈 방 발견! ({currentMembers}/{maxMembers}) 즉시 입장합니다.");
                    JoinRoom(lobby);
                    return;
                }
            }

            if (quickJoinNoRoomText != null)
            {
                if (hideQuickJoinNoRoomCoroutine != null)
                    StopCoroutine(hideQuickJoinNoRoomCoroutine);

                hideQuickJoinNoRoomCoroutine = StartCoroutine(ShowAndHideQuickJoinNoRoomText());
            }
            return;
        }

        ApplyFiltersAndRefresh();
    }

    private IEnumerator ShowAndHideQuickJoinNoRoomText()
    {
        quickJoinNoRoomText.SetActive(true);
        yield return new WaitForSeconds(3f);
        quickJoinNoRoomText.SetActive(false);
        hideQuickJoinNoRoomCoroutine = null;
    }

    private void OnFindLobbiesFailed(string error)
    {
        if (!isLocalSearchRequest) return;
        isLocalSearchRequest = false;

        isQuickJoining = false;
        SetInteractableAll(true);
        ShowError("방 목록을 불러오지 못했습니다: " + error);
    }

    public void OnSearchInputChanged(string input) { ApplyFiltersAndRefresh(); }

    public void OnClick_PrevFilterChapter()
    {
        selectedFilterChapter--;
        if (selectedFilterChapter < 0) selectedFilterChapter = 6;
        UpdateFilterChapterUI();
        ApplyFiltersAndRefresh();
    }

    public void OnClick_NextFilterChapter()
    {
        selectedFilterChapter++;
        if (selectedFilterChapter > 6) selectedFilterChapter = 0;
        UpdateFilterChapterUI();
        ApplyFiltersAndRefresh();
    }

    private void UpdateFilterChapterUI()
    {
        if (filterChapterText != null)
            filterChapterText.text = (selectedFilterChapter == 0) ? "Chapter All" : $"Chapter {selectedFilterChapter}";
    }

    private void ApplyFiltersAndRefresh()
    {
        filteredLobbies.Clear();
        string searchKey = (searchInputField != null) ? searchInputField.text.Trim().ToLower() : "";

        foreach (var lobby in allFetchedLobbies)
        {
            if (lobby == null) continue;

            string roomName = GetLobbyAttribute(lobby, "ROOM_NAME", "");
            if (!string.IsNullOrEmpty(searchKey) && !roomName.ToLower().Contains(searchKey))
                continue;

            if (selectedFilterChapter > 0)
            {
                string chapterStr = GetLobbyAttribute(lobby, "CHAPTER", "1");
                if (int.TryParse(chapterStr, out int ch) && ch != selectedFilterChapter)
                    continue;
            }

            filteredLobbies.Add(lobby);
        }

        currentPage = 0;
        RefreshUI();
    }

    private void RefreshUI()
    {
        if (roomListContainer != null)
        {
            foreach (Transform child in roomListContainer) Destroy(child.gameObject);
        }

        if (filteredLobbies.Count == 0)
        {
            if (publicRoomNoRoomText != null) publicRoomNoRoomText.SetActive(true);
            if (pageText != null) pageText.text = "0 / 0";
            if (prevPageButton != null) prevPageButton.interactable = false;
            if (nextPageButton != null) nextPageButton.interactable = false;
            return;
        }

        if (publicRoomNoRoomText != null) publicRoomNoRoomText.SetActive(false);

        int maxPage = Mathf.CeilToInt((float)filteredLobbies.Count / itemsPerPage);
        currentPage = Mathf.Clamp(currentPage, 0, maxPage - 1);

        int startIndex = currentPage * itemsPerPage;
        int endIndex = Mathf.Min(startIndex + itemsPerPage, filteredLobbies.Count);

        for (int i = startIndex; i < endIndex; i++)
        {
            var lobby = filteredLobbies[i];
            if (roomListContainer != null && roomItemPrefab != null)
            {
                GameObject itemObj = Instantiate(roomItemPrefab, roomListContainer);
                ClientRoomItemUI itemUI = itemObj.GetComponent<ClientRoomItemUI>();
                if (itemUI != null) itemUI.Setup(lobby, JoinRoom);
            }
        }

        if (pageText != null) pageText.text = $"{currentPage + 1} / {maxPage}";
        if (prevPageButton != null) prevPageButton.interactable = (currentPage > 0);
        if (nextPageButton != null) nextPageButton.interactable = (currentPage < maxPage - 1);
    }

    public void OnClick_PrevPage()
    {
        if (currentPage > 0) { currentPage--; RefreshUI(); }
    }

    public void OnClick_NextPage()
    {
        int maxPage = Mathf.CeilToInt((float)filteredLobbies.Count / itemsPerPage);
        if (currentPage < maxPage - 1) { currentPage++; RefreshUI(); }
    }

    #endregion

    #region Join Room Logic

    public void JoinRoom(LobbyDetails lobby)
    {
        SubscribeEvents();
        if (lobby == null) return;

        // 방이 꽉 찼는지 검사[cite: 2]
        if (!EOSLobby.IsLobbyJoinable(lobby, out uint currentMembers, out uint maxMembers))
        {
            ShowFullRoomWarning();
            return;
        }

        var eos = GetEOSLobby();
        if (eos == null)
        {
            ShowError("네트워크 시스템을 찾을 수 없습니다.");
            return;
        }

        SetInteractableAll(false);
        eos.JoinLobby(lobby);
    }

    private void ShowFullRoomWarning()
    {
        if (fullRoomMessageText != null)
        {
            if (fullRoomMessageCoroutine != null) StopCoroutine(fullRoomMessageCoroutine);
            fullRoomMessageCoroutine = StartCoroutine(ShowFullRoomMessageRoutine());
        }
    }

    private IEnumerator ShowFullRoomMessageRoutine()
    {
        fullRoomMessageText.text = "인원수가 다 차서 못들어 갑니다";
        fullRoomMessageText.gameObject.SetActive(true);
        yield return new WaitForSeconds(3f);
        fullRoomMessageText.gameObject.SetActive(false);
        fullRoomMessageCoroutine = null;
    }

    private void OnJoinLobbySucceeded(List<Epic.OnlineServices.Lobby.Attribute> attributes)
    {
        var eos = GetEOSLobby();
        if (eos != null && eos.ConnectedLobbyDetails != null)
        {
            Epic.OnlineServices.Lobby.Attribute attr;
            if (eos.ConnectedLobbyDetails.CopyAttributeByKey(new LobbyDetailsCopyAttributeByKeyOptions { AttrKey = EOSLobby.hostAddressKey }, out attr) == Result.Success)
            {
                string hostAddress = attr.Data?.Value.AsUtf8 ?? "";
                if (!string.IsNullOrEmpty(hostAddress))
                {
                    EosTransport transport = NetworkManager.singleton.transport as EosTransport;
                    if (transport != null) transport.ResetIgnoreMessagesAtStartUpTimer();

                    HideAllPanels();

                    GameObject panel = GetLoadingPanel();
                    if (panel != null) panel.SetActive(true);

                    NetworkManager.singleton.networkAddress = hostAddress;
                    NetworkManager.singleton.StartClient();

                    if (connectionTimeoutCoroutine != null)
                        StopCoroutine(connectionTimeoutCoroutine);

                    connectionTimeoutCoroutine = StartCoroutine(CheckConnectionTimeout());

                    return;
                }
            }
        }

        SetInteractableAll(true);
        ShowError("방장의 주소 정보를 가져오지 못했습니다.");
    }

    private void OnJoinLobbyFailed(string error)
    {
        SetInteractableAll(true);

        GameObject panel = GetLoadingPanel();
        if (panel != null) panel.SetActive(false);

        ShowError("방 입장에 실패했습니다: " + error);
    }

    #endregion

    private void HideAllPanels()
    {
        if (mainPanel != null) mainPanel.SetActive(false);
        if (clientSelectionPanel != null) clientSelectionPanel.SetActive(false);
        if (clientPublicPanel != null) clientPublicPanel.SetActive(false);
        if (clientPrivatePanel != null) clientPrivatePanel.SetActive(false);
        if (logo != null) logo.SetActive(true);
    }

    private string GetLobbyAttribute(LobbyDetails lobby, string key, string defaultValue)
    {
        Epic.OnlineServices.Lobby.Attribute attr;
        if (lobby.CopyAttributeByKey(new LobbyDetailsCopyAttributeByKeyOptions { AttrKey = key }, out attr) == Result.Success)
        {
            return attr.Data?.Value.AsUtf8 ?? defaultValue;
        }
        return defaultValue;
    }

    private void SetInteractableAll(bool interactable)
    {
        if (selectPublicModeButton != null) selectPublicModeButton.interactable = interactable;
        if (selectPrivateModeButton != null) selectPrivateModeButton.interactable = interactable;
        if (quickJoinSelectionButton != null) quickJoinSelectionButton.interactable = interactable;
        if (researchButton != null) researchButton.interactable = interactable;
        if (prevPageButton != null) prevPageButton.interactable = interactable;
        if (nextPageButton != null) nextPageButton.interactable = interactable;
        if (prevFilterChapterButton != null) prevFilterChapterButton.interactable = interactable;
        if (nextFilterChapterButton != null) nextFilterChapterButton.interactable = interactable;
    }

    private void SelectPrivateRoomUp()
    {
        if (EventSystem.current == null)
            return;

        GameObject current = EventSystem.current.currentSelectedGameObject;

        if (current == null)
            return;


        Selectable selectable = current.GetComponent<Selectable>();

        if (selectable == null)
            return;


        Navigation nav = selectable.navigation;


        if (nav.mode == Navigation.Mode.Explicit && nav.selectOnUp != null)
        {
            EventSystem.current.SetSelectedGameObject(nav.selectOnUp.gameObject);
            return;
        }


        Selectable previous = selectable.FindSelectableOnUp();

        if (previous != null)
        {
            EventSystem.current.SetSelectedGameObject(previous.gameObject);
        }
    }


    private void SelectPrivateRoomDown()
    {
        if (EventSystem.current == null)
            return;


        GameObject current = EventSystem.current.currentSelectedGameObject;

        if (current == null)
            return;


        Selectable selectable = current.GetComponent<Selectable>();

        if (selectable == null)
            return;


        Navigation nav = selectable.navigation;


        if (nav.mode == Navigation.Mode.Explicit && nav.selectOnDown != null)
        {
            EventSystem.current.SetSelectedGameObject(nav.selectOnDown.gameObject);
            return;
        }


        Selectable next = selectable.FindSelectableOnDown();

        if (next != null)
        {
            EventSystem.current.SetSelectedGameObject(next.gameObject);
        }
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

    private void ShowError(string msg)
    {
        if (errorPopupPanel != null)
        {
            if (errorMessageText != null) errorMessageText.text = msg;
            errorPopupPanel.SetActive(true);

            if (EventSystem.current != null)
            {
                EventSystem.current.SetSelectedGameObject(null);

                if (errorCloseButton != null)
                {
                    EventSystem.current.SetSelectedGameObject(errorCloseButton.gameObject);
                }
                else
                {
                    EventSystem.current.SetSelectedGameObject(errorPopupPanel);
                }
            }
        }
    }

    public void OnClick_CloseErrorPopup()
    {
        if (errorPopupPanel != null) errorPopupPanel.SetActive(false);
        RestoreInputScopeAfterPopup();
    }

    // 타임아웃 패널은 에디터에 세팅된 텍스트를 그대로 사용하도록 변경함
    private void ShowTimeoutPopup()
    {
        if (timeoutPopupPanel != null)
        {
            timeoutPopupPanel.SetActive(true);

            if (EventSystem.current != null)
            {
                EventSystem.current.SetSelectedGameObject(null);
                if (timeoutCloseButton != null)
                {
                    EventSystem.current.SetSelectedGameObject(timeoutCloseButton.gameObject);
                }
                else
                {
                    EventSystem.current.SetSelectedGameObject(timeoutPopupPanel);
                }
            }
        }
    }

    public void OnClick_CloseTimeoutPopup()
    {
        if (timeoutPopupPanel != null) timeoutPopupPanel.SetActive(false);
        RestoreInputScopeAfterPopup();
    }

    // 디스커넥트 패널은 에디터에 세팅된 텍스트를 그대로 사용하도록 변경함
    private void ShowDisconnectPopup()
    {
        if (disconnectPopupPanel != null)
        {
            disconnectPopupPanel.SetActive(true);

            if (EventSystem.current != null)
            {
                EventSystem.current.SetSelectedGameObject(null);
                if (disconnectCloseButton != null)
                {
                    EventSystem.current.SetSelectedGameObject(disconnectCloseButton.gameObject);
                }
                else
                {
                    EventSystem.current.SetSelectedGameObject(disconnectPopupPanel);
                }
            }
        }
    }

    public void OnClick_CloseDisconnectPopup()
    {
        if (disconnectPopupPanel != null) disconnectPopupPanel.SetActive(false);
        RestoreInputScopeAfterPopup();
    }

    private void RestoreInputScopeAfterPopup()
    {
        if (GlobalSceneInputManager.Instance != null)
        {
            if (clientPublicPanel != null && clientPublicPanel.activeSelf)
            {
                GlobalSceneInputManager.Instance.SetFocusScope(clientPublicPanel);
            }
            else if (clientPrivatePanel != null && clientPrivatePanel.activeSelf)
            {
                GlobalSceneInputManager.Instance.SetFocusScope(clientPrivatePanel);
            }
            else if (clientSelectionPanel != null && clientSelectionPanel.activeSelf)
            {
                GlobalSceneInputManager.Instance.SetFocusScope(clientSelectionPanel);
            }
            else if (mainPanel != null && mainPanel.activeSelf)
            {
                GlobalSceneInputManager.Instance.SetFocusScope(mainPanel);
            }
        }
    }

    public void OnClick_QuickJoin()
    {
        SubscribeEvents();

        var lobby = GetEOSLobby();
        if (lobby == null)
        {
            ShowError("네트워크 시스템이 준비되지 않았습니다.");
            return;
        }

        SetInteractableAll(false);

        LobbySearchSetParameterOptions[] searchOptions = new LobbySearchSetParameterOptions[]
        {
            new LobbySearchSetParameterOptions
            {
                ComparisonOp = ComparisonOp.Equal,
                Parameter = new AttributeData { Key = "IS_PUBLIC", Value = "1" }
            }
        };

        isQuickJoining = true;
        isLocalSearchRequest = true;
        lobby.FindLobbies(50, searchOptions);
    }

    private IEnumerator CheckConnectionTimeout()
    {
        float timeoutDuration = 10f;
        float timer = 0f;

        // 1단계: 초기 연결 대기 (최대 10초) - 로딩 중 타임아웃
        while (!NetworkClient.isConnected)
        {
            if (!NetworkClient.active)
            {
                break;
            }

            timer += Time.unscaledDeltaTime;
            if (timer >= timeoutDuration)
            {
                Debug.LogWarning("[ClientLobbyManager] 초기 P2P 연결 시간 초과 (로딩 중)");
                HandleInitialConnectionTimeout();
                yield break;
            }
            yield return null;
        }

        // 만약 연결에 실패한 상태로 빠져나왔다면
        if (!NetworkClient.isConnected)
        {
            HandleInitialConnectionTimeout();
            yield break;
        }

        // 2단계: 연결 성공 후 접속 유지 상태 모니터링
        while (NetworkClient.isConnected)
        {
            yield return null;
        }

        // 3단계: 게임 도중 호스트가 방을 폭파하거나 연결이 끊긴 경우 (호스트 디스커넥트)
        Debug.LogWarning("[ClientLobbyManager] 호스트와의 연결이 끊어짐 (방 폭파 / 호스트 디스커넥트 감지)");
        HandleHostDisconnected();
    }

    private void HandleInitialConnectionTimeout()
    {
        Debug.LogWarning("[ClientLobbyManager] 초기 접속 실패, EOS 로비에서 퇴장합니다.");

        // ★ EOS 로비 퇴장 로직 추가
        var eos = GetEOSLobby();
        if (eos != null && eos.ConnectedToLobby)
        {
            eos.LeaveLobby();
        }

        if (NetworkManager.singleton != null)
        {
            NetworkManager.singleton.StopClient();
        }

        GameObject panel = GetLoadingPanel();
        if (panel != null) panel.SetActive(false);

        if (clientPublicPanel != null) clientPublicPanel.SetActive(false);
        if (clientPrivatePanel != null) clientPrivatePanel.SetActive(false);
        if (clientSelectionPanel != null)
        {
            clientSelectionPanel.SetActive(true);
            if (EventSystem.current != null)
            {
                EventSystem.current.SetSelectedGameObject(null);
                if (selectPublicModeButton != null)
                    EventSystem.current.SetSelectedGameObject(selectPublicModeButton.gameObject);
            }
        }
        if (logo != null) logo.SetActive(true);

        SetInteractableAll(true);

        // 타임아웃 전용 패널 호출 (기본 TMP 텍스트 사용)
        ShowTimeoutPopup();
    }

    private void HandleHostDisconnected()
    {
        if (NetworkManager.singleton != null)
        {
            NetworkManager.singleton.StopClient();
        }

        GameObject panel = GetLoadingPanel();
        if (panel != null) panel.SetActive(false);

        if (clientPublicPanel != null) clientPublicPanel.SetActive(false);
        if (clientPrivatePanel != null) clientPrivatePanel.SetActive(false);
        if (clientSelectionPanel != null)
        {
            clientSelectionPanel.SetActive(true);
            if (EventSystem.current != null)
            {
                EventSystem.current.SetSelectedGameObject(null);
                if (selectPublicModeButton != null)
                    EventSystem.current.SetSelectedGameObject(selectPublicModeButton.gameObject);
            }
        }
        if (logo != null) logo.SetActive(true);

        SetInteractableAll(true);

        // 디스커넥트 전용 패널 호출 (기본 TMP 텍스트 사용)
        ShowDisconnectPopup();
    }
}