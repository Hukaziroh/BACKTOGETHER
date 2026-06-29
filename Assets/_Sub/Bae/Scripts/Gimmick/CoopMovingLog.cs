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

    [ServerCallback]
    private void OnTriggerEnter2D(Collider2D other)
    {
        if (other.CompareTag("Player"))
        {
            playersOnLog.Add(other.gameObject);
        }
    }

    [ServerCallback]
    private void OnTriggerExit2D(Collider2D other)
    {
        if (other.CompareTag("Player"))
        {
            playersOnLog.Remove(other.gameObject);
        }
    }

    [ServerCallback]
    void FixedUpdate()
    {
        if (logRigidbody == null) return;

        playersOnLog.RemoveWhere(go => go == null || !go.activeInHierarchy);

        Vector2 currentPos = logRigidbody.position;
        Vector2 targetPos = (playersOnLog.Count >= requiredPlayers) ? (Vector2)endPoint.position : startPos;
        Vector2 nextPos = Vector2.MoveTowards(currentPos, targetPos, moveSpeed * Time.fixedDeltaTime);
        Vector2 delta = nextPos - currentPos;

        logRigidbody.MovePosition(nextPos);

        foreach (GameObject player in playersOnLog)
        {
            if (player != null)
            {
                player.transform.position += (Vector3)delta;
            }
        }
    }
}