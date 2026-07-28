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
    public bool serverJumpHolding { get; private set; }
    public bool serverJumpPressed { get; private set; }
    public bool serverJumpReleased { get; private set; }

    // 🌟 [추가됨] Action(V키)도 점프처럼 서버 버퍼를 거치게 함!
    public bool serverActionPressed { get; private set; }

    public float HorizontalInput => serverHorizontalInput;
    public bool JumpHolding => serverJumpHolding;
    public bool JumpPressedThisFrame => serverJumpPressed;
    public bool JumpReleasedThisFrame => serverJumpReleased;
    public bool ActionPressedThisFrame => serverActionPressed; // 🌟 서버 버퍼 참조

    private float lastInput = -999f;
    private bool lastJumpHolding = false;

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

        float rawInput = moveAction.ReadValue<float>();

        bool jHolding = jumpAction.IsPressed();
        bool jPressed = jumpAction.WasPressedThisFrame();
        bool jReleased = jumpAction.WasReleasedThisFrame();
        bool aPressed = actionAction.WasPressedThisFrame();

        bool isMenuOpen = false;
        if (PauseManager.instance != null && PauseManager.instance.isPaused) isMenuOpen = true;
        if (EmojiRadialMenu.Instance != null && EmojiRadialMenu.Instance.IsOpen()) isMenuOpen = true;

        if (isMenuOpen)
        {
            rawInput = 0f;
            jHolding = false;
            jPressed = false;
            jReleased = false;
            aPressed = false;
        }

        if (rawInput != lastInput || jPressed || jReleased || jHolding != lastJumpHolding || aPressed)
        {
            CmdSendInput(rawInput, jHolding, jPressed, jReleased, aPressed);

            lastInput = rawInput;
            lastJumpHolding = jHolding;
        }
    }

    // 🌟 [수정됨] Action 정보(actionPressed) 추가 전송
    [Command]
    private void CmdSendInput(float value, bool jumpHolding, bool jumpPressed, bool jumpReleased, bool actionPressed)
    {
        serverHorizontalInput = value;
        serverJumpHolding = jumpHolding;

        // true가 들어왔을 때만 덮어씌움 (FixedUpdate 처리가 끝날 때까지 버퍼 유지)
        if (jumpPressed) serverJumpPressed = true;
        if (jumpReleased) serverJumpReleased = true;
        if (actionPressed) serverActionPressed = true; // 🌟 Action 버퍼 저장
    }

    // 🌟 [수정됨] 이름 변경: 모든 단발성 입력 버퍼 초기화
    public void ClearInputBuffers()
    {
        serverJumpPressed = false;
        serverJumpReleased = false;
        serverActionPressed = false;
    }
}