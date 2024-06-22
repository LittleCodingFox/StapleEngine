﻿using Bgfx;
using System;

namespace Staple.Internal;

internal class PixelTextureCreateMethod(string path, byte[] data, ushort width, ushort height, TextureMetadata metadata, bgfx.TextureFormat format, TextureFlags flags) : ITextureCreateMethod
{
    public string path = path;
    public byte[] data = data;
    public ushort width = width;
    public ushort height = height;
    public TextureMetadata metadata = metadata;
    public bgfx.TextureFormat format = format;
    public TextureFlags flags = flags;

    public bool Create(Texture texture)
    {
        unsafe
        {
            Texture.ProcessFlags(ref flags, metadata);

            bgfx.Memory* memory = bgfx.alloc((uint)data.Length);

            var source = new Span<byte>(data);

            var target = new Span<byte>(memory->data, data.Length);

            source.CopyTo(target);

            texture.handle = bgfx.create_texture_2d(width, height, metadata.useMipmaps, 1, format, (ulong)flags, memory);

            if (texture.handle.Valid == false)
            {
                return false;
            }

            texture.guid = path;
            texture.metadata = metadata;
            texture.info = new bgfx.TextureInfo()
            {
                bitsPerPixel = 24,
                format = format,
                height = height,
                width = width,
                numLayers = 1,
            };

            return true;
        }
    }
}
