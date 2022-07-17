#pragma once

#include "Rendering/IRenderer.hpp"

namespace Staple
{
	class BGFXRenderer : public IRenderer
	{
	private:
		uint32_t windowWidth;
		uint32_t windowHeight;
		uint32_t lastVideoFlags;
	public:
		BGFXRenderer();
		virtual bool Create(uint64_t monitorHandle, uint64_t windowHandle, RendererType rendererType,
			VideoFlags videoFlags, uint32_t width, uint32_t height) override;
		virtual void ResetRendering(VideoFlags flags, bool suspend) override;
		virtual void SetWindowSize(uint32_t width, uint32_t height) override;
		virtual void FinishFrame() override;
		virtual void Dispose() override;
	};
}
