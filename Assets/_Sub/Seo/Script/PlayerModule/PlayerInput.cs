using UnityEngine;
using UnityEngine.InputSystem;
using Mirror;

public class PlayerInput : NetworkBehaviour
{
    private PlayerController controller;

    [Header("입력 설정")]
    public InputAction moveAction;
    public InputAction jumpAction;
    public InputAction actionAction;

    public float HorizontalInput { get; private set; }
    public bool JumpPressedThisFrame => jumpAction.WasPressedThisFrame();
    public bool JumpReleasedThisFrame => jumpAction.WasReleasedThisFrame();
    public bool ActionPressedThisFrame => actionAction.WasPressedThisFrame();

    void Awake()
    {
        controller = GetComponent<PlayerController>();
        InitializeInputs();
    }

    private void InitializeInputs()
    {
        if (moveAction == null || moveAction.bindings.Count == 0)
        {
            moveAction = new InputAction("Move", InputActionType.Value);
            moveAction.AddCompositeBinding("1DAxis")
                .With("Negative", "<Keyboard>/a")
                .With("Negative", "<Keyboard>/leftArrow")
                .With("Negative", "<Gamepad>/dpad/left")
                .With("Negative", "<Gamepad>/leftStick/left")
                .With("Positive", "<Keyboard>/d")
                .With("Positive", "<Keyboard>/rightArrow")
                .With("Positive", "<Gamepad>/dpad/right")
                .With("Positive", "<Gamepad>/leftStick/right");
        }

        if (jumpAction == null || jumpAction.bindings.Count == 0)
        {
            jumpAction = new InputAction("Jump", InputActionType.Button);
            jumpAction.AddBinding("<Keyboard>/w");
            jumpAction.AddBinding("<Keyboard>/upArrow");
            jumpAction.AddBinding("<Keyboard>/space");
            jumpAction.AddBinding("<Gamepad>/buttonSouth");
        }

        if (actionAction == null || actionAction.bindings.Count == 0)
        {
            actionAction = new InputAction("Action", InputActionType.Button);
            actionAction.AddBinding("<Keyboard>/v");
            actionAction.AddBinding("<Gamepad>/buttonEast");
        }
    }

    // 최적화: OnEnable이 아닌 로컬 플레이어 시작 시점에 입력을 활성화해야 고스트 입력 방지 가능
    public override void OnStartLocalPlayer()
    {
        base.OnStartLocalPlayer();
        moveAction.Enable();
        jumpAction.Enable();
        actionAction.Enable();
    }

    void OnDisable()
    {
        moveAction.Disable();
        jumpAction.Disable();
        actionAction.Disable();
    }

    void Update()
    {
        if (!isLocalPlayer) return;

        float rawInput = 0f;

        if (controller.combineHandler != null && controller.combineHandler.isCombined)
        {
            if (gameObject != controller.combineHandler.bodyTarget)
            {
                HorizontalInput = 0f;
                return;
            }
            rawInput = controller.combineHandler.GetCombinedHorizontalInput();
        }
        else
        {
            rawInput = moveAction.ReadValue<float>();
        }

        // 🎯 수정된 부분: Player가 독자적으로 시간을 재지 않고, 현재 들어와 있는 존(서버)의 방향 상태를 그대로 따라갑니다.
        if (controller.currentReverseZone != null && !controller.currentReverseZone.isForward)
        {
            rawInput *= -1f; // 역방향일 경우 입력값 뒤집기
        }

        HorizontalInput = rawInput;
    }
}