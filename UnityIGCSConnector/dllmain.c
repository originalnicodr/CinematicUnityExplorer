// dllmain.cpp : Defines the entry point for the DLL application.
#include <Windows.h>
#include <stdint.h>
#include <stdio.h>

#define EXPOSE __declspec(dllexport)

// The function that will be the one called from the C# side, that will update the corresponding values.
typedef void (*MoveCameraCallback)(float, float, float, int);

typedef void (*SessionCallback)(void);

MoveCameraCallback GlobalCallback = NULL;
SessionCallback GlobalStartSession = NULL;
SessionCallback GlobalEndSession = NULL;

// There are things that only needs to be run once.
static int first_initialization = 1;

EXPOSE int IGCS_StartScreenshotSession(uint8_t _ignore) {
  if (GlobalStartSession) {
    GlobalStartSession();
    printf("Called StartSession\n");
  }
  return 0;
}

EXPOSE void IGCS_EndScreenshotSession() {}

EXPOSE void U_IGCS_Initialize(MoveCameraCallback cb, SessionCallback start_cb, SessionCallback end_cb) {
  AllocConsole();
  printf("Initializing callback\n");
  GlobalCallback = cb;
  GlobalStartSession = start_cb;

  // Load IGCS
  HMODULE igcs = LoadLibraryA("IgcsConnector.addon64");

  if (!igcs) {
    MessageBoxA(
      NULL,
      "Unable to find IgcsConnector",
      "IgcsConnector.addon64 was not found, make sure it is in the same directory as the executable.",
      MB_OK | MB_ICONERROR);
    return;
  }

  FARPROC cameraToolsFunction = GetProcAddress(igcs, "connectFromCameraTools");
  FARPROC getCameraData = GetProcAddress(igcs, "getDataFromCameraToolsBuffer");

  if (first_initialization) {
    cameraToolsFunction();
    first_initialization = 0;
  }

  // TODO: move this where it belongs. Maybe at some point we should actually fill in the data.
  uint8_t* cameraData = getCameraData();
  cameraData[0] = 1;

  printf("Camera connected!\n");

  return;

}

EXPOSE void IGCS_MoveCameraPanorama() {}

EXPOSE void IGCS_MoveCameraMultishot(float step_left, float step_up, float fov, int from_start) {
  printf("step_left: %f\t\tstep_up: %f\t\tfov: %f\t\tfrom_start: %d\n", step_left, step_up, fov, from_start);
  GlobalCallback(step_left, step_up, fov, from_start);

  return;
}



BOOL WINAPI DllMain( HMODULE hModule,
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