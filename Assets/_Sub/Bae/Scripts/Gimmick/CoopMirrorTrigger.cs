using UnityEngine;
using Mirror;

public class CoopMirrorTrigger : NetworkBehaviour
{
    public CoopMirrorPlatforms manager;
    [Tooltip("이 발판이 왼쪽(출발) 발판이면 체크, 오른쪽이면 해제")]
    public bool isLeftPlatform = true;

    private void OnTriggerEnter2D(Collider2D other)
    {
        if (other.CompareTag("Player") && manager != null)
        {
            if (isServer)
            {
                if (isLeftPlatform) manager.PlayerEnteredLeft(other.gameObject);
                else manager.PlayerEnteredRight(other.gameObject);
            }

            NetworkIdentity ni = other.GetComponent<NetworkIdentity>();
            if (isClient && ni != null && ni.isLocalPlayer)
            {
                if (isLeftPlatform) manager.SetLocalPlayerOnLeft(other.gameObject, other.transform, true);
                else manager.SetLocalPlayerOnRight(other.gameObject, other.transform, true);
            }
        }
    }

    private void OnTriggerExit2D(Collider2D other)
    {
        if (other.CompareTag("Player") && manager != null)
        {
            if (isServer)
            {
                if (isLeftPlatform) manager.PlayerExitedLeft(other.gameObject);
                else manager.PlayerExitedRight(other.gameObject);
            }

            NetworkIdentity ni = other.GetComponent<NetworkIdentity>();
            if (isClient && ni != null && ni.isLocalPlayer)
            {
                if (isLeftPlatform) manager.SetLocalPlayerOnLeft(other.gameObject, other.transform, false);
                else manager.SetLocalPlayerOnRight(other.gameObject, other.transform, false);
            }
        }
    }
}