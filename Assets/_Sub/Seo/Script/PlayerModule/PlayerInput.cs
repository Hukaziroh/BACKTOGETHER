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

        float rawInput = inputControls.GamePlay.Move.ReadValue<float>();

        if (Mathf.Abs(rawInput) < 0.15f)
        {
            rawInput = 0f;
        }

        bool jHolding = inputControls.GamePlay.Jump.IsPressed();
        bool jPressed = inputControls.GamePlay.Jump.WasPressedThisFrame();
        bool jReleased = inputControls.GamePlay.Jump.WasReleasedThisFrame();
        bool aPressed = inputControls.GamePlay.Action.WasPressedThisFrame();

        bool isMenuOpen =
            (PauseManager.instance != null && PauseManager.instance.isPaused)
            ||
            (OptionsManager.instance != null &&
             OptionsManager.instance.optionsPanel != null &&
             OptionsManager.instance.optionsPanel.activeSelf)
            ||
            (OptionsManager.instance != null &&
             OptionsManager.instance.keyGuidePanel != null &&
             OptionsManager.instance.keyGuidePanel.activeSelf)
            ||
            (EmojiRadialMenu.Instance != null && EmojiRadialMenu.Instance.IsOpen());

        if (isMenuOpen)
        {
            rawInput = 0f;
            jHolding = false;
            jPressed = false;
            jReleased = false;
            aPressed = false;
        }

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