﻿using ImGuiNET;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Numerics;

namespace Staple.Editor
{
    public static class EditorGUI
    {
        internal static ImGuiIOPtr io;
        internal static StapleEditor editor;

        internal static Dictionary<string, object> pendingObjectPickers = new();

        private static Dictionary<string, object> cachedEnumValues = new();

        public static bool Changed { get; internal set; }

        public static void SameLine()
        {
            ImGui.SameLine();
        }

        public static void Space()
        {
            ImGui.Spacing();
        }

        public static void Label(string text)
        {
            ImGui.Text(text);
        }

        public static bool Button(string label)
        {
            return ImGui.Button(label);
        }

        public static bool ButtonDisabled(string label)
        {
            ImGui.BeginDisabled();

            var result = ImGui.Button(label);

            ImGui.EndDisabled();

            return result;
        }

        public static int IntField(string label, int value)
        {
            Changed |= ImGui.InputInt(label, ref value);

            return value;
        }

        public static float FloatField(string label, float value)
        {
            Changed |= ImGui.InputFloat(label, ref value);

            return value;
        }

        public static Vector2 Vector2Field(string label, Vector2 value)
        {
            Changed |= ImGui.InputFloat2(label, ref value);

            return value;
        }

        public static Vector2Int Vector2IntField(string label, Vector2Int value)
        {
            var values = new int[] { value.X, value.Y };

            Changed |= ImGui.InputInt2(label, ref values[0]);

            return new Vector2Int(values[0], values[1]);
        }

        public static Vector3 Vector3Field(string label, Vector3 value)
        {
            Changed |= ImGui.InputFloat3(label, ref value);

            return value;
        }

        public static Vector4 Vector4Field(string label, Vector4 value)
        {
            Changed |= ImGui.InputFloat4(label, ref value);

            return value;
        }

        public static T EnumDropdown<T>(string label, T value) where T: struct, Enum
        {
            if(cachedEnumValues.TryGetValue(typeof(T).FullName, out var v) == false)
            {
                v = Enum.GetValues<T>()
                    .ToList();

                cachedEnumValues.Add(typeof(T).FullName, v);
            }

            var values = v as List<T>;

            var current = values.IndexOf(value);

            var valueStrings = values
                .Select(x => x.ToString())
                .ToArray();

            var newValue = values[Dropdown(label, valueStrings, current)];

            return newValue;
        }

        public static int Dropdown(string label, string[] options, int current)
        {
            Changed |= ImGui.Combo(label, ref current, $"{string.Join("\0", options)}\0");

            return current;
        }

        public static string TextField(string label, string value, int maxLength = 1000)
        {
            Changed |= ImGui.InputText(label, ref value, (uint)maxLength);

            return value;
        }

        public static string TextFieldMultiline(string label, string text, Vector2 size, int maxLength = 1000)
        {
            Changed |= ImGui.InputTextMultiline(label, ref text, (uint)maxLength, size);

            return text;
        }

        public static bool Toggle(string label, bool value)
        {
            Changed |= ImGui.Checkbox(label, ref value);

            return value;
        }

        public static Color ColorField(string label, Color value)
        {
            var v = new Vector4(value.r, value.g, value.b, value.a);

            Changed |= ImGui.ColorEdit4(label, ref v);

            return new Color(v.X, v.Y, v.Z, v.W);
        }

        public static Color ColorPicker(string label, Color value)
        {
            var v = new Vector4(value.r, value.g, value.b, value.a);

            Changed |= ImGui.ColorPicker4(label, ref v);

            return new Color(v.X, v.Y, v.Z, v.W);
        }

        public static object ObjectPicker(System.Type type, string name, object current)
        {
            ImGui.Text(name);

            ImGui.SameLine();

            if((type == typeof(Texture) || type.IsSubclassOf(typeof(Texture))) && current is Texture t && t.Disposed == false)
            {
                ImGui.Image(ImGuiProxy.GetImGuiTexture(t), new Vector2(32, 32));
            }
            else
            {
                ImGui.Text(current?.ToString() ?? "(None)");
            }

            ImGui.SameLine();

            var key = $"{type.FullName}{name}{current}";

            if (ImGui.SmallButton("O"))
            {
                editor.showingAssetPicker = true;
                editor.assetPickerSearch = "";
                editor.assetPickerType = type;
                editor.assetPickerKey = key;
            }

            if(pendingObjectPickers.ContainsKey(key) == false)
            {
                pendingObjectPickers.Add(key, current);
            }
            else if (pendingObjectPickers[key] != current)
            {
                var newValue = pendingObjectPickers[key];

                pendingObjectPickers.Remove(key);

                return newValue;
            }

            return current;
        }

        public static void Texture(Texture texture, Vector2 size)
        {
            if(texture == null)
            {
                return;
            }

            ImGui.Image(ImGuiProxy.GetImGuiTexture(texture), size);
        }
    }
}
