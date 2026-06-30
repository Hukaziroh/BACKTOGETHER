using System.Collections.Generic;
using UnityEngine;
using Mirror;

public class CoopPatrolPlatform : NetworkBehaviour
{
    [Header("패트롤 설정")]
    [Tooltip("출발 지점 (씬에 배치된 빈 오브젝트)")]
    public Transform startPoint;
    [Tooltip("도착 지점 (씬에 배치된 빈 오브젝트)")]
    public Transform endPoint;

    [Tooltip("이동 속도")]
    public float moveSpeed = 2f;

    [Tooltip("작동에 필요한 최소 플레이어 수 (0이면 혼자서 항상 움직임)")]
    public int requiredPlayers = 0;

    [Header("물리 설정")]
    public Rigidbody2D platformRigidbody;

    private HashSet<GameObject> playersOnPlatform = new HashSet<GameObject>();
    private Transform currentTarget;

    private bool isLocalPlayerOnPlatform = false;
    private Transform localPlayerTransform;
    private Vector3 lastPlatformPos;

    void Start()
    {
        if (platformRigidbody != null && startPoint != null)
        {
            platformRigidbody.position = startPoint.position;
        }

        currentTarget = endPoint; 
        lastPlatformPos = transform.position;
    }

    private void OnTriggerEnter2D(Collider2D other)
    {
        if (other.CompareTag("Player"))
        {
            if (isServer) playersOnPlatform.Add(other.gameObject);

            NetworkIdentity ni = other.GetComponent<NetworkIdentity>();
            if (isClient && ni != null && ni.isLocalPlayer)
            {
                isLocalPlayerOnPlatform = true;
                localPlayerTransform = other.transform;
            }
        }
    }

    private void OnTriggerExit2D(Collider2D other)
    {
        if (other.CompareTag("Player"))
        {
            if (isServer) playersOnPlatform.Remove(other.gameObject);

            NetworkIdentity ni = other.GetComponent<NetworkIdentity>();
            if (isClient && ni != null && ni.isLocalPlayer)
            {
                isLocalPlayerOnPlatform = false;
                localPlayerTransform = null;
            }
        }
    }

    void FixedUpdate()
    {
        if (isServer && platformRigidbody != null && startPoint != null && endPoint != null)
        {
            playersOnPlatform.RemoveWhere(go => go == null || !go.activeInHierarchy);

            bool canMove = (requiredPlayers == 0) || (playersOnPlatform.Count >= requiredPlayers);

            if (canMove)
            {
                Vector2 currentPos = platformRigidbody.position;
                Vector2 nextPos = Vector2.MoveTowards(currentPos, currentTarget.position, moveSpeed * Time.fixedDeltaTime);

                platformRigidbody.MovePosition(nextPos);

                if (Vector2.Distance(currentPos, currentTarget.position) < 0.05f)
                {
                    currentTarget = (currentTarget == endPoint) ? startPoint : endPoint;
                }
            }
        }
    }

    void LateUpdate()
    {
        if (isClient)
        {
            Vector3 currentPlatformPos = transform.position;
            Vector3 delta = currentPlatformPos - lastPlatformPos;

            if (isLocalPlayerOnPlatform && localPlayerTransform != null)
            {
                localPlayerTransform.position += delta;
            }

            lastPlatformPos = currentPlatformPos;
        }
    }
}