using UnityEngine;
using EpicTransport;
using Mirror;
using System.Collections;
using UnityEngine.UI;
using TMPro;
using UnityEngine.SceneManagement;
using UnityEngine.InputSystem;

public class PrivateLobbyManager : MonoBehaviour
{
    [Header("UI 패널 연결")]
    [SerializeField] private GameObject mainPanel;             // 메인 화면 패널
    [SerializeField] private GameObject hostSelectionPanel;    // [Public / Private] 선택 패널
    [SerializeField] private GameObject publicSettingsPanel;   // Public 방 설정 패널 (방이름, 챕터선택)

    [Header("메인 & 패널 버튼 연결")]
    [SerializeField] private Button mainHostButton;            // 메인 화면의 [HOST] 버튼
    [SerializeField] private Button selectPublicModeButton;    // 패널 내 [Public 방 생성] 선택 버튼
    [SerializeField] private Button selectPrivateModeButton;   // 패널 내 [Private 방 생성] 선택 버튼
    [SerializeField] private Button createPublicLobbyButton;   // Public 방 설정 완료 후 [방 생성] 실행 버튼

    [Header("UI 연결 - 퍼블릭 방 설정")]
    [SerializeField] private TMP_InputField roomNameInputField;  // 방 이름 입력창
    [SerializeField] private TextMeshProUGUI chapterDisplayText; // 선택된 챕터 표시 텍스트
    [SerializeField] private Button prevChapterButton;           // < (이전 챕터) 버튼
    [SerializeField] private Button nextChapterButton;           // > (다음 챕터) 버튼

    [Header("로딩 UI 연결 (비워두어도 자동 탐색됩니다)")]
    [SerializeField] private GameObject loadingPanel;

    [Header("에러 팝업 UI 연결")]
    [SerializeField] private GameObject errorPopupPanel;
    [SerializeField] private TextMeshProUGUI errorMessageText;
    [SerializeField] private Button errorConfirmButton;

    [Header("씬 이동 설정")]
    [SerializeField] private string mainSceneName = "MainScene";

    // 전역 상태 변수
    public static string currentShortCode = "";
    public static string currentRoomName = "";
    public static int currentChapter = 1;
    public static bool isCurrentLobbyPublic = false;

    // 내부 챕터 제어 변수
    private int selectedChapterIndex = 1;
    private const int MAX_CHAPTER = 6;

    private void Start()
    {
        SetAllButtonsInteractable(false);

        // 패널 초기화 (메인 패널만 켜고 선택 패널들은 끔)
        if (hostSelectionPanel != null) hostSelectionPanel.SetActive(false);
        if (publicSettingsPanel != null) publicSettingsPanel.SetActive(false);

        // 로딩 패널 자동 확보 및 비활성화
        GameObject panel = GetLoadingPanel();
        if (panel != null) panel.SetActive(false);

        // 에러 팝업 초기화
        if (errorPopupPanel != null) errorPopupPanel.SetActive(false);
        if (errorConfirmButton != null)
        {
            errorConfirmButton.onClick.AddListener(OnClick_ReturnToMain);
        }

        // 패널 내 버튼 리스너 자동 연결
        if (mainHostButton != null) mainHostButton.onClick.AddListener(OnClick_OpenHostSelectionPanel);
        if (selectPublicModeButton != null) selectPublicModeButton.onClick.AddListener(OnClick_SelectPublicMode);
        if (selectPrivateModeButton != null) selectPrivateModeButton.onClick.AddListener(OnClick_SelectPrivateMode);
        if (createPublicLobbyButton != null) createPublicLobbyButton.onClick.AddListener(OnStartPublicHostClicked);

        // 챕터 좌우 버튼 리스너 연결
        if (prevChapterButton != null) prevChapterButton.onClick.AddListener(OnClick_PrevChapter);
        if (nextChapterButton != null) nextChapterButton.onClick.AddListener(OnClick_NextChapter);
        UpdateChapterUI();

        StartCoroutine(WaitForEpicLoginRoutine());
    }

    private void Update()
    {
        if (Keyboard.current == null) return;

        // 1. 에러 팝업 엔터키 처리
        if (errorPopupPanel != null && errorPopupPanel.activeSelf)
        {
            if (Keyboard.current.enterKey.wasPressedThisFrame || Keyboard.current.numpadEnterKey.wasPressedThisFrame)
            {
                OnClick_ReturnToMain();
            }
            return;
        }

        // 2. 키보드 방향키/AD 조작으로 챕터 넘기기
        // 조건: 방 이름 입력창 입력 중이 아닐 때 && 퍼블릭 설정 패널이 켜져 있을 때
        bool isTyping = roomNameInputField != null && roomNameInputField.isFocused;
        bool isPublicPanelActive = publicSettingsPanel != null && publicSettingsPanel.activeSelf;

        if (!isTyping && isPublicPanelActive)
        {
            if (Keyboard.current.leftArrowKey.wasPressedThisFrame || Keyboard.current.aKey.wasPressedThisFrame)
            {
                OnClick_PrevChapter();
            }
            else if (Keyboard.current.rightArrowKey.wasPressedThisFrame || Keyboard.current.dKey.wasPressedThisFrame)
            {
                OnClick_NextChapter();
            }
        }
    }

    #region --- UI 패널 전환 로직 ---

    /// <summary>
    /// 메인 화면에서 [HOST] 버튼 클릭 시 호출
    /// </summary>
    public void OnClick_OpenHostSelectionPanel()
    {
        if (hostSelectionPanel != null) hostSelectionPanel.SetActive(true);
        if (publicSettingsPanel != null) publicSettingsPanel.SetActive(false);
    }

    /// <summary>
    /// 선택 패널에서 [Public] 버튼 클릭 시 호출
    /// </summary>
    public void OnClick_SelectPublicMode()
    {
        if (hostSelectionPanel != null) hostSelectionPanel.SetActive(false);
        if (publicSettingsPanel != null) publicSettingsPanel.SetActive(true);
    }

    /// <summary>
    /// 선택 패널에서 [Private] 버튼 클릭 시 호출 -> 바로 프라이빗 방 생성
    /// </summary>
    public void OnClick_SelectPrivateMode()
    {
        OnStartPrivateHostClicked();
    }

    /// <summary>
    /// 뒤로가기 버튼 클릭 시 호출 (패널 닫기)
    /// </summary>
    public void OnClick_CloseHostPanels()
    {
        if (hostSelectionPanel != null) hostSelectionPanel.SetActive(false);
        if (publicSettingsPanel != null) publicSettingsPanel.SetActive(false);
    }

    #endregion

    #region --- 챕터 선택 (좌우 넘기기) 로직 ---

    public void OnClick_PrevChapter()
    {
        if (selectedChapterIndex > 1)
        {
            selectedChapterIndex--;
            UpdateChapterUI();
        }
    }

    public void OnClick_NextChapter()
    {
        if (selectedChapterIndex < MAX_CHAPTER)
        {
            selectedChapterIndex++;
            UpdateChapterUI();
        }
    }

    private void UpdateChapterUI()
    {
        if (chapterDisplayText != null)
        {
            chapterDisplayText.text = $"Chapter {selectedChapterIndex}";
        }

        if (prevChapterButton != null) prevChapterButton.interactable = (selectedChapterIndex > 1);
        if (nextChapterButton != null) nextChapterButton.interactable = (selectedChapterIndex < MAX_CHAPTER);
    }

    #endregion

    private void SetAllButtonsInteractable(bool interactable)
    {
        if (mainHostButton != null) mainHostButton.interactable = interactable;
        if (selectPublicModeButton != null) selectPublicModeButton.interactable = interactable;
        if (selectPrivateModeButton != null) selectPrivateModeButton.interactable = interactable;
        if (createPublicLobbyButton != null) createPublicLobbyButton.interactable = interactable;
    }

    private GameObject GetLoadingPanel()
    {
        if (loadingPanel == null)
        {
            WalkingLoadingPanel[] allPanels = Resources.FindObjectsOfTypeAll<WalkingLoadingPanel>();

            foreach (var panel in allPanels)
            {
                if (panel != null && panel.gameObject.scene.IsValid())
                {
                    loadingPanel = panel.gameObject;
                    break;
                }
            }
        }
        return loadingPanel;
    }

    private IEnumerator WaitForEpicLoginRoutine()
    {
        float timeout = 5f;
        bool isLoggedIn = false;

        while (!isLoggedIn && timeout > 0f)
        {
            try
            {
                if (!string.IsNullOrEmpty(EOSSDKComponent.LocalUserProductIdString))
                {
                    isLoggedIn = true;
                }
            }
            catch { }

            timeout -= Time.deltaTime;
            yield return null;
        }

        SetAllButtonsInteractable(true);
    }

    #region --- EOS 방 생성 트리거 ---

    public void OnStartPublicHostClicked()
    {
        string roomTitle = "즐거운 게임 방";
        if (roomNameInputField != null && !string.IsNullOrEmpty(roomNameInputField.text))
        {
            roomTitle = roomNameInputField.text.Trim();
        }

        SetAllButtonsInteractable(false);
        StartCoroutine(CleanAndCreateLobbyRoutine(isPublic: true, roomName: roomTitle, chapter: selectedChapterIndex));
    }

    public void OnStartPrivateHostClicked()
    {
        SetAllButtonsInteractable(false);
        StartCoroutine(CleanAndCreateLobbyRoutine(isPublic: false, roomName: "", chapter: 1));
    }

    #endregion

    private IEnumerator CleanAndCreateLobbyRoutine(bool isPublic, string roomName, int chapter)
    {
        isCurrentLobbyPublic = isPublic;

        GameObject currentPanel = GetLoadingPanel();
        if (currentPanel != null)
        {
            currentPanel.SetActive(true);
            var panelScript = currentPanel.GetComponent<WalkingLoadingPanel>();
            if (panelScript != null) panelScript.SetProgress(0.05f);
        }

        if (NetworkServer.active) NetworkManager.singleton.StopHost();
        else if (NetworkClient.active) NetworkManager.singleton.StopClient();

        EOSLobby eosLobby = NetworkManager.singleton.GetComponent<EOSLobby>();

        if (eosLobby == null)
        {
            SetAllButtonsInteractable(true);
            if (currentPanel != null) currentPanel.SetActive(false);
            ShowErrorPopup("네트워크 시스템 오류가 발생했습니다.");
            yield break;
        }

        if (eosLobby.ConnectedToLobby)
        {
            eosLobby.LeaveLobby();
            float leaveTimeout = 5f;
            while (eosLobby.ConnectedToLobby && leaveTimeout > 0f)
            {
                leaveTimeout -= Time.unscaledDeltaTime;
                yield return null;
            }
        }

        yield return new WaitForSecondsRealtime(0.5f);

        uint maxPlayers = 4;
        Epic.OnlineServices.Lobby.LobbyPermissionLevel permissionLevel = Epic.OnlineServices.Lobby.LobbyPermissionLevel.Publicadvertised;
        eosLobby.CreateLobby(maxPlayers, permissionLevel, true);

        float timeout = 5f;
        while (string.IsNullOrEmpty(eosLobby.GetCurrentLobbyId()) && timeout > 0f)
        {
            timeout -= Time.unscaledDeltaTime;
            yield return null;
        }

        if (timeout <= 0f)
        {
            SetAllButtonsInteractable(true);
            if (currentPanel != null) currentPanel.SetActive(false);
            ShowErrorPopup("로비 생성 시간이 초과되었습니다.");
            yield break;
        }

        // EOS Attribute 등록
        if (isPublic)
        {
            currentRoomName = roomName;
            currentChapter = chapter;
            currentShortCode = "";

            eosLobby.UpdateLobbyAttribute("ROOM_TYPE", "PUBLIC");
            eosLobby.UpdateLobbyAttribute("ROOM_NAME", currentRoomName);
            eosLobby.UpdateLobbyAttribute("CHAPTER", currentChapter.ToString());
        }
        else
        {
            currentShortCode = GenerateShortCode();
            currentRoomName = "";
            currentChapter = 1;

            eosLobby.UpdateLobbyAttribute("ROOM_TYPE", "PRIVATE");
            eosLobby.UpdateLobbyAttribute("SHORTCODE", currentShortCode);
        }

        yield return new WaitForSecondsRealtime(0.3f);

        EosTransport transport = NetworkManager.singleton.transport as EosTransport;
        if (transport != null) transport.ResetIgnoreMessagesAtStartUpTimer();

        NetworkManager.singleton.StartHost();

        yield return new WaitForSecondsRealtime(0.2f);

        SetAllButtonsInteractable(true);
        if (currentPanel != null) currentPanel.SetActive(false);
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