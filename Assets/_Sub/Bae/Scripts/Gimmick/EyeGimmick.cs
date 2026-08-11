using UnityEngine;
using Mirror;
using System.Collections;
using System.Collections.Generic;

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

    private struct RayGizmoData
    {
        public Vector2 start;
        public Vector2 end;
        public Vector2 hitPoint;
        public bool blocked;
    }
    private readonly Dictionary<Collider2D, RayGizmoData> rayGizmos
    = new Dictionary<Collider2D, RayGizmoData>();

    private void Start()
    {
        UpdateVisuals();
    }

    [Server]
    public void StartEyeGimmick()
    {
        if (isGimmickActive)
            return;

        isGimmickActive = true;
        StartCoroutine(EyeToggleRoutine());
    }

    [Server]
    public void ClearGimmick()
    {
        if (!isGimmickActive)
            return;

        isGimmickActive = false;
        isEyeOn = false;

        rayGizmos.Clear();
    }

    [Server]
    private IEnumerator EyeToggleRoutine()
    {
        while (isGimmickActive)
        {
            isEyeOn = true;

            yield return new WaitForSeconds(onDuration);

            if (!isGimmickActive)
                break;

            isEyeOn = false;

            yield return new WaitForSeconds(offDuration);
        }
    }

    [ServerCallback]
    private void OnTriggerStay2D(Collider2D other)
    {
        if (!isGimmickActive || !isEyeOn)
            return;

        if (!other.CompareTag("Player"))
            return;

        Vector2 playerPosition = other.transform.position;

        float eyeY = transform.position.y;

        float distance = eyeY - playerPosition.y;

        if (distance <= 0f)
            return;

        Vector2 rayStart = new Vector2(
            playerPosition.x,
            eyeY
        );

        Vector2 rayDirection = Vector2.down;

        RaycastHit2D hit = Physics2D.Raycast(
            rayStart,
            rayDirection,
            distance,
            obstacleLayer
        );

        RayGizmoData data = new RayGizmoData
        {
            start = rayStart,
            end = playerPosition,
            blocked = hit.collider != null,
            hitPoint = hit.collider != null
         ? hit.point
         : playerPosition
        };

        rayGizmos[other] = data;
        Debug.DrawRay(
            rayStart,
            rayDirection * distance,
            hit.collider == null ? Color.green : Color.red
        );

        if (hit.collider == null)
        {
            PlayerKnockback knockback =
                other.GetComponent<PlayerKnockback>();

            if (knockback != null)
            {
                knockback.ApplyKnockbackFromEye(
                    transform.position
                );
            }
        }
    }

    private void OnGimmickActiveChanged(
        bool oldVal,
        bool newVal)
    {
        UpdateVisuals();
    }

    private void OnEyeOnChanged(
        bool oldVal,
        bool newVal)
    {
        UpdateVisuals();
    }

    private void UpdateVisuals()
    {
        if (eyeRenderer != null)
        {
            if (!isGimmickActive)
            {
                eyeRenderer.color = offColor;
            }
            else
            {
                eyeRenderer.color =
                    isEyeOn ? onColor : offColor;
            }
        }
    }


    private void OnDrawGizmos()
    {
        if (!Application.isPlaying)
            return;

        if (!isGimmickActive || !isEyeOn)
            return;

        foreach (RayGizmoData ray in rayGizmos.Values)
        {
            Gizmos.color =
                ray.blocked
                    ? Color.red
                    : Color.green;


            Gizmos.DrawLine(
                ray.start,
                ray.end
            );

            Gizmos.color = Color.yellow;

            Gizmos.DrawWireSphere(
                ray.start,
                0.1f
            );

            Gizmos.color = Color.cyan;

            Gizmos.DrawWireSphere(
                ray.end,
                0.12f
            );

            if (ray.blocked)
            {
                Gizmos.color = Color.magenta;
                Gizmos.DrawWireSphere(
                    ray.hitPoint,
                    0.15f
                );
                Gizmos.DrawLine(
                    ray.hitPoint + Vector2.left * 0.15f,
                    ray.hitPoint + Vector2.right * 0.15f
                );
                Gizmos.DrawLine(
                    ray.hitPoint + Vector2.up * 0.15f,
                    ray.hitPoint + Vector2.down * 0.15f
                );
            }
        }
    }
}