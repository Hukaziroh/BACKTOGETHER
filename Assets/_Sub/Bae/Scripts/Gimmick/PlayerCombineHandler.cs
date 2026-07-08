using UnityEngine;
using Mirror;
using UnityEngine.InputSystem;

public class PlayerCombineHandler : NetworkBehaviour
{
    [Header("합체 상태")]
    [SyncVar] public bool isCombined = false;
    [SyncVar] public string myRole = "";
    [SyncVar] public GameObject bodyTarget;

    [HideInInspector] public float ghostLeftInput = 0f;
    [HideInInspector] public float ghostRightInput = 0f;
    [HideInInspector] public float ghostDuoInput = 0f;

    private float lastSentMove = 0f;

    private SpriteRenderer spriteRenderer;
    private Collider2D col;
    private Rigidbody2D rb;

    void Awake()
    {
        spriteRenderer = GetComponent<SpriteRenderer>();
        col = GetComponent<Collider2D>();
        rb = GetComponent<Rigidbody2D>();
    }

    [Server]
    public void StartCombineMode(string role, GameObject body)
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
        if (Keyboard.current == null) return;

        if (gameObject != bodyTarget)
        {
            float currentMove = 0f;

            if (myRole == "Move_Left" && (Keyboard.current.aKey.isPressed || Keyboard.current.leftArrowKey.isPressed)) currentMove = -1f;
            else if (myRole == "Move_Right" && (Keyboard.current.dKey.isPressed || Keyboard.current.rightArrowKey.isPressed)) currentMove = 1f;
            else if (myRole == "Move")
            {
                if (Keyboard.current.aKey.isPressed || Keyboard.current.leftArrowKey.isPressed) currentMove = -1f;
                else if (Keyboard.current.dKey.isPressed || Keyboard.current.rightArrowKey.isPressed) currentMove = 1f;
            }

            if (currentMove != lastSentMove)
            {
                lastSentMove = currentMove;
                CmdSendMoveState(bodyTarget, currentMove, myRole);
            }

            if ((myRole == "Jump" || myRole == "Move") && (Keyboard.current.spaceKey.wasPressedThisFrame || Keyboard.current.wKey.wasPressedThisFrame))
            {
                CmdSendJumpToBody(bodyTarget);
            }
            if (myRole == "Action" && (Keyboard.current.downArrowKey.wasPressedThisFrame || Keyboard.current.sKey.wasPressedThisFrame))
            {
                CmdSendActionToBody(bodyTarget);
            }
        }
        else
        {
            if (myRole == "Jump" && (Keyboard.current.spaceKey.wasPressedThisFrame || Keyboard.current.wKey.wasPressedThisFrame))
            {
                GetComponent<PlayerController>().CallCombinedJump();
            }
            if (myRole == "Action" && (Keyboard.current.downArrowKey.wasPressedThisFrame || Keyboard.current.sKey.wasPressedThisFrame))
            {
                GetComponent<PlayerController>().CallCombinedAction();
            }
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

        if (Keyboard.current != null)
        {
            if (myRole == "Move_Left" && (Keyboard.current.aKey.isPressed || Keyboard.current.leftArrowKey.isPressed)) totalInput += -1f;
            if (myRole == "Move_Right" && (Keyboard.current.dKey.isPressed || Keyboard.current.rightArrowKey.isPressed)) totalInput += 1f;
            if (myRole == "Move")
            {
                if (Keyboard.current.aKey.isPressed || Keyboard.current.leftArrowKey.isPressed) totalInput += -1f;
                else if (Keyboard.current.dKey.isPressed || Keyboard.current.rightArrowKey.isPressed) totalInput += 1f;
            }
        }

        totalInput += ghostLeftInput;
        totalInput += ghostRightInput;
        totalInput += ghostDuoInput;

        return Mathf.Clamp(totalInput, -1f, 1f);
    }

    [Command]
    private void CmdSendMoveState(GameObject body, float moveValue, string role)
    {
        if (body == null) return;
        body.GetComponent<PlayerCombineHandler>().TargetReceiveMoveState(body.GetComponent<NetworkIdentity>().connectionToClient, moveValue, role);
    }

    [TargetRpc]
    private void TargetReceiveMoveState(NetworkConnection target, float moveValue, string role)
    {
        if (role == "Move_Left") ghostLeftInput = moveValue;
        else if (role == "Move_Right") ghostRightInput = moveValue;
        else if (role == "Move") ghostDuoInput = moveValue;
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
        myRole = "";
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