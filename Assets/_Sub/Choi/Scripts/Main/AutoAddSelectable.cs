using UnityEngine;
using UnityEngine.UI;

public class AutoAddSelectable : MonoBehaviour
{
    // [ContextMenu]를 쓰면 인스펙터 창에서 우클릭으로 이 기능을 바로 실행할 수 있습니다.
    [ContextMenu("Apply Buttons To Slots")]
    public void ApplyButtons()
    {
        foreach (Transform child in transform)
        {
            if (child.name.StartsWith("DigitSlot"))
            {
                if (child.GetComponent<Button>() == null)
                {
                    Button btn = child.gameObject.AddComponent<Button>();

                    // 색상 변화 없음
                    btn.transition = Selectable.Transition.None;

                    // 중요: 포커스 튀는 것 방지를 위해 내비게이션도 None으로 설정
                    Navigation customNav = new Navigation();
                    customNav.mode = Navigation.Mode.None;
                    btn.navigation = customNav;
                }
            }
        }
        // Debug.Log("모든 슬롯에 버튼이 영구적으로 추가되었습니다!");
    }
}