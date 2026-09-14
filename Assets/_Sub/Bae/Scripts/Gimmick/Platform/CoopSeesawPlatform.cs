using UnityEngine;
using Mirror;

public class CoopSeesawPlatform : NetworkBehaviour
{
    [Header("시소 설정")]
    [Tooltip("최대 기울기 각도 (예: 45도까지만 기울어짐)")]
    public float maxAngle = 45f;
    [Tooltip("기울어지는 속도")]
    public float rotationSpeed = 3f;
    [Tooltip("무게 민감도 (높을수록 플레이어가 조금만 옆으로 가도 확 기울어짐)")]
    public float weightSensitivity = 15f;
    [Tooltip("아무도 없을 때 수평으로 돌아오는 속도")]
    public float returnSpeed = 1.5f;

    [Header("물리 설정")]
    [Tooltip("시소 발판의 Rigidbody2D (Kinematic이어야 함)")]
    public Rigidbody2D platformRb;

    [Header("플레이어 감지 영역")]
    [Tooltip("인스펙터에서 기즈모(노란 선)를 보며 발판 크기에 맞게 조절하세요.")]
    public Vector2 checkSize = new Vector2(6f, 1f);
    public Vector2 checkOffset = new Vector2(0f, 0.5f);
    public LayerMask playerLayer;

    private Collider2D[] hitBuffer = new Collider2D[16];

    [ServerCallback]
    void FixedUpdate()
    {
        if (platformRb == null) return;
                ContactFilter2D filter = new ContactFilter2D();
        filter.SetLayerMask(playerLayer);
        filter.useLayerMask = true;
        
        int hitCount = Physics2D.OverlapBox(
            (Vector2)transform.position + (Vector2)(transform.rotation * checkOffset),
            checkSize,
            platformRb.rotation,
            filter,
            hitBuffer
        );

        float targetAngle = 0f;

        if (hitCount > 0)
        {
            float totalBalance = 0f;

            for (int i = 0; i < hitCount; i++)
            {
                var hit = hitBuffer[i];
                if (hit.CompareTag("Player"))
                {
                    Vector3 localPos = transform.InverseTransformPoint(hit.transform.position);
                    totalBalance += localPos.x; 
                }
            }
            targetAngle = -totalBalance * weightSensitivity;
            targetAngle = Mathf.Clamp(targetAngle, -maxAngle, maxAngle);
        }
        float speed = (hitCount > 0) ? rotationSpeed : returnSpeed;
        float nextAngle = Mathf.LerpAngle(platformRb.rotation, targetAngle, Time.fixedDeltaTime * speed);
        platformRb.MoveRotation(nextAngle);
    }
    private void OnDrawGizmosSelected()
    {
        Gizmos.color = Color.yellow;
        Matrix4x4 rotationMatrix = Matrix4x4.TRS(transform.position, transform.rotation, transform.localScale);
        Gizmos.matrix = rotationMatrix;
        Gizmos.DrawWireCube(checkOffset, checkSize);
    }
}