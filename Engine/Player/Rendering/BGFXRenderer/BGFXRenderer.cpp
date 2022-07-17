#include "BGFXRenderer.hpp"
#include <stdio.h>

namespace Staple
{
    uint32_t ResetFlags(VideoFlags flags, bool suspend)
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

        if (suspend)
        {
            outValue |= BGFX_RESET_SUSPEND;
        }

        return outValue;
    }

    BGFXRenderer::BGFXRenderer() : windowWidth(0), windowHeight(0), lastVideoFlags(0)
    {
    }

    bool BGFXRenderer::Create(uint64_t monitorHandle, uint64_t windowHandle, RendererType rendererType,
        VideoFlags videoFlags, uint32_t width, uint32_t height)
    {
        windowWidth = width;
        windowHeight = height;

        bgfx::renderFrame();

        bgfx::Init init;

        init.platformData.ndt = NULL;

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
        init.platformData.ndt = (void*)monitorHandle;
        init.platformData.nwh = (void*)windowHandle;
        init.resolution.width = windowWidth;
        init.resolution.height = windowHeight;
        init.resolution.reset = lastVideoFlags = ResetFlags(videoFlags, false);

        if (bgfx::init(init) == false)
        {
            return false;
        }

        bgfx::setViewClear(0, BGFX_CLEAR_COLOR | BGFX_CLEAR_DEPTH, 0x334455FF);
        bgfx::setViewRect(0, 0, 0, bgfx::BackbufferRatio::Equal);

        return true;
    }

    void BGFXRenderer::ResetRendering(VideoFlags videoFlags, bool suspend)
    {
        lastVideoFlags = ResetFlags(videoFlags, suspend);

        bgfx::reset(windowWidth, windowHeight, lastVideoFlags, bgfx::TextureFormat::RGBA32U);
    }

    void BGFXRenderer::SetWindowSize(uint32_t width, uint32_t height)
    {
        windowWidth = width;
        windowHeight = height;

        bgfx::reset(windowWidth, windowHeight, lastVideoFlags, bgfx::TextureFormat::RGBA32U);
    }

    void BGFXRenderer::FinishFrame()
    {
        bgfx::touch(0);

        bgfx::frame();
    }

    void BGFXRenderer::Dispose()
    {
        bgfx::shutdown();
    }
}
