using UnityEngine;

public class EchoModeController : MonoBehaviour
{
    // 이제 여기에는 어떤 타이머나 페이드 계산도 필요 없습니다.
    // 셰이더가 EchoManager가 보낸 전역 변수를 알아서 가져옵니다.

    // 혹시라도 SpriteRenderer가 꺼져있다면 켜주는 역할만 합니다.
    void Start()
    {
        var renderer = GetComponent<SpriteRenderer>();
        if (renderer != null)
        {
            renderer.enabled = true;
        }
    }
}