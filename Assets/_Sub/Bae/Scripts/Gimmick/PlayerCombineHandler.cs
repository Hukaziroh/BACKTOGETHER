using UnityEngine;
using Mirror;

public class PlayerCombineHandler : NetworkBehaviour
{
    [Header("합체 상태")]
    [SyncVar] public bool isCombined = false;
    [SyncVar] public string myRole = "";     // "Move", "Move_Left", "Move_Right", "Jump", "Action" 등
    [SyncVar] public GameObject bodyTarget;  // 내가 조종해야 할 본체

    // 🌟 본체 클라이언트가 유령들에게 원격으로 전달받아 저장할 입력 변수들
    [HideInInspector] public float ghostLeftInput = 0f;
    [HideInInspector] public float ghostRightInput = 0f;
    [HideInInspector] public float ghostDuoInput = 0f;

    private float lastSentMove = 0f; // 패킷 최적화용

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
        if (gameObject == body)
        {
            transform.localScale = new Vector3(2f, 2f, 1f);
        }
        else
        {
            spriteRenderer.enabled = false;
            col.enabled = false;
            rb.simulated = false;
        }
    }

    void Update()
    {
        if (!isLocalPlayer || !isCombined) return;

        // 1. [내가 유령(Ghost)일 때]
        if (gameObject != bodyTarget)
        {
            float currentMove = 0f;

            if (myRole == "Move_Left" && (Input.GetKey(KeyCode.LeftArrow) || Input.GetKey(KeyCode.A))) currentMove = -1f;
            else if (myRole == "Move_Right" && (Input.GetKey(KeyCode.RightArrow) || Input.GetKey(KeyCode.D))) currentMove = 1f;
            else if (myRole == "Move") // 2인 기믹 통합 이동
            {
                if (Input.GetKey(KeyCode.LeftArrow) || Input.GetKey(KeyCode.A)) currentMove = -1f;
                else if (Input.GetKey(KeyCode.RightArrow) || Input.GetKey(KeyCode.D)) currentMove = 1f;
            }

            // 누르고 있는 상태가 변경되었을 때만 딱 한 번 패킷 전송 (서버 과부하 방지)
            if (currentMove != lastSentMove)
            {
                lastSentMove = currentMove;
                CmdSendMoveState(bodyTarget, currentMove, myRole);
            }

            // 점프와 액션은 한 번 누를 때마다 전송
            if ((myRole == "Jump" || myRole == "Move") && (Input.GetKeyDown(KeyCode.Space) || Input.GetKeyDown(KeyCode.W)))
            {
                CmdSendJumpToBody(bodyTarget);
            }
            if (myRole == "Action" && (Input.GetKeyDown(KeyCode.DownArrow) || Input.GetKeyDown(KeyCode.S)))
            {
                CmdSendActionToBody(bodyTarget);
            }
        }
        // 2. [내가 본체(Body)일 때] 본인도 점프/액션 역할군이라면 바로 실행
        else
        {
            if (myRole == "Jump" && (Input.GetKeyDown(KeyCode.Space) || Input.GetKeyDown(KeyCode.W)))
            {
                GetComponent<PlayerController>().CallCombinedJump();
            }
            if (myRole == "Action" && (Input.GetKeyDown(KeyCode.DownArrow) || Input.GetKeyDown(KeyCode.S)))
            {
                GetComponent<PlayerController>().CallCombinedAction();
            }
        }
    }

    // 🌟 [에러 해결의 핵심!] PlayerController가 가져다 쓸 통합 수평 입력값 계산기
    public float GetCombinedHorizontalInput()
    {
        float totalInput = 0f;

        // 1. 본체 본인의 키보드 조작 처리
        if (myRole == "Move_Left" && (Input.GetKey(KeyCode.LeftArrow) || Input.GetKey(KeyCode.A))) totalInput += -1f;
        if (myRole == "Move_Right" && (Input.GetKey(KeyCode.RightArrow) || Input.GetKey(KeyCode.D))) totalInput += 1f;
        if (myRole == "Move")
        {
            if (Input.GetKey(KeyCode.LeftArrow) || Input.GetKey(KeyCode.A)) totalInput += -1f;
            else if (Input.GetKey(KeyCode.RightArrow) || Input.GetKey(KeyCode.D)) totalInput += 1f;
        }

        // 2. 유령 동료들에게 네트워크로 전달받은 값들 더하기
        totalInput += ghostLeftInput;
        totalInput += ghostRightInput;
        totalInput += ghostDuoInput;

        // -1 ~ 1 사이로 보정해서 반환
        return Mathf.Clamp(totalInput, -1f, 1f);
    }

    // ----------------------------------------------------
    // 네트워크 통신 (Ghost -> Server -> Body 클라이언트 전달)
    // ----------------------------------------------------

    [Command]
    private void CmdSendMoveState(GameObject body, float moveValue, string role)
    {
        if (body == null) return;
        NetworkIdentity bodyIdentity = body.GetComponent<NetworkIdentity>();
        body.GetComponent<PlayerCombineHandler>().TargetReceiveMoveState(bodyIdentity.connectionToClient, moveValue, role);
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
        NetworkIdentity bodyIdentity = body.GetComponent<NetworkIdentity>();
        body.GetComponent<PlayerCombineHandler>().TargetDoJump(bodyIdentity.connectionToClient);
    }

    [TargetRpc]
    public void TargetDoJump(NetworkConnection target)
    {
        GetComponent<PlayerController>().CallCombinedJump();
    }

    [Command]
    private void CmdSendActionToBody(GameObject body)
    {
        if (body == null) return;
        NetworkIdentity bodyIdentity = body.GetComponent<NetworkIdentity>();
        body.GetComponent<PlayerCombineHandler>().TargetDoAction(bodyIdentity.connectionToClient);
    }

    [TargetRpc]
    public void TargetDoAction(NetworkConnection target)
    {
        GetComponent<PlayerController>().CallCombinedAction();
    }
}