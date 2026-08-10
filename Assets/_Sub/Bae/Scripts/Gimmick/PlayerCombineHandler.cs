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
    [SyncVar(hook = nameof(OnCombineStateChanged))] public bool isCombined = false;
    [SyncVar] public CombineRole myRole = CombineRole.None;
    [SyncVar(hook = nameof(OnBodyTargetChanged))] public GameObject bodyTarget;

    [SyncVar] public bool canUseAction = false;

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

    // --- 🌟 새로 추가된 SyncVar Hook 함수들 🌟 ---
    private void OnCombineStateChanged(bool oldVal, bool newVal)
    {
        CoopPlayerIdentity identity = GetComponent<CoopPlayerIdentity>();
        if (identity != null) identity.ForceUpdateVisual();
    }

    private void OnBodyTargetChanged(GameObject oldVal, GameObject newVal)
    {
        CoopPlayerIdentity identity = GetComponent<CoopPlayerIdentity>();
        if (identity != null) identity.ForceUpdateVisual();
    }

    private void OnCombineColorChanged(int oldVal, int newVal)
    {
        CoopPlayerIdentity identity = GetComponent<CoopPlayerIdentity>();
        if (identity != null) identity.ForceUpdateVisual();
    }

    private void OnCombineFaceChanged(int oldVal, int newVal)
    {
        // 얼굴(눈)은 Update문에서 매 프레임 반영되므로 빈 칸으로 두어도 무방합니다.
    }
    // ---------------------------------------------

    [Server]
    public void StartCombineMode(CombineRole role, GameObject body)
    {
        isCombined = true;
        myRole = role;
        bodyTarget = body;

        if (gameObject == body)
        {
            connectedGhosts.Clear();

            if (spriteRenderer != null) spriteRenderer.enabled = true;
            if (col != null) col.enabled = true;
        }
        else
        {
            if (rb != null)
            {
                rb.simulated = false;
                rb.linearVelocity = Vector2.zero;
            }

            PlayerCombineHandler bodyHandler = body.GetComponent<PlayerCombineHandler>();
            if (bodyHandler != null && !bodyHandler.connectedGhosts.Contains(this))
            {
                bodyHandler.connectedGhosts.Add(this);
            }

            if (spriteRenderer != null) spriteRenderer.enabled = false;
            if (col != null) col.enabled = false;
        }

        CoopPlayerIdentity identity = GetComponent<CoopPlayerIdentity>();
        if (identity != null) identity.ForceUpdateVisual();

        NetworkIdentity bodyNetId = body.GetComponent<NetworkIdentity>();

        // 🌟 수정됨: 컬러와 표정 인덱스도 함께 RPC로 넘겨줍니다.
        RpcApplyCombineVisual(bodyNetId, combineColorIndex, combineFaceIndex);
    }

    // 🌟 수정됨: 매개변수(colorIdx, faceIdx)를 받고 내부에서 강제로 값을 동기화합니다.
    [ClientRpc]
    private void RpcApplyCombineVisual(NetworkIdentity bodyNetId, int colorIdx, int faceIdx)
    {
        if (bodyNetId != null)
        {
            bodyTarget = bodyNetId.gameObject;
        }

        // RPC 도달 즉시 상태 변수를 강제로 동일하게 맞춰줍니다. (타이밍 꼬임 방지)
        isCombined = true;
        combineColorIndex = colorIdx;
        combineFaceIndex = faceIdx;

        bool isBody = (bodyNetId != null && netIdentity.netId == bodyNetId.netId);

        if (isBody)
        {
            if (spriteRenderer != null) spriteRenderer.enabled = true;
            if (col != null) col.enabled = true;
        }
        else
        {
            if (spriteRenderer != null) spriteRenderer.enabled = false;
            if (col != null) col.enabled = false;
            if (rb != null) rb.simulated = false;
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
        isCombined = false;
        combineColorIndex = -1;
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