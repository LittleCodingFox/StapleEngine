﻿using MessagePack;
using System;
using System.Collections.Generic;
using System.Numerics;

namespace Staple.Internal
{
    public enum MaterialParameterType
    {
        Vector2,
        Vector3,
        Vector4,
        Texture,
        Color,
    }

    [Serializable]
    [MessagePackObject]
    public class Vector2Holder
    {
        [Key(0)]
        public float x;

        [Key(1)]
        public float y;
    }

    [Serializable]
    [MessagePackObject]
    public class Vector3Holder
    {
        [Key(0)]
        public float x;

        [Key(1)]
        public float y;

        [Key(2)]
        public float z;
    }

    [Serializable]
    [MessagePackObject]
    public class Vector4Holder
    {
        [Key(0)]
        public float x;

        [Key(1)]
        public float y;

        [Key(2)]
        public float z;

        [Key(3)]
        public float w;
    }

    [Serializable]
    [MessagePackObject]
    public class MaterialParameter
    {
        [Key(0)]
        public MaterialParameterType type;

        [Key(1)]
        public Vector2Holder vec2Value;

        [Key(2)]
        public Vector3Holder vec3Value;

        [Key(3)]
        public Vector4Holder vec4Value;

        [Key(4)]
        public string textureValue;

        [Key(5)]
        public Color32 colorValue;
    }

    [Serializable]
    [MessagePackObject]
    public class MaterialMetadata
    {
        [Key(0)]
        public string shaderPath;

        [Key(1)]
        public Dictionary<string, MaterialParameter> parameters = new Dictionary<string, MaterialParameter>();
    }
}
