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
        if (WalkingLoadingPanel.Instance != null) { loadingPanel = WalkingLoadingPanel.Instance.gameObject; return loadingPanel; }
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
            if (EventSystem.current != null)
            {
                EventSystem.current.SetSelectedGameObject(null);
                if (errorCloseButton != null) EventSystem.current.SetSelectedGameObject(errorCloseButton.gameObject);
                else EventSystem.current.SetSelectedGameObject(errorPopupPanel);
            }
        }
    }

    public void OnClick_CloseErrorPopup()
    {
        if (errorPopupPanel != null) errorPopupPanel.SetActive(false);

        if (GlobalSceneInputManager.Instance != null)
        {
            if (hostPanel != null && hostPanel.activeSelf)
            {
                GlobalSceneInputManager.Instance.SetFocusScope(hostPanel);
            }
            else if (mainPanel != null && mainPanel.activeSelf)
            {
                GlobalSceneInputManager.Instance.SetFocusScope(mainPanel);
            }
        }
    }

    public void OnClick_ReturnToMain()
    {
        NoCheckpointButtonController noCheckpointController =
            FindAnyObjectByType<NoCheckpointButtonController>(FindObjectsInactive.Include);
        if (noCheckpointController != null)
        {
            noCheckpointController.ResetSelectionToDefault();
        }

        var lobby = GetEOSLobby();
        if (lobby != null && lobby.ConnectedToLobby)
        {
            if (NetworkServer.active && NetworkClient.active) lobby.DestroyLobby();
            else if (NetworkClient.active) lobby.LeaveLobby();
            else lobby.DestroyLobby();
        }

        // ★ 수정된 핵심 부분: StopHost() 이후에 EosTransport를 강제로 Shutdown 시킵니다.
        if (NetworkManager.singleton != null)
        {
            NetworkManager.singleton.StopHost();
        }

        if (hostPanel != null) hostPanel.SetActive(false);
        if (logo != null) logo.SetActive(true);
        if (mainPanel != null)
        {
            mainPanel.SetActive(true);
            if (GlobalSceneInputManager.Instance != null) GlobalSceneInputManager.Instance.SetFocusScope(mainPanel);
        }
        if (errorPopupPanel != null) errorPopupPanel.SetActive(false);
    }

}
