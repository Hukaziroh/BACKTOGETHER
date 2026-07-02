using UnityEngine;

public class ClearTextFloat : MonoBehaviour
{
    public float floatSpeed = 2f;
    private float timer = 0f;

    void OnEnable() { timer = 0f; } // 활성화될 때마다 초기화

    void Update()
    {
        timer += Time.deltaTime;
        // 위로 이동
        transform.Translate(Vector3.up * floatSpeed * Time.deltaTime);

        // 1.5초 지나면 사라짐
        if (timer > 1.5f) gameObject.SetActive(false);
    }
}