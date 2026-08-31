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
    public bool IsGroundedSynced => syncGrounded;

    // 코요테 타임 동안은 syncGrounded가 이미 false라서, 점프 사운드 예측 재생용으로
    // 서버의 coyoteTime을 클라이언트에서도 흉내내서 "아직 점프 가능한 구간"을 판단한다.
    private float clientCoyoteTimer = 0f;
    public bool IsJumpableSynced => syncGrounded || clientCoyoteTimer > 0f;

    // 예측 재생으로 점프를 이미 소모했는데 syncGrounded가 서버에서 아직 false로
    // 안 내려온 상태라면, 그동안은 UpdateClientCoyoteTimer가 매 프레임 타이머를
    // 다시 꽉 채우는 걸 막는다. syncGrounded가 실제로 false로 갱신되면 해제된다.
    private bool coyoteConsumedPendingSync = false;

    // 예측 재생으로 점프 사운드를 이미 재생한 순간 호출해서, 서버가 coyoteTimeCounter를
    // 0으로 소모 처리하는 것과 똑같이 클라이언트 쪽 타이머도 즉시 소모시킨다.
    // 이걸 안 하면 syncGrounded가 아직 갱신되기 전이거나 코요테 타임이 남아있는 동안
    // 연타할 때 IsJumpableSynced가 계속 true로 남아서 예측 사운드가 중복 재생된다.
    public void ConsumeClientCoyoteTime()
    {
        clientCoyoteTimer = 0f;
        coyoteConsumedPendingSync = true;
    }

    [SyncVar]
    private bool syncStunned;

    [SyncVar]
    private float syncVelocityY; // 점프 및 낙하 애니메이션용 Y축 속도

    [SyncVar(hook = nameof(OnDirectionChanged))]
    public float syncDirectionX = 1f;

    [Header("발소리")]
    [Range(0f, 1f)] public float footstepVolume = 0.4f;
    public float footstepInterval = 0.35f;
    private float footstepTimer = 0f;


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

        // 3. 걷는 소리도 동기화된 값 기준으로 각자 클라이언트에서 재생합니다.
        //    (본인/타인 구분 없이 동일 로직 - 입력 전송을 기다리지 않아도 되고, 3D 위치 기반이라 거리감쇠도 자동 적용됨)
        UpdateFootsteps();

        // 4. 코요테 타임 동안의 점프 사운드 예측 재생을 위한 로컬 타이머 갱신
        UpdateClientCoyoteTimer();
    }

    private void UpdateFootsteps()
    {
        bool isWalking = syncGrounded && !syncStunned && syncSpeed > 0.1f;

        // 멈춰있어도 타이머를 0으로 리셋하지 않는다.
        // 방향을 빠르게 전환하거나 입력이 한 프레임 끊기는 것만으로 타이머가 리셋되면
        // 걷기가 재개되자마자 즉시 재생 -> 짧은 간격으로 계속 재생되어 겹쳐 들리게 된다.
        if (footstepTimer > 0f)
        {
            footstepTimer -= Time.deltaTime;
        }

        if (isWalking && footstepTimer <= 0f)
        {
            PlayFootstepSound();
            footstepTimer = footstepInterval;
        }
    }

    private void UpdateClientCoyoteTimer()
    {
        if (syncGrounded)
        {
            if (!coyoteConsumedPendingSync)
            {
                clientCoyoteTimer = controller.movement.coyoteTime;
            }
        }
        else
        {
            // 서버 상태가 따라잡혀서 실제로 공중에 뜬 게 확인되면, 다음 착지부터는 다시 정상 갱신
            coyoteConsumedPendingSync = false;

            if (clientCoyoteTimer > 0f)
            {
                clientCoyoteTimer -= Time.deltaTime;
            }
        }
    }

    private void PlayFootstepSound()
    {
        AudioClip clip = controller.soundLibrary != null ? controller.soundLibrary.footstepClip : null;
        float volume = isLocalPlayer ? footstepVolume : footstepVolume * controller.otherPlayerVolumeMultiplier;
        PlayerSoundUtility.PlayPositional(transform.position, clip, volume, controller.soundMinDistance, controller.soundMaxDistance);
    }

    [Server]
    private void UpdateAnimationServer()
    {
        if (controller == null) return;

        PlayerCombineHandler combineHandler = controller.combineHandler;

        float animationInput;

        if (combineHandler != null && combineHandler.isCombined)
        {
            animationInput = combineHandler.GetServerCombinedHorizontalInput();
        }
        else
        {
            animationInput = controller.input.HorizontalInput;
        }

        syncSpeed = Mathf.Abs(animationInput);

        syncGrounded = controller.movement.isGrounded;
        syncStunned = controller.knockback.IsStunned;

        float input = animationInput;

        if (controller.movement.ShouldReverseHorizontalInput)
        {
            if (input != 0)
                input *= -1f;
        }
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
