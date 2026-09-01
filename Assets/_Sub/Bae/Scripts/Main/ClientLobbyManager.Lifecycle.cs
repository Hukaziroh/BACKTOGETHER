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

public partial class ClientLobbyManager
{
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
        CancelPublicListRefreshFocusLock();
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

        escPressed = VirtualKeyboardManager.WasUICancelPressedThisFrame();

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

        if (VirtualKeyboardManager.BlocksGlobalBackInput)
        {
            escPressed = false;
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

    /// <summary>
    /// 매 프레임 최후순위에 실행되어 팝업창 외부로 UI 포커스가 이탈하거나 해제되는 것을 방지합니다.
    /// </summary>
    private void LateUpdate()
    {
        if (isPublicListRefreshFocusLocked &&
            (clientPublicPanel == null || !clientPublicPanel.activeInHierarchy))
        {
            CancelPublicListRefreshFocusLock();
        }

        if (EventSystem.current == null) return;

        if (isPublicListRefreshFocusLocked)
        {
            EnforcePublicListRefreshFocusLock();
            return;
        }

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

}
