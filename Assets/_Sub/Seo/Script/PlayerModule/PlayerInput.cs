using UnityEngine;
using UnityEngine.InputSystem;
using Mirror;
using PlayerControls;

public class PlayerInput : NetworkBehaviour
{
    private PlayerController controller;
    private PlayerControls.PlayerControls inputControls;

    // 외부 모듈 연동용 액션 프로퍼티
    public InputAction jumpAction => inputControls?.GamePlay.Jump;
    public InputAction actionAction => inputControls?.GamePlay.Action;
    public InputAction pauseAction => inputControls?.GamePlay.Pause;
    public InputAction backAction => inputControls?.GamePlay.Back;
    public InputAction spectateNextAction => inputControls?.GamePlay.SpectateNext;
    public InputAction returnToMeAction => inputControls?.GamePlay.ReturnToMe;
    public InputAction restartAction => inputControls?.GamePlay.Restart;
    public InputAction emojiAction => inputControls?.GamePlay.Emoji;

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

    // 🌟 [추가됨] 메뉴 닫힘 인풋 누수 방지용 타이머
    private float menuCloseGraceTimer = 0f;

    void Awake()
    {
        controller = GetComponent<PlayerController>();
        inputControls = new PlayerControls.PlayerControls();
    }

    public override void OnStartLocalPlayer()
    {
        base.OnStartLocalPlayer();
        inputControls.GamePlay.Enable();
    }

    void OnDisable()
    {
        if (inputControls != null)
        {
            inputControls.GamePlay.Disable();
        }
    }

    void Update()
    {
        if (!isLocalPlayer) return;

        // 이모지 휠 조작
        if (inputControls.GamePlay.Emoji.WasPressedThisFrame())
        {
            bool isPaused = PauseManager.instance != null && PauseManager.instance.isPaused;
            if (EmojiRadialMenu.Instance != null && !isPaused)
            {
                EmojiRadialMenu.Instance.OpenMenu();
            }
        }

        if (inputControls.GamePlay.Emoji.WasReleasedThisFrame())
        {
            if (EmojiRadialMenu.Instance != null)
            {
                EmojiRadialMenu.Instance.CloseMenu();
            }
        }

        // 이동 및 액션 키 읽기
        float rawInput = inputControls.GamePlay.Move.ReadValue<float>();

        if (Mathf.Abs(rawInput) < 0.15f)
        {
            rawInput = 0f;
        }

        bool jHolding = inputControls.GamePlay.Jump.IsPressed();
        bool jPressed = inputControls.GamePlay.Jump.WasPressedThisFrame();
        bool jReleased = inputControls.GamePlay.Jump.WasReleasedThisFrame();
        bool aPressed = inputControls.GamePlay.Action.WasPressedThisFrame();

        // ==========================================
        // 🌟 메뉴 열림 감지 및 0.15초 유예 시간(Grace Time) 적용
        // ==========================================
        bool isMenuOpen =
            (PauseManager.instance != null && PauseManager.instance.isPaused)
            ||
            (OptionsManager.instance != null &&
             OptionsManager.instance.optionsPanel != null &&
             OptionsManager.instance.optionsPanel.activeSelf)
            ||
            (OptionsManager.instance != null &&
             OptionsManager.instance.IsAnyKeyGuideActive())
            ||
            (EmojiRadialMenu.Instance != null && EmojiRadialMenu.Instance.IsOpen());

        // 메뉴가 켜져 있으면 차단 타이머를 계속 0.15초로 꽉 채움
        if (isMenuOpen)
        {
            menuCloseGraceTimer = 0.15f;
        }
        else if (menuCloseGraceTimer > 0f)
        {
            // 메뉴가 꺼지더라도 0.15초 동안은 서서히 줄어들며 입력을 계속 차단함
            menuCloseGraceTimer -= Time.unscaledDeltaTime;
        }

        // 🌟 isMenuOpen이 아니라 "타이머가 남아있는지"를 기준으로 입력 차단!
        if (menuCloseGraceTimer > 0f)
        {
            rawInput = 0f;
            jHolding = false;
            jPressed = false;
            jReleased = false;
            aPressed = false;
        }

        // 점프 사운드 예측 재생
        if (jPressed && controller.animationModule != null && controller.animationModule.IsJumpableSynced)
        {
            controller.movement.PlayJumpSoundLocal();
            // 서버가 coyoteTimeCounter를 즉시 0으로 소모하는 것과 동일하게,
            // 클라이언트 쪽 예측용 타이머도 바로 소모시켜서 연타 시 중복 재생을 막는다.
            controller.animationModule.ConsumeClientCoyoteTime();
        }

        // 서버 전송
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