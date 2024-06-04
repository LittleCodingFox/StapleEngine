﻿using Staple.Internal;
using System;

namespace Staple.Editor;

[CustomEditor(typeof(SpriteRenderer))]
internal class SpriteRendererEditor : Editor
{
    public override bool DrawProperty(Type fieldType, string name, Func<object> getter, Action<object> setter, Func<Type, Attribute> attributes)
    {
        var renderer = target as SpriteRenderer;

        switch(name)
        {
            case nameof(SpriteRenderer.texture):

                {
                    var value = (Texture)getter();

                    EditorUtils.SpritePicker(name, ref value, ref renderer.spriteIndex, setter);
                }

                return true;

            case nameof(SpriteRenderer.spriteIndex):

                return true;

            case nameof(SpriteRenderer.material):

                if(renderer.material == null)
                {
                    renderer.material = ResourceManager.instance.LoadMaterial("Hidden/Materials/Sprite.mat");
                }

                return false;
        }

        return false;
    }

    public override void OnInspectorGUI()
    {
        base.OnInspectorGUI();

        var renderer = (SpriteRenderer)target;

        EditorGUI.Label($"Bounds: Center: {renderer.bounds.center} Size: {renderer.bounds.Size}");
    }
}
