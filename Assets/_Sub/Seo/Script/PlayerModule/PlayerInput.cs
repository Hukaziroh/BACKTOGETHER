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
    public bool serverActionPressed { get; private set; }

    public float HorizontalInput => serverHorizontalInput;
    public bool JumpHolding => serverJumpHolding;
    public bool JumpPressedThisFrame => serverJumpPressed;
    public bool JumpReleasedThisFrame => serverJumpReleased;
    public bool ActionPressedThisFrame => serverActionPressed;

    private float lastInput = -999f;
    private bool lastJumpHolding = false;

    void Awake()
    {
        controller = GetComponent<PlayerController>();
        InitializeInputs();
    }

    private void InitializeInputs()
    {
        // 🌟 [조이스틱 추가] 방향키(D-Pad)와 아날로그 스틱 좌우 바인딩 추가
        if (moveAction == null || moveAction.bindings.Count == 0)
        {
            moveAction = new InputAction("Move", InputActionType.Value);
            moveAction.AddCompositeBinding("1DAxis")
                .With("Negative", "<Keyboard>/a")
                .With("Negative", "<Keyboard>/leftArrow")
                .With("Negative", "<Gamepad>/leftStick/left") // 패드 왼쪽 스틱
                .With("Negative", "<Gamepad>/dpad/left")      // 패드 십자키 왼쪽
                .With("Positive", "<Keyboard>/d")
                .With("Positive", "<Keyboard>/rightArrow")
                .With("Positive", "<Gamepad>/leftStick/right")// 패드 오른쪽 스틱
                .With("Positive", "<Gamepad>/dpad/right");    // 패드 십자키 오른쪽
        }

        // 🌟 [조이스틱 추가] 패드의 '아래쪽 버튼' (Xbox: A버튼 / PS: X버튼 / Switch: B버튼)
        if (jumpAction == null || jumpAction.bindings.Count == 0)
        {
            jumpAction = new InputAction("Jump", InputActionType.Button);
            jumpAction.AddBinding("<Keyboard>/space");
            jumpAction.AddBinding("<Keyboard>/w");
            jumpAction.AddBinding("<Keyboard>/upArrow");
            jumpAction.AddBinding("<Gamepad>/buttonSouth"); // 패드 점프
        }

        // 🌟 [조이스틱 추가] 패드의 '왼쪽 버튼' (Xbox: X버튼 / PS: 네모버튼 / Switch: Y버튼)
        if (actionAction == null || actionAction.bindings.Count == 0)
        {
            actionAction = new InputAction("Action", InputActionType.Button);
            actionAction.AddBinding("<Keyboard>/v");
            actionAction.AddBinding("<Gamepad>/buttonWest"); // 패드 액션 (손전등/합체 등)
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

        // 🌟 [핵심 디테일] 아날로그 스틱 데드존 (Stick Drift 방지)
        // 스틱을 놓아도 0.05f 처럼 미세하게 값이 들어와서 캐릭터가 스르륵 미끄러지는 현상을 방지합니다.
        if (Mathf.Abs(rawInput) < 0.15f)
        {
            rawInput = 0f;
        }

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

        // 실제로 점프가 가능한 상태(땅에 닿아있거나 코요테 타임 이내)에서 누른 경우에만 예측 재생한다.
        // 그냥 키를 눌렀다고 무조건 재생하면 공중에서 연타할 때마다 소리가 겹쳐 재생된다.
        if (jPressed && controller.animationModule != null && controller.animationModule.IsJumpableSynced)
        {
            controller.movement.PlayJumpSoundLocal();
        }

        if (rawInput != lastInput || jPressed || jReleased || jHolding != lastJumpHolding || aPressed)
        {
            CmdSendInput(rawInput, jHolding, jPressed, jReleased, aPressed);

            lastInput = rawInput;
            lastJumpHolding = jHolding;
        }
    }

    [Command]
    private void CmdSendInput(float value, bool jumpHolding, bool jumpPressed, bool jumpReleased, bool actionPressed)
    {
        serverHorizontalInput = value;
        serverJumpHolding = jumpHolding;

        if (jumpPressed) serverJumpPressed = true;
        if (jumpReleased) serverJumpReleased = true;
        if (actionPressed) serverActionPressed = true;
    }

    public void ClearInputBuffers()
    {
        serverJumpPressed = false;
        serverJumpReleased = false;
        serverActionPressed = false;
    }
}