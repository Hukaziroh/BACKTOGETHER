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
        CancelPublicListRefreshFocusLock();

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

    private void BeginPublicListRefreshFocusLock()
    {
        if (clientPublicPanel == null || !clientPublicPanel.activeInHierarchy)
            return;

        publicListRefreshLockedFocus = lastSelectedBeforeSearch;
        if (publicListRefreshLockedFocus == null || !publicListRefreshLockedFocus.activeInHierarchy)
        {
            publicListRefreshLockedFocus = researchButton != null ? researchButton.gameObject : null;
        }

        isPublicListRefreshFocusLocked = publicListRefreshLockedFocus != null;
        publicListRefreshDisabledSelectables.Clear();

        Selectable[] selectables = clientPublicPanel.GetComponentsInChildren<Selectable>(true);
        foreach (var selectable in selectables)
        {
            if (selectable != null &&
                selectable.interactable &&
                selectable.gameObject != publicListRefreshLockedFocus)
            {
                selectable.interactable = false;
                publicListRefreshDisabledSelectables.Add(selectable);
            }
        }

        EnforcePublicListRefreshFocusLock();
    }

    private void EndPublicListRefreshFocusLock()
    {
        foreach (var selectable in publicListRefreshDisabledSelectables)
        {
            if (selectable != null)
            {
                selectable.interactable = true;
            }
        }
        publicListRefreshDisabledSelectables.Clear();

        isPublicListRefreshFocusLocked = false;
        publicListRefreshLockedFocus = null;
    }

    private void CancelPublicListRefreshFocusLock()
    {
        isLocalSearchRequest = false;
        isQuickJoining = false;
        EndPublicListRefreshFocusLock();
        SetInteractableAll(true);
    }

    private void EnforcePublicListRefreshFocusLock()
    {
        if (!isPublicListRefreshFocusLocked || EventSystem.current == null)
            return;

        if (clientPublicPanel == null || !clientPublicPanel.activeInHierarchy ||
            publicListRefreshLockedFocus == null || !publicListRefreshLockedFocus.activeInHierarchy)
        {
            EndPublicListRefreshFocusLock();
            return;
        }

        if (EventSystem.current.currentSelectedGameObject != publicListRefreshLockedFocus)
        {
            EventSystem.current.SetSelectedGameObject(null);
            EventSystem.current.SetSelectedGameObject(publicListRefreshLockedFocus);
        }
    }

    private void RestoreFocusAfterPublicListRefresh()
    {
        if (!isPublicListRefreshFocusLocked)
            return;

        if (clientPublicPanel == null || !clientPublicPanel.activeInHierarchy)
            return;

        StartCoroutine(RestoreFocusAfterPublicListRefreshRoutine());
    }

    private IEnumerator RestoreFocusAfterPublicListRefreshRoutine()
    {
        yield return null;

        if (EventSystem.current == null || clientPublicPanel == null || !clientPublicPanel.activeInHierarchy)
            yield break;

        GameObject target = null;
        if (lastSelectedBeforeSearch != null &&
            lastSelectedBeforeSearch.activeInHierarchy &&
            lastSelectedBeforeSearch.transform.IsChildOf(clientPublicPanel.transform))
        {
            target = lastSelectedBeforeSearch;
        }
        else if (researchButton != null && researchButton.gameObject.activeInHierarchy)
        {
            target = researchButton.gameObject;
        }
        else if (searchInputField != null && searchInputField.gameObject.activeInHierarchy)
        {
            target = searchInputField.gameObject;
        }

        if (target != null)
        {
            EventSystem.current.SetSelectedGameObject(null);
            EventSystem.current.SetSelectedGameObject(target);
        }

        EndPublicListRefreshFocusLock();
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
        if (isReturningAfterConnectionFailure) return;
        if (errorPopupPanel != null) errorPopupPanel.SetActive(false);

        RestoreInputScopeAfterPopup();
        StartCoroutine(ReturnAfterConnectionFailureRoutine());
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
        if (isReturningAfterConnectionFailure) return;
        if (timeoutPopupPanel != null) timeoutPopupPanel.SetActive(false);

        StartCoroutine(ReturnAfterConnectionFailureRoutine());
    }

    private IEnumerator ReturnAfterConnectionFailureRoutine()
    {
        isReturningAfterConnectionFailure = true;

        var eos = GetEOSLobby();
        if (eos != null)
        {
            if (eos.ConnectedToLobby && !eos.IsLeavingLobby)
            {
                eos.LeaveLobby();
            }

            float leaveTimeout = 5f;
            while (eos.IsLeavingLobby && leaveTimeout > 0f)
            {
                leaveTimeout -= Time.unscaledDeltaTime;
                yield return null;
            }
        }

        if (NetworkManager.singleton != null && NetworkClient.active)
        {
            NetworkManager.singleton.StopClient();
        }

        string currentSceneName = SceneManager.GetActiveScene().name;
        yield return new WaitForSecondsRealtime(0.2f);

        if (SceneManager.GetActiveScene().name == currentSceneName)
        {
            SceneManager.LoadScene(currentSceneName);
        }

        isReturningAfterConnectionFailure = false;
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
