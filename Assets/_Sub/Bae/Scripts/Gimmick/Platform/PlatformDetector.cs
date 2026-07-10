using UnityEngine;
using Mirror;

public class PlatformDetector : NetworkBehaviour
{
    [Header("연결할 부모 매니저")]
    public CoopParallelPlatform manager;

    [Header("체크: 위쪽 발판인가요?")]
    public bool isTopPlatform;

    [ServerCallback]
    private void OnTriggerEnter2D(Collider2D other)
    {
        if (other.CompareTag("Player") && other.gameObject == other.transform.root.gameObject)
        {
            if (manager != null) manager.AddPlayer(other.gameObject, isTopPlatform);
        }
    }

    [ServerCallback]
    private void OnTriggerExit2D(Collider2D other)
    {
        if (other.CompareTag("Player") && other.gameObject == other.transform.root.gameObject)
        {
            if (manager != null) manager.RemovePlayer(other.gameObject, isTopPlatform);
        }
    }
}