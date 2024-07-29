﻿using Staple.Internal;
using System;
using System.Collections.Generic;

namespace Staple.Editor;

[CustomEditor(typeof(MaterialMetadata))]
internal class MaterialEditor : AssetEditor
{
    private Dictionary<string, Shader> cachedShaders = new();
    private Dictionary<string, Texture> cachedTextures = new();
    private Shader activeShader;

    public override bool DrawProperty(Type fieldType, string name, Func<object> getter, Action<object> setter, Func<Type, Attribute> attributes)
    {
        var material = target as MaterialMetadata;

        switch(name)
        {
            case nameof(MaterialMetadata.parameters):

                foreach (var parameter in material.parameters)
                {
                    var label = parameter.Key.ExpandCamelCaseName();

                    switch (parameter.Value.type)
                    {
                        case MaterialParameterType.Texture:

                            {
                                var key = parameter.Value.textureValue ?? "";

                                if(cachedTextures.ContainsKey(key) == false)
                                {
                                    var t = ResourceManager.instance.LoadTexture(key);

                                    if (t != null)
                                    {
                                        cachedTextures.AddOrSetKey(key, t);
                                    }
                                }

                                cachedTextures.TryGetValue(key, out var texture);

                                var newValue = EditorGUI.ObjectPicker(typeof(Texture), label, texture);

                                if(newValue != texture)
                                {
                                    if(newValue is Texture t)
                                    {
                                        var guid = t.Guid;

                                        if(guid != null)
                                        {
                                            parameter.Value.textureValue = guid;

                                            cachedTextures.AddOrSetKey(guid, t);
                                        }
                                    }
                                    else
                                    {
                                        parameter.Value.textureValue = null;
                                    }
                                }
                            }

                            break;

                        case MaterialParameterType.Vector2:

                            {
                                if (parameter.Value.vec2Value == null)
                                {
                                    parameter.Value.vec2Value = new();
                                }

                                var current = parameter.Value.vec2Value.ToVector2();

                                var newValue = EditorGUI.Vector2Field(label, parameter.Key, current);

                                if (newValue != current)
                                {
                                    parameter.Value.vec2Value.x = newValue.X;
                                    parameter.Value.vec2Value.y = newValue.Y;
                                }
                            }

                            break;

                        case MaterialParameterType.Vector3:

                            {
                                if (parameter.Value.vec3Value == null)
                                {
                                    parameter.Value.vec3Value = new();
                                }

                                var current = parameter.Value.vec3Value.ToVector3();

                                var newValue = EditorGUI.Vector3Field(label, parameter.Key, current);

                                if (newValue != current)
                                {
                                    parameter.Value.vec3Value.x = newValue.X;
                                    parameter.Value.vec3Value.y = newValue.Y;
                                    parameter.Value.vec3Value.z = newValue.Z;
                                }
                            }

                            break;

                        case MaterialParameterType.Vector4:

                            {
                                if (parameter.Value.vec4Value == null)
                                {
                                    parameter.Value.vec4Value = new();
                                }

                                var current = parameter.Value.vec4Value.ToVector4();

                                var newValue = EditorGUI.Vector4Field(label, parameter.Key, current);

                                if (newValue != current)
                                {
                                    parameter.Value.vec4Value.x = newValue.X;
                                    parameter.Value.vec4Value.y = newValue.Y;
                                    parameter.Value.vec4Value.z = newValue.Z;
                                    parameter.Value.vec4Value.w = newValue.W;
                                }
                            }

                            break;

                        case MaterialParameterType.Color:

                            parameter.Value.colorValue = EditorGUI.ColorField(label, parameter.Key, parameter.Value.colorValue);

                            break;

                        case MaterialParameterType.Float:

                            parameter.Value.floatValue = EditorGUI.FloatField(label, parameter.Key, parameter.Value.floatValue);

                            break;

                        case MaterialParameterType.TextureWrap:

                            parameter.Value.textureWrapValue = EditorGUI.EnumDropdown(label, parameter.Key, parameter.Value.textureWrapValue);

                            break;
                    }
                }

                return true;

            case nameof(MaterialMetadata.shader):

                {
                    var key = material.shader;
                    Shader shader = null;

                    if(key != null)
                    {
                        if (cachedShaders.TryGetValue(key, out shader) == false)
                        {
                            if(key.Length > 0)
                            {
                                shader = ResourceManager.instance.LoadShader(material.shader);

                                cachedShaders.AddOrSetKey(key, shader);

                                if(shader != null)
                                {
                                    material.shader = shader.Guid;
                                }

                                activeShader = shader;
                            }
                        }
                    }

                    var newValue = EditorGUI.ObjectPicker(typeof(Shader), "Shader: ", shader);

                    if (newValue != shader)
                    {
                        if (newValue is Shader s)
                        {
                            cachedShaders.AddOrSetKey(s.metadata.guid, s);

                            material.shader = s.Guid;

                            activeShader = s;
                        }
                        else
                        {
                            material.shader = "";

                            activeShader = null;
                        }
                    }
                }

                return true;

            case nameof(MaterialMetadata.enabledShaderVariants):

                if(activeShader != null && activeShader.metadata.variants.Count > 0)
                {
                    EditorGUI.Label("Enabled Variants");

                    EditorGUI.SameLine();

                    EditorGUI.Button("+", "MaterialVariantAdd", () =>
                    {
                        material.enabledShaderVariants.Add("");
                    });

                    var skip = false;

                    for(var i = 0; i < material.enabledShaderVariants.Count; i++)
                    {
                        var currentIndex = activeShader.metadata.variants.IndexOf(material.enabledShaderVariants[i]);
                        var index = EditorGUI.Dropdown("", $"MaterialVariant{i}", activeShader.metadata.variants.ToArray(), currentIndex);

                        if(currentIndex != index && index >= 0)
                        {
                            material.enabledShaderVariants[i] = activeShader.metadata.variants[index];
                        }

                        EditorGUI.SameLine();

                        EditorGUI.Button("-", $"MaterialVariantRemove{i}", () =>
                        {
                            skip = true;

                            material.enabledShaderVariants.RemoveAt(i);
                        });

                        if(skip)
                        {
                            break;
                        }
                    }
                }

                return true;
        }

        return false;
    }

    public override void OnInspectorGUI()
    {
        base.OnInspectorGUI();

        ShowAssetUI(null);
    }
}
