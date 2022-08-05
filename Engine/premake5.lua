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
		
		targetname "StapleCore"
		targetdir "../bin/Core/%{cfg.buildcfg}"
		objdir "../obj/Core/%{cfg.buildcfg}"

		libdirs {
			"../Dependencies/build/" .. cc .. "/bin/x86_64/Release/"
		}

        links {
			"glfwnet",
			"System",
            "System.Drawing",
			"System.Memory",
            "System.Numerics",
			"System.Core",
			"../Dependencies/build/" .. cc .. "/bin/x86_64/Release/MessagePack.dll"
        }
		
		files {
			"Core/**.cs"
		}

	project "Player"
		kind "ConsoleApp"
		language "C#"
		clr "Unsafe"
		
		targetdir "../bin/Player/%{cfg.buildcfg}"
		objdir "../obj/Player/%{cfg.buildcfg}"
		
		links {
			"Core"
		}
		
		files {
			"Player/**.cs"
		}
		
		postbuildcommands {
			"{MKDIR} %{wks.location}/../Staging/Data",
			"{COPYFILE} %{wks.location}%{cfg.targetdir}/*.exe %{wks.location}/../Staging",
			"{COPYFILE} %{wks.location}%{cfg.targetdir}/../../Core/%{cfg.buildcfg}/StapleCore.dll %{wks.location}/../Staging",
		}

        filter "system:windows"
    		postbuildcommands {
			    "{COPYFILE} %{wks.location}/../Dependencies/build/" .. cc .. "/bin/x86_64/%{cfg.buildcfg}/*.dll %{wks.location}/../Staging",
    		}

        filter "system:linux"
    		postbuildcommands {
				-- Linux is messy to build .NET projects with premake makefiles so we need to copy from two places
			    "{COPYFILE} %{wks.location}/../Dependencies/build/gmake/bin/x86_64/%{cfg.buildcfg}/*.so %{wks.location}/../Staging",
			    "{COPYFILE} %{wks.location}/../Dependencies/build/vs2019/bin/x86_64/%{cfg.buildcfg}/*.dll %{wks.location}/../Staging"
    		}

		filter "system:macos"
    		postbuildcommands {
			    "{COPYFILE} %{wks.location}/../Dependencies/build/" .. cc .. "/bin/x86_64/%{cfg.buildcfg}/*.so %{wks.location}/../Staging",
			    "{COPYFILE} %{wks.location}/../Dependencies/build/" .. cc .. "/bin/x86_64/%{cfg.buildcfg}/*.dll %{wks.location}/../Staging"
    		}
	
	project "TestGame"
		kind "SharedLib"
		language "C#"
		clr "Unsafe"
		
		targetname "Game"
		targetdir "../bin/TestGame/%{cfg.buildcfg}"
		objdir "../obj/TestGame/%{cfg.buildcfg}"
		
		links {
			"Core",
			"System.Numerics"
		}
		
		files {
			"TestGame/**.cs"
		}

		postbuildcommands {
			"{MKDIR} %{wks.location}/../Staging/Data",
			"{COPYFILE} %{wks.location}%{cfg.targetdir}/Game.dll %{wks.location}/../Staging/Data",
		}
