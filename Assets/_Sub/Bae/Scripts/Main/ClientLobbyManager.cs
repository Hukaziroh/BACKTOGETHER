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
using UnityEngine.SceneManagement; // 씬 초기화용 네임스페이스 추가
using UnityEngine.UI;

public class ClientLobbyManager : MonoBehaviour
{
    public bool IsConnecting { get; private set; }
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

    [Header("프라이빗 로비 매니저 연동")]
    [SerializeField] private PrivateLobbyManager privateLobbyManager; // ★ PrivateLobbyManager 연결 필드 추가

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
    private int selectedFilterChapter = 0; // 0: All, 1~N: Chapter 1~N
    private bool isQuickJoining = false;

    private Coroutine connectionTimeoutCoroutine;
    private Coroutine hideQuickJoinNoRoomCoroutine;
    private GameObject lastSelectedBeforeSearch; // ★ 리로드 직전 포커스 저장용 변수 추가


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
            searchInputField.onValueChanged.AddListener(OnSearchInputChanged);
            searchInputField.characterLimit = 15;
            searchInputField.interactable = false;
        }

        if (privateRoomInputField != null)
        {
            privateRoomInputField.onEndEdit.AddListener(OnPrivateRoomInputEndEdit);
            privateRoomInputField.characterLimit = 6;
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
            searchInputField.onValueChanged.RemoveListener(OnSearchInputChanged);
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
        bool submitPressed = false;

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

            submitPressed |= enterPressed || Keyboard.current.spaceKey.wasPressedThisFrame;
        }

        if (Gamepad.current != null)
        {
            leftPressed |= Gamepad.current.dpad.left.wasPressedThisFrame ||
                      Gamepad.current.leftStick.left.wasPressedThisFrame;

            rightPressed |= Gamepad.current.dpad.right.wasPressedThisFrame ||
                      Gamepad.current.leftStick.right.wasPressedThisFrame;

            downPressed |= Gamepad.current.dpad.down.wasPressedThisFrame ||
                      Gamepad.current.leftStick.down.wasPressedThisFrame;

            submitPressed |= Gamepad.current.buttonSouth.wasPressedThisFrame; // 패드 A / Cross 버튼
        }

        // 확인 / 제출 입력 처리 (팝업이 열려 있는 경우 엔터나 패드 A버튼으로 닫기 수행)
        if (submitPressed)
        {
            if (timeoutPopupPanel != null && timeoutPopupPanel.activeSelf)
            {
                OnClick_CloseTimeoutPopup();
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
                if (leftPressed || rightPressed)
                {
                    if (GlobalSceneInputManager.Instance != null) GlobalSceneInputManager.Instance.PlayConfirmSound();
                }

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
                if (leftPressed || rightPressed)
                {
                    if (GlobalSceneInputManager.Instance != null) GlobalSceneInputManager.Instance.PlayConfirmSound();
                }

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

    /// <summary>
    /// 매 프레임 최후순위에 실행되어 팝업창 외부로 UI 포커스가 이탈하거나 해제되는 것을 방지합니다.
    /// </summary>
    private void LateUpdate()
    {
        if (EventSystem.current == null) return;

        // 1. 타임아웃 팝업 포커스 강제 고정
        if (timeoutPopupPanel != null && timeoutPopupPanel.activeSelf)
        {
            GameObject selected = EventSystem.current.currentSelectedGameObject;
            if (selected == null || !selected.transform.IsChildOf(timeoutPopupPanel.transform))
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
            return;
        }

        // 2. 일반 에러 팝업 포커스 강제 고정
        if (errorPopupPanel != null && errorPopupPanel.activeSelf)
        {
            GameObject selected = EventSystem.current.currentSelectedGameObject;
            if (selected == null || !selected.transform.IsChildOf(errorPopupPanel.transform))
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
            return;
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
            // ★ 퍼블릭 패널이 열릴 때 첫 번째 버튼으로 튀는 현상을 방지하고 원하는 버튼(예: searchInputField 또는 첫 상호작용 요소)으로 정확히 고정
            if (EventSystem.current != null)
            {
                EventSystem.current.SetSelectedGameObject(null);
                if (searchInputField != null)
                {
                    EventSystem.current.SetSelectedGameObject(searchInputField.gameObject);
                }
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
            // ★ 프라이빗 패널이 열릴 때 첫 번째 버튼으로 튀는 현상을 방지하고 privateRoomInputField로 포커스 고정
            if (EventSystem.current != null)
            {
                EventSystem.current.SetSelectedGameObject(null);
                if (privateRoomInputField != null)
                {
                    EventSystem.current.SetSelectedGameObject(privateRoomInputField.gameObject);
                }
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

        // ★ 리로드 버튼을 누른 순간 현재 포커스(리로드 버튼)를 기억합니다.
        if (EventSystem.current != null)
        {
            lastSelectedBeforeSearch = EventSystem.current.currentSelectedGameObject;
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

        // ★ 저장해 둔 포커스 복구 (리로드 버튼으로 다시 포커스 고정)
        if (EventSystem.current != null && lastSelectedBeforeSearch != null && lastSelectedBeforeSearch.activeInHierarchy)
        {
            EventSystem.current.SetSelectedGameObject(lastSelectedBeforeSearch);
        }
        else if (researchButton != null)
        {
            EventSystem.current?.SetSelectedGameObject(researchButton.gameObject);
        }

        allFetchedLobbies = lobbies ?? new List<LobbyDetails>();

        if (isQuickJoining)
        {
            isQuickJoining = false;

            foreach (var lobby in allFetchedLobbies)
            {
                if (EOSLobby.IsLobbyJoinable(lobby, out uint currentMembers, out uint maxMembers))
                {
                    // ★ 유령방(0명)에는 퀵 조인으로 들어가지 않도록 방어
                    if (currentMembers > 0)
                    {
                        Debug.Log($"[QuickJoin] 빈 방 발견! ({currentMembers}/{maxMembers}) 즉시 입장합니다.");
                        JoinRoom(lobby);
                        return;
                    }
                }
            }

            if (quickJoinNoRoomText != null)
            {
                if (hideQuickJoinNoRoomCoroutine != null)
                    StopCoroutine(hideQuickJoinNoRoomCoroutine);

                hideQuickJoinNoRoomCoroutine = StartCoroutine(ShowAndHideQuickJoinNoRoomText());
            }

            // ★ 퀵 조인 시 빈 방이 없어 입장을 못 한 경우에도 버튼 상호작용 복구 및 포커스 정상 복원
            SetInteractableAll(true);
            if (EventSystem.current != null && lastSelectedBeforeSearch != null && lastSelectedBeforeSearch.activeInHierarchy)
            {
                EventSystem.current.SetSelectedGameObject(lastSelectedBeforeSearch);
            }
            else if (quickJoinSelectionButton != null)
            {
                EventSystem.current?.SetSelectedGameObject(quickJoinSelectionButton.gameObject);
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

        // ★ 실패 시에도 포커스 복구
        if (EventSystem.current != null && lastSelectedBeforeSearch != null && lastSelectedBeforeSearch.activeInHierarchy)
        {
            EventSystem.current.SetSelectedGameObject(lastSelectedBeforeSearch);
        }
        else if (researchButton != null)
        {
            EventSystem.current?.SetSelectedGameObject(researchButton.gameObject);
        }

        ShowError("방 목록을 불러오지 못했습니다: " + error);
    }

    public void OnSearchInputChanged(string input) { ApplyFiltersAndRefresh(); }

    // ★ PrivateLobbyManager의 chapterNames 배열을 보호 수준 에러(CS0122) 없이 안전하게 가져오는 헬퍼 메서드 (리플렉션 사용)
    private string[] GetChapterNamesFromPrivateManager()
    {
        if (privateLobbyManager == null) return null;
        try
        {
            var field = privateLobbyManager.GetType().GetField("chapterNames", System.Reflection.BindingFlags.Public | System.Reflection.BindingFlags.NonPublic | System.Reflection.BindingFlags.Instance);
            if (field != null)
            {
                return field.GetValue(privateLobbyManager) as string[];
            }
            var prop = privateLobbyManager.GetType().GetProperty("chapterNames", System.Reflection.BindingFlags.Public | System.Reflection.BindingFlags.NonPublic | System.Reflection.BindingFlags.Instance);
            if (prop != null)
            {
                return prop.GetValue(privateLobbyManager) as string[];
            }
        }
        catch (System.Exception e)
        {
            Debug.LogWarning($"[ClientLobbyManager] chapterNames 취득 실패: {e.Message}");
        }
        return null;
    }

    // PrivateLobbyManager의 챕터 개수를 동적으로 가져옴
    private int GetMaxChapterCount()
    {
        string[] names = GetChapterNamesFromPrivateManager();
        if (names != null && names.Length > 0)
        {
            return names.Length;
        }
        return 6; // 기본값
    }

    private bool IsExChapter(int chapterIndex)
    {
        if (chapterIndex <= 0) return false;
        int arrayIndex = chapterIndex - 1;
        string[] names = GetChapterNamesFromPrivateManager();
        string currentChapterName = (names != null && arrayIndex >= 0 && arrayIndex < names.Length) ? names[arrayIndex] : "";
        return currentChapterName.Contains("EX") || chapterIndex >= 7;
    }

    public void OnClick_PrevFilterChapter()
    {
        int maxCh = GetMaxChapterCount();
        int maxCleared = (GameSaveManager.Instance != null) ? GameSaveManager.Instance.currentData.maxClearedChapter : 0;

        selectedFilterChapter--;

        if (IsExChapter(selectedFilterChapter) && maxCleared < 6)
        {
            selectedFilterChapter--;
        }

        if (selectedFilterChapter < 0) selectedFilterChapter = maxCh;

        if (IsExChapter(selectedFilterChapter) && maxCleared < 6)
        {
            selectedFilterChapter = 0;
        }

        UpdateFilterChapterUI();
        ApplyFiltersAndRefresh();
    }

    public void OnClick_NextFilterChapter()
    {
        int maxCh = GetMaxChapterCount();
        int maxCleared = (GameSaveManager.Instance != null) ? GameSaveManager.Instance.currentData.maxClearedChapter : 0;

        selectedFilterChapter++;

        if (selectedFilterChapter > maxCh) selectedFilterChapter = 0;

        if (IsExChapter(selectedFilterChapter) && maxCleared < 6)
        {
            selectedFilterChapter++;
            if (selectedFilterChapter > maxCh) selectedFilterChapter = 0;
        }

        UpdateFilterChapterUI();
        ApplyFiltersAndRefresh();
    }

    private void UpdateFilterChapterUI()
    {
        if (filterChapterText != null)
        {
            if (selectedFilterChapter == 0)
            {
                filterChapterText.text = "Chapter All";
            }
            else
            {
                int index = selectedFilterChapter - 1;
                string[] names = GetChapterNamesFromPrivateManager();
                if (names != null && index >= 0 && index < names.Length && !string.IsNullOrEmpty(names[index]))
                {
                    filterChapterText.text = names[index];
                }
                else
                {
                    filterChapterText.text = $"Chapter {selectedFilterChapter}";
                }
            }
        }
    }

    private void ApplyFiltersAndRefresh()
    {
        filteredLobbies.Clear();
        string searchKey = (searchInputField != null) ? searchInputField.text.Trim().ToLower() : "";

        string[] chapterNames = GetChapterNamesFromPrivateManager();

        // 플레이어의 최대 클리어 챕터 정보 가져오기
        int maxClearedChapter = 0;
        if (GameSaveManager.Instance != null)
        {
            maxClearedChapter = GameSaveManager.Instance.currentData.maxClearedChapter;
        }

        foreach (var lobby in allFetchedLobbies)
        {
            if (lobby == null) continue;

            string roomName = GetLobbyAttribute(lobby, "ROOM_NAME", "");
            if (!string.IsNullOrEmpty(searchKey) && !roomName.ToLower().Contains(searchKey))
                continue;

            string chapterStr = GetLobbyAttribute(lobby, "CHAPTER", "");

            int chapterNum = 0;
            int.TryParse(chapterStr, out chapterNum);

            // ★ [핵심 요구사항] EX 스테이지 / 6챕 클리어 전까지 검색 결과에서 숨기기 로직
            bool isExStage = chapterStr.Contains("EX") || chapterNum >= 7;
            if (!isExStage && chapterNames != null && chapterNum > 0 && chapterNum <= chapterNames.Length)
            {
                if (chapterNames[chapterNum - 1].Contains("EX"))
                {
                    isExStage = true;
                }
            }

            if (isExStage && maxClearedChapter < 6)
            {
                continue;
            }

            // ★ 기존 챕터 필터링 로직
            if (selectedFilterChapter > 0)
            {
                int target1Based = selectedFilterChapter;
                int target0Based = selectedFilterChapter - 1;
                string targetName = (chapterNames != null && target0Based >= 0 && target0Based < chapterNames.Length) ? chapterNames[target0Based] : null;

                bool isChapterMatch = false;

                if (!string.IsNullOrEmpty(chapterStr))
                {
                    if (int.TryParse(chapterStr, out int chVal))
                    {
                        if (chVal == target1Based || chVal == target0Based)
                            isChapterMatch = true;
                    }

                    if (!isChapterMatch && !string.IsNullOrEmpty(targetName))
                    {
                        if (string.Equals(chapterStr.Trim(), targetName.Trim(), System.StringComparison.OrdinalIgnoreCase))
                            isChapterMatch = true;
                    }

                    if (!isChapterMatch && string.Equals(chapterStr.Trim(), $"Chapter{target1Based}", System.StringComparison.OrdinalIgnoreCase))
                    {
                        isChapterMatch = true;
                    }
                }

                if (!isChapterMatch)
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

        if (lobby == null)
            return;

        // 방 목록 아이템은 검색할 때마다 새로 생성되는 프리팹이라 전역 버튼 사운드 연결 타이밍을 놓치기 쉬워 여기서 직접 재생
        if (GlobalSceneInputManager.Instance != null) GlobalSceneInputManager.Instance.PlayConfirmSound();

        if (!EOSLobby.IsLobbyJoinable(
                lobby,
                out uint currentMembers,
                out uint maxMembers))
        {
            ShowFullRoomWarning();
            // ★ 방 입장 실패/거부 시 현재 선택된 버튼/요소의 포커스가 유실되거나 백그라운드로 새어나가지 않도록 현재 선택 상태 유지 또는 복구 처리
            return;
        }

        var eos = GetEOSLobby();

        if (eos == null)
        {
            ShowError("네트워크 시스템을 찾을 수 없습니다.");
            return;
        }

        HostDisconnectHandler disconnectHandler =
            FindFirstObjectByType<HostDisconnectHandler>();

        if (disconnectHandler != null)
        {
            disconnectHandler.BeginConnectionAttempt();
        }

        SetInteractableAll(false);

        Debug.Log(
            $"[ClientLobbyManager] 방 입장 시작 | " +
            $"Members={currentMembers}/{maxMembers}"
        );

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
        if (!isLocalSearchRequest) return;
        isLocalSearchRequest = false;

        isQuickJoining = false;

        GameObject panel = GetLoadingPanel();
        if (panel != null) panel.SetActive(false);

        SetInteractableAll(true);

        if (NetworkManager.singleton != null && NetworkClient.active)
        {
            NetworkManager.singleton.StopClient();
        }

        var eos = GetEOSLobby();
        if (eos != null && eos.ConnectedToLobby)
        {
            eos.LeaveLobby();
        }

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
        GameObject currentSelected = EventSystem.current != null ? EventSystem.current.currentSelectedGameObject : null;

        if (selectPublicModeButton != null && (interactable || selectPublicModeButton.gameObject != currentSelected)) selectPublicModeButton.interactable = interactable;
        if (selectPrivateModeButton != null && (interactable || selectPrivateModeButton.gameObject != currentSelected)) selectPrivateModeButton.interactable = interactable;
        if (quickJoinSelectionButton != null && (interactable || quickJoinSelectionButton.gameObject != currentSelected)) quickJoinSelectionButton.interactable = interactable;
        if (researchButton != null && (interactable || researchButton.gameObject != currentSelected)) researchButton.interactable = interactable;
        if (prevPageButton != null && (interactable || prevPageButton.gameObject != currentSelected)) prevPageButton.interactable = interactable;
        if (nextPageButton != null && (interactable || nextPageButton.gameObject != currentSelected)) nextPageButton.interactable = interactable;
        if (prevFilterChapterButton != null && (interactable || prevFilterChapterButton.gameObject != currentSelected)) prevFilterChapterButton.interactable = interactable;
        if (nextFilterChapterButton != null && (interactable || nextFilterChapterButton.gameObject != currentSelected)) nextFilterChapterButton.interactable = interactable;
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

            if (GlobalSceneInputManager.Instance != null)
            {
                GlobalSceneInputManager.Instance.SetFocusScope(errorPopupPanel);
            }

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

        if (NetworkManager.singleton != null)
        {
            NetworkManager.singleton.StopClient();
        }

        var eos = GetEOSLobby();
        if (eos != null && eos.ConnectedToLobby)
        {
            eos.LeaveLobby();
        }

        SceneManager.LoadScene(SceneManager.GetActiveScene().name);
    }

    private void ShowTimeoutPopup()
    {
        if (timeoutPopupPanel != null)
        {
            timeoutPopupPanel.SetActive(true);

            if (GlobalSceneInputManager.Instance != null)
            {
                GlobalSceneInputManager.Instance.SetFocusScope(timeoutPopupPanel);
            }

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

        var eos = GetEOSLobby();
        if (eos != null && eos.ConnectedToLobby)
        {
            eos.LeaveLobby();
        }

        if (NetworkManager.singleton != null)
        {
            NetworkManager.singleton.StopClient();
        }

        SceneManager.LoadScene(SceneManager.GetActiveScene().name);
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

        // ★ 퀵 조인 버튼을 누를 때 현재 선택되어 있던 포커스 오브젝트를 기억해 둠으로써 클릭 직후 백그라운드로 포커스가 새는 현상 차단
        if (EventSystem.current != null)
        {
            lastSelectedBeforeSearch = EventSystem.current.currentSelectedGameObject;
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
        float timer = 0f;
        float timeoutDuration = 10f;

        while (!NetworkClient.isConnected)
        {
            if (!NetworkClient.active) break;

            timer += Time.unscaledDeltaTime;
            if (timer >= timeoutDuration)
            {
                Debug.LogWarning("[ClientLobbyManager] P2P 연결 타임아웃!");
                HandleInitialConnectionTimeout();
                yield break;
            }
            yield return null;
        }

        if (!NetworkClient.isConnected)
        {
            HandleInitialConnectionTimeout();
        }
        else
        {
            // ★ 중요: 실제 Mirror 연결까지 성공했다면 Connecting 상태 해제
            // 이제부터 끊기는 건 HostDisconnectHandler가 담당함
            IsConnecting = false;
            Debug.Log("[ClientLobbyManager] Mirror 연결 성공, HostDisconnectHandler로 감시 이관");
        }
    }

    private void HandleInitialConnectionTimeout()
    {
        IsConnecting = false; // 접속 프로세스 종료

        Debug.LogWarning("[ClientLobbyManager] 초기 접속 실패, 네트워크 상태를 초기화합니다.");

        if (NetworkManager.singleton != null)
        {
            NetworkManager.singleton.StopClient();

            // P2P 소켓 찌꺼기 방지용
            EpicTransport.EosTransport transport = NetworkManager.singleton.GetComponent<EpicTransport.EosTransport>();
            if (NetworkManager.singleton != null && NetworkClient.active)
            {
                NetworkManager.singleton.StopClient();
            }

            var eos = GetEOSLobby();
            if (eos != null && eos.ConnectedToLobby)
            {
                eos.LeaveLobby();
            }

            GameObject panel = GetLoadingPanel();
            if (panel != null) panel.SetActive(false);

            SetInteractableAll(true);
            ShowTimeoutPopup();
        }
    }
}