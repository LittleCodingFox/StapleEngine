﻿using Newtonsoft.Json;
using System.IO;
using System;
using System.Diagnostics;
using System.Collections.Generic;
using Staple.Internal;

namespace Staple.Editor
{
    partial class StapleEditor
    {
        public void LoadProject(string path)
        {
            basePath = ThumbnailCache.basePath = path;

            Log.Info($"Base Path: {basePath}");

            AssetDatabase.basePath = basePath;

            AssetDatabase.Reload();

            UpdateProjectBrowserNodes();

            try
            {
                Directory.CreateDirectory(Path.Combine(basePath, "Cache"));
            }
            catch (Exception)
            {
            }

            try
            {
                Directory.CreateDirectory(Path.Combine(basePath, "Cache", "Thumbnails"));
            }
            catch (Exception)
            {
            }

            try
            {
                Directory.CreateDirectory(Path.Combine(basePath, "Cache", "Staging"));
            }
            catch (Exception)
            {
            }

            void Recursive(List<ProjectBrowserNode> nodes)
            {
                foreach (var node in nodes)
                {
                    if(node.type == ProjectBrowserNodeType.Folder)
                    {
                        {
                            try
                            {
                                if (File.Exists($"{node.path}.meta") == false)
                                {
                                    File.WriteAllText($"{node.path}.meta", Guid.NewGuid().ToString());
                                }
                            }
                            catch (System.Exception)
                            {
                            }
                        }

                        Recursive(node.subnodes);
                    }
                    else
                    {
                        switch(node.resourceType)
                        {
                            case ProjectResourceType.Texture:
                                {
                                    try
                                    {
                                        if (File.Exists($"{node.path}.meta") == false)
                                        {
                                            var jsonData = JsonConvert.SerializeObject(new TextureMetadata(), Formatting.Indented);

                                            File.WriteAllText($"{node.path}.meta", jsonData);
                                        }
                                    }
                                    catch (System.Exception)
                                    {
                                    }
                                }

                                break;

                            case ProjectResourceType.Shader:

                                {
                                    try
                                    {
                                        if (File.Exists($"{node.path}.meta") == false)
                                        {
                                            File.WriteAllText($"{node.path}.meta", Guid.NewGuid().ToString());
                                        }
                                    }
                                    catch (System.Exception)
                                    {
                                    }
                                }

                                break;
                        }
                    }
                }
            }

            Recursive(projectBrowserNodes);

            try
            {
                projectAppSettings = JsonConvert.DeserializeObject<AppSettings>(File.ReadAllText(Path.Combine(basePath, "Settings", "AppSettings.json")));
            }
            catch(Exception e)
            {
                Log.Error($"Failed to load project app settings: {e}");
            }

            if(projectAppSettings != null)
            {
                LayerMask.AllLayers = projectAppSettings.layers;
                LayerMask.AllSortingLayers = projectAppSettings.sortingLayers;

                window.appSettings.fixedTimeFrameRate = projectAppSettings.fixedTimeFrameRate;

                foreach(var pair in projectAppSettings.renderers)
                {
                    try
                    {
                        Directory.CreateDirectory(Path.Combine(basePath, "Cache", "Staging", pair.Key.ToString()));
                    }
                    catch(Exception)
                    {
                    }
                }
            }

            RefreshStaging();
        }

        public void RefreshStaging()
        {
            var bakerPath = Path.Combine(Environment.CurrentDirectory, "..", "Tools", "bin", "Baker");

            UpdateCSProj();

            if(projectAppSettings == null)
            {
                return;
            }

            foreach (var pair in projectAppSettings.renderers)
            {
                var renderers = new HashSet<string>();

                foreach(var item in pair.Value)
                {
                    switch(item)
                    {
                        case RendererType.Direct3D11:

                            renderers.Add("-r d3d11");

                            break;

                        case RendererType.Direct3D12:

                            renderers.Add("-r d3d12");

                            break;

                        case RendererType.Metal:

                            renderers.Add("-r metal");

                            break;

                        case RendererType.OpenGL:

                            renderers.Add("-r opengl");

                            break;

                        case RendererType.OpenGLES:

                            renderers.Add("-r opengles");

                            break;

                        case RendererType.Vulkan:

                            renderers.Add("-r spirv");

                            break;
                    }
                }

                var args = $"-i \"{basePath}/Assets\" -o \"{basePath}/Cache/Staging/{pair.Key}\" -editor {string.Join(" ", renderers)}".Replace("\\", "/");

                var processInfo = new ProcessStartInfo(bakerPath, args)
                {
                    CreateNoWindow = true,
                    RedirectStandardOutput = true,
                    WindowStyle = ProcessWindowStyle.Hidden,
                    WorkingDirectory = Environment.CurrentDirectory
                };

                var process = new Process
                {
                    StartInfo = processInfo
                };

                if (process.Start())
                {
                    while (process.HasExited == false)
                    {
                        var line = process.StandardOutput.ReadLine();

                        if (line != null)
                        {
                            Log.Info(line);
                        }
                    }
                }
            }
        }
    }
}