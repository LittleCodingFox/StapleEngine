﻿using System.Collections.Generic;
using System.Numerics;

namespace Staple;

internal class SkinnedMeshAnimator
{
    public MeshAsset.Animation animation;
    public MeshAsset meshAsset;
    public Dictionary<string, Matrix4x4> currentTransforms = new();

    public Dictionary<int, int> lastPositionIndex = new();
    public Dictionary<int, int> lastRotationIndex = new();
    public Dictionary<int, int> lastScaleIndex = new();
    public float lastTime;
    public float playTime;

    public Matrix4x4 GlobalTransform(string name)
    {
        if(meshAsset.nodes.TryGetValue(name, out var node) == false)
        {
            return Matrix4x4.Identity;
        }
        
        if(currentTransforms.TryGetValue(name, out var currentMatrix) == false)
        {
            currentMatrix = node.transform;
        }

        if(node.parent != null)
        {
            return currentMatrix * GlobalTransform(node.parent.name);
        }

        return currentMatrix;
    }

    public void Evaluate()
    {
        if(animation == null || meshAsset == null)
        {
            return;
        }

        playTime += Time.deltaTime;

        var t = playTime * animation.ticksPerSecond;

        var time = t % animation.duration;

        for(var i = 0; i < animation.channels.Count; i++)
        {
            var channel = animation.channels[i];

            if(channel.node == null)
            {
                continue;
            }

            Vector3 GetVector3(List<MeshAsset.AnimationKey<Vector3>> keys, ref int last)
            {
                var outValue = Vector3.Zero;

                if (keys.Count > 0)
                {
                    var frame = (time >= lastTime) ? last : 0;

                    while (frame < keys.Count - 1)
                    {
                        if (time < keys[frame + 1].time)
                        {
                            break;
                        }

                        frame++;
                    }

                    if (frame >= keys.Count)
                    {
                        frame = 0;
                    }

                    var nextFrame = (frame + 1) % keys.Count;

                    var current = keys[frame];
                    var next = keys[nextFrame];

                    var timeDifference = next.time - current.time;

                    if(timeDifference < 0)
                    {
                        timeDifference += animation.duration;
                    }

                    if(timeDifference > 0)
                    {
                        outValue = Vector3.Lerp(current.value, next.value, (time - current.time) / timeDifference);
                    }
                    else
                    {
                        outValue = current.value;
                    }

                    last = frame;
                }

                return outValue;
            }

            Quaternion GetQuaternion(List<MeshAsset.AnimationKey<Quaternion>> keys, ref int last)
            {
                var outValue = Quaternion.Zero;

                if (keys.Count > 0)
                {
                    var frame = (time >= lastTime) ? last : 0;

                    while (frame < keys.Count - 1)
                    {
                        if (time < keys[frame + 1].time)
                        {
                            break;
                        }

                        frame++;
                    }

                    if (frame >= keys.Count)
                    {
                        frame = 0;
                    }

                    var nextFrame = (frame + 1) % keys.Count;

                    var current = keys[frame];
                    var next = keys[nextFrame];

                    var timeDifference = next.time - current.time;

                    if (timeDifference < 0)
                    {
                        timeDifference += animation.duration;
                    }

                    if (timeDifference > 0)
                    {
                        outValue = Quaternion.Slerp(current.value, next.value, (time - current.time) / timeDifference);
                    }
                    else
                    {
                        outValue = current.value;
                    }

                    last = frame;
                }

                return outValue;
            }

            if(lastPositionIndex.TryGetValue(i, out var positionIndex) == false)
            {
                lastPositionIndex.Add(i, 0);
            }

            if (lastScaleIndex.TryGetValue(i, out var scaleIndex) == false)
            {
                lastScaleIndex.Add(i, 0);
            }

            if (lastRotationIndex.TryGetValue(i, out var rotationIndex) == false)
            {
                lastRotationIndex.Add(i, 0);
            }

            var position = GetVector3(channel.positions, ref positionIndex);
            var scale = GetVector3(channel.scales, ref scaleIndex);
            var rotation = GetQuaternion(channel.rotations, ref rotationIndex);

            lastPositionIndex.AddOrSetKey(i, positionIndex);
            lastScaleIndex.AddOrSetKey(i, scaleIndex);
            lastRotationIndex.AddOrSetKey(i, rotationIndex);

            var transform = Matrix4x4.CreateScale(scale) *
                    Matrix4x4.CreateFromQuaternion(rotation) *
                    Matrix4x4.CreateTranslation(position);

            channel.node.transform = transform;

            //currentTransforms.AddOrSetKey(channel.node.name, transform);
        }

        lastTime = time;
    }
}
