using UnityEngine;
using UnityEngine.InputSystem;
using System;

public class InputDeviceDetector : MonoBehaviour {
    public static event Action<GlyphMappingDatabase> OnInputDeviceChanged;

    private InputDevice currentInputDevice;

    [SerializeField] private GlyphMappingDatabase keyboard_GlyphMappingDatabase;
    [SerializeField] private GlyphMappingDatabase gamepad_GlyphMappingDatabase;
    [SerializeField] private float mouseMoveThreshold = 5f;

    private Vector2 lastMousePosition;

    private void OnEnable() {
        InputSystem.onDeviceChange += OnSystemDeviceChange;
    }

    private void OnDisable() {
        InputSystem.onDeviceChange -= OnSystemDeviceChange;
    }

    private void Start() {
        if (Mouse.current != null) {
            lastMousePosition = Mouse.current.position.ReadValue();
        }
        InitializeCurrentDevice();
    }

    private void Update() {
        CheckChange();
    }

    private void InitializeCurrentDevice() {
        if (Gamepad.current != null) {
            SetCurrentDevice(Gamepad.current, true);
        } else {
            SetCurrentDevice(Keyboard.current, true);
        }
    }

    private void OnSystemDeviceChange(InputDevice device, InputDeviceChange change) {
        if (device is not Gamepad) {
            return;
        }

        if (change == InputDeviceChange.Added ||
            change == InputDeviceChange.Reconnected ||
            change == InputDeviceChange.Enabled) {
            SetCurrentDevice(device, true);
            return;
        }

        if ((change == InputDeviceChange.Removed || change == InputDeviceChange.Disconnected) && currentInputDevice == device) {
            SetCurrentDevice(Keyboard.current, true);
        }
    }

    private void CheckChange() {
        // Switch to gamepad on any gamepad input
        if (Gamepad.current != null && Gamepad.current.wasUpdatedThisFrame && currentInputDevice != Gamepad.current) {
            SetCurrentDevice(Gamepad.current, false);
            return;
        }

        // Switch to keyboard/mouse on keyboard or significant mouse movement
        if (currentInputDevice is not Keyboard) {
            bool hasKeyboardInput = Keyboard.current != null && Keyboard.current.wasUpdatedThisFrame;
            if (hasKeyboardInput) {
                SetCurrentDevice(Keyboard.current, false);
                return;
            }

            if (Mouse.current != null) {
                Vector2 currentMousePos = Mouse.current.position.ReadValue();
                float mouseDelta = Vector2.Distance(currentMousePos, lastMousePosition);
                
                if (mouseDelta > mouseMoveThreshold) {
                    SetCurrentDevice(Keyboard.current, false);
                    lastMousePosition = currentMousePos;
                    return;
                }
            }
        }
    }

    private void SetCurrentDevice(InputDevice newDevice, bool forceNotify) {
        if (newDevice == null) {
            return;
        }

        if (!forceNotify && currentInputDevice == newDevice) {
            return;
        }

        currentInputDevice = newDevice;
        Debug.Log($"Input device changed to: {newDevice.displayName} ({newDevice.layout})");

        if (newDevice is Gamepad) {
            NotifyInputDeviceChanged(gamepad_GlyphMappingDatabase);
        } else {
            NotifyInputDeviceChanged(keyboard_GlyphMappingDatabase);
        }
    }

    private void NotifyInputDeviceChanged(GlyphMappingDatabase newGlyphMappingDatabase) {
        OnInputDeviceChanged?.Invoke(newGlyphMappingDatabase);
    }
}