using UnityEngine;

public class CameraFollow : MonoBehaviour
{
    public Transform target;      
    public float speed = 5f;       
    public Vector3 offset = new Vector3(0f, 0f, -10f);

    void LateUpdate()
    {
        if (target == null) return;

        Vector3 targetPos = target.position + offset;

        transform.position = Vector3.Lerp(transform.position, targetPos, speed * Time.deltaTime);
    }
}