using UnityEngine;

public class EnemyMove : MonoBehaviour
{
    public float moveSpeed = 3f;
    private int moveDirection = 1; // 1이면 오른쪽, -1이면 왼쪽

    private Rigidbody2D rb;

    void Start()
    {
        rb = GetComponent<Rigidbody2D>();
    }

    void FixedUpdate()
    {
        // 이동: 현재 방향대로 속도를 줍니다
        rb.linearVelocity = new Vector2(moveDirection * moveSpeed, rb.linearVelocity.y);
    }

    private void OnCollisionEnter2D(Collision2D collision)
    {
        // 'Wall'이라는 태그가 붙은 오브젝트에 닿으면?
        if (collision.gameObject.CompareTag("Wall"))
        {
            Flip(); // 방향 뒤집기!
        }
    }

    void Flip()
    {
        // 방향을 반대로 (1 -> -1, -1 -> 1)
        moveDirection *= -1;

        // 슬라임 이미지도 실제 왼쪽/오른쪽으로 뒤집어주기
        transform.localScale = new Vector3(moveDirection, 1, 1);
    }
}