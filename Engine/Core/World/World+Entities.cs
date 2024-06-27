﻿using System.Collections.Generic;

namespace Staple;

public partial class World
{
    private const string DefaultEntityName = "Entity";

    /// <summary>
    /// Checks whether an entity is valid
    /// </summary>
    /// <param name="entity">The entity</param>
    /// <returns>Whether it is valid</returns>
    public bool IsValidEntity(Entity entity)
    {
        var localID = entity.Identifier.ID - 1;

        lock (lockObject)
        {
            if (localID >= 0 &&
                localID < entities.Count &&
                entities[localID].alive &&
                entities[localID].generation == entity.Identifier.generation)
            {
                return true;
            }
        }

        return false;
    }

    /// <summary>
    /// Gets an entity's internal data if valid
    /// </summary>
    /// <param name="entity">The entity</param>
    /// <returns>The entity info, or null</returns>
    internal bool TryGetEntity(Entity entity, out EntityInfo info)
    {
        var localID = entity.Identifier.ID - 1;

        lock (lockObject)
        {
            if (localID < 0 ||
                localID >= entities.Count ||
                entities[localID].alive == false ||
                entities[localID].generation != entity.Identifier.generation)
            {
                info = default;

                return false;
            }

            info = entities[localID];

            return true;
        }
    }

    /// <summary>
    /// Checks whether an enity is enabled
    /// </summary>
    /// <param name="entity">The entity to check</param>
    /// <returns>Whether the entity is enabled</returns>
    public bool IsEntityEnabled(Entity entity, bool checkParent = false)
    {
        if (TryGetEntity(entity, out var entityInfo) == false)
        {
            return false;
        }

        lock (lockObject)
        {
            if (entityInfo.enabled == false)
            {
                return false;
            }

            if (checkParent == false)
            {
                return true;
            }

            var transform = GetComponent<Transform>(entity);

            if (transform == null)
            {
                return true;
            }

            bool Recursive(Transform t)
            {
                if (t == null)
                {
                    return true;
                }

                if (IsEntityEnabled(t.entity, false) == false)
                {
                    return false;
                }

                if (t.parent != null)
                {
                    return Recursive(t.parent);
                }

                return true;
            }

            return Recursive(transform.parent);
        }
    }

    /// <summary>
    /// Enables or disables an entity
    /// </summary>
    /// <param name="entity">The entity to change</param>
    /// <param name="enabled">Whether it should be enabled</param>
    public void SetEntityEnabled(Entity entity, bool enabled)
    {
        if (TryGetEntity(entity, out var entityInfo) == false)
        {
            return;
        }

        lock (lockObject)
        {
            entityInfo.enabled = enabled;
        }
    }

    /// <summary>
    /// Creates an empty entity. It might be a recycled entity.
    /// </summary>
    /// <returns>The entity</returns>
    public Entity CreateEntity()
    {
        lock (lockObject)
        {
            for (var i = 0; i < entities.Count; i++)
            {
                if (entities[i].alive == false)
                {
                    var other = entities[i];

                    other.name = DefaultEntityName;
                    other.generation++;
                    other.layer = 0;

                    other.alive = true;
                    other.enabled = true;

                    return new Entity()
                    {
                        Identifier = new()
                        {
                            ID = other.ID,
                            generation = other.generation,
                        },
                    };
                }
            }

            var newEntity = new EntityInfo()
            {
                ID = entities.Count + 1,
                localID = entities.Count,
                alive = true,
                enabled = true,
                name = DefaultEntityName,
            };

            entities.Add(newEntity);

            foreach (var pair in componentsRepository)
            {
                pair.Value.AddComponent();
            }

            return new Entity()
            {
                Identifier = new()
                {
                    ID = newEntity.ID,
                    generation = newEntity.generation,
                },
            };
        }
    }

    /// <summary>
    /// Destroys an entity.
    /// </summary>
    /// <param name="entity">The entity to destroy</param>
    public void DestroyEntity(Entity entity)
    {
        if (TryGetEntity(entity, out var entityInfo) == false)
        {
            return;
        }

        lock (lockObject)
        {
            var transform = GetComponent<Transform>(entity);

            transform?.SetParent(null);

            entityInfo.components.Clear();
            entityInfo.alive = false;

            while (transform.ChildCount > 0)
            {
                var child = transform.GetChild(0);

                DestroyEntity(child.entity);
            }
        }
    }

    /// <summary>
    /// Gets an entity's name.
    /// </summary>
    /// <param name="entity">The entity</param>
    /// <returns>The current name of the entity</returns>
    public string GetEntityName(Entity entity)
    {
        if (TryGetEntity(entity, out var entityInfo) == false)
        {
            return default;
        }

        lock (lockObject)
        {
            return entityInfo.name;
        }
    }

    /// <summary>
    /// Sets an entity's name.
    /// </summary>
    /// <param name="entity">The entity</param>
    /// <param name="name">The new name</param>
    public void SetEntityName(Entity entity, string name)
    {
        if (TryGetEntity(entity, out var entityInfo) == false)
        {
            return;
        }

        lock (lockObject)
        {
            entityInfo.name = name;
        }
    }

    /// <summary>
    /// Gets an entity's layer.
    /// </summary>
    /// <param name="entity">The entity</param>
    /// <returns>The current layer of the entity</returns>
    public uint GetEntityLayer(Entity entity)
    {
        if (TryGetEntity(entity, out var entityInfo) == false)
        {
            return default;
        }

        lock (lockObject)
        {
            return entityInfo.layer;
        }
    }

    /// <summary>
    /// Sets an entity's layer.
    /// </summary>
    /// <param name="entity">The entity</param>
    /// <param name="layer">The new layter</param>
    public void SetEntityLayer(Entity entity, uint layer)
    {
        if (TryGetEntity(entity, out var entityInfo) == false)
        {
            return;
        }

        lock (lockObject)
        {
            entityInfo.layer = layer;
        }
    }

    /// <summary>
    /// Sets an entity's prefab
    /// </summary>
    /// <param name="entity">the entity to set the prefab</param>
    /// <param name="guid">The prefab guid</param>
    /// <param name="localID">the local entity ID in the prefab</param>
    public void SetEntityPrefab(Entity entity, string guid, int localID)
    {
        if (TryGetEntity(entity, out var entityInfo) == false)
        {
            return;
        }

        lock (lockObject)
        {
            entityInfo.prefabGUID = guid;
            entityInfo.prefabLocalID = localID;
        }
    }

    /// <summary>
    /// Attempts to get an entity's prefab data, if any
    /// </summary>
    /// <param name="entity">The entity to check</param>
    /// <param name="guid">The prefab guid</param>
    /// <param name="localID">The prefab local ID</param>
    /// <returns>Whether the prefab data was found</returns>
    public bool TryGetEntityPrefab(Entity entity, out string guid, out int localID)
    {
        if (TryGetEntity(entity, out var entityInfo) == false)
        {
            guid = default;
            localID = default;

            return false;
        }

        lock (lockObject)
        {
            if((entityInfo.prefabGUID?.Length ?? 0) <= 0)
            {
                guid = default;
                localID = default;

                return false;
            }

            guid = entityInfo.prefabGUID;
            localID = entityInfo.prefabLocalID;

            return true;
        }
    }
}
