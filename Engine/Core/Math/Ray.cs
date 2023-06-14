﻿using System;
using System.Numerics;
using System.Runtime.InteropServices;

namespace Staple
{
    /// <summary>
    /// Represents a ray
    /// </summary>
    [Serializable]
    [StructLayout(LayoutKind.Sequential, Pack = 0)]
    public struct Ray
    {
        public Vector3 position;
        public Vector3 direction;

        public Ray(Vector3 position, Vector3 direction)
        {
            this.position = position;
            this.direction = direction;
        }

        public override string ToString()
        {
            return $"(position: {position}, direction: {direction})";
        }

        /// <summary>
        /// Test whether this ray intersects an AABB
        /// </summary>
        /// <param name="ray">The ray</param>
        /// <param name="aabb">The AABB</param>
        /// <returns>Whether it intersects</returns>
        public static bool IntersectsAABB(Ray ray, AABB aabb)
        {
            return IntersectsAABB(ray, aabb, out _, out _);
        }

        /// <summary>
        /// Test whether this ray intersects an AABB
        /// </summary>
        /// <param name="ray">The ray</param>
        /// <param name="aabb">The AABB</param>
        /// <param name="t0">The first point</param>
        /// <param name="t1">The last point</param>
        /// <returns>Whether it intersects</returns>
        public static bool IntersectsAABB(Ray ray, AABB aabb, out float t0, out float t1)
        {
            t0 = 0;
            t1 = 0;

            float tfirst = -999999;
            float tlast = 999999;

            bool RaySlabIntersect(float start, float dir, float min, float max)
            {
                if(Math.Abs(dir) < 1.0E-8)
                {
                    return (start < max && start > min);
                }

                float tmin = (min - start) / dir;
                float tmax = (max - start) / dir;

                if(tmin > tmax)
                {
                    (tmin, tmax) = (tmax, tmin);
                }

                if (tmax < tfirst || tmin > tlast)
                {
                    return false;
                }

                if(tmin > tfirst)
                {
                    tfirst = tmin;
                }

                if(tmax < tlast)
                {
                    tlast = tmax;
                }

                return true;
            }

            if(RaySlabIntersect(ray.position.X, ray.direction.X, aabb.Min.X, aabb.Max.X) == false)
            {
                return false;
            }

            if (RaySlabIntersect(ray.position.Y, ray.direction.Y, aabb.Min.Y, aabb.Max.Y) == false)
            {
                return false;
            }

            if (RaySlabIntersect(ray.position.Z, ray.direction.Z, aabb.Min.Z, aabb.Max.Z) == false)
            {
                return false;
            }

            t0 = tfirst;
            t1 = tlast;

            return true;
        }

        /// <summary>
        /// Test whether this ray intersects an AABB
        /// </summary>
        /// <param name="ray">The ray</param>
        /// <param name="aabb">The AABB</param>
        /// <param name="transform">A transform to apply to the AABB</param>
        /// <returns>Whether it intersects</returns>
        public static bool IntersectsAABB(Ray ray, AABB aabb, Transform transform)
        {
            return IntersectsAABB(ray, aabb, transform, out _, out _);
        }

        /// <summary>
        /// Test whether this ray intersects an AABB
        /// </summary>
        /// <param name="ray">The ray</param>
        /// <param name="aabb">The AABB</param>
        /// <param name="transform">A transform to apply to the AABB</param>
        /// <param name="t0">The first point</param>
        /// <param name="t1">The last point</param>
        /// <returns>Whether it intersects</returns>
        public static bool IntersectsAABB(Ray ray, AABB aabb, Transform transform, out float t0, out float t1)
        {
            t0 = 0;
            t1 = 0;

            var rayDelta = transform.Position - ray.position;
            var matrix = transform.Matrix;
            int p = 0;
            float tmin = -999999, tmax = 999999;

            var floatMatrix = new float[16]
            {
                matrix.M11, matrix.M12, matrix.M13, matrix.M14,
                matrix.M21, matrix.M22, matrix.M23, matrix.M24,
                matrix.M31, matrix.M32, matrix.M33, matrix.M34,
                matrix.M41, matrix.M42, matrix.M43, matrix.M44,
            };

            for(var i = 0; i < 3; i++, p += 4)
            {
                var axis = new Vector3(floatMatrix[p], floatMatrix[p + 1], floatMatrix[p + 2]);

                var nominatorLength = Vector3.Dot(axis, rayDelta);
                var denominatorLength = Vector3.Dot(ray.direction, axis);

                float boxMin = 0;
                float boxMax = 0;

                switch (i)
                {
                    case 0:
                        boxMin = aabb.Min.X;
                        boxMax = aabb.Max.X;

                        break;

                    case 1:
                        boxMin = aabb.Min.Y;
                        boxMax = aabb.Max.Y;

                        break;

                    case 2:
                        boxMin = aabb.Min.Z;
                        boxMax = aabb.Max.Z;

                        break;
                }

                if (Math.Abs(denominatorLength) > 0.00001)
                {

                    var min = (nominatorLength + boxMin) / denominatorLength;
                    var max = (nominatorLength + boxMax) / denominatorLength;

                    if(min > max)
                    {
                        (min, max) = (max, min);
                    }

                    if(min > tmin)
                    {
                        tmin = min;
                    }

                    if(max < tmax)
                    {
                        tmax = max;
                    }

                    if(tmax < tmin)
                    {
                        return false;
                    }
                }
                else if(-nominatorLength + boxMin > 0 || -nominatorLength + boxMax < 0)
                {
                    return false;
                }
            }

            t0 = tmin;
            t1 = tmax;

            return true;
        }
    }
}
