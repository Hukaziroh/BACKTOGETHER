using System.Collections;
using UnityEngine;
using Mirror;

public class CoopSpikeTimerManager : NetworkBehaviour
{
    [Header("연결할 공용 버튼들")]
    [Tooltip("여기에 CoopButton 오브젝트 4개를 드래그해서 넣으세요.")]
    public CoopButton[] requiredButtons;

    [Header("가시 이동 설정")]
    [Tooltip("움직일 가시들의 부모 오브젝트 (Transform)")]
    public Transform targetSpikes;

    [Tooltip("가시가 밑으로 내려갈 거리")]
    public float moveDistance = 1.5f;

    [Tooltip("가시가 오르내리는 속도")]
    public float moveSpeed = 5f;

    [Header("타이머 설정")]
    [Tooltip("버튼이 모두 눌린 후 가시가 내려가 있는 시간 (초)")]
    public float hideDuration = 10f;

    [SyncVar(hook = nameof(OnSpikeStateChanged))]
    private bool isSpikesHidden = false;

    private Vector3 originalSpikePos;
    private Vector3 hiddenSpikePos;
    private Coroutine moveCoroutine;

    void Awake()
    {
        if (targetSpikes != null)
        {
            originalSpikePos = targetSpikes.position;
            hiddenSpikePos = originalSpikePos + Vector3.down * moveDistance;
        }
    }


    public override void OnStartServer()
    {
        base.OnStartServer();
        foreach (var btn in requiredButtons)
        {
            if (btn != null)
            {
                btn.OnButtonStateChangedEvent += CheckAllButtons;
            }
        }
    }

    public override void OnStartClient()
    {
        base.OnStartClient();
        UpdateSpikesVisual(isSpikesHidden, true);
    }

    [Server]
    private void CheckAllButtons(CoopButton changedButton)
    {
        if (isSpikesHidden) return;

        bool allPressed = true;
        foreach (var btn in requiredButtons)
        {
            if (btn == null || !btn.isPressed)
            {
                allPressed = false;
                break;
            }
        }

        if (allPressed)
        {
            isSpikesHidden = true;
            StartCoroutine(SpikeTimerRoutine());
        }
    }

    [Server]
    private IEnumerator SpikeTimerRoutine()
    {
        yield return new WaitForSeconds(hideDuration);
        isSpikesHidden = false;
    }

    private void OnSpikeStateChanged(bool oldState, bool newState)
    {
        UpdateSpikesVisual(newState, false);
    }

    private void UpdateSpikesVisual(bool isHidden, bool instant)
    {
        if (targetSpikes == null) return;
        if (moveCoroutine != null)
        {
            StopCoroutine(moveCoroutine);
            moveCoroutine = null;
        }

        Vector3 targetPos = isHidden ? hiddenSpikePos : originalSpikePos;

        if (instant)
        {
            targetSpikes.position = targetPos;
        }
        else
        {
            moveCoroutine = StartCoroutine(MoveSpikesRoutine(targetPos));
        }
    }

    private IEnumerator MoveSpikesRoutine(Vector3 targetPos)
    {
        while (Vector3.Distance(targetSpikes.position, targetPos) > 0.01f)
        {
            targetSpikes.position = Vector3.MoveTowards(targetSpikes.position, targetPos, moveSpeed * Time.deltaTime);
            yield return null;
        }
        targetSpikes.position = targetPos;
        moveCoroutine = null;
    }
}