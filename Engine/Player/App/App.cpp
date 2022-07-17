#include "App.hpp"
#include "Rendering/BGFXRenderer/BGFXRenderer.hpp"
#define GLFW_INCLUDE_NONE

#if STAPLE_PLATFORM_LINUX
#	if ENTRY_CONFIG_USE_WAYLAND
#		include <wayland-egl.h>
#		define GLFW_EXPOSE_NATIVE_WAYLAND
#	else
#		define GLFW_EXPOSE_NATIVE_X11
#		define GLFW_EXPOSE_NATIVE_GLX
#	endif
#elif STAPLE_PLATFORM_OSX
#	define GLFW_EXPOSE_NATIVE_COCOA
#	define GLFW_EXPOSE_NATIVE_NSGL
#elif STAPLE_PLATFORM_WINDOWS
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

        renderer.reset(new BGFXRenderer());
    }

    uint32_t AppPlayer::ScreenWidth() const
    {
        return playerSettings.screenWidth;
    }

    uint32_t AppPlayer::ScreenHeight() const
    {
        return playerSettings.screenHeight;
    }

    void AppPlayer::ResetRendering(bool hasFocus)
    {
        renderer->ResetRendering(playerSettings.videoFlags, hasFocus == false && appSettings.runInBackground == false);
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

        uint64_t monitorHandle = 0;
        uint64_t windowHandle = 0;

        RendererType rendererType = RendererType::OpenGL;

#if STAPLE_PLATFORM_WINDOWS

        windowHandle = (uint64_t)glfwGetWin32Window(window);

        rendererType = appSettings.renderers[AppPlatform::Windows];

#elif STAPLE_PLATFORM_LINUX

        monitorHandle = (uint64_t)glfwGetX11Display();
        windowHandle = (uint64_t)glfwGetX11Window(window);

        if (windowHandle == 0)
        {
            monitorHandle = (uint64_t)glfwGetWaylandDisplay();
            windowHandle = (uint64_t)glfwGetWaylandWindow(window);
        }

        rendererType = appSettings.renderers[AppPlatform.Linux];

#elif STAPLE_PLATFORM_OSX

        monitorHandle = (uint64_t)glfwGetCocoaMonitor(monitor);
        windowHandle = (uint64_t)glfwGetCocoaWindow(window);

        rendererType = appSettings.renderers[AppPlatform.Mac];
#endif

        glfwGetFramebufferSize(window, &playerSettings.screenWidth, &playerSettings.screenHeight);

        if (!renderer->Create(monitorHandle, windowHandle, rendererType, playerSettings.videoFlags,
            playerSettings.screenWidth, playerSettings.screenHeight))
        {
            renderer->Dispose();
            renderer.reset();

            glfwDestroyWindow(window);

            glfwTerminate();

            return;
        }

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

                renderer->SetWindowSize(currentW, currentH);
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

            renderer->FinishFrame();
        }

        renderer->Dispose();
        renderer.reset();

        glfwTerminate();
    }
}

