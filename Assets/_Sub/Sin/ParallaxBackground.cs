using UnityEngine;

public class ParallaxBackground : MonoBehaviour
{
    [Range(0f, 1f)]
    public float parallaxFactor = 0.5f;

    private Transform cam;
    private float previousCamX;

    void Start()
    {
        cam = Camera.main.transform;
        previousCamX = cam.position.x;
    }

    void LateUpdate()
    {
        float delta = cam.position.x - previousCamX;
        transform.position += new Vector3(delta * parallaxFactor, 0f, 0f);
        previousCamX = cam.position.x;
    }
}
