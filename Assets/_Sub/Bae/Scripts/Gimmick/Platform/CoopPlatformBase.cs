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

    public override void OnStartClient()
    {
        base.OnStartClient();
        MovingPlatformClientSmoothing.Configure(this);
    }

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

internal static class MovingPlatformClientSmoothing
{
    public static void Configure(NetworkBehaviour platform, Rigidbody2D platformRb = null)
    {
        NetworkTransformReliable networkTransform = platform.GetComponent<NetworkTransformReliable>();
        Rigidbody2D rigidbody = platformRb != null ? platformRb : platform.GetComponent<Rigidbody2D>();

        // 호스트는 기존 물리 이동과 Rigidbody 보간을 그대로 사용합니다.
        if (platform.isServer)
        {
            if (networkTransform != null)
                networkTransform.useFixedUpdate = true;
            if (rigidbody != null)
                rigidbody.interpolation = RigidbodyInterpolation2D.Interpolate;
            return;
        }

        // 원격 클라이언트는 NetworkTransform 보간만 사용해 이중 보간을 막습니다.
        if (networkTransform != null)
            networkTransform.useFixedUpdate = false;
        if (rigidbody != null)
            rigidbody.interpolation = RigidbodyInterpolation2D.None;
    }
}
