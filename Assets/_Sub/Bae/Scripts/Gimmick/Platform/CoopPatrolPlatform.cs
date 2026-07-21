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

    // 💡 동기화 변수 추가
    [SyncVar] private Vector2 syncVelocity;
    public Vector2 CurrentVelocity { get { return syncVelocity; } }

    private HashSet<GameObject> playersOnPlatform = new HashSet<GameObject>();
    private Transform currentTarget;

    void Start()
    {
        if (platformRigidbody != null && startPoint != null)
            platformRigidbody.position = startPoint.position;

        currentTarget = endPoint;
    }

    private void OnTriggerEnter2D(Collider2D other)
    {
        if (other.CompareTag("Player") && isServer)
            playersOnPlatform.Add(other.gameObject);
    }

    private void OnTriggerExit2D(Collider2D other)
    {
        if (other.CompareTag("Player") && isServer)
            playersOnPlatform.Remove(other.gameObject);
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

                // 💡 계산된 속도를 서버에서 클라이언트로 동기화
                syncVelocity = (nextPos - currentPos) / Time.fixedDeltaTime;

                if (Vector2.Distance(currentPos, currentTarget.position) < 0.05f)
                    currentTarget = (currentTarget == endPoint) ? startPoint : endPoint;
            }
            else
            {
                syncVelocity = Vector2.zero; // 💡 멈췄을 때 속도 0 동기화
            }
        }
    }
}