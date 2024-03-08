#!/bin/sh

premake5 --os=macosx xcode4
premake5 --os=macosx --file=NativeFileDialog/build/premake5.lua xcode4
premake5 --os=macosx --file=premake5_dotnet.lua vs2022

cd build/native

xcodebuild -scheme bx -configuration Debug build -workspace Dependencies.xcworkspace
xcodebuild -scheme bx -configuration Release build -workspace Dependencies.xcworkspace

xcodebuild -scheme bimg -configuration Debug build -workspace Dependencies.xcworkspace
xcodebuild -scheme bimg -configuration Release build -workspace Dependencies.xcworkspace

xcodebuild -scheme bgfx -configuration Debug build -workspace Dependencies.xcworkspace
xcodebuild -scheme bgfx -configuration Release build -workspace Dependencies.xcworkspace

xcodebuild -scheme glfw -configuration Debug build -workspace Dependencies.xcworkspace
xcodebuild -scheme glfw -configuration Release build -workspace Dependencies.xcworkspace

xcodebuild -scheme dr_libs -configuration Debug build -workspace Dependencies.xcworkspace
xcodebuild -scheme dr_libs -configuration Release build -workspace Dependencies.xcworkspace

xcodebuild -scheme StapleSupport -configuration Debug build -workspace Dependencies.xcworkspace
xcodebuild -scheme StapleSupport -configuration Release build -workspace Dependencies.xcworkspace

cd ../../build/dotnet

dotnet publish Dependencies_Dotnet.sln -c Debug -o bin/Debug/net8.0
dotnet publish Dependencies_Dotnet.sln -c Release -o bin/Release/net8.0

cd ../../GENie

make

cd ../bgfx

make GENIE=../GENie/bin/darwin/genie tools -j $(sysctl -n hw.logicalcpu)

mkdir -p ../../Tools/bin

cp .build/osx-x64/bin/*cRelease ../../Tools/bin

cd ../../
