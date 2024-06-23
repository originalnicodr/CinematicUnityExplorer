﻿using System;
using UnityEngine;

namespace GizmosLibraryPlugin
{
    public static class GLHelper
    {
        static Material defaultGizmosMaterial;
        static Material alwaysDrawOnTopDefaultGizmosMaterial;
        public static Material GetDefaultMaterial()
        {
            if (!defaultGizmosMaterial)
            {
                Shader shader = Shader.Find("Hidden/Internal-Colored");
                defaultGizmosMaterial = new Material(shader)
                {
                    hideFlags = HideFlags.HideAndDontSave
                };
                // Turn on alpha blending
                defaultGizmosMaterial.SetInt("_SrcBlend", (int)UnityEngine.Rendering.BlendMode.SrcAlpha);
                defaultGizmosMaterial.SetInt("_DstBlend", (int)UnityEngine.Rendering.BlendMode.OneMinusSrcAlpha);
                // Turn backface culling off
                defaultGizmosMaterial.SetInt("_Cull", (int)UnityEngine.Rendering.CullMode.Off);
                // Turn off depth writes
                defaultGizmosMaterial.SetInt("_ZWrite", 0);
            }
            return defaultGizmosMaterial;
        }
        public static void SetDefaultMaterialPass(int pass = 0, bool alwaysDrawOnTop = false)
        {
            if(!alwaysDrawOnTop)
                GetDefaultMaterial().SetPass(pass);
            else
                GetAlwaysDrawOnTopDefaultMaterial().SetPass(pass);
        }
        public static Material GetAlwaysDrawOnTopDefaultMaterial()
        {
            if (!alwaysDrawOnTopDefaultGizmosMaterial)
            {
                Shader shader = Shader.Find("Hidden/Internal-Colored");
                alwaysDrawOnTopDefaultGizmosMaterial = new Material(shader)
                {
                    hideFlags = HideFlags.HideAndDontSave
                };
                // Turn on alpha blending
                alwaysDrawOnTopDefaultGizmosMaterial.SetInt("_SrcBlend", (int)UnityEngine.Rendering.BlendMode.SrcAlpha);
                alwaysDrawOnTopDefaultGizmosMaterial.SetInt("_DstBlend", (int)UnityEngine.Rendering.BlendMode.OneMinusSrcAlpha);
                // Turn backface culling off
                alwaysDrawOnTopDefaultGizmosMaterial.SetInt("_Cull", (int)UnityEngine.Rendering.CullMode.Off);
                // Turn off depth writes
                //From https://answers.unity.com/questions/1674373/rendering-gl-lines-z-order-manipulation.html
                alwaysDrawOnTopDefaultGizmosMaterial.SetInt("_ZWrite", -10);
                alwaysDrawOnTopDefaultGizmosMaterial.SetInt("_ZTest", (int)UnityEngine.Rendering.CompareFunction.Disabled);
            }
            return alwaysDrawOnTopDefaultGizmosMaterial;
        }
        public static void DrawWithReference(Transform reference, Action drawMethod) 
        {
            GL.PushMatrix();
            GL.MultMatrix(reference.localToWorldMatrix);
            drawMethod?.Invoke();
            GL.PopMatrix();
        }
        public static void DrawOnGlobalReference(Action drawMethod)
        {
            GL.PushMatrix();
            GL.MultMatrix(Matrix4x4.TRS(Vector3.zero, Quaternion.identity, Vector3.one));
            drawMethod?.Invoke();
            GL.PopMatrix();
        }
        public static void DrawWithOrthoProjection(Action drawMethod)
        {
            GL.PushMatrix();
            GL.LoadOrtho();
            drawMethod?.Invoke();
            GL.PopMatrix();
        }

        public static void DrawAxis(float headSize, Color color, Vector3 offset)
        {
            GLDraw.Vector(Vector3.up, headSize, offset, color);
            GLDraw.Vector(Vector3.forward, headSize, offset, color);
            GLDraw.Vector(Vector3.right, headSize, offset, color);
        }
        public static void DrawAxis(float headSize, Color upColor, Color fowardColor, Color rightColor, Vector3 offset)
        {
            GLDraw.Vector(Vector3.up, headSize, offset, upColor);
            GLDraw.Vector(Vector3.forward, headSize, offset, fowardColor);
            GLDraw.Vector(Vector3.right, headSize, offset, rightColor);
        }
        public static void DrawTransform(Transform transform, float headSize, Color color)
        {
            GLDraw.Vector(transform.up * transform.lossyScale.x, headSize, transform.position, color);
            GLDraw.Vector(transform.forward * transform.lossyScale.z, headSize, transform.position, color);
            GLDraw.Vector(transform.right * transform.lossyScale.y, headSize, transform.position, color);
        }
        public static void DrawTransform(Transform transform, float headSize, Color upColor, Color fowardColor, Color rightColor)
        {
            GLDraw.Vector(transform.up * transform.lossyScale.x, headSize, transform.position, upColor);
            GLDraw.Vector(transform.forward * transform.lossyScale.z, headSize, transform.position, fowardColor);
            GLDraw.Vector(transform.right * transform.lossyScale.y, headSize, transform.position, rightColor);
        }
        public static void DrawColliderBoundingBox(Collider collider, Color color)
        {
            Bounds box = collider.bounds;
            GLDraw.WireframeCube(Vector3.forward * box.size.z, Vector3.up * box.size.y, Vector3.right * box.size.z, box.center, color);
        }
        public static void DrawCollider(Collider collider, Color color)
        {
            const int resolution = 12;
            if (collider is BoxCollider)
            {
                BoxCollider box = collider as BoxCollider;
                GLDraw.WireframeCube(Vector3.forward * box.size.z, Vector3.up * box.size.y, Vector3.right * box.size.z, box.center, color);
            }
            else if (collider is SphereCollider)
            {
                SphereCollider sphere = collider as SphereCollider;
                GLDraw.WireframeSphere(sphere.radius, sphere.center, Vector3.forward, Vector3.up, color, resolution);
            }
            else if (collider is CapsuleCollider)
            {
                CapsuleCollider capsule = collider as CapsuleCollider;
                GLDraw.WireframeCapsule(capsule.radius, capsule.center + capsule.height * Vector3.up / 2f, capsule.center - capsule.height * Vector3.up / 2f, color, resolution);
            }
        }
    }
}
