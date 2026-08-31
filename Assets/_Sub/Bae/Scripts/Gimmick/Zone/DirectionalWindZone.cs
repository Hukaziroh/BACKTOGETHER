using System.Collections.Generic;
using UnityEngine;

/// <summary>
/// 좌 → (잠깐 무풍) → 우 → (잠깐 무풍) → 좌 ... 를 반복하는 바람 구역.
/// 방향이 바뀔 때 바람 세기가 0에서 목표치까지 서서히 올라갔다가(램프),
/// 쉬는 시간에는 다시 0으로 잦아듭니다.
/// 기존 <see cref="BlizzardZone"/> 와 동일하게 PlayerMovement.windVelocity 를 사용합니다.
///
/// BlizzardZone 처럼 순수 MonoBehaviour 입니다. 플레이어 이동 물리는 서버에서만 돌기 때문에
/// 실제로 바람을 적용하는 것은 서버 인스턴스이며, 클라이언트 인스턴스의 windVelocity 쓰기는
/// 물리에 반영되지 않습니다(연출용으로만 참고). 위상 타이머는 각 피어가 로컬로 돌립니다.
/// </summary>
public class DirectionalWindZone : MonoBehaviour
{
    [Header("바람 설정")]
    [Tooltip("바람 최대 세기(절댓값). 램프가 끝나면 windVelocity 가 이 값에 방향 부호(-1/+1)를 곱한 값이 됩니다.")]
    public float windStrength = 3f;

    [Tooltip("한 방향으로 바람이 부는 시간(초). 램프 시간 포함.")]
    public float blowDuration = 3f;

    [Tooltip("방향이 바뀌기 전, 바람이 아예 없는 쉬는 시간(초).")]
    public float restDuration = 1f;

    [Tooltip("0에서 최대 세기까지 서서히 강해지는 데 걸리는 시간(초). 0이면 즉시.")]
    public float rampUpTime = 1.5f;

    [Tooltip("바람이 0으로 잦아드는 데 걸리는 시간(초). 0이면 즉시.")]
    public float rampDownTime = 0.75f;

    [Tooltip("시작 방향. 음수면 왼쪽, 양수면 오른쪽.")]
    public float initialDirection = -1f;

    // -1 = 왼쪽으로 부는 바람, +1 = 오른쪽으로 부는 바람
    private float windDirection = -1f;
    private bool isResting;
    private float timer;

    // 램프가 적용된 현재 실제 바람 속도
    private float currentVelocity;

    /// <summary>현재 바람 방향(-1 왼쪽 / +1 오른쪽). 쉬는 중이어도 다음에 불 방향을 가리킵니다.</summary>
    public float WindDirection => windDirection;

    /// <summary>쉬는 시간(무풍) 여부.</summary>
    public bool IsResting => isResting;

    /// <summary>램프가 적용된, 지금 이 순간 실제로 걸리는 바람 속도.</summary>
    public float CurrentWindVelocity => currentVelocity;

    /// <summary>이번 위상의 목표 바람 속도(램프 도착점). 쉬는 중이면 0.</summary>
    private float TargetVelocity => isResting ? 0f : windDirection * windStrength;

    private readonly HashSet<PlayerController> playersInside = new HashSet<PlayerController>();

    private void OnEnable()
    {
        windDirection = Mathf.Sign(Mathf.Approximately(initialDirection, 0f) ? -1f : initialDirection);
        isResting = false;
        timer = 0f;
        currentVelocity = 0f;
    }

    private void Update()
    {
        AdvancePhase();
        AdvanceRamp();
        ApplyToPlayers();
    }

    private void AdvancePhase()
    {
        timer += Time.deltaTime;

        if (isResting)
        {
            if (restDuration <= 0f || timer >= restDuration)
            {
                timer = restDuration > 0f ? timer - restDuration : 0f;
                isResting = false;
                windDirection = -windDirection;
            }
        }
        else
        {
            if (blowDuration > 0f && timer >= blowDuration)
            {
                timer -= blowDuration;
                isResting = true;
            }
        }
    }

    private void AdvanceRamp()
    {
        float target = TargetVelocity;
        bool easingDown = Mathf.Abs(target) < Mathf.Abs(currentVelocity) || !Mathf.Approximately(Mathf.Sign(target), Mathf.Sign(currentVelocity));
        float rampTime = easingDown ? rampDownTime : rampUpTime;

        if (rampTime <= 0f || windStrength <= 0f)
        {
            currentVelocity = target;
            return;
        }

        float rate = windStrength / rampTime;
        currentVelocity = Mathf.MoveTowards(currentVelocity, target, rate * Time.deltaTime);
    }

    private void ApplyToPlayers()
    {
        playersInside.RemoveWhere(p => p == null);
        foreach (var player in playersInside)
            player.windVelocity = currentVelocity;
    }

    private void OnTriggerEnter2D(Collider2D other)
    {
        if (!other.CompareTag("Player"))
            return;
        if (!other.TryGetComponent<PlayerController>(out var player))
            return;

        if (playersInside.Add(player))
            player.windVelocity = currentVelocity;
    }

    private void OnTriggerExit2D(Collider2D other)
    {
        if (!other.CompareTag("Player"))
            return;
        if (!other.TryGetComponent<PlayerController>(out var player))
            return;

        if (playersInside.Remove(player) && player != null)
            player.windVelocity = 0f;
    }
}
