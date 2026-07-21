using System.Collections.Generic;
using UnityEngine;
using Mirror;

public class CoopMovingLog : NetworkBehaviour
{
    [Header("통나무 이동 설정")]
    public Transform endPoint;
    public float moveSpeed = 2f;
    public int requiredPlayers = 4;

    [Header("물리 설정")]
    public Rigidbody2D logRigidbody;

    // 💡 동기화 변수 추가
    [SyncVar] private Vector2 syncVelocity;
    public Vector2 CurrentVelocity { get { return syncVelocity; } }

    private Vector2 startPos;
    private HashSet<GameObject> playersOnLog = new HashSet<GameObject>();

    void Start()
    {
        if (logRigidbody != null)
        {
            startPos = logRigidbody.position;
        }
    }

    private void OnTriggerEnter2D(Collider2D other)
    {
        if (other.CompareTag("Player") && isServer)
            playersOnLog.Add(other.gameObject);
    }

    private void OnTriggerExit2D(Collider2D other)
    {
        if (other.CompareTag("Player") && isServer)
            playersOnLog.Remove(other.gameObject);
    }

    void FixedUpdate()
    {
        if (isServer && logRigidbody != null)
        {
            playersOnLog.RemoveWhere(go => go == null || !go.activeInHierarchy);

            Vector2 currentPos = logRigidbody.position;
            Vector2 targetPos = (playersOnLog.Count >= requiredPlayers) ? (Vector2)endPoint.position : startPos;
            Vector2 nextPos = Vector2.MoveTowards(currentPos, targetPos, moveSpeed * Time.fixedDeltaTime);

            logRigidbody.MovePosition(nextPos);

            // 💡 계산된 속도를 서버에서 클라이언트로 동기화
            syncVelocity = (nextPos - currentPos) / Time.fixedDeltaTime;
        }
    }
}