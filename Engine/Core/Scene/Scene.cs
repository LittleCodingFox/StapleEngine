﻿using Staple.Internal;
using System;
using System.Collections.Generic;
using System.Numerics;
using System.Text.Json;

namespace Staple;

public class Scene
{
    internal static bool InstancingComponent = false;

    /// <summary>
    /// The currently active scene
    /// </summary>
    internal static Scene current { get; set; }

    /// <summary>
    /// A list of all scenes we can load
    /// </summary>
    internal static List<string> sceneList = new();

    /// <summary>
    /// Gets all available cameras sorted by depth
    /// </summary>
    public static World.CameraInfo[] SortedCameras => World.Current?.SortedCameras ?? Array.Empty<World.CameraInfo>();

    /// <summary>
    /// Changes the currently active scene
    /// </summary>
    /// <param name="scene">The new scene</param>
    internal static void SetActiveScene(Scene scene)
    {
        current = scene;
    }

    /// <summary>
    /// Instantiates a scene object
    /// </summary>
    /// <param name="sceneObject">The scene object to instantiate</param>
    /// <param name="localID">The local ID of the entity</param>
    /// <param name="activate">Whether to activate the object and call lifecycle callbacks</param>
    /// <returns>The new entity, or Entity.Empty</returns>
    internal static Entity Instantiate(SceneObject sceneObject, out int localID, bool activate)
    {
        localID = sceneObject.ID;

        InstancingComponent = true;

        var entity = Entity.Create(sceneObject.name);

        var transform = entity.AddComponent<Transform>();

        entity.Enabled = sceneObject.enabled;

        var layer = LayerMask.NameToLayer(sceneObject.layer);

        if(layer >= 0)
        {
            entity.Layer = (uint)layer;
        }

        var rotation = sceneObject.transform.rotation.ToVector3();

        transform.LocalPosition = sceneObject.transform.position.ToVector3();
        transform.LocalRotation = Math.FromEulerAngles(rotation);
        transform.LocalScale = sceneObject.transform.scale.ToVector3();

        foreach (var component in sceneObject.components)
        {
            var type = TypeCache.GetType(component.type);

            if (type == null)
            {
                Log.Error($"Failed to create component {component.type} for entity {sceneObject.name}");

                continue;
            }

            var componentInstance = entity.AddComponent(type);

            if (componentInstance == null)
            {
                continue;
            }

            if (component.data != null)
            {
                foreach (var pair in component.data)
                {
                    var field = type.GetField(pair.Key);

                    if (field != null && pair.Value != null && pair.Value is JsonElement element)
                    {
                        SceneSerialization.DeserializeField(field, ref componentInstance, element);
                    }
                }
            }

            if (component.parameters != null)
            {
                foreach (var parameter in component.parameters)
                {
                    if (parameter.name == null)
                    {
                        continue;
                    }

                    try
                    {
                        var field = type.GetField(parameter.name);

                        if (field != null)
                        {
                            SceneSerialization.DeserializeField(field, ref componentInstance, parameter);
                        }
                    }
                    catch (Exception e)
                    {
                        return default;
                    }
                }
            }

            entity.UpdateComponent(componentInstance);
        }

        if(activate)
        {
            entity.IterateComponents((ref IComponent c) =>
            {
                World.Current?.EmitAddComponentEvent(entity, ref c);
            });
        }

        InstancingComponent = false;

        return entity;
    }

    /// <summary>
    /// Iterates through entities, querying for components.
    /// </summary>
    /// <typeparam name="T">The type of the first component</typeparam>
    /// <param name="callback">The callback when handling an entity</param>
    public static void ForEach<T>(World.ForEachCallback<T> callback) where T : IComponent
    {
        World.Current?.ForEach(callback);
    }

    /// <summary>
    /// Iterates through entities, querying for components.
    /// </summary>
    /// <typeparam name="T">The type of the first component</typeparam>
    /// <typeparam name="T2">The type of the second component</typeparam>
    /// <param name="callback">The callback when handling an entity</param>
    public static void ForEach<T, T2>(World.ForEachCallback<T, T2> callback)
        where T : IComponent
        where T2 : IComponent
    {
        World.Current?.ForEach(callback);
    }

    /// <summary>
    /// Iterates through entities, querying for components.
    /// </summary>
    /// <typeparam name="T">The type of the first component</typeparam>
    /// <typeparam name="T2">The type of the second component</typeparam>
    /// <typeparam name="T3">The type of the third component</typeparam>
    /// <param name="callback">The callback when handling an entity</param>
    public static void ForEach<T, T2, T3>(World.ForEachCallback<T, T2, T3> callback)
        where T : IComponent
        where T2 : IComponent
        where T3 : IComponent
    {
        World.Current?.ForEach(callback);
    }

    /// <summary>
    /// Iterates through entities, querying for components.
    /// </summary>
    /// <typeparam name="T">The type of the first component</typeparam>
    /// <typeparam name="T2">The type of the second component</typeparam>
    /// <typeparam name="T3">The type of the third component</typeparam>
    /// <typeparam name="T4">The type of the fourth component</typeparam>
    /// <param name="callback">The callback when handling an entity</param>
    public static void ForEach<T, T2, T3, T4>(World.ForEachCallback<T, T2, T3, T4> callback)
        where T : IComponent
        where T2 : IComponent
        where T3 : IComponent
        where T4 : IComponent
    {
        World.Current?.ForEach(callback);
    }

    /// <summary>
    /// Iterates through entities, querying for components.
    /// </summary>
    /// <typeparam name="T">The type of the first component</typeparam>
    /// <typeparam name="T2">The type of the second component</typeparam>
    /// <typeparam name="T3">The type of the third component</typeparam>
    /// <typeparam name="T4">The type of the fourth component</typeparam>
    /// <typeparam name="T5">The type of the fifth component</typeparam>
    /// <param name="callback">The callback when handling an entity</param>
    public static void ForEach<T, T2, T3, T4, T5>(World.ForEachCallback<T, T2, T3, T4, T5> callback)
        where T : IComponent
        where T2 : IComponent
        where T3 : IComponent
        where T4 : IComponent
        where T5 : IComponent
    {
        World.Current?.ForEach(callback);
    }

    /// <summary>
    /// Counts the amount of entities with a specific component
    /// </summary>
    /// <typeparam name="T">The type of the component</typeparam>
    /// <returns>The amount of entities with the component</returns>
    public static int CountEntities<T>() where T : IComponent
    {
        return CountEntities(typeof(T));
    }

    /// <summary>
    /// Counts the amount of entities with a specific component
    /// </summary>
    /// <param name="t">The type of the component</param>
    /// <returns>The amount of entities with the component</returns>
    public static int CountEntities(Type t)
    {
        return World.Current?.CountEntities(t) ?? 0;
    }

    /// <summary>
    /// Gets an entity by ID
    /// </summary>
    /// <param name="ID">The entity's ID</param>
    /// <returns>The entity if valid, or Entity.Empty</returns>
    public static Entity FindEntity(int ID)
    {
        return World.Current?.FindEntity(ID) ?? default;
    }

    /// <summary>
    /// Attempts to find an entity by name
    /// </summary>
    /// <param name="name">The entity's name</param>
    /// <param name="allowDisabled">Whether to allow finding disabled entities</param>
    /// <returns>The entity if valid, or Entity.Empty</returns>
    public static Entity FindEntity(string name, bool allowDisabled = false)
    {
        return World.Current?.FindEntity(name, allowDisabled) ?? default;
    }

    /// <summary>
    /// Attempts to find an entity and get a specific component
    /// </summary>
    /// <param name="name">The entity's name</param>
    /// <param name="allowDisabled">Whether to allow finding disabled entities</param>
    /// <param name="componentType">The component's type</param>
    /// <param name="component">The returned component if successful</param>
    /// <returns>Whether the entity and component were found</returns>
    public static bool TryFindEntityComponent(string name, bool allowDisabled, Type componentType, out IComponent component)
    {
        if(World.Current == null)
        {
            component = default;

            return false;
        }

        return World.Current.TryFindEntityComponent(name, allowDisabled, componentType, out component);
    }

    /// <summary>
    /// Attempts to find an entity and get a specific component
    /// </summary>
    /// <typeparam name="T"></typeparam>
    /// <param name="name">The entity's name</param>
    /// <param name="allowDisabled">Whether to allow finding disabled entities</param>
    /// <param name="component">The returned component if successful</param>
    /// <returns>Whether the entity and component were found</returns>
    public static bool TryFindEntityComponent<T>(string name, bool allowDisabled, out T component) where T : IComponent
    {
        if (World.Current == null)
        {
            component = default;

            return false;
        }

        return World.Current.TryFindEntityComponent(name, allowDisabled, out component);
    }

    /// <summary>
    /// Iterates through each entity in the scene/world
    /// </summary>
    /// <param name="callback">A callback to handle an entity</param>
    public static void IterateEntities(Action<Entity> callback)
    {
        World.Current?.Iterate(callback);
    }
}
