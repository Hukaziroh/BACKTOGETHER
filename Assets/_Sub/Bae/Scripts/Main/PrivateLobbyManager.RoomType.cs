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
    public void OnClick_PrevRoomType() { isPublicRoom = !isPublicRoom; UpdateRoomTypeUI(); }
    public void OnClick_NextRoomType() { isPublicRoom = !isPublicRoom; UpdateRoomTypeUI(); }

    private void UpdateRoomTypeUI()
    {
        if (roomTypeDisplayText != null) roomTypeDisplayText.text = isPublicRoom ? "Public" : "Private";
        if (roomNameInputField != null)
        {
            roomNameInputField.interactable = false;
            roomNameInputField.DeactivateInputField();
        }
    }

}
