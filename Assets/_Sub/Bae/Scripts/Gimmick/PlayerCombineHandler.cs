using UnityEngine;
using Mirror;

public class PlayerCombineHandler : NetworkBehaviour
{
    [Header("합체 상태")]
    [SyncVar] public bool isCombined = false;
    [SyncVar] public string myRole = "";     // "Move_Left", "Move_Right", "Jump", "Action" 등

    [SyncVar] public GameObject bodyTarget;  // 내가 조종해야 할 본체 (내가 본체면 나 자신)

    private SpriteRenderer spriteRenderer;
    private Collider2D col;
    private Rigidbody2D rb;

    void Awake()
    {
        spriteRenderer = GetComponent<SpriteRenderer>();
        col = GetComponent<Collider2D>();
        rb = GetComponent<Rigidbody2D>();
    }

    // ----------------------------------------------------
    // 기믹 스크립트(Trigger)가 서버에서 공통으로 호출해 주는 함수
    // 2인, 4인 트리거 모두 이 함수를 사용해 롤을 부여합니다.
    // ----------------------------------------------------
    [Server]
    public void StartCombineMode(string role, GameObject body)
    {
        isCombined = true;
        myRole = role;
        bodyTarget = body;

        // 클라이언트들의 화면(비주얼)을 바꾸기 위해 Rpc 호출
        RpcApplyCombineVisual(body);
    }

    [ClientRpc]
    private void RpcApplyCombineVisual(GameObject body)
    {
        if (gameObject == body)
        {
            // [내가 본체(Body)일 때]
            transform.localScale = new Vector3(2f, 2f, 1f);
        }
        else
        {
            // [내가 유령(Ghost)일 때]
            spriteRenderer.enabled = false;
            col.enabled = false;
            rb.simulated = false;
        }
    }

    // ----------------------------------------------------
    // 클라이언트의 입력(Input) 처리 및 전송
    // ----------------------------------------------------
    void Update()
    {
        if (!isLocalPlayer || !isCombined) return;

        // 1. 내가 유령(Ghost)일 때 ➔ 본체에게 명령을 쏜다!
        if (gameObject != bodyTarget)
        {
            if (myRole == "Move_Left" && (Input.GetKey(KeyCode.LeftArrow) || Input.GetKey(KeyCode.A)))
            {
                CmdSendMoveToBody(bodyTarget, -1f);
            }
            else if (myRole == "Move_Right" && (Input.GetKey(KeyCode.RightArrow) || Input.GetKey(KeyCode.D)))
            {
                CmdSendMoveToBody(bodyTarget, 1f);
            }
            else if (myRole == "Jump" && (Input.GetKeyDown(KeyCode.Space) || Input.GetKeyDown(KeyCode.W)))
            {
                CmdSendJumpToBody(bodyTarget);
            }
            else if (myRole == "Action" && (Input.GetKeyDown(KeyCode.DownArrow) || Input.GetKeyDown(KeyCode.S)))
            {
                CmdSendActionToBody(bodyTarget); // 중력(내리찍기) 명령 전송
            }
        }
        // 2. 내가 본체(Body)일 때 ➔ 자신이 맡은 역할이 있다면 직접 수행!
        else
        {
            if (myRole == "Move_Left" && (Input.GetKey(KeyCode.LeftArrow) || Input.GetKey(KeyCode.A)))
                transform.Translate(Vector3.left * 5f * Time.deltaTime);

            else if (myRole == "Move_Right" && (Input.GetKey(KeyCode.RightArrow) || Input.GetKey(KeyCode.D)))
                transform.Translate(Vector3.right * 5f * Time.deltaTime);

            else if (myRole == "Jump" && (Input.GetKeyDown(KeyCode.Space) || Input.GetKeyDown(KeyCode.W)))
                rb.AddForce(Vector2.up * 10f, ForceMode2D.Impulse);

            else if (myRole == "Action" && (Input.GetKeyDown(KeyCode.DownArrow) || Input.GetKeyDown(KeyCode.S)))
            {
                // 본체가 액션 역할일 경우 직접 내리찍기
                rb.linearVelocity = new Vector2(rb.linearVelocity.x, 0f);
                rb.AddForce(Vector2.down * 15f, ForceMode2D.Impulse);
            }
        }
    }

    // ----------------------------------------------------
    // 네트워크 통신 (Ghost -> Server -> Body)
    // ----------------------------------------------------

    // --- 이동 (좌/우) ---
    [Command]
    private void CmdSendMoveToBody(GameObject body, float direction)
    {
        NetworkIdentity bodyIdentity = body.GetComponent<NetworkIdentity>();
        body.GetComponent<PlayerCombineHandler>().TargetDoMove(bodyIdentity.connectionToClient, direction);
    }

    [TargetRpc]
    public void TargetDoMove(NetworkConnection target, float direction)
    {
        transform.Translate(new Vector3(direction, 0, 0) * 5f * Time.deltaTime);
    }

    // --- 점프 ---
    [Command]
    private void CmdSendJumpToBody(GameObject body)
    {
        NetworkIdentity bodyIdentity = body.GetComponent<NetworkIdentity>();
        body.GetComponent<PlayerCombineHandler>().TargetDoJump(bodyIdentity.connectionToClient);
    }

    [TargetRpc]
    public void TargetDoJump(NetworkConnection target)
    {
        rb.AddForce(Vector2.up * 10f, ForceMode2D.Impulse);
    }

    // --- 액션 (중력 / 내리찍기) ---
    [Command]
    private void CmdSendActionToBody(GameObject body)
    {
        NetworkIdentity bodyIdentity = body.GetComponent<NetworkIdentity>();
        body.GetComponent<PlayerCombineHandler>().TargetDoAction(bodyIdentity.connectionToClient);
    }

    [TargetRpc]
    public void TargetDoAction(NetworkConnection target)
    {
        // 🌟 내리찍기 로직: 현재 Y축 속도를 0으로 만들어 체공 관성을 없앤 뒤 강하게 아래로 힘을 가함
        rb.linearVelocity = new Vector2(rb.linearVelocity.x, 0f);
        rb.AddForce(Vector2.down * 15f, ForceMode2D.Impulse);
    }
}