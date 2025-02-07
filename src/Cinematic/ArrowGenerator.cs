using HarmonyLib;
using System.Collections.Generic;
using UnityEngine;
using UnityExplorer.Config;
#if UNHOLLOWER
using IL2CPPUtils = UnhollowerBaseLib.UnhollowerUtils;
#endif
#if INTEROP
using IL2CPPUtils = Il2CppInterop.Common.Il2CppInteropUtils;
#endif

namespace UnityExplorer
{
    public class ArrowGenerator
    {
        public static GameObject CreateArrow(Vector3 arrowPosition, Quaternion arrowRotation, Color color)
        {
            try {
                float arrowSizeValue = ConfigManager.Arrow_Size.Value;
                Vector3 arrowSize = new Vector3(Math.Max(arrowSizeValue, 0.1f), Math.Max(arrowSizeValue, 0.1f), Math.Max(arrowSizeValue, 0.1f));

                GameObject cylinder = GameObject.CreatePrimitive(PrimitiveType.Cylinder);
                cylinder.GetComponent<Collider>().enabled = false;
                cylinder.GetComponent<MeshFilter>().mesh = CreateCylinderMesh(0.01f, 20, 2);
                cylinder.transform.rotation = Quaternion.LookRotation(Vector3.down, Vector3.up);
                Renderer rendCylinder = cylinder.GetComponent<Renderer> ();
                rendCylinder.material = new Material(Shader.Find("Sprites/Default"));
                rendCylinder.material.color = color;

                GameObject cone = GameObject.CreatePrimitive(PrimitiveType.Sphere);
                cone.GetComponent<Collider>().enabled = false;
                cone.GetComponent<MeshFilter>().mesh = CreateConeMesh(10, 0.05f, 0.1f);
                cone.transform.SetParent(cylinder.transform, true);
                Renderer rendCone = cone.GetComponent<Renderer>();
                rendCone.material = new Material(Shader.Find("Sprites/Default"));
                rendCone.material.color = color;

                cylinder.transform.rotation = Quaternion.LookRotation(Vector3.back, Vector3.up);

                GameObject arrow = new GameObject("CUE-Arrow");
                cylinder.transform.SetParent(arrow.transform, true);
                arrow.transform.localScale = arrowSize;
                arrow.transform.position = arrowPosition;
                arrow.transform.rotation = arrowRotation;
                arrow.transform.position += 0.5f * arrow.transform.forward; // Move the arrow forward so the cylinder starts on the wanted position

                return arrow;
            }
            catch {
                return FallbackArrow(arrowPosition, arrowRotation, color);
            }
        }

        private static GameObject FallbackArrow(Vector3 arrowPosition, Quaternion arrowRotation, Color color){
            GameObject cylinder = GameObject.CreatePrimitive(PrimitiveType.Cylinder);
            cylinder.GetComponent<Collider>().enabled = false;
            Renderer rendCylinder = cylinder.GetComponent<Renderer> ();
            rendCylinder.material = new Material(Shader.Find("Sprites/Default"));
            rendCylinder.material.color = color;
            cylinder.transform.localScale = new Vector3(0.025f, 0.15f, 0.025f);

            GameObject sphere = GameObject.CreatePrimitive(PrimitiveType.Sphere);
            sphere.GetComponent<Collider>().enabled = false;
            
            sphere.transform.localScale = new Vector3(0.05f, 0.05f, 0.05f);
            sphere.transform.position = new Vector3(0, 0.15f, 0);
            sphere.transform.SetParent(cylinder.transform, true);
            Renderer rendSphere = sphere.GetComponent<Renderer>();
            rendSphere.material = new Material(Shader.Find("Sprites/Default"));
            rendSphere.material.color = color;

            GameObject arrow = new GameObject("CUE-Arrow");
            cylinder.transform.SetParent(arrow.transform, true);
            arrow.transform.position = arrowPosition;

            Vector3 arrowRotEulerAngles = arrowRotation.eulerAngles;
            arrowRotEulerAngles.x += 90f;
            arrow.transform.rotation = Quaternion.Euler(arrowRotEulerAngles);

            arrow.transform.position += 0.15f * arrow.transform.up; // Move the arrow forward so the cylinder starts on the wanted position

            return arrow;
        }
      
        // Patch the light class so we can color the meshes we use to visualize them the same color.
        public static void PatchLights(){
            try
            {
                PropertyInfo lightColorProperty = typeof(Light).GetProperty("color");
                MethodInfo setLightColorMethod = lightColorProperty.GetSetMethod();
#if CPP
                if (IL2CPPUtils.GetIl2CppMethodInfoPointerFieldForGeneratedMethod(setLightColorMethod) == null)
                    return;
#endif
                ExplorerCore.Harmony.Patch(setLightColorMethod,
                    prefix: new(AccessTools.Method(typeof(ArrowGenerator), nameof(ChangeArrowColor))));
            }
            catch { }
        }

        private static void ChangeArrowColor(Light __instance, Color __0){
            if (!__instance.name.Contains("CUE - Light"))
                return;

            Renderer[] renderers = __instance.GetComponentsInChildren<Renderer>();
            foreach (Renderer renderer in renderers)
            {
                renderer.material.color = __0;
            }
        }

        static Mesh CreateConeMesh(int subdivisions, float radius, float height){
            Mesh mesh = new Mesh();

            Vector3[] vertices = new Vector3[subdivisions + 2];
            Vector2[] uv = new Vector2[vertices.Length];
            int[] triangles = new int[(subdivisions * 2) * 3];

            vertices[0] = Vector3.zero;
            uv[0] = new Vector2(0.5f, 0f);
            for(int i = 0, n = subdivisions - 1; i < subdivisions; i++) {
                float ratio = (float)i / n;
                float r = ratio * (Mathf.PI * 2f);
                float x = Mathf.Cos(r) * radius;
                float z = Mathf.Sin(r) * radius;
                vertices[i + 1] = new Vector3(x, 0f, z);

                uv[i + 1] = new Vector2(ratio, 0f);
            }
            vertices[subdivisions + 1] = new Vector3(0f, height, 0f);
            uv[subdivisions + 1] = new Vector2(0.5f, 1f);

            // construct bottom

            for(int i = 0, n = subdivisions - 1; i < n; i++) {
                int offset = i * 3;
                triangles[offset] = 0; 
                triangles[offset + 1] = i + 1; 
                triangles[offset + 2] = i + 2; 
            }

            // construct sides

            int bottomOffset = subdivisions * 3;
            for(int i = 0, n = subdivisions - 1; i < n; i++) {
                int offset = i * 3 + bottomOffset;
                triangles[offset] = i + 1; 
                triangles[offset + 1] = subdivisions + 1; 
                triangles[offset + 2] = i + 2; 
            }

            mesh.vertices = vertices;
            mesh.uv = uv;
            mesh.triangles = triangles;
            mesh.RecalculateBounds();
            mesh.RecalculateNormals();

            return mesh;
        }


        static Mesh CreateCylinderMesh(float radius = 1, int iterations = 50, int lenggth = 10, float gap = 0.5f){
            // Making vertices
            int num = 0;
            Vector3[] vertices;
            int[] tris;
            int[] FinalTri;
            int[] firstplane;

            float x;
            float y;
            float z = 0;
            int i;
            int p = 0;
            float angle;
    
            vertices = new Vector3[(iterations * lenggth) + 2];
            int tempo = 0;
            vertices[vertices.Length - 2] = Vector3.zero;
    
            while (p < lenggth)
            {
                i = 0;
                while (i < iterations)
                {
                    angle = (i * 1.0f) / iterations * Mathf.PI * 2;
                    x = Mathf.Sin(angle) * radius;
                    y = Mathf.Cos(angle) * radius;
                    vertices[tempo] = new Vector3(x, y, z);
                    //GameObject go = Instantiate(cube, vertices[tempo], Quaternion.identity);
                    //go.name = num.ToString();
                    i++;
                    num++;
                    tempo += 1;
                }
                z += gap;
                p++;
            }
    
            vertices[vertices.Length - 1] = new Vector3(0, 0, vertices[vertices.Length - 3].z);

            Mesh mesh = new Mesh();
            mesh.vertices = vertices;

            // Making normals

            int j = 0;
            Vector3[] normals = new Vector3[num + 2];
            while (j < num)
            {
                normals[j] = Vector3.forward;
                j++;
            }
            mesh.normals = normals;

            // Making triangles
    
            j = 0;
            tris = new int[((3 * (lenggth - 1) * iterations) * 2) + 3];
            while (j < (lenggth - 1) * iterations)
            {
                tris[j * 3] = j;
                if ((j + 1) % iterations == 0)
                {
                    tris[j * 3 + 1] = 1 + j - iterations;
                }
                else
                {
                    tris[j * 3 + 1] = 1 + j;
                }
                tris[j * 3 + 2] = iterations + j;
                j++;
            }
            int IndexofNewTriangles = -1;
    
            for (int u = (tris.Length - 3) / 2; u < tris.Length - 6; u += 3)
            {
                //mesh.RecalculateTangents();
                if ((IndexofNewTriangles + 2) % iterations == 0)
                {
                    tris[u] = IndexofNewTriangles + iterations * 2 + 1;
                }
                else
                    tris[u] = IndexofNewTriangles + iterations + 1;
    
                tris[u + 1] = IndexofNewTriangles + 2;
                tris[u + 2] = IndexofNewTriangles + iterations + 2;
                IndexofNewTriangles += 1;
            }
            tris[tris.Length - 3] = 0;
            tris[tris.Length - 2] = (iterations * 2) - 1;
            tris[tris.Length - 1] = iterations;
    
            firstplane = new int[(iterations * 3) * 2];
            int felmnt = 0;
            for (int h = 0; h < firstplane.Length / 2; h += 3)
            {
    
                firstplane[h] = felmnt;
    
                if (felmnt + 1 != iterations)
                    firstplane[h + 1] = felmnt + 1;
                else
                    firstplane[h + 1] = 0;
                firstplane[h + 2] = vertices.Length - 2;
                felmnt += 1;
            }
    
            felmnt = iterations * (lenggth - 1);
            for (int h = firstplane.Length / 2; h < firstplane.Length; h += 3)
            {
    
                firstplane[h] = felmnt;
    
                if (felmnt + 1 != iterations * (lenggth - 1))
                    firstplane[h + 1] = felmnt + 1;
                else
                    firstplane[h + 1] = iterations * (lenggth - 1);
                firstplane[h + 2] = vertices.Length - 1;
                felmnt += 1;
            }
    
            firstplane[firstplane.Length - 3] = iterations * (lenggth - 1);
            firstplane[firstplane.Length - 2] = vertices.Length - 3;
            firstplane[firstplane.Length - 1] = vertices.Length - 1;
    
            FinalTri = new int[tris.Length + firstplane.Length];
    
            int k = 0, l = 0;
            for (k = 0, l = 0; k < tris.Length; k++)
            {
                FinalTri[l++] = tris[k];
            }
            for (k = 0; k < firstplane.Length; k++)
            {
                FinalTri[l++] = firstplane[k];
            }
    
            mesh.triangles = FinalTri;
            mesh.Optimize();
            mesh.RecalculateNormals();
            //mf.mesh = mesh;

            return mesh;
        }
    }
}
