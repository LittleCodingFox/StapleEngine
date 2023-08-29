﻿using ImGuiNET;
using System.Numerics;

namespace Staple.Editor
{
    public static class EditorGUI
    {
        internal static ImGuiIOPtr io;

        public static bool Changed { get; internal set; }

        public static void Space()
        {
            ImGui.Spacing();
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

        public static int Dropdown(string label, string[] options, int current)
        {
            Changed |= ImGui.Combo(label, ref current, string.Join("\0", options));

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
    }
}
