using Artemis;
using Artemis.Attributes;
using Bgfx;
using GlmSharp;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Staple
{
    [ArtemisComponentPool(IsResizable = true)]
    public class SpriteRenderer : Renderer
    {
        struct PosTexCoordVertex
        {
            Vector3 position;
            Vector2 uv;

            public PosTexCoordVertex(Vector3 position, Vector2 uv)
            {
                this.position = position;
                this.uv = uv;
            }

            static unsafe bgfx.VertexLayout *layout;

            static PosTexCoordVertex()
            {
                unsafe
                {
                    bgfx.VertexLayout vertexLayout = new bgfx.VertexLayout();

                    layout = bgfx.vertex_layout_begin(&vertexLayout, bgfx.RendererType.Count);
                    layout = bgfx.vertex_layout_add(&vertexLayout, bgfx.Attrib.Position, 3, bgfx.AttribType.Float, false, false);
                    layout = bgfx.vertex_layout_add(&vertexLayout, bgfx.Attrib.TexCoord0, 2, bgfx.AttribType.Float, false, false);

                    bgfx.vertex_layout_end(layout);
                }
            }
        }

        static PosTexCoordVertex[] vertices = new PosTexCoordVertex[]
        {
            new PosTexCoordVertex(Vector3.zero, Vector2.zero),
            new PosTexCoordVertex(new Vector3(0, 1, 0), new Vector2(0, 1)),
            new PosTexCoordVertex(Vector3.one, Vector2.one),
            new PosTexCoordVertex(new Vector3(1, 0, 0), new Vector2(1, 0)),
        };

        static ushort[] indices = new ushort[]
        {
            0, 1, 2, 2, 3, 0
        };

        private static bgfx.VertexBufferHandle vertexBuffer;
        private static bgfx.IndexBufferHandle indexBuffer;

        public SpriteRenderer()
        {
            /*
            if(vertexBuffer.Valid == false)
            {
                unsafe
                {
                    var memory = bgfx.alloc((uint)System.Runtime.InteropServices.Marshal.SizeOf(vertices));

                    vertexBuffer = bgfx.create_vertex_buffer()

                }
            }
            */
        }
    }
}
