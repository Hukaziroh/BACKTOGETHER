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
    public bool isReversed { get; private set; } = false;
    private Coroutine reverseToggleCoroutine;

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
            actionAction.AddBinding("<Keyboard>/s");
            actionAction.AddBinding("<Keyboard>/downArrow");
            actionAction.AddBinding("<Gamepad>/buttonEast");
        }
    }

    void OnEnable()
    {
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

        if (isReversed)
        {
            rawInput *= -1f;
        }

        DistanceJoint2D ropeJoint = GetComponent<DistanceJoint2D>();

        if (ropeJoint != null && controller.movement != null)
        {
            Rigidbody2D rb = GetComponent<Rigidbody2D>();

            if (!controller.movement.isGrounded && rb.linearVelocity.y <= 0.1f)
            {
                rawInput = 0f;
            }
        }

        HorizontalInput = rawInput;
    }

    public void StartReverseToggle(float interval)
    {
        if (!isLocalPlayer) return;

        if (reverseToggleCoroutine != null)
        {
            StopCoroutine(reverseToggleCoroutine);
        }

        reverseToggleCoroutine = StartCoroutine(ReverseToggleRoutine(interval));
    }

    public void StopReverseToggle()
    {
        if (!isLocalPlayer) return;

        if (reverseToggleCoroutine != null)
        {
            StopCoroutine(reverseToggleCoroutine);
            reverseToggleCoroutine = null;
        }

        isReversed = false;
        Debug.Log("반전 구역 이탈: 조작이 정상으로 돌아옵니다.");
    }

    private System.Collections.IEnumerator ReverseToggleRoutine(float interval)
    {
        isReversed = false;
        Debug.Log($"반전 구역 진입: {interval}초 뒤부터 조작이 주기적으로 바뀝니다.");

        while (true)
        {
            yield return new WaitForSeconds(interval);

            isReversed = !isReversed;

            if (isReversed)
                Debug.Log("🚨 조작 방향 [역방향]으로 변경!");
            else
                Debug.Log("🟢 조작 방향 [정방향]으로 복구!");
        }
    }
}