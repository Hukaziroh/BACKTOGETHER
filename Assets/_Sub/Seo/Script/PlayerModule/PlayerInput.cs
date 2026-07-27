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

    // 서버용 입력 데이터 (버퍼)
    private float serverHorizontalInput;
    public bool serverJumpHolding { get; private set; }  // 🚀 추가됨 (점프 키 누름 유지 상태)
    public bool serverJumpPressed { get; private set; }
    public bool serverJumpReleased { get; private set; }

    public float HorizontalInput => serverHorizontalInput;
    public bool JumpHolding => serverJumpHolding;        // 🚀 추가됨
    public bool JumpPressedThisFrame => serverJumpPressed;
    public bool JumpReleasedThisFrame => serverJumpReleased;
    public bool ActionPressedThisFrame => actionAction.WasPressedThisFrame();

    // 패킷 폭주 방지용 이전 입력값 저장
    private float lastInput = -999f;
    private bool lastJumpHolding = false; // 🚀 유지 상태 변경 감지용

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
                .With("Positive", "<Keyboard>/d")
                .With("Positive", "<Keyboard>/rightArrow");
        }
        if (jumpAction == null || jumpAction.bindings.Count == 0)
        {
            jumpAction = new InputAction("Jump", InputActionType.Button);
            jumpAction.AddBinding("<Keyboard>/space");
            jumpAction.AddBinding("<Keyboard>/w");
            jumpAction.AddBinding("<Keyboard>/upArrow");

        }
        if (actionAction == null || actionAction.bindings.Count == 0)
        {
            actionAction = new InputAction("Action", InputActionType.Button);
            actionAction.AddBinding("<Keyboard>/v");
        }
    }

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
            if (gameObject != controller.combineHandler.bodyTarget) rawInput = 0f;
            else rawInput = controller.combineHandler.GetCombinedHorizontalInput();
        }
        else
        {
            rawInput = moveAction.ReadValue<float>();
        }

        bool jHolding = jumpAction.IsPressed(); // 🚀 추가됨 (유지)
        bool jPressed = jumpAction.WasPressedThisFrame();
        bool jReleased = jumpAction.WasReleasedThisFrame();

        // 입력값이 바뀌거나 틱이 발생했을 때만 서버로 전송
        if (rawInput != lastInput || jPressed || jReleased || jHolding != lastJumpHolding)
        {
            CmdSendInput(rawInput, jHolding, jPressed, jReleased);

            lastInput = rawInput;
            lastJumpHolding = jHolding;
        }
    }

    // 🚀 유저님 추천 방식: 모든 입력 상태를 하나의 Command로 통합
    [Command]
    private void CmdSendInput(float value, bool jumpHolding, bool jumpPressed, bool jumpReleased)
    {
        serverHorizontalInput = value;
        serverJumpHolding = jumpHolding;

        // true가 들어왔을 때만 덮어씌움 (FixedUpdate 처리가 끝날 때까지 버퍼 유지)
        if (jumpPressed) serverJumpPressed = true;
        if (jumpReleased) serverJumpReleased = true;
    }

    public void ClearJumpInput()
    {
        serverJumpPressed = false;
        serverJumpReleased = false;
    }
}