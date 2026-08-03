using UnityEngine;
using Mirror;
using UnityEngine.InputSystem;

public class PlayerGravityController : NetworkBehaviour
{
    [Header("중력 상태")]
    public bool canInvertGravity = false;
    public bool isGravityInverted = false;
    public float gravityMultiplier = 1f;

    private Rigidbody2D rb;

    public InputAction invertAction;

    void Awake()
    {
        if (invertAction == null || invertAction.bindings.Count == 0)
        {
            invertAction = new InputAction("InvertGravity", InputActionType.Button);
            invertAction.AddBinding("<Keyboard>/v");
            invertAction.AddBinding("<Gamepad>/buttonWest");
        }
    }

    void Start()
    {
        rb = GetComponent<Rigidbody2D>();
    }

    public override void OnStartLocalPlayer()
    {
        base.OnStartLocalPlayer();
        invertAction.Enable();
    }

    void OnDisable()
    {
        if (isLocalPlayer) invertAction.Disable();
    }

    void Update()
    {
        if (!isLocalPlayer) return;

        if (canInvertGravity && invertAction.WasPressedThisFrame())
        {
            TriggerGravityInversion();
        }
    }

    public void TriggerGravityInversion()
    {
        if (!isLocalPlayer) return;
        CmdInvertGravity();
    }

    [Command]
    void CmdInvertGravity()
    {
        RpcInvertGravity();
    }
    
    [Server]
    public void ServerToggleGravity()
    {
        RpcInvertGravity();
    }
    
    [ClientRpc]
    public void RpcInvertGravity()
    {
        isGravityInverted = !isGravityInverted;
        gravityMultiplier = isGravityInverted ? -1f : 1f;
        rb.linearVelocity = new Vector2(rb.linearVelocity.x, -rb.linearVelocity.y);

        Vector3 currentScale = transform.localScale;
        currentScale.y = isGravityInverted ? -1f : 1f;
        transform.localScale = currentScale;
    }

    public void ResetGravity()
    {
        if (isGravityInverted)
        {
            if (isServer)
            {
                RpcInvertGravity();
            }
            else if (isLocalPlayer)
            {
                CmdInvertGravity();
            }
        }
    }
}