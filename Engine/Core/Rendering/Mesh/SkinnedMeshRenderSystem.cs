﻿using Bgfx;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Numerics;

namespace Staple;

internal class SkinnedMeshRenderSystem : IRenderSystem
{
    private struct RenderInfo
    {
        public SkinnedMeshRenderer renderer;
        public Matrix4x4 transform;
        public ushort viewID;
    }

    private readonly List<RenderInfo> renderers = new();

    public void Destroy()
    {
    }

    public void Prepare()
    {
        renderers.Clear();
    }

    public void Preprocess(Entity entity, Transform transform, IComponent relatedComponent,
        Camera activeCamera, Transform activeCameraTransform)
    {
        var r = relatedComponent as SkinnedMeshRenderer;

        if (r.mesh == null ||
            r.mesh.meshAsset == null ||
            r.mesh.meshAssetIndex < 0 ||
            r.mesh.meshAssetIndex >= r.mesh.meshAsset.meshes.Count ||
            r.materials == null ||
            r.materials.Count != r.mesh.submeshes.Count ||
            r.materials.Any(x => x.IsValid == false))
        {
            return;
        }

        void GatherNodes(MeshAsset.Node node)
        {
            if(node == null)
            {
                return;
            }

            var t = transform.parent?.SearchChild(node.name);

            if (t == null)
            {
                return;
            }

            r.nodeRenderers.AddOrSetKey(node.name, new SkinnedMeshRendererItem()
            {
                node = node,
                transform = t,
            });

            foreach(var child in node.children)
            {
                GatherNodes(child);
            }
        }

        GatherNodes(r.mesh.meshAsset.rootNode);
    }

    public void Process(Entity entity, Transform transform, IComponent relatedComponent,
        Camera activeCamera, Transform activeCameraTransform, ushort viewId)
    {
        var r = relatedComponent as SkinnedMeshRenderer;

        if (r.mesh == null ||
            r.mesh.meshAsset == null ||
            r.mesh.meshAssetIndex < 0 ||
            r.mesh.meshAssetIndex >= r.mesh.meshAsset.meshes.Count ||
            r.materials == null ||
            r.materials.Count != r.mesh.submeshes.Count ||
            r.materials.Any(x => x.IsValid == false))
        {
            return;
        }

        renderers.Add(new RenderInfo()
        {
            renderer = r,
            transform = transform.Matrix,
            viewID = viewId,
        });
    }

    public Type RelatedComponent()
    {
        return typeof(SkinnedMeshRenderer);
    }

    public void Submit()
    {
        bgfx.StateFlags state = bgfx.StateFlags.WriteRgb |
            bgfx.StateFlags.WriteA |
            bgfx.StateFlags.WriteZ |
            bgfx.StateFlags.DepthTestLequal;

        foreach (var pair in renderers)
        {
            var renderer = pair.renderer;
            var mesh = renderer.mesh;
            var meshAsset = mesh.meshAsset;
            var meshAssetMesh = meshAsset.meshes[mesh.meshAssetIndex];

            var useAnimator = (renderer.animation?.Length ?? 0) > 0 && meshAsset.animations.ContainsKey(renderer.animation);

            if(useAnimator &&
                (renderer.animator == null ||
                renderer.animator.animation.name != renderer.animation))
            {
                renderer.animator = new(meshAsset, meshAsset.animations[renderer.animation]);
            }

            if(useAnimator)
            {
                renderer.animator.Evaluate();
            }

            for(var i = 0; i < renderer.mesh.submeshes.Count; i++)
            {
                var boneMatrices = new Matrix4x4[meshAssetMesh.bones[i].Count];

                for (var j = 0; j < boneMatrices.Length; j++)
                {
                    var bone = meshAssetMesh.bones[i][j];

                    Matrix4x4 localTransform;
                    Matrix4x4 globalTransform;

                    if (useAnimator && renderer.animator.nodes.TryGetValue(bone.name, out var localNode))
                    {
                        globalTransform = localNode.GlobalTransform;
                        localTransform = localNode.transform;
                    }
                    else
                    {
                        var node = mesh.meshAsset.GetNode(bone.name);

                        localTransform = node.transform;
                        globalTransform = node.GlobalTransform;
                    }

                    boneMatrices[j] = bone.offsetMatrix * globalTransform * renderer.mesh.meshAsset.inverseTransform;

                    if (renderer.nodeRenderers.TryGetValue(bone.name, out var item) &&
                        Matrix4x4.Decompose(localTransform, out var scale, out var rotation, out var translation))
                    {
                        item.transform.LocalPosition = translation;
                        item.transform.LocalRotation = rotation;
                        item.transform.LocalScale = scale;
                    }
                }

                unsafe
                {
                    var transform = pair.transform;

                    _ = bgfx.set_transform(&transform, 1);
                }

                bgfx.set_state((ulong)(state | renderer.mesh.PrimitiveFlag() | renderer.materials[i].shader.BlendingFlag()), 0);

                renderer.materials[i].ApplyProperties();

                renderer.materials[i].shader.SetMatrix4x4("u_boneMatrices", boneMatrices, boneMatrices.Length);

                renderer.mesh.SetActive(i);

                bgfx.submit(pair.viewID, renderer.materials[i].shader.program, 0, (byte)bgfx.DiscardFlags.All);
            }
        }
    }
}