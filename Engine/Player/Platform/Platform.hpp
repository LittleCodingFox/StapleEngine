#pragma once

#include <bgfx/bgfx.h>
#include <bgfx/platform.h>
#include <type_traits>

#define STAPLE_PLATFORM_LINUX	0
#define STAPLE_PLATFORM_WINDOWS	0
#define STAPLE_PlATFORM_OSX		0

#if BX_PLATFORM_LINUX || BX_PLATFORM_BSD
#undef STAPLE_PLATFORM_LINUX
#define STAPLE_PLATFORM_LINUX	1
#elif BX_PLATFORM_OSX
#undef STAPLE_PLATFORM_OSX
#define STAPLE_PLATFORM_OSX		1
#elif BX_PLATFORM_WINDOWS
#undef STAPLE_PLATFORM_WINDOWS
#define STAPLE_PLATFORM_WINDOWS	1
#endif

#define HAS_FLAG(x, flag) ((x & flag) == flag)

namespace Staple
{
	template<typename T>
	T operator | (T lhs, T rhs)
	{
		return static_cast<T>(static_cast<std::underlying_type<T>::type>(lhs) | static_cast<std::underlying_type<T>::type>(rhs));
	}

	template<typename T>
	T operator & (T lhs, T rhs)
	{
		return static_cast<T>(static_cast<std::underlying_type<T>::type>(lhs) & static_cast<std::underlying_type<T>::type>(rhs));
	}
}
