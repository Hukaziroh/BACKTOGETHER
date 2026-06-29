using System.Collections.Generic;
using UnityEngine;
using Mirror;

public class CoopMovingLog : NetworkBehaviour
{
    [Header("통나무 이동 설정")]
    [Tooltip("통나무가 이동할 목표 지점")]
    public Transform endPoint;

    [Tooltip("통나무 이동 속도")]
    public float moveSpeed = 2f;

    [Tooltip("출발하기 위해 필요한 플레이어 수")]
    public int requiredPlayers = 4;

    private Vector3 startPos;
    private HashSet<GameObject> playersOnLog = new HashSet<GameObject>();

    void Start()
    {
        startPos = transform.position;
    }

    [ServerCallback]
    private void OnTriggerEnter2D(Collider2D other)
    {
        if (other.CompareTag("Player"))
        {
            playersOnLog.Add(other.gameObject);
            Debug.Log($"[통나무] 현재 탑승 인원: {playersOnLog.Count} / {requiredPlayers}");
        }
    }

    [ServerCallback]
    private void OnTriggerExit2D(Collider2D other)
    {
        if (other.CompareTag("Player"))
        {
            playersOnLog.Remove(other.gameObject);
            Debug.Log($"[통나무] 현재 탑승 인원: {playersOnLog.Count} / {requiredPlayers}");
        }
    }

    [ServerCallback]
    void Update()
    {
        playersOnLog.RemoveWhere(go => go == null || !go.activeInHierarchy);

        Vector3 targetPos = (playersOnLog.Count >= requiredPlayers) ? endPoint.position : startPos;

        transform.position = Vector3.MoveTowards(transform.position, targetPos, moveSpeed * Time.deltaTime);
    }
}