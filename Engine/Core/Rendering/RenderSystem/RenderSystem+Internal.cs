﻿using Bgfx;
using System;
using System.Collections.Generic;
using System.Numerics;
using System.Runtime.CompilerServices;

namespace Staple.Internal;

public partial class RenderSystem
{
    #region Helpers
    /// <summary>
    /// Calculates the blending function for blending flags
    /// </summary>
    /// <param name="source">The blending source flag</param>
    /// <param name="destination">The blending destination flag</param>
    /// <returns>The combined function</returns>
    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    internal static ulong BlendFunction(bgfx.StateFlags source, bgfx.StateFlags destination)
    {
        return BlendFunction(source, destination, source, destination);
    }

    /// <summary>
    /// Calculates the blending function for blending flags
    /// </summary>
    /// <param name="sourceColor">The source color flag</param>
    /// <param name="destinationColor">The destination color flag</param>
    /// <param name="sourceAlpha">The source alpha blending flag</param>
    /// <param name="destinationAlpha">The destination alpha blending flag</param>
    /// <returns>The combined function</returns>
    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    internal static ulong BlendFunction(bgfx.StateFlags sourceColor, bgfx.StateFlags destinationColor, bgfx.StateFlags sourceAlpha, bgfx.StateFlags destinationAlpha)
    {
        return (ulong)sourceColor | (ulong)destinationColor << 4 | ((ulong)sourceAlpha | (ulong)destinationAlpha << 4) << 8;
    }

    /// <summary>
    /// Checks if we have an available transient buffer
    /// </summary>
    /// <param name="numVertices">The amount of vertices to check</param>
    /// <param name="layout">The vertex layout to use</param>
    /// <param name="numIndices">The amount of indices to check</param>
    /// <returns></returns>
    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    internal static bool CheckAvailableTransientBuffers(uint numVertices, bgfx.VertexLayout layout, uint numIndices)
    {
        unsafe
        {
            return numVertices == bgfx.get_avail_transient_vertex_buffer(numVertices, &layout)
                && (0 == numIndices || numIndices == bgfx.get_avail_transient_index_buffer(numIndices, false));
        }
    }

    /// <summary>
    /// Gets the reset flags for specific video flags
    /// </summary>
    /// <param name="videoFlags">The video flags to use</param>
    /// <returns>The BGFX Reset Flags</returns>
    internal static bgfx.ResetFlags ResetFlags(VideoFlags videoFlags)
    {
        var resetFlags = bgfx.ResetFlags.FlushAfterRender | bgfx.ResetFlags.FlipAfterRender; //bgfx.ResetFlags.SrgbBackbuffer;

        if (videoFlags.HasFlag(VideoFlags.Vsync))
        {
            resetFlags |= bgfx.ResetFlags.Vsync;
        }

        if (videoFlags.HasFlag(VideoFlags.MSAAX2))
        {
            resetFlags |= bgfx.ResetFlags.MsaaX2;
        }
        else if (videoFlags.HasFlag(VideoFlags.MSAAX4))
        {
            resetFlags |= bgfx.ResetFlags.MsaaX4;
        }
        else if (videoFlags.HasFlag(VideoFlags.MSAAX8))
        {
            resetFlags |= bgfx.ResetFlags.MsaaX8;
        }
        else if (videoFlags.HasFlag(VideoFlags.MSAAX16))
        {
            resetFlags |= bgfx.ResetFlags.MsaaX16;
        }

        if (videoFlags.HasFlag(VideoFlags.HDR10))
        {
            resetFlags |= bgfx.ResetFlags.Hdr10;
        }

        if (videoFlags.HasFlag(VideoFlags.HiDPI))
        {
            resetFlags |= bgfx.ResetFlags.Hidpi;
        }

        return resetFlags;
    }
    #endregion

    #region Frame Callbacks
    internal void QueueFrameCallback(uint frame, Action callback)
    {
        lock (lockObject)
        {
            if (queuedFrameCallbacks.TryGetValue(frame, out var list) == false)
            {
                list = new();

                queuedFrameCallbacks.Add(frame, list);
            }

            list.Add(callback);
        }
    }

    internal void OnFrame(uint frame)
    {
        List<Action> callbacks = null;

        lock (lockObject)
        {
            if (queuedFrameCallbacks.TryGetValue(frame, out callbacks) == false)
            {
                return;
            }
        }

        foreach (var item in callbacks)
        {
            try
            {
                item?.Invoke();
            }
            catch (Exception e)
            {
                Log.Debug($"[RenderSystem] While executing a frame callback at frame {frame}: {e}");
            }
        }

        lock (lockObject)
        {
            queuedFrameCallbacks.Remove(frame);
        }
    }
    #endregion

    #region Lifecycle
    public void Startup()
    {
        RegisterSystem(new SpriteRenderSystem());
        RegisterSystem(new SkinnedAnimationPoserSystem());
        RegisterSystem(new SkinnedMeshAnimatorSystem());
        RegisterSystem(new SkinnedMeshRenderSystem());
        RegisterSystem(new MeshRenderSystem());
        RegisterSystem(new TextRenderSystem());

        Time.OnAccumulatorFinished += () =>
        {
            needsDrawCalls = true;
        };
    }

    public void Shutdown()
    {
        foreach (var system in renderSystems)
        {
            system.Destroy();
        }
    }

    public void Update()
    {
        if (World.Current == null)
        {
            return;
        }

        CurrentViewID = 0;

        if (UseDrawcallInterpolator)
        {
            UpdateAccumulator();
        }
        else
        {
            UpdateStandard();
        }
    }
    #endregion

    #region Render Modes
    public void RenderStandard(Entity cameraEntity, Camera camera, Transform cameraTransform, ushort viewID)
    {
        var systems = new List<IRenderSystem>();

        lock(lockObject)
        {
            systems.AddRange(renderSystems);
        }

        foreach (var system in systems)
        {
            system.Prepare();
        }

        PrepareCamera(cameraEntity, camera, cameraTransform, viewID);

        Scene.ForEach((Entity entity, ref Transform t) =>
        {
            var layer = entity.Layer;

            if (camera.cullingLayers.HasLayer(layer) == false)
            {
                return;
            }

            foreach (var system in systems)
            {
                var related = entity.GetComponent(system.RelatedComponent());

                if (related != null)
                {
                    system.Preprocess(entity, t, related, camera, cameraTransform);

                    if (related is Renderable renderable &&
                        renderable.enabled)
                    {
                        renderable.isVisible = frustumCuller.AABBTest(renderable.bounds) != FrustumAABBResult.Invisible || true; //TEMP: Figure out what's wrong with the frustum culler

                        if (renderable.isVisible && renderable.forceRenderingOff == false)
                        {
                            system.Process(entity, t, related, camera, cameraTransform, viewID);
                        }
                    }
                    else if (related is not Renderable) //Systems that do not require a renderer
                    {
                        system.Process(entity, t, related, camera, cameraTransform, viewID);
                    }
                }
            }
        });

        foreach (var system in systems)
        {
            system.Submit();
        }
    }

    public void RenderAccumulator(Entity cameraEntity, Camera camera, Transform cameraTransform, ushort viewID)
    {
        var systems = new List<IRenderSystem>();

        lock(lockObject)
        {
            systems.AddRange(renderSystems);
        }

        foreach (var system in systems)
        {
            system.Prepare();
        }

        PrepareCamera(cameraEntity, camera, cameraTransform, viewID);

        var alpha = accumulator / Time.fixedDeltaTime;

        lock (lockObject)
        {
            if (currentDrawBucket.drawCalls.TryGetValue(viewID, out var drawCalls) && previousDrawBucket.drawCalls.TryGetValue(viewID, out var previousDrawCalls))
            {
                foreach (var call in drawCalls)
                {
                    var previous = previousDrawCalls.Find(x => x.entity.Identifier == call.entity.Identifier);

                    if (call.renderable.enabled)
                    {
                        var currentPosition = call.position;
                        var currentRotation = call.rotation;
                        var currentScale = call.scale;

                        if (previous == null)
                        {
                            stagingTransform.LocalPosition = currentPosition;
                            stagingTransform.LocalRotation = currentRotation;
                            stagingTransform.LocalScale = currentScale;
                        }
                        else
                        {
                            var previousPosition = previous.position;
                            var previousRotation = previous.rotation;
                            var previousScale = previous.scale;

                            stagingTransform.LocalPosition = Vector3.Lerp(previousPosition, currentPosition, alpha);
                            stagingTransform.LocalRotation = Quaternion.Lerp(previousRotation, currentRotation, alpha);
                            stagingTransform.LocalScale = Vector3.Lerp(previousScale, currentScale, alpha);
                        }

                        foreach (var system in systems)
                        {
                            if (call.relatedComponent.GetType() == system.RelatedComponent())
                            {
                                system.Process(call.entity, stagingTransform, call.relatedComponent,
                                    camera, cameraTransform, viewID);
                            }
                        }
                    }
                }
            }
        }

        foreach (var system in systems)
        {
            system.Submit();
        }
    }

    private void UpdateStandard()
    {
        CurrentViewID = 1;

        var cameras = World.Current.SortedCameras;

        if (cameras.Length > 0)
        {
            foreach (var c in cameras)
            {
                RenderStandard(c.entity, c.camera, c.transform, CurrentViewID++);
            }
        }
    }

    private void UpdateAccumulator()
    {
        CurrentViewID = 1;

        var cameras = Scene.SortedCameras;

        if (cameras.Length > 0)
        {
            foreach (var c in cameras)
            {
                RenderAccumulator(c.entity, c.camera, c.transform, CurrentViewID++);
            }
        }

        if (needsDrawCalls)
        {
            CurrentViewID = 1;

            lock (lockObject)
            {
                (currentDrawBucket, previousDrawBucket) = (previousDrawBucket, currentDrawBucket);

                currentDrawBucket.drawCalls.Clear();
            }

            foreach (var c in cameras)
            {
                var camera = c.camera;
                var cameraTransform = c.transform;

                unsafe
                {
                    var projection = Camera.Projection(c.entity, c.camera);
                    var view = cameraTransform.Matrix;

                    Matrix4x4.Invert(view, out view);

                    frustumCuller.Update(view, projection);
                }

                Scene.ForEach((Entity entity, ref Transform t) =>
                {
                    var layer = entity.Layer;

                    if (camera.cullingLayers.HasLayer(layer) == false)
                    {
                        return;
                    }

                    foreach (var system in renderSystems)
                    {
                        var related = entity.GetComponent(system.RelatedComponent());

                        if (related != null)
                        {
                            system.Preprocess(entity, t, related, camera, cameraTransform);

                            if (related is Renderable renderable &&
                                renderable.enabled)
                            {
                                renderable.isVisible = frustumCuller.AABBTest(renderable.bounds) != FrustumAABBResult.Invisible || true; //TEMP: Figure out what's wrong with the frustum culler

                                if (renderable.isVisible && renderable.forceRenderingOff == false)
                                {
                                    AddDrawCall(entity, t, related, renderable, CurrentViewID);
                                }
                            }
                        }
                    }
                });

                CurrentViewID++;
            }
        }

        if (needsDrawCalls)
        {
            needsDrawCalls = false;
        }

        accumulator = Time.Accumulator;
    }
    #endregion

    #region Render Helpers

    private void PrepareCamera(Entity entity, Camera camera, Transform cameraTransform, ushort viewID)
    {
        unsafe
        {
            var projection = Camera.Projection(entity, camera);
            var view = cameraTransform.Matrix;

            Matrix4x4.Invert(view, out view);

            bgfx.set_view_transform(viewID, &view, &projection);

            frustumCuller.Update(view, projection);
        }

        switch (camera.clearMode)
        {
            case CameraClearMode.Depth:
                bgfx.set_view_clear(viewID, (ushort)bgfx.ClearFlags.Depth, 0, 1, 0);

                break;

            case CameraClearMode.None:
                bgfx.set_view_clear(viewID, (ushort)bgfx.ClearFlags.None, 0, 1, 0);

                break;

            case CameraClearMode.SolidColor:
                bgfx.set_view_clear(viewID, (ushort)(bgfx.ClearFlags.Color | bgfx.ClearFlags.Depth), camera.clearColor.UIntValue, 1, 0);

                break;
        }

        bgfx.set_view_rect(viewID, (ushort)camera.viewport.X, (ushort)camera.viewport.Y,
            (ushort)(camera.viewport.Z * Screen.Width), (ushort)(camera.viewport.W * Screen.Height));
    }

    /// <summary>
    /// Adds a drawcall to the drawcall list
    /// </summary>
    /// <param name="entity">The entity to draw</param>
    /// <param name="transform">The entity's transform</param>
    /// <param name="relatedComponent">The entity's related component</param>
    /// <param name="renderable">The entity's Renderable</param>
    /// <param name="viewID">The current view ID</param>
    private void AddDrawCall(Entity entity, Transform transform, IComponent relatedComponent, Renderable renderable, ushort viewID)
    {
        lock (lockObject)
        {
            if (currentDrawBucket.drawCalls.TryGetValue(viewID, out var drawCalls) == false)
            {
                drawCalls = new();

                currentDrawBucket.drawCalls.Add(viewID, drawCalls);
            }

            drawCalls.Add(new()
            {
                entity = entity,
                renderable = renderable,
                position = transform.Position,
                rotation = transform.Rotation,
                scale = transform.Scale,
                relatedComponent = relatedComponent,
            });
        }
    }
    #endregion
}
