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
	
	project "Player"
		kind "ConsoleApp"
		language "C++"
		
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
