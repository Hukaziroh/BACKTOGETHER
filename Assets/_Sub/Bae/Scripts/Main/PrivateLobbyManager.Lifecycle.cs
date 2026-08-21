using System.Collections;
using System.Collections.Generic;
using Epic.OnlineServices.Lobby;
using EpicTransport;
using Mirror;
using TMPro;
using UnityEngine;
using UnityEngine.EventSystems;
using UnityEngine.InputSystem;
using UnityEngine.SceneManagement;
using UnityEngine.UI;

public partial class PrivateLobbyManager
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
        Application.runInBackground = true;

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

        if (roomNameInputField != null)
        {
            roomNameInputField.onEndEdit.AddListener(OnRoomNameEndEdit);
            roomNameInputField.characterLimit = 15; // ★ 방 이름 입력 최대 글자 수 제한 (원하는 숫자로 변경 가능)
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
        bool leftPressed = false;
        bool rightPressed = false;
        bool enterOrActionPressed = false;
        bool escPressed = false;

        if (Keyboard.current != null)
        {
            leftPressed |= Keyboard.current.leftArrowKey.wasPressedThisFrame || Keyboard.current.aKey.wasPressedThisFrame;
            rightPressed |= Keyboard.current.rightArrowKey.wasPressedThisFrame || Keyboard.current.dKey.wasPressedThisFrame;
            enterOrActionPressed |= Keyboard.current.enterKey.wasPressedThisFrame || Keyboard.current.numpadEnterKey.wasPressedThisFrame;
            escPressed |= Keyboard.current.escapeKey.wasPressedThisFrame;
        }

        if (Gamepad.current != null)
        {
            leftPressed |= Gamepad.current.dpad.left.wasPressedThisFrame || Gamepad.current.leftStick.left.wasPressedThisFrame;
            rightPressed |= Gamepad.current.dpad.right.wasPressedThisFrame || Gamepad.current.leftStick.right.wasPressedThisFrame;
        }

        // ESC 입력 처리 (에러 팝업 -> 호스트 패널 순서로 역방향 닫기)
        if (escPressed)
        {
            if (errorPopupPanel != null && errorPopupPanel.activeSelf)
            {
                OnClick_CloseErrorPopup();
                return;
            }
            else if (hostPanel != null && hostPanel.activeSelf)
            {
                OnClick_ReturnToMain();
                return;
            }
        }

        if (EventSystem.current == null) return;
        GameObject selected = EventSystem.current.currentSelectedGameObject;
        if (selected == null) return;

        bool isChapterSelected = (chapterSelectObject != null && selected == chapterSelectObject) ||
                                 (chapterDisplayText != null && (selected == chapterDisplayText.gameObject || selected == chapterDisplayText.transform.parent.gameObject)) ||
                                 (prevChapterButton != null && selected == prevChapterButton.gameObject) ||
                                 (nextChapterButton != null && selected == nextChapterButton.gameObject);

        if (isChapterSelected)
        {
            if (leftPressed) OnClick_PrevChapter();
            else if (rightPressed) OnClick_NextChapter();
            return;
        }

        bool isRoomTypeSelected = (roomTypeSelectObject != null && selected == roomTypeSelectObject) ||
                                 (roomTypeDisplayText != null && (selected == roomTypeDisplayText.gameObject || selected == roomTypeDisplayText.transform.parent.gameObject)) ||
                                 (prevRoomTypeButton != null && selected == prevRoomTypeButton.gameObject) ||
                                 (nextRoomTypeButton != null && selected == nextRoomTypeButton.gameObject);

        if (isRoomTypeSelected)
        {
            if (leftPressed) OnClick_PrevRoomType();
            else if (rightPressed) OnClick_NextRoomType();
            return;
        }

        if (isPublicRoom && roomNameInputField != null)
        {
            bool isInputFieldSelected = (selected == roomNameInputField.gameObject) ||
                                        (selected == roomNameInputField.transform.parent?.gameObject);

            if (isInputFieldSelected && enterOrActionPressed)
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
