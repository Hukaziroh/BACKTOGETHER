using System.Collections.Generic;
using UnityEngine;
using Mirror;

public class CoopMirrorPlatforms : NetworkBehaviour
{
    [Header("발판 설정")]
    [Tooltip("왼쪽(출발) 발판의 Rigidbody2D")]
    public Rigidbody2D leftPlatform;
    [Tooltip("오른쪽(도착) 발판의 Rigidbody2D")]
    public Rigidbody2D rightPlatform;

    [Header("위치 설정 (씬에 배치된 빈 오브젝트)")]
    public Transform leftStartPoint;
    public Transform rightStartPoint;
    public Vector3 le;
    public Vector3 ri;
    [Tooltip("두 발판이 만나서 플레이어가 갈아탈 가운데 지점")]
    public Transform meetingPoint;

    [Header("이동 설정")]
    public float moveSpeed = 3f;
    [Tooltip("기믹 작동에 필요한 플레이어 수 (테스트 시 1로 변경 가능)")]
    public int requiredPlayers = 4;

    private HashSet<GameObject> playersOnLeft = new HashSet<GameObject>();
    private HashSet<GameObject> playersOnRight = new HashSet<GameObject>();

    private bool isWaitingToReturn = false;

    private enum PlatformState { Idle, Meeting, Returning }
    [SyncVar]
    private PlatformState currentState = PlatformState.Idle;

    void Start()
    {
        if (leftPlatform != null && leftStartPoint != null)
            leftPlatform.position = leftStartPoint.position;
        if (rightPlatform != null && rightStartPoint != null)
            rightPlatform.position = rightStartPoint.position;
    }

    void FixedUpdate()
    {
        if (!isServer) return;

        playersOnLeft.RemoveWhere(go => go == null || !go.activeInHierarchy);
        playersOnRight.RemoveWhere(go => go == null || !go.activeInHierarchy);

        switch (currentState)
        {
            case PlatformState.Idle:
                if (playersOnLeft.Count >= requiredPlayers)
                {
                    currentState = PlatformState.Meeting;
                    Debug.Log("[거울 발판] 왼쪽 발판 4명 탑승! 가운데로 모입니다.");
                }
                break;

            case PlatformState.Meeting:
                MovePlatform(leftPlatform, meetingPoint.position - le);
                MovePlatform(rightPlatform, meetingPoint.position + ri);

                if (playersOnLeft.Count < requiredPlayers && !isWaitingToReturn)
                {
                    StartCoroutine(WaitAndReturn());
                }
                break;

            case PlatformState.Returning:
                MovePlatform(leftPlatform, leftStartPoint.position);
                MovePlatform(rightPlatform, rightStartPoint.position);

                if (Vector2.Distance(leftPlatform.position, leftStartPoint.position) < 0.05f &&
                    Vector2.Distance(rightPlatform.position, rightStartPoint.position) < 0.05f)
                {
                    currentState = PlatformState.Idle;
                    Debug.Log("[거울 발판] 원위치 복귀 완료. 대기 상태.");
                }
                break;
        }
    }

    private void MovePlatform(Rigidbody2D rb, Vector2 targetPos)
    {
        if (rb == null) return;
        Vector2 nextPos = Vector2.MoveTowards(rb.position, targetPos, moveSpeed * Time.fixedDeltaTime);
        rb.MovePosition(nextPos);
    }

    private System.Collections.IEnumerator WaitAndReturn()
    {
        isWaitingToReturn = true;
        Debug.Log("[거울 발판] 1초 뒤 돌아갑니다...");

        yield return new WaitForSeconds(1.0f);

        currentState = PlatformState.Returning;
        isWaitingToReturn = false;
        Debug.Log("[거울 발판] 원래 위치로 돌아갑니다.");
    }

    [Server]
    public void PlayerEnteredLeft(GameObject player) { playersOnLeft.Add(player); }
    [Server]
    public void PlayerExitedLeft(GameObject player) { playersOnLeft.Remove(player); }
    [Server]
    public void PlayerEnteredRight(GameObject player) { playersOnRight.Add(player); }
    [Server]
    public void PlayerExitedRight(GameObject player) { playersOnRight.Remove(player); }

   
}