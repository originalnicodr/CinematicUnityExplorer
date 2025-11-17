using CinematicUnityExplorer.Inspectors;
using CinematicUnityExplorer.LineDrawing;
using System.Collections;
using UnityExplorer.UI;
using UnityExplorer.UI.Panels;

namespace UnityExplorer.Inspectors.MouseInspectors
{
    public class HudInspector : MouseInspectorBase
    {
        private static Camera MainCamera;
        public static readonly List<GameObject> LastHitObjects = new();
        private static readonly List<GameObject> currentHitObjects = new();
        private List<TriangleVertices> vert = new List<TriangleVertices>(); // 使用你的自定義結構體
        public Renderer[] rendererCache = null;
        public float cacheTime = 0;

        public List<LineData> Lines { get; } = new();

        private void AddLine(Vector2 a, Vector2 b, float z)
        {
            //UnityExplorerPlus.Instance.settings.enableRendererBox.Value
            if (!true) return;
            var c = Mathf.RoundToInt((z * 50) % 255) / 255f;
            var color = new Color(1, c, c, 1);
            Lines.Add(new(LocalToScreenPoint(a), LocalToScreenPoint(b), color, Mathf.RoundToInt(z * 100)));
        }

        private Vector2 LocalToScreenPoint(Vector3 point)
        {
            Vector2 result = MainCamera.WorldToScreenPoint(point);
            return new Vector2((int)Math.Round(result.x), (int)Math.Round(Screen.height - result.y));
        }

        public static bool TestPointInTrig(Vector2 a, Vector2 b, Vector2 c, Vector2 p)
        {
            var signOfTrig = (b.x - a.x) * (c.y - a.y) - (b.y - a.y) * (c.x - a.x);
            var signOfAB = (b.x - a.x) * (p.y - a.y) - (b.y - a.y) * (p.x - a.x);
            var signOfCA = (a.x - c.x) * (p.y - c.y) - (a.y - c.y) * (p.x - c.x);
            var signOfBC = (c.x - b.x) * (p.y - c.y) - (c.y - b.y) * (p.x - c.x);
            var d1 = signOfAB * signOfTrig > 0;
            var d2 = signOfCA * signOfTrig > 0;
            var d3 = signOfBC * signOfTrig > 0;
            return d1 && d2 && d3;
        }

        public override void OnBeginMouseInspect()
        {

            if (!EnsureMainCamera())
            {
                ExplorerCore.LogWarning("No MainCamera found! Cannot inspect world!");
                return;
            }
        }

        /// <summary>
        /// Assigns it as the MainCamera and updates the inspector title.
        /// </summary>
        /// <param name="cam">The camera to assign.</param>
        private static void AssignCamAndUpdateTitle(Camera cam)
        {
            MainCamera = cam;
            MouseInspector.Instance.UpdateInspectorTitle(
                $"<b>Hud Inspector ({MainCamera.name})</b> (press <b>ESC</b> to cancel)"
            );
        }

        public override void ClearHitData()
        {
            currentHitObjects.Clear();
        }

        public override void OnSelectMouseInspect(Action<GameObject> inspectorAction)
        {
            LastHitObjects.Clear();
            LastHitObjects.AddRange(currentHitObjects);
            RuntimeHelper.StartCoroutine(SetPanelActiveCoro());
        }

        IEnumerator SetPanelActiveCoro()
        {
            yield return null;
            HudInspectorResultsPanel panel = UIManager.GetPanel<HudInspectorResultsPanel>(UIManager.Panels.HudInspectorResults);
            panel.SetActive(true);
            panel.ShowResults();
        }

        /// <summary>
        /// Attempts to ensure that MainCamera is assigned. If not then attempts to find it.
        /// If no cameras are available, logs a warning and returns null.
        /// </summary>
        private static Camera EnsureMainCamera()
        {
            foreach (var camera in Camera.allCameras)
            {
                if (camera.name == "HudCamera")
                {
                    AssignCamAndUpdateTitle(camera);
                    return MainCamera;
                }
            }

            if (MainCamera)
            {
                // We still call this in case the last title was from the UIInspector
                AssignCamAndUpdateTitle(MainCamera);
                return MainCamera;
            }

            if (Camera.main)
            {
                AssignCamAndUpdateTitle(Camera.main);
                return MainCamera;
            }

            ExplorerCore.LogWarning("No Camera.main found, trying to find a camera named 'Main Camera' or 'MainCamera'...");
            Camera namedCam = Camera.allCameras.FirstOrDefault(c => c.name is "Main Camera" or "MainCamera");
            if (namedCam)
            {
                AssignCamAndUpdateTitle(namedCam);
                return MainCamera;
            }

            if (FreeCamPanel.inFreeCamMode && FreeCamPanel.GetFreecam())
            {
                AssignCamAndUpdateTitle(FreeCamPanel.GetFreecam());
                return MainCamera;
            }

            ExplorerCore.LogWarning("No camera named found, using the first camera created...");
            var fallbackCam = Camera.allCameras.FirstOrDefault();
            if (fallbackCam)
            {
                AssignCamAndUpdateTitle(fallbackCam);
                return MainCamera;
            }

            // If we reach here, no cameras were found at all.
            ExplorerCore.LogWarning("No valid cameras found!");
            return null;
        }

        public override void UpdateMouseInspect(Vector2 mousePos)
        {
            currentHitObjects.Clear();

            // Attempt to ensure camera each time UpdateMouseInspect is called
            // in case something changed or wasn't set initially.
            if (!EnsureMainCamera())
            {
                ExplorerCore.LogWarning("No Main Camera was found, unable to inspect world!");
                MouseInspector.Instance.StopInspect();
                return;
            }

            // - - - - - - - - - - - - - - - - - - - - - - - - - - - - - - - - 
            vert.Clear(); // 確保每次更新時都清空

            if (Time.unscaledTime - cacheTime > 0.5f || rendererCache is null)
            {
                rendererCache = UnityEngine.Object.FindObjectsOfType<Renderer>();
            }

            Lines.Clear();

            var p = GetCurrentMousePosition();

            foreach (var v in rendererCache
                .Where(x => x != null)
                .Where(x => x.isVisible)
                .Where(x => x.enabled)
                .Where(x => x.gameObject.activeInHierarchy)
                .OrderBy(x => x.transform.position.z))
            {
                Vector2 pos = v.transform.position;
                Vector3 pos3 = v.transform.position;
                var scale = new Vector3(v.transform.GetScaleX(), v.transform.GetScaleY(), 1);
                if (v is MeshRenderer mr)
                {
                    var filter = v.GetComponent<MeshFilter>();
                    if (filter == null) continue;
                    var mesh = filter.sharedMesh;
                    if (mesh == null) continue;
                    var points = mesh.vertices;
                    vert.Clear();
                    bool isTouch = false;

                    for (int i = 0; i < mesh.subMeshCount; i++)
                    {
                        var trig = filter.sharedMesh.GetTriangles(i);
                        for (int i2 = 0; i2 < trig.Length; i2 += 3)
                        {
                            var a = points[trig[i]].MultiplyElements(scale) + (Vector3)pos;
                            var b = points[trig[i + 1]].MultiplyElements(scale) + (Vector3)pos;
                            var c = points[trig[i + 2]].MultiplyElements(scale) + (Vector3)pos;
                            vert.Add(new TriangleVertices(a, b, c));
                            if (!isTouch && TestPointInTrig(a, b, c, p))
                            {
                                currentHitObjects.Add(v.gameObject);
                                isTouch = true;
                            }
                        }
                    }
                    if (isTouch)
                    {
                        foreach (var triangle in vert)
                        {
                            AddLine(triangle.VertexA, triangle.VertexB, pos3.z);
                            AddLine(triangle.VertexB, triangle.VertexC, pos3.z);
                            AddLine(triangle.VertexA, triangle.VertexC, pos3.z);
                        }
                    }
                }
                else if (v is SpriteRenderer sprite)
                {
                    if (sprite.sprite == null) continue;
                    var points = sprite.sprite.vertices;
                    var trig = sprite.sprite.triangles;
                    vert.Clear();
                    bool isTouch = false;
                    for (int i = 0; i < trig.Length; i += 3)
                    {
                        var a = points[trig[i]].MultiplyElements(scale) + pos;
                        var b = points[trig[i + 1]].MultiplyElements(scale) + pos;
                        var c = points[trig[i + 2]].MultiplyElements(scale) + pos;
                        vert.Add(new TriangleVertices(a, b, c));
                        if (!isTouch && TestPointInTrig(a, b, c, p))
                        {
                            currentHitObjects.Add(v.gameObject);
                            isTouch = true;
                        }
                    }
                    if (isTouch)
                    {
                        foreach (var (a, b, c) in vert)
                        {
                            AddLine(a, b, pos3.z);
                            AddLine(b, c, pos3.z);
                            AddLine(a, c, pos3.z);
                        }
                    }
                }
            }

            OnHitGameObject();
        }

        internal void OnHitGameObject()
        {
            if (currentHitObjects.Any())
                MouseInspector.Instance.UpdateObjectNameLabel($"Click to view Renderer Objects under mouse: {currentHitObjects.Count}");
            else
                MouseInspector.Instance.UpdateObjectNameLabel($"No World objects under mouse.");

        }

        public override void OnEndInspect()
        {
            // not needed
        }

        public static Vector2 GetCurrentMousePosition()
        {
            var cam = MainCamera;
            var mousePos = Input.mousePosition;
            mousePos.z = cam.WorldToScreenPoint(Vector3.zero).z;
            return cam.ScreenToWorldPoint(mousePos);
        }
    }
}
