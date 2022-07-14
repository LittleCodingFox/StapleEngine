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
    public class Camera : ComponentPoolable
    {
        public CameraClearMode clearMode = CameraClearMode.SolidColor;

        public CameraType cameraType = CameraType.Perspective;

        public float fov = 90;

        public float zNear = 0.1f;

        public float zFar = 100;

        public float orthoSize = 5;

        public Vector4 viewport = new Vector4(0, 0, 1, 1);

        public ushort depth = 0;

        public Color32 clearColor;

        internal Transform ProjectionTransform()
        {
            var cameraMatrix = new Transform();

            switch(cameraType)
            {
                case CameraType.Orthographic:

                    {
                        var computedViewport = new Vector4(viewport.x * AppPlayer.ScreenWidth, viewport.y * AppPlayer.ScreenHeight,
                            viewport.z * AppPlayer.ScreenWidth, viewport.w * AppPlayer.ScreenHeight);

                        cameraMatrix.matrix = mat4.Ortho(computedViewport.x, computedViewport.z, computedViewport.w, computedViewport.y);
                    }

                    break;

                case CameraType.Perspective:

                    cameraMatrix.matrix = mat4.PerspectiveFov(fov, AppPlayer.ScreenWidth, AppPlayer.ScreenHeight, zNear, zFar);

                    break;
            }

            cameraMatrix.MarkCleared();

            return cameraMatrix;
        }

        internal void PrepareRender()
        {
            switch(clearMode)
            {
                case CameraClearMode.Depth:
                    bgfx.set_view_clear(depth, (ushort)(bgfx.ClearFlags.Depth), 0, 24, 0);

                    break;

                case CameraClearMode.None:
                    bgfx.set_view_clear(depth, (ushort)(bgfx.ClearFlags.None), 0, 24, 0);

                    break;

                case CameraClearMode.SolidColor:
                    bgfx.set_view_clear(depth, (ushort)(bgfx.ClearFlags.Color | bgfx.ClearFlags.Depth), clearColor.uintValue, 24, 0);

                    break;
            }

            bgfx.set_view_rect(depth, (ushort)viewport.x, (ushort)viewport.y, (ushort)(viewport.z * AppPlayer.ScreenWidth), (ushort)(viewport.w * AppPlayer.ScreenHeight));
        }
    }
}
