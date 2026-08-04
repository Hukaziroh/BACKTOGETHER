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

    // 본체(Body)가 자신에게 붙은 고스트들을 기억하는 리스트 (서버 전용 최적화)
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
            canUseAction = true;
        }

        if (gameObject != body && rb != null)
        {
            rb.simulated = false;
            rb.linearVelocity = Vector2.zero;

            PlayerCombineHandler bodyHandler = body.GetComponent<PlayerCombineHandler>();
            if (bodyHandler != null && !bodyHandler.connectedGhosts.Contains(this))
            {
                bodyHandler.connectedGhosts.Add(this);
            }
        }

        RpcApplyCombineVisual(body);
    }

    [ClientRpc]
    private void RpcApplyCombineVisual(GameObject body)
    {
        if (gameObject != body)
        {
            spriteRenderer.enabled = false;
            col.enabled = false;
            if (rb != null) rb.simulated = false;
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

    void LateUpdate()
    {
        if (isCombined && bodyTarget != null && gameObject != bodyTarget)
        {
            transform.position = bodyTarget.transform.position;
        }
    }

    // ====================================================
    // 서버 최적화 입력 긁어오기 (Move, Jump, Action)
    // ====================================================

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

    // 🌟 [에러 해결!] 점프 담당 고스트가 점프 키를 유지(Hold)하고 있는지 확인
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

    // 본체와 고스트(팀원)들의 입력 버퍼를 한 번에 지워주는 함수
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

    // ====================================================
    // 합체 해제
    // ====================================================

    [Server]
    public void StopCombineMode(Vector3 releasePosition)
    {
        // 명단 정리
        if (gameObject != bodyTarget && bodyTarget != null)
        {
            PlayerCombineHandler bodyHandler = bodyTarget.GetComponent<PlayerCombineHandler>();
            if (bodyHandler != null) bodyHandler.connectedGhosts.Remove(this);
        }
        connectedGhosts.Clear();

        isCombined = false;
        myRole = CombineRole.None;
        bodyTarget = null;
        canUseAction = false;

        // 물리 복구
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