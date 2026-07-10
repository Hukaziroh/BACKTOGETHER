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

    [Header("기믹: 좌우반전")]
    public bool isReversedControl = false;
    private float reverseTimer = 0f;

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
            jumpAction.AddBinding("<Keyboard>/space");
            jumpAction.AddBinding("<Keyboard>/w");
            jumpAction.AddBinding("<Keyboard>/upArrow");
            jumpAction.AddBinding("<Gamepad>/buttonSouth");
        }

        if (actionAction == null || actionAction.bindings.Count == 0)
        {
            actionAction = new InputAction("Action", InputActionType.Button);
            actionAction.AddBinding("<Keyboard>/v");
        }
    }

    public override void OnStartLocalPlayer()
    {
        moveAction.Enable();
        jumpAction.Enable();
        actionAction.Enable();
    }

    void OnDisable()
    {
        if (isLocalPlayer)
        {
            moveAction.Disable();
            jumpAction.Disable();
            actionAction.Disable();
        }
    }

    void Update()
    {
        if (!isLocalPlayer) return;

        // 합체 상태 체크
        if (controller.combineHandler != null && controller.combineHandler.isCombined)
        {
            if (gameObject != controller.combineHandler.bodyTarget)
            {
                HorizontalInput = 0f;
                return;
            }
            HorizontalInput = controller.combineHandler.GetCombinedHorizontalInput();
        }
        else
        {
            HorizontalInput = moveAction.ReadValue<float>();
        }

        if (isReversedControl)
        {
            HorizontalInput *= -1f;
            reverseTimer -= Time.deltaTime;
            if (reverseTimer <= 0f) StopReverseControl();
        }
    }

    public void StartReverseControl(float duration)
    {
        if (!isLocalPlayer) return;
        isReversedControl = true;
        reverseTimer = duration;
    }

    public void StopReverseControl()
    {
        if (!isLocalPlayer) return;
        isReversedControl = false;
        reverseTimer = 0f;
    }
}