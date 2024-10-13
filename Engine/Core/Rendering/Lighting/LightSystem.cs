﻿using System;
using System.Collections.Generic;
using System.Linq;
using System.Numerics;

namespace Staple.Internal;

public class LightSystem : IRenderSystem, IWorldChangeReceiver
{
    public const int MaxLights = 16;

    private static readonly string LightAmbientKey = "u_lightAmbient";
    private static readonly string LightCountKey = "u_lightCount";
    private static readonly string LightDiffuseKey = "u_lightDiffuse";
    private static readonly string LightSpecularKey = "u_lightSpecular";
    private static readonly string LightTypePositionKey = "u_lightTypePosition";
    private static readonly string LightSpotDirectionKey = "u_lightSpotDirection";
    private static readonly string LightSpotValuesKey = "u_lightSpotValues";
    private static readonly string NormalMatrixKey = "u_normalMatrix";
    private static readonly string ViewPosKey = "u_viewPos";

    private readonly List<(Transform, Light)> lights = [];

    private readonly SceneQuery<Transform, Light> lightQuery = new();

    private readonly Vector4[] cachedLightTypePositions = new Vector4[MaxLights];
    private readonly Vector4[] cachedLightDiffuse = new Vector4[MaxLights];
    private readonly Vector4[] cachedLightSpotDirection = new Vector4[MaxLights];

    public LightSystem()
    {
        Shader.DefaultUniforms.Add((LightAmbientKey, ShaderUniformType.Color));
        Shader.DefaultUniforms.Add((LightCountKey, ShaderUniformType.Vector4));
        Shader.DefaultUniforms.Add(($"{LightDiffuseKey}[{MaxLights}]", ShaderUniformType.Vector4));
        Shader.DefaultUniforms.Add(($"{LightSpecularKey}[{MaxLights}]", ShaderUniformType.Vector4));
        Shader.DefaultUniforms.Add(($"{LightTypePositionKey}[{MaxLights}]", ShaderUniformType.Vector4));
        Shader.DefaultUniforms.Add(($"{LightSpotDirectionKey}[{MaxLights}]", ShaderUniformType.Vector4));
        Shader.DefaultUniforms.Add(($"{LightSpotValuesKey}[{MaxLights}]", ShaderUniformType.Vector4));
        Shader.DefaultUniforms.Add((NormalMatrixKey, ShaderUniformType.Matrix3x3));
        Shader.DefaultUniforms.Add((ViewPosKey, ShaderUniformType.Vector3));
    }

    public void Destroy()
    {
    }

    public void Prepare()
    {
    }

    public void Preprocess(Entity entity, Transform transform, IComponent relatedComponent, Camera activeCamera, Transform activeCameraTransform)
    {
    }

    public void Process(Entity entity, Transform transform, IComponent relatedComponent, Camera activeCamera, Transform activeCameraTransform, ushort viewId)
    {
    }

    public Type RelatedComponent()
    {
        return null;
    }

    public void Submit()
    {
    }

    public void ApplyMaterialLighting(Material material, MeshLighting lighting)
    {
        switch (lighting)
        {
            case MeshLighting.Lit:

                material.EnableShaderKeyword(Shader.LitKeyword);

                if (material.metadata.enabledShaderVariants.Contains(Shader.HalfLambertKeyword) == false)
                {
                    material.DisableShaderKeyword(Shader.HalfLambertKeyword);
                }

                break;

            case MeshLighting.Unlit:

                if (material.metadata.enabledShaderVariants.Contains(Shader.LitKeyword) == false)
                {
                    material.DisableShaderKeyword(Shader.LitKeyword);
                }

                if (material.metadata.enabledShaderVariants.Contains(Shader.HalfLambertKeyword) == false)
                {
                    material.DisableShaderKeyword(Shader.HalfLambertKeyword);
                }

                break;

            case MeshLighting.HalfLambert:

                material.EnableShaderKeyword(Shader.LitKeyword);
                material.EnableShaderKeyword(Shader.HalfLambertKeyword);

                break;
        }
    }

    public void ApplyLightProperties(Matrix4x4 transform, Material material, Vector3 cameraPosition, List<(Transform, Light)> lights)
    {
        if ((material?.IsValid ?? false) == false ||
            lights.Count == 0)
        {
            return;
        }

        var targets = lights;

        Matrix4x4.Decompose(transform, out _, out _, out var position);

        if (lights.Count > MaxLights)
        {
            targets = lights
                .OrderBy(x => Vector3.DistanceSquared(x.Item1.Position, position))
                .Take(MaxLights)
                .ToList();

            targets = lights.Take(MaxLights).ToList();
        }

        Matrix4x4.Invert(transform, out var invTransform);

        var transTransform = Matrix4x4.Transpose(invTransform);

        var normalMatrix = transTransform.ToMatrix3x3();

        var lightAmbient = AppSettings.Current.ambientLight;
        var lightCount = new Vector4(targets.Count);

        lightCount.X = targets.Count;

        for (var i = 0; i < targets.Count; i++)
        {
            cachedLightTypePositions[i] = new((float)targets[i].Item2.type,
                targets[i].Item1.Position.X,
                targets[i].Item1.Position.Y,
                targets[i].Item1.Position.Z);

            var forward = targets[i].Item1.Forward;

            if (targets[i].Item2.type == LightType.Directional)
            {
                (cachedLightTypePositions[i].Y, cachedLightTypePositions[i].Z, cachedLightTypePositions[i].W) = (-forward.X, -forward.Y, -forward.Z);
            }

            cachedLightDiffuse[i] = targets[i].Item2.color;

            cachedLightSpotDirection[i] = forward.ToVector4();
        }

        var viewPosHandle = material.GetShaderHandle(ViewPosKey);
        var normalMatrixHandle = material.GetShaderHandle(NormalMatrixKey);
        var lightAmbientHandle = material.GetShaderHandle(LightAmbientKey);
        var lightCountHandle = material.GetShaderHandle(LightCountKey);
        var lightTypePositionHandle = material.GetShaderHandle(LightTypePositionKey);
        var lightDiffuseHandle = material.GetShaderHandle(LightDiffuseKey);
        var lightSpotDirectionHandle = material.GetShaderHandle(LightSpotDirectionKey);

        material.shader.SetVector3(viewPosHandle, cameraPosition);
        material.shader.SetMatrix3x3(normalMatrixHandle, normalMatrix);
        material.shader.SetColor(lightAmbientHandle, lightAmbient);
        material.shader.SetVector4(lightCountHandle, lightCount);
        material.shader.SetVector4(lightTypePositionHandle, cachedLightTypePositions);
        material.shader.SetVector4(lightDiffuseHandle, cachedLightDiffuse);
        material.shader.SetVector4(lightSpotDirectionHandle, cachedLightSpotDirection);
    }

    public void ApplyLightProperties(Matrix4x4 transform, Material material, Vector3 cameraPosition)
    {
        ApplyLightProperties(transform, material, cameraPosition, lights);
    }

    public void WorldChanged()
    {
        lights.Clear();

        foreach (var pair in lightQuery)
        {
            lights.Add((pair.Item2, pair.Item3));
        }
    }
}
