using UnityEngine;
using Mirror;
using System.Collections;

public class CoopTimedSpikeGate : NetworkBehaviour
{
    [Header("연결할 버튼")]
    [Tooltip("이 가시를 열게 만들 CoopButton을 넣어주세요.")]
    public CoopButton targetButton;

    [Header("가시 오브젝트 연결")]
    public Transform topSpike;    
    public Transform bottomSpike; 

    [Header("작동 설정")]
    [Tooltip("열릴 때 각각 위/아래로 이동할 거리")]
    public float openDistance = 2f;
    [Tooltip("열려있는 유지 시간 (초)")]
    public float openDuration = 3f;
    [Tooltip("가시가 열리고 닫히는 이동 속도")]
    public float moveSpeed = 5f;

    [SyncVar]
    private bool isOpen = false;
    private Vector2 topClosedPos;
    private Vector2 bottomClosedPos;
    private Vector2 topOpenPos;
    private Vector2 bottomOpenPos;

    private Coroutine closeCoroutine;

    void Start()
    {
        if (topSpike != null)
        {
            topClosedPos = topSpike.position;
            topOpenPos = topClosedPos + Vector2.up * openDistance;
        }
        if (bottomSpike != null)
        {
            bottomClosedPos = bottomSpike.position;
            bottomOpenPos = bottomClosedPos + Vector2.down * openDistance;
        }

        if (isServer && targetButton != null)
        {
            targetButton.OnButtonStateChangedEvent += HandleButtonStateChanged;
        }
    }

    private void OnDestroy()
    { 
        if (isServer && targetButton != null)
        {
            targetButton.OnButtonStateChangedEvent -= HandleButtonStateChanged;
        }
    }

    [Server]
    private void HandleButtonStateChanged(CoopButton btn)
    {
        if (btn.isPressed && !isOpen)
        {
            isOpen = true; 
            if (closeCoroutine != null) StopCoroutine(closeCoroutine);
            closeCoroutine = StartCoroutine(CloseAfterDelay());
        }
    }

    [Server]
    private IEnumerator CloseAfterDelay()
    {
        yield return new WaitForSeconds(openDuration);

        isOpen = false;
    }

    void Update()
    {
        if (topSpike != null)
        {
            Vector2 targetTop = isOpen ? topOpenPos : topClosedPos;
            topSpike.position = Vector2.MoveTowards(topSpike.position, targetTop, moveSpeed * Time.deltaTime);
        }

        if (bottomSpike != null)
        {
            Vector2 targetBottom = isOpen ? bottomOpenPos : bottomClosedPos;
            bottomSpike.position = Vector2.MoveTowards(bottomSpike.position, targetBottom, moveSpeed * Time.deltaTime);
        }
    }
}