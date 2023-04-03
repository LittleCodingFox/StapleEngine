﻿using Bgfx;
using Staple.Internal;
using System;
using System.Collections.Generic;
using System.Numerics;
using System.Runtime.InteropServices;

namespace Staple
{
    /// <summary>
    /// Sprite Render System
    /// </summary>
    internal class SpriteRenderSystem : IRenderSystem
    {
        /// <summary>
        /// Contains render information for a sprite
        /// </summary>
        private class SpriteRenderInfo
        {
            public Matrix4x4 transform;
            public Material material;
            public Color color;
            public Texture texture;
            public ushort viewID;
        }

        private readonly List<SpriteRenderInfo> sprites = new();

        private Mesh spriteMesh;

        public void Destroy()
        {
            spriteMesh?.Destroy();
        }

        public Type RelatedComponent()
        {
            return typeof(Sprite);
        }

        public void Prepare()
        {
            sprites.Clear();
        }

        public void Preprocess(Entity entity, Transform transform, IComponent renderer)
        {
            var r = renderer as Sprite;

            //We recalculate the bounds of this sprite
            if(r.texture != null && r.material != null && r.material.shader != null)
            {
                r.localBounds = new AABB(Vector3.Zero, new Vector3(r.texture.SpriteWidth, r.texture.SpriteHeight, 0));

                r.bounds = new AABB(transform.Position, new Vector3(r.texture.SpriteWidth, r.texture.SpriteHeight, 0));
            }
        }

        public void Process(Entity entity, Transform transform, IComponent renderer, ushort viewId)
        {
            var r = renderer as Sprite;

            if(r.material == null || r.material.shader == null || r.material.Disposed || r.material.shader.Disposed)
            {
                return;
            }

            var scale = Vector3.Zero;

            if (r.texture != null)
            {
                scale.X = r.texture.SpriteWidth;
                scale.Y = r.texture.SpriteHeight;
            }

            var matrix = Matrix4x4.CreateScale(scale) * transform.Matrix;

            sprites.Add(new SpriteRenderInfo()
            {
                color = r.color,
                material = r.material,
                texture = r.texture,
                transform = matrix,
                viewID = viewId
            });
        }

        public void Submit()
        {
            if (spriteMesh == null)
            {
                spriteMesh = ResourceManager.instance.LoadMesh("Internal/Quad");
            }

            if(sprites.Count == 0)
            {
                return;
            }

            spriteMesh?.SetActive();

            bgfx.StateFlags state = bgfx.StateFlags.WriteRgb | bgfx.StateFlags.WriteA | bgfx.StateFlags.DepthTestGequal | bgfx.StateFlags.PtTristrip;

            for (var i = 0; i < sprites.Count; i++)
            {
                var s = sprites[i];

                unsafe
                {
                    var transform = s.transform;

                    _ = bgfx.set_transform(&transform, 1);
                }

                bgfx.set_state((ulong)state, 0);

                s.material.shader.SetColor(Material.MainColorProperty, s.color);
                s.material.shader.SetTexture(Material.MainTextureProperty, s.texture);

                var discardFlags = i == sprites.Count - 1 ? bgfx.DiscardFlags.All : bgfx.DiscardFlags.Transform | bgfx.DiscardFlags.Bindings | bgfx.DiscardFlags.State;

                bgfx.submit(s.viewID, s.material.shader.program, 0, (byte)discardFlags);
            }
        }
    }
}
