// dllmain.cpp : Defines the entry point for the DLL application.
#include <Windows.h>
#include <stdint.h>
#include <stdio.h>
#include <Psapi.h>

#define EXPOSE __declspec(dllexport)

// Since we're using C, we're a bit limited on how we define enums,
// so for the screenshot session, we have to do this.

#ifdef _M_IX86
typedef uint8_t SessionStatus;
#else
typedef int SessionStatus;
#endif

#define SESSION_STATUS_OK ((SessionStatus)0)
#define SESSION_STATUS_CAMERA_NOT_ENABLED ((SessionStatus)1)
#define SESSION_STATUS_CAMERA_PATH_PLAYING ((SessionStatus)2)
#define SESSION_STATUS_ALREADY_SESSION_ACTIVE ((SessionStatus)3)
#define SESSION_STATUS_CAMERA_FEAT_NOT_AVAILABLE ((SessionStatus)4)
#define SESSION_STATUS_UNKNOWN_ERROR ((SessionStatus)5)

// The function that will be the one called from the C# side, that will update the corresponding values.
typedef void (*MoveCameraCallback)(float, float, float, int);

typedef int (*SessionCallback)(void);

typedef uint8_t* (*GetCameraDataFunc)(void);

MoveCameraCallback GlobalCallback = NULL;
SessionCallback GlobalStartSession = NULL;
SessionCallback GlobalEndSession = NULL;

EXPOSE uint8_t* U_IGCS_CameraData[10];

// There are things that only needs to be run once.
static int first_initialization = 1;



EXPOSE SessionStatus __cdecl IGCS_StartScreenshotSession(uint8_t _ignore) {
  if (GlobalStartSession) {
    printf("Called StartSession\n");
    return (SessionStatus)GlobalStartSession();
  }
  return SESSION_STATUS_CAMERA_FEAT_NOT_AVAILABLE;
}

EXPOSE void __cdecl IGCS_EndScreenshotSession() {
  GlobalEndSession();
  printf("Called EndSession\n");
}

int connect_all_tools() {
  HMODULE mods[1024];
  DWORD cbNeeded;
  HANDLE proc = GetCurrentProcess();
  int cameraSize = 0;

  if (!EnumProcessModules(proc, mods, sizeof(mods), &cbNeeded)) {
    return -1;
  }

  for (int i = 0; i < (cbNeeded / sizeof(HMODULE)); i++) {
    FARPROC connect_camera_tools = GetProcAddress(mods[i], "connectFromCameraTools");
    GetCameraDataFunc getCameraData = (GetCameraDataFunc)GetProcAddress(mods[i], "getDataFromCameraToolsBuffer");

    if (connect_camera_tools == NULL) continue;

    char module_name[512];
    GetModuleFileNameA(mods[i], module_name, sizeof(module_name));

    printf("Connected to %s tool\n", module_name);
    connect_camera_tools();
    U_IGCS_CameraData[cameraSize] = getCameraData();
    cameraSize++;

  }
  return 0;
}

EXPOSE uint8_t* __cdecl U_IGCS_Initialize(MoveCameraCallback cb, SessionCallback start_cb, SessionCallback end_cb) {
  AllocConsole();
  printf("Initializing callback\n");
  GlobalCallback = cb;
  GlobalStartSession = start_cb;
  GlobalEndSession = end_cb;

  if (first_initialization) {
    
    if (connect_all_tools()) {
      printf("Error connecting cameras\n");
      return 0;
    }
    first_initialization = 0;
  }

  printf("Camera connected!\n");

  return &U_IGCS_CameraData[0];

}

EXPOSE void __cdecl IGCS_MoveCameraPanorama() {}

EXPOSE void __cdecl IGCS_MoveCameraMultishot(float step_left, float step_up, float fov, int from_start) {
  GlobalCallback(step_left, step_up, fov, from_start);
  return;
}

BOOL WINAPI DllMain(HMODULE hModule,
  DWORD  ul_reason_for_call,
  LPVOID lpReserved
)
{
  switch (ul_reason_for_call)
  {
  case DLL_PROCESS_ATTACH:
  case DLL_THREAD_ATTACH:
  case DLL_THREAD_DETACH:
  case DLL_PROCESS_DETACH:
    break;
  }
  return TRUE;
}