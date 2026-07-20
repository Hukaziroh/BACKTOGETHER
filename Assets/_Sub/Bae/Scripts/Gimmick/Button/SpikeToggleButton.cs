using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using Mirror;

public class SpikeToggleButton : NetworkBehaviour
{
    [Header("가시 이동 설정")]
    [Tooltip("움직일 가시들의 부모 오브젝트 (Transform)")]
    public Transform targetSpikes;

    [Tooltip("가시가 밑으로 내려갈 거리")]
    public float moveDistance = 1.5f;

    [Tooltip("가시가 오르내리는 속도")]
    public float moveSpeed = 5f;

    [Header("버튼 비주얼 설정")]
    public GameObject unpressedVisual;
    public GameObject pressedVisual;

    [Header("에코 관리 매니저")]
    public SpikeEchoParentManager echoManager; // 가시 에코 매니저

    private HashSet<GameObject> playersOnButton = new HashSet<GameObject>();

    [SyncVar(hook = nameof(OnButtonStateChanged))]
    private bool isPressed = false;

    private Vector3 originalSpikePos;
    private Vector3 hiddenSpikePos;
    private Coroutine moveCoroutine;

    public override void OnStartClient()
    {
        base.OnStartClient();

        if (targetSpikes != null)
        {
            originalSpikePos = targetSpikes.position;
            hiddenSpikePos = originalSpikePos + Vector3.down * moveDistance;
        }
        UpdateState(isPressed, true);
    }

    [ServerCallback]
    private void OnTriggerEnter2D(Collider2D other)
    {
        if (other.CompareTag("Player") && other.gameObject == other.transform.root.gameObject)
        {
            playersOnButton.Add(other.gameObject);
            UpdateButtonState();
        }
    }

    [ServerCallback]
    private void OnTriggerExit2D(Collider2D other)
    {
        if (other.CompareTag("Player") && other.gameObject == other.transform.root.gameObject)
        {
            playersOnButton.Remove(other.gameObject);
            UpdateButtonState();
        }
    }

    [Server]
    private void UpdateButtonState()
    {
        playersOnButton.RemoveWhere(go => go == null || !go.activeInHierarchy);

        bool currentlyPressed = (playersOnButton.Count > 0);

        if (isPressed != currentlyPressed)
        {
            isPressed = currentlyPressed;
        }
    }

    private void OnButtonStateChanged(bool oldState, bool newState)
    {
        UpdateState(newState, false);
    }

    private void UpdateState(bool pressed, bool instant)
    {
        // 1. 비주얼 오브젝트 켜고 끄기 (여기에 따라 버튼 윤곽선도 자동으로 켜지고 꺼짐)
        if (unpressedVisual != null) unpressedVisual.SetActive(!pressed);
        if (pressedVisual != null) pressedVisual.SetActive(pressed);

        // 2. 가시 에코 제어
        if (echoManager != null)
        {
            echoManager.SetEchoActive(!pressed);
        }

        // 3. 가시 이동 처리
        if (targetSpikes != null)
        {
            if (moveCoroutine != null) StopCoroutine(moveCoroutine);
            Vector3 targetPos = pressed ? hiddenSpikePos : originalSpikePos;

            if (instant)
            {
                targetPos.y = Mathf.Round(targetPos.y * 1000f) / 1000f;
                targetSpikes.position = targetPos;
            }
            else
            {
                moveCoroutine = StartCoroutine(MoveSpikesRoutine(targetPos));
            }
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