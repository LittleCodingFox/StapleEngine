using Bgfx;
using GLFW;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Runtime.InteropServices;
using System.Text;
using System.Threading.Tasks;

namespace Staple
{
    public class AppPlayer
    {
        public readonly AppSettings appSettings;

        private const ushort ClearView = 0;

        private PlayerSettings playerSettings;

        public static int ScreenWidth { get; private set; }
        public static int ScreenHeight { get; private set; }

        public AppPlayer(AppSettings appSettings)
        {
            this.appSettings = appSettings;
        }

        private bgfx.ResetFlags ResetFlags
        {
            get
            {
                var resetFlags = bgfx.ResetFlags.SrgbBackbuffer;

                if (playerSettings.videoFlags.HasFlag(PlayerSettings.VideoFlags.Vsync))
                {
                    resetFlags |= bgfx.ResetFlags.Vsync;
                }

                if (playerSettings.videoFlags.HasFlag(PlayerSettings.VideoFlags.MsaaX2))
                {
                    resetFlags |= bgfx.ResetFlags.MsaaX2;
                }

                if (playerSettings.videoFlags.HasFlag(PlayerSettings.VideoFlags.MsaaX4))
                {
                    resetFlags |= bgfx.ResetFlags.MsaaX4;
                }

                if (playerSettings.videoFlags.HasFlag(PlayerSettings.VideoFlags.MsaaX8))
                {
                    resetFlags |= bgfx.ResetFlags.MsaaX8;
                }

                if (playerSettings.videoFlags.HasFlag(PlayerSettings.VideoFlags.MsaaX16))
                {
                    resetFlags |= bgfx.ResetFlags.MsaaX16;
                }

                if (playerSettings.videoFlags.HasFlag(PlayerSettings.VideoFlags.HDR10))
                {
                    resetFlags |= bgfx.ResetFlags.Hdr10;
                }

                if (playerSettings.videoFlags.HasFlag(PlayerSettings.VideoFlags.HiDpi))
                {
                    resetFlags |= bgfx.ResetFlags.Hidpi;
                }

                return resetFlags;
            }
        }

        public void ResetRendering(bool hasFocus)
        {
            var flags = ResetFlags;

            if(hasFocus == false && appSettings.runInBackground == false)
            {
                flags |= bgfx.ResetFlags.Suspend;
            }

            bgfx.reset((uint)ScreenWidth, (uint)ScreenHeight, (uint)flags, bgfx.TextureFormat.RGBA32U);
            bgfx.set_view_rect_ratio(ClearView, 0, 0, bgfx.BackbufferRatio.Equal);
        }

        public void Run()
        {
            playerSettings = new PlayerSettings()
            {
                screenWidth = 1024,
                screenHeight = 768,
            };

            Glfw.Init();

            Glfw.WindowHint(Hint.ClientApi, ClientApi.None);
            Glfw.WindowHint(Hint.Resizable, false);

            NativeWindow window = null;

            var monitor = Glfw.Monitors.Skip(playerSettings.monitorIndex).FirstOrDefault();

            if (monitor == null)
            {
                monitor = Glfw.PrimaryMonitor;
            }

            switch (playerSettings.windowMode)
            {
                case PlayerSettings.WindowMode.Windowed:

                    window = new NativeWindow(playerSettings.screenWidth, playerSettings.screenHeight, appSettings.appName);

                    break;

                case PlayerSettings.WindowMode.Fullscreen:

                    window = new NativeWindow(playerSettings.screenWidth, playerSettings.screenHeight, appSettings.appName, monitor, Window.None);

                    break;

                case PlayerSettings.WindowMode.Borderless:

                    Glfw.WindowHint(Hint.Floating, true);
                    Glfw.WindowHint(Hint.Decorated, false);

                    var videoMode = Glfw.GetVideoMode(monitor);

                    window = new NativeWindow(videoMode.Width, videoMode.Height, appSettings.appName);

                    break;
            }

            if (window == null)
            {
                return;
            }

            bgfx.render_frame(0);

            var init = new bgfx.Init();
            var rendererType = RendererType.OpenGL;

            unsafe
            {
                init.platformData.ndt = null;

                if (RuntimeInformation.IsOSPlatform(OSPlatform.Windows))
                {
                    init.platformData.nwh = Native.GetWin32Window(window).ToPointer();

                    if(appSettings.renderers.TryGetValue(AppPlatform.Windows, out var type))
                    {
                        rendererType = type;
                    }
                }
                else if (RuntimeInformation.IsOSPlatform(OSPlatform.Linux))
                {
                    var display = Native.GetX11Display();
                    var windowHandle = Native.GetX11Window(window);

                    if (display == IntPtr.Zero || windowHandle == IntPtr.Zero)
                    {
                        display = Native.GetWaylandDisplay();
                        windowHandle = Native.GetWaylandWindow(window);
                    }

                    init.platformData.ndt = display.ToPointer();
                    init.platformData.nwh = windowHandle.ToPointer();

                    if (appSettings.renderers.TryGetValue(AppPlatform.Linux, out var type))
                    {
                        rendererType = type;
                    }
                }
                else if (RuntimeInformation.IsOSPlatform(OSPlatform.OSX))
                {
                    init.platformData.ndt = (void*)Native.GetCocoaMonitor(window.Monitor);
                    init.platformData.nwh = Native.GetCocoaWindow(window).ToPointer();

                    if (appSettings.renderers.TryGetValue(AppPlatform.MacOSX, out var type))
                    {
                        rendererType = type;
                    }
                }
            }

            Glfw.GetFramebufferSize(window, out var sw, out var sh);

            playerSettings.screenWidth = ScreenWidth = sw;
            playerSettings.screenHeight = ScreenHeight = sh;

            var initRenderer = bgfx.RendererType.Count;

            switch(rendererType)
            {
                case RendererType.OpenGL:

                    initRenderer = bgfx.RendererType.OpenGL;

                    break;

                case RendererType.Direct3D11:

                    initRenderer = bgfx.RendererType.Direct3D11;

                    break;

                case RendererType.Direct3D12:

                    initRenderer = bgfx.RendererType.Direct3D12;

                    break;

                case RendererType.Metal:

                    initRenderer = bgfx.RendererType.Metal;

                    break;

                case RendererType.Vulkan:

                    initRenderer = bgfx.RendererType.Vulkan;

                    break;
            }

            init.type = initRenderer;
            init.resolution.width = (uint)ScreenWidth;
            init.resolution.height = (uint)ScreenHeight;
            init.resolution.reset = (uint)ResetFlags;

            unsafe
            {
                if (!bgfx.init(&init))
                {
                    return;
                }
            }

            ushort kClearView = 0;

            bgfx.set_view_clear(kClearView, (ushort)(bgfx.ClearFlags.Color | bgfx.ClearFlags.Depth), 0x334455FF, 24, 0);
            bgfx.set_view_rect_ratio(kClearView, 0, 0, bgfx.BackbufferRatio.Equal);

            bool hasFocus = window.IsFocused;

            if(appSettings.runInBackground == false && hasFocus == false)
            {
                ResetRendering(hasFocus);
            }

            SceneManager.instance.CreateScene();

            while (!Glfw.WindowShouldClose(window) && window.IsClosed == false)
            {
                Glfw.PollEvents();

                Glfw.GetFramebufferSize(window, out var currentW, out var currentH);

                if (currentW != ScreenWidth || currentH != ScreenHeight)
                {
                    playerSettings.screenWidth = ScreenWidth = currentW;
                    playerSettings.screenHeight = ScreenHeight = currentH;

                    ResetRendering(hasFocus);
                }

                if(appSettings.runInBackground == false && window.IsFocused != hasFocus)
                {
                    hasFocus = window.IsFocused;

                    ResetRendering(hasFocus);

                    if(hasFocus == false)
                    {
                        continue;
                    }
                }

                bgfx.touch(0);

                bgfx.frame(false);
            }

            bgfx.shutdown();

            Glfw.Terminate();
        }

    }
}
