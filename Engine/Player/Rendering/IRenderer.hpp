#pragma once
#include <stdint.h>
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

	enum class RendererType
	{
		OpenGL,
		Direct3D11,
		Direct3D12,
		Metal,
		Vulkan
	};

	class IRenderer
	{
	private:
		IRenderer(const IRenderer&);
		IRenderer(const IRenderer&&);
	public:
		IRenderer() {}
		virtual ~IRenderer() {}

		virtual bool Create(uint64_t monitorHandle, uint64_t windowHandle, RendererType rendererType,
			VideoFlags videoFlags, uint32_t width, uint32_t height) = 0;
		virtual void ResetRendering(VideoFlags flags, bool suspend) = 0;
		virtual void SetWindowSize(uint32_t width, uint32_t height) = 0;
		virtual void FinishFrame() = 0;
		virtual void Dispose() = 0;
	};
}
