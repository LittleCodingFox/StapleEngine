#pragma once

#include <stdint.h>
#include <string>
#include <map>
#include "Platform/Platform.hpp"

namespace Staple
{
	enum class WindowMode : uint32_t
	{
		Windowed,
		Fullscreen,
		Borderless
	};

    enum class VideoFlags : uint32_t
    {
        None = 0,
        Vsync = (1 << 1),
        MsaaX2 = (1 << 2),
        MsaaX4 = (1 << 3),
        MsaaX8 = (1 << 4),
        MsaaX16 = (1 << 5),
        HDR10 = (1 << 6),
        HiDpi = (1 << 7),
    };

	struct PlayerSettings
	{
        WindowMode windowMode;
        VideoFlags videoFlags;
        int screenWidth;
        int screenHeight;
        int monitorIndex;

        PlayerSettings();

        int AALevel() const;
	};

    enum class AppPlatform
    {
        Windows,
        Linux,
        Mac
    };

    enum class RendererType
    {
        OpenGL,
        Direct3D11,
        Direct3D12,
        Metal,
        Vulkan
    };

    struct AppSettings
    {
        bool runInBackground;
        std::string appName;
        std::map<AppPlatform, RendererType> renderers;

        AppSettings();
    };

    class AppPlayer
    {
    private:
        AppSettings appSettings;
        PlayerSettings playerSettings;
        void ResetRendering(bool hasFocus);
        AppPlayer();
        AppPlayer(const AppPlayer&);
        AppPlayer(const AppPlayer&&);
    public:
        uint32_t ScreenWidth() const;
        uint32_t ScreenHeight() const;

        AppPlayer(AppSettings appSettings);

        void Run();
    };
}
