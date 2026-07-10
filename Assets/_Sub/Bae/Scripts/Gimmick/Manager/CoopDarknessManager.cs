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
    public float darknessDuration = 5f; // 💡 이 시간을 조절해서 난이도를 맞추세요!

    [Header("네트워크 상태 동기화")]
    [SyncVar(hook = nameof(OnGimmickStateChanged))]
    private GimmickState currentState = GimmickState.Before;

    [Header("이벤트 설정")]
    public UnityEvent OnRevealStart;
    public UnityEvent OnDarknessStart;
    public UnityEvent OnGimmickClear;

    // 반복 코루틴을 추적하고 멈추기 위한 변수
    private Coroutine loopCoroutine;

    [Server]
    public void StartGimmickSequence()
    {
        if (currentState != GimmickState.Before) return;

        // 반복 사이클 시작
        loopCoroutine = StartCoroutine(GimmickLoopRoutine());
    }

    [Server]
    private IEnumerator GimmickLoopRoutine()
    {
        // 버튼을 눌러 Clear 상태가 되기 전까지 무한 반복
        while (currentState != GimmickState.Clear)
        {
            // 1. 불 켜고 시간 멈추기
            currentState = GimmickState.Reveal;
            yield return new WaitForSecondsRealtime(revealDuration);

            // (혹시 모를 예외 처리) 그 사이에 클리어됐다면 루프 종료
            if (currentState == GimmickState.Clear) break;

            // 2. 불 끄고 시간 흐르기 (유저들 이동 시작)
            currentState = GimmickState.Darkness;
            yield return new WaitForSecondsRealtime(darknessDuration);
        }
    }

    [ServerCallback]
    private void Update()
    {
        // 시간이 흐르고 있는 암막(Darkness) 상태일 때만 클리어 체크
        if (currentState != GimmickState.Darkness) return;
        if (unlockButtons == null || unlockButtons.Length == 0) return;

        bool allPressed = true;
        foreach (var btn in unlockButtons)
        {
            if (btn == null || !btn.isPressed)
            {
                allPressed = false;
                break;
            }
        }

        // 버튼이 모두 눌렸다면!
        if (allPressed)
        {
            currentState = GimmickState.Clear;

            // 돌아가고 있던 무한 반복 코루틴을 강제로 정지
            if (loopCoroutine != null)
            {
                StopCoroutine(loopCoroutine);
            }
        }
    }

    // 서버의 상태가 변할 때마다 모든 클라이언트에서 자동 실행 (동기화)
    private void OnGimmickStateChanged(GimmickState oldState, GimmickState newState)
    {
        switch (newState)
        {
            case GimmickState.Reveal:
                Time.timeScale = 0f; // 시간 정지
                OnRevealStart?.Invoke();
                break;

            case GimmickState.Darkness:
                Time.timeScale = 1f; // 시간 흐름
                OnDarknessStart?.Invoke();
                break;

            case GimmickState.Clear:
                Time.timeScale = 1f; // 시간 원상복구 확인
                OnGimmickClear?.Invoke();
                break;
        }
    }
}