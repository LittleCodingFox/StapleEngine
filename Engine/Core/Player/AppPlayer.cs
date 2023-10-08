﻿using Bgfx;
using Staple.Internal;
using System;
using System.IO;
using System.Linq;
using System.Runtime.CompilerServices;

namespace Staple
{
    internal class AppPlayer
    {
        public readonly AppSettings appSettings;

        private PlayerSettings playerSettings;

        public static int ScreenWidth { get; internal set; }

        public static int ScreenHeight { get; internal set; }

        public static bgfx.RendererType ActiveRendererType { get; internal set; }

        public static AppPlayer active;

        public AppPlayer(AppSettings settings, string[] args)
        {
            appSettings = settings;
            active = this;

            Storage.Update(appSettings.appName, appSettings.companyName);

            var path = Path.Combine(Storage.PersistentDataPath, "Player.log");

            Log.SetLog(new FSLog(path));

            Log.Instance.onLog += (type, message) =>
            {
                Console.WriteLine($"[{type}] {message}");
            };
        }

        public void ResetRendering(bool hasFocus)
        {
            var flags = RenderSystem.ResetFlags(playerSettings.videoFlags);

            if(hasFocus == false && appSettings.runInBackground == false)
            {
                flags |= bgfx.ResetFlags.Suspend;
            }

            AppEventQueue.instance.Add(AppEvent.ResetFlags(flags));
        }

        public void Run()
        {
            playerSettings = PlayerSettings.Load(appSettings);
            PlayerSettings.Save(playerSettings);

            var renderWindow = RenderWindow.Create(playerSettings.screenWidth, playerSettings.screenHeight, false, playerSettings.windowMode,
                appSettings, playerSettings.maximized, playerSettings.monitorIndex, RenderSystem.ResetFlags(playerSettings.videoFlags));

            renderWindow.OnInit = () =>
            {
                Time.fixedDeltaTime = 1000.0f / appSettings.fixedTimeFrameRate / 1000.0f;

                bool hasFocus = renderWindow.window.IsFocused;

                if (appSettings.runInBackground == false && hasFocus == false)
                {
                    ResetRendering(hasFocus);
                }

                Scene.sceneList = ResourceManager.instance.LoadSceneList();

                if (Scene.sceneList == null || Scene.sceneList.Count == 0)
                {
                    Log.Error($"Failed to load scene list");

                    renderWindow.shouldStop = true;

                    throw new Exception("Failed to load scene list");
                }

                Log.Info("Loaded scene list");

                var renderSystem = new RenderSystem();

                try
                {
                    Physics3D.Instance = new Physics3D(new JoltPhysics3D());
                }
                catch(Exception e)
                {
                    Log.Error(e.ToString());

                    renderWindow.shouldStop = true;

                    throw new Exception("Failed to initialize physics");
                }

                SubsystemManager.instance.RegisterSubsystem(renderSystem, RenderSystem.Priority);
                SubsystemManager.instance.RegisterSubsystem(EntitySystemManager.GetEntitySystem(SubsystemType.FixedUpdate), EntitySystemManager.Priority);
                SubsystemManager.instance.RegisterSubsystem(EntitySystemManager.GetEntitySystem(SubsystemType.Update), EntitySystemManager.Priority);
                SubsystemManager.instance.RegisterSubsystem(Physics3D.Instance, Physics3D.Priority);

                var types = TypeCache.AllTypes()
                    .Where(x => typeof(IEntitySystem).IsAssignableFrom(x) && x != typeof(IEntitySystem))
                    .ToArray();

                Log.Info($"Loading {types.Length} entity systems");

                foreach(var type in types)
                {
                    try
                    {
                        var instance = (IEntitySystem)Activator.CreateInstance(type);

                        if (instance != null)
                        {
                            EntitySystemManager.GetEntitySystem(instance.UpdateType)?.RegisterSystem(instance);

                            Log.Info($"Created entity system {type.FullName}");
                        }
                        else
                        {
                            Log.Info($"Failed to create entity system {type.FullName}");
                        }
                    }
                    catch (Exception e)
                    {
                        Log.Warning($"Player: Failed to load entity system {type.FullName}: {e}");
                    }
                }

                Scene.current = ResourceManager.instance.LoadScene(Scene.sceneList[0]);

                if (Scene.current == null)
                {
                    Log.Error($"Failed to load main scene");

                    renderWindow.shouldStop = true;

                    throw new Exception("Failed to load main scene");
                }

                Log.Info("Loaded first scene");

                Log.Info("Finished initializing");
            };

            renderWindow.OnFixedUpdate = () =>
            {
                SubsystemManager.instance.Update(SubsystemType.FixedUpdate);
            };

            renderWindow.OnUpdate = () =>
            {
                SubsystemManager.instance.Update(SubsystemType.Update);
            };

            renderWindow.OnScreenSizeChange = (focus) =>
            {
                ScreenWidth = playerSettings.screenWidth = renderWindow.width;
                ScreenHeight = playerSettings.screenHeight = renderWindow.height;

                ResetRendering(focus);

                PlayerSettings.Save(playerSettings);
            };

            renderWindow.OnCleanup = () =>
            {
                Log.Info("Terminating");

                SubsystemManager.instance.Destroy();

                ResourceManager.instance.Destroy();

                Log.Info("Done");

                Log.Cleanup();
            };

            renderWindow.Run();
        }
    }
}
