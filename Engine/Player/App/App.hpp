#pragma once

#include <stdint.h>
#include <string>
#include <map>
#include <memory>
#include "Rendering/IRenderer.hpp"

namespace Staple
{
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
        std::unique_ptr<IRenderer> renderer;
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
