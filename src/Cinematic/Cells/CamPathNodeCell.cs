using UnityEngine;
using UniverseLib.UI;
using UniverseLib.UI.Models;
using UniverseLib.UI.Widgets.ScrollView;

namespace UnityExplorer.UI.Panels
{
    public class CamPathNodeCell : ICell
    {
        // ICell
        public float DefaultHeight => 25f;
        public GameObject UIRoot { get; set; }
        public RectTransform Rect { get; set; }
        public Text indexLabel;

        public bool Enabled => UIRoot.activeSelf;
        public void Enable() => UIRoot.SetActive(true);
        public void Disable() => UIRoot.SetActive(false);

        public int index;
        public CatmullRom.CatmullRomPoint point;

        public virtual GameObject CreateContent(GameObject parent)
        {
            UIRoot = UIFactory.CreateUIObject("NodeCell", parent, new Vector2(25, 25));
            Rect = UIRoot.GetComponent<RectTransform>();
            UIFactory.SetLayoutGroup<HorizontalLayoutGroup>(UIRoot, false, false, true, true, 3);
            UIFactory.SetLayoutElement(UIRoot, minHeight: 25, minWidth: 50, flexibleWidth: 9999);

            indexLabel = UIFactory.CreateLabel(UIRoot, "IndexLabel", "i", TextAnchor.MiddleCenter, Color.white, false, 12);
            UIFactory.SetLayoutElement(indexLabel.gameObject, minHeight: 25, minWidth: 30, flexibleWidth: 40);

            ButtonRef moveToCameraButton = UIFactory.CreateButton(UIRoot, "Copy Camera pos and rot", "Copy Camera pos and rot");
            UIFactory.SetLayoutElement(moveToCameraButton.GameObject, minWidth: 130, minHeight: 25, flexibleWidth: 9999);
            moveToCameraButton.OnClick += () => {
                point.rotation = FreeCamPanel.GetCameraRotation();
                point.position = FreeCamPanel.GetCameraPosition();
                GetCamPathsPanel().controlPoints[index] = point;

                GetCamPathsPanel().MaybeRedrawPath();
            };

            ButtonRef copyFovButton = UIFactory.CreateButton(UIRoot, "Copy Camera FoV", "Copy Camera FoV");
            UIFactory.SetLayoutElement(copyFovButton.GameObject, minWidth: 100, minHeight: 25, flexibleWidth: 9999);
            copyFovButton.OnClick += () => {
                Camera freecam = FreeCamPanel.GetFreecam();
                if (freecam != null) {
                    point.fov = freecam.fieldOfView;
                    GetCamPathsPanel().controlPoints[index] = point;
                }
            };

            ButtonRef moveToPointButton = UIFactory.CreateButton(UIRoot, "Move Cam to Node", "Move Cam to Node");
            UIFactory.SetLayoutElement(moveToPointButton.GameObject, minWidth: 100, minHeight: 25, flexibleWidth: 9999);
            moveToPointButton.OnClick += () => {
                FreeCamPanel.SetCameraRotation(point.rotation);
                FreeCamPanel.SetCameraPosition(point.position);
                FreeCamPanel.SetFOV(point.fov);
            };

            ButtonRef moveUpButton = UIFactory.CreateButton(UIRoot, "MoveUp", "▲");
            UIFactory.SetLayoutElement(moveUpButton.GameObject, minWidth: 25, minHeight: 25, flexibleWidth: 0);
            moveUpButton.OnClick += () => {
                int pointIndex = GetCamPathsPanel().controlPoints.IndexOf(point);
                if (pointIndex != 0){
                    CatmullRom.CatmullRomPoint temporalPoint = GetCamPathsPanel().controlPoints[pointIndex - 1];
                    GetCamPathsPanel().controlPoints[pointIndex - 1] = point;
                    GetCamPathsPanel().controlPoints[pointIndex] = temporalPoint;
                }
                GetCamPathsPanel().nodesScrollPool.Refresh(true, false);
                GetCamPathsPanel().MaybeRedrawPath();
            };

            ButtonRef moveDownButton = UIFactory.CreateButton(UIRoot, "MoveDown", "▼");
            UIFactory.SetLayoutElement(moveDownButton.GameObject, minWidth: 25, minHeight: 25, flexibleWidth: 0);
            moveDownButton.OnClick += () => {
                int pointIndex = GetCamPathsPanel().controlPoints.IndexOf(point);
                if (pointIndex != GetCamPathsPanel().controlPoints.Count - 1){
                    CatmullRom.CatmullRomPoint temporalPoint = GetCamPathsPanel().controlPoints[pointIndex + 1];
                    GetCamPathsPanel().controlPoints[pointIndex + 1] = point;
                    GetCamPathsPanel().controlPoints[pointIndex] = temporalPoint;
                }
                GetCamPathsPanel().nodesScrollPool.Refresh(true, false);
                GetCamPathsPanel().MaybeRedrawPath();
            };

            ButtonRef destroyButton = UIFactory.CreateButton(UIRoot, "Delete", "Delete");
            UIFactory.SetLayoutElement(destroyButton.GameObject, minWidth: 30, minHeight: 25, flexibleWidth: 9999);
            destroyButton.OnClick += () => {
                int deletedIndex = GetCamPathsPanel().controlPoints.IndexOf(point);
                GetCamPathsPanel().controlPoints.Remove(point);
                GetCamPathsPanel().OnNodeDeleted(deletedIndex);
                GetCamPathsPanel().nodesScrollPool.Refresh(true, false);
                GetCamPathsPanel().MaybeRedrawPath();
            };

            return UIRoot;
        }

        public UnityExplorer.UI.Panels.CamPaths GetCamPathsPanel(){
            return UIManager.GetPanel<UnityExplorer.UI.Panels.CamPaths>(UIManager.Panels.CamPaths);
        }
    }
}
