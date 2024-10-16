﻿using Newtonsoft.Json.Converters;
using Newtonsoft.Json;
using System.Collections.Generic;
using System.IO;
using System;
using System.Linq;
using Staple.Internal;

namespace Staple.Editor;

internal class AppSettingsWindow : EditorWindow
{
    public string basePath;
    public AppSettings projectAppSettings;

    private readonly List<ModuleType> moduleKinds = [];
    private readonly Dictionary<ModuleType, string[]> moduleNames = [];

    public AppSettingsWindow()
    {
        title = "Settings";

        allowDocking = false;

        moduleKinds = Enum.GetValues<ModuleType>().ToList();

        foreach(var pair in StapleEditor.instance.modulesList)
        {
            var names = pair.Value
                .Select(x => x.moduleName)
                .ToList();

            names.Insert(0, "(None)");

            moduleNames.Add(pair.Key, names.ToArray());
        }
    }

    public override void OnGUI()
    {
        base.OnGUI();

        EditorGUI.TreeNode("General", "APPSETTINGSGENERAL", false, () =>
        {
            projectAppSettings.appName = EditorGUI.TextField("App Name", "APPSETTINGSGENERALNAME", projectAppSettings.appName ?? "");

            projectAppSettings.companyName = EditorGUI.TextField("Company Name", "APPSETTINGSGENERALCOMPANYNAME", projectAppSettings.companyName ?? "");

            projectAppSettings.appBundleID = EditorGUI.TextField("App Bundle ID", "APPSETTINGSGENERALBUNDLEID", projectAppSettings.appBundleID ?? "");

            projectAppSettings.appDisplayVersion = EditorGUI.TextField("App Display Version", "APPSETTINGSGENERALDISPLAYVERSION", projectAppSettings.appDisplayVersion ?? "");

            projectAppSettings.appVersion = EditorGUI.IntField("App Version ID", "APPSETTINGSGENERALVERSION", projectAppSettings.appVersion);

            projectAppSettings.profilingMode = EditorGUI.EnumDropdown("Profiling", "APPSETTINGSGENERALPROFILING", projectAppSettings.profilingMode);
        }, null);

        EditorGUI.TreeNode("Timing", "APPSETTINGSTIMING", false, () =>
        {
            projectAppSettings.fixedTimeFrameRate = EditorGUI.IntField("Fixed Time Frame Rate", "APPSETTINGSTIMINGFIXEDTIMEFRAMERATE", projectAppSettings.fixedTimeFrameRate);

            if (projectAppSettings.fixedTimeFrameRate <= 0)
            {
                projectAppSettings.fixedTimeFrameRate = 1;
            }

            projectAppSettings.maximumFixedTimestepTime = EditorGUI.FloatField("Maximum time spent on fixed timesteps", "APPSETTINGSTIMINGMAXIMUMTIME", projectAppSettings.maximumFixedTimestepTime);

            if (projectAppSettings.maximumFixedTimestepTime <= 0)
            {
                projectAppSettings.maximumFixedTimestepTime = 0.1f;
            }
        }, null);

        EditorGUI.TreeNode("Physics", "APPSETTINGSPHYSICS", false, () =>
        {
            projectAppSettings.physicsFrameRate = EditorGUI.IntField("Physics Frame Rate", "APPSETTINGSPHYSICSFRAMERATE", projectAppSettings.physicsFrameRate);

            if (projectAppSettings.physicsFrameRate <= 0)
            {
                projectAppSettings.physicsFrameRate = 1;
            }
        }, null);

        EditorGUI.TreeNode("Layers", "APPSETTINGSLAYERS", false, () =>
        {
            void Handle(List<string> layers)
            {
                EditorGUI.SameLine();

                EditorGUI.Button("+", "APPSETTINGSLAYERSADD", () =>
                {
                    layers.Add("Layer");
                });

                for (var i = 0; i < layers.Count; i++)
                {
                    layers[i] = EditorGUI.TextField($"Layer {i + 1}", $"APPSETTINGSLAYERS{i}", layers[i]);

                    //Can't remove default layer
                    if (i > 1)
                    {
                        EditorGUI.SameLine();

                        EditorGUI.Button("Up", $"APPSETTINGSLAYERS{i}UP", () =>
                        {
                            (layers[i], layers[i - 1]) = (layers[i - 1], layers[i]);
                        });
                    }

                    if (i > 0 && i + 1 < layers.Count)
                    {
                        EditorGUI.SameLine();

                        EditorGUI.Button("Down", $"APPSETTINGSLAYERS{i}DOWN", () =>
                        {
                            (layers[i], layers[i + 1]) = (layers[i + 1], layers[i]);
                        });
                    }

                    //Can't remove default layer
                    if (i > 0)
                    {
                        EditorGUI.SameLine();

                        EditorGUI.Button("X", $"APPSETTINGSLAYERS{i}REMOVE", () =>
                        {
                            layers.RemoveAt(i);
                        });
                    }
                }

                LayerMask.AllLayers = new(projectAppSettings.layers);
                LayerMask.AllSortingLayers = projectAppSettings.sortingLayers;

                StapleEditor.instance.AddEditorLayers();
            }

            EditorGUI.Label("Layers");

            Handle(projectAppSettings.layers);

            EditorGUI.Label("Sorting Layers");

            Handle(projectAppSettings.sortingLayers);
        }, null);

        EditorGUI.TreeNode("Modules", "APPSETTINGSMODULES", false, () =>
        {
            foreach (var kind in moduleKinds)
            {
                if (StapleEditor.instance.modulesList.TryGetValue(kind, out var modules))
                {
                    projectAppSettings.usedModules.TryGetValue(kind, out var localName);

                    var index = EditorGUI.Dropdown(kind.ToString(), $"APPSETTINGSMODULES{kind}", moduleNames[kind], modules.FindIndex(x => x.moduleName == localName) + 1);

                    if (index > 0 && index <= modules.Count)
                    {
                        projectAppSettings.usedModules.AddOrSetKey(kind, modules[index - 1].moduleName);
                    }
                    else
                    {
                        projectAppSettings.usedModules.AddOrSetKey(kind, "");
                    }
                }
            }
        }, null);

        EditorGUI.TreeNode("Lighting", $"APPSETTINGSLIGHTING", false, () =>
        {
            {
                var current = projectAppSettings.enableLighting;

                projectAppSettings.enableLighting = EditorGUI.Toggle("Enable Lighting", "APPSETTINGSLIGHTINGAMBIENTCOLOR", current);

                if(projectAppSettings.enableLighting != current)
                {
                    LightSystem.Enabled = projectAppSettings.enableLighting;
                }
            }

            {
                var current = projectAppSettings.ambientLight;

                projectAppSettings.ambientLight = EditorGUI.ColorField("Ambient Color", "APPSETTINGSLIGHTINGAMBIENTCOLOR", projectAppSettings.ambientLight);

                if (projectAppSettings.ambientLight != current)
                {
                    AppSettings.Current.ambientLight = projectAppSettings.ambientLight;
                }
            }
        }, null);

        EditorGUI.TreeNode("Rendering and Presentation", $"APPSETTINGSRENDERING", false, () =>
        {
            projectAppSettings.runInBackground = EditorGUI.Toggle("Run in Background", $"APPSETTINGSRENDERINGBACKGROUND", projectAppSettings.runInBackground);

            projectAppSettings.multiThreadedRenderer = EditorGUI.Toggle("Multithreaded Renderer (experimental)", $"APPSETTINGSRENDERINGMULTITHREAD",
                projectAppSettings.multiThreadedRenderer);

            EditorGUI.TabBar(PlayerBackendManager.BackendNames, "APPSETTINGSRENDERINGBACKENDS", (index) =>
            {
                var backend = PlayerBackendManager.Instance.GetBackend(PlayerBackendManager.BackendNames[index]);

                if (backend.platform == AppPlatform.Windows ||
                    backend.platform == AppPlatform.Linux ||
                    backend.platform == AppPlatform.MacOSX)
                {
                    projectAppSettings.defaultWindowMode = EditorGUI.EnumDropdown("Window Mode *", $"APPSETTINGSRENDERINGBACKEND{index}WINDOWMODE",
                        projectAppSettings.defaultWindowMode);

                    projectAppSettings.defaultWindowWidth = EditorGUI.IntField("Window Width *", $"APPSETTINGSRENDERINGBACKEND{index}WINDOWWIDTH",
                        projectAppSettings.defaultWindowWidth);

                    projectAppSettings.defaultWindowHeight = EditorGUI.IntField("Window Height *", $"APPSETTINGSRENDERINGBACKEND{index}WINDOWHEIGHT",
                        projectAppSettings.defaultWindowHeight);
                }
                else if (backend.platform == AppPlatform.Android ||
                    backend.platform == AppPlatform.iOS)
                {
                    projectAppSettings.portraitOrientation = EditorGUI.Toggle("Portrait Orientation *", $"APPSETTINGSRENDERINGBACKEND{index}PORTRAIT",
                        projectAppSettings.portraitOrientation);

                    projectAppSettings.landscapeOrientation = EditorGUI.Toggle("Landscape Orientation *", $"APPSETTINGSRENDERINGBACKEND{index}LANDSCAPE",
                        projectAppSettings.landscapeOrientation);

                    if (backend.platform == AppPlatform.Android)
                    {
                        projectAppSettings.androidMinSDK = EditorGUI.IntField("Android Min SDK", $"APPSETTINGSRENDERINGBACKEND{index}ANDROIDSDK",
                            projectAppSettings.androidMinSDK);

                        if (projectAppSettings.androidMinSDK < 26)
                        {
                            projectAppSettings.androidMinSDK = 26;
                        }
                    }
                    else if (backend.platform == AppPlatform.iOS)
                    {
                        projectAppSettings.iOSDeploymentTarget = EditorGUI.IntField("iOS Deployment Target", $"APPSETTINGSRENDERINGBACKEND{index}IOSDEPLOYMENT",
                            projectAppSettings.iOSDeploymentTarget);

                        if (projectAppSettings.iOSDeploymentTarget < 13)
                        {
                            projectAppSettings.iOSDeploymentTarget = 13;
                        }
                    }
                }

                EditorGUI.Label("Renderers");

                if (projectAppSettings.renderers.TryGetValue(backend.platform, out var renderers) == false)
                {
                    renderers = [];

                    projectAppSettings.renderers.Add(backend.platform, renderers);
                }

                for (var i = 0; i < renderers.Count; i++)
                {
                    var result = EditorGUI.EnumDropdown("Renderer", $"APPSETTINGSRENDERINGRENDERER{index}{i}", renderers[i], backend.renderers);

                    if (result != renderers[i] && renderers.All(x => x != result))
                    {
                        renderers[i] = result;
                    }

                    EditorGUI.SameLine();

                    EditorGUI.Button("-", $"APPSETTINGSRENDERINGRENDERER{index}{i}REMOVE", () =>
                    {
                        renderers.RemoveAt(i);
                    });
                }

                EditorGUI.Button("+", $"APPSETTINGSRENDERINGRENDERER{index}ADD", () =>
                {
                    renderers.Add(backend.renderers.FirstOrDefault());
                });
            });
        }, null);

        EditorGUI.Label("* - Shared setting between platforms");

        EditorGUI.Button("Apply Changes", "APPSETTINGSAPPLYCHANGES", () =>
        {
            try
            {
                var json = JsonConvert.SerializeObject(projectAppSettings, Formatting.Indented, Staple.Tooling.Utilities.JsonSettings);

                File.WriteAllText(Path.Combine(basePath, "Settings", "AppSettings.json"), json);
            }
            catch (Exception)
            {
            }
        });
    }
}
