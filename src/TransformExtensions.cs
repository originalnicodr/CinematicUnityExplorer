// TransformExtensions.cs
using UnityEngine;

namespace CinematicUnityExplorer.Inspectors // 或者你的擴展方法所在的命名空間
{
    public static class TransformExtensions
    {
        public static float GetScaleX(this Transform transform) => transform.localScale.x;
        public static float GetScaleY(this Transform transform) => transform.localScale.y;
        public static string GetTransformPath(this Transform transform, bool includeSelf = true)
        {
            string path = includeSelf ? transform.name : "";
            Transform current = transform.parent;
            while (current != null)
            {
                path = current.name + "/" + path;
                current = current.parent;
            }
            return path;
        }
    }

    public static class VectorExtensions
    {
        public static Vector3 MultiplyElements(this Vector3 a, Vector3 b)
        {
            return new Vector3(a.x * b.x, a.y * b.y, a.z * b.z);
        }
        public static Vector2 MultiplyElements(this Vector2 a, Vector2 b)
        {
            return new Vector2(a.x * b.x, a.y * b.y);
        }
    }
}