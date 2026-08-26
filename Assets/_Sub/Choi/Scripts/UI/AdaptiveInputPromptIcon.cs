using System;
using UnityEngine;
using UnityEngine.InputSystem;
using UnityEngine.InputSystem.Utilities;
using UnityEngine.UI;

[RequireComponent(typeof(Image))]
public sealed class AdaptiveInputPromptIcon : MonoBehaviour
{
    [SerializeField] private Image targetImage;
    [SerializeField] private Sprite keyboardSprite;
    [SerializeField] private Sprite gamepadSprite;
    [SerializeField] private bool keyboardByDefault = true;

    private IDisposable buttonPressSubscription;
    private bool isShowingGamepad;
    private Color keyboardIconColor;

    private void Awake()
    {
        if (targetImage == null)
        {
            targetImage = GetComponent<Image>();
        }

        keyboardIconColor = targetImage.color;
    }

    private void OnEnable()
    {
        bool startWithGamepad = !keyboardByDefault && Gamepad.current != null;
        if (Keyboard.current == null && Gamepad.current != null)
        {
            startWithGamepad = true;
        }

        SetDeviceIcon(startWithGamepad);
        buttonPressSubscription = InputSystem.onAnyButtonPress.Call(OnButtonPressed);
    }

    private void OnDisable()
    {
        buttonPressSubscription?.Dispose();
        buttonPressSubscription = null;
    }

    private void OnButtonPressed(InputControl control)
    {
        if (control.device is Gamepad)
        {
            SetDeviceIcon(true);
        }
        else if (control.device is Keyboard || control.device is Mouse)
        {
            SetDeviceIcon(false);
        }
    }

    private void SetDeviceIcon(bool useGamepad)
    {
        if (targetImage == null || (targetImage.sprite != null && isShowingGamepad == useGamepad))
        {
            return;
        }

        isShowingGamepad = useGamepad;
        targetImage.sprite = useGamepad ? gamepadSprite : keyboardSprite;
        targetImage.color = useGamepad ? Color.white : keyboardIconColor;
        targetImage.enabled = targetImage.sprite != null;
        targetImage.preserveAspect = true;
    }
}
