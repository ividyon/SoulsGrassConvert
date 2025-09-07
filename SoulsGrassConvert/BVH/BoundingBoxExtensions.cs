// Copyright(C) David W. Jeske, 2013
// Released to the public domain. 

using System.Numerics;
using BoundingBox = SoulsFormats.GRASS.BoundingBox;

namespace SoulsGrassConvert.BVH
{
    public static class BoundingBoxExtensions
    {
        public static Vector3 ComponentMin(this Vector3 vec, Vector3 other)
        {
            return new Vector3(Math.Min(vec.X, other.X), Math.Min(vec.Y, other.Y), Math.Min(vec.Z, other.Z));
        }
        public static Vector3 ComponentMax(this Vector3 vec, Vector3 other)
        {
            return new Vector3(Math.Max(vec.X, other.X), Math.Max(vec.Y, other.Y), Math.Max(vec.Z, other.Z));
        }

        public static void Combine(this BoundingBox box, ref BoundingBox other)
        {
            box.Min = box.Min.ComponentMin(other.Min);
            box.Max = box.Max.ComponentMax(other.Max);
        }

        public static bool IntersectsSphere(this BoundingBox box, Vector3 origin, float radius)
        {
            if (
                (origin.X + radius < box.Min.X) ||
                (origin.Y + radius < box.Min.Y) ||
                (origin.Z + radius < box.Min.Z) ||
                (origin.X - radius > box.Max.X) ||
                (origin.Y - radius > box.Max.Y) ||
                (origin.Z - radius > box.Max.Z)
            )
            {
                return false;
            }
            else
            {
                return true;
            }
        }

        public static bool IntersectsBoundingBox(this BoundingBox box, BoundingBox other)
        {
            return ((box.Max.X > other.Min.X) && (box.Min.X < other.Max.X) &&
                    (box.Max.Y > other.Min.Y) && (box.Min.Y < other.Max.Y) &&
                    (box.Max.Z > other.Min.Z) && (box.Min.Z < other.Max.Z));
        }

        public static bool Equals(this BoundingBox box, BoundingBox other)
        {
            return
                (box.Min.X == other.Min.X) &&
                (box.Min.Y == other.Min.Y) &&
                (box.Min.Z == other.Min.Z) &&
                (box.Max.X == other.Max.X) &&
                (box.Max.Y == other.Max.Y) &&
                (box.Max.Z == other.Max.Z);
        }

        public static void UpdateMin(this BoundingBox box, Vector3 localMin)
        {
            box.Min = box.Min.ComponentMin(localMin);
        }

        public static void UpdateMax(this BoundingBox box, Vector3 localMax)
        {
            box.Max = box.Max.ComponentMax(localMax);
        }

        public static Vector3 Center(this BoundingBox box)
        {
            return (box.Min + box.Max) / 2f;
        }

        public static Vector3 Diff(this BoundingBox box)
        {
            return box.Max - box.Min;
        }

        // public SSSphere ToSphere()
        // {
        // 	float r = (Diff ().LengthFast + 0.001f)/2f;
        // 	return new SSSphere (Center (), r);
        // }

        public static void ExpandToFit(this BoundingBox box, BoundingBox other)
        {
            if (other.Min.X < box.Min.X)
            {
                box.Min = box.Min with { X = other.Min.X };
            }

            if (other.Min.Y < box.Min.Y)
            {
                box.Min = box.Min with { Y = other.Min.Y };
            }

            if (other.Min.Z < box.Min.Z)
            {
                box.Min = box.Min with { Z = other.Min.Z };
            }

            if (other.Max.X > box.Max.X)
            {
                box.Max = box.Max with { X = other.Max.X };
            }

            if (other.Max.Y > box.Max.Y)
            {
                box.Max = box.Max with { Y = other.Max.Y };
            }

            if (other.Max.Z > box.Max.Z)
            {
                box.Max = box.Max with { Z = other.Max.Z };
            }
        }

        public static BoundingBox ExpandedBy(this BoundingBox box, BoundingBox other)
        {
            BoundingBox newbox = box;
            if (other.Min.X < newbox.Min.X)
            {
                newbox.Min = newbox.Min with { X = other.Min.X };
            }

            if (other.Min.Y < newbox.Min.Y)
            {
                newbox.Min = newbox.Min with { Y = other.Min.Y };
            }

            if (other.Min.Z < newbox.Min.Z)
            {
                newbox.Min = newbox.Min with { Z = other.Min.Z };
            }

            if (other.Max.X > newbox.Max.X)
            {
                newbox.Max = newbox.Max with { X = other.Max.X };
            }

            if (other.Max.Y > newbox.Max.Y)
            {
                newbox.Max = newbox.Max with { Y = other.Max.Y };
            }

            if (other.Max.Z > newbox.Max.Z)
            {
                newbox.Max = newbox.Max with { Z = other.Max.Z };
            }

            return newbox;
        }

        public static void ExpandBy(this BoundingBox box, BoundingBox other)
        {
            box = box.ExpandedBy(other);
        }

        public static BoundingBox FromSphere(Vector3 pos, float radius)
        {
            BoundingBox box = new();
            box.Min = new Vector3(pos.X - radius, pos.Y - radius, pos.Z - radius);
            box.Max = new Vector3(pos.X + radius, pos.Y + radius, pos.Z + radius);
            return box;
        }

        private static readonly Vector4[] c_homogenousCorners =
        {
            new Vector4(-1f, -1f, -1f, 1f),
            new Vector4(-1f, 1f, -1f, 1f),
            new Vector4(1f, 1f, -1f, 1f),
            new Vector4(1f, -1f, -1f, 1f),

            new Vector4(-1f, -1f, 1f, 1f),
            new Vector4(-1f, 1f, 1f, 1f),
            new Vector4(1f, 1f, 1f, 1f),
            new Vector4(1f, -1f, 1f, 1f),
        };

        // public static BoundingBox FromFrustum(ref Matrix4 axisTransform, ref Matrix4 modelViewProj) {
        //     SSAABB ret = new SSAABB(float.PositiveInfinity, float.NegativeInfinity);
        //     Matrix4 inverse = modelViewProj;
        //     inverse.Invert();
        //     for (int i = 0; i < c_homogenousCorners.Length; ++i) {
        //         Vector4 corner = Vector4.Transform(c_homogenousCorners [i], inverse);
        //         //Vector3 transfPt = Vector3.Transform(corner.Xyz / corner.W, axisTransform);
        //
        //         //some_name code start 24112019
        //         Vector3 transfPt = (new Vector4(corner.Xyz / corner.W, 1) * axisTransform).Xyz;
        //         //some_name code end
        //
        //         ret.UpdateMin(transfPt);
        //         ret.UpdateMax(transfPt);
        //     }
        //     return ret;
        // }
    }
}