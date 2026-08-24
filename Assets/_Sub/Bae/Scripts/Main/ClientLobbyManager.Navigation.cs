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
    private void OnSearchInputEndEdit(string text)
    {
        if (searchInputField != null)
        {
            bool submittedWithKeyboard = WasKeyboardEnterPressedThisFrame();
            searchInputField.interactable = false;
            searchInputField.DeactivateInputField();

            if (submittedWithKeyboard)
            {
                StartCoroutine(RestoreInputSelectionNextFrame(searchInputField));
            }
        }
    }

    private void OnPrivateRoomInputEndEdit(string text)
    {
        if (privateRoomInputField != null)
        {
            bool submittedWithKeyboard = WasKeyboardEnterPressedThisFrame();
            privateRoomInputField.interactable = false;
            privateRoomInputField.DeactivateInputField();

            if (submittedWithKeyboard)
            {
                StartCoroutine(RestoreInputSelectionNextFrame(privateRoomInputField));
            }
        }
    }

    private static bool WasKeyboardEnterPressedThisFrame()
    {
        return Keyboard.current != null &&
               (Keyboard.current.enterKey.wasPressedThisFrame ||
                Keyboard.current.numpadEnterKey.wasPressedThisFrame);
    }

    private IEnumerator RestoreInputSelectionNextFrame(TMP_InputField inputField)
    {
        yield return null;

        if (inputField != null && inputField.isActiveAndEnabled && EventSystem.current != null)
        {
            EventSystem.current.SetSelectedGameObject(inputField.gameObject);
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
            // 퍼블릭 패널 진입 기본 포커스는 새로고침 버튼으로 고정한다.
            if (EventSystem.current != null)
            {
                EventSystem.current.SetSelectedGameObject(null);
                if (researchButton != null)
                {
                    EventSystem.current.SetSelectedGameObject(researchButton.gameObject);
                }
            }
        }
        if (logo != null) logo.SetActive(false);

        OnClick_Research();
    }

    public void OnClick_SelectPrivateMode()
    {
        SubscribeEvents();
        CancelPublicListRefreshFocusLock();

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
        CancelPublicListRefreshFocusLock();

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
        CancelPublicListRefreshFocusLock();

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

}
