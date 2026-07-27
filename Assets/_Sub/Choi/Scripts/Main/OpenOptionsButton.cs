using UnityEngine;
using UnityEngine.UI;

[RequireComponent(typeof(Button))]
public class OpenOptionsButton : MonoBehaviour
{
    private Button targetButton;

    private void Awake()
    {
        targetButton = GetComponent<Button>();
    }

    private void Start()
    {
        // 버튼이 생성될 때(씬이 로드될 때마다) UIManager의 RequestOptions 함수를 안전하게 자동 연결합니다.
        targetButton.onClick.AddListener(OnOptionsButtonClicked);
    }

    private void OnOptionsButtonClicked()
    {
        if (UIManager.Instance != null)
        {
            UIManager.Instance.RequestOptions();
        }
        else
        {
            Debug.LogWarning("UIManager 인스턴스를 찾을 수 없습니다!");
        }
    }
}