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

    [SyncVar]
    private bool syncStunned;

    [SyncVar]
    private float syncVelocityY; // 점프 및 낙하 애니메이션용 Y축 속도

    [SyncVar(hook = nameof(OnDirectionChanged))]
    public float syncDirectionX = 1f;

    [Header("발소리")]
    public AudioClip footstepClip;
    [Range(0f, 1f)] public float footstepVolume = 0.4f;
    public float footstepInterval = 0.35f;
    public float footstepMinDistance = 9f;
    public float footstepMaxDistance = 40f;
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
            clientCoyoteTimer = controller.movement.coyoteTime;
        }
        else if (clientCoyoteTimer > 0f)
        {
            clientCoyoteTimer -= Time.deltaTime;
        }
    }

    private void PlayFootstepSound()
    {
        if (footstepClip == null) return;

        GameObject soundObj = new GameObject("FootstepSound_Temp");
        soundObj.transform.position = transform.position;

        AudioSource source = soundObj.AddComponent<AudioSource>();
        source.clip = footstepClip;
        source.volume = footstepVolume;
        source.spatialBlend = 1f;
        source.rolloffMode = AudioRolloffMode.Linear;
        source.minDistance = footstepMinDistance;
        source.maxDistance = footstepMaxDistance;
        source.Play();

        Destroy(soundObj, footstepClip.length);
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