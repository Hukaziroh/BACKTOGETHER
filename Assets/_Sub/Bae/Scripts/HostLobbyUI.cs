using UnityEngine;
using TMPro; 
using EpicTransport;

public class HostLobbyUI : MonoBehaviour
{
    public TextMeshProUGUI roomCodeText;

    void Start()
    {
        string myRoomCode = EOSSDKComponent.LocalUserProductIdString;

        if (roomCodeText != null)
        {
            roomCodeText.text = "Room Code: " + myRoomCode;
        }
    }

    public void OnCopyButtonClicked()
    {
        GUIUtility.systemCopyBuffer = EOSSDKComponent.LocalUserProductIdString;
        Debug.Log("방 코드가 복사되었습니다!");
    }
}
