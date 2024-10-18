﻿using Bgfx;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Numerics;

namespace Staple.Internal;

public class SkinnedMeshRenderSystem : IRenderSystem
{
    internal const int MaxBones = 255;

    private struct RenderInfo
    {
        public SkinnedMeshRenderer renderer;
        public Vector3 position;
        public Matrix4x4 transform;
        public ushort viewID;
    }

    private readonly List<RenderInfo> renderers = [];

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
    }

    public void Process(Entity entity, Transform transform, IComponent relatedComponent,
        Camera activeCamera, Transform activeCameraTransform, ushort viewId)
    {
        var renderer = relatedComponent as SkinnedMeshRenderer;

        if (renderer.mesh == null ||
            renderer.mesh.meshAsset == null ||
            renderer.mesh.meshAssetIndex < 0 ||
            renderer.mesh.meshAssetIndex >= renderer.mesh.meshAsset.meshes.Count ||
            renderer.materials == null ||
            renderer.materials.Count != renderer.mesh.submeshes.Count)
        {
            return;
        }

        for (var i = 0; i < renderer.materials.Count; i++)
        {
            if (renderer.materials[i]?.IsValid == false)
            {
                return;
            }
        }

        if(renderer.checkedAnimator == false)
        {
            renderer.checkedAnimator = true;

            renderer.animator = entity.GetComponentInParent<SkinnedMeshAnimator>();
        }

        renderers.Add(new RenderInfo()
        {
            renderer = renderer,
            position = transform.Position,
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
            var animator = pair.renderer.animator;
            var mesh = renderer.mesh;
            var meshAsset = mesh.meshAsset;
            var meshAssetMesh = meshAsset.meshes[mesh.meshAssetIndex];

            var useAnimator = animator != null && animator.evaluator != null;

            if(renderer.cachedBoneMatrices.Count != renderer.mesh.submeshes.Count)
            {
                renderer.cachedBoneMatrices.Clear();
                
                for(var i = 0; i < renderer.mesh.submeshes.Count; i++)
                {
                    renderer.cachedBoneMatrices.Add(new Matrix4x4[meshAssetMesh.bones[i].Count]);
                }
            }

            for (var i = 0; i < renderer.mesh.submeshes.Count; i++)
            {
                if (meshAssetMesh.bones[i].Count > MaxBones)
                {
                    Log.Warning($"Skipping skinned mesh render for {meshAssetMesh.name}: " +
                        $"Bone count of {meshAssetMesh.bones[i].Count} exceeds limit of {MaxBones}, try setting split large meshes in the import settings!");

                    continue;
                }

                if (renderer.cachedBoneMatrices[i].Length != meshAssetMesh.bones[i].Count)
                {
                    renderer.cachedBoneMatrices[i] = new Matrix4x4[meshAssetMesh.bones[i].Count];
                }

                var boneMatrices = renderer.cachedBoneMatrices[i];

                for (var j = 0; j < boneMatrices.Length; j++)
                {
                    var bone = meshAssetMesh.bones[i][j];

                    var globalTransform = Matrix4x4.Identity;

                    if (useAnimator && MeshAsset.TryGetNode(animator.evaluator.rootNode, bone.name, out var localNode))
                    {
                        globalTransform = localNode.BakedTransform;
                    }
                    else
                    {
                        var node = mesh.meshAsset.GetNode(bone.name);

                        globalTransform = node.GlobalTransform;
                    }

                    boneMatrices[j] = bone.offsetMatrix * globalTransform;
                }

                unsafe
                {
                    var transform = pair.transform;

                    _ = bgfx.set_transform(&transform, 1);
                }

                bgfx.set_state((ulong)(state |
                    renderer.mesh.PrimitiveFlag() |
                    renderer.materials[i].shader.BlendingFlag |
                    renderer.materials[i].CullingFlag), 0);

                var material = renderer.materials[i];

                material.ApplyProperties();

                material.shader.SetMatrix4x4(material.GetShaderHandle("u_boneMatrices"), boneMatrices);

                renderer.mesh.SetActive(i);

                material.EnableShaderKeyword(Shader.SkinningKeyword);

                var lightSystem = RenderSystem.Instance.Get<LightSystem>();

                lightSystem?.ApplyMaterialLighting(material, pair.renderer.lighting);

                var program = material.ShaderProgram;

                if (program.Valid)
                {
                    lightSystem?.ApplyLightProperties(pair.position, pair.transform, material,
                        RenderSystem.CurrentCamera.Item2.Position, pair.renderer.lighting);

                    bgfx.submit(pair.viewID, program, 0, (byte)bgfx.DiscardFlags.All);
                }
                else
                {
                    bgfx.discard((byte)bgfx.DiscardFlags.All);
                }
            }
        }
    }

    public static void GatherNodes(Transform parent, Dictionary<string, MeshAsset.Node> nodeCache, MeshAsset.Node rootNode)
    {
        if (parent == null || nodeCache == null)
        {
            return;
        }

        nodeCache.Clear();

        void GatherNodes(MeshAsset.Node node)
        {
            if (node == null)
            {
                return;
            }

            nodeCache.AddOrSetKey(node.name, node);

            foreach (var child in node.children)
            {
                GatherNodes(child);
            }
        }

        GatherNodes(rootNode);
    }

    public static void GatherNodeTransforms(Transform parent, Dictionary<string, Transform> transformCache, MeshAsset.Node rootNode)
    {
        if (parent == null || transformCache == null)
        {
            return;
        }

        transformCache.Clear();

        void GatherNodes(MeshAsset.Node node)
        {
            if (node == null)
            {
                return;
            }

            var childTransform = parent.SearchChild(node.name);

            if (childTransform == null)
            {
                foreach (var child in node.children)
                {
                    GatherNodes(child);
                }

                return;
            }

            transformCache.AddOrSetKey(node.name, childTransform);

            foreach (var child in node.children)
            {
                GatherNodes(child);
            }
        }

        GatherNodes(rootNode);
    }

    public static void ApplyNodeTransform(Dictionary<string, MeshAsset.Node> nodeCache, Dictionary<string, Transform> transformCache, bool original = false)
    {
        //return;
        foreach (var pair in transformCache)
        {
            if(nodeCache.TryGetValue(pair.Key, out var node) == false)
            {
                continue;
            }

            pair.Value.LocalPosition = original ? node.OriginalPosition : node.Position;
            pair.Value.LocalRotation = original ? node.OriginalRotation : node.Rotation;
            pair.Value.LocalScale = original ? node.OriginalScale : node.Scale;
        }
    }

    public static void ApplyTransformsToNodes(Dictionary<string, MeshAsset.Node> nodeCache, Dictionary<string, Transform> transformCache)
    {
        //return;

        foreach(var pair in nodeCache)
        {
            if(transformCache.TryGetValue(pair.Key, out var transform) == false)
            {
                continue;
            }

            pair.Value.Transform = Math.TransformationMatrix(transform.LocalPosition, transform.LocalScale, transform.LocalRotation);
        }
    }
}
