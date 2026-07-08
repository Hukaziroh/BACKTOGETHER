using Mirror;
using UnityEngine;
using UnityEngine.InputSystem;

public enum CombineRole
{
    None, Move_Left, Move_Right, Move, Jump, Action
}

public class PlayerCombineHandler : NetworkBehaviour
{
    [Header("합체 상태")]
    [SyncVar] public bool isCombined = false;
    [SyncVar] public CombineRole myRole = CombineRole.None;
    [SyncVar] public GameObject bodyTarget;

    [HideInInspector] public float ghostLeftInput = 0f;
    [HideInInspector] public float ghostRightInput = 0f;
    [HideInInspector] public float ghostDuoInput = 0f;

    private float lastSentMove = 0f;

    private SpriteRenderer spriteRenderer;
    private Collider2D col;
    private Rigidbody2D rb;

    [Header("입력 설정")]
    public InputAction moveAction;
    public InputAction jumpAction;
    public InputAction actionAction;

    void Awake()
    {
        spriteRenderer = GetComponent<SpriteRenderer>();
        col = GetComponent<Collider2D>();
        rb = GetComponent<Rigidbody2D>();

        // 입력 바인딩 초기화
        if (moveAction == null || moveAction.bindings.Count == 0)
        {
            moveAction = new InputAction("CombineMove", InputActionType.Value);
            moveAction.AddCompositeBinding("1DAxis")
                .With("Negative", "<Keyboard>/a").With("Negative", "<Keyboard>/leftArrow")
                .With("Positive", "<Keyboard>/d").With("Positive", "<Keyboard>/rightArrow");
        }

        if (jumpAction == null || jumpAction.bindings.Count == 0)
        {
            jumpAction = new InputAction("CombineJump", InputActionType.Button);
            jumpAction.AddBinding("<Keyboard>/space");
            jumpAction.AddBinding("<Keyboard>/w");
            jumpAction.AddBinding("<Keyboard>/upArrow");
        }

        if (actionAction == null || actionAction.bindings.Count == 0)
        {
            actionAction = new InputAction("CombineAction", InputActionType.Button);
            actionAction.AddBinding("<Keyboard>/s");
            actionAction.AddBinding("<Keyboard>/downArrow");
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
        if (isLocalPlayer)
        {
            moveAction.Disable(); jumpAction.Disable(); actionAction.Disable();
        }
    }

    [Server]
    public void StartCombineMode(CombineRole role, GameObject body)
    {
        isCombined = true;
        myRole = role;
        bodyTarget = body;
        RpcApplyCombineVisual(body);
    }

    [ClientRpc]
    private void RpcApplyCombineVisual(GameObject body)
    {
        if (gameObject != body)
        {
            spriteRenderer.enabled = false;
            col.enabled = false;
            rb.simulated = false;
        }

        if (isLocalPlayer && body != null)
        {
            Camera mainCam = Camera.main;
            if (mainCam != null)
            {
                CameraFollow cam = mainCam.GetComponent<CameraFollow>();
                if (cam != null) cam.target = body.transform;
            }
        }
    }

    void Update()
    {
        if (!isLocalPlayer || !isCombined) return;

        if (gameObject != bodyTarget)
        {
            float currentMove = 0f;
            float inputVal = moveAction.ReadValue<float>();

            if (myRole == CombineRole.Move_Left && inputVal < 0) currentMove = -1f;
            else if (myRole == CombineRole.Move_Right && inputVal > 0) currentMove = 1f;
            else if (myRole == CombineRole.Move) currentMove = inputVal;

            if (currentMove != lastSentMove)
            {
                lastSentMove = currentMove;
                CmdSendMoveState(bodyTarget, currentMove, myRole);
            }
            if ((myRole == CombineRole.Jump || myRole == CombineRole.Move) && jumpAction.WasPressedThisFrame())
            {
                CmdSendJumpToBody(bodyTarget);
            }

            if (myRole == CombineRole.Action && actionAction.WasPressedThisFrame())
            {
                CmdSendActionToBody(bodyTarget);
            }
        }
        else
        {
            if (myRole == CombineRole.Jump && jumpAction.WasPressedThisFrame())
                GetComponent<PlayerController>().CallCombinedJump();

            if (myRole == CombineRole.Action && actionAction.WasPressedThisFrame())
                GetComponent<PlayerController>().CallCombinedAction();
        }
    }

    void LateUpdate()
    {
        if (isCombined && bodyTarget != null && gameObject != bodyTarget)
        {
            transform.position = bodyTarget.transform.position;
        }
    }

    public float GetCombinedHorizontalInput()
    {
        float totalInput = 0f;
        float inputVal = moveAction.ReadValue<float>();

        if (myRole == CombineRole.Move_Left && inputVal < 0) totalInput += -1f;
        if (myRole == CombineRole.Move_Right && inputVal > 0) totalInput += 1f;
        if (myRole == CombineRole.Move) totalInput += inputVal;

        totalInput += ghostLeftInput;
        totalInput += ghostRightInput;
        totalInput += ghostDuoInput;

        return Mathf.Clamp(totalInput, -1f, 1f);
    }

    [Command]
    private void CmdSendMoveState(GameObject body, float moveValue, CombineRole role)
    {
        if (body == null) return;
        body.GetComponent<PlayerCombineHandler>().TargetReceiveMoveState(body.GetComponent<NetworkIdentity>().connectionToClient, moveValue, role);
    }

    [TargetRpc]
    private void TargetReceiveMoveState(NetworkConnection target, float moveValue, CombineRole role)
    {
        if (role == CombineRole.Move_Left) ghostLeftInput = moveValue;
        else if (role == CombineRole.Move_Right) ghostRightInput = moveValue;
        else if (role == CombineRole.Move) ghostDuoInput = moveValue;
    }

    [Command]
    private void CmdSendJumpToBody(GameObject body)
    {
        if (body == null) return;
        body.GetComponent<PlayerCombineHandler>().TargetDoJump(body.GetComponent<NetworkIdentity>().connectionToClient);
    }

    [TargetRpc]
    public void TargetDoJump(NetworkConnection target) { GetComponent<PlayerController>().CallCombinedJump(); }

    [Command]
    private void CmdSendActionToBody(GameObject body)
    {
        if (body == null) return;
        body.GetComponent<PlayerCombineHandler>().TargetDoAction(body.GetComponent<NetworkIdentity>().connectionToClient);
    }

    [TargetRpc]
    public void TargetDoAction(NetworkConnection target) { GetComponent<PlayerController>().CallCombinedAction(); }

    [Server]
    public void StopCombineMode(Vector3 releasePosition)
    {
        isCombined = false;
        myRole = CombineRole.None;
        bodyTarget = null;

        ghostLeftInput = 0f;
        ghostRightInput = 0f;
        ghostDuoInput = 0f;

        transform.position = releasePosition;
        RpcApplySeparateVisual(releasePosition);
    }

    [ClientRpc]
    private void RpcApplySeparateVisual(Vector3 releasePosition)
    {
        spriteRenderer.enabled = true;
        col.enabled = true;
        rb.simulated = true;

        if (isLocalPlayer)
        {
            transform.position = releasePosition;
            rb.position = releasePosition;
            rb.linearVelocity = Vector2.zero;

            Camera mainCam = Camera.main;
            if (mainCam != null)
            {
                CameraFollow cam = mainCam.GetComponent<CameraFollow>();
                if (cam != null) cam.target = transform;
            }
        }
    }
}