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

    private bool isLocalPlayerOnLog = false;
    private Transform localPlayerTransform;
    private Vector3 lastLogPos;

    void Start()
    {
        if (logRigidbody != null)
        {
            startPos = logRigidbody.position;
        }
        lastLogPos = transform.position;
    }

    private void OnTriggerEnter2D(Collider2D other)
    {
        if (other.CompareTag("Player"))
        {
            if (isServer) playersOnLog.Add(other.gameObject);

            NetworkIdentity ni = other.GetComponent<NetworkIdentity>();
            if (isClient && ni != null && ni.isLocalPlayer)
            {
                isLocalPlayerOnLog = true;
                localPlayerTransform = other.transform;
            }
        }
    }

    private void OnTriggerExit2D(Collider2D other)
    {
        if (other.CompareTag("Player"))
        {
            if (isServer) playersOnLog.Remove(other.gameObject);

            NetworkIdentity ni = other.GetComponent<NetworkIdentity>();
            if (isClient && ni != null && ni.isLocalPlayer)
            {
                isLocalPlayerOnLog = false;
                localPlayerTransform = null;
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

    void LateUpdate()
    {
        if (isClient)
        {
            Vector3 currentLogPos = transform.position;
            Vector3 delta = currentLogPos - lastLogPos;

            if (isLocalPlayerOnLog && localPlayerTransform != null)
            {
                localPlayerTransform.position += delta;
            }

            lastLogPos = currentLogPos;
        }
    }
}