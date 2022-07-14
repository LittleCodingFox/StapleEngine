local BUILD_DIR = path.join("build", _ACTION)
local cc = _ACTION

if _OPTIONS["cc"] ~= nil then
	BUILD_DIR = BUILD_DIR .. "_" .. _OPTIONS["cc"]
	cc = _OPTIONS["cc"]
end

local BGFX_DIR = "bgfx"
local BIMG_DIR = "bimg"
local BX_DIR = "bx"
local ENTT_DIR = "entt"
local GLFW_DIR = "glfw"

solution "Engine"
	configurations { "Debug", "Release" }
	platforms { "x64" }

	filter "configurations:Release"
		defines "NDEBUG"
		optimize "Full"
	filter "configurations:Debug*"
		defines "_DEBUG"
		optimize "Debug"
		symbols "On"
	filter "platforms:x86_64"
		architecture "x86_64"
	filter "system:macosx"
		xcodebuildsettings {
			["MACOSX_DEPLOYMENT_TARGET"] = "10.9",
			["ALWAYS_SEARCH_USER_PATHS"] = "YES", -- This is the minimum version of macos we'll be able to run on
		};

	project "Core"
		kind "SharedLib"
		language "C#"
		clr "Unsafe"
		
		targetdir "../bin/Core/%{cfg.buildcfg}"
		objdir "../obj/Core/%{cfg.buildcfg}"
		
		files {
			"Core/**.cs"
		}
		
		prebuildcommands {
			"{COPYFILE} %{wks.location}/../Dependencies/build/" .. cc .. "/bin/x86_64/Release/*.dll %{wks.location}%{cfg.targetdir}"
		}

function setBxCompat()
	filter "action:vs*"
		includedirs { path.join("%{wks.location}/../Dependencies/", BX_DIR, "include/compat/msvc") }
	filter { "system:windows", "action:gmake" }
		includedirs { path.join("%{wks.location}/../Dependencies/", BX_DIR, "include/compat/mingw") }
	filter { "system:macosx" }
		includedirs { path.join("%{wks.location}/../Dependencies/", BX_DIR, "include/compat/osx") }
		buildoptions { "-x objective-c++" }
end
	
	project "Player"
		kind "ConsoleApp"
		language "C++"
		cppdialect "C++20"
		
		targetdir "../bin/Player/%{cfg.buildcfg}"
		objdir "../obj/Player/%{cfg.buildcfg}"
		
		includedirs {
			"Player/",
			"../Dependencies/" .. BGFX_DIR .. "/include",
			"../Dependencies/" .. BIMG_DIR .. "/include",
			"../Dependencies/" .. BX_DIR .. "/include",
			"../Dependencies/" .. ENTT_DIR .. "/include",
			"../Dependencies/" .. GLFW_DIR .. "/include"
		}
		
		files {
			"Player/**.hpp",
			"Player/**.cpp"
		}

		libdirs {
			"%{wks.location}/../Dependencies/build/" .. cc .. "/bin/x86_64/%{cfg.buildcfg}"
		}
		
		links {
			"glfw",
			"bgfx",
			"bimg",
			"bx"
		}
		
		prebuildcommands {
			"{COPYFILE} %{wks.location}/../Dependencies/build/" .. cc .. "/bin/x86_64/Release/*.dll %{wks.location}%{cfg.targetdir}"
		}

		filter "action:vs*"
			defines "_CRT_SECURE_NO_WARNINGS"
			buildoptions { "/Zc:__cplusplus" }

		setBxCompat()
