using MonoMod.Utils;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Runtime.InteropServices;
using System.Text;
using UnityExplorer;

namespace CinematicUnityExplorer.Cinematic
{
    // StepCommand is basically the offset of step_left and step_up, what IGCS sends to move the camera.
    using StepCommand = Mono.CSharp.Tuple<float, float>;

    [StructLayout(LayoutKind.Sequential)]
    struct Vec3
    {
        public float x, y, z;

        public void FromVector3(Vector3 v)
        {
            this.x = v.x; this.y = v.y; this.z = v.z;
        }
    }

    [StructLayout(LayoutKind.Sequential)]
    struct Vec4
    {
        public float x, y, z, w;
        public void FromQuaternion(Quaternion q) { this.x = q.x; this.y = q.y ; this.z = q.z; this.w = q.w; }
    }

    [StructLayout(LayoutKind.Sequential)]
    struct CameraToolsData
    {
        public byte cameraEnabled;
        public byte cameraMovementLocked;
        public byte reserved1;
        public byte reserved2;
        public float fov;
        public Vec3 coordinates;
        public Vec4 lookQuaternion;
        public Vec3 rotationMatrixUpVector;
        public Vec3 rotationMatrixRightVector;
        public Vec3 rotationMatrixForwardVector;
        public float pitch;
        public float yaw;
        public float roll;
    }

    internal static class NativeMethods
    {
        [DllImport("kernel32.dll")]
        public static extern IntPtr LoadLibrary(string dll);

        [DllImport("kernel32.dll")]
        public static extern IntPtr GetProcAddress(IntPtr hModule, string procName);
    }
    public class UnityIGCSConnector
    {
        // UnityIGCSConnector.dll definitions.
        [UnmanagedFunctionPointer(CallingConvention.Cdecl)]
        private delegate void MoveCameraCallback(float step_left, float step_up, float fov, int from_start);

        [UnmanagedFunctionPointer(CallingConvention.Cdecl)]
        private delegate int SessionCallback();

        [UnmanagedFunctionPointer(CallingConvention.Cdecl)]
        private delegate IntPtr U_IGCS_Initialize(MoveCameraCallback callback, SessionCallback start_cb, SessionCallback end_cb);

        // Store the initial position when a session start in IGCSDof.
        Mono.CSharp.Tuple<Vector3, Quaternion> position = null;

        // When we end a session, we need to make sure to go back to the position when the session ends.
        private Mono.CSharp.Tuple<Vector3, Quaternion> endSessionPosition = null;

        private readonly bool isValid = false;
        private bool _isActive = false;
        public bool IsActive => isValid && _isActive;

        // Delegate holder for the MoveCamera, StartSession and EndSession.
        private readonly List<System.Delegate> delegates = new();

        // Since some games use multi-threaded, in order to make sure we're only moving things during
        // the main thread is executing, we use this Queue to enqueue the move commands and dequeue them in the Update function.
        // This object *must* be used with a Lock.
        private readonly Queue<StepCommand> commands = new();

        private IntPtr AllCamerasData = IntPtr.Zero;

        private IEnumerable<IntPtr> GetValidCameras()
        {
            for (int i = 0; i < 10; i++)
            {
                IntPtr camPtr = Marshal.ReadIntPtr(AllCamerasData, i * IntPtr.Size);

                if (camPtr == IntPtr.Zero)
                    yield break;

                yield return camPtr;
            }
        }
        public void UpdateFreecamStatus(bool enabled)
        {
            if (AllCamerasData == IntPtr.Zero) return;
            unsafe
            {
                foreach (IntPtr pCamera in this.GetValidCameras())
                {
                    CameraToolsData* data = (CameraToolsData*)pCamera;
                    data->cameraEnabled = enabled ? (byte)1 : (byte) 0;
                }
            }
        }

        internal void UpdateCameraData(Camera cam)
        {
            if (AllCamerasData == IntPtr.Zero) return;

            Transform transform = cam.transform;
            unsafe
            {
                foreach (IntPtr pCamera in this.GetValidCameras())
                {
                    CameraToolsData* data = (CameraToolsData*)pCamera;
                    data->fov = cam.fieldOfView;
                    data->cameraMovementLocked = _isActive ? (byte)1 : (byte)0;

                    data->coordinates.FromVector3(transform.position);

                    data->lookQuaternion.FromQuaternion(transform.rotation);

                    data->rotationMatrixRightVector.FromVector3(transform.right);
                    data->rotationMatrixUpVector.FromVector3(transform.up);
                    data->rotationMatrixForwardVector.FromVector3(transform.forward);

                    Vector3 rot = transform.eulerAngles;
                    data->pitch = rot.x * Mathf.Deg2Rad;
                    data->yaw = rot.y * Mathf.Deg2Rad;
                    data->roll = rot.z * Mathf.Deg2Rad;
                }
            }
        }

        public void ExecuteCameraCommand(Camera cam)
        {
            var transform = cam.transform;

            // Check whether we should go back to the original position despite being active or not
            this.ShouldMoveToOriginalPosition(transform);

            if (!_isActive || position == null)
            {
                position = new(transform.position, transform.rotation);
            }

            if (!_isActive || position == null) { return; }

            StepCommand c = null;

            lock (commands)
            {
                if (commands.Count <= 0) return;
                c = commands.Dequeue();
            }

            transform.position = position.Item1;
            transform.rotation = position.Item2;
            transform.Translate(c.Item1, c.Item2, 0.0f);
        }

        private void MoveCamera(float stepLeft, float stepUp, float fov, int fromStart)
        {
            lock (commands)
            {
                commands.Enqueue(new StepCommand(stepLeft, stepUp));
            }
        }
        private int StartSession()
        {
            if (_isActive) return 3;
            _isActive = true;
            return 0;
        }

        // At the EndSession, since we have a queue system, we have to have a special check when the session ends and
        // then move the camera back to the original position, because the queue gets cleaned as soon as the session
        // ends.
        public void ShouldMoveToOriginalPosition(Transform transform)
        {
            if (!isValid) return;
            if (endSessionPosition == null) return;

            transform.position = endSessionPosition.Item1;
            transform.rotation = endSessionPosition.Item2;

            endSessionPosition = null;
        }

        private int EndSession()
        {
            endSessionPosition = position;
            position = null;
            _isActive = false;

            lock (commands)
                commands.Clear();

            return 0;
        }

        public UnityIGCSConnector()
        {
            var libraryName = IntPtr.Size == 8 ? @"UnityIGCSConnector.dll" : @"UnityIGCSConnector.32.dll";
            var lib = NativeMethods.LoadLibrary(libraryName);
            if (lib == IntPtr.Zero) 
            {
                ExplorerCore.LogWarning($"{libraryName} was not found so IGCSDof will not be available");
                return;
            }

            var func = NativeMethods.GetProcAddress(lib, @"U_IGCS_Initialize");
            if (func == IntPtr.Zero)
            {
                throw new EntryPointNotFoundException("Failed to find 'U_IGCS_Initialize' which means you can have a corrupt UnityIGCSConnector.dll.");
            }

            var initFunc = (U_IGCS_Initialize)Marshal.GetDelegateForFunctionPointer(func, typeof(U_IGCS_Initialize));

            delegates.Add(new MoveCameraCallback(MoveCamera));
            delegates.Add(new SessionCallback(StartSession));
            delegates.Add(new SessionCallback(EndSession));

            AllCamerasData = initFunc((MoveCameraCallback)delegates[0], (SessionCallback)delegates[1], (SessionCallback)delegates[2]);
            if (AllCamerasData == IntPtr.Zero){
                // This is actually a InvalidDataException, but some games dont allow you to throw that.
                throw new Exception("IGCSDof returned an invalid pointer which means something went wrong");
            }

            isValid = true;
        }
    }
}
