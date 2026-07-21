using UnityEngine;
using Mirror;

public class CoopTogglePlatform : NetworkBehaviour
{
    [Header("점멸 타이밍 설정")]
    [Tooltip("발판이 유지되는 시간 (초)")]
    public float toggleInterval = 2.0f;
    [Tooltip("체크 시 켜진 상태로 시작, 해제 시 꺼진 상태로 시작")]
    public bool startActive = true;

    [Header("연결할 컴포넌트")]
    public GameObject platformVisual;
    public BoxCollider2D platformCollider;

    [Header("끼임 방지 설정")]
    public LayerMask playerLayer;
    [Tooltip("물리 콜라이더 대비 내부 스캔 영역의 비율 (0.9 권장)")]
    [Range(0.5f, 0.99f)]
    public float scanScale = 0.8f;

    [SyncVar(hook = nameof(OnVisualStateChanged))]
    private bool isVisualOn;

    [SyncVar(hook = nameof(OnColliderStateChanged))]
    private bool isColliderOn;

    private float timer = 0f;
    private bool shouldBeActive;

    public override void OnStartServer()
    {
        base.OnStartServer();
        shouldBeActive = startActive;
        timer = 0f;
        ApplyState();
    }

    public override void OnStartClient()
    {
        base.OnStartClient();
        UpdateVisualLocally(isVisualOn);
        UpdateColliderLocally(isColliderOn);
    }

    [ServerCallback]
    void Update()
    {
        timer += Time.deltaTime;

        if (timer >= toggleInterval)
        {
            timer = 0f;
            shouldBeActive = !shouldBeActive;
            ApplyState();
        }

        if (shouldBeActive && !isColliderOn)
        {
            if (!IsPlayerInside())
            {
                isColliderOn = true;
            }
        }
    }

    [Server]
    private void ApplyState()
    {
        if (shouldBeActive)
        {
            isVisualOn = true;
            if (IsPlayerInside()) isColliderOn = false;
            else isColliderOn = true;
        }
        else
        {
            isVisualOn = false;
            isColliderOn = false;
        }
    }

    [Server]
    private bool IsPlayerInside()
    {
        if (platformCollider == null) return false;

        Vector2 center = transform.TransformPoint(platformCollider.offset);

        Vector2 size = new Vector2(
            platformCollider.size.x * Mathf.Abs(transform.lossyScale.x),
            platformCollider.size.y * Mathf.Abs(transform.lossyScale.y)
        ) * scanScale;

        Collider2D hit = Physics2D.OverlapBox(center, size, transform.eulerAngles.z, playerLayer);
        return hit != null;
    }

    private void OnVisualStateChanged(bool oldState, bool newState) { UpdateVisualLocally(newState); }
    private void OnColliderStateChanged(bool oldState, bool newState) { UpdateColliderLocally(newState); }

    private void UpdateVisualLocally(bool state)
    {
        if (platformVisual != null)
        {
            if (platformVisual == this.gameObject)
            {
                SpriteRenderer[] renderers = GetComponentsInChildren<SpriteRenderer>();
                foreach (var sr in renderers)
                {
                    sr.enabled = state;
                }
            }
            else
            {
                platformVisual.SetActive(state);
            }
        }
    }

    private void UpdateColliderLocally(bool state)
    {
        if (platformCollider != null) platformCollider.enabled = state;
    }

    private void OnDrawGizmosSelected()
    {
        if (platformCollider != null)
        {
            Gizmos.color = Color.red;
            Matrix4x4 oldMatrix = Gizmos.matrix;

            Gizmos.matrix = Matrix4x4.TRS(transform.position, transform.rotation, transform.lossyScale);

            Vector2 scaledSize = platformCollider.size * scanScale;
            Gizmos.DrawWireCube(platformCollider.offset, scaledSize);

            Gizmos.matrix = oldMatrix;
        }
    }
}