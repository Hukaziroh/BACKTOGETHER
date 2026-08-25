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

    [Header("기믹 상태 (리버스 존)")]
    public IntervalTrigger currentReverseZone { get; private set; } // 추가됨

    [Header("사운드 공통 설정 (점프/피격/발소리 공용)")]
    // 카메라가 항상 (0, 1.5, -10) 오프셋으로 떨어져 있어서(CameraFollow 참고),
    // 어떤 캐릭터 사운드든 리스너와의 거리 계산 기준은 동일하다. 그래서 사운드별로
    // 따로 두지 않고 여기 한 곳에서만 관리한다.
    public float soundMinDistance = 9f;
    public float soundMaxDistance = 40f;
    [Range(0f, 1f)] public float otherPlayerVolumeMultiplier = 0.25f;
    // 클립 자체는 여기 한 군데(에셋)에서만 관리 - 어떤 프리팹도 따로 안 들고 있음
    public PlayerSoundLibrary soundLibrary;

  
    void Awake()
    {
        rb = GetComponent<Rigidbody2D>();

        rb.bodyType = RigidbodyType2D.Dynamic;
        rb.interpolation = RigidbodyInterpolation2D.Interpolate;

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

    // ==========================================
    // 리버스 존 상태 참조 함수 추가
    // ==========================================
    public void SetCurrentReverseZone(IntervalTrigger zone)
    {
        currentReverseZone = zone;
        movement?.CancelReverseZoneExitTransition();

        if (zone != null && !zone.isForward)
            movement?.BeginReverseZoneEntryTransition();
        else
            movement?.CancelReverseZoneEntryTransition();
    }

    public void ClearCurrentReverseZone()
    {
        bool preserveCurrentDirection =
            movement != null && movement.ShouldReverseHorizontalInput;

        currentReverseZone = null;
        movement?.CancelReverseZoneEntryTransition();

        if (preserveCurrentDirection)
            movement?.BeginReverseZoneExitTransition();
        else
            movement?.CancelReverseZoneExitTransition();
    }
}
