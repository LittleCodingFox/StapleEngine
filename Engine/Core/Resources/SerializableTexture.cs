﻿using MessagePack;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Staple.Internal
{
    [Serializable]
    [MessagePackObject]
    public class SerializableTextureHeader
    {
        [IgnoreMember]
        public readonly static char[] ValidHeader = new char[]
        {
            'S', 'T', 'E', 'X'
        };

        [IgnoreMember]
        public const byte ValidVersion = 1;

        [Key(0)]
        public char[] header = ValidHeader;

        [Key(1)]
        public byte version = ValidVersion;
    }

    [Serializable]
    [MessagePackObject]
    public class SerializableTexture
    {
        [Key(0)]
        public TextureMetadata metadata;

        [Key(1)]
        public byte[] data;
    }
}
