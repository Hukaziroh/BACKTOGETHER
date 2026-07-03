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
        if (other.CompareTag("Player"))
        {
            // 서버는 인원수만 체크합니다.
            if (isServer) playersOnLog.Add(other.gameObject);

            // 🌟 핵심: 로컬 플레이어가 통나무를 밟으면 통나무의 '자식'으로 들어갑니다.
            NetworkIdentity ni = other.GetComponent<NetworkIdentity>();
            if (isClient && ni != null && ni.isLocalPlayer)
            {
                other.transform.SetParent(transform, true);
            }
        }
    }

    private void OnTriggerExit2D(Collider2D other)
    {
        if (other.CompareTag("Player"))
        {
            // 서버 인원수 차감
            if (isServer) playersOnLog.Remove(other.gameObject);

            // 🌟 통나무에서 내리면 다시 부모 관계를 끊고 독립합니다.
            NetworkIdentity ni = other.GetComponent<NetworkIdentity>();
            if (isClient && ni != null && ni.isLocalPlayer)
            {
                other.transform.SetParent(null);
            }
        }
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
        }
    }
}