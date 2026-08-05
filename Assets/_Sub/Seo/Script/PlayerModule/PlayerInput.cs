using UnityEngine;
using UnityEngine.InputSystem;
using Mirror;
using PlayerControls; // 자동 생성된 액션 네임스페이스

public class PlayerInput : NetworkBehaviour
{
    private PlayerController controller;

    // 🌟 New Input System 액션 클래스
    private PlayerControls.PlayerControls inputControls;

    // 🌟 [추가] ESC / Pause / Back 연속 이중 입력 방지용 쿨다운 타이머
    private float pauseCooldownTimer = 0f;

    // ==========================================
    // 🌟 외부 모듈 연동용 액션 프로퍼티
    // ==========================================
    public InputAction jumpAction => inputControls?.GamePlay.Jump;
    public InputAction actionAction => inputControls?.GamePlay.Action;
    public InputAction pauseAction => inputControls?.GamePlay.Pause;
    public InputAction backAction => inputControls?.GamePlay.Back;
    public InputAction spectatePrevAction => inputControls?.GamePlay.SpectatePrev;
    public InputAction spectateNextAction => inputControls?.GamePlay.SpectateNext;
    public InputAction returnToMeAction => inputControls?.GamePlay.ReturnToMe;
    public InputAction restartAction => inputControls?.GamePlay.Restart;
    public InputAction emojiAction => inputControls?.GamePlay.Emoji;

    // 서버 동기화용 입력 버퍼
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
        // 🌟 Time.timeScale 변화(일시정지)에 영향을 받지 않는 쿨다운 차감
        if (pauseCooldownTimer > 0f)
        {
            pauseCooldownTimer -= Time.unscaledDeltaTime;
        }

        if (!isLocalPlayer) return;

        // 현재 일시정지 메뉴 열림 여부 확인
        bool isPaused = PauseManager.instance != null && PauseManager.instance.isPaused;

        // ==========================================
        // 🌟 일시정지 및 뒤로가기 (Pause / Back / 패드 B버튼 / ESC) 처리
        // 🌟 쿨다운(pauseCooldownTimer <= 0) 상태일 때만 입력 수용
        // ==========================================
        if (pauseCooldownTimer <= 0f)
        {
            if (!isPaused)
            {
                // 게임 중 → 메뉴 열기
                if (inputControls.GamePlay.Pause.WasPressedThisFrame())
                {
                    if (PauseManager.instance != null)
                    {
                        PauseManager.instance.PauseGame();
                        pauseCooldownTimer = 0.25f;
                    }
                }
            }
            else
            {
                // 메뉴 안 → 닫기
                if (inputControls.GamePlay.Pause.WasPressedThisFrame() ||
                    inputControls.GamePlay.Back.WasPressedThisFrame())
                {
                    if (OptionsManager.instance != null &&
                        OptionsManager.instance.optionsPanel != null &&
                        OptionsManager.instance.optionsPanel.activeSelf)
                    {
                        PauseManager.instance.CloseOptions();
                    }
                    else if (PauseManager.instance != null)
                    {
                        PauseManager.instance.ResumeGame();
                    }

                    pauseCooldownTimer = 0.25f;
                }
            }
        }

        // ==========================================
        // 🌟 이모티콘 휠 메뉴 조작 (LT 홀드 및 릴리즈)
        // ==========================================
        if (inputControls.GamePlay.Emoji.WasPressedThisFrame())
        {
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

        // 좌우 이동 값 (아날로그 스틱 / 방향키)
        float rawInput = inputControls.GamePlay.Move.ReadValue<float>();

        // 스틱 데드존 처리
        if (Mathf.Abs(rawInput) < 0.15f)
        {
            rawInput = 0f;
        }

        // 버튼 상태 읽기
        bool jHolding = inputControls.GamePlay.Jump.IsPressed();
        bool jPressed = inputControls.GamePlay.Jump.WasPressedThisFrame();
        bool jReleased = inputControls.GamePlay.Jump.WasReleasedThisFrame();
        bool aPressed = inputControls.GamePlay.Action.WasPressedThisFrame();

        // UI 메뉴가 열려있을 때 게임 캐릭터 이동 차단
        bool isMenuOpen = isPaused;
        if (EmojiRadialMenu.Instance != null && EmojiRadialMenu.Instance.IsOpen()) isMenuOpen = true;

        if (isMenuOpen)
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