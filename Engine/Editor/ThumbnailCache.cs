﻿using Staple.Internal;
using System.Collections.Generic;
using System.IO;

namespace Staple.Editor
{
    internal class ThumbnailCache
    {
        private static readonly Dictionary<string, Texture> cachedThumbnails = new();
        private static readonly List<Texture> pendingDestructionTextures = new();

        internal static string basePath;

        public static Texture GetThumbnail(string path)
        {
            if(cachedThumbnails.TryGetValue(path, out var texture))
            {
                return texture;
            }

            var platform = Platform.CurrentPlatform;

            if(platform.HasValue == false)
            {
                return null;
            }

            var cachePath = path;

            var index = path.IndexOf("Assets");

            if(index >= 0)
            {
                cachePath = Path.Combine(basePath, "Cache", "Staging", platform.Value.ToString(), path.Substring(index + "Assets\\".Length));
            }

            try
            {
                texture = ResourceManager.instance.LoadTexture(cachePath);
            }
            catch(System.Exception)
            {
                return null;
            }

            if(texture != null)
            {
                cachedThumbnails.Add(path, texture);
            }

            return texture;
        }

        public static void OnFrameStart()
        {
            foreach(var texture in pendingDestructionTextures)
            {
                texture.Destroy();
            }

            pendingDestructionTextures.Clear();
        }

        public static void Clear()
        {
            foreach(var pair in cachedThumbnails)
            {
                pendingDestructionTextures.Add(pair.Value);
            }

            cachedThumbnails.Clear();
        }

        public static void ClearSingle(string path)
        {
            if(cachedThumbnails.TryGetValue(path, out var texture))
            {
                cachedThumbnails.Remove(path);

                pendingDestructionTextures.Add(texture);
            }
        }
    }
}
