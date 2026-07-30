using UnityEngine;

public class PanelSwitcher : MonoBehaviour
{
    [Header("패널 설정")]
    public GameObject currentPanel; // 끌(비활성화할) 기존 패널
    public GameObject nextPanel;    // 켤(활성화할) 새로운 패널

    // 버튼의 OnClick 이벤트에 연결할 함수
    public void SwitchPanel()
    {
        // 기존 패널 끄기
        if (currentPanel != null)
        {
            currentPanel.SetActive(false);
        }

        // 새 패널 켜기
        if (nextPanel != null)
        {
            nextPanel.SetActive(true);
        }
    }
}