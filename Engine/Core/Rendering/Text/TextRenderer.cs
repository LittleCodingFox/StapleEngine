﻿using System;
using System.Collections.Generic;
using System.Numerics;
using System.Runtime.InteropServices;

namespace Staple.Internal;

internal class TextRenderer
{
    [StructLayout(LayoutKind.Sequential, Pack = 0)]
    private struct PosTexVertex
    {
        public Vector2 position;
        public Vector2 uv;
    }

    private TextFont defaultFont;

    private Lazy<VertexLayout> vertexLayout = new(() => new VertexLayoutBuilder()
        .Add(Bgfx.bgfx.Attrib.Position, 2, Bgfx.bgfx.AttribType.Float)
        .Add(Bgfx.bgfx.Attrib.TexCoord0, 2, Bgfx.bgfx.AttribType.Float)
        .Build());

    public void LoadDefaultFont()
    {
        var data = Convert.FromBase64String(FontData.IntelOneMonoRegular);

        defaultFont = TextFont.FromData(data);

        if(defaultFont != null)
        {
            Log.Debug($"[TextRenderer] Loaded default font");
        }
    }

    public Rect MeasureTextSimple(string str, TextParameters parameters)
    {
        //TODO: Update to new logic
        if (str == null || str.Length == 0)
        {
            return default;
        }

        var font = parameters.font.TryGetTarget(out var f) ? f : defaultFont;

        if (font == null)
        {
            return default;
        }

        var lineSpacing = font.LineSpacing(parameters);
        var spaceSize = font.GetGlyph(' ').xAdvance;

        var position = new Vector2Int(0, parameters.fontSize);

        var min = Vector2Int.Zero;
        var max = Vector2Int.Zero;

        var lines = str.Replace("\r", "").Split("\n".ToCharArray());
        var first = true;

        for(var i = 0; i < lines.Length; i++)
        {
            for(var j = 0; j < lines[i].Length; j++)
            {
                var glyph = font.GetGlyph(lines[i][j]);

                if(glyph == null)
                {
                    position.X += spaceSize;

                    continue;
                }

                if(first)
                {
                    first = false;

                    min.X = glyph.bounds.left;
                    min.Y = glyph.bounds.top;

                    max.X = glyph.bounds.right;
                    max.Y = position.Y - glyph.bounds.bottom;
                }

                switch (lines[i][j])
                {
                    case ' ':

                        position.X += spaceSize;

                        break;

                    default:

                        if(j > 0)
                        {
                            position.X += font.Kerning(lines[i][j - 1], lines[i][j], parameters);
                        }

                        position.X += glyph.xAdvance;

                        break;
                }
            }

            if(position.X < min.X)
            {
                min.X = position.X;
            }

            if(position.X > max.X)
            {
                max.X = position.X;
            }

            if (position.Y < min.Y)
            {
                min.Y = position.Y;
            }

            if (position.Y > max.Y)
            {
                max.Y = position.Y;
            }

            position.X = 0;
            position.Y += lineSpacing;
        }

        return new Rect(min.X, min.Y, max.X - min.X, max.Y - max.X);
    }

    public void FitTextAroundLength(string str, TextParameters parameters, float lengthInPixels, out int fontSize)
    {
        if(lengthInPixels <= 0)
        {
            fontSize = 0;

            return;
        }

        parameters = parameters.Clone();

        fontSize = parameters.fontSize;

        while(MeasureTextSimple(str, parameters.FontSize(fontSize)).right > lengthInPixels)
        {
            fontSize--;
        }
    }

    public Rect MeasureTextLines(IEnumerable<string> lines, TextParameters parameters)
    {
        //TODO: Update to new logic
        var outValue = new Rect();

        var additionalBottom = 0;

        var y = 0;
        var first = true;

        foreach(var line in lines)
        {
            var temp = MeasureTextSimple(line, parameters);

            if(first)
            {
                outValue = temp;
            }

            //Compensate for extra space due to lower letters like y and p
            if(temp.bottom > parameters.fontSize)
            {
                additionalBottom += temp.bottom - parameters.fontSize;
            }

            temp.top += y;
            temp.bottom += y;

            if(temp.left < outValue.left)
            {
                outValue.left = temp.left;
            }

            if(temp.top < outValue.top)
            {
                outValue.top = temp.top;
            }

            if(temp.right > outValue.right)
            {
                outValue.right = temp.right;
            }

            if(temp.bottom > outValue.bottom)
            {
                outValue.bottom = temp.bottom;
            }

            y += parameters.fontSize;
        }

        outValue.bottom += additionalBottom;

        return outValue;
    }

    public void DrawText(string text, Matrix4x4 transform, TextParameters parameters, Material material, float scale, ushort viewID)
    {
        if(text == null)
        {
            throw new ArgumentNullException("text");
        }

        if(parameters == null)
        {
            throw new ArgumentNullException("parameters");
        }

        if(material == null)
        {
            throw new ArgumentNullException("material");
        }

        var mesh = Mesh.Quad;

        if(mesh.changed)
        {
            mesh.UploadMeshData();
        }

        var actualParams = parameters.font != null && parameters.font.TryGetTarget(out _) ? parameters : parameters.Font(defaultFont);

        var font = parameters.font.TryGetTarget(out var textFont) ? textFont : defaultFont;

        if(font == null)
        {
            return;
        }

        var lineSpace = font.LineSpacing(actualParams);
        var spaceSize = font.GetGlyph(' ').xAdvance;

        var position = new Vector2(actualParams.position.X, actualParams.position.Y + actualParams.fontSize * scale);

        var initialPosition = position;

        var lines = text.Replace("\r", "").Split("\n".ToCharArray());

        foreach(var line in lines)
        {
            for(var j = 0; j < line.Length; j++)
            {
                switch(line[j])
                {
                    case ' ':

                        position.X += spaceSize * scale;

                        break;

                    default:

                        if(j > 0)
                        {
                            position.X += font.Kerning(line[j - 1], line[j], actualParams) * scale;
                        }

                        var glyph = font.GetGlyph(line[j]);

                        if(glyph != null)
                        {
                            var size = new Vector2(glyph.bounds.Width * scale, glyph.bounds.Height * scale);

                            var advance = (glyph.xAdvance + glyph.bounds.Width / 2) * scale;

                            var p = position + new Vector2(glyph.xOffset * scale, -glyph.yOffset * scale);

                            PosTexVertex[] vertices = [

                                new()
                                {
                                    position = p - size / 2,
                                    uv = new Vector2(glyph.bounds.left / (float)font.textureSize, glyph.bounds.bottom / (float)font.textureSize)
                                },
                                new()
                                {
                                    position = p + new Vector2(-size.X / 2, size.Y / 2),
                                    uv = new Vector2(glyph.bounds.left / (float)font.textureSize, glyph.bounds.top / (float)font.textureSize)
                                },
                                new()
                                {
                                    position = p + size / 2,
                                    uv = new Vector2(glyph.bounds.right / (float)font.textureSize, glyph.bounds.top / (float)font.textureSize)
                                },
                                new()
                                {
                                    position = p + size / 2,
                                    uv = new Vector2(glyph.bounds.right / (float)font.textureSize, glyph.bounds.top / (float)font.textureSize)
                                },
                                new()
                                {
                                    position = p + new Vector2(size.X / 2, -size.Y / 2),
                                    uv = new Vector2(glyph.bounds.right / (float)font.textureSize, glyph.bounds.bottom / (float)font.textureSize)
                                },
                                new()
                                {
                                    position = p - size / 2,
                                    uv = new Vector2(glyph.bounds.left / (float)font.textureSize, glyph.bounds.bottom / (float)font.textureSize)
                                },
                            ];

                            material.MainTexture = font.Texture;

                            var vertexBuffer = VertexBuffer.Create(vertices.AsSpan(), vertexLayout.Value,
                                VertexBuffer.TransientBufferHasSpace(vertices.Length, vertexLayout.Value));

                            if(vertexBuffer == null)
                            {
                                position.X += advance;

                                continue;
                            }

                            ushort[] indices = [0, 1, 2, 3, 4, 5];

                            var indexBuffer = IndexBuffer.Create(indices, RenderBufferFlags.Write, IndexBuffer.TransientBufferHasSpace(6, false));

                            if(indexBuffer == null)
                            {
                                vertexBuffer.Destroy();

                                position.X += advance;

                                continue;
                            }

                            Graphics.RenderGeometry(vertexBuffer, indexBuffer, 0, vertices.Length, 0, indices.Length,
                                material, transform, MeshTopology.Triangles, viewID);

                            position.X += advance;

                            vertexBuffer.Destroy();
                            indexBuffer.Destroy();
                        }
                        else
                        {
                            position.X += spaceSize * scale;
                        }

                        break;
                }
            }

            position.X = initialPosition.X;
            position.Y += lineSpace * scale;
        }
    }
}
