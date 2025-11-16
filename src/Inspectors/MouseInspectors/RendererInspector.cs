using System.Collections;
using UnityExplorer.UI;
using UnityExplorer.UI.Panels;

namespace UnityExplorer.Inspectors.MouseInspectors
{
    public class RendererInspector : MouseInspectorBase
    {
        private static Camera MainCamera;
        public static readonly List<GameObject> LastHitObjects = new();
        private static readonly List<GameObject> currentHitObjects = new();

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
                $"<b>World Inspector ({MainCamera.name})</b> (press <b>ESC</b> to cancel)"
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
            RendererInspectorResultPanel panel = UIManager.GetPanel<RendererInspectorResultPanel>(UIManager.Panels.RendererInspectorResults);
            panel.SetActive(true);
            panel.ShowResults();
        }

        /// <summary>
        /// Attempts to ensure that MainCamera is assigned. If not then attempts to find it.
        /// If no cameras are available, logs a warning and returns null.
        /// </summary>
        private static Camera EnsureMainCamera()
        {
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

            Ray ray = MainCamera.ScreenPointToRay(mousePos);
            var tmp = new Vector3(ray.origin.x, ray.origin.y, 0f);
            ray.origin = tmp;

            RaycastHit2D[] hits = Physics2D.RaycastAll(ray.origin, ray.direction, Mathf.Infinity, Physics2D.DefaultRaycastLayers);

            if (hits.Length > 0)
            {
                foreach (var hit in hits)
                {
                    if (hit.collider != null)
                    {
                        if (hit.collider.gameObject)
                        {
                            currentHitObjects.Add(hit.collider.gameObject);
                        }
                    }
                }
            }

            OnHitGameObject();
        }

        internal void OnHitGameObject()
        {
            if (currentHitObjects.Any())
                MouseInspector.Instance.UpdateObjectNameLabel($"Click to view World Objects under mouse: {currentHitObjects.Count}");
            else
                MouseInspector.Instance.UpdateObjectNameLabel($"No World objects under mouse.");

        }

        public override void OnEndInspect()
        {
            // not needed
        }
    }
}
