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

}
