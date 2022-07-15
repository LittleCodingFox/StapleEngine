#include "App.hpp"
#define GLFW_INCLUDE_NONE

#if BX_PLATFORM_LINUX || BX_PLATFORM_BSD
#	if ENTRY_CONFIG_USE_WAYLAND
#		include <wayland-egl.h>
#		define GLFW_EXPOSE_NATIVE_WAYLAND
#	else
#		define GLFW_EXPOSE_NATIVE_X11
#		define GLFW_EXPOSE_NATIVE_GLX
#	endif
#elif BX_PLATFORM_OSX
#	define GLFW_EXPOSE_NATIVE_COCOA
#	define GLFW_EXPOSE_NATIVE_NSGL
#elif BX_PLATFORM_WINDOWS
#	define GLFW_EXPOSE_NATIVE_WIN32
#	define GLFW_EXPOSE_NATIVE_WGL
#endif //

#include <GLFW/glfw3.h>
#include <GLFW/glfw3native.h>

namespace Staple
{
    PlayerSettings::PlayerSettings() : windowMode(WindowMode::Windowed), videoFlags(VideoFlags::Vsync),
        screenWidth(0), screenHeight(0), monitorIndex(0)
    {
    }

    int PlayerSettings::AALevel() const
    {
        if (HAS_FLAG(videoFlags, VideoFlags::MsaaX2))
        {
            return 2;
        }

        if (HAS_FLAG(videoFlags, VideoFlags::MsaaX4))
        {
            return 4;
        }

        if (HAS_FLAG(videoFlags, VideoFlags::MsaaX8))
        {
            return 8;
        }

        if (HAS_FLAG(videoFlags, VideoFlags::MsaaX16))
        {
            return 16;
        }

        return 1;
    }

    AppSettings::AppSettings() : runInBackground(false), appName("Staple")
    {
        renderers[AppPlatform::Windows] = RendererType::Direct3D11;
        renderers[AppPlatform::Linux] = RendererType::Vulkan;
        renderers[AppPlatform::Mac] = RendererType::Metal;
    }

    AppPlayer::AppPlayer(AppSettings appSettings) : appSettings(appSettings)
    {
        playerSettings.screenWidth = 1024;
        playerSettings.screenHeight = 768;
    }

    uint32_t AppPlayer::ScreenWidth() const
    {
        return playerSettings.screenWidth;
    }

    uint32_t AppPlayer::ScreenHeight() const
    {
        return playerSettings.screenHeight;
    }

    uint32_t ResetFlags(VideoFlags flags)
    {
        uint32_t outValue = BGFX_RESET_SRGB_BACKBUFFER;

        if (HAS_FLAG(flags, VideoFlags::Vsync))
        {
            outValue |= BGFX_RESET_VSYNC;
        }

        if (HAS_FLAG(flags, VideoFlags::MsaaX2))
        {
            outValue |= BGFX_RESET_MSAA_X2;
        }
        else if (HAS_FLAG(flags, VideoFlags::MsaaX4))
        {
            outValue |= BGFX_RESET_MSAA_X4;
        }
        else if (HAS_FLAG(flags, VideoFlags::MsaaX8))
        {
            outValue |= BGFX_RESET_MSAA_X8;
        }
        else if (HAS_FLAG(flags, VideoFlags::MsaaX16))
        {
            outValue |= BGFX_RESET_MSAA_X16;
        }

        if (HAS_FLAG(flags, VideoFlags::HDR10))
        {
            outValue |= BGFX_RESET_HDR10;
        }

        if (HAS_FLAG(flags, VideoFlags::HiDpi))
        {
            outValue |= BGFX_RESET_HIDPI;
        }

        return outValue;
    }

    void AppPlayer::ResetRendering(bool hasFocus)
    {
        uint32_t flags = ResetFlags(playerSettings.videoFlags);

        if (hasFocus == false && appSettings.runInBackground == false)
        {
            flags |= BGFX_RESET_SUSPEND;
        }

        bgfx::reset(ScreenWidth(), ScreenHeight(), flags, bgfx::TextureFormat::RGBA32U);
        bgfx::setViewRect(0, 0, 0, bgfx::BackbufferRatio::Equal);
    }

    void AppPlayer::Run()
    {
        glfwInit();

        glfwWindowHint(GLFW_CLIENT_API, GLFW_NO_API);
        glfwWindowHint(GLFW_RESIZABLE, GLFW_FALSE);

        GLFWwindow* window = NULL;

        int monitorCount = 0;
        
        GLFWmonitor** monitors = glfwGetMonitors(&monitorCount);

        GLFWmonitor* monitor = glfwGetPrimaryMonitor();

        if (playerSettings.monitorIndex >= 0 && playerSettings.monitorIndex < monitorCount)
        {
            monitor = monitors[playerSettings.monitorIndex];
        }

        switch (playerSettings.windowMode)
        {
        case WindowMode::Windowed:

            window = glfwCreateWindow(playerSettings.screenWidth, playerSettings.screenHeight, appSettings.appName.c_str(), NULL, NULL);

            break;

        case WindowMode::Fullscreen:

            window = glfwCreateWindow(playerSettings.screenWidth, playerSettings.screenHeight, appSettings.appName.c_str(), monitor, NULL);

            break;

        case WindowMode::Borderless:

            glfwWindowHint(GLFW_FLOATING, GLFW_TRUE);
            glfwWindowHint(GLFW_DECORATED, GLFW_FALSE);

            const GLFWvidmode* videoMode = glfwGetVideoMode(monitor);

            window = glfwCreateWindow(videoMode->width, videoMode->height, appSettings.appName.c_str(), NULL, NULL);

            break;
        }

        if (window == NULL)
        {
            glfwTerminate();

            return;
        }

        bgfx::renderFrame();

        bgfx::Init init;

        init.platformData.ndt = NULL;

        RendererType rendererType = RendererType::OpenGL;

#if BX_PLATFORM_WINDOWS

        init.platformData.nwh = glfwGetWin32Window(window);

        rendererType = appSettings.renderers[AppPlatform::Windows];

#elif BX_PLATFORM_LINUX

        init.platformData.ndt = glfwGetX11Display();
        init.platformData.nwh = glfwGetX11Window(window);

        if (init.platformData.nwh == NULL)
        {
            init.platformData.ndt = glfwGetWaylandDisplay();
            init.platformData.nwh = glfwGetWaylandWindow(window);
        }

        rendererType = appSettings.renderers[AppPlatform.Linux];

#elif BX_PLATFORM_OSX

        init.platformData.ndt = glfwGetCocoaMonitor(monitor);
        init.platformData.nwh = glfwGetCocoaWindow(window);

        rendererType = appSettings.renderers[AppPlatform.Mac];
#endif

        glfwGetFramebufferSize(window, &playerSettings.screenWidth, &playerSettings.screenHeight);

        bgfx::RendererType::Enum initRenderer = bgfx::RendererType::Count;

        switch (rendererType)
        {
        case RendererType::Direct3D11:

            initRenderer = bgfx::RendererType::Direct3D11;

            break;

        case RendererType::Direct3D12:

            initRenderer = bgfx::RendererType::Direct3D12;

            break;

        case RendererType::Metal:

            initRenderer = bgfx::RendererType::Metal;

            break;

        case RendererType::OpenGL:

            initRenderer = bgfx::RendererType::OpenGL;

            break;

        case RendererType::Vulkan:

            initRenderer = bgfx::RendererType::Vulkan;

            break;
        }

        init.type = initRenderer;
        init.resolution.width = playerSettings.screenWidth;
        init.resolution.height = playerSettings.screenHeight;
        init.resolution.reset = ResetFlags(playerSettings.videoFlags);

        if (bgfx::init(init) == false)
        {
            glfwDestroyWindow(window);

            glfwTerminate();

            return;
        }

        bgfx::setViewClear(0, BGFX_CLEAR_COLOR | BGFX_CLEAR_DEPTH, 0x334455FF);
        bgfx::setViewRect(0, 0, 0, bgfx::BackbufferRatio::Equal);

        bool hasFocus = glfwGetWindowAttrib(window, GLFW_FOCUSED) == GLFW_TRUE;

        if (appSettings.runInBackground == false && hasFocus == false)
        {
            ResetRendering(hasFocus);
        }

        while (!glfwWindowShouldClose(window))
        {
            glfwPollEvents();

            int currentW = playerSettings.screenWidth;
            int currentH = playerSettings.screenHeight;

            glfwGetFramebufferSize(window, &currentW, &currentH);

            if (currentW != playerSettings.screenWidth || currentH != playerSettings.screenHeight)
            {
                playerSettings.screenWidth = currentW;
                playerSettings.screenHeight = currentH;

                ResetRendering(hasFocus);
            }

            bool focused = glfwGetWindowAttrib(window, GLFW_FOCUSED) == GLFW_TRUE;

            if (appSettings.runInBackground == false && focused != hasFocus)
            {
                hasFocus = focused;

                ResetRendering(hasFocus);

                if (hasFocus == false)
                {
                    continue;
                }
            }

            bgfx::touch(0);

            bgfx::frame();
        }

        bgfx::shutdown();

        glfwTerminate();
    }
}

