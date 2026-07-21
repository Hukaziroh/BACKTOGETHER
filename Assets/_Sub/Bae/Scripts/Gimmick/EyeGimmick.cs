using UnityEngine;
using Mirror;
using System.Collections;

public class EyeGimmick : NetworkBehaviour
{
    [Header("눈 감시 설정")]
    [Tooltip("눈이 켜져 있는 시간 (초)")]
    public float onDuration = 2f;
    [Tooltip("눈이 꺼져 있는 시간 (초)")]
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
        UpdateVisuals(); 
    }

    [Server]
    public void StartEyeGimmick()
    {
        if (isGimmickActive) return;

        isGimmickActive = true;
        StartCoroutine(EyeToggleRoutine());
    }

    [Server]
    public void ClearGimmick()
    {
        if (!isGimmickActive) return;

        isGimmickActive = false;
        isEyeOn = false;
        Debug.Log("[첫번째 눈 기믹] 클리어 완료! 눈이 영구적으로 꺼집니다.");
    }

    [Server]
    private IEnumerator EyeToggleRoutine()
    {
        while (isGimmickActive)
        {
            isEyeOn = true;
            yield return new WaitForSeconds(onDuration);

            if (!isGimmickActive) break;

            isEyeOn = false;
            yield return new WaitForSeconds(offDuration);
        }
    }

    [ServerCallback]
    private void OnTriggerStay2D(Collider2D other)
    {
        if (!isGimmickActive || !isEyeOn) return;

        if (other.CompareTag("Player"))
        {
            Vector2 directionToPlayer = other.transform.position - transform.position;
            float distanceToPlayer = directionToPlayer.magnitude;

            RaycastHit2D hit = Physics2D.Raycast(transform.position, directionToPlayer.normalized, distanceToPlayer, obstacleLayer);

            Debug.DrawRay(
           transform.position,
           directionToPlayer.normalized * distanceToPlayer,
           hit.collider == null ? Color.green : Color.red
       );

            Debug.Log(
            $"[EyeGimmick] 플레이어: {other.name} | " +
            $"눈 위치: {transform.position} | " +
            $"플레이어 위치: {other.transform.position} | " +
            $"Raycast 결과: {(hit.collider != null ? hit.collider.name : "없음")} | " +
            $"Layer: {(hit.collider != null ? LayerMask.LayerToName(hit.collider.gameObject.layer) : "없음")}"
        );

            if (hit.collider == null) 
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
            PlayerKnockback knockback = playerObj.GetComponent<NetworkIdentity>().GetComponent<PlayerKnockback>();
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