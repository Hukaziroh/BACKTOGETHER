using System.Collections.Generic;
using UnityEngine;
using Mirror;

public class CoopPatrolPlatform : NetworkBehaviour
{
    [Header("패트롤 설정")]
    public Transform startPoint;
    public Transform endPoint;
    public float moveSpeed = 2f;
    public int requiredPlayers = 0;

    [Header("물리 설정")]
    public Rigidbody2D platformRigidbody;

    // 💡 에러 해결: 플레이어가 가져갈 발판의 현재 속도 변수 추가!
    public Vector2 CurrentVelocity { get; private set; }

    private HashSet<GameObject> playersOnPlatform = new HashSet<GameObject>();
    private Transform currentTarget;
    private bool isLocalPlayerOnPlatform = false;
    private Transform localPlayerTransform;
    private Vector3 lastPlatformPos;

    void Start()
    {
        if (platformRigidbody != null && startPoint != null)
            platformRigidbody.position = startPoint.position;

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

                // 💡 에러 해결: 이동량 기반 속도 계산 (PlayerController가 이 값을 가져갑니다)
                CurrentVelocity = (nextPos - currentPos) / Time.fixedDeltaTime;

                platformRigidbody.MovePosition(nextPos);

                if (Vector2.Distance(currentPos, currentTarget.position) < 0.05f)
                    currentTarget = (currentTarget == endPoint) ? startPoint : endPoint;
            }
            else
            {
                CurrentVelocity = Vector2.zero;
            }
        }
    }

    void LateUpdate()
    {
        if (isClient)
        {
            if (isLocalPlayerOnPlatform && localPlayerTransform != null)
            {
                Vector3 currentPlatformPos = transform.position;
                localPlayerTransform.position += (currentPlatformPos - lastPlatformPos);
            }
            lastPlatformPos = transform.position;
        }
    }
}