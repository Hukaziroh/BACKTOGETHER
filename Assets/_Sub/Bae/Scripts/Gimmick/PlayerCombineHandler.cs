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
        CoopPlayerIdentity identity = GetComponent<CoopPlayerIdentity>();
        if (identity != null) identity.ForceUpdateVisual();
    }

    private void OnBodyTargetChanged(GameObject oldVal, GameObject newVal)
    {
        bodyTarget = newVal; // 필수!
        CoopPlayerIdentity identity = GetComponent<CoopPlayerIdentity>();
        if (identity != null) identity.ForceUpdateVisual();
    }

    private void OnCombineColorChanged(int oldVal, int newVal)
    {
        combineColorIndex = newVal;
        CoopPlayerIdentity identity = GetComponent<CoopPlayerIdentity>();
        if (identity != null) identity.ForceUpdateVisual();
    }

    private void OnCombineFaceChanged(int oldVal, int newVal)
    {
        combineFaceIndex = newVal;
    }

    // 🌟 유저님의 예전 코드에서 카메라가 완벽하게 따라가게 해주었던 1등 공신 (그대로 복구)
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

        // ---------------------------------
        // 1. 합체 상태 먼저 확정
        // ---------------------------------
        isCombined = true;
        myRole = role;
        bodyTarget = body;

        // ---------------------------------
        // 2. Body / Ghost 물리 및 Renderer 결정
        // ---------------------------------
        bool isBody = gameObject == body;

        if (isBody)
        {
            connectedGhosts.Clear();

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

            PlayerCombineHandler bodyHandler =
                body.GetComponent<PlayerCombineHandler>();

            if (bodyHandler != null &&
                !bodyHandler.connectedGhosts.Contains(this))
            {
                bodyHandler.connectedGhosts.Add(this);
            }

            if (spriteRenderer != null)
                spriteRenderer.enabled = false;

            if (col != null)
                col.enabled = false;
        }

        // ---------------------------------
        // 3. 서버에서도 즉시 합체 색 적용
        // ---------------------------------
        CoopPlayerIdentity identity =
            GetComponent<CoopPlayerIdentity>();

        if (identity != null)
        {
            identity.ForceUpdateVisual();
        }

        // ---------------------------------
        // 4. 클라이언트에 Body 정보 전달
        // ---------------------------------
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

        // ---------------------------------
        // 몸통 / 고스트 표시 상태
        // ---------------------------------
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

        // ---------------------------------
        // 비주얼 적용
        // ---------------------------------
        CoopPlayerIdentity identity =
            GetComponent<CoopPlayerIdentity>();

        if (identity != null)
        {
            identity.ForceUpdateVisual();
        }

        // ---------------------------------
        // 🌟 예전 코드의 카메라 로직 100% 동일하게 가져옴
        // ---------------------------------
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

        // 🌟 예전 코드의 카메라 로직 100% 동일하게 가져옴
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