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
    private void OnRoomNameEndEdit(string text)
    {
        if (roomNameInputField != null)
        {
            bool submittedWithKeyboard = Keyboard.current != null &&
                                         (Keyboard.current.enterKey.wasPressedThisFrame ||
                                          Keyboard.current.numpadEnterKey.wasPressedThisFrame);

            roomNameInputField.interactable = false;
            roomNameInputField.DeactivateInputField();

            if (submittedWithKeyboard)
            {
                StartCoroutine(RestoreRoomNameSelectionNextFrame());
            }
        }
    }

    private IEnumerator RestoreRoomNameSelectionNextFrame()
    {
        yield return null;

        if (roomNameInputField != null && roomNameInputField.isActiveAndEnabled && EventSystem.current != null)
        {
            EventSystem.current.SetSelectedGameObject(roomNameInputField.gameObject);
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

}
