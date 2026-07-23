using UnityEngine;
using Mirror;

[RequireComponent(typeof(Rigidbody2D), typeof(Animator), typeof(BoxCollider2D))]
public class PlayerController : NetworkBehaviour
{
    [Header("Core Components")]
    public Rigidbody2D rb { get; private set; }
    public BoxCollider2D bodyCollider { get; private set; }
    public Animator anim { get; private set; }

    [Header("External Modules (기믹)")]
    public PlayerGravityController gravityModule { get; private set; }
    public PlayerCombineHandler combineHandler { get; private set; }
    public PlayerSyncJump syncJumpHandler { get; private set; }

    [Header("Internal Modules (분리된 기능들)")]
    public PlayerInput input { get; private set; }
    public PlayerMovement movement { get; private set; }
    public PlayerKnockback knockback { get; private set; }
    public PlayerRespawn respawn { get; private set; }
    public PlayerFlashlight flashlight { get; private set; }
    public PlayerAnimation animationModule { get; private set; }

    void Awake()
    {
        rb = GetComponent<Rigidbody2D>();
        anim = GetComponent<Animator>();
        bodyCollider = GetComponent<BoxCollider2D>();

        gravityModule = GetComponent<PlayerGravityController>();
        combineHandler = GetComponent<PlayerCombineHandler>();
        syncJumpHandler = GetComponent<PlayerSyncJump>();

        input = GetComponent<PlayerInput>();
        movement = GetComponent<PlayerMovement>();
        knockback = GetComponent<PlayerKnockback>();
        respawn = GetComponent<PlayerRespawn>();
        flashlight = GetComponent<PlayerFlashlight>();
        animationModule = GetComponent<PlayerAnimation>();
    }

    void Start()
    {
        if (!isLocalPlayer)
        {
            rb.bodyType = RigidbodyType2D.Kinematic;
            rb.interpolation = RigidbodyInterpolation2D.Interpolate;
        }
    }

    public override void OnStartLocalPlayer()
    {
        base.OnStartLocalPlayer();
        Camera mainCam = Camera.main;
        if (mainCam != null)
        {
            CameraFollow cam = mainCam.GetComponent<CameraFollow>() ?? mainCam.gameObject.AddComponent<CameraFollow>();
            cam.target = transform;
        }
    }

    // 💡 [핵심 리팩토링]: localPlayer의 Update 실행 순서를 중앙 통제 (Race Condition 및 프레임 밀림 방지)
    void Update()
    {
        if (!isLocalPlayer) return;

        // 1. 키보드/패드 입력 최우선 수집
        if (input != null) input.CustomUpdate();

        // 2. 입력 기반 기능 업데이트
        if (respawn != null) respawn.CustomUpdate();
        if (flashlight != null) flashlight.CustomUpdate();
        if (knockback != null) knockback.CustomUpdate();

        // 3. 최신 입력 및 위치를 바탕으로 애니메이션 최종 갱신
        if (animationModule != null) animationModule.CustomUpdate();
    }

    // 💡 [핵심 리팩토링]: localPlayer의 물리 연산 순서 중앙 통제
    void FixedUpdate()
    {
        if (!isLocalPlayer) return;

        if (movement != null) movement.CustomFixedUpdate();
        if (knockback != null) knockback.CustomFixedUpdate();
    }

    public Vector3 currentSpawnPoint
    {
        get => respawn.currentSpawnPoint;
        set => respawn.currentSpawnPoint = value;
    }

    public UnityEngine.InputSystem.InputAction jumpAction => input.jumpAction;

    public float windVelocity
    {
        get => movement.windVelocity;
        set => movement.windVelocity = value;
    }

    public void CallCombinedJump() => movement.CallCombinedJump();
    public void ApplyShortJump() => movement.ApplyShortJump();
    public void CallCombinedAction() => movement.CallCombinedAction();

    public void StartReverseToggle(float interval) => input.StartReverseToggle(interval);
    public void StopReverseToggle() => input.StopReverseToggle();
}