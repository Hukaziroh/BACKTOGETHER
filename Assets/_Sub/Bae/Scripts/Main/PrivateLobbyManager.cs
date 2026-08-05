using System.Collections;
using System.Collections.Generic;
using Epic.OnlineServices.Lobby;
using EpicTransport;
using Mirror;
using TMPro;
using UnityEngine;
using UnityEngine.EventSystems;
using UnityEngine.InputSystem; // 신형 입력 시스템
using UnityEngine.SceneManagement;
using UnityEngine.UI;

public class PrivateLobbyManager : MonoBehaviour
{
    [Header("UI 패널 연결")]
    [SerializeField] private GameObject mainPanel;
    [SerializeField] private GameObject hostPanel;
    [SerializeField] private GameObject logo;

    [Header("메인 & 실행 버튼 연결")]
    [SerializeField] private Button mainHostButton;
    [SerializeField] private Button makeRoomButton;

    [Header("UI 연결 - 방 설정 (패널 내부)")]
    [SerializeField] private TMP_InputField roomNameInputField;

    [Header("챕터 선택 UI (< Chapter 1 >)")]
    [SerializeField] private GameObject chapterSelectObject;
    [SerializeField] private TextMeshProUGUI chapterDisplayText;
    [SerializeField] private Button prevChapterButton;
    [SerializeField] private Button nextChapterButton;
    [SerializeField] private Image chapterPreviewImage;      // 챕터 이미지 UI Component
    [SerializeField] private Sprite[] chapterSprites;        // 챕터별 Sprite 이미지 배열

    [Header("챕터 잠금 UI")]
    [SerializeField] private GameObject chapterLockObject;

    [Header("방 타입 선택 UI (< Public / Private >)")]
    [SerializeField] private GameObject roomTypeSelectObject;
    [SerializeField] private TextMeshProUGUI roomTypeDisplayText;
    [SerializeField] private Button prevRoomTypeButton;
    [SerializeField] private Button nextRoomTypeButton;

    [Header("로딩 UI 연결 (비워두어도 자동 탐색됩니다)")]
    [SerializeField] private GameObject loadingPanel;

    [Header("에러 팝업 UI 연결")]
    [SerializeField] private GameObject errorPopupPanel;
    [SerializeField] private TextMeshProUGUI errorMessageText;
    [SerializeField] private Button errorCloseButton;

    [Header("씬 설정")]
    [SerializeField] private string lobbySceneName = "Lobby";
    [SerializeField] private string mainSceneName = "Main";

    private EOSLobby eosLobby;
    private bool isSubscribed = false;

    public static string currentShortCode = "";
    public static int selectedChapter = 1;
    private int selectedChapterIndex = 1;
    private int maxChapterCount = 6;
    private bool isPublicRoom = true;

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

        if (mainPanel != null)
        {
            mainPanel.SetActive(true);
            if (GlobalSceneInputManager.Instance != null)
            {
                GlobalSceneInputManager.Instance.SetFocusScope(mainPanel);
            }
        }

        // 인풋 필드 작성 완료(엔터/포커스 해제) 시 다시 interactable = false 처리
        if (roomNameInputField != null)
        {
            roomNameInputField.onEndEdit.AddListener(OnRoomNameEndEdit);
        }

        UpdateChapterUI();
        UpdateRoomTypeUI();
    }

    private void OnDisable()
    {
        UnsubscribeEvents();
        if (roomNameInputField != null)
        {
            roomNameInputField.onEndEdit.RemoveListener(OnRoomNameEndEdit);
        }
    }

    private void Update()
    {
        // 🌟 ESC 키 입력 처리 (최우선 순위: 에러 팝업 -> 호스트 패널)
        if (Keyboard.current != null && Keyboard.current.escapeKey.wasPressedThisFrame)
        {
            // 1. 에러 팝업이 켜져있다면 에러 팝업 닫기
            if (errorPopupPanel != null && errorPopupPanel.activeSelf)
            {
                errorPopupPanel.SetActive(false);
                return;
            }

            // 2. 호스트 패널이 켜져있다면 메인 패널로 돌아가기
            if (hostPanel != null && hostPanel.activeSelf)
            {
                OnClick_ReturnToMain();
                return;
            }
        }

        if (EventSystem.current == null) return;

        GameObject selected = EventSystem.current.currentSelectedGameObject;
        if (selected == null) return;

        // 1. Chapter Select 오브젝트가 선택되어 있는 경우
        bool isChapterSelected = (chapterSelectObject != null && selected == chapterSelectObject) ||
                                 (chapterDisplayText != null && (selected == chapterDisplayText.gameObject || selected == chapterDisplayText.transform.parent.gameObject)) ||
                                 (prevChapterButton != null && selected == prevChapterButton.gameObject) ||
                                 (nextChapterButton != null && selected == nextChapterButton.gameObject);

        if (isChapterSelected)
        {
            if (Keyboard.current != null)
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
            return;
        }

        // 2. Room Type Select 오브젝트가 선택되어 있는 경우
        bool isRoomTypeSelected = (roomTypeSelectObject != null && selected == roomTypeSelectObject) ||
                                  (roomTypeDisplayText != null && (selected == roomTypeDisplayText.gameObject || selected == roomTypeDisplayText.transform.parent.gameObject)) ||
                                  (prevRoomTypeButton != null && selected == prevRoomTypeButton.gameObject) ||
                                  (nextRoomTypeButton != null && selected == nextRoomTypeButton.gameObject);

        if (isRoomTypeSelected)
        {
            if (Keyboard.current != null)
            {
                if (Keyboard.current.leftArrowKey.wasPressedThisFrame || Keyboard.current.aKey.wasPressedThisFrame)
                {
                    OnClick_PrevRoomType();
                }
                else if (Keyboard.current.rightArrowKey.wasPressedThisFrame || Keyboard.current.dKey.wasPressedThisFrame)
                {
                    OnClick_NextRoomType();
                }
            }
            return;
        }

        // ★ 3. 퍼블릭 방 모드일 때 인풋 필드 선택 후 엔터 키 입력 시 활성화
        if (isPublicRoom && roomNameInputField != null)
        {
            bool isInputFieldSelected = (selected == roomNameInputField.gameObject) ||
                                        (selected == roomNameInputField.transform.parent?.gameObject);

            if (isInputFieldSelected && Keyboard.current != null)
            {
                if (Keyboard.current.enterKey.wasPressedThisFrame || Keyboard.current.numpadEnterKey.wasPressedThisFrame)
                {
                    if (!roomNameInputField.interactable)
                    {
                        roomNameInputField.interactable = true;
                        roomNameInputField.ActivateInputField();
                        roomNameInputField.Select();
                    }
                }
            }
        }
    }

    private void OnRoomNameEndEdit(string text)
    {
        // 텍스트 입력 완료 후 비활성화 상태로 복귀
        if (roomNameInputField != null)
        {
            roomNameInputField.interactable = false;
        }
    }

    public void OnClick_MainHost()
    {
        SubscribeEvents();

        if (mainPanel != null) mainPanel.SetActive(false);
        if (logo != null) logo.SetActive(false);
        if (hostPanel != null) hostPanel.SetActive(true);

        if (GlobalSceneInputManager.Instance != null)
        {
            GlobalSceneInputManager.Instance.SetFocusScope(hostPanel);
        }
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
        int displayChapter = selectedChapterIndex;
        int arrayIndex = selectedChapterIndex - 1; // 배열 및 세이브 데이터 비교용 (0~5)

        if (chapterDisplayText != null)
        {
            chapterDisplayText.text = $"Chapter {displayChapter}";
        }

        // 챕터 이미지 배열 적용
        if (chapterPreviewImage != null && chapterSprites != null && chapterSprites.Length > arrayIndex)
        {
            chapterPreviewImage.sprite = chapterSprites[arrayIndex];
        }

        // ==========================================
        // 🌟 챕터 잠금 여부 확인 로직
        // ==========================================
        bool isUnlocked = true;

        if (GameSaveManager.Instance != null)
        {
            int maxCleared = GameSaveManager.Instance.currentData.maxClearedChapter;

            if (maxCleared < arrayIndex)
            {
                isUnlocked = false;
            }
        }

        // 1. 자물쇠 UI 켜기/끄기
        if (chapterLockObject != null)
        {
            chapterLockObject.SetActive(!isUnlocked);
        }

        // 2. 방 만들기 버튼 활성화/비활성화
        if (makeRoomButton != null)
        {
            makeRoomButton.interactable = isUnlocked;
        }

        // 3. (보너스 연출) 잠겨있을 때 챕터 이미지를 어둡게 처리
        if (chapterPreviewImage != null)
        {
            chapterPreviewImage.color = isUnlocked ? Color.white : new Color(0.3f, 0.3f, 0.3f, 1f);
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
        if (roomTypeDisplayText != null) roomTypeDisplayText.text = isPublicRoom ? "Public" : "Private";

        if (roomNameInputField != null)
        {
            roomNameInputField.interactable = false;
            roomNameInputField.DeactivateInputField();
        }
    }

    public void OnClick_MakeRoom()
    {
        SubscribeEvents();
        if (isCreatingLobby) return;

        selectedChapter = selectedChapterIndex;

        var lobby = GetEOSLobby();
        if (lobby == null)
        {
            ShowErrorPopup("네트워크 시스템이 준비되지 않았습니다.");
            return;
        }

        string roomTitle = (roomNameInputField != null && !string.IsNullOrEmpty(roomNameInputField.text))
            ? roomNameInputField.text : "Pico Room";

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

        lobby.CreateLobby(4, LobbyPermissionLevel.Publicadvertised, false, null);

        StartCoroutine(CreateLobbyAndSetAttributesRoutine(roomTitle));
    }

    private void OnCreateLobbySucceeded(List<Epic.OnlineServices.Lobby.Attribute> attributes)
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

        List<AttributeData> attrDataList = new List<AttributeData>();

        attrDataList.Add(new AttributeData { Key = "ROOM_NAME", Value = roomTitle });
        attrDataList.Add(new AttributeData { Key = "CHAPTER", Value = selectedChapterIndex.ToString() });
        attrDataList.Add(new AttributeData { Key = "IS_PUBLIC", Value = isPublicRoom ? "1" : "0" });

        if (!isPublicRoom)
        {
            attrDataList.Add(new AttributeData { Key = "SHORTCODE", Value = currentShortCode });
        }

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

            // 🌟 에러 팝업이 뜰 때 포커스를 강제로 잡아줌
            if (EventSystem.current != null)
            {
                EventSystem.current.SetSelectedGameObject(null); // 기존 포커스 초기화

                if (errorCloseButton != null)
                {
                    EventSystem.current.SetSelectedGameObject(errorCloseButton.gameObject);
                }
                else
                {
                    // 확인 버튼 변수를 따로 안 빼뒀다면 팝업 패널 자체나 내부 첫 번째 버튼을 지정
                    EventSystem.current.SetSelectedGameObject(errorPopupPanel);
                }
            }
        }
    }

    public void OnClick_ReturnToMain()
    {
        if (hostPanel != null) hostPanel.SetActive(false);
        if (logo != null) logo.SetActive(true);
        if (mainPanel != null)
        {
            mainPanel.SetActive(true);

            if (GlobalSceneInputManager.Instance != null)
            {
                GlobalSceneInputManager.Instance.SetFocusScope(mainPanel);
            }
        }

        if (errorPopupPanel != null) errorPopupPanel.SetActive(false);
    }
}