using UnityEngine;
using Mirror;

public class CoopShurikenTrap : NetworkBehaviour
{
    [Header("연결할 오브젝트")]
    [Tooltip("실제로 움직이고 회전할 표창 오브젝트(Shuriken_Active)를 넣어주세요.")]
    public Transform targetShuriken;

    [Header("운동 설정")]
    public float moveSpeed = 3f;
    public float moveDistance = 2f;
    public float rotationSpeed = 360f;

    private Vector3 startLocalPosition;

    void Start()
    {
        if (targetShuriken != null)
        {
            startLocalPosition = targetShuriken.localPosition;
        }
    }

    [ServerCallback]
    void Update()
    {
        if (targetShuriken == null) return;
        float pingPongY = Mathf.Sin(Time.time * moveSpeed) * moveDistance;
        targetShuriken.localPosition = startLocalPosition + new Vector3(0, pingPongY, 0);
        targetShuriken.Rotate(0, 0, rotationSpeed * Time.deltaTime);
    }

    [ServerCallback]
    private void OnTriggerEnter2D(Collider2D other)
    {
        if (!other.CompareTag("Player")) return;
        PlayerKnockback knockbackScript = other.GetComponent<PlayerKnockback>();

        if (knockbackScript != null)
        {
            knockbackScript.ApplyKnockbackFromEye(transform.position);
            Debug.Log($"[Shuriken Trap] 플레이어 '{other.name}' 피격! 왼쪽으로 넉백시킵니다.");
        }
    }
}