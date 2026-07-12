using UnityEngine;
using Mirror;
using System.Collections;

public class EyeGimmick : NetworkBehaviour
{
    [Header("눈 감시 설정")]
    public float onDuration = 2f;
    public float offDuration = 2f;
    [Tooltip("빛을 가려줄 지형지물(땅)의 레이어 마스크")]
    public LayerMask obstacleLayer;

    [Header("비주얼 설정")]
    public SpriteRenderer eyeRenderer;
    public Color onColor = Color.red;
    public Color offColor = Color.gray;

    [SyncVar(hook = nameof(OnGimmickActiveChanged))]
    public bool isGimmickActive = false;

    [SyncVar(hook = nameof(OnEyeOnChanged))]
    public bool isEyeOn = false;

    private void Start()
    {
        UpdateVisuals(); // 시작 시 무조건 꺼진 상태로 세팅
    }

    // EyeRoomTrigger에서 4명이 들어오면 호출하는 함수
    [Server]
    public void StartEyeGimmick()
    {
        if (isGimmickActive) return;

        isGimmickActive = true;
        StartCoroutine(EyeToggleRoutine());
    }

    [Server]
    private IEnumerator EyeToggleRoutine()
    {
        // 원한다면 여기에 시작 전 대기 시간을 넣어도 됩니다. (예: yield return new WaitForSeconds(1f);)
        while (isGimmickActive)
        {
            isEyeOn = true;
            yield return new WaitForSeconds(onDuration);

            isEyeOn = false;
            yield return new WaitForSeconds(offDuration);
        }
    }

    // 🌟 이 충돌은 오직 '눈 시야 콜라이더' 영역에서만 일어남!
    [ServerCallback]
    private void OnTriggerStay2D(Collider2D other)
    {
        if (!isGimmickActive || !isEyeOn) return;

        if (other.CompareTag("Player"))
        {
            Vector2 directionToPlayer = other.transform.position - transform.position;
            float distanceToPlayer = directionToPlayer.magnitude;

            RaycastHit2D hit = Physics2D.Raycast(transform.position, directionToPlayer.normalized, distanceToPlayer, obstacleLayer);

            if (hit.collider == null) // 가려주는 땅이 없다면
            {
                NetworkIdentity identity = other.GetComponent<NetworkIdentity>();
                if (identity != null)
                {
                    TargetTriggerKnockback(identity.connectionToClient, other.gameObject);
                }
            }
        }
    }

    [TargetRpc]
    private void TargetTriggerKnockback(NetworkConnection target, GameObject playerObj)
    {
        if (playerObj != null)
        {
            PlayerKnockback knockback = playerObj.GetComponent<PlayerKnockback>();
            if (knockback != null)
            {
                knockback.ApplyKnockbackFromEye(transform.position);
            }
        }
    }

    private void OnGimmickActiveChanged(bool oldVal, bool newVal) { UpdateVisuals(); }
    private void OnEyeOnChanged(bool oldVal, bool newVal) { UpdateVisuals(); }

    private void UpdateVisuals()
    {
        if (eyeRenderer != null)
        {
            if (!isGimmickActive) eyeRenderer.color = offColor;
            else eyeRenderer.color = isEyeOn ? onColor : offColor;
        }
    }
}