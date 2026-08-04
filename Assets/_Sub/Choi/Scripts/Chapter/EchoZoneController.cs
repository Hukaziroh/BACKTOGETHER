using UnityEngine;
using UnityEngine.Rendering.Universal;
using Mirror;

public class EchoZoneController : MonoBehaviour
{
    // 이 프로젝트 라이팅 셋업 특성: Global Light 2D는 꺼져있으면 전체가 기본으로 밝고,
    // 켜지면 그 라이트가 비추는 범위만 밝아지고 나머지는 어두워진다.
    // 그래서 "존에 들어가서 어두워져야 할 때"는 라이트를 꺼야 하는게 아니라 오히려 켜야 한다.
    [Header("존에 들어가면 켤 라이트 (씬의 Global Light 2D)")]
    public Light2D globalLight;

    [Header("존에 들어가면 켤 에코 매니저")]
    public GameObject echoManager;

    // 버튼 4개를 다 눌러서 해결되면 true로 고정 -> 존을 다시 드나들어도 어두워지지 않음
    private bool isSolved = false;

    void Start()
    {
        if (echoManager != null) echoManager.SetActive(false);
    }

    void OnTriggerEnter2D(Collider2D other)
    {
        if (isSolved) return;
        if (!IsLocalPlayer(other)) return;

        SetDark(true);
    }

    void OnTriggerExit2D(Collider2D other)
    {
        if (isSolved) return;
        if (!IsLocalPlayer(other)) return;

        SetDark(false);
    }

    private bool IsLocalPlayer(Collider2D other)
    {
        if (!other.CompareTag("Player")) return false;

        NetworkIdentity identity = other.GetComponentInParent<NetworkIdentity>();
        return identity != null && identity.isLocalPlayer;
    }

    private void SetDark(bool dark)
    {
        // globalLight.enabled만 바꾸면, 오브젝트 자체가 비활성 상태일 때 아무 효과가 없다.
        // 오브젝트 자체를 켜고 꺼야 확실히 반영된다.
        if (globalLight != null) globalLight.gameObject.SetActive(dark);
        if (echoManager != null) echoManager.SetActive(dark);
    }

    // EchoLightButtonManager가 전원 버튼을 다 눌렀을 때 모든 클라이언트에서 호출
    public void ForceLightsOn()
    {
        isSolved = true;
        SetDark(false);
    }
}
