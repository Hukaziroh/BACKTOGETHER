using UnityEngine;
using UnityEngine.InputSystem;

public class EmojiMenuController : MonoBehaviour
{
    public GameObject emojiMenuPanel; // Inspector에서 EmojiMenuPanel 연결

    void Update()
    {
        if (Keyboard.current != null && Keyboard.current.tabKey.wasPressedThisFrame)
        {
            // 탭 누를 때마다 활성화 상태 반전
            emojiMenuPanel.SetActive(!emojiMenuPanel.activeSelf);
        }
    }
}