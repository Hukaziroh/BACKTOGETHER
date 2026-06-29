using UnityEngine;
using TMPro; 
using Mirror;

public class PlayerCountUI : MonoBehaviour
{
    public TextMeshProUGUI playerCountText; 

    void Update()
    {
        if (NetworkServer.active)
        {
            int count = NetworkServer.connections.Count;
            playerCountText.text = $"Player: {count} / 4";
        }
        else
        {
            playerCountText.text = $"Player: {NetworkManager.singleton.numPlayers} / 4";
        }
    }
}