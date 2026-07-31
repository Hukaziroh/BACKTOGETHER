using UnityEngine;
using Mirror;

public class PlayerAnimation : NetworkBehaviour
{
    private PlayerController controller;

    [Header("Sync Animation Variables")]
    [SyncVar]
    private float syncSpeed;

    [SyncVar]
    private bool syncGrounded;

    [SyncVar]
    private bool syncStunned;

    [SyncVar]
    private float syncVelocityY; // 점프 및 낙하 애니메이션용 Y축 속도

    [SyncVar(hook = nameof(OnDirectionChanged))]
    public float syncDirectionX = 1f;


    void Awake()
    {
        controller = GetComponent<PlayerController>();
    }

    void Update()
    {
        // 1. 서버인 경우에만 애니메이션 데이터를 갱신하여 SyncVar에 반영합니다.
        if (isServer)
        {
            UpdateAnimationServer();
        }

        // 2. 모든 클라이언트(서버 포함)는 동기화된 값으로 애니메이션을 재생합니다.
        ApplyAnimation();
    }

    [Server]
    private void UpdateAnimationServer()
    {
        if (controller == null) return;

        // 이동 속도 계산 (절댓값)
        float speed = Mathf.Abs(controller.input.HorizontalInput);

        syncSpeed = speed;
        syncGrounded = controller.movement.isGrounded;
        syncStunned = controller.knockback.IsStunned;

        float input = controller.input.HorizontalInput;

        // 🌟 [추가된 부분] 리버스 존(좌우 반전) 기믹 적용
        // 무브먼트 쪽과 동일하게, 역방향 구역이라면 애니메이션 방향용 입력값도 뒤집어줍니다.
        if (controller.currentReverseZone != null && !controller.currentReverseZone.isForward)
        {
            if (input != 0)
            {
                input *= -1f;
            }
        }

        // 기절 상태가 아니고 입력이 있을 때만 방향 갱신
        if (input != 0 && !controller.knockback.IsStunned)
        {
            syncDirectionX = input > 0 ? 1f : -1f;
        }
    }

    private void ApplyAnimation()
    {
        if (controller.anim == null)
            return;

        // Animator 파라미터 적용
        controller.anim.SetFloat("Speed", syncSpeed);
        controller.anim.SetBool("isGrounded", syncGrounded);
        controller.anim.SetBool("isStunned", syncStunned);

        // 스프라이트 방향 및 중력 반전 적용
        ApplyScale(syncDirectionX);
    }

    private void OnDirectionChanged(float oldDir, float newDir)
    {
        ApplyScale(newDir);
    }

    private void ApplyScale(float dirX)
    {
        bool inverted = false;

        if (controller != null && controller.gravityModule != null)
        {
            inverted = controller.gravityModule.isGravityInverted;
        }

        transform.localScale = new Vector3(
            dirX,
            inverted ? -1f : 1f,
            1f
        );
    }
}