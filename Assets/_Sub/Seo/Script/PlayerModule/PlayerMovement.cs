using UnityEngine;
using Mirror;

public class PlayerMovement : NetworkBehaviour
{
    private PlayerController controller;
    public bool isRestrictedByRope = false;
    private int playerLayerMask;
    public bool isTouchingPlayer { get; private set; }
    [Header("무브")]
    public float moveSpeed = 7f;

    [Header("관성 및 미끄러짐 셋팅")]
    public float normalFriction = 20f;
    public float iceSlideFriction = 2.5f;
    public float airFriction = 3f;

    [Header("얼음 셋팅")]
    public float iceAcceleration = 150f;

    [Header("점프셋팅")]
    public float jumpHeight = 4f;
    public float jumpSpeed = 4f;
    public float fallSpeed = 2.5f;
    public float maxFallSpeed = 20f;
    public float coyoteTime = 0.15f;
    public float coyoteTimeCounter { get; private set; }

    [Range(0f, 1f)]
    public float superJump = 0.5f;
    private float jumpCk = 0.1f;
    private float ckTimer;

    [Header("기즈모 및 감지 범위 설정")]
    public bool showGizmo = true;

    [Header("그라운드 체크")]
    [Range(0.01f, 5f)]
    public float checkRadius = 0.5f;
    public Vector2 checkOffset = Vector2.zero;
    public Transform groundCheck;
    public LayerMask groundLayer;
    public bool isGrounded { get; private set; }

    [Header("헤드 체크 (머리 위 플레이어 감지)")]
    public Vector2 headCheckBoxSize = new Vector2(0.8f, 0.2f);
    public Vector2 headCheckOffset = Vector2.zero;
    public Transform headCheck;
    public bool hasPlayerOnHead { get; private set; }

    private bool isOnIce = false;
    private bool wasOnIceLastFrame = false;

    private Collider2D[] groundCheckResults = new Collider2D[5];
    private Collider2D[] playerCheckResults = new Collider2D[5];
    private Collider2D[] headCheckResults = new Collider2D[5];
    private ContactFilter2D groundFilter;
    private ContactFilter2D playerFilter;

    [Header("외부 환경 속도")]
    public float windVelocity = 0f;
    private Transform currentPlatform;
    private Vector2 platformVelocity;
    private System.Func<Vector2> getPlatformVelocityFunc;

    private bool justJumped = false;
    private bool wasGroundedLastFrame = false;

    [Header("코너 충돌 보정 (실제 충돌은 항상 유지하고, 그로 인한 잔여 속도만 제거)")]
    [Tooltip("이 값보다 큰 X속도 변화가 감지되면 '물리엔진이 충돌 해소 중 끼워넣은 값'으로 간주하고 지운다.")]
    public float cornerPushEpsilon = 0.02f;
    [Tooltip("겹친 깊이가 이 값을 넘으면 '깊게 낀 것'으로 보고 즉시 강제로 떼어놓는다. 이 값 이하의 살짝 스치는 정도는 물리엔진에 맡긴다.")]
    public float overlapPushTolerance = 0.01f;
    [Tooltip("체크하면 겹침이 감지/보정될 때마다 콘솔에 상세 정보(깊이, 속도, 중력, 질량 등)를 출력한다. 원인 파악용 - 확인 끝나면 꺼도 됨.")]
    public bool debugLogOverlap = false;
    private float lastCommandedVelocityX;
    private bool hasCommandedVelocity;

    void Awake()
    {
        controller = GetComponent<PlayerController>();
        playerLayerMask = 1 << LayerMask.NameToLayer("Player");

        groundFilter = new ContactFilter2D { useLayerMask = true, useTriggers = false, layerMask = groundLayer };
        playerFilter = new ContactFilter2D { useLayerMask = true, useTriggers = false, layerMask = playerLayerMask };

        // 🔧 [디버그] 체크박스/설정과 무관하게 무조건 찍힘 - 이 스크립트가 실제로 로드/실행되는지 확인용.
        // 이것조차 콘솔에 안 뜨면 코드 문제가 아니라, 이 파일이 실제로 적용이 안 된 것임.
        Debug.Log($"★★★★★ [PlayerMovement] Awake 실행됨 - {gameObject.name} (진단용 스크립트 버전) ★★★★★", this);
    }

    void Update()
    {
        if (!isLocalPlayer) return;


        UpdateTimers();
        UpdateCoyoteTime();
        HandleJumpInput();
        UpdateGravity();
    }

    void FixedUpdate()
    {
        // 🔧 [디버그] 체크박스와 무관하게 1초에 한 번 무조건 찍힘 - isLocalPlayer가 실제로
        // true인지, FixedUpdate 자체가 이 오브젝트에서 돌고 있는지 확인용.
        if (Time.frameCount % 50 == 0)
        {
            Debug.Log($"★ [FixedUpdate 체크] {gameObject.name} isLocalPlayer={isLocalPlayer}", this);
        }

        if (!isLocalPlayer) return;

        if (controller.combineHandler != null && controller.combineHandler.isCombined && gameObject != controller.combineHandler.bodyTarget)
            return;

        // 🔧 [디버그] 매 프레임 기본 상태. 겹침 감지 로그가 하나도 안 뜬다면
        // 이 버그가 플레이어-플레이어 충돌이 아닐 가능성이 높다는 뜻.
        if (debugLogOverlap)
        {
            string parentName = transform.parent != null ? transform.parent.name : "없음";
            bool isCombined = controller.combineHandler != null && controller.combineHandler.isCombined;
            bool isBodyTarget = controller.combineHandler != null && gameObject == controller.combineHandler.bodyTarget;
            string platformName = currentPlatform != null ? currentPlatform.name : "없음";
            Debug.Log($"[프레임상태:{gameObject.name}] pos={controller.rb.position} vel={controller.rb.linearVelocity} gravityScale={controller.rb.gravityScale} mass={controller.rb.mass} isGrounded={isGrounded} isTouchingPlayer={isTouchingPlayer} 부모={parentName} isCombined={isCombined} isBodyTarget={isBodyTarget} 플랫폼={platformName} 플랫폼속도={platformVelocity}", this);
        }

        // 🔧 [제거됨] ResolvePlayerOverlap()은 겹침을 "즉시 텔레포트로 떼어놓는" 방식이었는데,
        // 실제 로그 확인 결과 이게 오히려 눈에 보이는 슬라이딩의 직접 원인이었다(vel=0인데
        // pos만 계속 바뀜 = 이 함수가 매 프레임 위치를 강제로 밀고 있었던 것).
        // 진짜 원인(상대가 계속 파고드는 것 자체)을 해결하기 전까지는 비활성화.
        // ResolvePlayerOverlap();

        // 🔧 [코너 충돌 보정] 실제 물리 충돌(겹침 방지)은 절대 끄지 않는다.
        // 다만 지난 물리 스텝에서 충돌을 "풀어내는" 과정 중 옆으로 튀는 속도가 끼어들었다면,
        // 여기서 그 잔여 속도만 지운다. 두 플레이어가 겹치는 것 자체는 항상 물리엔진이 막는다.
        CancelUnexpectedPlayerCollisionPush();

        HandleMovingPlatform();
        CheckGroundOrPlayer();
        CheckHeadForPlayer();
        CheckTouchingPlayer();
        HandleMovementPhysics();
        ClampVelocity();

        wasGroundedLastFrame = isGrounded;

        float maxSpeed = 40f;
        if (controller.rb.linearVelocity.magnitude > maxSpeed)
        {
            controller.rb.linearVelocity = controller.rb.linearVelocity.normalized * maxSpeed;
        }

        // 🔧 이번 프레임에 스크립트가 최종적으로 확정한 X속도를 기억해둔다.
        // 다음 FixedUpdate 시작 시 이 값과 실제 값이 다르면, 그 사이(물리 시뮬레이션 단계)에
        // 충돌 해소 과정에서 X속도가 건드려졌다는 뜻이 된다.
        lastCommandedVelocityX = controller.rb.linearVelocity.x;
        hasCommandedVelocity = true;
    }

    private void LogDebug(string message)
    {
        if (debugLogOverlap) Debug.Log($"[겹침디버그:{gameObject.name}] {message}", this);
    }

    /// <summary>
    /// 🔧 [핵심] 다른 플레이어와 실제로 "깊게" 겹쳐있다면(=단순히 스치는 정도가 아니라 콜라이더가
    /// 서로 파고든 상태), 물리엔진의 다음 스텝을 기다리지 않고 여기서 직접 침투 깊이(AABB 기준
    /// 최소 분리 거리)를 계산해서 즉시 떼어놓는다. 얕게 스치는 정도(허용 오차 이하)는 건드리지
    /// 않고 물리엔진에 맡긴다 — "밟고 서있기"처럼 정상적으로 맞닿아있는 상태는 겹침이 아니라
    /// 거의 0에 가까우므로 여기 걸리지 않는다.
    /// </summary>
    private void ResolvePlayerOverlap()
    {
        Vector2 boxCenter = controller.bodyCollider.bounds.center;
        Vector2 boxSize = (Vector2)controller.bodyCollider.bounds.size + new Vector2(0.02f, 0.02f);

        int hitCount = Physics2D.OverlapBox(boxCenter, boxSize, 0f, playerFilter, playerCheckResults);

        if (debugLogOverlap && hitCount > 0)
        {
            LogDebug($"OverlapBox 감지됨 - hitCount={hitCount} 내위치={controller.rb.position} 내속도={controller.rb.linearVelocity} 중력스케일={controller.rb.gravityScale} 질량={controller.rb.mass} isGrounded={isGrounded} isTouchingPlayer={isTouchingPlayer} hasPlayerOnHead={hasPlayerOnHead}");
        }

        for (int i = 0; i < hitCount; i++)
        {
            Collider2D col = playerCheckResults[i];
            if (col.gameObject == gameObject || col.isTrigger) continue;

            Bounds myBounds = controller.bodyCollider.bounds;
            Bounds otherBounds = col.bounds;

            float overlapX = Mathf.Min(myBounds.max.x, otherBounds.max.x) - Mathf.Max(myBounds.min.x, otherBounds.min.x);
            float overlapY = Mathf.Min(myBounds.max.y, otherBounds.max.y) - Mathf.Max(myBounds.min.y, otherBounds.min.y);

            LogDebug($"상대={col.gameObject.name} overlapX={overlapX:F4} overlapY={overlapY:F4} (허용치={overlapPushTolerance})");

            // 살짝 스치는 정도(허용 오차 이하)는 물리엔진이 알아서 처리하도록 둔다.
            if (overlapX <= overlapPushTolerance || overlapY <= overlapPushTolerance) continue;

            Vector2 vel = controller.rb.linearVelocity;
            Vector2 pos = controller.rb.position;

            // 더 얕게 겹친 축으로 밀어낸다 (표준 AABB 최소 분리 벡터 방식)
            if (overlapX < overlapY)
            {
                float dir = myBounds.center.x <= otherBounds.center.x ? -1f : 1f;
                pos.x += dir * overlapX;
                if (vel.x * dir < 0f) vel.x = 0f; // 다시 파고드는 방향 속도는 제거
                LogDebug($"⚠️ 강제 분리 실행(X축) - dir={dir} 이동량={dir * overlapX:F4} 보정후위치={pos}");
            }
            else
            {
                float dir = myBounds.center.y <= otherBounds.center.y ? -1f : 1f;
                pos.y += dir * overlapY;
                if (vel.y * dir < 0f) vel.y = 0f;
                LogDebug($"⚠️ 강제 분리 실행(Y축) - dir={dir} 이동량={dir * overlapY:F4} 보정후위치={pos}");
            }

            controller.rb.position = pos;
            controller.rb.linearVelocity = vel;
            break; // 한 프레임에 한 명씩만 처리 (여러 명과 동시에 겹쳐도 몇 프레임 안에 순차적으로 다 풀림)
        }
    }
    /// <summary>
    /// 플레이어끼리 박스 콜라이더가 겹칠 때(특히 모서리), Unity/Box2D 물리엔진이 겹침을
    /// 풀어내면서 자동으로 주입하는 X축 속도(불필요한 옆방향 튐)를 무효화한다.
    /// 겹침 자체를 막는 실제 충돌 처리는 절대 건드리지 않는다 — Y축과 "겹침 방지"는
    /// 100% 물리엔진에 맡기고, 그 결과로 생긴 원치 않는 X속도만 지운다.
    /// </summary>
    private void CancelUnexpectedPlayerCollisionPush()
    {
        if (!hasCommandedVelocity) return; // 첫 프레임은 비교 기준이 없으므로 스킵

        // 넉백/기절 중에는 다른 시스템이 의도적으로 X속도를 바꾸는 것이므로 건드리지 않는다.
        if (controller.knockback != null && (controller.knockback.isKnockedBack || controller.knockback.IsStunned))
            return;

        // 실제로 다른 플레이어와 겹쳐있을 때만 보정한다. (벽 충돌 등 다른 상황은 건드리지 않음)
        if (!IsOverlappingAnyPlayer()) return;

        float actualX = controller.rb.linearVelocity.x;
        float diff = actualX - lastCommandedVelocityX;

        LogDebug($"코너보정 체크 - 이전확정속도={lastCommandedVelocityX:F3} 실제속도={actualX:F3} diff={diff:F3} (허용치={cornerPushEpsilon})");

        if (Mathf.Abs(diff) > cornerPushEpsilon)
        {
            LogDebug($"⚠️ 코너보정 발동 - {actualX:F3} → {lastCommandedVelocityX:F3} 으로 되돌림");
            controller.rb.linearVelocity = new Vector2(lastCommandedVelocityX, controller.rb.linearVelocity.y);
        }
    }

    /// <summary>
    /// 마찰 계산용 CheckTouchingPlayer()는 y축을 줄여서 검사하기 때문에 정작 모서리가
    /// 닿는 지점은 감지 범위 밖일 수 있다. 코너 충돌 보정은 콜라이더 전체 범위를 그대로 써서
    /// 다른 플레이어와 조금이라도 닿아있으면 놓치지 않고 잡는다.
    /// </summary>
    private bool IsOverlappingAnyPlayer()
    {
        Vector2 boxCenter = controller.bodyCollider.bounds.center;
        Vector2 boxSize = (Vector2)controller.bodyCollider.bounds.size + new Vector2(0.02f, 0.02f);

        int hitCount = Physics2D.OverlapBox(boxCenter, boxSize, 0f, playerFilter, playerCheckResults);
        for (int i = 0; i < hitCount; i++)
        {
            Collider2D col = playerCheckResults[i];
            if (col.gameObject != gameObject && !col.isTrigger) return true;
        }
        return false;
    }

    private void UpdateTimers() { if (ckTimer > 0f) ckTimer -= Time.deltaTime; }

    private void UpdateCoyoteTime()
    {
        if (isGrounded && ckTimer <= 0f) coyoteTimeCounter = coyoteTime;
        else coyoteTimeCounter -= Time.deltaTime;
    }

    private void HandleJumpInput()
    {
        if (PauseManager.instance != null && PauseManager.instance.isPaused)
            return;

        if (EmojiRadialMenu.Instance != null &&
            EmojiRadialMenu.Instance.IsOpen())
            return;
        if (controller.combineHandler == null || !controller.combineHandler.isCombined)
        {
            if (controller.input.JumpPressedThisFrame && coyoteTimeCounter > 0f && !controller.knockback.IsStunned && !hasPlayerOnHead)
            {
                Jump();
                coyoteTimeCounter = 0f;
            }

            bool inverted = controller.gravityModule != null && controller.gravityModule.isGravityInverted;
            bool isMovingUp = inverted ? (controller.rb.linearVelocity.y < 0f) : (controller.rb.linearVelocity.y > 0f);

            if (controller.input.JumpReleasedThisFrame && isMovingUp && !controller.knockback.IsStunned)
            {
                if (controller.syncJumpHandler != null && controller.syncJumpHandler.isInSyncZone) controller.syncJumpHandler.CmdCutSyncJump();
                else ApplyShortJump();
            }
        }
    }

    public void Jump()
    {
        if (controller.knockback.isKnockedBack) return;
        float gravity = Mathf.Abs(Physics2D.gravity.y) * jumpSpeed;
        float jumpForce = Mathf.Sqrt(2f * gravity * jumpHeight);
        float mult = controller.gravityModule != null ? controller.gravityModule.gravityMultiplier : 1f;
        controller.rb.linearVelocity = new Vector2(controller.rb.linearVelocity.x, jumpForce * mult);
        ckTimer = jumpCk;

        justJumped = true;
    }

    public void ApplyShortJump()
    {
        bool inverted = controller.gravityModule != null && controller.gravityModule.isGravityInverted;
        bool isMovingUpCheck = inverted ? (controller.rb.linearVelocity.y < 0f) : (controller.rb.linearVelocity.y > 0f);
        if (isMovingUpCheck) controller.rb.linearVelocity = new Vector2(controller.rb.linearVelocity.x, controller.rb.linearVelocity.y * superJump);
    }

    private void UpdateGravity()
    {
        bool isInverted = controller.gravityModule != null && controller.gravityModule.isGravityInverted;
        float mult = controller.gravityModule != null ? controller.gravityModule.gravityMultiplier : 1f;
        bool isFalling = isInverted ? (controller.rb.linearVelocity.y > 0f) : (controller.rb.linearVelocity.y < 0f);
        controller.rb.gravityScale = isFalling ? (jumpSpeed * fallSpeed * mult) : (jumpSpeed * mult);
    }

    private void HandleMovementPhysics()
    {
        float rawInput = controller.input.HorizontalInput;

        if ((PauseManager.instance != null && PauseManager.instance.isPaused) ||
            (EmojiRadialMenu.Instance != null && EmojiRadialMenu.Instance.IsOpen()))
        {
            rawInput = 0f;
        }
        if (controller.knockback.isKnockedBack) return;

        if (controller.knockback.IsStunned)
        {
            float slideSpeed = Mathf.Lerp(controller.rb.linearVelocity.x, 0f, 10f * Time.fixedDeltaTime);
            controller.rb.linearVelocity = new Vector2(slideSpeed, controller.rb.linearVelocity.y);
            return;
        }

        if (controller.currentReverseZone != null && !controller.currentReverseZone.isForward)
        {
            float originalInput = 0f;
            if (controller.combineHandler != null && controller.combineHandler.isCombined)
                originalInput = controller.combineHandler.GetCombinedHorizontalInput();
            else
                originalInput = controller.input.moveAction.ReadValue<float>();

            if ((originalInput > 0 && rawInput > 0) || (originalInput < 0 && rawInput < 0))
            {
                rawInput *= -1f;
            }
        }

        isTouchingPlayer = false;
        if (Mathf.Abs(rawInput) > 0.1f)
        {
            float moveDir = Mathf.Sign(rawInput);
            Vector2 boxCenter = controller.bodyCollider.bounds.center;
            Vector2 boxSize = controller.bodyCollider.bounds.size;
            boxSize.y -= 0.3f;

            // 🔧 [핵심 수정] 감지 거리가 고정 0.05f였는데, moveSpeed=7 기준 한 프레임 최대 이동거리(약 0.14,
            // Time.fixedDeltaTime=0.02 가정)보다 짧아서 빠르게 다가갈 때 감지망을 매 프레임 살짝씩 피해서
            // 파고들 수 있었다(로그의 overlapX가 조금씩 깊어지던 것과 일치). 실제 이번 프레임에 움직일 수
            // 있는 거리 + 약간의 여유만큼 항상 커버하도록 동적으로 계산한다.
            float currentSpeedX = Mathf.Abs(controller.rb.linearVelocity.x);
            float castDistance = Mathf.Max(0.05f, currentSpeedX * Time.fixedDeltaTime + 0.05f);

            RaycastHit2D hit = Physics2D.BoxCast(boxCenter, boxSize, 0f, new Vector2(moveDir, 0f), castDistance, playerLayerMask);
            if (debugLogOverlap && hit.collider != null && hit.collider.gameObject != gameObject)
            {
                LogDebug($"BoxCast 차단됨 - castDistance={castDistance:F3} 상대={hit.collider.gameObject.name} hit.distance={hit.distance:F3}");
            }
            if (hit.collider != null && hit.collider.gameObject != gameObject)
            {
                rawInput = 0f;
                // isTouchingPlayer = true; ❌ 이 부분 삭제! (이제 방어막 해제 용도로만 씁니다)
            }
        }

        float targetVelocityX = (rawInput * moveSpeed) + windVelocity;
        float currentPlatformVelX = isGrounded ? platformVelocity.x : 0f;

        float currentFriction = isGrounded
            ? (isTouchingPlayer ? 15f : normalFriction)
            : (isTouchingPlayer ? 15f : (isRestrictedByRope ? 0f : 9999f));

        float newX;

        if (isOnIce || (!isGrounded && wasOnIceLastFrame))
        {
            if (Mathf.Abs(rawInput) > 0.01f)
            {
                newX = Mathf.MoveTowards(
                    controller.rb.linearVelocity.x,
                    targetVelocityX + currentPlatformVelX,
                    iceAcceleration * Time.fixedDeltaTime
                );
            }
            else
            {
                newX = Mathf.MoveTowards(
                    controller.rb.linearVelocity.x,
                    windVelocity + currentPlatformVelX,
                    iceSlideFriction * Time.fixedDeltaTime
                );
            }
        }
        else
        {
            newX = Mathf.MoveTowards(
                controller.rb.linearVelocity.x,
                targetVelocityX + currentPlatformVelX,
                (currentFriction > 100f ? currentFriction : currentFriction * moveSpeed) * Time.fixedDeltaTime
            );
        }

        controller.rb.linearVelocity = new Vector2(newX, controller.rb.linearVelocity.y);
    }
    void CheckTouchingPlayer()
    {
        Vector2 boxCenter = controller.bodyCollider.bounds.center;
        Vector2 boxSize = controller.bodyCollider.bounds.size;
        boxSize.x += 0.1f; // 좌우로 아주 살짝 늘려서 닿았는지 판정
        boxSize.y -= 0.2f; // 바닥 판정과 겹치지 않게 살짝 줄임

        int hitCount = Physics2D.OverlapBox(boxCenter, boxSize, 0f, playerFilter, playerCheckResults);
        isTouchingPlayer = false;

        for (int i = 0; i < hitCount; i++)
        {
            Collider2D col = playerCheckResults[i];
            if (col.gameObject != gameObject && !col.isTrigger)
            {
                isTouchingPlayer = true;
                break;
            }
        }
    }
    private void ClampVelocity()
    {
        float maxSpeedX = 30f;
        Vector2 clampedVelocity = controller.rb.linearVelocity;
        clampedVelocity.x = Mathf.Clamp(clampedVelocity.x, -maxSpeedX, maxSpeedX);

        bool isInverted = controller.gravityModule != null && controller.gravityModule.isGravityInverted;
        clampedVelocity.y = Mathf.Clamp(clampedVelocity.y, isInverted ? -maxFallSpeed * 1.5f : -maxFallSpeed, isInverted ? maxFallSpeed : maxFallSpeed * 1.5f);

        bool isKnocked = controller.knockback != null && controller.knockback.isKnockedBack;

        // 🚀 [핵심 수정]: !hasPlayerOnHead 와 !isTouchingPlayer 추가!
        if (wasGroundedLastFrame && !justJumped && !isKnocked && !hasPlayerOnHead && !isTouchingPlayer)
        {
            float allowedSpeed = currentPlatform != null ? platformVelocity.y : 0f;
            if (isInverted)
            {
                if (clampedVelocity.y < allowedSpeed - 0.1f) clampedVelocity.y = allowedSpeed;
            }
            else
            {
                if (clampedVelocity.y > allowedSpeed + 0.1f) clampedVelocity.y = allowedSpeed;
            }
        }

        controller.rb.linearVelocity = clampedVelocity;
    }

    void CheckGroundOrPlayer()
    {
        int hitCount = Physics2D.OverlapCircle(groundCheck.position, checkRadius, groundFilter, groundCheckResults);

        isGrounded = false;
        bool currentOnIce = false;
        bool foundPlatform = false;
        Transform detectedPlatform = null;

        for (int i = 0; i < hitCount; i++)
        {
            Collider2D col = groundCheckResults[i];
            if (col.CompareTag("Spike") || col.gameObject == gameObject || col.isTrigger) continue;

            isGrounded = true;
            if (col.CompareTag("Ice")) currentOnIce = true;
            if (col.CompareTag("MovingPlatform"))
            {
                detectedPlatform = col.transform;
                foundPlatform = true;
            }
        }

        if (!isGrounded)
        {
            int playerHitCount = Physics2D.OverlapCircle(groundCheck.position, checkRadius, playerFilter, playerCheckResults);
            for (int i = 0; i < playerHitCount; i++)
            {
                Collider2D col = playerCheckResults[i];
                if (col.gameObject != gameObject && col.CompareTag("Player") && groundCheck.position.y > col.bounds.max.y - 0.05f)
                {
                    isGrounded = true;
                    break;
                }
            }
        }

        if (isGrounded && Mathf.Abs(controller.rb.linearVelocity.y) <= 0.1f) justJumped = false;

        if (foundPlatform)
        {
            if (currentPlatform != detectedPlatform)
            {
                currentPlatform = detectedPlatform;
                CachePlatformVelocityDelegate(currentPlatform);
            }
        }
        else
        {
            currentPlatform = null;
            getPlatformVelocityFunc = null;
        }

        isOnIce = currentOnIce;
        if (isGrounded) wasOnIceLastFrame = isOnIce;
    }

    void CheckHeadForPlayer()
    {
        if (headCheck == null) return;
        hasPlayerOnHead = false;
        Vector2 checkPosition = (Vector2)headCheck.position + headCheckOffset;

        int hitCount = Physics2D.OverlapBox(checkPosition, headCheckBoxSize, 0f, playerFilter, headCheckResults);

        for (int i = 0; i < hitCount; i++)
        {
            Collider2D col = headCheckResults[i];
            if (col.gameObject == gameObject || col.isTrigger) continue;

            Vector2 dir = col.transform.position - transform.position;
            if (dir.y <= 0.2f) continue;
            if (col.attachedRigidbody != null && col.attachedRigidbody.linearVelocity.y > 0.1f) continue;

            hasPlayerOnHead = true;
            break;
        }
    }

    private void CachePlatformVelocityDelegate(Transform platform)
    {
        if (platform == null) { getPlatformVelocityFunc = null; return; }

        var roundTrip = platform.GetComponentInParent<CoopRoundTripPlatform>();
        if (roundTrip != null) { getPlatformVelocityFunc = () => roundTrip.CurrentVelocity; return; }

        var patrol = platform.GetComponentInParent<CoopPatrolPlatform>();
        if (patrol != null) { getPlatformVelocityFunc = () => patrol.CurrentVelocity; return; }

        var log = platform.GetComponentInParent<CoopMovingLog>();
        if (log != null) { getPlatformVelocityFunc = () => log.CurrentVelocity; return; }

        var parallel = platform.GetComponentInParent<CoopParallelPlatform>();
        if (parallel != null) { getPlatformVelocityFunc = () => parallel.CurrentVelocity; return; }

        var mirror = platform.GetComponentInParent<CoopMirrorPlatforms>();
        if (mirror != null)
        {
            bool isLeft = platform.gameObject == mirror.leftPlatform.gameObject || platform.IsChildOf(mirror.leftPlatform.transform);
            getPlatformVelocityFunc = () => isLeft ? mirror.LeftVelocity : mirror.RightVelocity;
            return;
        }
        getPlatformVelocityFunc = null;
    }

    private void HandleMovingPlatform()
    {
        platformVelocity = getPlatformVelocityFunc != null ? getPlatformVelocityFunc() : Vector2.zero;
    }

    public void CallCombinedJump()
    {
        if ((isGrounded || coyoteTimeCounter > 0f) && !hasPlayerOnHead)
        {
            controller.rb.linearVelocity = new Vector2(controller.rb.linearVelocity.x, 0f);
            Jump();
            coyoteTimeCounter = 0f;
        }
    }

    public void CallCombinedAction()
    {
        controller.rb.linearVelocity = new Vector2(controller.rb.linearVelocity.x, 0f);
        controller.rb.AddForce(Vector2.down * 18f, ForceMode2D.Impulse);
    }

    private void OnDrawGizmosSelected()
    {
        if (!showGizmo) return;
        if (groundCheck != null)
        {
            Gizmos.color = Color.yellow;
            Gizmos.DrawWireSphere((Vector2)groundCheck.position + checkOffset, checkRadius);
        }
        if (headCheck != null)
        {
            Gizmos.color = Color.cyan;
            Gizmos.DrawWireCube((Vector2)headCheck.position + headCheckOffset, headCheckBoxSize);
        }
    }


}