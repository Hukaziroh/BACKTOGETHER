using System.Collections.Generic;
using Mirror;
using UnityEngine;
using System.Linq;

public enum CombineRole
{
    None, Move_Left, Move_Right, Move, Jump, Action
}

public class PlayerCombineHandler : NetworkBehaviour
{
    [Header("합체 상태")]
    [SyncVar(hook = nameof(OnCombineStateChanged))]
    public bool isCombined = false;

    [SyncVar]
    public CombineRole myRole = CombineRole.None;

    [SyncVar(hook = nameof(OnBodyTargetChanged))]
    public GameObject bodyTarget;

    [SyncVar]
    public bool canUseAction = false;

    [SyncVar(hook = nameof(OnCombineColorChanged))]
    public int combineColorIndex = -1;

    [SyncVar(hook = nameof(OnCombineFaceChanged))]
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

    private void OnCombineStateChanged(bool oldVal, bool newVal)
    {
        isCombined = newVal;
        EnforceCombineVisuals();
    }

    private void OnBodyTargetChanged(GameObject oldVal, GameObject newVal)
    {
        bodyTarget = newVal;
        EnforceCombineVisuals();
    }

    public override void OnStartClient()
    {
        base.OnStartClient();
        EnforceCombineVisuals();
    }

    private void EnforceCombineVisuals()
    {
        if (isCombined)
        {
            bool isBody = (bodyTarget != null && bodyTarget == gameObject);
            if (spriteRenderer != null) spriteRenderer.enabled = isBody;
            if (col != null) col.enabled = isBody;
            if (rb != null) rb.simulated = isBody;

            CoopPlayerIdentity identity = GetComponent<CoopPlayerIdentity>();
            if (identity != null) identity.ForceUpdateVisual();

            if (isLocalPlayer && bodyTarget != null)
            {
                Camera mainCam = Camera.main;
                if (mainCam != null)
                {
                    CameraFollow camFollow = mainCam.GetComponent<CameraFollow>();
                    if (camFollow != null) camFollow.target = bodyTarget.transform;
                }
            }
        }
        else
        {
            if (spriteRenderer != null) spriteRenderer.enabled = true;
            if (col != null) col.enabled = true;
            if (rb != null) rb.simulated = true;
        }
    }

    private void OnCombineColorChanged(int oldVal, int newVal)
    {
        combineColorIndex = newVal;
        CoopPlayerIdentity identity = GetComponent<CoopPlayerIdentity>();
        if (identity != null) identity.ForceUpdateVisual();
    }

    [Server]
    void OnDestroy()
    {
        if (isCombined)
        {
            List<CombineRole> rolesToDistribute = new List<CombineRole>();
            rolesToDistribute.Add(myRole);
            rolesToDistribute.AddRange(extraRoles);

            PlayerCombineHandler newBody = null;
            if (bodyTarget == gameObject)
            {
                newBody = connectedGhosts.FirstOrDefault(g => g != null && g.gameObject != gameObject);
                if (newBody != null)
                {
                    foreach (var ghost in connectedGhosts)
                    {
                        if (ghost != null && ghost != newBody)
                        {
                            newBody.connectedGhosts.Add(ghost);
                            ghost.bodyTarget = newBody.gameObject;
                        }
                    }
                    newBody.bodyTarget = newBody.gameObject;
                    newBody.BecomeBody();
                }
            }
            else if (bodyTarget != null)
            {
                PlayerCombineHandler bodyHandler = bodyTarget.GetComponent<PlayerCombineHandler>();
                if (bodyHandler != null)
                {
                    bodyHandler.connectedGhosts.Remove(this);
                }
            }

            // Distribute missing roles to remaining players
            List<PlayerCombineHandler> remaining = new List<PlayerCombineHandler>();
            if (bodyTarget != gameObject && bodyTarget != null)
            {
                PlayerCombineHandler bh = bodyTarget.GetComponent<PlayerCombineHandler>();
                if (bh != null) remaining.Add(bh);
            }
            if (newBody != null && !remaining.Contains(newBody)) remaining.Add(newBody);

            foreach (var g in (newBody != null ? newBody.connectedGhosts : (bodyTarget != null ? bodyTarget.GetComponent<PlayerCombineHandler>().connectedGhosts : new List<PlayerCombineHandler>())))
            {
                if (g != null && g.gameObject != gameObject && !remaining.Contains(g))
                    remaining.Add(g);
            }

            if (remaining.Count > 0)
            {
                foreach (var r in rolesToDistribute)
                {
                    if (r != CombineRole.None)
                    {
                        var target = remaining.OrderBy(p => 1 + p.extraRoles.Count).First();
                        target.extraRoles.Add(r);
                    }
                }
            }
        }
    }

    [Server]
    public void BecomeBody()
    {
        bodyTarget = gameObject;
        if (rb != null)
        {
            rb.simulated = true;
        }
        RpcApplyCombineVisual(netIdentity, combineColorIndex, combineFaceIndex);
    }

    private void OnCombineFaceChanged(int oldVal, int newVal)
    {
        combineFaceIndex = newVal;
    }

    void LateUpdate()
    {
        if (isCombined && bodyTarget != null && gameObject != bodyTarget)
        {
            transform.position = bodyTarget.transform.position;
        }
    }

    [Server]
    public void StartCombineMode(CombineRole role, GameObject body)
    {
        if (body == null)
            return;

        myRole = role;
        isCombined = true;
        bodyTarget = body;

        bool isBody = gameObject == body;

        if (isBody)
        {
            if (spriteRenderer != null)
                spriteRenderer.enabled = true;

            if (col != null)
                col.enabled = true;

            if (rb != null)
                rb.simulated = true;
        }
        else
        {
            if (rb != null)
            {
                rb.simulated = false;
                rb.linearVelocity = Vector2.zero;
                rb.angularVelocity = 0f;
            }

            if (spriteRenderer != null)
                spriteRenderer.enabled = false;

            if (col != null)
                col.enabled = false;
        }

        CoopPlayerIdentity identity =
            GetComponent<CoopPlayerIdentity>();

        if (identity != null)
        {
            identity.ForceUpdateVisual();
        }

        NetworkIdentity bodyNetId =
            body.GetComponent<NetworkIdentity>();

        RpcApplyCombineVisual(
            bodyNetId,
            combineColorIndex,
            combineFaceIndex
        );
    }

    [ClientRpc]
    private void RpcApplyCombineVisual(
        NetworkIdentity bodyNetId,
        int colorIdx,
        int faceIdx)
    {
        GameObject bodyObj = null;

        if (bodyNetId != null)
        {
            bodyObj = bodyNetId.gameObject;
            bodyTarget = bodyObj;
        }

        isCombined = true;
        combineColorIndex = colorIdx;
        combineFaceIndex = faceIdx;

        bool isBody =
            bodyNetId != null &&
            netIdentity != null &&
            netIdentity.netId == bodyNetId.netId;
        if (isBody)
        {
            if (spriteRenderer != null)
                spriteRenderer.enabled = true;

            if (col != null)
                col.enabled = true;

            if (rb != null)
                rb.simulated = true;
        }
        else
        {
            if (spriteRenderer != null)
                spriteRenderer.enabled = false;

            if (col != null)
                col.enabled = false;

            if (rb != null)
                rb.simulated = false;
        }
        CoopPlayerIdentity identity =
            GetComponent<CoopPlayerIdentity>();

        if (identity != null)
        {
            identity.ForceUpdateVisual();
        }
        if (isLocalPlayer && bodyObj != null)
        {
            Camera mainCam = Camera.main;
            if (mainCam != null)
            {
                CameraFollow cam = mainCam.GetComponent<CameraFollow>();
                if (cam != null) cam.target = bodyObj.transform;
            }
        }
    }

    public List<CombineRole> extraRoles = new List<CombineRole>();

    private bool HasRole(CombineRole role)
    {
        return myRole == role || extraRoles.Contains(role);
    }

    [Server]
    public float GetServerCombinedHorizontalInput()
    {
        float totalInput = 0f;
        PlayerInput myInput = GetComponent<PlayerInput>();

        if (HasRole(CombineRole.Move) || (HasRole(CombineRole.Move_Left) && myInput.HorizontalInput < 0) || (HasRole(CombineRole.Move_Right) && myInput.HorizontalInput > 0)) 
            totalInput += myInput.HorizontalInput;

        foreach (var ghost in connectedGhosts)
        {
            if (ghost == null) continue;
            PlayerInput ghostInput = ghost.GetComponent<PlayerInput>();
            if (ghostInput != null)
            {
                if (ghost.HasRole(CombineRole.Move) || (ghost.HasRole(CombineRole.Move_Left) && ghostInput.HorizontalInput < 0) || (ghost.HasRole(CombineRole.Move_Right) && ghostInput.HorizontalInput > 0))
                    totalInput += ghostInput.HorizontalInput;
            }
        }
        return Mathf.Clamp(totalInput, -1f, 1f);
    }

    [Server]
    public bool GetServerCombinedJumpPressed()
    {
        PlayerInput myInput = GetComponent<PlayerInput>();
        if (HasRole(CombineRole.Jump) && myInput != null && myInput.JumpPressedThisFrame) return true;

        foreach (var ghost in connectedGhosts)
        {
            if (ghost == null) continue;
            PlayerInput ghostInput = ghost.GetComponent<PlayerInput>();
            if (ghost.HasRole(CombineRole.Jump) && ghostInput != null && ghostInput.JumpPressedThisFrame) return true;
        }
        return false;
    }

    [Server]
    public bool GetServerCombinedJumpReleased()
    {
        PlayerInput myInput = GetComponent<PlayerInput>();
        if (HasRole(CombineRole.Jump) && myInput != null && myInput.JumpReleasedThisFrame) return true;

        foreach (var ghost in connectedGhosts)
        {
            if (ghost == null) continue;
            PlayerInput ghostInput = ghost.GetComponent<PlayerInput>();
            if (ghost.HasRole(CombineRole.Jump) && ghostInput != null && ghostInput.JumpReleasedThisFrame) return true;
        }
        return false;
    }

    [Server]
    public bool GetServerCombinedJumpHolding()
    {
        PlayerInput myInput = GetComponent<PlayerInput>();
        if (HasRole(CombineRole.Jump) && myInput != null && myInput.JumpHolding) return true;

        foreach (var ghost in connectedGhosts)
        {
            if (ghost == null) continue;
            PlayerInput ghostInput = ghost.GetComponent<PlayerInput>();
            if (ghost.HasRole(CombineRole.Jump) && ghostInput != null && ghostInput.JumpHolding) return true;
        }
        return false;
    }

    [Server]
    public bool GetServerCombinedActionPressed()
    {
        if (bodyTarget != gameObject) return false;
        PlayerInput myInput = GetComponent<PlayerInput>();

        if (HasRole(CombineRole.Action) && myInput != null && myInput.ActionPressedThisFrame) return true;

        foreach (var ghost in connectedGhosts)
        {
            if (ghost == null) continue;
            PlayerInput ghostInput = ghost.GetComponent<PlayerInput>();
            if (ghost.HasRole(CombineRole.Action) && ghostInput != null && ghostInput.ActionPressedThisFrame) return true;
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
    public void JoinExistingCombine(GameObject bodyObj)
    {
        if (bodyObj == null) return;
        PlayerCombineHandler bodyHandler = bodyObj.GetComponent<PlayerCombineHandler>();
        if (bodyHandler == null) return;

        CombineRole assignedRole = CombineRole.None;

        List<PlayerCombineHandler> allCombinedPlayers = new List<PlayerCombineHandler>();
        allCombinedPlayers.Add(bodyHandler);
        foreach (var g in bodyHandler.connectedGhosts)
        {
            if (g != null) allCombinedPlayers.Add(g);
        }

        var playerWithExtra = allCombinedPlayers.Where(p => p.extraRoles.Count > 0).OrderByDescending(p => p.extraRoles.Count).FirstOrDefault();
        
        if (playerWithExtra != null)
        {
            assignedRole = playerWithExtra.extraRoles[0];
            playerWithExtra.extraRoles.RemoveAt(0);
        }

        if (assignedRole != CombineRole.None)
        {
            if (!bodyHandler.connectedGhosts.Contains(this))
            {
                bodyHandler.connectedGhosts.Add(this);
            }
            combineColorIndex = bodyHandler.combineColorIndex;
            combineFaceIndex = bodyHandler.combineFaceIndex;
            StartCombineMode(assignedRole, bodyObj);
        }
        else
        {
            Debug.LogWarning("합체에 참여하려 했으나, 여분의 역할이 없습니다.");
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
        if (spriteRenderer != null) spriteRenderer.enabled = true;
        if (col != null) col.enabled = true;
        if (rb != null) rb.simulated = true;

        isCombined = false;
        combineColorIndex = -1;
        combineFaceIndex = -1;
        bodyTarget = null;

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