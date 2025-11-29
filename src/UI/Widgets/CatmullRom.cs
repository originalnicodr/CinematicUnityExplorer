using UnityExplorer.UI.Panels;

using System;
using System.Threading;

#if UNHOLLOWER
using UnhollowerRuntimeLib;
#endif
#if CPP
#if INTEROP
using Il2CppInterop.Runtime.Attributes;
using Il2CppInterop.Runtime.Injection;
#else
using UnhollowerBaseLib.Attributes;
#endif
#endif

using UnityExplorer.UI;

namespace UnityExplorer.CatmullRom
{
    // Struct to keep position, rotation and fov of a spline point
    [System.Serializable]
    public struct CatmullRomPoint
    {
        public Vector3 position;
        public Quaternion rotation;
        public float fov;

        public CatmullRomPoint(Vector3 position, Quaternion rotation, float fov)
        {
            this.position = position;
            this.rotation = rotation;
            this.fov = fov;
        }

        public CatmullRomPoint(Vector3 position, Vector4 rotation, float fov)
        {
            this.position = position;
            this.rotation = Vector4ToQuaternion(rotation);
            this.fov = fov;
        }

        public static Vector4 QuaternionToVector4(Quaternion q){
            return new Vector4(q.x, q.y, q.z, q.w);
        }

        public static Quaternion Vector4ToQuaternion(Vector4 v){
            return new Quaternion(v.x, v.y, v.z, v.w);
        }

        public static CatmullRomPoint operator +(CatmullRomPoint a, CatmullRomPoint b)
        {
            Vector4 ra = QuaternionToVector4(a.rotation);
            Vector4 rb = QuaternionToVector4(b.rotation);

            Vector4 newRot = ra + rb;
            return new CatmullRomPoint(a.position + b.position, Vector4ToQuaternion(newRot), a.fov + b.fov); // what to do with the fov??
        }

        public static CatmullRomPoint operator -(CatmullRomPoint a, CatmullRomPoint b)
        {
            Vector4 newRot = QuaternionToVector4(a.rotation) - QuaternionToVector4(b.rotation);
            return new CatmullRomPoint(a.position - b.position, Vector4ToQuaternion(newRot), a.fov - b.fov); // what to do with the fov??
        }

        public static CatmullRomPoint operator /(CatmullRomPoint a, float b)
        {
            //Vector4 newRot = QuaternionToVector4(a.rotation)/b;
            //return new CatmullRomPoint(a.position/b, Vector4ToQuaternion(newRot), a.fov / b); // what to do with the fov??
            return new CatmullRomPoint(a.position/b, a.rotation, a.fov);
        }

        public static CatmullRomPoint operator *(CatmullRomPoint a, float b)
        {
            //Vector4 newRot = QuaternionToVector4(a.rotation)*b;
            //return new CatmullRomPoint(a.position*b, Vector4ToQuaternion(newRot), a.fov * b); // what to do with the fov??
            return new CatmullRomPoint(a.position*b, a.rotation, a.fov); // what to do with the fov??
        }

        public CatmullRomPoint Normalize()
        {
            position.Normalize();
            return this;
        }
    }

    public class CatmullRomMover : MonoBehaviour
    {
#if CPP
        static CatmullRomMover()
        {
            ClassInjector.RegisterTypeInIl2Cpp<CatmullRomMover>();
        }

        public CatmullRomMover(IntPtr ptr) : base(ptr) { }
#endif

        public bool playingPath;
        private bool closedLoop;
        private CatmullRomPoint[] splinePoints;
        // The point the camera will be following along the path
        private List<CatmullRomPoint> lookaheadPoints = new List<CatmullRomPoint>();
        private CatmullRomPoint lookahead;
        // How smooth the path is (beware of not making too many calculations per update tho)
        float lookaheadDelta;
        float time;
        float speed;
        // Current position in the path, from 0 to 1
        float delta;

        float tension = 0;
        float alpha = 0.5f;

        private CatmullRomPoint pauseCamCache;

        public CatmullRomMover(){
            splinePoints = new CatmullRomPoint[] { };
            lookaheadPoints = new List<CatmullRomPoint>();
        }

        public void StartPath(){
            if(splinePoints == null || splinePoints.Length <= 2)
            {
                int nodeCount = splinePoints?.Length ?? 0;
                ExplorerCore.LogWarning($"Cannot start camera path: Need at least 3 nodes, but only {nodeCount} node(s) exist. Please add more nodes to create a path.");
                throw new ArgumentException("Catmull Rom Error: Too few spline points!");
            }
            else {
                playingPath = true;
                delta = lookaheadDelta;
                MoveCameraToPoint(splinePoints[0]);
                lookahead = lookaheadPoints[1];
            }
        }

        public void TogglePause(){
            playingPath = !playingPath;
            if (!playingPath){
                pauseCamCache = GetCurrentPoint();
            } else {
                MoveCameraToPoint(pauseCamCache);
            }
            
        }

        public bool IsPaused(){
            return playingPath;
        }

        public void Stop(){
            playingPath = false;
            delta = lookaheadDelta;
            lookahead = GetPointFromPath(delta);
            MoveCameraToPoint(splinePoints[0]);
            pauseCamCache = GetCurrentPoint();
        }

        public void setClosedLoop(bool newClosedLoop){
            closedLoop = newClosedLoop;
        }

#if CPP
        [HideFromIl2Cpp]
#endif
        public void setSplinePoints(CatmullRomPoint[] newSplinePoints){
            splinePoints = newSplinePoints;
        }

        public void setTime(float newTime){
            time = newTime;
        }

        public void setCatmullRomVariables(float newAlpha, float newTension){
            alpha = newAlpha;
            tension = newTension;
        }

        void Update(){
            if (playingPath) AdvanceMover(Time.fixedDeltaTime);
        }

        private CatmullRomPoint GetCurrentPoint(){
            Vector3 camPos = FreeCamPanel.GetCameraPosition();
            Quaternion camRot = FreeCamPanel.GetCameraRotation();

            return new CatmullRomPoint(camPos, camRot, FreeCamPanel.GetFreecam().fieldOfView);
        }

        private void AdvanceMover(float dt){
            // We use 0.01665f (60fps) in place of Time.DeltaTime so the camera moves at fullspeed even in slow motion.
            // Units to move
            float move = 0.01666666666f * speed;
            while (move > 0.0) {
                CatmullRomPoint currentPoint = GetCurrentPoint();
                // Distance between target and current position
                float room = Vector3.Distance(lookahead.position, currentPoint.position); 

                // How much we're actually moving this iteration
                // Move the whole distance to the lookahead, or would the move variable cut us short
                float actual = Math.Min(move, room);

                MoveCameraStep(currentPoint, actual, room);

                // Update move to be the remaining amount we need to move this frame
                move -= actual;

                currentPoint = GetCurrentPoint();

                // Update room to check if we need a new lookahead
                room = Vector3.Distance(lookahead.position, currentPoint.position); 
                // While ends, need another lookaheadDelta for the next Update
                // It will do, at most, 2 iterations
                // We check a very small number instead of zero because if not it can get stuck in an infinite loop
                while (room <= 0.0002f) {
                    // If the camera reached the last lookAhead we stop it
                    if (delta >= 1){
                        PathFinished();
                    }
                    delta = Math.Min(delta + lookaheadDelta, 1);

                    // May not be as good as calculating GetPointFromPath(delta) itself, but since its a lookahead and not the actual cam position
                    // the movement will still be smooth.
                    int lookaheadIndex = (int)((lookaheadPoints.Count - 1) * delta);
                    lookahead = lookaheadPoints[lookaheadIndex];
                    room = Vector3.Distance(lookahead.position, currentPoint.position); 
                }
            }
        }

        public void MoveCameraStep(CatmullRomPoint currentPoint, float actual, float room){
            CatmullRomPoint direction = (lookahead - currentPoint).Normalize() * actual;
            Vector4 ra = CatmullRomPoint.QuaternionToVector4(currentPoint.rotation);
            Vector4 rb = CatmullRomPoint.QuaternionToVector4(direction.rotation);
            rb = rb * actual / room;

            Vector4 newRot = ra + rb;
            newRot.Normalize();

            FreeCamPanel.SetCameraRotation(CatmullRomPoint.Vector4ToQuaternion(newRot));
            FreeCamPanel.SetCameraPosition(FreeCamPanel.GetCameraPosition() + direction.position);
            FreeCamPanel.SetFOV(FreeCamPanel.GetFreecam().fieldOfView + direction.fov * actual / room);
        }

        public void MoveCameraToPoint(CatmullRomPoint newPoint){
            FreeCamPanel.SetCameraRotation(newPoint.rotation);
            FreeCamPanel.SetCameraPosition(newPoint.position);

            FreeCamPanel.SetFOV(newPoint.fov);
        }

        private CatmullRomPoint GetPointFromPath(float d)
        {
            Vector3 p0, p1, p2, p3; //Previous position, start position, end position, next position
            Vector4 r0, r1, r2, r3; //Previous rotation, start rotation, end rotation, next rotation
            float fov0, fov1;

            // First for loop goes through each individual control point and connects it to the next, so 0-1, 1-2, 2-3 and so on
            int closedAdjustment = closedLoop ? 0 : 1;

            int currentPoint = (int)((splinePoints.Length - closedAdjustment) * d);
            
            bool closedLoopFinalPoint = (closedLoop && currentPoint == splinePoints.Length - 1);

            // Had tu add "splinePoints.Length" because C# mod doesnt work well with negative values
            int previousPoint = closedLoop ? (currentPoint + splinePoints.Length - 1)%(splinePoints.Length) : System.Math.Max(currentPoint - 1, 0);
            int endSegmentPoint = closedLoop ? (currentPoint + 1)%(splinePoints.Length) : System.Math.Min(currentPoint + 1, splinePoints.Length - 1);
            int nextPoint = closedLoop ? (currentPoint + 2)%(splinePoints.Length) : System.Math.Min(currentPoint + 2, splinePoints.Length - 1);

            // Ideally we should really loop over tho.
            if (d >= 1) return closedLoop ? splinePoints[0] : splinePoints[splinePoints.Length - 1];

            p0 = splinePoints[previousPoint].position;
            r0 = CatmullRomPoint.QuaternionToVector4(splinePoints[previousPoint].rotation);

            p1 = splinePoints[currentPoint].position;
            r1 = CatmullRomPoint.QuaternionToVector4(splinePoints[currentPoint].rotation);

            p2 = splinePoints[endSegmentPoint].position;
            r2 = CatmullRomPoint.QuaternionToVector4(splinePoints[endSegmentPoint].rotation);

            p3 = splinePoints[nextPoint].position;
            r3 = CatmullRomPoint.QuaternionToVector4(splinePoints[nextPoint].rotation);

            // Check if we are using the shortest path on the rotation. If not, change each rotation to represent that shortest path.
            if (Vector4.Dot(r0, r1) < 0)
                r1 = - r1;

            if (Vector4.Dot(r1, r2) < 0)
                r2 = - r2;

            if (Vector4.Dot(r2, r3) < 0)
                r3 = - r3;

            fov0 = splinePoints[currentPoint].fov;
            fov1 = splinePoints[endSegmentPoint].fov;

            float t = ((splinePoints.Length - closedAdjustment) * d) % 1;

            CatmullRomPoint newPoint = Evaluate(p0, p1, p2, p3, r0, r1, r2, r3, fov0, fov1, t);

            return newPoint;
        }

        private CatmullRomPoint Evaluate(Vector3 p0, Vector3 p1, Vector3 p2, Vector3 p3, Vector4 r0, Vector4 r1, Vector4 r2, Vector4 r3, float fovStart, float fovEnd, float t)
        {
            Vector3 position = CatmullRomInterpolation(p0, p1, p2, p3, t);
            Vector4 v4rot = CatmullRomInterpolation(r0, r1, r2, r3, t);
            Quaternion rotation = new Quaternion(v4rot.x, v4rot.y, v4rot.z, v4rot.w);
            float fov = Mathf.SmoothStep(fovStart, fovEnd, t);

            return new CatmullRomPoint(position, rotation, fov);
        }

        // Implementation from: https://qroph.github.io/2018/07/30/smooth-paths-using-catmull-rom-splines.html
        private Vector3 CatmullRomInterpolation(Vector3 p0, Vector3 p1, Vector3 p2, Vector3 p3, float t){
            float t01 = (float) System.Math.Pow((double) Vector3.Distance(p0, p1), (double) alpha);
            float t12 = (float) System.Math.Pow((double) Vector3.Distance(p1, p2), (double) alpha);
            float t23 = (float) System.Math.Pow((double) Vector3.Distance(p2, p3), (double) alpha);

            Vector3 v1 = t01 != 0 ? (p1 - p0) / t01 : new Vector3(0, 0, 0);
            Vector3 v2 = t23 != 0 ? (p3 - p2) / t23 : new Vector3(0, 0, 0);
            
            Vector3 m1 = (1.0f - tension) * (p2 - p1 + t12 * (v1 - (p2 -p0) / (t01 + t12)));
            Vector3 m2 = (1.0f - tension) * (p2 - p1 + t12 * (v2 - (p3 -p1) / (t12 + t23)));

            Vector3 a = 2.0f * (p1 - p2) + m1 + m2;
            Vector3 b = -3.0f * (p1 - p2) - m1 - m1 - m2;
            Vector3 c = m1;
            Vector3 d = p1;

            return a * t * t * t + b * t * t + c * t + d;
        }

        private Vector4 CatmullRomInterpolation(Vector4 r0, Vector4 r1, Vector4 r2, Vector4 r3, float t){
            float t01 = (float) System.Math.Pow((double) Vector4.Distance(r0, r1), (double) alpha);
            float t12 = (float) System.Math.Pow((double) Vector4.Distance(r1, r2), (double) alpha);
            float t23 = (float) System.Math.Pow((double) Vector4.Distance(r2, r3), (double) alpha);

            Vector4 v1 = t01 != 0 ? (r1 - r0) / t01 : new Vector4(0, 0, 0, 0);
            Vector4 v2 = t23 != 0 ? (r3 - r2) / t23 : new Vector4(0, 0, 0, 0);
            
            Vector4 m1 = (1.0f - tension) * (r2 - r1 + t12 * (v1 - (r2 -r0) / (t01 + t12)));
            Vector4 m2 = (1.0f - tension) * (r2 - r1 + t12 * (v2 - (r3 -r1) / (t12 + t23)));

            Vector4 a = 2.0f * (r1 - r2) + m1 + m2;
            Vector4 b = -3.0f * (r1 - r2) - m1 - m1 - m2;
            Vector4 c = m1;
            Vector4 d = r1;

            return a * t * t * t + b * t * t + c * t + d;
        }

        public void CalculateLookahead() {
            // We assume 60 locked fps.
            float frames = time * 60;
            float pathLength = 0;
            if (lookaheadPoints.Count > 0) lookaheadPoints.Clear();

            for (int i = 0; i <= frames; i++){
                CatmullRomPoint newPoint = GetPointFromPath(i / frames);
                if (i != 0 && Vector4.Dot(CatmullRomPoint.QuaternionToVector4(lookaheadPoints[i - 1].rotation), CatmullRomPoint.QuaternionToVector4(newPoint.rotation)) < 0){
                    newPoint.rotation = CatmullRomPoint.Vector4ToQuaternion(- CatmullRomPoint.QuaternionToVector4(newPoint.rotation));
                }
                lookaheadPoints.Add(newPoint);

                if (i != 0){
                    pathLength += Vector3.Distance(lookaheadPoints[i - 1].position, lookaheadPoints[i].position);
                }
            }

            speed = pathLength / time;
            lookaheadDelta = speed / 1000;

            //ExplorerCore.LogWarning($"Calculating speed {speed}");
            //ExplorerCore.LogWarning($"for time {time}");
            //ExplorerCore.LogWarning($"on path length {pathLength}");
            //ExplorerCore.LogWarning($"lookaheadDelta {lookaheadDelta}");
        }

#if CPP
        [HideFromIl2Cpp]
#endif
        public List<CatmullRomPoint> GetLookaheadPoints(){
            return lookaheadPoints;
        }

        void PathFinished(){
            playingPath = false;
            delta = 0;
            pauseCamCache = GetCurrentPoint();

            if (UIManager.GetPanel<CamPaths>(UIManager.Panels.CamPaths).pauseOnFinish && !UIManager.GetTimeScaleWidget().IsPaused()){
                UIManager.GetTimeScaleWidget().PauseToggle();
            }
        }
    } 
}
