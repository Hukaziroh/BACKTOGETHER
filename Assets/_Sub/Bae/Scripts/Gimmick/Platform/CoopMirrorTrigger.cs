using UnityEngine;
using Mirror;

public class CoopMirrorTrigger : NetworkBehaviour
{
    public CoopMirrorPlatforms manager;
    [Tooltip("이 발판이 왼쪽(출발) 발판이면 체크, 오른쪽이면 해제")]
    public bool isLeftPlatform = true;

    public override void OnStartClient()
    {
        base.OnStartClient();
        MovingPlatformClientSmoothing.Configure(this);
    }

    private void OnTriggerEnter2D(Collider2D other)
    {
        if (other.CompareTag("Player") && manager != null && isServer)
        {
            if (isLeftPlatform) manager.PlayerEnteredLeft(other.gameObject);
            else manager.PlayerEnteredRight(other.gameObject);
        }
    }

    private void OnTriggerExit2D(Collider2D other)
    {
        if (other.CompareTag("Player") && manager != null && isServer)
        {
            if (isLeftPlatform) manager.PlayerExitedLeft(other.gameObject);
            else manager.PlayerExitedRight(other.gameObject);
        }
    }
}
