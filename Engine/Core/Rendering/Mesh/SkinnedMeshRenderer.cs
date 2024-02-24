﻿using System.Collections.Generic;

namespace Staple;

internal class SkinnedMeshRendererItem
{
    public MeshRenderer meshRenderer;
    public MeshAsset.Node node;
    public Transform transform;
}

public class SkinnedMeshRenderer : Renderable
{
    public Mesh mesh;
    public Material material;
    public string animation;

    internal Dictionary<string, SkinnedMeshRendererItem> nodeRenderers = new();
    internal SkinnedMeshAnimator animator;
}
