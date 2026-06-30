using UnityEngine;
using UnityEngine.InputSystem;

public class EmojiUIManager : MonoBehaviour
{
    public GameObject emojiMenuPanel; // 인스펙터에서 EmojiMenuPanel 연결

    void Update()
    {
        // 탭 키 입력을 감지 (New Input System 방식)
        if (Keyboard.current != null && Keyboard.current.tabKey.wasPressedThisFrame)
        {
            ToggleEmojiMenu();
        }
    }

    void ToggleEmojiMenu()
    {
        emojiMenuPanel.SetActive(!emojiMenuPanel.activeSelf);
    }
}