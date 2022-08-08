﻿using Staple.Internal;
using System;
using System.Collections.Generic;

namespace Baker
{
    [Serializable]
    internal class UnprocessedShader
    {
        public ShaderType type;
        public List<ShaderParameter> parameters = new List<ShaderParameter>();
        public ShaderPiece vertex;
        public ShaderPiece fragment;
        public ShaderPiece compute;
    }
}