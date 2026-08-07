using System.Collections.Generic;
using Mirror;
using UnityEngine;

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

    [SyncVar] public bool canUseAction = false;

    [SyncVar]
    public int combineColorIndex = -1;

    [SyncVar]
    public int combineFaceIndex = -1;

    public List<PlayerCombineHandler> connectedGhosts = new List<PlayerCombineHandler>();

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
    public void StartCombineMode(CombineRole role, GameObject body)
    {
        isCombined = true;
        myRole = role;
        bodyTarget = body;

        if (gameObject == body)
        {
            connectedGhosts.Clear();
        }
        else // gameObject != body
        {
            rb.simulated = false;
            rb.linearVelocity = Vector2.zero;

            PlayerCombineHandler bodyHandler = body.GetComponent<PlayerCombineHandler>();

            if (bodyHandler != null)
            {
                if (!bodyHandler.connectedGhosts.Contains(this))
                {
                    bodyHandler.connectedGhosts.Add(this);
                }
            }
        }

        NetworkIdentity bodyNetId = body.GetComponent<NetworkIdentity>();
        RpcApplyCombineVisual(bodyNetId);
    }

    [ClientRpc]
    private void RpcApplyCombineVisual(NetworkIdentity bodyNetId)
    {
        if (bodyNetId == null || netIdentity.netId != bodyNetId.netId)
        {
            spriteRenderer.enabled = false;
            col.enabled = false;
            if (rb != null) rb.simulated = false;
        }
        else
        {
            spriteRenderer.enabled = true; 
        }

        CoopPlayerIdentity identity = GetComponent<CoopPlayerIdentity>();
        if (identity != null) identity.ForceUpdateVisual();

        if (isLocalPlayer && bodyNetId != null)
        {
            Camera mainCam = Camera.main;
            if (mainCam != null)
            {
                CameraFollow cam = mainCam.GetComponent<CameraFollow>();
                if (cam != null) cam.target = bodyNetId.transform;
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

    [Server]
    public float GetServerCombinedHorizontalInput()
    {
        float totalInput = 0f;
        PlayerInput myInput = GetComponent<PlayerInput>();

        if (myRole == CombineRole.Move_Left && myInput.HorizontalInput < 0) totalInput += myInput.HorizontalInput;
        else if (myRole == CombineRole.Move_Right && myInput.HorizontalInput > 0) totalInput += myInput.HorizontalInput;
        else if (myRole == CombineRole.Move) totalInput += myInput.HorizontalInput;

        foreach (var ghost in connectedGhosts)
        {
            if (ghost == null) continue;
            PlayerInput ghostInput = ghost.GetComponent<PlayerInput>();
            if (ghostInput != null)
            {
                if (ghost.myRole == CombineRole.Move_Left && ghostInput.HorizontalInput < 0) totalInput += ghostInput.HorizontalInput;
                else if (ghost.myRole == CombineRole.Move_Right && ghostInput.HorizontalInput > 0) totalInput += ghostInput.HorizontalInput;
                else if (ghost.myRole == CombineRole.Move) totalInput += ghostInput.HorizontalInput;
            }
        }
        return Mathf.Clamp(totalInput, -1f, 1f);
    }

    [Server]
    public bool GetServerCombinedJumpPressed()
    {
        PlayerInput myInput = GetComponent<PlayerInput>();
        if (myRole == CombineRole.Jump && myInput != null && myInput.JumpPressedThisFrame) return true;

        foreach (var ghost in connectedGhosts)
        {
            if (ghost != null && ghost.myRole == CombineRole.Jump)
            {
                PlayerInput ghostInput = ghost.GetComponent<PlayerInput>();
                if (ghostInput != null && ghostInput.JumpPressedThisFrame) return true;
            }
        }
        return false;
    }

    [Server]
    public bool GetServerCombinedJumpReleased()
    {
        PlayerInput myInput = GetComponent<PlayerInput>();
        if (myRole == CombineRole.Jump && myInput != null && myInput.JumpReleasedThisFrame) return true;

        foreach (var ghost in connectedGhosts)
        {
            if (ghost != null && ghost.myRole == CombineRole.Jump)
            {
                PlayerInput ghostInput = ghost.GetComponent<PlayerInput>();
                if (ghostInput != null && ghostInput.JumpReleasedThisFrame) return true;
            }
        }
        return false;
    }

    [Server]
    public bool GetServerCombinedJumpHolding()
    {
        PlayerInput myInput = GetComponent<PlayerInput>();
        if (myRole == CombineRole.Jump && myInput != null && myInput.JumpHolding) return true;

        foreach (var ghost in connectedGhosts)
        {
            if (ghost != null && ghost.myRole == CombineRole.Jump)
            {
                PlayerInput ghostInput = ghost.GetComponent<PlayerInput>();
                if (ghostInput != null && ghostInput.JumpHolding) return true;
            }
        }
        return false;
    }

    [Server]
    public bool GetServerCombinedActionPressed()
    {
        PlayerInput myInput = GetComponent<PlayerInput>();
        if (myRole == CombineRole.Action && myInput != null && myInput.ActionPressedThisFrame) return true;

        foreach (var ghost in connectedGhosts)
        {
            if (ghost != null && ghost.myRole == CombineRole.Action)
            {
                PlayerInput ghostInput = ghost.GetComponent<PlayerInput>();
                if (ghostInput != null && ghostInput.ActionPressedThisFrame) return true;
            }
        }
        return false;
    }

    [Server]
    public void ClearAllCombinedInputBuffers()
    {
        PlayerInput myInput = GetComponent<PlayerInput>();
        if (myInput != null) myInput.ClearInputBuffers();

        foreach (var ghost in connectedGhosts)
        {
            if (ghost != null)
            {
                PlayerInput ghostInput = ghost.GetComponent<PlayerInput>();
                if (ghostInput != null) ghostInput.ClearInputBuffers();
            }
        }
    }


    [Server]
    public void StopCombineMode(Vector3 releasePosition)
    {
        if (connectedGhosts.Count > 0)
        {
            foreach (var ghost in connectedGhosts.ToArray())
            {
                if (ghost != null && ghost != this)
                {
                    ghost.StopCombineMode(releasePosition);
                }
            }
        }

        if (combineColorIndex >= 0) CoopDuoCombineTrigger.ReleaseColor(combineColorIndex);
        if (combineFaceIndex >= 0) CoopDuoCombineTrigger.ReleaseFace(combineFaceIndex);

        if (gameObject != bodyTarget && bodyTarget != null)
        {
            PlayerCombineHandler bodyHandler = bodyTarget.GetComponent<PlayerCombineHandler>();
            if (bodyHandler != null && bodyHandler.connectedGhosts.Contains(this))
            {
                bodyHandler.connectedGhosts.Remove(this);
            }
        }
        connectedGhosts.Clear();

        isCombined = false;
        myRole = CombineRole.None;
        bodyTarget = null;
        canUseAction = false;
        combineColorIndex = -1;
        combineFaceIndex = -1;

        if (rb != null)
        {
            rb.simulated = true;
            rb.linearVelocity = Vector2.zero;
            rb.angularVelocity = 0f;
        }

        transform.position = releasePosition;
        Physics2D.SyncTransforms();

        RpcApplySeparateVisual(releasePosition);
    }

    [ClientRpc]
    private void RpcApplySeparateVisual(Vector3 releasePosition)
    {
        spriteRenderer.enabled = true;
        col.enabled = true;
        if (rb != null) rb.simulated = true;

        CoopPlayerIdentity identity = GetComponent<CoopPlayerIdentity>();
        if (identity != null)
        {
            identity.ResetFaceVisual();
            identity.ResetPlayerColor();
            identity.ForceUpdateVisual();
        }

        if (isLocalPlayer)
        {
            transform.position = releasePosition;
            Camera mainCam = Camera.main;
            if (mainCam != null)
            {
                CameraFollow cam = mainCam.GetComponent<CameraFollow>();
                if (cam != null) cam.target = transform;
            }
        }
    }
}