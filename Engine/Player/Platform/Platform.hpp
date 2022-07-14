#pragma once

#include <bgfx/bgfx.h>
#include <bgfx/platform.h>
#include <type_traits>

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
