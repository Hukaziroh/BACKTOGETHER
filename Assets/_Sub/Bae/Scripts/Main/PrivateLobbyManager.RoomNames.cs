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
    private string GetRandomDefaultRoomName()
    {
        if (defaultRoomNames != null && defaultRoomNames.Length > 0)
        {
            int randomIndex = Random.Range(0, defaultRoomNames.Length);
            return defaultRoomNames[randomIndex];
        }
        return "Back Together!";
    }
}
