using System.Collections;
using UnityEngine;
using Mirror;

public class MultiButtonSpikeManager : NetworkBehaviour
{
    [Header("연결할 발판들 (CoopButton)")]
    [Tooltip("이 가시를 제어할 모든 버튼을 배열에 넣어주세요.")]
    public CoopButton[] connectedButtons;

    [Header("가시 이동 설정")]
    public Transform targetSpikes;
    public float moveDistance = 1.5f;
    public float moveSpeed = 5f;

    [Header("에코 관리 매니저")]
    public SpikeEchoParentManager echoManager; 

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
        foreach (var btn in connectedButtons)
        {
            if (btn != null)
            {
                btn.OnButtonStateChangedEvent += CheckAllButtons;
            }
        }
    }

    [Server]
    private void CheckAllButtons(CoopButton changedButton)
    {
        bool isAnyPressed = false;
        foreach (var btn in connectedButtons)
        {
            if (btn != null && btn.isPressed)
            {
                isAnyPressed = true;
                break;
            }
        }
        if (isSpikesHidden != isAnyPressed)
        {
            isSpikesHidden = isAnyPressed;
        }
    }

    private void OnSpikeStateChanged(bool oldState, bool newState)
    {
        UpdateSpikesVisual(newState, false);
    }

    private void UpdateSpikesVisual(bool isHidden, bool instant)
    {
        if (targetSpikes == null) return;
        if (echoManager != null) echoManager.SetEchoActive(!isHidden);

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