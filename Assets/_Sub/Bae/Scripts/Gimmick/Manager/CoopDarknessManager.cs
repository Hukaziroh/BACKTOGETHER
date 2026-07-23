using UnityEngine;
using UnityEngine.Events;
using System.Collections;
using Mirror;

public class CoopDarknessManager : NetworkBehaviour
{
    public enum GimmickState { Before, Reveal, Darkness, Clear }

    [Header("버튼 설정")]
    [Tooltip("암막을 해제하기 위해 눌러야 하는 버튼들")]
    public CoopButton[] unlockButtons;

    [Header("시간 설정 (반복)")]
    [Tooltip("장애물을 보여주며 시간이 멈춰있는 시간 (초)")]
    public float revealDuration = 3f;
    [Tooltip("암막 상태로 유저들이 움직일 수 있는 시간 (초)")]
    public float darknessDuration = 5f;

    [Header("네트워크 상태 동기화")]
    [SyncVar(hook = nameof(OnGimmickStateChanged))]
    private GimmickState currentState = GimmickState.Before;

    [Header("이벤트 설정")]
    public UnityEvent OnRevealStart;
    public UnityEvent OnDarknessStart;
    public UnityEvent OnGimmickClear;

    private Coroutine loopCoroutine;
    public override void OnStartServer()
    {
        base.OnStartServer();
        CoopButtonUtility.SubscribeButtons(unlockButtons, CheckAllButtons);
    }

    [Server]
    public void StartGimmickSequence()
    {
        if (currentState != GimmickState.Before) return;

        loopCoroutine = StartCoroutine(GimmickLoop());
    }

    [Server]
    private IEnumerator GimmickLoop()
    {
        while (currentState != GimmickState.Clear)
        {
            currentState = GimmickState.Reveal;
            yield return new WaitForSecondsRealtime(revealDuration);

            currentState = GimmickState.Darkness;

          
            CheckAllButtons(null);

            yield return new WaitForSecondsRealtime(darknessDuration);
        }
    }

    [Server]
    private void CheckAllButtons(CoopButton changedButton)
    {
        if (currentState != GimmickState.Darkness) return;
        if (CoopButtonUtility.AreAllPressed(unlockButtons))
        {
            currentState = GimmickState.Clear;
            if (loopCoroutine != null)
            {
                StopCoroutine(loopCoroutine);
                loopCoroutine = null;
            }
        }
    }

    private void OnGimmickStateChanged(GimmickState oldState, GimmickState newState)
    {
        switch (newState)
        {
            case GimmickState.Reveal:
                Time.timeScale = 0f;
                OnRevealStart?.Invoke();
                break;

            case GimmickState.Darkness:
                Time.timeScale = 1f; 
                OnDarknessStart?.Invoke();
                break;

            case GimmickState.Clear:
                Time.timeScale = 1f; 
                OnGimmickClear?.Invoke();
                break;
        }
    }
}