using System.Collections.Generic;
using UnityEngine;
using Mirror;

/// <summary>
/// 움직이는 발판들의 공통 기능(속도 동기화, 플레이어 탑승 감지)을 담당하는 부모 클래스입니다.
/// </summary>
public abstract class CoopPlatformBase : NetworkBehaviour
{
    [SyncVar] protected Vector2 syncVelocity;
    public Vector2 CurrentVelocity { get { return syncVelocity; } }
    protected HashSet<GameObject> playersOnPlatform = new HashSet<GameObject>();
    [ServerCallback]
    protected virtual void OnTriggerEnter2D(Collider2D other)
    {
        if (other.CompareTag("Player") && other.gameObject == other.transform.root.gameObject)
        {
            playersOnPlatform.Add(other.gameObject);
        }
    }

    [ServerCallback]
    protected virtual void OnTriggerExit2D(Collider2D other)
    {
        if (other.CompareTag("Player") && other.gameObject == other.transform.root.gameObject)
        {
            playersOnPlatform.Remove(other.gameObject);
        }
    }

    [Server]
    protected void CleanUpPlayers()
    {
        playersOnPlatform.RemoveWhere(go => go == null || !go.activeInHierarchy);
    }
}