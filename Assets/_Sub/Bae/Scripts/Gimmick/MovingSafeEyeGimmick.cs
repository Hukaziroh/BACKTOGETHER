using UnityEngine;
using Mirror;

public class MovingSafeEyeGimmick : NetworkBehaviour
{
    [System.Serializable]
    public struct BoxStepData
    {
        [Tooltip("이 단계를 조종할 바닥 버튼 (CoopButton)")]
        public CoopButton triggerButton;

        // 🌟 [수정] 리지드바디 없이 박스의 Transform을 직접 제어합니다.
        [Tooltip("이동시킬 박스의 Transform 컴포넌트")]
        public Transform boxTransform;

        [Tooltip("박스에 붙어있는 SafeBoxTrigger 스크립트")]
        public SafeBoxTrigger boxTrigger;

        [Tooltip("버튼을 누르고 있을 때 박스가 가야 할 목표 웨이포인트 (Transform)")]
        public Transform targetWaypoint;
        [HideInInspector] public Vector3 startPosition;
    }

    [Header("★ 1~3번 단계 설정 (인스펙터에서 3개 설정)")]
    public BoxStepData[] boxSteps;

    [Header("★ 최종 눈 감기 버튼 설정")]
    [Tooltip("여기에 완전히 마지막에 배치된 눈 끄기 버튼을 넣어주세요.")]
    public CoopButton finalEyeOffButton;

    [Header("기믹 옵션 설정")]
    [Tooltip("박스들이 이동하는 속도")]
    public float boxMoveSpeed = 4f;

    [Tooltip("체크하면 박스가 이동할 때 처음 높이(Y)를 강제로 고정합니다. (수평 이동 기믹이라면 무조건 켜두세요!)")]
    public bool lockYHeight = true;

    [Header("눈 비주얼 설정")]
    public SpriteRenderer eyeRenderer;
    public Color onColor = Color.red;
    public Color offColor = Color.gray;

    [SyncVar(hook = nameof(OnEyeStatusChanged))]
    private bool isEyePermanentlyOff = false;

    private void Start()
    {
        for (int i = 0; i < boxSteps.Length; i++)
        {
            if (boxSteps[i].boxTransform != null)
            {
                boxSteps[i].startPosition = boxSteps[i].boxTransform.position;
            }
        }
        UpdateVisuals();
    }

    private void Update()
    {
        MoveBoxesBasedOnButtonState();

        if (!isServer) return;
        if (isEyePermanentlyOff) return;
        if (finalEyeOffButton != null && finalEyeOffButton.isPressed)
        {
            isEyePermanentlyOff = true;
        }
    }

    private void MoveBoxesBasedOnButtonState()
    {
        if (boxSteps == null) return;

        foreach (var step in boxSteps)
        {
            if (step.boxTransform == null || step.triggerButton == null) continue;
            Vector3 targetPos = step.triggerButton.isPressed ? step.targetWaypoint.position : step.startPosition;
            if (lockYHeight)
            {
                targetPos.y = step.startPosition.y;
            }
            targetPos.z = step.boxTransform.position.z;
            step.boxTransform.position = Vector3.MoveTowards(
                step.boxTransform.position,
                targetPos,
                boxMoveSpeed * Time.deltaTime
            );
        }
    }
    [ServerCallback]
    private void OnTriggerStay2D(Collider2D other)
    {
        if (isEyePermanentlyOff) return;

        if (other.CompareTag("Player"))
        {
            bool isPlayerSafe = false;

            if (boxSteps != null)
            {
                foreach (var step in boxSteps)
                {
                    if (step.boxTrigger != null && step.boxTrigger.playersInBox.Contains(other.gameObject))
                    {
                        isPlayerSafe = true;
                        break;
                    }
                }
            }

            if (!isPlayerSafe)
            {
                NetworkIdentity identity = other.GetComponent<NetworkIdentity>();
                if (identity != null)
                {
                    Debug.Log($"[{other.name}] 안전 박스 영역 밖으로 이탈! 가시 넉백을 발사합니다.");
                    TargetTriggerKnockback(identity.connectionToClient, other.gameObject);
                }
            }
        }
    }

    [TargetRpc]
    private void TargetTriggerKnockback(NetworkConnection target, GameObject playerObj)
    {
        if (playerObj != null)
        {
            PlayerKnockback knockback = playerObj.GetComponent<PlayerKnockback>();
            if (knockback != null)
            {
                knockback.ApplyKnockbackFromEye(transform.position);
            }
        }
    }

    private void OnEyeStatusChanged(bool oldState, bool newState) { UpdateVisuals(); }

    private void UpdateVisuals()
    {
        if (eyeRenderer != null)
        {
            eyeRenderer.color = isEyePermanentlyOff ? offColor : onColor;
        }
    }
}