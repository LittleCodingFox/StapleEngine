﻿using ImGuiNET;
using MessagePack;
using Newtonsoft.Json;
using Newtonsoft.Json.Converters;
using Staple.Internal;
using System;
using System.IO;
using System.Linq;
using System.Numerics;

namespace Staple.Editor
{
    internal partial class StapleEditor
    {
        private void Dockspace()
        {
            var windowFlags = ImGuiWindowFlags.MenuBar | ImGuiWindowFlags.NoDocking | ImGuiWindowFlags.NoTitleBar | ImGuiWindowFlags.NoCollapse |
                ImGuiWindowFlags.NoResize | ImGuiWindowFlags.NoMove | ImGuiWindowFlags.NoBringToFrontOnFocus | ImGuiWindowFlags.NoNavFocus |
                ImGuiWindowFlags.NoBackground;

            ImGui.Begin("Dockspace", windowFlags);

            var dockID = ImGui.GetID("Dockspace");

            ImGui.PushStyleColor(ImGuiCol.WindowBg, Vector4.Zero);

            ImGui.DockSpace(dockID, new Vector2(0, 0), ImGuiDockNodeFlags.PassthruCentralNode);

            ImGui.PopStyleColor();

            if (ImGui.BeginMainMenuBar())
            {
                if (ImGui.BeginMenu("File"))
                {
                    if (ImGui.MenuItem("Project Settings"))
                    {
                        var window = EditorWindow.GetWindow<AppSettingsWindow>();

                        window.projectAppSettings = projectAppSettings;
                        window.basePath = basePath;
                    }

                    if(ImGui.MenuItem("Open Solution"))
                    {
                        csProjManager.OpenGameSolution();
                    }

                    if (ImGui.MenuItem("Save"))
                    {
                        if (Scene.current != null && lastOpenScene != null)
                        {
                            var serializableScene = Scene.current.Serialize();

                            var text = JsonConvert.SerializeObject(serializableScene.objects, Formatting.Indented, new JsonSerializerSettings()
                            {
                                Converters =
                                {
                                    new StringEnumConverter(),
                                }
                            });

                            try
                            {
                                File.WriteAllText(lastOpenScene, text);
                            }
                            catch (Exception)
                            {
                            }
                        }
                    }

                    ImGui.Separator();

                    if (ImGui.MenuItem("Build"))
                    {
                        var window = EditorWindow.GetWindow<BuildWindow>();

                        window.basePath = basePath;
                    }

                    ImGui.Separator();

                    if (ImGui.MenuItem("Exit"))
                    {
                        window.shouldStop = true;
                    }

                    ImGui.EndMenu();
                }

                if (ImGui.BeginMenu("Assets"))
                {
                    if (ImGui.BeginMenu("Create"))
                    {
                        foreach (var pair in registeredAssetTypes)
                        {
                            var name = pair.Value.Name;

                            if (ImGui.MenuItem($"{name}##{pair.Key}"))
                            {
                                var fileName = name;
                                var currentPath = projectBrowser.currentContentNode?.path ?? Path.Combine(projectBrowser.basePath, "Assets");
                                var assetPath = Path.Combine(currentPath, $"{fileName}.asset");

                                try
                                {
                                    if (File.Exists(assetPath))
                                    {
                                        var counter = 1;

                                        for (; ; )
                                        {
                                            assetPath = Path.Combine(currentPath, $"{fileName}{counter++}.asset");

                                            if (File.Exists(assetPath) == false)
                                            {
                                                break;
                                            }
                                        }
                                    }

                                    var assetInstance = (IStapleAsset)Activator.CreateInstance(pair.Value);

                                    if (assetInstance != null && SaveAsset(assetPath, assetInstance))
                                    {
                                        RefreshAssets(false);
                                    }
                                }
                                catch (Exception e)
                                {
                                    Log.Error($"Failed to create asset at {assetPath}: {e}");
                                }
                            }
                        }

                        ImGui.EndMenu();
                    }

                    ImGui.EndMenu();
                }

                void Recursive(MenuItemInfo parent)
                {
                    if(parent.children.Count == 0)
                    {
                        if(ImGui.MenuItem($"{parent.name}##0") && parent.onClick != null)
                        {
                            try
                            {
                                parent.onClick();
                            }
                            catch(Exception)
                            {
                            }
                        }
                    }
                    else if(ImGui.BeginMenu($"{parent.name}##0"))
                    {
                        foreach(var child in parent.children)
                        {
                            Recursive(child);
                        }

                        ImGui.EndMenu();
                    }
                }

                foreach(var item in menuItems)
                {
                    Recursive(item);
                }

                ImGui.EndMainMenuBar();
            }

            mouseIsHoveringImGui |= ImGui.IsWindowHovered();

            ImGui.End();
        }

        private void Entities(ImGuiIOPtr io)
        {
            ImGui.Begin("Entities");

            ImGui.BeginChildFrame(ImGui.GetID("EntityFrame"), new Vector2(0, 0));

            if (Scene.current != null)
            {
                bool skip = false;

                void Recursive(Transform transform)
                {
                    if(skip || transform == null)
                    {
                        return;
                    }

                    var flags = ImGuiTreeNodeFlags.SpanFullWidth | ImGuiTreeNodeFlags.OpenOnArrow;

                    if (transform.ChildCount == 0)
                    {
                        flags |= ImGuiTreeNodeFlags.Leaf | ImGuiTreeNodeFlags.NoTreePushOnOpen;
                    }

                    var entityName = Scene.current.world.GetEntityName(transform.entity);

                    if (ImGui.TreeNodeEx($"{entityName}##0", flags))
                    {
                        if (ImGui.IsItemHovered())
                        {
                            if(ImGui.IsMouseClicked(ImGuiMouseButton.Right))
                            {
                                ImGui.OpenPopup($"{transform.entity.ID}_Context");
                            }
                            else if(ImGui.IsMouseDoubleClicked(ImGuiMouseButton.Left))
                            {
                                SetSelectedEntity(transform.entity);
                            }
                        }

                        if (ImGui.BeginPopup($"{transform.entity.ID}_Context"))
                        {
                            if (ImGui.MenuItem("Create Entity"))
                            {
                                var entity = Scene.current.world.CreateEntity();

                                var t = Scene.current.world.AddComponent<Transform>(entity);

                                t.entity = entity;

                                t.SetParent(transform);

                                ImGui.EndPopup();

                                skip = true;

                                return;
                            }

                            if (ImGui.MenuItem("Delete"))
                            {
                                Scene.current.world.DestroyEntity(transform.entity);

                                ImGui.EndPopup();

                                skip = true;

                                return;
                            }

                            ImGui.EndPopup();
                        }

                        foreach (var child in transform)
                        {
                            var childEntity = Scene.current.FindEntity(child.entity.ID);

                            if (childEntity != Entity.Empty)
                            {
                                var t = Scene.current.GetComponent<Transform>(childEntity);

                                if(t != null)
                                {
                                    Recursive(t);
                                }
                            }

                            if(skip)
                            {
                                break;
                            }
                        }

                        if (transform.ChildCount > 0 || skip)
                        {
                            ImGui.TreePop();
                        }
                    }
                }

                Scene.current.world.Iterate((entity) =>
                {
                    var transform = Scene.current.GetComponent<Transform>(entity);

                    if(transform == null)
                    {
                        return;
                    }

                    if (transform.parent == null)
                    {
                        Recursive(transform);
                    }
                });
            }

            if(ImGui.IsWindowHovered() && ImGui.IsAnyItemHovered() == false && ImGui.IsMouseClicked(ImGuiMouseButton.Right))
            {
                ImGui.OpenPopup("EntityPanelContext");
            }

            if(ImGui.BeginPopup("EntityPanelContext"))
            {
                if(ImGui.MenuItem("Create Entity"))
                {
                    var entity = Scene.current.world.CreateEntity();

                    var transform = Scene.current.world.AddComponent<Transform>(entity);

                    transform.entity = entity;

                    ImGui.EndMenu();
                }

                ImGui.EndPopup();
            }

            mouseIsHoveringImGui |= ImGui.IsWindowHovered();

            ImGui.EndChildFrame();

            mouseIsHoveringImGui |= ImGui.IsWindowHovered();

            ImGui.End();
        }

        private void Viewport(ImGuiIOPtr io)
        {
            ImGui.Begin("Viewport", ImGuiWindowFlags.NoBackground);

            if (ImGui.BeginTabBar("Viewport Tab"))
            {
                if (ImGui.BeginTabItem("Scene"))
                {
                    viewportType = ViewportType.Scene;

                    ImGui.EndTabItem();
                }

                if (ImGui.BeginTabItem("Game"))
                {
                    viewportType = ViewportType.Game;

                    ImGui.EndTabItem();
                }

                ImGui.EndTabBar();
            }

            if (viewportType == ViewportType.Game)
            {
                ImGui.BeginChildFrame(ImGui.GetID("GameView"), new Vector2(0, 0), ImGuiWindowFlags.NoBackground);

                var width = (ushort)ImGui.GetContentRegionAvail().X;
                var height = (ushort)ImGui.GetContentRegionAvail().Y;

                if (gameRenderTarget == null || gameRenderTarget.width != width || gameRenderTarget.height != height)
                {
                    gameRenderTarget?.Destroy();

                    gameRenderTarget = RenderTarget.Create(width, height);
                }

                var texture = gameRenderTarget.GetTexture();

                if (texture != null)
                {
                    ImGui.Image(ImGuiProxy.GetImGuiTexture(texture), new Vector2(width, height));
                }

                mouseIsHoveringImGui |= ImGui.IsWindowHovered();

                ImGui.End();
            }

            ImGui.End();
        }

        private void Inspector(ImGuiIOPtr io)
        {
            EditorGUI.Changed = false;

            ImGui.Begin("Inspector");

            ImGui.BeginChildFrame(ImGui.GetID("Toolbar"), new Vector2(0, 0));

            if (selectedEntity != null && Scene.current != null && Scene.current.world.IsValidEntity(selectedEntity))
            {
                var name = Scene.current.world.GetEntityName(selectedEntity);

                if(ImGui.InputText("Name", ref name, 120))
                {
                    Scene.current.world.SetEntityName(selectedEntity, name);
                }

                var enabled = Scene.current.world.IsEntityEnabled(selectedEntity);

                var newValue = EditorGUI.Toggle("Enabled", enabled);

                if(newValue != enabled)
                {
                    Scene.current.world.SetEntityEnabled(selectedEntity, newValue);
                }

                var currentLayer = Scene.current.world.GetEntityLayer(selectedEntity);
                var layers = LayerMask.AllLayers;

                if (ImGui.BeginCombo("Layer", currentLayer < layers.Count ? layers[(int)currentLayer] : ""))
                {
                    for (var j = 0; j < layers.Count; j++)
                    {
                        bool selected = j == currentLayer;

                        if (ImGui.Selectable(layers[j], selected))
                        {
                            Scene.current.world.SetEntityLayer(selectedEntity, (uint)j);
                        }
                    }

                    ImGui.EndCombo();
                }

                var counter = 0;

                Scene.current.world.IterateComponents(selectedEntity, (ref IComponent component) =>
                {
                    if (ImGui.TreeNodeEx(component.GetType().Name + $"##{counter++}", ImGuiTreeNodeFlags.SpanFullWidth | ImGuiTreeNodeFlags.OpenOnArrow))
                    {
                        if (component is Transform transform)
                        {
                            transform.LocalPosition = EditorGUI.Vector3Field("Position", transform.LocalPosition);

                            var rotation = Math.ToEulerAngles(transform.LocalRotation);

                            var newRotation = EditorGUI.Vector3Field("Rotation", rotation);

                            if (rotation != newRotation)
                            {
                                transform.LocalRotation = Math.FromEulerAngles(newRotation);
                            }

                            transform.LocalScale = EditorGUI.Vector3Field("Scale", transform.LocalScale);
                        }
                        else
                        {
                            if(ImGui.SmallButton("X"))
                            {
                                Scene.current.world.RemoveComponent(selectedEntity, component.GetType());

                                ImGui.TreePop();

                                return;
                            }

                            if (cachedEditors.TryGetValue($"{counter}{component.GetType().FullName}", out var editor))
                            {
                                editor.OnInspectorGUI();
                            }
                            else
                            {
                                defaultEditor.target = component;

                                defaultEditor.OnInspectorGUI();
                            }
                        }

                        Scene.current.UpdateComponent(selectedEntity, component);

                        ImGui.TreePop();
                    }
                });

                if(ImGui.Button("Add Component"))
                {
                    ImGui.OpenPopup("SelectedEntityComponentList");
                }

                if(ImGui.BeginPopup("SelectedEntityComponentList"))
                {
                    ImGui.SetNextWindowPos(new Vector2(ImGui.GetItemRectMin().X, ImGui.GetItemRectMax().Y));
                    ImGui.SetNextWindowSize(new Vector2(ImGui.GetContentRegionAvail().X, 0));

                    if(ImGui.Begin("##ComponentsList", ImGuiWindowFlags.NoTitleBar |
                        ImGuiWindowFlags.NoMove |
                        ImGuiWindowFlags.NoResize |
                        ImGuiWindowFlags.Tooltip |
                        ImGuiWindowFlags.NoFocusOnAppearing |
                        ImGuiWindowFlags.ChildWindow))
                    {
                        foreach(var component in registeredComponents)
                        {
                            if(Scene.current.world.GetComponent(selectedEntity, component) != null)
                            {
                                continue;
                            }

                            ImGui.Selectable($"{component.Name}##0");

                            if (ImGui.IsItemClicked())
                            {
                                mouseIsHoveringImGui = true;

                                Scene.current.world.AddComponent(selectedEntity, component);

                                ImGui.CloseCurrentPopup();

                                break;
                            }
                        }

                        mouseIsHoveringImGui |= ImGui.IsWindowHovered();

                        ImGui.End();
                    }

                    mouseIsHoveringImGui |= ImGui.IsWindowHovered();

                    ImGui.EndPopup();
                }
            }
            else if(selectedProjectNode != null && selectedProjectNodeData != null)
            {
                if (cachedEditors.Count > 0)
                {
                    var editor = cachedEditors.First().Value;

                    editor.target = selectedProjectNodeData;

                    editor.OnInspectorGUI();
                }
            }

            mouseIsHoveringImGui |= ImGui.IsWindowHovered();

            ImGui.EndChildFrame();

            mouseIsHoveringImGui |= ImGui.IsWindowHovered();

            ImGui.End();
        }

        private void BottomPanel(ImGuiIOPtr io)
        {
            ImGui.Begin("BottomPanel");

            ImGui.BeginChildFrame(ImGui.GetID("Toolbar"), new Vector2(0, 32));

            if (ImGui.BeginTabBar("BottomTabBar"))
            {
                if (ImGui.TabItemButton("Project"))
                {
                    activeBottomTab = 0;
                }

                if (ImGui.TabItemButton("Log"))
                {
                    activeBottomTab = 1;
                }

                ImGui.EndTabBar();
            }

            mouseIsHoveringImGui |= ImGui.IsWindowHovered();

            ImGui.EndChildFrame();

            switch (activeBottomTab)
            {
                case 0:

                    ProjectBrowser(io);

                    break;

                case 1:

                    Console(io);

                    break;
            }

            mouseIsHoveringImGui |= ImGui.IsWindowHovered();

            ImGui.End();
        }

        private void ProjectBrowser(ImGuiIOPtr io)
        {
            mouseIsHoveringImGui |= projectBrowser.Draw(io, (item) =>
            {
                selectedEntity = Entity.Empty;
                selectedProjectNode = item;
                selectedProjectNodeData = null;

                cachedEditors.Clear();
                cachedGizmoEditors.Clear();
                EditorGUI.pendingObjectPickers.Clear();

                EditorWindow.GetWindow<AssetPickerWindow>().Close();

                if (selectedProjectNode == null)
                {
                    return;
                }

                var data = string.Empty;

                try
                {
                    data = File.ReadAllText($"{item.path}.meta");
                }
                catch (Exception)
                {
                }

                if (data.Length > 0)
                {
                    object original = null;

                    var cachePath = item.path;

                    var pathIndex = item.path.IndexOf("Assets");

                    if (pathIndex >= 0)
                    {
                        cachePath = Path.Combine(basePath, "Cache", "Staging", currentPlatform.ToString(), item.path.Substring(pathIndex + "Assets\\".Length));
                    }

                    if (item.typeName == typeof(Texture).FullName)
                    {
                        try
                        {
                            original = JsonConvert.DeserializeObject<TextureMetadata>(data);
                            selectedProjectNodeData = JsonConvert.DeserializeObject<TextureMetadata>(data);
                        }
                        catch (Exception)
                        {
                        }

                        if (original != null && selectedProjectNodeData != null)
                        {
                            var editor = new TextureAssetEditor()
                            {
                                original = original as TextureMetadata,
                                path = $"{item.path}.meta",
                                cachePath = cachePath,
                                target = selectedProjectNodeData,
                            };

                            cachedEditors.Add("", editor);

                            editor.UpdatePreview();
                        }
                    }
                    else if (item.typeName == typeof(Material).FullName)
                    {
                        try
                        {
                            selectedProjectNodeData = JsonConvert.DeserializeObject<MaterialMetadata>(data);
                        }
                        catch (Exception)
                        {
                        }
                    }
                    else if (item.typeName == typeof(Shader).FullName)
                    {
                        try
                        {
                            selectedProjectNodeData = JsonConvert.DeserializeObject<ShaderMetadata>(data);
                        }
                        catch (Exception)
                        {
                        }
                    }
                    else if(item.typeName == typeof(FolderAsset).FullName)
                    {
                        try
                        {
                            original = JsonConvert.DeserializeObject<FolderAsset>(data);
                            selectedProjectNodeData = JsonConvert.DeserializeObject<FolderAsset>(data);

                            var editor = Editor.CreateEditor(selectedProjectNodeData);

                            if(editor != null)
                            {
                                editor.original = original;
                                editor.path = $"{item.path}.meta";
                                editor.cachePath = $"{cachePath}.meta";

                                cachedEditors.Add("", editor);
                            }
                        }
                        catch (Exception)
                        {
                        }
                    }
                    else
                    {
                        var type = TypeCache.GetType(item.typeName);

                        if(type != null && typeof(IStapleAsset).IsAssignableFrom(type))
                        {
                            try
                            {
                                var byteData = File.ReadAllBytes(item.path);

                                using var stream = new MemoryStream(byteData);

                                var header = MessagePackSerializer.Deserialize<SerializableStapleAssetHeader>(stream);

                                if(header.header.SequenceEqual(SerializableStapleAssetHeader.ValidHeader) &&
                                    header.version == SerializableStapleAssetHeader.ValidVersion)
                                {
                                    var asset = MessagePackSerializer.Deserialize<SerializableStapleAsset>(stream);

                                    if(asset != null)
                                    {
                                        selectedProjectNodeData = AssetSerialization.Deserialize(asset);
                                        original = AssetSerialization.Deserialize(asset);

                                        if (selectedProjectNodeData != null)
                                        {
                                            var editor = Editor.CreateEditor(selectedProjectNodeData);

                                            if (editor == null)
                                            {
                                                editor = new StapleAssetEditor()
                                                {
                                                    target = selectedProjectNodeData,
                                                };
                                            }

                                            editor.original = original;
                                            editor.path = item.path;
                                            editor.cachePath = cachePath;

                                            cachedEditors.Add("", editor);
                                        }
                                    }
                                }
                            }
                            catch(Exception)
                            {
                            }
                        }
                    }
                }
            },
            (item) =>
            {
                if(item.type != ProjectBrowserNodeType.File)
                {
                    return;
                }

                switch (item.action)
                {
                    case ProjectBrowserNodeAction.InspectScene:

                        var scene = ResourceManager.instance.LoadRawSceneFromPath(item.path);

                        Scene.SetActiveScene(scene);

                        if (scene != null)
                        {
                            lastOpenScene = item.path;

                            ResetScenePhysics();

                            UpdateLastSession();
                        }

                        break;
                }
            });
        }

        private void Console(ImGuiIOPtr io)
        {
        }

        private void ProgressPopup(ImGuiIOPtr io)
        {
            if(wasShowingProgress != showingProgress && showingProgress)
            {
                ImGui.OpenPopup("ShowingProgress");
            }

            if (showingProgress)
            {
                ImGui.SetNextWindowPos(new Vector2((io.DisplaySize.X - 300) / 2, (io.DisplaySize.Y - 200) / 2));
                ImGui.SetNextWindowSize(new Vector2(300, 200));

                ImGui.BeginPopupModal("ShowingProgress", ref showingProgress,
                    ImGuiWindowFlags.NoTitleBar |
                    ImGuiWindowFlags.NoDocking |
                    ImGuiWindowFlags.NoResize |
                    ImGuiWindowFlags.NoMove);

                ImGui.ProgressBar(progressFraction, new Vector2(250, 20));

                ImGui.EndPopup();

                lock (backgroundLock)
                {
                    if (backgroundThreads.Count == 0)
                    {
                        showingProgress = false;

                        ImGui.CloseCurrentPopup();
                    }
                }
            }

            wasShowingProgress = showingProgress;
        }
    }
}
