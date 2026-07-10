using System.Collections;
using UnityEngine;
using Mirror;

public class PermanentSpikeButton : NetworkBehaviour
{
    [Header("연결할 버튼들")]
    [Tooltip("여기에 버튼을 드래그해서 넣으세요. (하나만 넣어도 작동합니다)")]
    public CoopButton[] requiredButtons;

    [Header("가시 이동 설정")]
    public Transform targetSpikes;
    public float moveDistance = 1.5f;
    public float moveSpeed = 5f;

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

    public override void OnStartClient()
    {
        base.OnStartClient();
        UpdateSpikesVisual(isSpikesHidden, true);
    }

    [ServerCallback]
    void Update()
    {
        if (isSpikesHidden) return;
        bool isAnyButtonPressed = false;
        foreach (var btn in requiredButtons)
        {
            if (btn != null && btn.isPressed)
            {
                isAnyButtonPressed = true;
                break; 
            }
        }
        if (isAnyButtonPressed)
        {
            isSpikesHidden = true;
        }
    }

    private void OnSpikeStateChanged(bool oldState, bool newState)
    {
        UpdateSpikesVisual(newState, false);
    }

    private void UpdateSpikesVisual(bool isHidden, bool instant)
    {
        if (targetSpikes == null) return;

        if (moveCoroutine != null) StopCoroutine(moveCoroutine);

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
    }
}