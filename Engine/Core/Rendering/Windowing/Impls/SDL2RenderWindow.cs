﻿#if !ANDROID && !IOS
using SDL2;
using System;
using System.Collections.Generic;
using System.Runtime.InteropServices;
using System.Text;

namespace Staple.Internal;

internal class SDL2RenderWindow : IRenderWindow
{
    private const short AxisDeadzone = 8000;

    private class GamepadState
    {
        public nint instance;
    }

    public nint window;

    private Dictionary<int, GamepadState> gamepads = new();

    private bool movedWindow = false;
    private DateTime movedWindowTimer;
    private Vector2Int previousWindowPosition;
    private bool closedWindow = false;
    private bool windowFocused = true;
    private bool windowMaximized = false;

    public bool ContextLost { get; set; } = false;

    public bool IsFocused => windowFocused;

    public bool ShouldClose => closedWindow;

    public bool Unavailable => false;

    public bool Maximized => windowMaximized;

    public string Title
    {
        get => SDL.SDL_GetWindowTitle(window);

        set => SDL.SDL_SetWindowTitle(window, value);
    }

    public Vector2Int Position
    {
        get
        {
            SDL.SDL_GetWindowPosition(window, out var x, out var y);

            return new(x, y);
        }

        set
        {
            SDL.SDL_SetWindowPosition(window, value.X, value.Y);
        }
    }

    public int MonitorIndex
    {
        get
        {
            if(window == nint.Zero)
            {
                return 0;
            }

            return SDL.SDL_GetWindowDisplayIndex(window);
        }
    }

    public bool Create(ref int width, ref int height, string title, bool resizable, WindowMode windowMode, Vector2Int? position,
        bool maximized, int monitorIndex)
    {
        var monitor = monitorIndex >= 0 && monitorIndex < SDL.SDL_GetNumVideoDisplays() ? monitorIndex : 0;

        SDL.SDL_GetDisplayBounds(monitor, out var displayBounds);

        var windowFlags = SDL.SDL_WindowFlags.SDL_WINDOW_ALLOW_HIGHDPI | SDL.SDL_WindowFlags.SDL_WINDOW_SHOWN;

        if(resizable && windowMode == WindowMode.Windowed)
        {
            windowFlags |= SDL.SDL_WindowFlags.SDL_WINDOW_RESIZABLE;
        }

        if(maximized)
        {
            windowFlags |= SDL.SDL_WindowFlags.SDL_WINDOW_MAXIMIZED;
        }

        var windowPosition = new Vector2Int();

        switch(windowMode)
        {
            case WindowMode.Windowed:

                if(position.HasValue)
                {
                    windowPosition = position.Value;
                }

                break;

            case WindowMode.Fullscreen:

                windowFlags |= SDL.SDL_WindowFlags.SDL_WINDOW_FULLSCREEN;

                break;

            case WindowMode.Borderless:

                windowFlags |= SDL.SDL_WindowFlags.SDL_WINDOW_BORDERLESS;

                width = displayBounds.w;
                height = displayBounds.h;

                break;
        }

        window = SDL.SDL_CreateWindow(title, windowPosition.X + displayBounds.x, windowPosition.Y + displayBounds.y, width, height, windowFlags);

        if (window == nint.Zero)
        {
            return false;
        }

        if(maximized)
        {
            windowMaximized = true;
        }

        return true;
    }

    public void Destroy()
    {
        SDL.SDL_DestroyWindow(window);
    }

    public void GetWindowSize(out int width, out int height)
    {
        SDL.SDL_GetWindowSize(window, out width, out height);
    }

    private static KeyCode MapSDLKey(SDL.SDL_Keycode sym)
    {
        return sym switch
        {
            SDL.SDL_Keycode.SDLK_0 => KeyCode.Alpha0,
            SDL.SDL_Keycode.SDLK_1 => KeyCode.Alpha1,
            SDL.SDL_Keycode.SDLK_2 => KeyCode.Alpha2,
            SDL.SDL_Keycode.SDLK_3 => KeyCode.Alpha3,
            SDL.SDL_Keycode.SDLK_4 => KeyCode.Alpha4,
            SDL.SDL_Keycode.SDLK_5 => KeyCode.Alpha5,
            SDL.SDL_Keycode.SDLK_6 => KeyCode.Alpha6,
            SDL.SDL_Keycode.SDLK_7 => KeyCode.Alpha7,
            SDL.SDL_Keycode.SDLK_8 => KeyCode.Alpha8,
            SDL.SDL_Keycode.SDLK_9 => KeyCode.Alpha9,
            SDL.SDL_Keycode.SDLK_a => KeyCode.A,
            SDL.SDL_Keycode.SDLK_b => KeyCode.B,
            SDL.SDL_Keycode.SDLK_c => KeyCode.C,
            SDL.SDL_Keycode.SDLK_d => KeyCode.D,
            SDL.SDL_Keycode.SDLK_e => KeyCode.E,
            SDL.SDL_Keycode.SDLK_f => KeyCode.F,
            SDL.SDL_Keycode.SDLK_g => KeyCode.G,
            SDL.SDL_Keycode.SDLK_h => KeyCode.H,
            SDL.SDL_Keycode.SDLK_i => KeyCode.I,
            SDL.SDL_Keycode.SDLK_j => KeyCode.J,
            SDL.SDL_Keycode.SDLK_k => KeyCode.K,
            SDL.SDL_Keycode.SDLK_l => KeyCode.L,
            SDL.SDL_Keycode.SDLK_m => KeyCode.M,
            SDL.SDL_Keycode.SDLK_n => KeyCode.N,
            SDL.SDL_Keycode.SDLK_o => KeyCode.O,
            SDL.SDL_Keycode.SDLK_p => KeyCode.P,
            SDL.SDL_Keycode.SDLK_q => KeyCode.Q,
            SDL.SDL_Keycode.SDLK_r => KeyCode.R,
            SDL.SDL_Keycode.SDLK_s => KeyCode.S,
            SDL.SDL_Keycode.SDLK_t => KeyCode.T,
            SDL.SDL_Keycode.SDLK_u => KeyCode.U,
            SDL.SDL_Keycode.SDLK_v => KeyCode.V,
            SDL.SDL_Keycode.SDLK_w => KeyCode.W,
            SDL.SDL_Keycode.SDLK_x => KeyCode.X,
            SDL.SDL_Keycode.SDLK_y => KeyCode.Y,
            SDL.SDL_Keycode.SDLK_z => KeyCode.Z,
            SDL.SDL_Keycode.SDLK_BACKSLASH => KeyCode.Backslash,
            SDL.SDL_Keycode.SDLK_BACKSPACE => KeyCode.Backspace,
            SDL.SDL_Keycode.SDLK_CAPSLOCK => KeyCode.CapsLock,
            SDL.SDL_Keycode.SDLK_COMMA => KeyCode.Comma,
            SDL.SDL_Keycode.SDLK_DELETE => KeyCode.Delete,
            SDL.SDL_Keycode.SDLK_DOWN => KeyCode.Down,
            SDL.SDL_Keycode.SDLK_UP => KeyCode.Up,
            SDL.SDL_Keycode.SDLK_LEFT => KeyCode.Left,
            SDL.SDL_Keycode.SDLK_RIGHT => KeyCode.Right,
            SDL.SDL_Keycode.SDLK_END => KeyCode.End,
            SDL.SDL_Keycode.SDLK_EQUALS => KeyCode.Equal,
            SDL.SDL_Keycode.SDLK_ESCAPE => KeyCode.Escape,
            SDL.SDL_Keycode.SDLK_F1 => KeyCode.F1,
            SDL.SDL_Keycode.SDLK_F2 => KeyCode.F2,
            SDL.SDL_Keycode.SDLK_F3 => KeyCode.F3,
            SDL.SDL_Keycode.SDLK_F4 => KeyCode.F4,
            SDL.SDL_Keycode.SDLK_F5 => KeyCode.F5,
            SDL.SDL_Keycode.SDLK_F6 => KeyCode.F6,
            SDL.SDL_Keycode.SDLK_F7 => KeyCode.F7,
            SDL.SDL_Keycode.SDLK_F8 => KeyCode.F8,
            SDL.SDL_Keycode.SDLK_F9 => KeyCode.F9,
            SDL.SDL_Keycode.SDLK_F10 => KeyCode.F10,
            SDL.SDL_Keycode.SDLK_F11 => KeyCode.F11,
            SDL.SDL_Keycode.SDLK_F12 => KeyCode.F12,
            SDL.SDL_Keycode.SDLK_F13 => KeyCode.F13,
            SDL.SDL_Keycode.SDLK_F14 => KeyCode.F14,
            SDL.SDL_Keycode.SDLK_F15 => KeyCode.F15,
            SDL.SDL_Keycode.SDLK_F16 => KeyCode.F16,
            SDL.SDL_Keycode.SDLK_F17 => KeyCode.F17,
            SDL.SDL_Keycode.SDLK_F18 => KeyCode.F18,
            SDL.SDL_Keycode.SDLK_F19 => KeyCode.F19,
            SDL.SDL_Keycode.SDLK_F20 => KeyCode.F20,
            SDL.SDL_Keycode.SDLK_F21 => KeyCode.F21,
            SDL.SDL_Keycode.SDLK_F22 => KeyCode.F22,
            SDL.SDL_Keycode.SDLK_F23 => KeyCode.F23,
            SDL.SDL_Keycode.SDLK_F24 => KeyCode.F24,
            SDL.SDL_Keycode.SDLK_HOME => KeyCode.Home,
            SDL.SDL_Keycode.SDLK_INSERT => KeyCode.Insert,
            SDL.SDL_Keycode.SDLK_KP_0 => KeyCode.Numpad0,
            SDL.SDL_Keycode.SDLK_KP_1 => KeyCode.Numpad1,
            SDL.SDL_Keycode.SDLK_KP_2 => KeyCode.Numpad2,
            SDL.SDL_Keycode.SDLK_KP_3 => KeyCode.Numpad3,
            SDL.SDL_Keycode.SDLK_KP_4 => KeyCode.Numpad4,
            SDL.SDL_Keycode.SDLK_KP_5 => KeyCode.Numpad5,
            SDL.SDL_Keycode.SDLK_KP_6 => KeyCode.Numpad6,
            SDL.SDL_Keycode.SDLK_KP_7 => KeyCode.Numpad7,
            SDL.SDL_Keycode.SDLK_KP_8 => KeyCode.Numpad8,
            SDL.SDL_Keycode.SDLK_KP_9 => KeyCode.Numpad9,
            SDL.SDL_Keycode.SDLK_LALT => KeyCode.LeftAlt,
            SDL.SDL_Keycode.SDLK_LCTRL => KeyCode.LeftControl,
            SDL.SDL_Keycode.SDLK_LEFTBRACKET => KeyCode.LeftBracket,
            SDL.SDL_Keycode.SDLK_LSHIFT => KeyCode.LeftShift,
            SDL.SDL_Keycode.SDLK_RALT => KeyCode.RightAlt,
            SDL.SDL_Keycode.SDLK_RCTRL => KeyCode.RightControl,
            SDL.SDL_Keycode.SDLK_RIGHTBRACKET => KeyCode.RightBracket,
            SDL.SDL_Keycode.SDLK_RSHIFT => KeyCode.RightShift,
            SDL.SDL_Keycode.SDLK_MINUS => KeyCode.Minus,
            SDL.SDL_Keycode.SDLK_PAGEDOWN => KeyCode.PageDown,
            SDL.SDL_Keycode.SDLK_PAGEUP => KeyCode.PageUp,
            SDL.SDL_Keycode.SDLK_PERIOD => KeyCode.Period,
            SDL.SDL_Keycode.SDLK_PRINTSCREEN => KeyCode.PrintScreen,
            SDL.SDL_Keycode.SDLK_RETURN => KeyCode.Enter,
            SDL.SDL_Keycode.SDLK_RETURN2 => KeyCode.Enter,
            SDL.SDL_Keycode.SDLK_SEMICOLON => KeyCode.SemiColon,
            SDL.SDL_Keycode.SDLK_SLASH => KeyCode.Slash,
            SDL.SDL_Keycode.SDLK_SPACE => KeyCode.Space,
            SDL.SDL_Keycode.SDLK_TAB => KeyCode.Tab,
            _ => KeyCode.Unknown,
        };
    }

    private ModifierKeys GetModifiers(SDL.SDL_Keymod mod)
    {
        ModifierKeys modifiers = 0;

        if (mod.HasFlag(SDL.SDL_Keymod.KMOD_CAPS))
        {
            modifiers |= ModifierKeys.CapsLock;
        }

        if(mod.HasFlag(SDL.SDL_Keymod.KMOD_ALT))
        {
            modifiers |= ModifierKeys.Alt;
        }

        if (mod.HasFlag(SDL.SDL_Keymod.KMOD_CTRL))
        {
            modifiers |= ModifierKeys.Control;
        }

        if (mod.HasFlag(SDL.SDL_Keymod.KMOD_SHIFT))
        {
            modifiers |= ModifierKeys.Shift;
        }

        if (mod.HasFlag(SDL.SDL_Keymod.KMOD_NUM))
        {
            modifiers |= ModifierKeys.NumLock;
        }

        return modifiers;
    }

    private bool TryFindGamepad(int which, out int key, out GamepadState state)
    {
        foreach (var pair in gamepads)
        {
            var joystsickInstance = SDL.SDL_JoystickInstanceID(SDL.SDL_GameControllerGetJoystick(pair.Value.instance));

            if (joystsickInstance == which)
            {
                key = pair.Key;
                state = pair.Value;

                return true;
            }
        }

        key = default;
        state = default;

        return false;
    }

    public void PollEvents()
    {
        while(SDL.SDL_PollEvent(out var _event) != 0)
        {
            switch(_event.type)
            {
                case SDL.SDL_EventType.SDL_WINDOWEVENT:

                    switch(_event.window.windowEvent)
                    {
                        case SDL.SDL_WindowEventID.SDL_WINDOWEVENT_FOCUS_GAINED:

                            windowFocused = true;

                            break;

                        case SDL.SDL_WindowEventID.SDL_WINDOWEVENT_FOCUS_LOST:

                            windowFocused = false;

                            break;

                        case SDL.SDL_WindowEventID.SDL_WINDOWEVENT_MAXIMIZED:

                            windowMaximized = true;

                            AppEventQueue.instance.Add(AppEvent.Maximize(windowMaximized));

                            break;

                        case SDL.SDL_WindowEventID.SDL_WINDOWEVENT_RESTORED:

                            windowMaximized = false;

                            AppEventQueue.instance.Add(AppEvent.Maximize(windowMaximized));

                            break;

                        case SDL.SDL_WindowEventID.SDL_WINDOWEVENT_MOVED:

                            SDL.SDL_GetWindowPosition(window, out var winX, out var winY);

                            AppEventQueue.instance.Add(AppEvent.MoveWindow(new Vector2Int(winX, winY)));

                            break;
                    }

                    break;

                case SDL.SDL_EventType.SDL_KEYDOWN:
                case SDL.SDL_EventType.SDL_KEYUP:

                    AppEventQueue.instance.Add(AppEvent.Key(MapSDLKey(_event.key.keysym.sym), (int)_event.key.keysym.scancode,
                        _event.key.state switch
                        {
                            SDL.SDL_PRESSED => InputState.Press,
                            _ => InputState.Release,
                        }, GetModifiers(_event.key.keysym.mod)));

                    break;

                case SDL.SDL_EventType.SDL_MOUSEBUTTONDOWN:
                case SDL.SDL_EventType.SDL_MOUSEBUTTONUP:

                    AppEventQueue.instance.Add(AppEvent.Mouse((uint)_event.button.button switch
                        {
                            SDL.SDL_BUTTON_LEFT => MouseButton.Left,
                            SDL.SDL_BUTTON_MIDDLE => MouseButton.Middle,
                            SDL.SDL_BUTTON_RIGHT => MouseButton.Right,
                            SDL.SDL_BUTTON_X1 => MouseButton.Button1,
                            SDL.SDL_BUTTON_X2 => MouseButton.Button2,
                            _ => 0,
                        },
                        _event.button.state switch
                        {
                            SDL.SDL_PRESSED => InputState.Press,
                            _ => InputState.Release,
                        }, GetModifiers(SDL.SDL_GetModState())));

                    break;

                case SDL.SDL_EventType.SDL_MOUSEMOTION:

                    Input.CursorPosCallback(_event.motion.x, _event.motion.y);

                    break;

                case SDL.SDL_EventType.SDL_MOUSEWHEEL:

                    Input.MouseScrollCallback(_event.wheel.preciseX, _event.wheel.preciseY);

                    break;

                case SDL.SDL_EventType.SDL_CONTROLLERDEVICEADDED:

                    {
                        var instance = SDL.SDL_GameControllerOpen(_event.cdevice.which);

                        gamepads.Add(_event.cdevice.which, new()
                        {
                            instance = instance,
                        });

                        Input.GamepadConnect(AppEvent.GamepadConnect(_event.cdevice.which, GamepadConnectionState.Connected));
                    }

                    break;

                case SDL.SDL_EventType.SDL_CONTROLLERDEVICEREMOVED:

                    {
                        if(TryFindGamepad(_event.cdevice.which, out var key, out var state))
                        {
                            SDL.SDL_GameControllerClose(state.instance);

                            gamepads.Remove(key);

                            Input.GamepadConnect(AppEvent.GamepadConnect(key, GamepadConnectionState.Disconnected));
                        }
                    }

                    break;

                case SDL.SDL_EventType.SDL_CONTROLLERBUTTONDOWN:
                case SDL.SDL_EventType.SDL_CONTROLLERBUTTONUP:

                    {
                        if (TryFindGamepad(_event.cdevice.which, out var key, out _))
                        {
                            Input.GamepadButton(AppEvent.GamepadButton(key, (SDL.SDL_GameControllerButton)_event.cbutton.button switch
                            {
                                SDL.SDL_GameControllerButton.SDL_CONTROLLER_BUTTON_A => GamepadButton.A,
                                SDL.SDL_GameControllerButton.SDL_CONTROLLER_BUTTON_B => GamepadButton.B,
                                SDL.SDL_GameControllerButton.SDL_CONTROLLER_BUTTON_X => GamepadButton.X,
                                SDL.SDL_GameControllerButton.SDL_CONTROLLER_BUTTON_Y => GamepadButton.Y,
                                SDL.SDL_GameControllerButton.SDL_CONTROLLER_BUTTON_BACK => GamepadButton.Back,
                                SDL.SDL_GameControllerButton.SDL_CONTROLLER_BUTTON_GUIDE => GamepadButton.Guide,
                                SDL.SDL_GameControllerButton.SDL_CONTROLLER_BUTTON_START => GamepadButton.Start,
                                SDL.SDL_GameControllerButton.SDL_CONTROLLER_BUTTON_LEFTSTICK => GamepadButton.LeftStick,
                                SDL.SDL_GameControllerButton.SDL_CONTROLLER_BUTTON_RIGHTSTICK => GamepadButton.RightStick,
                                SDL.SDL_GameControllerButton.SDL_CONTROLLER_BUTTON_LEFTSHOULDER => GamepadButton.LeftShoulder,
                                SDL.SDL_GameControllerButton.SDL_CONTROLLER_BUTTON_RIGHTSHOULDER => GamepadButton.RightShoulder,
                                SDL.SDL_GameControllerButton.SDL_CONTROLLER_BUTTON_DPAD_UP => GamepadButton.DPadUp,
                                SDL.SDL_GameControllerButton.SDL_CONTROLLER_BUTTON_DPAD_DOWN => GamepadButton.DPadDown,
                                SDL.SDL_GameControllerButton.SDL_CONTROLLER_BUTTON_DPAD_LEFT => GamepadButton.DPadLeft,
                                SDL.SDL_GameControllerButton.SDL_CONTROLLER_BUTTON_DPAD_RIGHT => GamepadButton.DPadRight,
                                SDL.SDL_GameControllerButton.SDL_CONTROLLER_BUTTON_MISC1 => GamepadButton.Misc1,
                                SDL.SDL_GameControllerButton.SDL_CONTROLLER_BUTTON_PADDLE1 => GamepadButton.Paddle1,
                                SDL.SDL_GameControllerButton.SDL_CONTROLLER_BUTTON_PADDLE2 => GamepadButton.Paddle2,
                                SDL.SDL_GameControllerButton.SDL_CONTROLLER_BUTTON_PADDLE3 => GamepadButton.Paddle3,
                                SDL.SDL_GameControllerButton.SDL_CONTROLLER_BUTTON_PADDLE4 => GamepadButton.Paddle4,
                                SDL.SDL_GameControllerButton.SDL_CONTROLLER_BUTTON_TOUCHPAD => GamepadButton.TouchPad,
                                _ => GamepadButton.Invalid,
                            }, _event.cbutton.state switch
                            {
                                SDL.SDL_PRESSED => InputState.Press,
                                _ => InputState.Release,
                            }));
                        }
                    }

                    break;

                case SDL.SDL_EventType.SDL_CONTROLLERAXISMOTION:

                    {
                        if (TryFindGamepad(_event.cdevice.which, out var key, out _))
                        {
                            var value = _event.caxis.axisValue;

                            if (Math.Abs(value) <= AxisDeadzone)
                            {
                                value = 0;
                            }

                            var floatValue = value / (float)short.MaxValue;

                            var axis = (SDL.SDL_GameControllerAxis)_event.caxis.axis switch
                            {
                                SDL.SDL_GameControllerAxis.SDL_CONTROLLER_AXIS_LEFTX => GamepadAxis.LeftX,
                                SDL.SDL_GameControllerAxis.SDL_CONTROLLER_AXIS_LEFTY => GamepadAxis.LeftY,
                                SDL.SDL_GameControllerAxis.SDL_CONTROLLER_AXIS_RIGHTX => GamepadAxis.RightX,
                                SDL.SDL_GameControllerAxis.SDL_CONTROLLER_AXIS_RIGHTY => GamepadAxis.RightY,
                                SDL.SDL_GameControllerAxis.SDL_CONTROLLER_AXIS_TRIGGERLEFT => GamepadAxis.TriggerLeft,
                                SDL.SDL_GameControllerAxis.SDL_CONTROLLER_AXIS_TRIGGERRIGHT => GamepadAxis.TriggerRight,
                                _ => GamepadAxis.Invalid,
                            };

                            if(axis == GamepadAxis.LeftY || axis == GamepadAxis.RightY)
                            {
                                floatValue *= -1;
                            }

                            Input.GamepadMovement(AppEvent.GamepadMovement(key, axis, floatValue));
                        }
                    }

                    break;

                case SDL.SDL_EventType.SDL_QUIT:

                    closedWindow = true;

                    break;

                case SDL.SDL_EventType.SDL_TEXTINPUT:

                    unsafe
                    {
                        byte[] buffer = new byte[SDL.SDL_TEXTINPUTEVENT_TEXT_SIZE];

                        Marshal.Copy((nint)_event.text.text, buffer, 0, buffer.Length);

                        var text = Encoding.UTF8.GetString(buffer);

                        Input.HandleTextEvent(AppEvent.Text(text.Length > 0 ? (uint)text[0] : 0));
                    }

                    break;
            }
        }

        var windowPosition = new Vector2Int();

        SDL.SDL_GetWindowPosition(window, out windowPosition.X, out windowPosition.Y);

        if (previousWindowPosition != windowPosition)
        {
            previousWindowPosition = windowPosition;

            movedWindow = true;
            movedWindowTimer = DateTime.Now;
        }

        if (movedWindow && (DateTime.Now - movedWindowTimer).TotalSeconds >= 1.0f)
        {
            movedWindow = false;

            AppEventQueue.instance.Add(AppEvent.MoveWindow(windowPosition));
        }
    }

    public void Init()
    {
        SDL.SDL_Init(SDL.SDL_INIT_TIMER | SDL.SDL_INIT_VIDEO | SDL.SDL_INIT_HAPTIC | SDL.SDL_INIT_GAMECONTROLLER);
    }

    public void Terminate()
    {
        foreach(var pair in gamepads)
        {
            if(pair.Value.instance != nint.Zero)
            {
                SDL.SDL_GameControllerClose(pair.Value.instance);

                pair.Value.instance = nint.Zero;
            }
        }

        if(window != nint.Zero)
        {
            SDL.SDL_DestroyWindow(window);
        }

        SDL.SDL_Quit();
    }

    public nint WindowPointer(AppPlatform platform)
    {
        var info = new SDL.SDL_SysWMinfo();

        SDL.SDL_GetVersion(out info.version);

        if (SDL.SDL_GetWindowWMInfo(window, ref info) == SDL.SDL_bool.SDL_FALSE)
        {
            return nint.Zero;
        }

        switch (platform)
        {
            case AppPlatform.Windows:

                return info.info.win.window;

            case AppPlatform.Linux:

                if (info.info.wl.surface != nint.Zero)
                {
                    return info.info.wl.surface;
                }

                return info.info.x11.window;

            case AppPlatform.MacOSX:

                return info.info.cocoa.window;

            default:

                return nint.Zero;
        }
    }

    public nint MonitorPointer(AppPlatform platform)
    {
        var info = new SDL.SDL_SysWMinfo();

        SDL.SDL_GetVersion(out info.version);

        if (SDL.SDL_GetWindowWMInfo(window, ref info) == SDL.SDL_bool.SDL_FALSE)
        {
            return nint.Zero;
        }

        switch (platform)
        {
            case AppPlatform.Windows:

                return nint.Zero;

            case AppPlatform.Linux:

                if(info.info.wl.display != nint.Zero)
                {
                    return info.info.wl.display;
                }

                return info.info.x11.display;

            case AppPlatform.MacOSX:

                return nint.Zero;

            default:

                return nint.Zero;
        }
    }

    public void LockCursor()
    {
        SDL.SDL_SetWindowMouseGrab(window, SDL.SDL_bool.SDL_TRUE);
    }

    public void UnlockCursor()
    {
        SDL.SDL_SetWindowMouseGrab(window, SDL.SDL_bool.SDL_FALSE);
    }

    public void HideCursor()
    {
        SDL.SDL_ShowCursor(SDL.SDL_DISABLE);
    }

    public void ShowCursor()
    {
        SDL.SDL_ShowCursor(SDL.SDL_ENABLE);
    }

    public void SetIcon(RawTextureData icon)
    {
        if (Platform.IsMacOS)
        {
            return;
        }

        var pinnedArray = GCHandle.Alloc(icon.data, GCHandleType.Pinned);

        unsafe
        {
            var ptr = pinnedArray.AddrOfPinnedObject();

            var surface = SDL.SDL_CreateRGBSurfaceFrom(ptr, icon.width, icon.height, 32, icon.width * 4, 0x000000FF, 0x0000FF00, 0x00FF0000, 0xFF000000);

            SDL.SDL_SetWindowIcon(window, surface);

            SDL.SDL_FreeSurface(surface);
        }

        pinnedArray.Free();
    }
}
#endif