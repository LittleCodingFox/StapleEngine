﻿using System.Collections;
using System.Collections.Generic;
using System.Numerics;

namespace Staple;

/// <summary>
/// Transform component.
/// Contains rotation, position, scale, and parent connection.
/// </summary>
[AutoAssignEntity]
public class Transform : IComponent, IEnumerable<Transform>
{
    private readonly List<Transform> children = new();
    private Matrix4x4 matrix = Matrix4x4.Identity;
    private Quaternion rotation = Quaternion.Identity;
    private Vector3 position;
    private Vector3 scale = Vector3.One;

    private Matrix4x4 finalMatrix = Matrix4x4.Identity;
    private Vector3 finalPosition;
    private Vector3 finalScale;
    private Quaternion finalRotation;

    /// <summary>
    /// The parent of this transform, if any.
    /// </summary>
    public Transform parent { get; private set; }

    /// <summary>
    /// The entity related to this transform
    /// </summary>
    public Entity entity { get; internal set; }

    /// <summary>
    /// Gets the transform's Transformation Matrix
    /// </summary>
    public Matrix4x4 Matrix
    {
        get
        {
            if(Changed)
            {
                Changed = false;

                matrix = Math.TransformationMatrix(position, scale, rotation);

                finalMatrix = parent != null ? matrix * parent.Matrix : matrix;

                finalPosition = parent != null ? Vector3.Transform(position, parent.Matrix) : position;
                finalRotation = parent != null ? parent.Rotation * rotation : rotation;
                finalScale = parent != null ? parent.Scale * scale : scale;
            }

            return finalMatrix;
        }
    }

    /// <summary>
    /// The world-space position
    /// </summary>
    public Vector3 Position
    {
        get
        {
            return finalPosition;
        }

        set
        {
            var p = position;

            var parentPosition = parent?.Position ?? Vector3.Zero;

            position = value - parentPosition;

            Changed |= p != position;
        }
    }

    /// <summary>
    /// The local-space position
    /// </summary>
    public Vector3 LocalPosition
    {
        get => position;

        set
        {
            var p = position;

            position = value;

            Changed |= p != position;
        }
    }

    /// <summary>
    /// The world-space scale
    /// </summary>
    public Vector3 Scale
    {
        get
        {
            return finalScale;
        }

        set
        {
            var s = scale;

            var parentScale = parent?.Scale ?? Vector3.One;

            scale = value / parentScale;

            Changed |= s != scale;
        }
    }

    /// <summary>
    /// The local-space scale
    /// </summary>
    public Vector3 LocalScale
    {
        get => scale;

        set
        {
            var s = scale;

            scale = value;

            Changed |= s != scale;
        }
    }

    /// <summary>
    /// The world-space rotation
    /// </summary>
    public Quaternion Rotation
    {
        get
        {
            return finalRotation;
        }

        set
        {
            var r = rotation;

            var parentRotation = parent?.Rotation ?? Quaternion.Identity;

            rotation = Quaternion.Inverse(parentRotation) * value;

            Changed |= r != rotation;
        }
    }

    /// <summary>
    /// The local-space rotation
    /// </summary>
    public Quaternion LocalRotation
    {
        get
        {
            return rotation;
        }

        set
        {
            var r = rotation;

            rotation = value;

            Changed |= r != rotation;
        }
    }

    /// <summary>
    /// The forward direction
    /// </summary>
    public Vector3 Forward
    {
        get
        {
            return Vector3.Normalize(Vector3.Transform(new Vector3(0, 0, -1), Rotation));
        }
    }

    /// <summary>
    /// The backwards direction
    /// </summary>
    public Vector3 Back
    {
        get
        {
            return Vector3.Normalize(Vector3.Transform(new Vector3(0, 0, 1), Rotation));
        }
    }

    /// <summary>
    /// The up direction
    /// </summary>
    public Vector3 Up
    {
        get
        {
            return Vector3.Normalize(Vector3.Transform(new Vector3(0, 1, 0), Rotation));
        }
    }

    /// <summary>
    /// The down direction
    /// </summary>
    public Vector3 Down
    {
        get
        {
            return Vector3.Normalize(Vector3.Transform(new Vector3(0, -1, 0), Rotation));
        }
    }

    /// <summary>
    /// The left direction
    /// </summary>
    public Vector3 Left
    {
        get
        {
            return Vector3.Normalize(Vector3.Transform(new Vector3(-1, 0, 0), Rotation));
        }
    }

    /// <summary>
    /// The right direction
    /// </summary>
    public Vector3 Right
    {
        get
        {
            return Vector3.Normalize(Vector3.Transform(new Vector3(1, 0, 0), Rotation));
        }
    }

    /// <summary>
    /// Whether this transform changed.
    /// We need this to recalculate and cache the transformation matrix.
    /// </summary>
    internal bool Changed { get; set; } = true;

    /// <summary>
    /// The root transform of this transform
    /// </summary>
    public Transform Root
    {
        get
        {
            return parent != null ? parent.Root : this;
        }
    }

    /// <summary>
    /// The total children in this transform
    /// </summary>
    public int ChildCount => children.Count;

    /// <summary>
    /// The index of this transform in its parent
    /// </summary>
    public int SiblingIndex => parent != null ? parent.ChildIndex(this) : 0;

    /// <summary>
    /// Sets this transform's index in its parent
    /// </summary>
    /// <param name="index">The new index</param>
    /// <returns>Whether this was moved</returns>
    public bool SetSiblingIndex(int index) => parent?.MoveChild(this, index) ?? false;

    /// <summary>
    /// Gets a child at a specific index
    /// </summary>
    /// <param name="index">The index of the child</param>
    /// <returns>The child, or null</returns>
    public Transform GetChild(int index) => index >= 0 && index < children.Count ? children[index] : null;

    /// <summary>
    /// Searches for a child transform with a specific name and optional partial search
    /// </summary>
    /// <param name="name">The name of the child</param>
    /// <param name="partial">Whether the search should be partial. If so, it will search as a prefix</param>
    /// <returns>The child transform, or null</returns>
    public Transform SearchChild(string name, bool partial = false)
    {
        if(ChildCount == 0)
        {
            return null;
        }

        foreach(var child in children)
        {
            if(partial && child.entity.Name.StartsWith(name, System.StringComparison.Ordinal))
            {
                return child;
            }

            if(child.entity.Name == name)
            {
                return child;
            }

            var result = child.SearchChild(name, partial);

            if(result != null)
            {
                return result;
            }
        }

        return null;
    }

    /// <summary>
    /// Sets this transform's parent
    /// </summary>
    /// <param name="parent">The new parent (can be null to remove)</param>
    public void SetParent(Transform parent)
    {
        this.parent?.DetachChild(this);

        this.parent = parent;

        parent?.AttachChild(this);

        Scene.RequestWorldUpdate();
    }

    /// <summary>
    /// Detaches a child from our children list
    /// </summary>
    /// <param name="child">The child to detach</param>
    private void DetachChild(Transform child)
    {
        if(children.Contains(child))
        {
            children.Remove(child);
        }
    }

    /// <summary>
    /// Attaches a child to this transform
    /// </summary>
    /// <param name="child">The new child</param>
    private void AttachChild(Transform child)
    {
        if(!children.Contains(child))
        {
            children.Add(child);
        }
    }

    /// <summary>
    /// Gets the index of a child (used exclusively in the sibling index property)
    /// </summary>
    /// <param name="child">The child</param>
    /// <returns>The index, or 0</returns>
    private int ChildIndex(Transform child)
    {
        var index = children.IndexOf(child);

        if(index >= 0)
        {
            return index;
        }

        return 0;
    }

    /// <summary>
    /// Moves a child in our children list
    /// </summary>
    /// <param name="child">The child</param>
    /// <param name="index">The new index</param>
    /// <returns>Whether it was successfully moved</returns>
    private bool MoveChild(Transform child, int index)
    {
        if(children.Contains(child) && index >= 0 && index < children.Count)
        {
            children.Remove(child);
            children.Insert(index, child);

            Scene.RequestWorldUpdate();

            return true;
        }

        return false;
    }

    public IEnumerator<Transform> GetEnumerator()
    {
        return ((IEnumerable<Transform>)children).GetEnumerator();
    }

    IEnumerator IEnumerable.GetEnumerator()
    {
        return ((IEnumerable)children).GetEnumerator();
    }
}
