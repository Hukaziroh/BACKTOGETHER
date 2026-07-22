using UnityEngine;
using Mirror;

public class CoopTogglePlatform : NetworkBehaviour
{
    [Header("점멸 타이밍 설정")]
    [Tooltip("발판이 유지되는 기준 주기 시간 (초)")]
    public float toggleInterval = 2.0f;

    [Tooltip("새 발판이 켜진 후 기존 발판이 유지될 추가 여유 시간 (초)")]
    public float overlapDuration = 0.5f;

    [Tooltip("체크 시 A타입(켜지며 시작), 해제 시 B타입(꺼지며 시작)")]
    public bool startActive = true;

    [Header("연결할 컴포넌트")]
    public GameObject platformVisual;
    public BoxCollider2D platformCollider;

    [Header("끼임 방지 설정")]
    public LayerMask playerLayer;
    [Tooltip("물리 콜라이더 대비 내부 스캔 영역의 비율 (0.8 ~ 0.9 권장)")]
    [Range(0.5f, 0.99f)]
    public float scanScale = 0.8f;

    [SyncVar(hook = nameof(OnVisualStateChanged))]
    private bool isVisualOn;

    [SyncVar(hook = nameof(OnColliderStateChanged))]
    private bool isColliderOn;

    private bool shouldBeActive;

    public override void OnStartServer()
    {
        base.OnStartServer();
        ApplyStateCalculation();
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
        ApplyStateCalculation();

        if (shouldBeActive && !isColliderOn)
        {
            if (!IsPlayerInside())
            {
                isColliderOn = true;
            }
        }
    }

    [Server]
    private void ApplyStateCalculation()
    {
        float totalCycle = toggleInterval * 2f;
        float currentTime = Time.time % totalCycle;

        bool nextState = false;

        if (startActive)
        {
            nextState = (currentTime >= 0f && currentTime < (toggleInterval + overlapDuration));
        }
        else
        {
            nextState = (currentTime >= toggleInterval && currentTime < totalCycle) || (currentTime >= 0f && currentTime < overlapDuration);
        }

        if (shouldBeActive != nextState)
        {
            shouldBeActive = nextState;

            if (shouldBeActive)
            {
                isVisualOn = true;
                isColliderOn = !IsPlayerInside();
            }
            else
            {
                isVisualOn = false;
                isColliderOn = false;
            }
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
                foreach (var sr in renderers) sr.enabled = state;
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