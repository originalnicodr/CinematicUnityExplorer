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

    static class NativeMethods
    {
        [DllImport("kernel32.dll")]
        public static extern IntPtr LoadLibrary(string dll);

        [DllImport("kernel32.dll")]
        public static extern IntPtr GetProcAddress(IntPtr hModule, string procName);
    }
    public class UnityIGCSConnector
    {
        // UnityIGCSConnector.dll definitions.
        private delegate void MoveCameraCallback(float step_left, float step_up, float fov, int from_start);
        private delegate void SessionCallback();
        private delegate void U_IGCS_Initialize(MoveCameraCallback callback, SessionCallback start_cb, SessionCallback end_cb);

        // Store the initial position when a session start in IGCSDof.
        Mono.CSharp.Tuple<Vector3, Quaternion> position = null;

        private bool isValid = false;
        private bool _isActive = false;
        public bool isActive
        {
            get
            {
                return isValid && _isActive;
            }
        }

        // Since some games use multithreaded, in order to make sure we're only moving things during
        // the main thread is executing, we use this Queue to enqueue the move commands and dequeue them in the Update function.
        // This object *must* be used with a Lock.
        Queue<StepCommand> commands = new();

        public void executeCameraCommand(Camera cam)
        {
            if (!_isActive || position == null)
            {
                position = new(cam.transform.position, cam.transform.rotation);
            }

            if (!_isActive || position == null) { return; }

            StepCommand c = null;

            lock (commands)
            {
                if (commands.Count <= 0) return;
                c = commands.Dequeue();
            }

            cam.transform.position = position.Item1;
            cam.transform.rotation = position.Item2;
            cam.transform.Translate(c.Item1, c.Item2, 0.0f);
        }

        private void MoveCamera(float step_left, float step_up, float fov, int from_start)
        {
            lock (commands)
            {
                commands.Enqueue(new StepCommand(step_left, step_up));
            }
        }
        private void StartSession()
        {
            _isActive = true;
        }

        private void EndSession()
        {
            position = null;
            _isActive = false;
        }

        public UnityIGCSConnector()
        {
            var lib = NativeMethods.LoadLibrary(@"UnityIGCSConnector.dll");
            if (lib == IntPtr.Zero) 
            {
                ExplorerCore.LogWarning("UnityIGCSConnector.dll was not found so IGCSDof will not be available");
                return;
            }

            var func = NativeMethods.GetProcAddress(lib, @"U_IGCS_Initialize");
            if (func == IntPtr.Zero)
            {
                throw new EntryPointNotFoundException("Failed to find 'U_IGCS_Initialize' which means you cna have a corrupt UnityIGCSConnector.dll.");
            }

            var initFunc = (U_IGCS_Initialize)Marshal.GetDelegateForFunctionPointer(func, typeof(U_IGCS_Initialize));

            var startSession = new SessionCallback(this.StartSession);
            var endSession = new SessionCallback(this.EndSession);
            var moveCamera = new MoveCameraCallback(this.MoveCamera);

            var hStartSession = GCHandle.Alloc(startSession, GCHandleType.Pinned);
            var hEndSession = GCHandle.Alloc(endSession, GCHandleType.Pinned);
            var hMoveCamera = GCHandle.Alloc(moveCamera, GCHandleType.Pinned);
            initFunc(moveCamera, startSession, endSession);
            isValid = true;
        }
    }
}
