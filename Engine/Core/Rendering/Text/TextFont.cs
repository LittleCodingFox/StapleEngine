﻿using System;
using System.Collections.Generic;

namespace Staple.Internal;

internal class TextFont : IDisposable
{
    public class FontAtlasInfo
    {
        public int fontSize;
        public FontCharacterSet ranges;
        public Texture atlas;
        public int lineSpacing;

        public Dictionary<int, Glyph> glyphs = new();

        public Dictionary<Vector2Int, int> kerning = new();
    }

    internal static readonly Dictionary<FontCharacterSet, (int, int)> characterRanges = new()
    {
        { FontCharacterSet.BasicLatin, (0x0020, 0x007F) },
        { FontCharacterSet.Latin1Supplement, (0x00A0, 0x00FF) },
        { FontCharacterSet.LatinExtendedA, (0x0100, 0x017F) },
        { FontCharacterSet.LatinExtendedB, (0x0180, 0x024F) },
        { FontCharacterSet.Cyrillic, (0x0400, 0x04FF) },
        { FontCharacterSet.CyrillicSupplement, (0x0500, 0x052F) },
        { FontCharacterSet.Hiragana, (0x3040, 0x309F) },
        { FontCharacterSet.Katakana, (0x30A0, 0x30FF) },
        { FontCharacterSet.Greek, (0x0370, 0x03FF) },
        { FontCharacterSet.CjkSymbolsAndPunctuation, (0x3000, 0x303F) },
        { FontCharacterSet.CjkUnifiedIdeographs, (0x4E00, 0x9FFF) },
        { FontCharacterSet.HangulCompatibilityJamo, (0x3130, 0x318F) },
        { FontCharacterSet.HangulSyllables, (0xAC00, 0xD7AF) },
    };

    internal const FontCharacterSet AllCharacterSets = FontCharacterSet.BasicLatin |
        FontCharacterSet.CjkSymbolsAndPunctuation |
        FontCharacterSet.CjkUnifiedIdeographs |
        FontCharacterSet.Cyrillic |
        FontCharacterSet.CyrillicSupplement |
        FontCharacterSet.Greek |
        FontCharacterSet.HangulCompatibilityJamo |
        FontCharacterSet.HangulSyllables |
        FontCharacterSet.Hiragana |
        FontCharacterSet.Katakana |
        FontCharacterSet.Latin1Supplement |
        FontCharacterSet.LatinExtendedA |
        FontCharacterSet.LatinExtendedB;

    internal int textureSize;

    internal ITextFontSource fontSource;

    internal FontCharacterSet includedRanges;

    internal Dictionary<string, FontAtlasInfo> atlas = new();

    internal string guid;

    private bool useAntiAliasing = true;

    private HashSet<string> failedLoads = new();

    public int FontSize
    {
        get => fontSource.FontSize;

        set
        {
            if(value <= 0)
            {
                return;
            }

            fontSource.FontSize = value;

            GenerateTextureAtlas();
        }
    }

    public Color TextColor { get; set; } = Color.White;

    public Color SecondaryTextColor { get; set; } = Color.White;

    public Color BorderColor { get; set; } = Color.White;

    public int BorderSize { get; set; }

    internal string Key
    {
        get
        {
            return $"{FontSize}:{TextColor.r},{TextColor.g},{TextColor.b},{TextColor.a}:" +
                $"{SecondaryTextColor.r},{SecondaryTextColor.g},{SecondaryTextColor.b},{SecondaryTextColor.a}:" + 
                $"{BorderSize}:{BorderColor.r},{BorderColor.g},{BorderColor.b},{BorderColor.a}";
        }
    }

    public Texture Texture => atlas.TryGetValue(Key, out var info) ? info.atlas : null;

    public bool MakePixelData(int fontSize, int textureSize, out byte[] bitmapData, out int lineSpacing, out Dictionary<int, Glyph> glyphs)
    {
        if (fontSize <= 0 || textureSize <= 0)
        {
            bitmapData = default;
            glyphs = default;
            lineSpacing = default;

            return false;
        }

        glyphs = new Dictionary<int, Glyph>();

        var values = Enum.GetValues<FontCharacterSet>();

        foreach (var value in values)
        {
            if (includedRanges.HasFlag(value) == false ||
                characterRanges.TryGetValue(value, out var range) == false ||
                range.Item1 > range.Item2)
            {
                continue;
            }

            for(var c = range.Item1; c <= range.Item2; c++)
            {
                var glyph = fontSource.LoadGlyph((uint)c, fontSize, TextColor, SecondaryTextColor, BorderSize, BorderColor);

                if(glyph == null || glyph.bitmap == null)
                {
                    continue;
                }

                glyphs.Add(c, glyph);
            }
        }

        var bitmaps = new RawTextureData[glyphs.Count];

        var counter = 0;

        foreach(var pair in glyphs)
        {
            bitmaps[counter++] = new()
            {
                colorComponents = StandardTextureColorComponents.RGBA,
                data = pair.Value.bitmap,
                height = pair.Value.bounds.Height,
                width = pair.Value.bounds.Width,
            };
        }

        if(Texture.PackTextures(bitmaps, textureSize, textureSize, textureSize, 1, out var rects, out var main))
        {
            bitmapData = main.data;
            lineSpacing = fontSize;

            counter = 0;

            foreach(var pair in glyphs)
            {
                var rect = rects[counter++];

                pair.Value.uvBounds = new RectFloat(rect.left / (float)textureSize,
                    rect.right / (float)textureSize,
                    rect.top / (float)textureSize,
                    rect.bottom / (float)textureSize);
            }

            return true;
        }

        bitmapData = null;
        lineSpacing = default;

        return false;
    }

    public void GenerateTextureAtlas()
    {
        var key = Key;

        if(atlas.ContainsKey(key) || failedLoads.Contains(key))
        {
            return;
        }

        if(MakePixelData(FontSize, textureSize, out var rgbBitmap, out var lineGap, out var glyphs) == false)
        {
            if(Platform.IsPlaying)
            {
                failedLoads.Add(key);

                Log.Debug($"[TextFont] Failed to make font atlas for {guid} with font size {FontSize} and texture size {textureSize} due to texture being too small");
            }

            return;
        }

        var texture = Texture.CreatePixels("", rgbBitmap, (ushort)textureSize, (ushort)textureSize, new()
        {
            filter = useAntiAliasing ? TextureFilter.Linear : TextureFilter.Point,
            type = TextureType.Texture,
            useMipmaps = false,
        }, Bgfx.bgfx.TextureFormat.RGBA8);

        if(texture == null)
        {
            return;
        }

        atlas.Add(key, new()
        {
            atlas = texture,
            fontSize = FontSize,
            glyphs = glyphs,
            lineSpacing = lineGap,
            ranges = includedRanges,
        });
    }

    public void Clear()
    {
        foreach(var pair in atlas)
        {
            pair.Value.atlas.Destroy();
        }

        atlas.Clear();
    }

    public int LineSpacing(TextParameters parameters)
    {
        FontSize = parameters.fontSize;

        if (atlas.TryGetValue(Key, out var info) == false)
        {
            return FontSize;
        }

        return info.lineSpacing;
    }

    public int Kerning(char from, char to, TextParameters parameters)
    {
        if(atlas.TryGetValue(Key, out var info) == false ||
            info.kerning.TryGetValue(new(from, to), out var kerning) == false)
        {
            return 0;
        }

        return kerning;
    }

    public void Dispose()
    {
        Clear();
    }

    public Glyph GetGlyph(int codepoint)
    {
        return atlas.TryGetValue(Key, out var info) && info.glyphs.TryGetValue(codepoint, out var glyph) ? glyph : new();
    }

    public static TextFont FromData(byte[] data, string guid, bool useAntiAliasing, int textureSize = 1024, FontCharacterSet ranges = AllCharacterSets)
    {
        var fontSource = new FreeTypeFontSource();

        if(fontSource.Initialize(data) == false)
        {
            return null;
        }

        var outValue = new TextFont()
        {
            guid = guid,
            useAntiAliasing = useAntiAliasing,
            fontSource = fontSource,
            textureSize = textureSize,
            includedRanges = ranges,
        };

        outValue.FontSize = 14;

        return outValue;
    }
}
