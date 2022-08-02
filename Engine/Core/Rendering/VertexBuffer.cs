﻿using Bgfx;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Runtime.InteropServices;
using System.Text;
using System.Threading.Tasks;

namespace Staple
{
    internal class VertexBuffer
    {
        public unsafe bgfx.Memory *data;
        public VertexLayout layout;
        public bgfx.VertexBufferHandle handle;
        public readonly int length;

        public unsafe VertexBuffer(bgfx.Memory* data, VertexLayout layout, bgfx.VertexBufferHandle handle, int length)
        {
            this.data = data;
            this.layout = layout;
            this.handle = handle;
            this.length = length;
        }

        public void SetActive(byte stream, uint start, uint count)
        {
            bgfx.set_vertex_buffer(stream, handle, start, count);
        }

        public static VertexBuffer Create<T>(T[] data, VertexLayout layout)
        {
            var size = Marshal.SizeOf(typeof(T));

            byte[] buffer = new byte[size * data.Length];

            IntPtr ptr = IntPtr.Zero;

            unsafe
            {
                bgfx.Memory* outData;

                try
                {
                    ptr = Marshal.AllocHGlobal(size);

                    for(var i = 0; i < data.Length; i++)
                    {
                        Marshal.StructureToPtr(data[i], ptr, false);
                        Marshal.Copy(ptr, buffer, i * size, size);
                    }
                }
                finally
                {
                    Marshal.FreeHGlobal(ptr);
                }

                fixed(void * dataPtr = buffer)
                {
                    outData = bgfx.copy(dataPtr, (uint)(size * data.Length));
                }

                fixed(bgfx.VertexLayout *vertexLayout = &layout.layout)
                {
                    var handle = bgfx.create_vertex_buffer(outData, vertexLayout, (ushort)RenderBufferFlags.None);

                    return new VertexBuffer(outData, layout, handle, data.Length);
                }
            }
        }
    }
}
